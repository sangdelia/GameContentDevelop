using UnityEngine;

public class EnemyVisual : MonoBehaviour
{
    public enum EnemyVisualType
    {
        Melee,
        Ranged,
        Flying
    }

    private Transform visualRoot;
    private Transform core;
    private Transform leftPart;
    private Transform rightPart;
    private Transform chargeOrb;
    private Renderer[] renderers;
    private Color baseColor;
    private float bobSeed;
    private float chargeTimer;
    private float chargeDuration;

    public static EnemyVisual Attach(GameObject enemy, EnemyVisualType type)
    {
        EnemyVisual visual = enemy.GetComponent<EnemyVisual>();

        if (visual == null)
        {
            visual = enemy.AddComponent<EnemyVisual>();
        }

        visual.Build(type);
        return visual;
    }

    public void PlayAttackCharge(float duration)
    {
        chargeDuration = Mathf.Max(0.05f, duration);
        chargeTimer = chargeDuration;

        if (chargeOrb != null)
        {
            chargeOrb.gameObject.SetActive(true);
        }
    }

    public void PlayMeleePulse()
    {
        GameVfx.SpawnHitSpark(transform.position + transform.forward * 0.8f + Vector3.up * 0.8f, -transform.forward, true);
    }

    private void Build(EnemyVisualType type)
    {
        ClearExistingVisual();
        HideSourceRenderers();

        visualRoot = new GameObject("EnemyVisualRoot").transform;
        visualRoot.SetParent(transform, false);
        bobSeed = Random.Range(0f, 100f);

        bool importedModelApplied = TryBuildImportedEnemyModel(type);

        if (!importedModelApplied && type == EnemyVisualType.Melee)
        {
            baseColor = new Color(0.9f, 0.12f, 0.18f);
            core = CreateModelPart("Models/SpaceStation/container-tall", "MeleeContainerCore", Vector3.zero, Vector3.one * 1.15f, baseColor);

            if (core == null)
            {
                core = CreatePart("MeleeCore", PrimitiveType.Capsule, new Vector3(0f, 0.08f, 0f), new Vector3(0.75f, 1.05f, 0.75f), baseColor);
            }

            leftPart = CreateModelPart("Models/SpaceStation/pipe", "MeleeLeftPipeClaw", new Vector3(-0.55f, 0.2f, 0.28f), new Vector3(0.9f, 0.9f, 1.5f), new Color(0.95f, 0.22f, 0.14f));
            rightPart = CreateModelPart("Models/SpaceStation/pipe", "MeleeRightPipeClaw", new Vector3(0.55f, 0.2f, 0.28f), new Vector3(0.9f, 0.9f, 1.5f), new Color(0.95f, 0.22f, 0.14f));

            if (leftPart == null)
            {
                leftPart = CreatePart("MeleeLeftClaw", PrimitiveType.Cube, new Vector3(-0.55f, 0.2f, 0.28f), new Vector3(0.18f, 0.18f, 0.75f), new Color(0.95f, 0.22f, 0.14f));
            }

            if (rightPart == null)
            {
                rightPart = CreatePart("MeleeRightClaw", PrimitiveType.Cube, new Vector3(0.55f, 0.2f, 0.28f), new Vector3(0.18f, 0.18f, 0.75f), new Color(0.95f, 0.22f, 0.14f));
            }
        }
        else if (!importedModelApplied && type == EnemyVisualType.Ranged)
        {
            baseColor = new Color(0.08f, 0.8f, 1f);
            core = CreateModelPart("Models/SpaceStation/computer-system", "RangedComputerCore", Vector3.zero, Vector3.one * 1.35f, baseColor);

            if (core == null)
            {
                core = CreatePart("RangedCore", PrimitiveType.Capsule, new Vector3(0f, 0.05f, 0f), new Vector3(0.62f, 0.95f, 0.62f), baseColor);
            }

            leftPart = CreateModelPart("Models/SpaceStation/pipe-ring-colored", "RangedBarrelLeft", new Vector3(-0.32f, 0.18f, 0.58f), Vector3.one * 0.95f, new Color(0.04f, 0.28f, 0.36f));
            rightPart = CreateModelPart("Models/SpaceStation/pipe-ring-colored", "RangedBarrelRight", new Vector3(0.32f, 0.18f, 0.58f), Vector3.one * 0.95f, new Color(0.04f, 0.28f, 0.36f));

            if (leftPart == null)
            {
                leftPart = CreatePart("RangedBarrelLeft", PrimitiveType.Cube, new Vector3(-0.32f, 0.18f, 0.58f), new Vector3(0.14f, 0.14f, 0.72f), new Color(0.04f, 0.28f, 0.36f));
            }

            if (rightPart == null)
            {
                rightPart = CreatePart("RangedBarrelRight", PrimitiveType.Cube, new Vector3(0.32f, 0.18f, 0.58f), new Vector3(0.14f, 0.14f, 0.72f), new Color(0.04f, 0.28f, 0.36f));
            }
        }
        else if (!importedModelApplied)
        {
            baseColor = new Color(1f, 0.22f, 0.95f);
            core = CreateModelPart("Models/SpaceStation/computer-wide", "DroneComputerCore", Vector3.zero, Vector3.one * 1.05f, baseColor);

            if (core == null)
            {
                core = CreatePart("DroneCore", PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), new Vector3(0.9f, 0.45f, 0.9f), baseColor);
            }

            leftPart = CreateModelPart("Models/KenneySpace/cables", "DroneWingLeft", new Vector3(-0.72f, 0f, 0f), Vector3.one * 0.95f, new Color(0.95f, 0.45f, 1f));
            rightPart = CreateModelPart("Models/KenneySpace/cables", "DroneWingRight", new Vector3(0.72f, 0f, 0f), Vector3.one * 0.95f, new Color(0.95f, 0.45f, 1f));

            if (leftPart == null)
            {
                leftPart = CreatePart("DroneWingLeft", PrimitiveType.Cube, new Vector3(-0.72f, 0f, 0f), new Vector3(0.7f, 0.08f, 0.24f), new Color(0.95f, 0.45f, 1f));
            }

            if (rightPart == null)
            {
                rightPart = CreatePart("DroneWingRight", PrimitiveType.Cube, new Vector3(0.72f, 0f, 0f), new Vector3(0.7f, 0.08f, 0.24f), new Color(0.95f, 0.45f, 1f));
            }
        }

