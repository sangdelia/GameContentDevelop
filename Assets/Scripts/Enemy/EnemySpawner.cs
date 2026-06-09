using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("View Source")]
    [SerializeField] private Camera viewCamera;

    [Header("Enemy")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform enemyRoot;

    [Header("Enemy Types")]
    [Range(0f, 1f)]
    [SerializeField] private float rangedEnemyChance = 0.25f;
    [Range(0f, 1f)]
    [SerializeField] private float flyingRangedEnemyChance = 0.15f;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 1.25f;
    [SerializeField] private float minSpawnDistance = 22f;
    [SerializeField] private float maxSpawnDistance = 34f;
    [SerializeField] private int maxEnemies = 36;

    [Header("Difficulty Ramp")]
    [SerializeField] private bool scaleDifficultyOverTime = true;
    [SerializeField] private float difficultyStepSeconds = 30f;
    [SerializeField] private float spawnIntervalDecreasePerStep = 0.12f;
    [SerializeField] private float minSpawnInterval = 0.65f;
    [SerializeField] private int maxEnemiesIncreasePerStep = 5;
    [SerializeField] private int maxEnemyLimit = 70;
    [SerializeField] private float enemyHpIncreasePerStep = 0.1f;

    [Header("Melee Stats")]
    [SerializeField] private float meleeHp = 30f;
    [SerializeField] private float meleeMoveSpeed = 2.25f;
    [SerializeField] private float meleeDamage = 8f;
    [SerializeField] private float meleeAttackInterval = 1.1f;

    [Header("Ranged Stats")]
    [SerializeField] private float rangedHp = 24f;
    [SerializeField] private float rangedMoveSpeed = 1.85f;
    [SerializeField] private float rangedDamage = 7f;
    [SerializeField] private float rangedAttackInterval = 2.35f;

    [Header("Flying Stats")]
    [SerializeField] private float flyingHp = 18f;
    [SerializeField] private float flyingMoveSpeed = 2.65f;
    [SerializeField] private float flyingDamage = 6f;
    [SerializeField] private float flyingAttackInterval = 2f;

    [Header("View Based Spawn")]
    [Range(0f, 1f)]
    [SerializeField] private float frontSpawnChance = 0.85f;

    [SerializeField] private float frontAngle = 180f;
    [SerializeField] private float backAngle = 180f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayStartHeight = 20f;
    [SerializeField] private float rayDistance = 50f;
    [SerializeField] private float enemyHalfHeight = 1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;
    [SerializeField] private bool showDebugRay = false;

    private float timer;
    private float spawnStartTime;
    private Vector3 currentViewForward;

    private void Awake()
    {
        spawnStartTime = Time.time;

        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }
    }

    private void Update()
    {
        UpdateViewDirection();

        if (player == null || enemyPrefab == null)
            return;

        timer += Time.deltaTime;

        if (timer >= GetCurrentSpawnInterval())
        {
            timer = 0f;

            if (CountEnemies() < GetCurrentMaxEnemies())
            {
                TrySpawnEnemy();
            }
        }
    }

    private void UpdateViewDirection()
    {
        Transform source = null;

        if (viewCamera != null)
        {
            source = viewCamera.transform;
        }
        else if (Camera.main != null)
        {
            source = Camera.main.transform;
        }
        else if (player != null)
        {
            source = player;
        }

        if (source == null)
            return;

        currentViewForward = source.forward;
        currentViewForward.y = 0f;

        if (currentViewForward.sqrMagnitude < 0.001f && player != null)
        {
            currentViewForward = player.forward;
            currentViewForward.y = 0f;
        }

        currentViewForward.Normalize();

        if (showDebugRay && player != null)
        {
            Debug.DrawRay(player.position + Vector3.up * 1.5f, currentViewForward * 10f, Color.cyan);
        }
    }

    private void TrySpawnEnemy()
    {
        for (int i = 0; i < 20; i++)
        {
            if (TryGetSpawnPosition(out Vector3 spawnPosition, out float usedAngle, out bool isFront))
            {
                GameObject enemy = Instantiate(
                    enemyPrefab,
                    spawnPosition,
                    Quaternion.identity,
                    enemyRoot
                );

                SetupSpawnedEnemy(enemy);

                if (showDebugLog)
                {
                    string area = isFront ? "front" : "back";
                    Debug.Log($"Enemy spawned / source: camera forward / area: {area} / angle: {usedAngle:F1}");
                }

                return;
            }
        }

        if (showDebugLog)
        {
            Debug.LogWarning("Enemy spawn failed: no valid ground position found.");
        }
    }

    private void SetupSpawnedEnemy(GameObject enemy)
    {
        float typeRoll = Random.value;
        bool makeFlyingRanged = typeRoll < flyingRangedEnemyChance;
        bool makeRanged = !makeFlyingRanged && typeRoll < flyingRangedEnemyChance + rangedEnemyChance;

        if (makeFlyingRanged)
        {
            EnemyMoveToPlayer meleeMover = enemy.GetComponent<EnemyMoveToPlayer>();
            if (meleeMover != null)
            {
                Destroy(meleeMover);
            }

            EnemyFlyingRangedAttack flyingAttack = enemy.AddComponent<EnemyFlyingRangedAttack>();
            flyingAttack.Configure(flyingMoveSpeed, flyingAttackInterval, flyingDamage, 13f, 4.2f);
            flyingAttack.Init(player);
            ConfigureHealth(enemy, flyingHp);
            enemy.name = "Enemy_Flying_Drone";
            enemy.transform.position += Vector3.up * 3.2f;
            enemy.transform.localScale = Vector3.one * 0.7f;
            ApplyEnemyColor(enemy, new Color(1f, 0.25f, 0.95f));
            EnemyVisual.Attach(enemy, EnemyVisual.EnemyVisualType.Flying);
            return;
        }

        if (makeRanged)
        {
            EnemyMoveToPlayer meleeMover = enemy.GetComponent<EnemyMoveToPlayer>();
            if (meleeMover != null)
            {
                Destroy(meleeMover);
            }

            EnemyRangedAttack rangedAttack = enemy.AddComponent<EnemyRangedAttack>();
            rangedAttack.Configure(rangedMoveSpeed, rangedAttackInterval, rangedDamage, 11f, 6f);
            rangedAttack.Init(player);
            ConfigureHealth(enemy, rangedHp);
            enemy.name = "Enemy_Ranged_Gunner";
            ApplyEnemyColor(enemy, new Color(0.1f, 0.85f, 1f));
            enemy.transform.localScale = Vector3.one * 0.9f;
            EnemyVisual.Attach(enemy, EnemyVisual.EnemyVisualType.Ranged);
            return;
        }

        EnemyMoveToPlayer mover = enemy.GetComponent<EnemyMoveToPlayer>();

        if (mover != null)
        {
            mover.Configure(meleeMoveSpeed, meleeDamage, meleeAttackInterval, 1.25f);
            mover.Init(player);
        }

        ConfigureHealth(enemy, meleeHp);
        enemy.name = "Enemy_Melee_Chaser";
        EnemyVisual.Attach(enemy, EnemyVisual.EnemyVisualType.Melee);
    }

    private void ConfigureHealth(GameObject enemy, float baseHp)
    {
        EnemyHealth health = enemy.GetComponent<EnemyHealth>();

        if (health == null)
            return;

        health.SetMaxHp(baseHp * GetCurrentEnemyHpMultiplier());
    }

    private void ApplyEnemyColor(GameObject enemy, Color color)
    {
        Renderer renderer = enemy.GetComponentInChildren<Renderer>();

        if (renderer == null)
            return;

        renderer.material = new Material(renderer.material);
        renderer.material.color = color;
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", color * 0.7f);
    }

    private bool TryGetSpawnPosition(out Vector3 spawnPosition, out float usedAngle, out bool isFront)
    {
        spawnPosition = Vector3.zero;

        Vector3 direction = GetViewBasedDirection(out usedAngle, out isFront);

        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);

        Vector3 candidate = player.position + direction * distance;
        Vector3 rayOrigin = candidate + Vector3.up * rayStartHeight;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, groundLayer))
        {
            return false;
        }

        spawnPosition = hit.point + Vector3.up * enemyHalfHeight;

        if (showDebugRay)
        {
            Debug.DrawLine(player.position + Vector3.up * 1.2f, spawnPosition, isFront ? Color.green : Color.red, 1.5f);
        }

        return true;
    }

    private Vector3 GetViewBasedDirection(out float usedAngle, out bool isFront)
    {
        isFront = Random.value <= frontSpawnChance;

        if (isFront)
        {
            usedAngle = Random.Range(-frontAngle * 0.5f, frontAngle * 0.5f);
        }
        else
        {
            float halfBack = backAngle * 0.5f;
            usedAngle = Random.Range(180f - halfBack, 180f + halfBack);
        }

        Quaternion rotation = Quaternion.Euler(0f, usedAngle, 0f);

        Vector3 direction = rotation * currentViewForward;
        direction.y = 0f;
        direction.Normalize();

        return direction;
    }

    private int CountEnemies()
    {
        return FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Length;
    }

    private int GetDifficultyStep()
    {
        if (!scaleDifficultyOverTime || difficultyStepSeconds <= 0f)
            return 0;

        return Mathf.FloorToInt((Time.time - spawnStartTime) / difficultyStepSeconds);
    }

    private float GetCurrentSpawnInterval()
    {
        float interval = spawnInterval - GetDifficultyStep() * spawnIntervalDecreasePerStep;
        return Mathf.Max(minSpawnInterval, interval);
    }

    private int GetCurrentMaxEnemies()
    {
        int scaledMax = maxEnemies + GetDifficultyStep() * maxEnemiesIncreasePerStep;
        return Mathf.Min(maxEnemyLimit, scaledMax);
    }

    private float GetCurrentEnemyHpMultiplier()
    {
        return 1f + GetDifficultyStep() * enemyHpIncreasePerStep;
    }
}
