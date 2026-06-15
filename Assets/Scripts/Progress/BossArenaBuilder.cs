using UnityEngine;

public static class BossArenaBuilder
{
    private const string SpaceKitPath = "Models/KenneySpace/";

    public static GameObject BuildArena(Vector3 center, float size)
    {
        GameObject root = new GameObject("TempBossArena");
        root.transform.position = center;

        CreateCube(root.transform, "ArenaFloor", Vector3.zero, new Vector3(size, 0.4f, size), new Color(0.09f, 0.1f, 0.13f));
        BuildSpaceKitSet(root.transform, size);

        float half = size * 0.5f;
        CreateCube(root.transform, "NorthWall", new Vector3(0f, 2f, half), new Vector3(size, 4f, 1f), new Color(0.18f, 0.24f, 0.3f));
        CreateCube(root.transform, "SouthWall", new Vector3(0f, 2f, -half), new Vector3(size, 4f, 1f), new Color(0.18f, 0.24f, 0.3f));
        CreateCube(root.transform, "EastWall", new Vector3(half, 2f, 0f), new Vector3(1f, 4f, size), new Color(0.18f, 0.24f, 0.3f));
        CreateCube(root.transform, "WestWall", new Vector3(-half, 2f, 0f), new Vector3(1f, 4f, size), new Color(0.18f, 0.24f, 0.3f));

        BuildLaserCoverWalls(root.transform);
        BuildBossArenaDressing(root.transform, size);

        GameObject lightObject = new GameObject("ArenaLight");
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = Vector3.up * 9f;

        Light mainLight = lightObject.AddComponent<Light>();
        mainLight.type = LightType.Point;
        mainLight.color = new Color(0.35f, 0.85f, 1f);
        mainLight.range = size;
        mainLight.intensity = 2.4f;

        return root;
    }

