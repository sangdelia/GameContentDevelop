using UnityEngine;

public class PlayerCombatStats : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float flatDamageBonus;
    [SerializeField] private float damageMultiplier = 1f;

    [Header("Attack Speed")]
    [SerializeField] private float flatAttackSpeedBonus;
    [SerializeField] private float attackSpeedMultiplier = 1f;
    [SerializeField] private float minFireInterval = 0.08f;

    public float DamageMultiplier => damageMultiplier;
    public float AttackSpeedMultiplier => attackSpeedMultiplier;
    public float MinFireInterval => minFireInterval;

    public float GetFinalDamage(float baseDamage)
    {
        return Mathf.Max(0f, (baseDamage + flatDamageBonus) * damageMultiplier);
    }

    public float GetFinalShotsPerSecond(float baseShotsPerSecond)
    {
        float finalShots = Mathf.Max(0.1f, (baseShotsPerSecond + flatAttackSpeedBonus) * attackSpeedMultiplier);
        float maxShotsFromMinInterval = 1f / Mathf.Max(0.01f, minFireInterval);
        return Mathf.Min(finalShots, maxShotsFromMinInterval);
    }

    public void SetAttackSpeedMultiplier(float multiplier)
    {
        attackSpeedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = Mathf.Max(0f, multiplier);
    }
}
