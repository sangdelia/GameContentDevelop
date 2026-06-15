using UnityEngine;

public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    [Header("Boss Portal Condition")]
    [SerializeField] private int killsRequiredForPortal = 10;
    [SerializeField] private float portalSpawnDistance = 8f;

    [Header("Boss Arena")]
    [SerializeField] private Vector3 bossArenaCenter = new Vector3(0f, 0f, 500f);
    [SerializeField] private Vector3 bossPlayerSpawnPosition = new Vector3(0f, 1f, 488f);
    [SerializeField] private Vector3 bossSpawnPosition = new Vector3(0f, 0f, 508f);
    [SerializeField] private float bossArenaSize = 46f;
    [SerializeField] private float bossHp = 5000f;

    private int killCount;
    private bool portalOpened;
    private bool bossFightStarted;
    private Transform player;
    private BossPortal activePortal;
    private TempBossController activeBoss;
    private GameObject bossArenaRoot;

    public int KillCount => killCount;
    public int KillsRequiredForPortal => killsRequiredForPortal;
    public bool PortalOpened => portalOpened;
    public bool BossFightStarted => bossFightStarted;

    public event System.Action<int, int, bool> ProgressChanged;
    public event System.Action BossPortalOpened;
    public event System.Action BossFightStartedEvent;
    public event System.Action<EnemyHealth> BossSpawned;
    public event System.Action BossDefeated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeManager()
    {
        if (FindFirstObjectByType<GameProgressManager>() != null)
            return;

        GameObject managerObject = new GameObject("GameProgressManager");
        managerObject.AddComponent<GameProgressManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        EnemyHealth.EnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        EnemyHealth.EnemyKilled -= HandleEnemyKilled;
    }

    private void Start()
    {
        FindPlayer();
        PrepareBossArena();
        NotifyProgressChanged();
    }

    private void HandleEnemyKilled(EnemyHealth enemy)
    {
        if (enemy != null && enemy.GetComponent<TempBossController>() != null)
            return;

        if (bossFightStarted)
            return;

        killCount++;
        NotifyProgressChanged();

        if (!portalOpened && killCount >= killsRequiredForPortal)
        {
            OpenBossPortal();
        }
    }

    private void OpenBossPortal()
    {
        FindPlayer();

        if (player == null)
            return;

        portalOpened = true;

        Vector3 spawnPosition = player.position + GetFlatForward(player) * portalSpawnDistance;
        spawnPosition.y = player.position.y + 0.15f;

        activePortal = BossPortal.Create(spawnPosition, this);
        GameAudio.PlayPortalOpen(spawnPosition);
        BossPortalOpened?.Invoke();
        NotifyProgressChanged();
    }

    public void EnterBossPortal()
    {
        if (!portalOpened || bossFightStarted)
            return;

        FindPlayer();

        if (player == null)
            return;

        bossFightStarted = true;
        BossFightStartedEvent?.Invoke();

        if (activePortal != null)
        {
            Destroy(activePortal.gameObject);
        }

        DisableNormalBattleSystems();
        ShowBossArena();

        TeleportPlayerToBossArena();

        activeBoss = TempBossController.Create(bossSpawnPosition, player, bossHp);
        activeBoss.Defeated += HandleBossDefeated;
        BossSpawned?.Invoke(activeBoss.GetComponent<EnemyHealth>());

        NotifyProgressChanged();
    }

    private void HandleBossDefeated()
    {
        BossDefeated?.Invoke();
    }

    private void DisableNormalBattleSystems()
    {
        EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);

        foreach (EnemySpawner spawner in spawners)
        {
            spawner.enabled = false;
        }

        MapChunkManager[] mapManagers = FindObjectsByType<MapChunkManager>(FindObjectsSortMode.None);

        foreach (MapChunkManager mapManager in mapManagers)
        {
            mapManager.enabled = false;
            mapManager.SetChunksVisible(false);
        }

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy != null && enemy.GetComponent<TempBossController>() == null)
            {
                Destroy(enemy.gameObject);
            }
        }
    }

    private void PrepareBossArena()
    {
        if (bossArenaRoot != null)
            return;

        bossArenaRoot = BossArenaBuilder.BuildArena(bossArenaCenter, bossArenaSize);
        bossArenaRoot.SetActive(false);
    }

    private void ShowBossArena()
    {
        PrepareBossArena();
        bossArenaRoot.SetActive(true);
    }

    private void TeleportPlayerToBossArena()
    {
        Vector3 safePlayerSpawnPosition = GetSafeBossPlayerSpawnPosition();
        Vector3 lookDirection = bossSpawnPosition - safePlayerSpawnPosition;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < 0.001f)
        {
            lookDirection = Vector3.forward;
        }

        Quaternion playerRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

        PlayerDummyMove dummyMove = player.GetComponent<PlayerDummyMove>();

        if (dummyMove != null)
        {
            dummyMove.TeleportTo(safePlayerSpawnPosition, playerRotation);
        }
        else
        {
            player.SetPositionAndRotation(safePlayerSpawnPosition, playerRotation);
        }

        CameraFollowTarget cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollowTarget>() : null;

        if (cameraFollow != null)
        {
            cameraFollow.SnapToTarget();
        }
    }

    private Vector3 GetSafeBossPlayerSpawnPosition()
    {
        Vector3[] offsets =
        {
            Vector3.zero,
            Vector3.forward * 2.5f,
            Vector3.right * 2.5f,
            Vector3.left * 2.5f,
            Vector3.forward * 2.5f + Vector3.right * 2.5f,
            Vector3.forward * 2.5f + Vector3.left * 2.5f,
            Vector3.back * 2.5f
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 candidate = bossPlayerSpawnPosition + offsets[i];
            if (!IsBossPlayerSpawnBlocked(candidate))
            {
                return candidate;
            }
        }

        return bossPlayerSpawnPosition + Vector3.forward * 3f;
    }

    private bool IsBossPlayerSpawnBlocked(Vector3 position)
    {
        float radius = 0.55f;
        float height = 1.9f;

        CapsuleCollider playerCollider = player != null ? player.GetComponent<CapsuleCollider>() : null;
        if (playerCollider != null)
        {
            radius = Mathf.Max(0.25f, playerCollider.radius * Mathf.Max(player.lossyScale.x, player.lossyScale.z));
            height = Mathf.Max(radius * 2f + 0.1f, playerCollider.height * player.lossyScale.y);
        }

        Vector3 center = position + Vector3.up * (height * 0.5f);
        float halfLine = Mathf.Max(0f, height * 0.5f - radius);
        Vector3 point1 = center + Vector3.up * halfLine;
        Vector3 point2 = center - Vector3.up * halfLine;

        Collider[] hits = Physics.OverlapCapsule(point1, point2, radius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];

            if (ShouldIgnoreBossSpawnBlocker(hit))
                continue;

            return true;
        }

        return false;
    }

    private bool ShouldIgnoreBossSpawnBlocker(Collider hit)
    {
        if (hit == null || hit.isTrigger)
            return true;

        if (player != null && hit.transform.IsChildOf(player))
            return true;

        if (hit.GetComponentInParent<PlayerHealth>() != null)
            return true;

        if (hit.GetComponentInParent<EnemyHealth>() != null)
            return true;

        if (hit.CompareTag("Ground"))
            return true;

        string objectName = hit.name;
        return objectName.Contains("Ground") || objectName.Contains("Floor") || objectName.Contains("Projectile");
    }

    private void FindPlayer()
    {
        if (player != null)
            return;

        PlayerLevel playerLevel = FindFirstObjectByType<PlayerLevel>();

        if (playerLevel != null)
        {
            player = playerLevel.transform;
        }
    }

    private Vector3 GetFlatForward(Transform source)
    {
        Camera camera = Camera.main;
        Vector3 forward = camera != null ? camera.transform.forward : source.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = source.forward;
            forward.y = 0f;
        }

        return forward.normalized;
    }

    private void NotifyProgressChanged()
    {
        ProgressChanged?.Invoke(killCount, killsRequiredForPortal, portalOpened);
    }
}
