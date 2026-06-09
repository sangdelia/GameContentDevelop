using UnityEngine;

public static class BossArenaBuilder
{
    public static GameObject BuildArena(Vector3 center, float size)
    {
        GameObject root = new GameObject("TempBossArena");
        root.transform.position = center;

        CreateCube(root.transform, "ArenaFloor", Vector3.zero, new Vector3(size, 0.4f, size), new Color(0.09f, 0.1f, 0.13f));

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
}
