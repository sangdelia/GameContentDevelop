using UnityEngine;

public static class GameVfx
{
    private const string ParticleTextureRoot = "Textures/KenneyParticles/";

    public static void SpawnMuzzleFlash(Vector3 position, Vector3 direction)
    {
        SpawnMuzzleFlash(position, direction, new Color(1f, 0.72f, 0.18f));
    }

    public static void SpawnMuzzleFlash(Vector3 position, Vector3 direction, Color color)
    {
        Vector3 forward = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        CreateTexturedQuad("VFX_MuzzleFlash", null, position + forward * 0.1f, Quaternion.LookRotation(forward), Vector3.one * 0.72f, "muzzle_03", color, 0.055f);
        CreateTexturedQuad("VFX_MuzzleCore", null, position + forward * 0.13f, Quaternion.LookRotation(forward), Vector3.one * 0.46f, "light_02", Color.white, 0.04f);

        SpawnBurst(position, color, 12, 0.08f, 0.18f, 0.18f);
    }

    public static void SpawnHitSpark(Vector3 position, Vector3 normal, bool hitEnemy)
    {
        Color color = hitEnemy ? new Color(1f, 0.18f, 0.12f) : new Color(0.65f, 0.85f, 1f);
        Vector3 facing = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;
        SpawnBurst(position + facing * 0.04f, color, hitEnemy ? 18 : 10, 0.05f, 0.16f, 0.28f);
        CreateTexturedQuad("VFX_ImpactSpark", null, position + facing * 0.06f, Quaternion.LookRotation(facing), Vector3.one * (hitEnemy ? 0.9f : 0.62f), hitEnemy ? "spark_06" : "spark_05", color, 0.16f);

        if (!hitEnemy)
        {
            CreateTexturedQuad("VFX_ImpactScorch", null, position + facing * 0.035f, Quaternion.LookRotation(facing), Vector3.one * 0.48f, "scorch_02", new Color(0.12f, 0.16f, 0.18f, 0.85f), 1.6f);
        }
    }

    public static void SpawnEnemyDeathBurst(Vector3 position)
    {
        CreateTexturedQuad("VFX_DeathFlash", null, position + Vector3.up * 0.8f, Quaternion.LookRotation(Vector3.up), Vector3.one * 1.45f, "circle_04", new Color(0.95f, 0.1f, 1f), 0.2f);
        SpawnBurst(position + Vector3.up * 0.8f, new Color(0.95f, 0.1f, 1f), 34, 0.12f, 0.3f, 0.6f);
    }

    public static void SpawnExpCollect(Vector3 position)
    {
        SpawnBurst(position, new Color(0.18f, 0.85f, 1f), 10, 0.04f, 0.12f, 0.25f);
    }

    public static void SpawnLevelUp(Vector3 position)
    {
        CreateTexturedQuad("VFX_LevelUpRing", null, position + Vector3.up * 1.2f, Quaternion.LookRotation(Vector3.up), Vector3.one * 1.7f, "twirl_02", new Color(0.2f, 1f, 0.55f), 0.5f);
        SpawnBurst(position + Vector3.up * 1.2f, new Color(0.2f, 1f, 0.55f), 42, 0.08f, 0.45f, 0.75f);
    }

    public static void SpawnLaserImpact(Vector3 position, Vector3 direction, Color color)
    {
        Vector3 facing = direction.sqrMagnitude > 0.001f ? -direction.normalized : Vector3.up;
        CreateTexturedQuad("VFX_LaserImpact", null, position + facing * 0.08f, Quaternion.LookRotation(facing), Vector3.one * 1.05f, "spark_07", color, 0.22f);
        CreateTexturedQuad("VFX_LaserRing", null, position + facing * 0.06f, Quaternion.LookRotation(facing), Vector3.one * 0.8f, "circle_03", color, 0.28f);
        SpawnBurst(position + facing * 0.08f, color, 22, 0.07f, 0.2f, 0.28f);
    }

    public static Transform CreatePersistentVfxQuad(string name, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, string textureName, Color color)
    {
        GameObject quad = CreateTexturedQuad(name, parent, Vector3.zero, Quaternion.identity, localScale, textureName, color, -1f);
        quad.transform.localPosition = localPosition;
        quad.transform.localRotation = localRotation;
        return quad.transform;
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

    private static GameObject CreateTexturedQuad(
        string name,
        Transform parent,
        Vector3 worldPosition,
        Quaternion rotation,
        Vector3 scale,
        string textureName,
        Color color,
        float lifetime)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;

        if (parent != null)
        {
            quad.transform.SetParent(parent, false);
        }
        else
        {
            quad.transform.position = worldPosition;
            quad.transform.rotation = rotation;
        }

        quad.transform.localScale = scale;
        RemoveCollider(quad);

        Renderer renderer = quad.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = CreateTextureMaterial(textureName, color);
        }

        if (lifetime > 0f)
        {
            Object.Destroy(quad, lifetime);
        }

        return quad;
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

    private static Material CreateTextureMaterial(string textureName, Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        Material material = new Material(shader);
        material.renderQueue = 3000;
        Texture2D texture = Resources.Load<Texture2D>(ParticleTextureRoot + textureName);

        if (texture == null)
        {
            Sprite sprite = Resources.Load<Sprite>(ParticleTextureRoot + textureName);
            texture = sprite != null ? sprite.texture : null;
        }

        if (texture != null)
        {
            material.mainTexture = texture;

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }
        }

        material.color = color;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        return material;
    }
}
