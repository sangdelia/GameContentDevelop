using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class TempBossController : MonoBehaviour
{
    private const string ToiletMechBossResourcePath = "Models/Boss/ToiletMech_Boss";

    private enum BossState
    {
        Idle,
        Chase,
        MeleeAttack,
        LaserAttack,
        FlameAttack,
        Dead
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.2f;
    [SerializeField] private float closeAttackDistance = 4f;
    [SerializeField] private float fightStartGraceTime = 3f;
    [SerializeField] private float bossCollisionRadius = 1.55f;
    [SerializeField] private float bossCollisionHeight = 4.2f;
    [SerializeField] private float bossCollisionCenterY = 0f;
    [SerializeField] private float obstacleProbeDistance = 2.1f;
    [SerializeField] private float sideStepProbeDistance = 2.8f;
    [SerializeField] private float moveDirectionSmoothSpeed = 8f;
    [SerializeField] private float turnSmoothSpeed = 10f;

    [Header("Melee")]
    [SerializeField] private float closeAttackDamage = 18f;
    [SerializeField] private float meleeAttackCooldown = 1.35f;
    [SerializeField] private float meleeAttackDuration = 1.05f;
    [SerializeField] private float meleeRecoveryDuration = 0.55f;
    [SerializeField] private float meleeImpactDelay = 0.46f;
    [SerializeField] private float meleeHitboxActiveTime = 0.28f;
    [SerializeField] private Vector3 meleeDetectionOffset = new Vector3(0f, 0.8f, 0.55f);
    [SerializeField] private Vector3 meleeDetectionSize = new Vector3(3.1f, 2.4f, 3.05f);

    [Header("Laser")]
    [SerializeField] private float deathBeamDamage = 55f;
    [SerializeField] private float deathBeamRange = 45f;
    [SerializeField] private float deathBeamRadius = 0.55f;
    [SerializeField] private float deathBeamWarningTime = 1.4f;
    [SerializeField] private float deathBeamFireDelay = 0.18f;
    [SerializeField] private float deathBeamVisibleTime = 0.18f;

    [Header("Flame Thrower")]
    [SerializeField] private float flameThrowerRange = 10f;
    [SerializeField] private float flameThrowerAngle = 54f;
    [SerializeField] private float flameThrowerWarningTime = 0.45f;
    [SerializeField] private float flameThrowerActiveTime = 0.85f;

    [Header("Pattern")]
    [SerializeField] private float patternInterval = 3.2f;

    [Header("Death")]
    [SerializeField] private float fallbackBossDeathDestroyDelay = 2.2f;

    [Header("ToiletMech Visual")]
    [SerializeField] private Vector3 toiletMechLocalPosition = new Vector3(0f, -1.05f, 0f);
    [SerializeField] private Vector3 toiletMechLocalRotation = Vector3.zero;
    [SerializeField] private Vector3 toiletMechLocalScale = Vector3.one * 1.28f;
    [SerializeField] private float visualGroundClearance = 0.03f;
    [SerializeField] private float groundProbeHeight = 8f;
    [SerializeField] private float groundProbeDistance = 20f;

    private Transform player;
    private PlayerHealth playerHealth;
    private EnemyHealth health;
    private BossState state = BossState.Idle;
    private float patternTimer;
    private float nextMeleeAttackTime;
    private float nextHitReactionTime;
    private int rangedPatternIndex;
    private float bossStartTime;
    private Vector3 smoothedMoveDirection;

    private Renderer bodyRenderer;
    private LineRenderer warningLine;
    private LineRenderer fireLine;
    private Transform topRing;
    private Transform lowerRing;
    private Transform eyeCore;
    private Transform bossAura;
    private Transform bossModelRoot;
    private Transform leftWeapon;
    private Transform rightWeapon;
    private Transform shoulderArray;
    private GameObject bossModelInstance;
    private Animator bossAnimator;
    private BossMeleeDetectionBox meleeDetectionBox;
    private BossMeleeHitbox[] meleeHitboxes;
    private readonly HashSet<PlayerHealth> meleeHitPlayers = new HashSet<PlayerHealth>();
    private readonly HashSet<string> missingAnimatorStateLogs = new HashSet<string>();

    private CapsuleCollider bossCollider;
    private Rigidbody bossRigidbody;
    private bool importedBossVisuals;
    private bool playerInMeleeZone;
    private bool meleeHitboxesActive;
    private string currentBossAnimationName;
    private Vector3 importedBossModelAnchorPosition;
    private Quaternion importedBossModelAnchorRotation;
    private Vector3 importedBossModelAnchorScale;
    private Transform bossAnimatorTransform;
    private Vector3 bossAnimatorAnchorPosition;
    private Quaternion bossAnimatorAnchorRotation;
    private Vector3 bossAnimatorAnchorScale;
    private Color normalColor = new Color(0.55f, 0.08f, 0.9f);
    private Color warningColor = new Color(1f, 0.05f, 0.05f);

    public event System.Action Defeated;

    public static TempBossController Create(Vector3 position, Transform target, float hp)
    {
        GameObject bossObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bossObject.name = "Temp_Boss_Stargrave_Core";
        bossObject.transform.position = position;
        bossObject.transform.localScale = new Vector3(3f, 3f, 3f);

        EnemyHealth bossHealth = bossObject.AddComponent<EnemyHealth>();
        bossHealth.Configure(hp, null, 0);
        bossHealth.SetDestroyDelay(2.2f);

        TempBossController boss = bossObject.AddComponent<TempBossController>();
        boss.Init(target);

        return boss;
    }

    public void Init(Transform target)
    {
        player = target;
        playerHealth = player != null ? player.GetComponent<PlayerHealth>() : null;
    }

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        health.Died += HandleDied;
        health.Damaged += HandleDamaged;
        health.SetDestroyDelay(fallbackBossDeathDestroyDelay);

        bodyRenderer = GetComponent<Renderer>();
        bodyRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bodyRenderer.material.color = normalColor;
        bodyRenderer.material.EnableKeyword("_EMISSION");
        bodyRenderer.material.SetColor("_EmissionColor", normalColor * 0.8f);
        bodyRenderer.enabled = false;
        SnapBossRootToGround();
        ConfigureBossBodyCollision();

        bossStartTime = Time.time;
        BuildBossVisuals();

        warningLine = CreateLine("DeathBeamWarningLine", new Color(1f, 0.05f, 0.05f, 0.75f), 0.08f);
        fireLine = CreateLine("DeathBeamFireLine", new Color(1f, 0.1f, 0.02f, 1f), deathBeamRadius * 2f);
        UpdateDeathDestroyDelay();
        SetState(BossState.Idle);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= HandleDied;
            health.Damaged -= HandleDamaged;
        }
    }

    private void Update()
    {
        AnimateBossVisuals();

        if (state == BossState.Dead || player == null || health.IsDead)
            return;

        if (state == BossState.MeleeAttack || state == BossState.LaserAttack || state == BossState.FlameAttack)
            return;

        UpdateIdleOrChase();
    }

    private void LateUpdate()
    {
        if (state != BossState.Dead)
        {
            SnapBossRootToGround();
        }

        KeepImportedBossModelAnchored();
    }

    private void UpdateIdleOrChase()
    {
        FacePlayer();

        if (Time.time - bossStartTime < fightStartGraceTime)
        {
            if (playerInMeleeZone && Time.time >= nextMeleeAttackTime)
            {
                StartCoroutine(MeleeAttackRoutine());
                return;
            }

            MoveTowardPlayer();
            return;
        }

        patternTimer += Time.deltaTime;
        if (patternTimer >= patternInterval)
        {
            patternTimer = 0f;
            StartNextRangedPattern();
            return;
        }

        if (playerInMeleeZone)
        {
            if (Time.time >= nextMeleeAttackTime)
            {
                StartCoroutine(MeleeAttackRoutine());
            }
            else
            {
                SetState(BossState.Idle);
            }

            return;
        }

        MoveTowardPlayer();
    }

    private void MoveTowardPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= closeAttackDistance)
        {
            SetState(BossState.Idle);
            return;
        }

        SetState(BossState.Chase);
        MoveWithObstacleAvoidance(direction.normalized, moveSpeed * Time.deltaTime);
    }

    private void MoveWithObstacleAvoidance(Vector3 desiredDirection, float distance)
    {
        if (desiredDirection.sqrMagnitude < 0.001f || distance <= 0f)
            return;

        desiredDirection.y = 0f;
        desiredDirection.Normalize();
        desiredDirection = SmoothBossMoveDirection(desiredDirection);

        if (!CapsuleCastBoss(desiredDirection, Mathf.Max(distance, obstacleProbeDistance), out RaycastHit directHit))
        {
            MoveBossRoot(desiredDirection * distance);
            RotateToward(desiredDirection);
            return;
        }

        Vector3 left = Quaternion.Euler(0f, -72f, 0f) * desiredDirection;
        Vector3 right = Quaternion.Euler(0f, 72f, 0f) * desiredDirection;
        ChooseSideStepOrder(left, right, out Vector3 firstSideStep, out Vector3 secondSideStep);

        if (TrySideStep(firstSideStep, distance))
        {
            return;
        }

        if (TrySideStep(secondSideStep, distance))
        {
            return;
        }

        Vector3 slide = Vector3.ProjectOnPlane(desiredDirection, directHit.normal);
        slide.y = 0f;

        if (slide.sqrMagnitude > 0.001f && !CapsuleCastBoss(slide.normalized, Mathf.Max(distance, obstacleProbeDistance * 0.7f), out _))
        {
            MoveBossRoot(slide.normalized * distance * 0.75f);
            RotateToward(slide);
            return;
        }

        SetState(BossState.Idle);
    }

    private Vector3 SmoothBossMoveDirection(Vector3 desiredDirection)
    {
        if (smoothedMoveDirection.sqrMagnitude < 0.001f)
        {
            smoothedMoveDirection = desiredDirection;
            return desiredDirection;
        }

        float blend = 1f - Mathf.Exp(-moveDirectionSmoothSpeed * Time.deltaTime);
        smoothedMoveDirection = Vector3.Slerp(smoothedMoveDirection, desiredDirection, blend);
        smoothedMoveDirection.y = 0f;

        if (smoothedMoveDirection.sqrMagnitude < 0.001f)
            return desiredDirection;

        return smoothedMoveDirection.normalized;
    }

    private void MoveBossRoot(Vector3 movement)
    {
        Vector3 nextPosition = transform.position + movement;
        nextPosition.y = GetGroundY(nextPosition);
        transform.position = nextPosition;
    }

    private bool TrySideStep(Vector3 direction, float distance)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return false;

        Vector3 normalized = direction.normalized;
        if (CapsuleCastBoss(normalized, Mathf.Max(distance, sideStepProbeDistance), out _))
            return false;

        MoveBossRoot(normalized * distance);
        RotateToward(normalized);
        return true;
    }

    private void ChooseSideStepOrder(Vector3 left, Vector3 right, out Vector3 first, out Vector3 second)
    {
        if (player == null)
        {
            first = left;
            second = right;
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.001f)
        {
            first = left;
            second = right;
            return;
        }

        float leftScore = Vector3.Dot(left.normalized, toPlayer.normalized);
        float rightScore = Vector3.Dot(right.normalized, toPlayer.normalized);

        if (leftScore >= rightScore)
        {
            first = left;
            second = right;
        }
        else
        {
            first = right;
            second = left;
        }
    }

    private void RotateToward(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float blend = 1f - Mathf.Exp(-turnSmoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blend);
        }
    }

    private bool CapsuleCastBoss(Vector3 direction, float distance, out RaycastHit hit)
    {
        GetBossCapsulePoints(out Vector3 point1, out Vector3 point2, out float radius);

        RaycastHit[] hits = Physics.CapsuleCastAll(
            point1,
            point2,
            radius,
            direction,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        hit = default;
        float nearestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit candidate = hits[i];

            if (candidate.collider == null || ShouldIgnoreBossMovementHit(candidate.collider))
                continue;

            if (candidate.distance < nearestDistance)
            {
                nearestDistance = candidate.distance;
                hit = candidate;
                found = true;
            }
        }

        return found;
    }

    private bool ShouldIgnoreBossMovementHit(Collider collider)
    {
        if (collider == null || collider.isTrigger)
            return true;

        if (collider == bossCollider || collider.transform.IsChildOf(transform))
            return true;

        if (collider.GetComponentInParent<PlayerHealth>() != null)
            return true;

        if (collider.GetComponentInParent<EnemyHealth>() != null)
            return true;

        if (collider.CompareTag("Ground"))
            return true;

        string objectName = collider.name;
        return objectName.Contains("Ground") || objectName.Contains("Floor") || objectName.Contains("Projectile");
    }

    private void GetBossCapsulePoints(out Vector3 point1, out Vector3 point2, out float radius)
    {
        if (bossCollider != null)
        {
            Vector3 colliderCenter = transform.TransformPoint(bossCollider.center);
            float colliderHeight = Mathf.Max(bossCollider.height * transform.lossyScale.y, 0.01f);
            radius = bossCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            float colliderHalfLine = Mathf.Max(0f, colliderHeight * 0.5f - radius);
            point1 = colliderCenter + Vector3.up * colliderHalfLine;
            point2 = colliderCenter - Vector3.up * colliderHalfLine;
            return;
        }

        Vector3 center = transform.position + Vector3.up * (bossCollisionHeight * 0.5f - 3f);
        float height = Mathf.Max(bossCollisionHeight, bossCollisionRadius * 2f + 0.1f);
        radius = Mathf.Max(0.2f, bossCollisionRadius);
        float halfLine = Mathf.Max(0f, height * 0.5f - radius);

        point1 = center + Vector3.up * halfLine;
        point2 = center - Vector3.up * halfLine;
    }

    private void FacePlayer()
    {
        if (state != BossState.Chase && state != BossState.Idle)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float blend = 1f - Mathf.Exp(-turnSmoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blend);
        }
    }

    private void FacePlayerForAiming()
    {
        if (state == BossState.Dead)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float blend = 1f - Mathf.Exp(-turnSmoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blend);
        }
    }

    private void StartNextRangedPattern()
    {
        if (state != BossState.Idle && state != BossState.Chase)
            return;

        rangedPatternIndex = (rangedPatternIndex + 1) % 2;
        if (rangedPatternIndex == 0)
        {
            StartCoroutine(LaserAttackRoutine());
        }
        else
        {
            StartCoroutine(FlameAttackRoutine());
        }
    }

    private System.Collections.IEnumerator MeleeAttackRoutine()
    {
        if (state == BossState.Dead || Time.time < nextMeleeAttackTime)
            yield break;

        SetState(BossState.MeleeAttack);
        nextMeleeAttackTime = Time.time + Mathf.Max(meleeAttackCooldown, meleeAttackDuration + meleeRecoveryDuration);
        PlayBossAnimation("Attack4", meleeAttackDuration);

        yield return new WaitForSeconds(meleeImpactDelay);

        if (state != BossState.MeleeAttack)
            yield break;

        EnableMeleeHitboxes(true);
        ApplyMeleeBodyContactDamage();

        float activeTime = Mathf.Min(meleeHitboxActiveTime, 0.12f);
        if (activeTime > 0f)
        {
            yield return new WaitForSeconds(activeTime);
        }

        EnableMeleeHitboxes(false);

        float remainingTime = Mathf.Max(0f, meleeAttackDuration - meleeImpactDelay - activeTime);
        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        CleanupAttackState();

        if (state != BossState.Dead)
        {
            SetState(BossState.Idle);
            PlayBossAnimation("Idle1_Toilet", meleeRecoveryDuration, true);

            if (meleeRecoveryDuration > 0f)
            {
                yield return new WaitForSeconds(meleeRecoveryDuration);
            }
        }

        ReturnToMovementState();
    }

    private System.Collections.IEnumerator LaserAttackRoutine()
    {
        if (state == BossState.Dead)
            yield break;

        SetState(BossState.LaserAttack);
        PlayBossAnimation("Roar", deathBeamWarningTime);

        Vector3 origin = GetBeamOrigin();
        Vector3 aimDirection = GetAimDirectionToPlayer(origin);

        try
        {
            GameAudio.PlayBossWarning(origin);
            ConfigureWarningLine(new Color(1f, 0.05f, 0.05f, 0.75f), 0.08f, 0.08f);
            warningLine.enabled = true;

            float timer = 0f;
            while (timer < deathBeamWarningTime)
            {
                FacePlayerForAiming();
                origin = GetBeamOrigin();
                aimDirection = GetAimDirectionToPlayer(origin);
                SetLine(warningLine, origin, GetBeamEnd(origin, aimDirection));

                timer += Time.deltaTime;
                yield return null;
            }

            warningLine.enabled = false;

            origin = GetBeamOrigin();
            Vector3 lockedDirection = aimDirection.normalized;
            Vector3 end = GetBeamEnd(origin, lockedDirection);

            PlayBossAnimation("Attack3", deathBeamFireDelay + deathBeamVisibleTime + 0.25f);
            yield return new WaitForSeconds(deathBeamFireDelay);

            if (state != BossState.LaserAttack)
                yield break;

            SetLine(fireLine, origin, end);
            fireLine.enabled = true;
            GameVfx.SpawnLaserImpact(end, lockedDirection, new Color(1f, 0.12f, 0.03f));
            GameAudio.PlayBossLaser(origin);
            ApplyBeamDamage(origin, lockedDirection);

            yield return new WaitForSeconds(deathBeamVisibleTime);
        }
        finally
        {
            CleanupAttackState();
        }

        ReturnToMovementState();
    }

    private System.Collections.IEnumerator FlameAttackRoutine()
    {
        if (state == BossState.Dead)
            yield break;

        SetState(BossState.FlameAttack);
        PlayBossAnimation("Attack5", flameThrowerWarningTime + flameThrowerActiveTime + 0.25f);

        Vector3 origin = GetAttackOrigin();
        Vector3 aimDirection = GetAimDirectionToPlayer(origin);

        try
        {
            ConfigureWarningLine(new Color(1f, 0.45f, 0.05f, 0.82f), 0.18f, 1.6f);
            warningLine.enabled = true;

            float timer = 0f;
            while (timer < flameThrowerWarningTime)
            {
                FacePlayerForAiming();
                origin = GetAttackOrigin();
                aimDirection = GetAimDirectionToPlayer(origin);
                SetLine(warningLine, origin, origin + aimDirection * flameThrowerRange);

                timer += Time.deltaTime;
                yield return null;
            }

            warningLine.enabled = false;

            origin = GetAttackOrigin();
            Vector3 lockedDirection = aimDirection.normalized;
            ConfigureFireLine(new Color(1f, 0.58f, 0.05f, 1f), new Color(1f, 0.05f, 0.02f, 0.75f), 0.7f, 3.1f);
            fireLine.enabled = true;
            GameAudio.PlayBossLaser(origin);

            timer = 0f;
            HashSet<PlayerHealth> flameHitPlayers = new HashSet<PlayerHealth>();
            while (timer < flameThrowerActiveTime)
            {
                origin = GetAttackOrigin();
                Vector3 end = origin + lockedDirection * flameThrowerRange;
                SetLine(fireLine, origin, end);
                GameVfx.SpawnLaserImpact(end, lockedDirection, new Color(1f, 0.32f, 0.02f));

                if (IsPlayerInsideFlameCone(origin, lockedDirection) && !flameHitPlayers.Contains(playerHealth))
                {
                    flameHitPlayers.Add(playerHealth);
                    ApplyFlameThrowerKill(playerHealth);
                }

                timer += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            CleanupAttackState();
        }

        ReturnToMovementState();
    }

    private void SetState(BossState nextState)
    {
        if (state == BossState.Dead && nextState != BossState.Dead)
            return;

        if (state == nextState)
            return;

        state = nextState;

        if (state == BossState.Idle)
        {
            PlayBossAnimation("Idle1_Toilet");
        }
        else if (state == BossState.Chase)
        {
            PlayBossAnimation("Walk_F");
        }
    }

    private void ReturnToMovementState()
    {
        if (state == BossState.Dead || health == null || health.IsDead)
            return;

        if (playerInMeleeZone)
        {
            SetState(BossState.Idle);
            return;
        }

        if (GetFlatDistanceToPlayer() > closeAttackDistance)
        {
            SetState(BossState.Chase);
        }
        else
        {
            SetState(BossState.Idle);
        }
    }

    private void CleanupAttackState()
    {
        EnableMeleeHitboxes(false);

        if (warningLine != null)
        {
            warningLine.enabled = false;
        }

        if (fireLine != null)
        {
            fireLine.enabled = false;
        }

        ResetBossAttackLines();
        bodyRenderer.material.color = normalColor;
        bodyRenderer.material.SetColor("_EmissionColor", normalColor * 0.8f);
    }

    private void BuildBossVisuals()
    {
        bossModelRoot = new GameObject("BossModelRoot").transform;
        bossModelRoot.SetParent(transform, false);
        bossModelRoot.localPosition = Vector3.zero;
        bossModelRoot.localRotation = Quaternion.identity;
        bossModelRoot.localScale = Vector3.one;

        bool importedBossApplied = TryCreateImportedToiletMechBoss();
        importedBossVisuals = importedBossApplied;

        if (importedBossApplied)
            return;

        CreateBossModelPart("Models/KenneySpace/room-small-variation", "BossArmoredShell", new Vector3(0f, -0.05f, 0f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.2f, normalColor, 0.8f);
        CreateBossModelPart("Models/SpaceStation/computer-system", "BossReactorTorso", new Vector3(0f, 0.08f, 0.08f), Quaternion.identity, Vector3.one * 2.8f, normalColor, 1.1f);
        CreateBossModelPart("Models/KenneySpace/template-wall-detail-a", "BossBackArmor", new Vector3(0f, 0.2f, -0.58f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.6f, new Color(0.25f, 0.08f, 0.36f), 0.7f);
        leftWeapon = CreateBossModelPart("Models/SpaceStation/pipe-ring-colored", "BossLeftEmitter", new Vector3(-1.1f, 0.08f, 0.62f), Quaternion.Euler(0f, -8f, 0f), new Vector3(2.2f, 2.2f, 3.6f), new Color(0.1f, 0.85f, 1f), 1.6f);
        rightWeapon = CreateBossModelPart("Models/SpaceStation/pipe-ring-colored", "BossRightEmitter", new Vector3(1.1f, 0.08f, 0.62f), Quaternion.Euler(0f, 8f, 0f), new Vector3(2.2f, 2.2f, 3.6f), new Color(1f, 0.2f, 0.75f), 1.5f);
        shoulderArray = CreateBossModelPart("Models/KenneySpace/cables", "BossShoulderCableArray", new Vector3(0f, 0.86f, -0.12f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.3f, new Color(0.08f, 0.9f, 1f), 1.0f);
        topRing = CreateBossRing("TopReactorRing", new Vector3(0f, 0.82f, 0f), new Vector3(1.28f, 0.05f, 1.28f), new Color(0.1f, 0.9f, 1f));
        lowerRing = CreateBossRing("LowerReactorRing", new Vector3(0f, -0.42f, 0f), new Vector3(1.08f, 0.04f, 1.08f), new Color(1f, 0.2f, 0.9f));

        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "BossEyeCore";
        eye.transform.SetParent(transform, false);
        eye.transform.localPosition = new Vector3(0f, 0.12f, 0.55f);
        eye.transform.localScale = Vector3.one * 0.28f;
        Destroy(eye.GetComponent<Collider>());
        ApplyBossMaterial(eye, new Color(1f, 0.05f, 0.05f), 2.4f);
        eyeCore = eye.transform;

        bossAura = GameVfx.CreatePersistentVfxQuad(
            "BossCoreAuraSprite",
            transform,
            new Vector3(0f, 0.15f, 0.64f),
            Quaternion.identity,
            Vector3.one * 1.35f,
            "circle_05",
            new Color(1f, 0.1f, 0.55f, 0.86f)
        );
    }

    private Transform CreateBossRing(string name, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = name;
        ring.transform.SetParent(transform, false);
        ring.transform.localPosition = localPosition;
        ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ring.transform.localScale = localScale;
        Destroy(ring.GetComponent<Collider>());
        ApplyBossMaterial(ring, color, 2f);
        return ring.transform;
    }

    private bool TryCreateImportedToiletMechBoss()
    {
        GameObject prefab = Resources.Load<GameObject>(ToiletMechBossResourcePath);

        if (prefab == null || bossModelRoot == null)
        {
            Debug.LogError($"[Boss] Failed to load boss prefab. Prefab null: {prefab == null}, BossModelRoot null: {bossModelRoot == null}");
            return false;
        }

        bossModelInstance = Instantiate(prefab, bossModelRoot);
        bossModelInstance.name = "Boss_ToiletMech_Model";
        bossModelInstance.transform.localPosition = toiletMechLocalPosition;
        bossModelInstance.transform.localRotation = Quaternion.Euler(toiletMechLocalRotation);
        bossModelInstance.transform.localScale = toiletMechLocalScale;
        bossAnimator = bossModelInstance.GetComponentInChildren<Animator>();

        if (bossAnimator == null)
        {
            Debug.LogError("[Boss] CRITICAL: Animator component not found on boss model instance.");
            return false;
        }

        if (bossAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError("[Boss] CRITICAL: Animator controller is null. Check the imported boss prefab.");
            return false;
        }

        if (bossAnimator.avatar == null)
        {
            Debug.LogWarning("[Boss] Animator avatar is null. Animation may not play correctly.");
        }

        bossAnimator.applyRootMotion = false;
        bossAnimator.enabled = true;
        bossAnimator.speed = 1f;
        bossAnimatorTransform = bossAnimator.transform;

        Collider[] colliders = bossModelInstance.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }

        BuildBossCombatHitboxes();
        FixImportedBossMaterials(bossModelInstance);
        AlignImportedBossVisualToGround();
        importedBossModelAnchorPosition = bossModelInstance.transform.localPosition;
        importedBossModelAnchorRotation = bossModelInstance.transform.localRotation;
        importedBossModelAnchorScale = bossModelInstance.transform.localScale;
        bossAnimatorAnchorPosition = bossAnimatorTransform.localPosition;
        bossAnimatorAnchorRotation = bossAnimatorTransform.localRotation;
        bossAnimatorAnchorScale = bossAnimatorTransform.localScale;
        return true;
    }

    private void AlignImportedBossVisualToGround()
    {
        if (bossModelInstance == null)
            return;

        if (!TryGetRendererWorldBounds(bossModelInstance, out Bounds bounds))
            return;

        float targetBottomY = GetGroundY(transform.position) + visualGroundClearance;
        float yOffset = targetBottomY - bounds.min.y;

        if (Mathf.Abs(yOffset) <= 0.001f)
            return;

        Vector3 worldPosition = bossModelInstance.transform.position + Vector3.up * yOffset;
        bossModelInstance.transform.position = worldPosition;
    }

    private void SnapBossRootToGround()
    {
        float groundY = GetGroundY(transform.position);
        transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
    }

    private bool TryGetRendererWorldBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || !renderers[i].enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return hasBounds;
    }

    private void ConfigureBossBodyCollision()
    {
        bossCollider = GetComponent<CapsuleCollider>();
        if (bossCollider == null)
        {
            bossCollider = gameObject.AddComponent<CapsuleCollider>();
        }

        bossCollider.isTrigger = false;
        bossCollider.radius = bossCollisionRadius / Mathf.Max(0.001f, transform.localScale.x);
        bossCollider.height = bossCollisionHeight / Mathf.Max(0.001f, transform.localScale.y);
        float worldCenterY = bossCollisionCenterY;
        if (Mathf.Approximately(worldCenterY, 0f))
        {
            float groundY = GetGroundY(transform.position);
            float localGroundY = transform.InverseTransformPoint(new Vector3(transform.position.x, groundY, transform.position.z)).y;
            worldCenterY = localGroundY * transform.lossyScale.y + bossCollisionHeight * 0.5f;
        }

        bossCollider.center = Vector3.up * (worldCenterY / Mathf.Max(0.001f, transform.localScale.y));

        bossRigidbody = GetComponent<Rigidbody>();
        if (bossRigidbody == null)
        {
            bossRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        bossRigidbody.useGravity = false;
        bossRigidbody.isKinematic = true;
        bossRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private float GetGroundY(Vector3 referencePosition)
    {
        Vector3 rayOrigin = referencePosition + Vector3.up * groundProbeHeight;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, groundProbeDistance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null)
                continue;

            if (hit.collider.GetComponentInParent<TempBossController>() != null)
                continue;

            if (hit.collider.GetComponentInParent<PlayerHealth>() != null)
                continue;

            if (hit.collider.GetComponentInParent<EnemyHealth>() != null)
                continue;

            string objectName = hit.collider.name;
            if (hit.collider.CompareTag("Ground") || objectName.Contains("ArenaFloor") || objectName.Contains("Ground") || objectName.Contains("Floor"))
            {
                return hit.point.y;
            }
        }

        return referencePosition.y;
    }

    private void KeepImportedBossModelAnchored()
    {
        if (!importedBossVisuals || bossModelInstance == null || state == BossState.Dead)
            return;

        Transform modelTransform = bossModelInstance.transform;
        modelTransform.localPosition = importedBossModelAnchorPosition;
        modelTransform.localRotation = importedBossModelAnchorRotation;
        modelTransform.localScale = importedBossModelAnchorScale;

        if (bossAnimatorTransform != null && bossAnimatorTransform != modelTransform)
        {
            bossAnimatorTransform.localPosition = bossAnimatorAnchorPosition;
            bossAnimatorTransform.localRotation = bossAnimatorAnchorRotation;
            bossAnimatorTransform.localScale = bossAnimatorAnchorScale;
        }
    }

    private void BuildBossCombatHitboxes()
    {
        GameObject detectionObject = new GameObject("BossMeleeDetectionBox");
        detectionObject.transform.SetParent(transform, false);
        detectionObject.transform.localPosition = meleeDetectionOffset;
        detectionObject.transform.localRotation = Quaternion.identity;

        BoxCollider detectionCollider = detectionObject.AddComponent<BoxCollider>();
        detectionCollider.isTrigger = true;
        detectionCollider.size = meleeDetectionSize;

        Rigidbody detectionBody = detectionObject.AddComponent<Rigidbody>();
        detectionBody.useGravity = false;
        detectionBody.isKinematic = true;

        meleeDetectionBox = detectionObject.AddComponent<BossMeleeDetectionBox>();
        meleeDetectionBox.Init(this);

        List<BossMeleeHitbox> hitboxes = new List<BossMeleeHitbox>();
        AddBoneHitbox(hitboxes, "LeftLeg", new[] { "leftleg", "left_leg", "leg_l", "l_leg", "leftfoot", "foot_l", "l_foot", "lefttoe", "toe_l", "l_toe", "thigh_l", "calf_l", "shin_l" }, new Vector3(0.48f, 0.58f, 0.82f));
        AddBoneHitbox(hitboxes, "RightLeg", new[] { "rightleg", "right_leg", "leg_r", "r_leg", "rightfoot", "foot_r", "r_foot", "righttoe", "toe_r", "r_toe", "thigh_r", "calf_r", "shin_r" }, new Vector3(0.48f, 0.58f, 0.82f));
        AddFixedMeleeHitbox(hitboxes, "ForwardSweep", meleeDetectionOffset + new Vector3(0f, 0.05f, 0.35f), meleeDetectionSize);
        AddFixedMeleeHitbox(hitboxes, "BodyContact", new Vector3(0f, 1.15f, 0.2f), new Vector3(2.4f, 2.8f, 2.2f));
        meleeHitboxes = hitboxes.ToArray();
        EnableMeleeHitboxes(false);
    }

    private void AddFixedMeleeHitbox(List<BossMeleeHitbox> hitboxes, string label, Vector3 localPosition, Vector3 size)
    {
        GameObject hitboxObject = new GameObject("Boss" + label + "Hitbox");
        hitboxObject.transform.SetParent(transform, false);
        hitboxObject.transform.localPosition = localPosition;
        hitboxObject.transform.localRotation = Quaternion.identity;
        hitboxObject.transform.localScale = Vector3.one;

        BoxCollider collider = hitboxObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = size;
        collider.enabled = false;

        Rigidbody body = hitboxObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;

        BossMeleeHitbox hitbox = hitboxObject.AddComponent<BossMeleeHitbox>();
        hitbox.Init(this, collider);
        hitboxes.Add(hitbox);
    }

    private void AddBoneHitbox(List<BossMeleeHitbox> hitboxes, string label, string[] nameHints, Vector3 size)
    {
        Transform bone = FindChildByNameHints(bossModelInstance != null ? bossModelInstance.transform : null, nameHints);

        if (bone == null)
        {
            Debug.LogError($"[Boss] Could not find {label} bone for melee hitbox. No fixed fallback hitbox was created.");
            return;
        }

        GameObject hitboxObject = new GameObject("Boss" + label + "Hitbox");
        hitboxObject.transform.SetParent(bone, false);
        hitboxObject.transform.localPosition = Vector3.zero;
        hitboxObject.transform.localRotation = Quaternion.identity;
        hitboxObject.transform.localScale = Vector3.one;

        BoxCollider collider = hitboxObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = size;
        collider.enabled = false;

        Rigidbody body = hitboxObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;

        BossMeleeHitbox hitbox = hitboxObject.AddComponent<BossMeleeHitbox>();
        hitbox.Init(this, collider);
        hitboxes.Add(hitbox);
    }

    private Transform FindChildByNameHints(Transform root, string[] hints)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string normalizedName = NormalizeName(children[i].name);
            for (int j = 0; j < hints.Length; j++)
            {
                if (normalizedName.Contains(NormalizeName(hints[j])))
                {
                    return children[i];
                }
            }
        }

        return null;
    }

    private string NormalizeName(string value)
    {
        return value.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
    }

    private void AnimateBossVisuals()
    {
        if (topRing != null)
        {
            topRing.Rotate(0f, 0f, 85f * Time.deltaTime, Space.Self);
        }

        if (lowerRing != null)
        {
            lowerRing.Rotate(0f, 0f, -120f * Time.deltaTime, Space.Self);
        }

        if (eyeCore != null)
        {
            float pulse = 0.24f + Mathf.Sin(Time.time * 5.5f) * 0.04f;
            eyeCore.localScale = Vector3.one * pulse;
        }

        if (bossAura != null)
        {
            bossAura.localRotation = Quaternion.Euler(0f, 0f, Time.time * 95f);
            float auraBaseSize = importedBossVisuals ? 0.95f : 1.25f;
            bossAura.localScale = Vector3.one * (auraBaseSize + Mathf.Sin(Time.time * 6.2f) * 0.12f);
        }

        if (bossModelRoot != null)
        {
            bossModelRoot.localPosition = importedBossVisuals ? Vector3.zero : Vector3.up * (Mathf.Sin(Time.time * 2.4f) * 0.06f);
        }

        if (leftWeapon != null)
        {
            leftWeapon.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 3.6f) * 3f, -8f, 0f);
        }

        if (rightWeapon != null)
        {
            rightWeapon.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 3.6f + Mathf.PI) * 3f, 8f, 0f);
        }

        if (shoulderArray != null)
        {
            shoulderArray.localRotation = Quaternion.Euler(0f, 90f + Mathf.Sin(Time.time * 2.2f) * 5f, 0f);
        }
    }

    private void ApplyBossMaterial(GameObject target, Color color, float emission)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = color;
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", color * emission);
    }

    private Transform CreateBossModelPart(string resourcePath, string partName, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Color color, float emission)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcePath);

        if (prefab == null || bossModelRoot == null)
            return null;

        GameObject instance = Instantiate(prefab, bossModelRoot);
        instance.name = partName;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = localRotation;
        instance.transform.localScale = localScale;

        Collider[] colliders = instance.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = CreateBossMaterial(color, emission);
        }

        return instance.transform;
    }

    private Material CreateBossMaterial(Color color, float emission)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * emission);
        return material;
    }

    private LineRenderer CreateLine(string lineName, Color color, float width)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = width;
        line.endWidth = width;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
        line.enabled = false;

        return line;
    }

    private Vector3 GetBeamOrigin()
    {
        return GetAttackOrigin();
    }

    private Vector3 GetAttackOrigin()
    {
        float height = importedBossVisuals ? 1.65f : 0.85f;
        float forwardOffset = importedBossVisuals ? 2.45f : 1.7f;
        return transform.position + Vector3.up * height + transform.forward * forwardOffset;
    }

    private Vector3 GetPlayerAimPoint()
    {
        if (player == null)
            return transform.position + transform.forward;

        return player.position + Vector3.up * 0.75f;
    }

    private Vector3 GetAimDirectionToPlayer(Vector3 origin)
    {
        if (player == null)
            return transform.forward;

        Vector3 direction = GetPlayerAimPoint() - origin;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = transform.forward;
        }

        return direction.normalized;
    }

    private float GetFlatDistanceToPlayer()
    {
        if (player == null)
            return float.MaxValue;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        return direction.magnitude;
    }

    private Vector3 GetBeamEnd(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, deathBeamRange, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<TempBossController>() == null)
            {
                return hit.point;
            }
        }

        return origin + direction * deathBeamRange;
    }

    private void SetLine(LineRenderer line, Vector3 start, Vector3 end)
    {
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    private void ConfigureWarningLine(Color color, float startWidth, float endWidth)
    {
        warningLine.startWidth = startWidth;
        warningLine.endWidth = endWidth;
        warningLine.startColor = color;
        warningLine.endColor = color;
    }

    private void ConfigureFireLine(Color startColor, Color endColor, float startWidth, float endWidth)
    {
        fireLine.startWidth = startWidth;
        fireLine.endWidth = endWidth;
        fireLine.startColor = startColor;
        fireLine.endColor = endColor;
    }

    public void AddMeleeDetectionContact(Collider contact)
    {
        if (state == BossState.Dead || meleeDetectionBox == null)
            return;

        meleeDetectionBox.AddContact(contact);
        playerInMeleeZone = meleeDetectionBox.HasContacts;
    }

    public void RemoveMeleeDetectionContact(Collider contact)
    {
        if (meleeDetectionBox == null)
            return;

        meleeDetectionBox.RemoveContact(contact);
        playerInMeleeZone = meleeDetectionBox.HasContacts;

        if (!playerInMeleeZone && state == BossState.Idle && player != null && !health.IsDead)
        {
            SetState(BossState.Chase);
        }
    }

    public void TryApplyMeleeHit(PlayerHealth target)
    {
        if (!meleeHitboxesActive || state != BossState.MeleeAttack || target == null || meleeHitPlayers.Contains(target))
            return;

        meleeHitPlayers.Add(target);
        target.TakeDamage(closeAttackDamage);
        GameVfx.SpawnEnemyDeathBurst(target.transform.position + Vector3.up * 0.75f);
    }

    private void ApplyMeleeBodyContactDamage()
    {
        if (!meleeHitboxesActive || state != BossState.MeleeAttack)
            return;

        GetBossCapsulePoints(out Vector3 point1, out Vector3 point2, out float radius);
        Collider[] hits = Physics.OverlapCapsule(
            point1,
            point2,
            radius + 0.12f,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerHealth hitPlayer = hits[i].GetComponentInParent<PlayerHealth>();
            if (hitPlayer != null)
            {
                TryApplyMeleeHit(hitPlayer);
            }
        }

        Vector3 meleeCenter = transform.TransformPoint(meleeDetectionOffset + new Vector3(0f, 0.05f, 0.35f));
        Vector3 meleeHalfExtents = Vector3.Scale(meleeDetectionSize, transform.lossyScale) * 0.5f;
        Collider[] boxHits = Physics.OverlapBox(
            meleeCenter,
            meleeHalfExtents,
            transform.rotation,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < boxHits.Length; i++)
        {
            PlayerHealth hitPlayer = boxHits[i].GetComponentInParent<PlayerHealth>();
            if (hitPlayer != null)
            {
                TryApplyMeleeHit(hitPlayer);
            }
        }
    }

    private void EnableMeleeHitboxes(bool enabled)
    {
        meleeHitboxesActive = enabled;

        if (enabled)
        {
            meleeHitPlayers.Clear();
        }

        if (meleeHitboxes == null)
            return;

        for (int i = 0; i < meleeHitboxes.Length; i++)
        {
            meleeHitboxes[i].SetEnabled(enabled);
        }
    }

    private bool IsPlayerInsideFlameCone(Vector3 origin, Vector3 direction)
    {
        if (playerHealth == null || playerHealth.IsDead)
            return false;

        Vector3 target = GetPlayerAimPoint();
        Vector3 toPlayer = target - origin;
        float distance = toPlayer.magnitude;

        if (distance > flameThrowerRange)
            return false;

        Vector3 toPlayerDirection = toPlayer.normalized;
        if (Vector3.Angle(direction, toPlayerDirection) > flameThrowerAngle * 0.5f)
            return false;

        if (Physics.Raycast(origin, toPlayerDirection, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<PlayerHealth>() == null && !ShouldBeamIgnoreCollider(hit.collider))
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyFlameThrowerKill(PlayerHealth target)
    {
        if (target == null || target.IsDead)
            return;

        float lethalDamage = target.CurrentHealth + target.FlatDamageReduction + 1f;
        target.TakeDamage(lethalDamage);
    }

    private void ResetBossAttackLines()
    {
        if (warningLine != null)
        {
            warningLine.startWidth = 0.08f;
            warningLine.endWidth = 0.08f;
            warningLine.startColor = new Color(1f, 0.05f, 0.05f, 0.75f);
            warningLine.endColor = new Color(1f, 0.05f, 0.05f, 0.75f);
        }

        if (fireLine != null)
        {
            fireLine.startWidth = deathBeamRadius * 2f;
            fireLine.endWidth = deathBeamRadius * 2f;
            fireLine.startColor = new Color(1f, 0.1f, 0.02f, 1f);
            fireLine.endColor = new Color(1f, 0.1f, 0.02f, 1f);
        }
    }

    private void ApplyBeamDamage(Vector3 origin, Vector3 direction)
    {
        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            deathBeamRadius,
            direction,
            deathBeamRange,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (hit.collider.GetComponentInParent<TempBossController>() != null)
                continue;

            PlayerHealth hitPlayer = hit.collider.GetComponentInParent<PlayerHealth>();

            if (hitPlayer != null)
            {
                hitPlayer.TakeDamage(deathBeamDamage);
                return;
            }

            if (!ShouldBeamIgnoreCollider(hit.collider))
            {
                return;
            }
        }
    }

    private bool ShouldBeamIgnoreCollider(Collider collider)
    {
        if (collider.isTrigger)
            return true;

        if (collider.CompareTag("Ground"))
            return true;

        string objectName = collider.name;
        return objectName.Contains("Ground") || objectName.Contains("Floor") || objectName.Contains("Projectile");
    }

    private void HandleDied(EnemyHealth enemy)
    {
        if (state == BossState.Dead)
            return;

        SetState(BossState.Dead);
        StopAllCoroutines();
        CleanupAttackState();

        if (meleeDetectionBox != null)
        {
            meleeDetectionBox.SetEnabled(false);
        }

        PlayBossAnimation("Death1", GetAnimationLength("Death1"), true);
        GameVfx.SpawnLevelUp(transform.position);
        GameVfx.SpawnEnemyDeathBurst(transform.position + Vector3.up * 1.5f);
        GameVfx.SpawnEnemyDeathBurst(transform.position + transform.right * 1.8f + Vector3.up);
        GameVfx.SpawnEnemyDeathBurst(transform.position - transform.right * 1.8f + Vector3.up);

        Defeated?.Invoke();

        StargraveRuntimeUI ui = FindFirstObjectByType<StargraveRuntimeUI>();
        if (ui != null)
        {
            ui.ShowClear();
        }
    }

    private void HandleDamaged(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (state == BossState.Dead || health == null || health.IsDead || Time.time < nextHitReactionTime)
            return;

        nextHitReactionTime = Time.time + 0.45f;
        GameVfx.SpawnHitSpark(hitPoint, hitDirection, true);
    }

    private void PlayBossAnimation(string stateName, float lockDuration = 0f, bool force = false)
    {
        if (bossAnimator == null)
            return;

        if (state == BossState.Dead && !force)
            return;

        if (!HasAnimatorState(stateName))
            return;

        if (!force && lockDuration <= 0f && (state == BossState.MeleeAttack || state == BossState.LaserAttack || state == BossState.FlameAttack))
            return;

        AnimatorStateInfo currentState = bossAnimator.GetCurrentAnimatorStateInfo(0);
        if (currentState.IsName(stateName) && currentBossAnimationName == stateName && lockDuration <= 0f)
            return;

        float transitionDuration = lockDuration > 0f ? 0.14f : 0.22f;
        bossAnimator.CrossFadeInFixedTime(stateName, transitionDuration, 0);
        currentBossAnimationName = stateName;
    }

    private bool HasAnimatorState(string stateName)
    {
        if (bossAnimator == null)
            return false;

        int stateHash = Animator.StringToHash(stateName);
        if (bossAnimator.HasState(0, stateHash))
            return true;

        if (!missingAnimatorStateLogs.Contains(stateName))
        {
            missingAnimatorStateLogs.Add(stateName);
            Debug.LogError($"[Boss] Animator state '{stateName}' does not exist on layer 0. Animation was not played.");
        }

        return false;
    }

    private float GetAnimationLength(string stateName)
    {
        if (bossAnimator == null || bossAnimator.runtimeAnimatorController == null)
            return fallbackBossDeathDestroyDelay;

        AnimationClip[] clips = bossAnimator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name == stateName)
            {
                return clips[i].length;
            }
        }

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name.Contains(stateName))
            {
                return clips[i].length;
            }
        }

        return fallbackBossDeathDestroyDelay;
    }

    private void UpdateDeathDestroyDelay()
    {
        if (health == null)
            return;

        health.SetDestroyDelay(GetAnimationLength("Death1") + 0.25f);
    }

    private void FixImportedBossMaterials(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] sourceMaterials = renderers[i].sharedMaterials;
            Material[] runtimeMaterials = new Material[sourceMaterials.Length];

            for (int j = 0; j < sourceMaterials.Length; j++)
            {
                runtimeMaterials[j] = CreateImportedBossMaterial(sourceMaterials[j]);
            }

            renderers[i].materials = runtimeMaterials;
        }
    }

    private Material CreateImportedBossMaterial(Material source)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);

        Texture baseTexture = GetMaterialTexture(source, "_BaseMap");
        if (baseTexture == null)
        {
            baseTexture = GetMaterialTexture(source, "_MainTex");
        }

        Texture normalTexture = GetMaterialTexture(source, "_BumpMap");
        Texture emissionTexture = GetMaterialTexture(source, "_EmissionMap");
        Color baseColor = GetMaterialColor(source, "_Color", Color.white);

        SetMaterialTexture(material, "_BaseMap", "_MainTex", baseTexture);
        SetMaterialTexture(material, "_BumpMap", "_BumpMap", normalTexture);
        SetMaterialTexture(material, "_EmissionMap", "_EmissionMap", emissionTexture);
        SetMaterialColor(material, "_BaseColor", "_Color", baseColor);

        if (normalTexture != null)
        {
            material.EnableKeyword("_NORMALMAP");
        }

        if (emissionTexture != null || material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            SetMaterialColor(material, "_EmissionColor", "_EmissionColor", new Color(0.08f, 0.55f, 0.8f));
        }

        SetMaterialFloat(material, "_Metallic", 0.4f);
        SetMaterialFloat(material, "_Smoothness", 0.62f);
        SetMaterialFloat(material, "_Glossiness", 0.62f);
        return material;
    }

    private Texture GetMaterialTexture(Material material, string property)
    {
        return material != null && material.HasProperty(property) ? material.GetTexture(property) : null;
    }

    private Color GetMaterialColor(Material material, string property, Color fallback)
    {
        return material != null && material.HasProperty(property) ? material.GetColor(property) : fallback;
    }

    private void SetMaterialTexture(Material material, string primaryProperty, string fallbackProperty, Texture texture)
    {
        if (texture == null)
            return;

        if (material.HasProperty(primaryProperty))
        {
            material.SetTexture(primaryProperty, texture);
        }
        else if (material.HasProperty(fallbackProperty))
        {
            material.SetTexture(fallbackProperty, texture);
        }
    }

    private void SetMaterialColor(Material material, string primaryProperty, string fallbackProperty, Color color)
    {
        if (material.HasProperty(primaryProperty))
        {
            material.SetColor(primaryProperty, color);
        }
        else if (material.HasProperty(fallbackProperty))
        {
            material.SetColor(fallbackProperty, color);
        }
    }

    private void SetMaterialFloat(Material material, string property, float value)
    {
        if (material.HasProperty(property))
        {
            material.SetFloat(property, value);
        }
    }
}

