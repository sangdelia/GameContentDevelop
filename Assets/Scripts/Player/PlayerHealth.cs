using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Defense")]
    [SerializeField] private float flatDamageReduction = 0f;
    [SerializeField] private float healthRegenPerSecond = 0f;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float FlatDamageReduction => flatDamageReduction;
    public float HealthRegenPerSecond => healthRegenPerSecond;
    public bool IsDead => currentHealth <= 0f;

    public event System.Action<float, float> HealthChanged;
    public event System.Action Died;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    private void Start()
    {
        NotifyChanged();
    }

    private void Update()
    {
        if (IsDead || healthRegenPerSecond <= 0f || currentHealth >= maxHealth)
            return;

        Heal(healthRegenPerSecond * Time.deltaTime);
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        float reducedDamage = Mathf.Max(1f, damage - flatDamageReduction);
        currentHealth = Mathf.Max(0f, currentHealth - reducedDamage);
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

    private void NotifyChanged()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
