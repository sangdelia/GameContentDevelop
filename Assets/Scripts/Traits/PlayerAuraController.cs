using System.Collections.Generic;
using UnityEngine;

public class PlayerAuraController : MonoBehaviour
{
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private int maxTargets = 128;
    [SerializeField] private float ringHeight = 0.03f;

    private readonly HashSet<EnemyHealth> scanSet = new HashSet<EnemyHealth>();
    private Collider[] hits;
    private LineRenderer ring;
    private bool slowAuraActive;
    private float radius;
    private float normalSlowMultiplier = 0.75f;
    private float bossSlowMultiplier = 0.9f;
    private float refreshInterval = 0.2f;
    private float refreshTimer;

    private void Awake()
    {
        hits = new Collider[Mathf.Max(16, maxTargets)];
        CreateRing();
    }

    private void Update()
    {
        if (!slowAuraActive)
            return;

        refreshTimer -= Time.deltaTime;
        UpdateRing();

        if (refreshTimer > 0f)
            return;

        refreshTimer = refreshInterval;
        ApplySlowAura();
    }

    public void ConfigureSlowAura(float newRadius, float normalSlow, float bossSlow, float interval)
    {
        radius = Mathf.Max(0.5f, newRadius);
        normalSlowMultiplier = Mathf.Clamp01(1f - Mathf.Clamp01(normalSlow));
        bossSlowMultiplier = Mathf.Clamp01(1f - Mathf.Clamp01(bossSlow));
        refreshInterval = Mathf.Max(0.05f, interval);
        refreshTimer = 0f;
        slowAuraActive = true;

        if (ring != null)
        {
            ring.enabled = true;
            BuildRingPoints();
        }
    }

    private void ApplySlowAura()
    {
        scanSet.Clear();
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, hits, targetMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            if (hits[i] == null)
                continue;

            EnemyHealth enemy = hits[i].GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !scanSet.Add(enemy))
                continue;

            StatusEffectController status = enemy.GetComponent<StatusEffectController>();
            if (status == null)
            {
                status = enemy.gameObject.AddComponent<StatusEffectController>();
            }

            bool isBoss = enemy.GetComponent<TempBossController>() != null;
            float multiplier = isBoss ? bossSlowMultiplier : normalSlowMultiplier;
            status.ApplySlow("trait_slow_aura", refreshInterval + 0.25f, multiplier);
        }
    }

    private void CreateRing()
    {
        GameObject ringObject = new GameObject("SlowAuraRing");
        ringObject.transform.SetParent(transform, false);
        ring = ringObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.positionCount = 64;
        ring.startWidth = 0.035f;
        ring.endWidth = 0.035f;
        ring.material = new Material(Shader.Find("Sprites/Default"));
        ring.startColor = new Color(0.2f, 0.8f, 1f, 0.45f);
        ring.endColor = new Color(0.2f, 0.8f, 1f, 0.45f);
        ring.enabled = false;
    }

    private void BuildRingPoints()
    {
        if (ring == null)
            return;

        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = (i / (float)ring.positionCount) * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, ringHeight, Mathf.Sin(angle) * radius);
            ring.SetPosition(i, transform.position + offset);
        }
    }

    private void UpdateRing()
    {
        BuildRingPoints();
    }
}
