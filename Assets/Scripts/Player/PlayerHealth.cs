using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Defense")]
    [SerializeField] private float flatDamageReduction = 0f;
    [SerializeField] private float healthRegenPerSecond = 0f;
    [SerializeField] private float healOnKill = 0f;
    [SerializeField] private float shieldRechargeSeconds = 0f;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float FlatDamageReduction => flatDamageReduction;
    public float HealthRegenPerSecond => healthRegenPerSecond;
    public float HealOnKill => healOnKill;
    public bool HasShieldReady => shieldRechargeSeconds > 0f && shieldReady;
    public bool IsDead => currentHealth <= 0f;

    public event System.Action<float, float> HealthChanged;
    public event System.Action<float> Damaged;
    public event System.Action Died;

    private bool shieldReady;
    private float shieldRechargeTimer;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    private void Start()
    {
        NotifyChanged();
    }

    private void OnEnable()
    {
        EnemyHealth.EnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        EnemyHealth.EnemyKilled -= HandleEnemyKilled;
    }

    private void Update()
    {
        RechargeShield();

        if (!IsDead && healthRegenPerSecond > 0f && currentHealth < maxHealth)
        {
            Heal(healthRegenPerSecond * Time.deltaTime);
        }
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        if (shieldReady)
        {
            shieldReady = false;
            shieldRechargeTimer = shieldRechargeSeconds;
            GameVfx.SpawnShieldBlock(transform.position);
            NotifyChanged();
            return;
        }

        float reducedDamage = Mathf.Max(1f, damage - flatDamageReduction);
        currentHealth = Mathf.Max(0f, currentHealth - reducedDamage);
        Damaged?.Invoke(reducedDamage);
        GameVfx.SpawnHitSpark(transform.position + Vector3.up * 0.8f, -transform.forward, false);
        NotifyChanged();

        if (IsDead)
        {
            Died?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        NotifyChanged();
    }

    public void AddMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth += amount;
        NotifyChanged();
    }

    public void AddFlatDamageReduction(float amount)
    {
        flatDamageReduction += amount;
    }

    public void AddHealthRegen(float amount)
    {
        healthRegenPerSecond += amount;
    }

    public void AddHealOnKill(float amount)
    {
        healOnKill += amount;
    }

    public void ImproveRechargeShield(float baseRechargeSeconds, float cooldownReduction)
    {
        if (shieldRechargeSeconds <= 0f)
        {
            shieldRechargeSeconds = baseRechargeSeconds;
            shieldReady = true;
            shieldRechargeTimer = 0f;
            NotifyChanged();
            return;
        }

        shieldRechargeSeconds = Mathf.Max(6f, shieldRechargeSeconds - cooldownReduction);
    }

    private void RechargeShield()
    {
        if (shieldRechargeSeconds <= 0f || shieldReady)
            return;

        shieldRechargeTimer -= Time.deltaTime;

        if (shieldRechargeTimer <= 0f)
        {
            shieldReady = true;
            shieldRechargeTimer = 0f;
            NotifyChanged();
        }
    }

    private void HandleEnemyKilled(EnemyHealth enemy)
    {
        if (healOnKill <= 0f || IsDead || enemy == null || enemy.GetComponent<TempBossController>() != null)
            return;

        Heal(healOnKill);
    }

    private void NotifyChanged()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
