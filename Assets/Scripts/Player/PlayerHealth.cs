using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
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

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
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

    private void NotifyChanged()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
