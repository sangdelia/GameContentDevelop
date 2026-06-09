using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class TempBossController : MonoBehaviour
{
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

        bodyRenderer = GetComponent<Renderer>();
        bodyRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bodyRenderer.material.color = normalColor;
        bodyRenderer.material.EnableKeyword("_EMISSION");
        bodyRenderer.material.SetColor("_EmissionColor", normalColor * 0.8f);
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
            return;

        transform.position += direction.normalized * moveSpeed * Time.deltaTime;
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
            playerHealth.TakeDamage(closeAttackDamage);
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
        playerHealth.TakeDamage(touchDamage);
    }

    private void FireProjectileBurst()
    {
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

        origin = GetBeamOrigin();
        end = GetBeamEnd(origin, direction);
        SetLine(fireLine, origin, end);
        fireLine.enabled = true;

        GameAudio.PlayBossLaser(origin);
        ApplyBeamDamage(origin, direction);

        yield return new WaitForSeconds(deathBeamVisibleTime);

        fireLine.enabled = false;

        bodyRenderer.material.color = normalColor;
        bodyRenderer.material.SetColor("_EmissionColor", normalColor * 0.8f);
    }

    private void BuildBossVisuals()
    {
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
    }

    private void ApplyBossMaterial(GameObject target, Color color, float emission)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = color;
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", color * emission);
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
        return new Vector3(transform.position.x, 1.35f, transform.position.z);
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
        Defeated?.Invoke();

        StargraveRuntimeUI ui = FindFirstObjectByType<StargraveRuntimeUI>();
        if (ui != null)
        {
            ui.ShowClear();
        }
    }
}
