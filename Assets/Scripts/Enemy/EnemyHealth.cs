using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public static event System.Action<EnemyHealth> EnemyKilled;

    [Header("Health")]
    [SerializeField] private float maxHp = 30f;

    [Header("Drop")]
    [SerializeField] private GameObject expOrbPrefab;
    [SerializeField] private int expDropCount = 1;

    [Header("Debug")]
    [SerializeField] private bool logDamage = false;
    [SerializeField] private float destroyDelay;

    private float currentHp;
    private bool isDead;

    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public bool IsDead => isDead;

    public event System.Action<float, Vector3, Vector3> Damaged;
    public event System.Action<float, float> HealthChanged;
    public event System.Action<EnemyHealth> Died;

    private void Awake()
    {
        currentHp = maxHp;
    }

    public void Configure(float hp, GameObject dropPrefab, int dropCount)
    {
        maxHp = hp;
        currentHp = maxHp;
        expOrbPrefab = dropPrefab;
        expDropCount = dropCount;
        isDead = false;
        HealthChanged?.Invoke(currentHp, maxHp);
    }

    public void SetMaxHp(float hp)
    {
        maxHp = hp;
        currentHp = maxHp;
        isDead = false;
        HealthChanged?.Invoke(currentHp, maxHp);
    }

    public void SetDestroyDelay(float delay)
    {
        destroyDelay = Mathf.Max(0f, delay);
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position + Vector3.up * 0.8f, -transform.forward);
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        DamageInfo info = new DamageInfo(damage, null, DamageType.Direct, hitPoint, hitDirection);
        TakeDamage(info);
    }

    public void TakeDamage(DamageInfo info)
    {
        if (isDead)
            return;

        float damage = Mathf.Max(0f, info.Damage);
        currentHp -= damage;
        currentHp = Mathf.Max(0f, currentHp);
        Damaged?.Invoke(damage, info.HitPoint, info.HitDirection);
        HealthChanged?.Invoke(currentHp, maxHp);
        GameAudio.PlayHit(transform.position);

        if (logDamage)
        {
            Debug.Log($"{name} HP: {currentHp}/{maxHp}");
        }

        if (currentHp <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        GameVfx.SpawnEnemyDeathBurst(transform.position);
        GameAudio.PlayEnemyDie(transform.position);
        DropExp();
        Died?.Invoke(this);
        EnemyKilled?.Invoke(this);

        Destroy(gameObject, destroyDelay);
    }

    private void DropExp()
    {
        if (expOrbPrefab == null)
            return;

        for (int i = 0; i < expDropCount; i++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                0.3f,
                Random.Range(-0.5f, 0.5f)
            );

            Instantiate(
                expOrbPrefab,
                transform.position + randomOffset,
                Quaternion.identity
            );
        }
    }
}
