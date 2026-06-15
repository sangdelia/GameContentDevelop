using System.Collections.Generic;
using UnityEngine;

public enum StatusEffectType
{
    Burn,
    Slow,
    Shock,
    Freeze,
    KnockbackImmune
}

public enum StackingRule
{
    RefreshDuration,
    AddStack,
    ReplaceIfStronger,
    IgnoreIfActive
}

public class StatusEffectController : MonoBehaviour
{
    private class ActiveEffect
    {
        public string EffectId;
        public StatusEffectType Type;
        public float Remaining;
        public float TickInterval;
        public float TickTimer;
        public float Value;
        public int Stacks;
        public int MaxStacks;
        public GameObject Source;
        public Vector3 LastHitPoint;
        public Vector3 LastHitDirection;
    }

    private readonly Dictionary<string, ActiveEffect> effects = new Dictionary<string, ActiveEffect>();
    private readonly List<string> effectKeys = new List<string>();
    private readonly List<string> expiredKeys = new List<string>();
    private EnemyHealth health;
    private float moveSpeedMultiplier = 1f;
    private ParticleSystem burnParticles;
    private Transform burnHalo;
    private ParticleSystem slowParticles;
    private Transform slowHalo;

    public float MoveSpeedMultiplier => moveSpeedMultiplier;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
    }

    private void OnDisable()
    {
        effects.Clear();
        moveSpeedMultiplier = 1f;
        SetBurnVisualActive(false);
        SetSlowVisualActive(false);
    }

    private void Update()
    {
        if (effects.Count == 0)
            return;

        effectKeys.Clear();
        foreach (string key in effects.Keys)
        {
            effectKeys.Add(key);
        }

        expiredKeys.Clear();

        for (int i = 0; i < effectKeys.Count; i++)
        {
            if (!effects.TryGetValue(effectKeys[i], out ActiveEffect effect))
                continue;

            effect.Remaining -= Time.deltaTime;

            if (effect.Type == StatusEffectType.Burn)
            {
                TickBurn(effect);
            }

            if (effect.Remaining <= 0f)
            {
                expiredKeys.Add(effectKeys[i]);
            }
        }

        if (expiredKeys.Count > 0)
        {
            for (int i = 0; i < expiredKeys.Count; i++)
            {
                effects.Remove(expiredKeys[i]);
            }

            RecalculateMoveSpeedMultiplier();
        }

        UpdateStatusVisualState();
    }

    public void ApplyBurn(string effectId, GameObject source, float duration, float tickInterval, float damagePerTick, int maxStacks, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (health == null || health.IsDead)
            return;

        if (!effects.TryGetValue(effectId, out ActiveEffect effect))
        {
            effect = new ActiveEffect
            {
                EffectId = effectId,
                Type = StatusEffectType.Burn,
                TickInterval = Mathf.Max(0.05f, tickInterval),
                TickTimer = Mathf.Max(0.05f, tickInterval),
                Stacks = 0
            };
            effects.Add(effectId, effect);
        }

        effect.Source = source;
        effect.Remaining = Mathf.Max(effect.Remaining, duration);
        effect.Value = Mathf.Max(0f, damagePerTick);
        effect.MaxStacks = Mathf.Max(1, maxStacks);
        effect.Stacks = Mathf.Clamp(effect.Stacks + 1, 1, effect.MaxStacks);
        effect.LastHitPoint = hitPoint;
        effect.LastHitDirection = hitDirection;
        SetBurnVisualActive(true);
    }

    public void ApplySlow(string effectId, float duration, float speedMultiplier)
    {
        if (health == null || health.IsDead)
            return;

        if (!effects.TryGetValue(effectId, out ActiveEffect effect))
        {
            effect = new ActiveEffect
            {
                EffectId = effectId,
                Type = StatusEffectType.Slow,
                Stacks = 1,
                MaxStacks = 1
            };
            effects.Add(effectId, effect);
        }

        effect.Remaining = Mathf.Max(effect.Remaining, duration);
        effect.Value = Mathf.Clamp(speedMultiplier, 0.05f, 1f);
        RecalculateMoveSpeedMultiplier();
        SetSlowVisualActive(true);
    }

    public bool HasEffect(StatusEffectType type)
    {
        effectKeys.Clear();
        foreach (string key in effects.Keys)
        {
            effectKeys.Add(key);
        }

        for (int i = 0; i < effectKeys.Count; i++)
        {
            if (!effects.TryGetValue(effectKeys[i], out ActiveEffect effect))
                continue;

            if (effect.Type == type)
                return true;
        }

        return false;
    }

    private void TickBurn(ActiveEffect effect)
    {
        if (health == null || health.IsDead)
            return;

        effect.TickTimer -= Time.deltaTime;

        if (effect.TickTimer > 0f)
            return;

        effect.TickTimer = effect.TickInterval;
        float damage = effect.Value * Mathf.Max(1, effect.Stacks);
        DamageInfo info = new DamageInfo(damage, effect.Source, DamageType.Status, effect.LastHitPoint, effect.LastHitDirection);
        health.TakeDamage(info);
    }

    private void RecalculateMoveSpeedMultiplier()
    {
        float multiplier = 1f;

        effectKeys.Clear();
        foreach (string key in effects.Keys)
        {
            effectKeys.Add(key);
        }

        for (int i = 0; i < effectKeys.Count; i++)
        {
            if (!effects.TryGetValue(effectKeys[i], out ActiveEffect effect))
                continue;

            if (effect.Type == StatusEffectType.Slow)
            {
                multiplier = Mathf.Min(multiplier, effect.Value);
            }
        }

        moveSpeedMultiplier = multiplier;
    }

    private void UpdateStatusVisualState()
    {
        SetBurnVisualActive(HasEffect(StatusEffectType.Burn));
        SetSlowVisualActive(HasEffect(StatusEffectType.Slow));
    }

    private void SetBurnVisualActive(bool active)
    {
        if (active)
        {
            EnsureBurnVisual();
            if (burnParticles != null && !burnParticles.isPlaying)
            {
                burnParticles.Play();
            }

            if (burnHalo != null)
            {
                burnHalo.gameObject.SetActive(true);
                burnHalo.localRotation = Quaternion.Euler(0f, Time.time * 180f, 0f);
            }

            return;
        }

        if (burnParticles != null && burnParticles.isPlaying)
        {
            burnParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (burnHalo != null)
        {
            burnHalo.gameObject.SetActive(false);
        }
    }

    private void EnsureBurnVisual()
    {
        if (burnParticles == null)
        {
            GameObject particleObject = new GameObject("BurnStatusFlames");
            particleObject.transform.SetParent(transform, false);
            particleObject.transform.localPosition = Vector3.up * 1.05f;

            burnParticles = particleObject.AddComponent<ParticleSystem>();
            burnParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = burnParticles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 0.8f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.34f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.05f, 0.01f, 0.95f), new Color(1f, 0.45f, 0.02f, 0.82f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = burnParticles.emission;
            emission.rateOverTime = 24f;

            ParticleSystem.ShapeModule shape = burnParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.42f;
            shape.angle = 16f;

            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateBurnMaterial(new Color(1f, 0.12f, 0.02f), 2.4f);
        }

        if (burnHalo == null)
        {
            GameObject haloObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            haloObject.name = "BurnStatusHalo";
            haloObject.transform.SetParent(transform, false);
            haloObject.transform.localPosition = Vector3.up * 0.1f;
            haloObject.transform.localScale = new Vector3(1.45f, 0.035f, 1.45f);

            Collider collider = haloObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = haloObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateBurnMaterial(new Color(1f, 0.05f, 0.01f, 0.8f), 2.1f);
            }

            burnHalo = haloObject.transform;
        }
    }

    private void SetSlowVisualActive(bool active)
    {
        if (active)
        {
            EnsureSlowVisual();
            if (slowParticles != null && !slowParticles.isPlaying)
            {
                slowParticles.Play();
            }

            if (slowHalo != null)
            {
                slowHalo.gameObject.SetActive(true);
                slowHalo.localRotation = Quaternion.Euler(0f, -Time.time * 130f, 0f);
            }

            return;
        }

        if (slowParticles != null && slowParticles.isPlaying)
        {
            slowParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (slowHalo != null)
        {
            slowHalo.gameObject.SetActive(false);
        }
    }

    private void EnsureSlowVisual()
    {
        if (slowParticles == null)
        {
            GameObject particleObject = new GameObject("SlowStatusFrost");
            particleObject.transform.SetParent(transform, false);
            particleObject.transform.localPosition = Vector3.up * 1.15f;

            slowParticles = particleObject.AddComponent<ParticleSystem>();
            slowParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = slowParticles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 0.9f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.9f, 1f, 0.9f), new Color(0.1f, 0.55f, 1f, 0.75f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = slowParticles.emission;
            emission.rateOverTime = 18f;

            ParticleSystem.ShapeModule shape = slowParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.52f;

            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateStatusMaterial(new Color(0.25f, 0.85f, 1f), 2.1f);
        }

        if (slowHalo == null)
        {
            GameObject haloObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            haloObject.name = "SlowStatusHalo";
            haloObject.transform.SetParent(transform, false);
            haloObject.transform.localPosition = Vector3.up * 0.16f;
            haloObject.transform.localScale = new Vector3(1.62f, 0.028f, 1.62f);

            Collider collider = haloObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = haloObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateStatusMaterial(new Color(0.15f, 0.72f, 1f, 0.78f), 2f);
            }

            slowHalo = haloObject.transform;
        }
    }

    private Material CreateBurnMaterial(Color color, float emission)
    {
        return CreateStatusMaterial(color, emission);
    }

    private Material CreateStatusMaterial(Color color, float emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }

        return material;
    }
}