public class BossMeleeDetectionBox : MonoBehaviour
{
    private TempBossController boss;
    private BoxCollider detectionCollider;
    private readonly HashSet<Collider> contacts = new HashSet<Collider>();

    public bool HasContacts => contacts.Count > 0;

    public void Init(TempBossController owner)
    {
        boss = owner;
        detectionCollider = GetComponent<BoxCollider>();
    }

    public void AddContact(Collider contact)
    {
        if (contact != null)
        {
            contacts.Add(contact);
        }
    }

    public void RemoveContact(Collider contact)
    {
        if (contact != null)
        {
            contacts.Remove(contact);
        }
    }

    public void SetEnabled(bool enabled)
    {
        contacts.Clear();

        if (detectionCollider != null)
        {
            detectionCollider.enabled = enabled;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null)
        {
            boss.AddMeleeDetectionContact(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null)
        {
            boss.RemoveMeleeDetectionContact(other);
        }
    }
}

public class BossMeleeHitbox : MonoBehaviour
{
    private TempBossController boss;
    private BoxCollider hitboxCollider;

    public void Init(TempBossController owner, BoxCollider collider)
    {
        boss = owner;
        hitboxCollider = collider;
    }

    public void SetEnabled(bool enabled)
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = enabled;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }

    private void TryHit(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            boss.TryApplyMeleeHit(playerHealth);
        }
    }
}
