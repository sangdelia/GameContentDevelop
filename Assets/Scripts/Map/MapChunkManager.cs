using System.Collections.Generic;
using UnityEngine;

public class MapChunkManager : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Chunk Settings")]
    [SerializeField] private GameObject chunkBasePrefab;
    [SerializeField] private float chunkSize = 45f;
    [SerializeField] private int viewRange = 3;
    [SerializeField] private int worldSeed = 12345;
    [SerializeField] private float preloadMargin = 12f;

    [Header("Random Props")]
    [SerializeField] private GameObject[] propPrefabs;
    [SerializeField] private bool useSciFiResourceProps = true;
    [SerializeField] private float sciFiPropChance = 0.75f;
    [SerializeField] private int minPropsPerChunk = 3;
    [SerializeField] private int maxPropsPerChunk = 8;
    [SerializeField] private float propSpawnPadding = 5f;
    [SerializeField] private float minPropSpacing = 7f;
    [SerializeField] private float startSafeRadius = 7f;
    [SerializeField] private int maxPlacementAttempts = 40;

    private Vector2Int currentChunkCoord;
    private Vector2Int startChunkCoord;
    private Vector3 startPosition;
    private readonly Dictionary<Vector2Int, GameObject> activeChunks = new();
    private readonly List<GameObject> sciFiPropPrefabs = new();
    private bool chunksVisible = true;

    private readonly string[] sciFiPropResourceNames =
    {
        "computer",
        "computer-wide",
        "computer-system",
        "container",
        "container-wide",
        "container-tall",
        "display-wall",
        "display-wall-wide",
        "pipe",
        "pipe-bend",
        "pipe-ring-colored",
        "rail",
        "rail-narrow",
        "door-single-closed",
        "door-double-closed",
        "skip"
    };

    private void OnValidate()
    {
        chunkSize = Mathf.Max(1f, chunkSize);
        viewRange = Mathf.Max(1, viewRange);
        preloadMargin = Mathf.Clamp(preloadMargin, 0f, chunkSize * 0.45f);
        minPropsPerChunk = Mathf.Max(0, minPropsPerChunk);
        maxPropsPerChunk = Mathf.Max(minPropsPerChunk, maxPropsPerChunk);
        propSpawnPadding = Mathf.Clamp(propSpawnPadding, 0f, chunkSize * 0.49f);
        minPropSpacing = Mathf.Max(0f, minPropSpacing);
        startSafeRadius = Mathf.Max(0f, startSafeRadius);
        maxPlacementAttempts = Mathf.Max(1, maxPlacementAttempts);
    }

    private void Start()
    {
        ApplyRuntimeDefaults();
        LoadSciFiProps();

        if (player == null)
        {
            PlayerLevel playerLevel = FindFirstObjectByType<PlayerLevel>();
            if (playerLevel != null)
            {
                player = playerLevel.transform;
            }
        }

        if (player == null || chunkBasePrefab == null)
        {
            Debug.LogWarning("MapChunkManager: player or chunkBasePrefab is missing.");
            enabled = false;
            return;
        }

        currentChunkCoord = GetChunkCoord(player.position);
        startChunkCoord = currentChunkCoord;
        startPosition = player.position;
        RefreshChunks();
    }

    private void ApplyRuntimeDefaults()
    {
        if (maxPlacementAttempts <= 0)
        {
            maxPlacementAttempts = 40;
        }

        if (minPropSpacing <= 0f)
        {
            minPropSpacing = 5f;
        }

        if (startSafeRadius <= 0f)
        {
            startSafeRadius = 7f;
        }

        if (maxPropsPerChunk < minPropsPerChunk)
        {
            maxPropsPerChunk = minPropsPerChunk;
        }
    }

    private void Update()
    {
        if (player == null)
            return;

        Vector2Int newChunkCoord = GetChunkCoord(player.position);

        if (newChunkCoord != currentChunkCoord)
        {
            currentChunkCoord = newChunkCoord;
        }

        RefreshChunks();
    }

    private Vector2Int GetChunkCoord(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / chunkSize),
            Mathf.FloorToInt(worldPosition.z / chunkSize)
        );
    }

    private Vector3 GetChunkOrigin(Vector2Int coord)
    {
        return new Vector3(coord.x * chunkSize, 0f, coord.y * chunkSize);
    }

    private void RefreshChunks()
    {
        HashSet<Vector2Int> neededCoords = new();
        GetPreloadDirection(out int preloadX, out int preloadZ);

        for (int x = -viewRange; x <= viewRange; x++)
        {
            for (int z = -viewRange; z <= viewRange; z++)
            {
                AddNeededCoord(neededCoords, currentChunkCoord + new Vector2Int(x, z));
            }
        }

        if (preloadX != 0)
        {
            int x = preloadX > 0 ? viewRange + 1 : -viewRange - 1;

            for (int z = -viewRange; z <= viewRange; z++)
            {
                AddNeededCoord(neededCoords, currentChunkCoord + new Vector2Int(x, z));
            }
        }

        if (preloadZ != 0)
        {
            int z = preloadZ > 0 ? viewRange + 1 : -viewRange - 1;

            for (int x = -viewRange; x <= viewRange; x++)
            {
                AddNeededCoord(neededCoords, currentChunkCoord + new Vector2Int(x, z));
            }
        }

        if (preloadX != 0 && preloadZ != 0)
        {
            int x = preloadX > 0 ? viewRange + 1 : -viewRange - 1;
            int z = preloadZ > 0 ? viewRange + 1 : -viewRange - 1;
            AddNeededCoord(neededCoords, currentChunkCoord + new Vector2Int(x, z));
        }

        List<Vector2Int> removeCoords = new();

        foreach (Vector2Int coord in activeChunks.Keys)
        {
            if (!neededCoords.Contains(coord))
            {
                removeCoords.Add(coord);
            }
        }

        foreach (Vector2Int coord in removeCoords)
        {
            Destroy(activeChunks[coord]);
            activeChunks.Remove(coord);
        }
    }

    private void AddNeededCoord(HashSet<Vector2Int> neededCoords, Vector2Int coord)
    {
        neededCoords.Add(coord);

        if (!activeChunks.ContainsKey(coord))
        {
            CreateChunk(coord);
        }
    }

    private void GetPreloadDirection(out int preloadX, out int preloadZ)
    {
        preloadX = 0;
        preloadZ = 0;

        if (player == null || preloadMargin <= 0f)
            return;

        Vector3 chunkOrigin = GetChunkOrigin(currentChunkCoord);
        float localX = player.position.x - chunkOrigin.x;
        float localZ = player.position.z - chunkOrigin.z;

        if (localX >= chunkSize - preloadMargin)
        {
            preloadX = 1;
        }
        else if (localX <= preloadMargin)
        {
            preloadX = -1;
        }

        if (localZ >= chunkSize - preloadMargin)
        {
            preloadZ = 1;
        }
        else if (localZ <= preloadMargin)
        {
            preloadZ = -1;
        }
    }

    public void SetChunksVisible(bool visible)
    {
        chunksVisible = visible;

        foreach (GameObject chunk in activeChunks.Values)
        {
            if (chunk != null)
            {
                chunk.SetActive(visible);
            }
        }
    }

    private void CreateChunk(Vector2Int coord)
    {
        GameObject chunkRoot = new GameObject($"Chunk_{coord.x}_{coord.y}");
        chunkRoot.transform.position = GetChunkOrigin(coord);

        GameObject ground = Instantiate(chunkBasePrefab, chunkRoot.transform);
        ground.name = "Ground";
        ground.transform.localPosition = Vector3.zero;
        ground.transform.localRotation = Quaternion.identity;
        MatchGroundSize(ground);

        GameObject propsRoot = new GameObject("Props");
        propsRoot.transform.SetParent(chunkRoot.transform, false);

        GenerateProps(coord, propsRoot.transform);

        activeChunks.Add(coord, chunkRoot);
        chunkRoot.SetActive(chunksVisible);
    }

    private void MatchGroundSize(GameObject ground)
    {
        Renderer renderer = ground.GetComponentInChildren<Renderer>();

        if (renderer == null)
            return;

        Bounds bounds = renderer.bounds;

        if (bounds.size.x <= 0f || bounds.size.z <= 0f)
            return;

        Vector3 scale = ground.transform.localScale;
        scale.x *= chunkSize / bounds.size.x;
        scale.z *= chunkSize / bounds.size.z;
        ground.transform.localScale = scale;
    }

    private void GenerateProps(Vector2Int coord, Transform propsRoot)
    {
        if ((propPrefabs == null || propPrefabs.Length == 0) && sciFiPropPrefabs.Count == 0)
            return;

        int seed = worldSeed + coord.x * 73856093 + coord.y * 19349663;
        System.Random random = new System.Random(seed);

        int propCount = random.Next(minPropsPerChunk, maxPropsPerChunk + 1);
        float min = propSpawnPadding;
        float max = chunkSize - propSpawnPadding;
        float minPropSpacingSqr = minPropSpacing * minPropSpacing;
        float startSafeRadiusSqr = startSafeRadius * startSafeRadius;
        List<Vector3> placedPositions = new();

        for (int i = 0; i < propCount; i++)
        {
            if (!TryGetPropPosition(random, coord, min, max, minPropSpacingSqr, startSafeRadiusSqr, placedPositions, out Vector3 localPosition))
                break;

            placedPositions.Add(localPosition);
            CreateProp(random, propsRoot, i, localPosition);
        }
    }

    private bool TryGetPropPosition(
        System.Random random,
        Vector2Int coord,
        float min,
        float max,
        float minPropSpacingSqr,
        float startSafeRadiusSqr,
        List<Vector3> placedPositions,
        out Vector3 localPosition)
    {
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            localPosition = new Vector3(
                RandomRange(random, min, max),
                0f,
                RandomRange(random, min, max)
            );

            if (IsInsideStartSafeArea(coord, localPosition, startSafeRadiusSqr))
                continue;

            if (IsTooCloseToPlacedProps(localPosition, placedPositions, minPropSpacingSqr))
                continue;

            return true;
        }

        localPosition = Vector3.zero;
        return false;
    }

    private bool IsInsideStartSafeArea(Vector2Int coord, Vector3 localPosition, float startSafeRadiusSqr)
    {
        if (coord != startChunkCoord)
            return false;

        Vector3 chunkOrigin = GetChunkOrigin(coord);
        Vector3 worldPosition = chunkOrigin + localPosition;
        Vector2 offset = new Vector2(worldPosition.x - startPosition.x, worldPosition.z - startPosition.z);

        return offset.sqrMagnitude < startSafeRadiusSqr;
    }

    private bool IsTooCloseToPlacedProps(Vector3 localPosition, List<Vector3> placedPositions, float minPropSpacingSqr)
    {
        foreach (Vector3 placedPosition in placedPositions)
        {
            Vector2 offset = new Vector2(localPosition.x - placedPosition.x, localPosition.z - placedPosition.z);

            if (offset.sqrMagnitude < minPropSpacingSqr)
                return true;
        }

        return false;
    }

    private void CreateProp(System.Random random, Transform propsRoot, int index, Vector3 localPosition)
    {
        GameObject prefab = PickPropPrefab(random);

        if (prefab == null)
            return;

        Quaternion localRotation = Quaternion.Euler(0f, RandomRange(random, 0f, 360f), 0f);
        float scale = sciFiPropPrefabs.Contains(prefab)
            ? RandomRange(random, 1.3f, 2.4f)
            : RandomRange(random, 0.85f, 1.25f);

        GameObject prop = Instantiate(prefab, propsRoot);
        prop.name = $"{prefab.name}_{index}";
        prop.transform.localPosition = localPosition;
        prop.transform.localRotation = localRotation;
        prop.transform.localScale = Vector3.Scale(prop.transform.localScale, Vector3.one * scale);

        PlaceObjectOnGround(prop, 0f);
        EnsureObstacleCollider(prop);
    }

    private GameObject PickPropPrefab(System.Random random)
    {
        bool useSciFi = useSciFiResourceProps &&
            sciFiPropPrefabs.Count > 0 &&
            (propPrefabs == null || propPrefabs.Length == 0 || random.NextDouble() <= sciFiPropChance);

        if (useSciFi)
        {
            return sciFiPropPrefabs[random.Next(0, sciFiPropPrefabs.Count)];
        }

        if (propPrefabs == null || propPrefabs.Length == 0)
            return null;

        return propPrefabs[random.Next(0, propPrefabs.Length)];
    }

    private void LoadSciFiProps()
    {
        sciFiPropPrefabs.Clear();

        if (!useSciFiResourceProps)
            return;

        for (int i = 0; i < sciFiPropResourceNames.Length; i++)
        {
            GameObject prefab = Resources.Load<GameObject>("Models/SpaceStation/" + sciFiPropResourceNames[i]);

            if (prefab != null)
            {
                sciFiPropPrefabs.Add(prefab);
            }
        }
    }

    private void EnsureObstacleCollider(GameObject prop)
    {
        Collider existingCollider = prop.GetComponentInChildren<Collider>();

        if (existingCollider != null)
            return;

        Renderer[] renderers = prop.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        BoxCollider collider = prop.AddComponent<BoxCollider>();
        collider.center = prop.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = prop.transform.InverseTransformVector(bounds.size);
        collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
    }

    private void PlaceObjectOnGround(GameObject obj, float groundY)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float offsetY = groundY - bounds.min.y;
        obj.transform.position += Vector3.up * offsetY;
    }

    private float RandomRange(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }
}
