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

        CreateCube(root.transform, "LeftCover", new Vector3(-9f, 1f, 5f), new Vector3(3f, 2f, 7f), new Color(0.12f, 0.18f, 0.23f));
        CreateCube(root.transform, "RightCover", new Vector3(9f, 1f, 5f), new Vector3(3f, 2f, 7f), new Color(0.12f, 0.18f, 0.23f));
        CreateCube(root.transform, "BackCover", new Vector3(0f, 1f, -9f), new Vector3(8f, 2f, 2.5f), new Color(0.12f, 0.18f, 0.23f));

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
        CreateModel(root, "cables", "LeftCableRun", new Vector3(-half + 4f, 0.05f, -2f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.4f);
        CreateModel(root, "cables", "RightCableRun", new Vector3(half - 4f, 0.05f, 2f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 2.4f);

        CreateNeonStrip(root, "NorthNeon", new Vector3(0f, 0.08f, half - 1.3f), new Vector3(size - 5f, 0.06f, 0.2f));
        CreateNeonStrip(root, "SouthNeon", new Vector3(0f, 0.08f, -half + 1.3f), new Vector3(size - 5f, 0.06f, 0.2f));
        CreateNeonStrip(root, "EastNeon", new Vector3(half - 1.3f, 0.08f, 0f), new Vector3(0.2f, 0.06f, size - 5f));
        CreateNeonStrip(root, "WestNeon", new Vector3(-half + 1.3f, 0.08f, 0f), new Vector3(0.2f, 0.06f, size - 5f));
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
}