    private static void BuildSpaceKitSet(Transform root, float size)
    {
        float half = size * 0.5f;

        GameObject mainRoom = CreateModel(root, "room-large", "SpaceKitRoomLarge", Vector3.zero, Quaternion.identity, Vector3.one * 4.8f);

        if (mainRoom != null)
        {
            SetLayerRecursive(mainRoom, LayerMask.NameToLayer("Default"));
        }

        CreateModel(root, "gate", "NorthGateDecor", new Vector3(0f, 0.05f, half - 0.9f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 3.2f);
        CreateModel(root, "gate", "SouthGateDecor", new Vector3(0f, 0.05f, -half + 0.9f), Quaternion.identity, Vector3.one * 3.2f);
        CreateModel(root, "gate-door-window", "BossDoorNorthWindow", new Vector3(0f, 0.1f, half - 0.62f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 3.1f);
        CreateModel(root, "gate-door", "BossDoorSouthBulkhead", new Vector3(0f, 0.1f, -half + 0.62f), Quaternion.identity, Vector3.one * 3.1f);
        CreateModel(root, "cables", "LeftCableRun", new Vector3(-half + 4f, 0.05f, -2f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.4f);
        CreateModel(root, "cables", "RightCableRun", new Vector3(half - 4f, 0.05f, 2f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 2.4f);
        CreateModel(root, "template-floor-detail-a", "NorthFloorCircuit", new Vector3(0f, 0.06f, 8f), Quaternion.identity, Vector3.one * 3.6f);
        CreateModel(root, "template-floor-detail", "SouthFloorCircuit", new Vector3(0f, 0.06f, -8f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 3.6f);

        CreateNeonStrip(root, "NorthNeon", new Vector3(0f, 0.08f, half - 1.3f), new Vector3(size - 5f, 0.06f, 0.2f));
        CreateNeonStrip(root, "SouthNeon", new Vector3(0f, 0.08f, -half + 1.3f), new Vector3(size - 5f, 0.06f, 0.2f));
        CreateNeonStrip(root, "EastNeon", new Vector3(half - 1.3f, 0.08f, 0f), new Vector3(0.2f, 0.06f, size - 5f));
        CreateNeonStrip(root, "WestNeon", new Vector3(-half + 1.3f, 0.08f, 0f), new Vector3(0.2f, 0.06f, size - 5f));
    }

    private static void BuildLaserCoverWalls(Transform root)
    {
        Color coverColor = new Color(0.11f, 0.16f, 0.21f);
        Color accentColor = new Color(0.04f, 0.75f, 1f);

        CreateCoverWall(root, "LaserCover_LeftForward", new Vector3(-8.5f, 2.05f, 7.5f), new Vector3(2.2f, 4.1f, 8.2f), coverColor, accentColor);
        CreateCoverWall(root, "LaserCover_RightForward", new Vector3(8.5f, 2.05f, 7.5f), new Vector3(2.2f, 4.1f, 8.2f), coverColor, accentColor);
        CreateCoverWall(root, "LaserCover_LeftBack", new Vector3(-8.5f, 2.05f, -8.5f), new Vector3(2.2f, 4.1f, 7.4f), coverColor, accentColor);
        CreateCoverWall(root, "LaserCover_RightBack", new Vector3(8.5f, 2.05f, -8.5f), new Vector3(2.2f, 4.1f, 7.4f), coverColor, accentColor);
        CreateCoverWall(root, "LaserCover_RearGate", new Vector3(0f, 2.05f, -17.5f), new Vector3(9f, 4.1f, 1.8f), coverColor, accentColor);
    }

    private static void CreateCoverWall(Transform root, string name, Vector3 localPosition, Vector3 scale, Color color, Color accentColor)
    {
        GameObject wall = CreateCube(root, name, localPosition, scale, color);
        wall.tag = "Untagged";

        Collider collider = wall.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = false;
        }

        Vector3 accentScale = new Vector3(
            Mathf.Max(0.12f, scale.x + 0.05f),
            0.08f,
            Mathf.Max(0.12f, scale.z + 0.05f)
        );

        GameObject topAccent = CreateCube(root, name + "_NeonTop", localPosition + Vector3.up * (scale.y * 0.5f + 0.08f), accentScale, accentColor);
        Collider accentCollider = topAccent.GetComponent<Collider>();
        if (accentCollider != null)
        {
            Object.Destroy(accentCollider);
        }

        Renderer accentRenderer = topAccent.GetComponent<Renderer>();
        accentRenderer.material.EnableKeyword("_EMISSION");
        accentRenderer.material.SetColor("_EmissionColor", accentColor * 2.1f);
    }

    private static void BuildBossArenaDressing(Transform root, float size)
    {
        float half = size * 0.5f;

        CreateModel(root, "template-wall-detail-a", "WestWallTechPanelA", new Vector3(-half + 0.7f, 1.8f, -7f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.4f);
        CreateModel(root, "template-wall-detail-a", "EastWallTechPanelA", new Vector3(half - 0.7f, 1.8f, 7f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 2.4f);
        CreateModel(root, "template-wall-top", "NorthUpperPanel", new Vector3(-8f, 3.6f, half - 0.7f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 2.6f);
        CreateModel(root, "template-wall-top", "SouthUpperPanel", new Vector3(8f, 3.6f, -half + 0.7f), Quaternion.identity, Vector3.one * 2.6f);

        CreateModel(root, "corridor-end", "LeftAlcoveModule", new Vector3(-half + 2.8f, 0.05f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.4f);
        CreateModel(root, "corridor-end", "RightAlcoveModule", new Vector3(half - 2.8f, 0.05f, 0f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 2.4f);

        CreateEnergyPillar(root, "NorthWestEnergyPillar", new Vector3(-half + 4.3f, 1.2f, half - 4.3f));
        CreateEnergyPillar(root, "NorthEastEnergyPillar", new Vector3(half - 4.3f, 1.2f, half - 4.3f));
        CreateEnergyPillar(root, "SouthWestEnergyPillar", new Vector3(-half + 4.3f, 1.2f, -half + 4.3f));
        CreateEnergyPillar(root, "SouthEastEnergyPillar", new Vector3(half - 4.3f, 1.2f, -half + 4.3f));
    }

    private static void CreateEnergyPillar(Transform parent, string name, Vector3 localPosition)
    {
        GameObject pillar = CreateCube(parent, name, localPosition, new Vector3(0.42f, 2.4f, 0.42f), new Color(0.08f, 0.14f, 0.18f));
        Renderer renderer = pillar.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", new Color(0.02f, 0.8f, 1f) * 1.15f);
        }

        Light light = pillar.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.05f, 0.9f, 1f);
        light.range = 6f;
        light.intensity = 0.9f;
    }

    private static GameObject CreateModel(Transform parent, string resourceName, string objectName, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        GameObject prefab = Resources.Load<GameObject>(SpaceKitPath + resourceName);

        if (prefab == null)
            return null;

        GameObject instance = Object.Instantiate(prefab, parent);
        instance.name = objectName;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = localRotation;
        instance.transform.localScale = localScale;
        RemoveCollidersRecursive(instance);

        return instance;
    }

    private static void CreateNeonStrip(Transform parent, string name, Vector3 localPosition, Vector3 scale)
    {
        GameObject strip = CreateCube(parent, name, localPosition, scale, new Color(0.05f, 0.9f, 1f));
        Collider collider = strip.GetComponent<Collider>();

        if (collider != null)
        {
            Object.Destroy(collider);
        }

        Renderer renderer = strip.GetComponent<Renderer>();
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", new Color(0.05f, 0.9f, 1f) * 2.4f);
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 localPosition, Vector3 scale, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = scale;

        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = color;

        return cube;
    }

    private static void SetLayerRecursive(GameObject target, int layer)
    {
        if (layer < 0)
            return;

        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    private static void RemoveCollidersRecursive(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Object.Destroy(colliders[i]);
        }
    }
}
