using UnityEngine;

public static class GameVfx
{
    public static void SpawnMuzzleFlash(Vector3 position, Vector3 direction)
    {
        SpawnMuzzleFlash(position, direction, new Color(1f, 0.72f, 0.18f));
    }

    public static void SpawnMuzzleFlash(Vector3 position, Vector3 direction, Color color)
    {
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "VFX_MuzzleFlash";
        flash.transform.position = position + direction.normalized * 0.08f;
        flash.transform.localScale = Vector3.one * 0.22f;

        RemoveCollider(flash);
        ApplyMaterial(flash, color, 3f);
        Object.Destroy(flash, 0.045f);

        SpawnBurst(position, color, 12, 0.08f, 0.18f, 0.18f);
    }

    public static void SpawnHitSpark(Vector3 position, Vector3 normal, bool hitEnemy)
    {
        Color color = hitEnemy ? new Color(1f, 0.18f, 0.12f) : new Color(0.65f, 0.85f, 1f);
        SpawnBurst(position + normal.normalized * 0.04f, color, hitEnemy ? 18 : 10, 0.05f, 0.16f, 0.28f);
    }

    public static void SpawnEnemyDeathBurst(Vector3 position)
    {
        SpawnBurst(position + Vector3.up * 0.8f, new Color(0.95f, 0.1f, 1f), 34, 0.12f, 0.3f, 0.6f);
    }

    public static void SpawnExpCollect(Vector3 position)
    {
        SpawnBurst(position, new Color(0.18f, 0.85f, 1f), 10, 0.04f, 0.12f, 0.25f);
    }

    public static void SpawnLevelUp(Vector3 position)
    {
        SpawnBurst(position + Vector3.up * 1.2f, new Color(0.2f, 1f, 0.55f), 42, 0.08f, 0.45f, 0.75f);
    }

    public static void SpawnShieldBlock(Vector3 position)
    {
        GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shield.name = "VFX_ShieldBlock";
        shield.transform.position = position + Vector3.up * 0.85f;
        shield.transform.localScale = Vector3.one * 1.8f;

        RemoveCollider(shield);
        ApplyMaterial(shield, new Color(0.1f, 0.75f, 1f, 0.35f), 1.6f);
        Object.Destroy(shield, 0.18f);

        SpawnBurst(shield.transform.position, new Color(0.25f, 0.9f, 1f), 24, 0.08f, 0.28f, 0.32f);
    }

    private static void SpawnBurst(Vector3 position, Color color, int count, float startSize, float speed, float lifetime)
    {
        GameObject burstObject = new GameObject("VFX_Burst");
        burstObject.transform.position = position;

        ParticleSystem particles = burstObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.duration = 0.04f;
        main.loop = false;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = startSize;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        ParticleSystemRenderer renderer = burstObject.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateMaterial(color, 1.8f);

        particles.Emit(count);
        particles.Play();
        Object.Destroy(burstObject, lifetime + 0.15f);
    }

    private static void RemoveCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();

        if (collider != null)
        {
            Object.Destroy(collider);
        }
    }

    private static void ApplyMaterial(GameObject target, Color color, float emission)
    {
        Renderer renderer = target.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material = CreateMaterial(color, emission);
        }
    }

    private static Material CreateMaterial(Color color, float emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader);
        material.color = color;

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }

        return material;
    }
}
