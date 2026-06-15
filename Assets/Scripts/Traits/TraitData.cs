using System.Collections.Generic;
using UnityEngine;

public enum TraitCategory
{
    ProjectileModifier,
    StatBuff,
    Aura,
    Special
}

public enum TraitRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

public enum TraitEffectKind
{
    FireProjectile,
    IceProjectile,
    Damage,
    AttackSpeed,
    MoveSpeed,
    Magnet,
    MaxHealth,
    Armor,
    HealthRegen,
    HealOnKill,
    Shield,
    SlowAura,
    InstantKill
}

[System.Serializable]
public class TraitLevelData
{
    public float value;
    public float secondaryValue;
    public float duration;
    public float probability;
    public float radius;
    public int maxStacks = 1;
}

[CreateAssetMenu(menuName = "Stargrave/Traits/Trait Data", fileName = "TraitData")]
public class TraitData : ScriptableObject
{
    public string traitId;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public TraitCategory category;
    public TraitRarity rarity;
    public TraitEffectKind effectKind;
    public int maxLevel = 1;
    public List<TraitLevelData> levels = new List<TraitLevelData>();

    public TraitLevelData GetLevelData(int level)
    {
        if (levels == null || levels.Count == 0)
            return null;

        int index = Mathf.Clamp(level - 1, 0, levels.Count - 1);
        return levels[index];
    }
}
