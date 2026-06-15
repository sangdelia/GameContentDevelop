using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class TempBossController : MonoBehaviour
{
    private const string ToiletMechBossResourcePath = "Models/Boss/ToiletMech_Boss";

    [SerializeField] private float moveSpeed = 3.2f;
    [SerializeField] private float closeAttackDistance = 4f;
    [SerializeField] private float closeAttackDamage = 18f;
    [SerializeField] private float touchDamage = 8f;
    [SerializeField] private float touchDamageInterval = 0.8f;
    [SerializeField] private float projectileDamage = 12f;
    [SerializeField] private float deathBeamDamage = 55f;
    [SerializeField] private float deathBeamRange = 45f;
    [SerializeField] private float deathBeamRadius = 0.55f;
    [SerializeField] private float deathBeamWarningTime = 1.4f;
    [SerializeField] private float deathBeamVisibleTime = 0.18f;
    [SerializeField] private float patternInterval = 3.2f;
    [SerializeField] private float fightStartGraceTime = 3f;
    [SerializeField] private float meleeImpactDelay = 0.32f;
    [SerializeField] private float projectileFireDelay = 0.34f;
    [SerializeField] private float bossDeathDestroyDelay = 2.2f;
    [SerializeField] private Vector3 toiletMechLocalPosition = new Vector3(0f, -1.05f, 0f);
    [SerializeField] private Vector3 toiletMechLocalRotation = new Vector3(0f, 180f, 0f);
    [SerializeField] private Vector3 toiletMechLocalScale = Vector3.one * 1.28f;

    private Transform player;
    private PlayerHealth playerHealth;
    private EnemyHealth health;
    private float patternTimer;
    private float touchDamageTimer;
    private int patternIndex;
    private float bossStartTime;
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
    private Animator bossAnimator;
    private bool importedBossVisuals;
    private float animationLockUntil;
    private float nextHitReactionTime;
    private Color normalColor = new Color(0.55f, 0.08f, 0.9f);
    private Color warningColor = new Color(1f, 0.05f, 0.05f);

    public event System.Action Defeated;

    public static TempBossController Create(Vector3 position, Transform target, float hp)
    {
        GameObject bossObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bossObject.name = "Temp_Boss_Stargrave_Core";
        bossObject.transform.position = position + Vector3.up * 3f;
        bossObject.transform.localScale = new Vector3(3f, 3f, 3f);

        EnemyHealth health = bossObject.AddComponent<EnemyHealth>();
        health.Configure(hp, null, 0);
        health.SetDestroyDelay(2.2f);

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
        health.SetDestroyDelay(bossDeathDestroyDelay);

        bodyRenderer = GetComponent<Renderer>();
        bodyRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bodyRenderer.material.color = normalColor;
        bodyRenderer.material.EnableKeyword("_EMISSION");
        bodyRenderer.material.SetColor("_EmissionColor", normalColor * 0.8f);
        bodyRenderer.enabled = false;
        bossStartTime = Time.time;
        BuildBossVisuals();

        warningLine = CreateLine("DeathBeamWarningLine", new Color(1f, 0.05f, 0.05f, 0.75f), 0.08f);
        fireLine = CreateLine("DeathBeamFireLine", new Color(1f, 0.1f, 0.02f, 1f), deathBeamRadius * 2f);
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
        if (player == null || health.IsDead)
            return;

        FacePlayer();
        MoveTowardPlayer();
        TryTouchDamage();
        AnimateBossVisuals();

        if (Time.time - bossStartTime < fightStartGraceTime)
            return;

        patternTimer += Time.deltaTime;

        if (patternTimer >= patternInterval)
        {
            patternTimer = 0f;
            RunNextPattern();
        }
    }

    private void MoveTowardPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= closeAttackDistance)
        {
            PlayBossAnimation("Idle1_Toilet");
            return;
        }

        transform.position += direction.normalized * moveSpeed * Time.deltaTime;
        PlayBossAnimation("Walk_F");
    }

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void RunNextPattern()
    {
        patternIndex = (patternIndex + 1) % 3;

        if (patternIndex == 0)
        {
            CloseShock();
        }
        else if (patternIndex == 1)
        {
            FireProjectileBurst();
        }
        else
        {
            DeathBeamWarning();
        }
    }

    private void CloseShock()
    {
        if (playerHealth == null)
            return;

        float distance = GetFlatDistanceToPlayer();

        if (distance <= closeAttackDistance + 1.5f)
        {
            PlayBossAnimation("Attack1", 0.9f);
            StartCoroutine(CloseShockRoutine());
        }
    }

    private System.Collections.IEnumerator CloseShockRoutine()
    {
        yield return new WaitForSeconds(meleeImpactDelay);

        if (health == null || health.IsDead || playerHealth == null)
            yield break;

        if (GetFlatDistanceToPlayer() <= closeAttackDistance + 1.8f)
        {
            playerHealth.TakeDamage(closeAttackDamage);
            GameVfx.SpawnEnemyDeathBurst(transform.position + transform.forward * 1.4f + Vector3.up * 0.4f);
        }
    }

    private void TryTouchDamage()
    {
        if (playerHealth == null)
            return;

        float distance = GetFlatDistanceToPlayer();

        if (distance > closeAttackDistance)
            return;

        touchDamageTimer -= Time.deltaTime;

        if (touchDamageTimer > 0f)
            return;

        touchDamageTimer = touchDamageInterval;
        PlayBossAnimation("Attack2", 0.45f);
        playerHealth.TakeDamage(touchDamage);
    }

    private void FireProjectileBurst()
    {
        PlayBossAnimation("Attack6_Shoot", 0.95f);
        StartCoroutine(FireProjectileBurstRoutine());
    }

    private System.Collections.IEnumerator FireProjectileBurstRoutine()
    {
        yield return new WaitForSeconds(projectileFireDelay);

        if (health == null || health.IsDead)
            yield break;

        Vector3 origin = GetAttackOrigin();
        Vector3 baseDirection = GetAimDirectionToPlayer(origin);

        for (int i = -1; i <= 1; i++)
        {
            Quaternion spread = Quaternion.Euler(0f, i * 12f, 0f);
            Vector3 direction = spread * baseDirection;
            BossProjectile.Create(origin + direction * 2.2f, direction, projectileDamage);
        }
    }

    private void DeathBeamWarning()
    {
        StartCoroutine(DeathBeamRoutine());
    }

    private System.Collections.IEnumerator DeathBeamRoutine()
    {
        bodyRenderer.material.color = warningColor;
        bodyRenderer.material.SetColor("_EmissionColor", warningColor * 2.2f);
        PlayBossAnimation("Roar", deathBeamWarningTime);

        Vector3 origin = GetBeamOrigin();
        Vector3 direction = GetAimDirectionToPlayer(origin);
        Vector3 end = GetBeamEnd(origin, direction);

        GameAudio.PlayBossWarning(origin);
        warningLine.enabled = true;

        float timer = 0f;
        while (timer < deathBeamWarningTime)
        {
            origin = GetBeamOrigin();
            end = GetBeamEnd(origin, direction);
            SetLine(warningLine, origin, end);

            timer += Time.deltaTime;
            yield return null;
        }

        warningLine.enabled = false;
        PlayBossAnimation("Attack3", deathBeamVisibleTime + 0.45f);

        origin = GetBeamOrigin();
        end = GetBeamEnd(origin, direction);
        SetLine(fireLine, origin, end);
        fireLine.enabled = true;
        GameVfx.SpawnLaserImpact(end, direction, new Color(1f, 0.12f, 0.03f));

        GameAudio.PlayBossLaser(origin);
        ApplyBeamDamage(origin, direction);

        yield return new WaitForSeconds(deathBeamVisibleTime);

        fireLine.enabled = false;

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
            return false;

        GameObject instance = Instantiate(prefab, bossModelRoot);
        instance.name = "Boss_ToiletMech_Model";
        instance.transform.localPosition = toiletMechLocalPosition;
        instance.transform.localRotation = Quaternion.Euler(toiletMechLocalRotation);
        instance.transform.localScale = toiletMechLocalScale;
        bossAnimator = instance.GetComponentInChildren<Animator>();

        if (bossAnimator != null)
        {
            bossAnimator.applyRootMotion = false;
            bossAnimator.speed = 1f;
            bossAnimator.Play("Idle1_Toilet", 0, 0f);
        }

        Collider[] colliders = instance.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].material;
            if (material != null && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.1f, 0.75f, 1f) * 0.55f);
            }
        }

        return true;
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

    private Vector3 GetFlatDirectionToPlayer()
    {
        if (player == null)
            return transform.forward;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return transform.forward;

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
        warningLine.enabled = false;
        fireLine.enabled = false;
        PlayBossAnimation("Death1", bossDeathDestroyDelay);
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
        if (health == null || health.IsDead || Time.time < nextHitReactionTime)
            return;

        nextHitReactionTime = Time.time + 0.45f;
        PlayBossAnimation("GetHit_F", 0.28f);
    }

    private void PlayBossAnimation(string stateName, float lockDuration = 0f)
    {
        if (bossAnimator == null)
            return;

        if (Time.time < animationLockUntil && lockDuration <= 0f)
            return;

        AnimatorStateInfo currentState = bossAnimator.GetCurrentAnimatorStateInfo(0);

        if (currentState.IsName(stateName) && lockDuration <= 0f)
            return;

        bossAnimator.CrossFadeInFixedTime(stateName, 0.12f, 0);

        if (lockDuration > 0f)
        {
            animationLockUntil = Time.time + lockDuration;
        }
    }
}