        chargeOrb = CreatePart("AttackChargeOrb", PrimitiveType.Sphere, new Vector3(0f, 0.35f, 0.82f), Vector3.one * 0.22f, Color.white);
        chargeOrb.gameObject.SetActive(false);
        renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
    }

    private void Update()
    {
        if (visualRoot == null)
            return;

        visualRoot.localPosition = Vector3.up * (Mathf.Sin(Time.time * 4f + bobSeed) * 0.035f);

        if (leftPart != null)
        {
            leftPart.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 6f + bobSeed) * 8f, 0f);
        }

        if (rightPart != null)
        {
            rightPart.localRotation = Quaternion.Euler(0f, -Mathf.Sin(Time.time * 6f + bobSeed) * 8f, 0f);
        }

        UpdateCharge();
    }

    private void UpdateCharge()
    {
        if (chargeTimer <= 0f || chargeOrb == null)
            return;

        chargeTimer -= Time.deltaTime;
        float progress = 1f - Mathf.Clamp01(chargeTimer / chargeDuration);
        chargeOrb.localScale = Vector3.one * Mathf.Lerp(0.15f, 0.46f, progress);

        Renderer chargeRenderer = chargeOrb.GetComponent<Renderer>();
        if (chargeRenderer != null)
        {
            Color color = Color.Lerp(Color.white, baseColor, progress);
            chargeRenderer.material.color = color;
            chargeRenderer.material.SetColor("_EmissionColor", color * Mathf.Lerp(2f, 5f, progress));
        }

        if (chargeTimer <= 0f)
        {
            chargeOrb.gameObject.SetActive(false);
        }
    }

    private Transform CreatePart(string partName, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = partName;
        part.transform.SetParent(visualRoot, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = part.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = color;
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", color * 0.9f);

        return part.transform;
    }

    private Transform CreateModelPart(string resourcePath, string partName, Vector3 localPosition, Vector3 localScale, Color tint)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcePath);

        if (prefab == null)
            return null;

        GameObject part = Instantiate(prefab, visualRoot);
        part.name = partName;
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;

        Collider[] colliders = part.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }

        Renderer[] modelRenderers = part.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < modelRenderers.Length; i++)
        {
            ApplyRendererMaterial(modelRenderers[i], tint, false);
        }

        return part.transform;
    }

    private bool TryBuildImportedEnemyModel(EnemyVisualType type)
    {
        string resourcePath;
        string modelName;
        Vector3 localPosition;
        Vector3 localScale;

        if (type == EnemyVisualType.Melee)
        {
            resourcePath = "Models/Enemies/Enemy_Trilobite";
            modelName = "EnemyModel_Trilobite";
            localPosition = new Vector3(0f, -0.2f, 0f);
            localScale = Vector3.one * 0.95f;
            baseColor = new Color(0.9f, 0.12f, 0.18f);
        }
        else if (type == EnemyVisualType.Ranged)
        {
            resourcePath = "Models/Enemies/Enemy_QuadShell";
            modelName = "EnemyModel_QuadShell";
            localPosition = new Vector3(0f, -0.15f, 0f);
            localScale = Vector3.one * 0.82f;
            baseColor = new Color(0.08f, 0.8f, 1f);
        }
        else
        {
            resourcePath = "Models/Enemies/Enemy_EyeDrone";
            modelName = "EnemyModel_EyeDrone";
            localPosition = Vector3.zero;
            localScale = Vector3.one * 0.8f;
            baseColor = new Color(1f, 0.22f, 0.95f);
        }

        GameObject prefab = Resources.Load<GameObject>(resourcePath);

        if (prefab == null)
            return false;

        GameObject model = Instantiate(prefab, visualRoot);
        model.name = modelName;
        model.transform.localPosition = localPosition;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = localScale;

        Collider[] colliders = model.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }

        Renderer[] modelRenderers = model.GetComponentsInChildren<Renderer>();
        bool useLargeEnemyTextures = type != EnemyVisualType.Flying;

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            ApplyRendererMaterial(modelRenderers[i], baseColor, useLargeEnemyTextures);
        }

        core = model.transform;
        leftPart = null;
        rightPart = null;
        return true;
    }

    private void ApplyRendererMaterial(Renderer targetRenderer, Color tint, bool useLargeEnemyTextures)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Texture2D baseMap = Resources.Load<Texture2D>(useLargeEnemyTextures
            ? "Textures/ImportedSciFi/T_Enemies_Large_BaseColor"
            : "Textures/ImportedSciFi/T_Enemies_BaseColor");
        Texture2D normalMap = Resources.Load<Texture2D>(useLargeEnemyTextures
            ? "Textures/ImportedSciFi/T_Enemies_Large_Normal"
            : "Textures/ImportedSciFi/T_Enemies_Normal");
        Texture2D emissionMap = Resources.Load<Texture2D>(useLargeEnemyTextures
            ? "Textures/ImportedSciFi/T_Enemies_Large_Emissive"
            : "Textures/ImportedSciFi/T_Enemies_Emissive");

        material.color = Color.Lerp(Color.white, tint, 0.18f);

        if (baseMap != null)
        {
            material.mainTexture = baseMap;
        }

        if (normalMap != null && material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", normalMap);
            material.EnableKeyword("_NORMALMAP");
        }

        if (emissionMap != null && material.HasProperty("_EmissionMap"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap", emissionMap);
            material.SetColor("_EmissionColor", tint * 1.35f);
        }
        else if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", tint * 0.55f);
        }

        targetRenderer.material = material;
    }

    private void HideSourceRenderers()
    {
        Renderer[] sourceRenderers = GetComponentsInChildren<Renderer>();

        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            if (sourceRenderers[i].transform.IsChildOf(transform) && sourceRenderers[i].transform.name != "EnemyVisualRoot")
            {
                sourceRenderers[i].enabled = false;
            }
        }
    }

    private void ClearExistingVisual()
    {
        Transform existing = transform.Find("EnemyVisualRoot");

        if (existing != null)
        {
            Destroy(existing.gameObject);
        }
    }
}
