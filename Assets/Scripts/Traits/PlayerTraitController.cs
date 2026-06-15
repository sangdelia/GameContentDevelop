using System.Collections.Generic;
using UnityEngine;

public enum PlayerProjectileElement
{
    Normal,
    Fire,
    Ice
}

[RequireComponent(typeof(PlayerCombatStats))]
public class PlayerTraitController : MonoBehaviour
{
    [SerializeField] private List<TraitData> traitCatalog = new List<TraitData>();
    [SerializeField] private int choiceCount = 3;

    private readonly Dictionary<string, int> traitLevels = new Dictionary<string, int>();
    private PlayerCombatStats stats;
    private PlayerAuraController auraController;
    private PlayerHealth playerHealth;
    private PlayerDummyMove pcMove;
    private SimpleQuest2VrRig vrMove;
    private TraitData activeProjectileTrait;
    private TraitData instantKillTrait;

    public IReadOnlyDictionary<string, int> TraitLevels => traitLevels;
    public PlayerProjectileElement CurrentProjectileElement { get; private set; } = PlayerProjectileElement.Normal;

    private void Awake()
    {
        stats = GetComponent<PlayerCombatStats>();
        if (stats == null)
        {
            stats = gameObject.AddComponent<PlayerCombatStats>();
        }

        auraController = GetComponent<PlayerAuraController>();
        if (auraController == null)
        {
            auraController = gameObject.AddComponent<PlayerAuraController>();
        }

        playerHealth = GetComponent<PlayerHealth>();
        pcMove = GetComponent<PlayerDummyMove>();
        vrMove = GetComponent<SimpleQuest2VrRig>();

        EnsureDefaultCatalog();
    }

    public List<TraitChoiceView> RollChoices()
    {
        EnsureDefaultCatalog();

        List<TraitData> pool = new List<TraitData>();
        for (int i = 0; i < traitCatalog.Count; i++)
        {
            TraitData trait = traitCatalog[i];
            if (trait != null && GetTraitLevel(trait) < trait.maxLevel)
            {
                pool.Add(trait);
            }
        }

        List<TraitChoiceView> choices = new List<TraitChoiceView>();
        int count = Mathf.Min(choiceCount, pool.Count);
        for (int i = 0; i < count; i++)
        {
            int selectedIndex = Random.Range(0, pool.Count);
            TraitData trait = pool[selectedIndex];
            pool.RemoveAt(selectedIndex);
            choices.Add(new TraitChoiceView(trait, GetTraitLevel(trait)));
        }

        return choices;
    }

    public void ApplyTrait(TraitData trait)
    {
        if (trait == null)
            return;

        int nextLevel = Mathf.Clamp(GetTraitLevel(trait) + 1, 1, trait.maxLevel);
        traitLevels[trait.traitId] = nextLevel;
        TraitLevelData levelData = trait.GetLevelData(nextLevel);

        switch (trait.effectKind)
        {
            case TraitEffectKind.FireProjectile:
                activeProjectileTrait = trait;
                CurrentProjectileElement = PlayerProjectileElement.Fire;
                break;
            case TraitEffectKind.IceProjectile:
                activeProjectileTrait = trait;
                CurrentProjectileElement = PlayerProjectileElement.Ice;
                break;
            case TraitEffectKind.Damage:
                if (levelData != null)
                {
                    stats.SetDamageMultiplier(1f + levelData.value);
                }
                break;
            case TraitEffectKind.AttackSpeed:
                if (levelData != null)
                {
                    stats.SetAttackSpeedMultiplier(1f + levelData.value);
                }
                break;
            case TraitEffectKind.MoveSpeed:
                ApplyMoveSpeed(levelData);
                break;
            case TraitEffectKind.Magnet:
                if (levelData != null)
                {
                    ExpOrb.AddGlobalAttractBonus(levelData.value);
                }
                break;
            case TraitEffectKind.MaxHealth:
                if (playerHealth != null && levelData != null)
                {
                    playerHealth.AddMaxHealth(levelData.value);
                }
                break;
            case TraitEffectKind.Armor:
                if (playerHealth != null && levelData != null)
                {
                    playerHealth.AddFlatDamageReduction(levelData.value);
                }
                break;
            case TraitEffectKind.HealthRegen:
                if (playerHealth != null && levelData != null)
                {
                    playerHealth.AddHealthRegen(levelData.value);
                }
                break;
            case TraitEffectKind.HealOnKill:
                if (playerHealth != null && levelData != null)
                {
                    playerHealth.AddHealOnKill(levelData.value);
                }
                break;
            case TraitEffectKind.Shield:
                if (playerHealth != null && levelData != null)
                {
                    playerHealth.ImproveRechargeShield(levelData.value, levelData.secondaryValue);
                }
                break;
            case TraitEffectKind.SlowAura:
                if (levelData != null)
                {
                    auraController.ConfigureSlowAura(levelData.radius, levelData.value, levelData.secondaryValue, Mathf.Max(0.05f, levelData.duration));
                }
                break;
            case TraitEffectKind.InstantKill:
                instantKillTrait = trait;
                break;
        }
    }

    public float GetFinalDamage(float baseDamage)
    {
        return stats != null ? stats.GetFinalDamage(baseDamage) : baseDamage;
    }

    public float GetFinalShotsPerSecond(float baseShotsPerSecond)
    {
        return stats != null ? stats.GetFinalShotsPerSecond(baseShotsPerSecond) : baseShotsPerSecond;
    }

    public Color GetProjectileRayColor()
    {
        switch (CurrentProjectileElement)
        {
            case PlayerProjectileElement.Fire:
                return new Color(1f, 0.05f, 0.01f);
            case PlayerProjectileElement.Ice:
                return new Color(0.25f, 0.85f, 1f);
            default:
                return new Color(1f, 0.82f, 0.18f);
        }
    }

    public Color GetProjectileMuzzleColor()
    {
        switch (CurrentProjectileElement)
        {
            case PlayerProjectileElement.Fire:
                return new Color(1f, 0.18f, 0.02f);
            case PlayerProjectileElement.Ice:
                return new Color(0.35f, 0.95f, 1f);
            default:
                return new Color(1f, 0.72f, 0.18f);
        }
    }

    public bool HasFireProjectileTrait()
    {
        return CurrentProjectileElement == PlayerProjectileElement.Fire && activeProjectileTrait != null;
    }

    public bool HasIceProjectileTrait()
    {
        return CurrentProjectileElement == PlayerProjectileElement.Ice && activeProjectileTrait != null;
    }

    public void HandleDirectEnemyHit(EnemyHealth enemy, Vector3 hitPoint, Vector3 hitDirection, float directDamage)
    {
        if (enemy == null)
            return;

        if (CurrentProjectileElement == PlayerProjectileElement.Fire)
        {
            ApplyFireTrait(enemy, hitPoint, hitDirection, directDamage);
        }
        else if (CurrentProjectileElement == PlayerProjectileElement.Ice)
        {
            ApplyIceTrait(enemy);
        }

        TryInstantKill(enemy, hitPoint, hitDirection);
    }

    public string GetChoiceDescription(TraitData trait)
    {
        if (trait == null)
            return string.Empty;

        int nextLevel = Mathf.Clamp(GetTraitLevel(trait) + 1, 1, trait.maxLevel);
        TraitLevelData data = trait.GetLevelData(nextLevel);
        if (data == null)
            return GetLocalizedDescription(trait);

        switch (trait.effectKind)
        {
            case TraitEffectKind.FireProjectile:
                return $"탄환 속성: 화염. {data.secondaryValue:0.##}초마다 피해량 {Mathf.RoundToInt(data.value * 100f)}%, {data.duration:0.#}초 지속";
            case TraitEffectKind.IceProjectile:
                return $"탄환 속성: 얼음. 명중한 적 이동속도 {Mathf.RoundToInt(data.value * 100f)}% 감소, {data.duration:0.#}초 지속";
            case TraitEffectKind.Damage:
                return $"무기 피해 +{Mathf.RoundToInt(data.value * 100f)}%";
            case TraitEffectKind.AttackSpeed:
                return $"공격 속도 +{Mathf.RoundToInt(data.value * 100f)}%";
            case TraitEffectKind.MoveSpeed:
                return $"이동 속도 +{Mathf.RoundToInt(data.value * 100f)}%";
            case TraitEffectKind.Magnet:
                return $"경험치 자석 범위 +{data.value:0.#}m";
            case TraitEffectKind.MaxHealth:
                return $"최대 체력 +{data.value:0}";
            case TraitEffectKind.Armor:
                return $"받는 피해 고정 감소 +{data.value:0.#}";
            case TraitEffectKind.HealthRegen:
                return $"초당 체력 재생 +{data.value:0.#}";
            case TraitEffectKind.HealOnKill:
                return $"일반 적 처치 시 체력 +{data.value:0.#}";
            case TraitEffectKind.Shield:
                return $"보호막 획득/강화. 재충전 {data.value:0.#}초, 강화 시 -{data.secondaryValue:0.#}초";
            case TraitEffectKind.SlowAura:
                return $"주변 {data.radius:0.#}m 적 이동속도 {Mathf.RoundToInt(data.value * 100f)}% 감소";
            case TraitEffectKind.InstantKill:
                return $"일반 적 명중 시 {Mathf.RoundToInt(data.probability * 100f)}% 확률로 즉사";
            default:
                return GetLocalizedDescription(trait);
        }
    }

    public string GetSummary()
    {
        if (traitLevels.Count == 0)
            return "능력: 없음";

        string summary = "능력:";
        for (int i = 0; i < traitCatalog.Count; i++)
        {
            TraitData trait = traitCatalog[i];
            if (trait == null)
                continue;

            int level = GetTraitLevel(trait);
            if (level > 0)
            {
                summary += $" {GetLocalizedDisplayName(trait)} {level}/{trait.maxLevel}";
            }
        }

        return summary;
    }

    public string GetDisplayName(TraitData trait)
    {
        return GetLocalizedDisplayName(trait);
    }

    public string GetBaseDescription(TraitData trait)
    {
        return GetLocalizedDescription(trait);
    }

    public int GetTraitLevel(TraitData trait)
    {
        if (trait == null || string.IsNullOrEmpty(trait.traitId))
            return 0;

        return traitLevels.TryGetValue(trait.traitId, out int level) ? level : 0;
    }

    private void ApplyMoveSpeed(TraitLevelData levelData)
    {
        if (levelData == null)
            return;

        float multiplier = 1f + levelData.value;
        if (pcMove != null)
        {
            pcMove.AddMoveSpeedMultiplier(multiplier);
        }

        if (vrMove != null)
        {
            vrMove.AddMoveSpeedMultiplier(multiplier);
        }
    }

    private void ApplyFireTrait(EnemyHealth enemy, Vector3 hitPoint, Vector3 hitDirection, float directDamage)
    {
        if (activeProjectileTrait == null)
            return;

        int level = GetTraitLevel(activeProjectileTrait);
        TraitLevelData data = activeProjectileTrait.GetLevelData(level);
        if (data == null)
            return;

        StatusEffectController status = GetOrCreateStatus(enemy);
        float tickDamage = Mathf.Max(0f, directDamage * data.value);
        status.ApplyBurn("trait_fire_projectile", gameObject, data.duration, data.secondaryValue, tickDamage, Mathf.Max(1, data.maxStacks), hitPoint, hitDirection);
    }

    private void ApplyIceTrait(EnemyHealth enemy)
    {
        if (activeProjectileTrait == null)
            return;

        int level = GetTraitLevel(activeProjectileTrait);
        TraitLevelData data = activeProjectileTrait.GetLevelData(level);
        if (data == null)
            return;

        StatusEffectController status = GetOrCreateStatus(enemy);
        float speedMultiplier = Mathf.Clamp01(1f - Mathf.Clamp01(data.value));
        status.ApplySlow("trait_ice_projectile", data.duration, speedMultiplier);
    }

    private StatusEffectController GetOrCreateStatus(EnemyHealth enemy)
    {
        StatusEffectController status = enemy.GetComponent<StatusEffectController>();
        if (status == null)
        {
            status = enemy.gameObject.AddComponent<StatusEffectController>();
        }

        return status;
    }

    private void TryInstantKill(EnemyHealth enemy, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (instantKillTrait == null || enemy.IsDead)
            return;

        if (enemy.GetComponent<TempBossController>() != null)
            return;

        int level = GetTraitLevel(instantKillTrait);
        TraitLevelData data = instantKillTrait.GetLevelData(level);
        if (data == null || Random.value > data.probability)
            return;

        GameVfx.SpawnEnemyDeathBurst(hitPoint);
        DamageInfo info = new DamageInfo(enemy.CurrentHp + enemy.MaxHp + 1f, gameObject, DamageType.InstantDeath, hitPoint, hitDirection);
        enemy.TakeDamage(info);
    }

    private void EnsureDefaultCatalog()
    {
        if (traitCatalog.Count > 0)
            return;

        traitCatalog.Add(CreateRuntimeTrait("fire_projectile", "화염탄", "탄환 속성을 화염으로 교체합니다.", TraitCategory.ProjectileModifier, TraitRarity.Common, TraitEffectKind.FireProjectile, 3,
            Level(0.10f, 0.5f, 3f, 0f, 0f, 3),
            Level(0.15f, 0.5f, 3.5f, 0f, 0f, 3),
            Level(0.20f, 0.5f, 3.5f, 0f, 0f, 5)));

        traitCatalog.Add(CreateRuntimeTrait("ice_projectile", "빙결탄", "탄환 속성을 얼음으로 교체합니다.", TraitCategory.ProjectileModifier, TraitRarity.Common, TraitEffectKind.IceProjectile, 3,
            Level(0.28f, 0f, 2.5f),
            Level(0.38f, 0f, 3f),
            Level(0.50f, 0f, 3.5f)));

        traitCatalog.Add(CreateRuntimeTrait("damage", "과충전 탄약", "무기 피해량이 증가합니다.", TraitCategory.StatBuff, TraitRarity.Common, TraitEffectKind.Damage, 5,
            Level(0.15f), Level(0.30f), Level(0.45f), Level(0.60f), Level(0.80f)));

        traitCatalog.Add(CreateRuntimeTrait("attack_speed", "고속 약실", "무기의 연사 속도가 증가합니다.", TraitCategory.StatBuff, TraitRarity.Common, TraitEffectKind.AttackSpeed, 5,
            Level(0.10f), Level(0.20f), Level(0.35f), Level(0.50f), Level(0.70f)));

        traitCatalog.Add(CreateRuntimeTrait("move_speed", "전투 자극제", "이동 속도가 증가합니다.", TraitCategory.StatBuff, TraitRarity.Common, TraitEffectKind.MoveSpeed, 5,
            Level(0.08f), Level(0.16f), Level(0.25f), Level(0.35f), Level(0.48f)));

        traitCatalog.Add(CreateRuntimeTrait("magnet", "중력 수집기", "경험치 흡입 범위가 증가합니다.", TraitCategory.StatBuff, TraitRarity.Common, TraitEffectKind.Magnet, 5,
            Level(6f), Level(8f), Level(10f), Level(12f), Level(15f)));

        traitCatalog.Add(CreateRuntimeTrait("max_health", "강화 생체장갑", "최대 체력이 증가합니다.", TraitCategory.StatBuff, TraitRarity.Common, TraitEffectKind.MaxHealth, 4,
            Level(20f), Level(25f), Level(30f), Level(40f)));

        traitCatalog.Add(CreateRuntimeTrait("armor", "도금 전투복", "받는 피해가 고정 감소합니다.", TraitCategory.StatBuff, TraitRarity.Rare, TraitEffectKind.Armor, 4,
            Level(1.5f), Level(2f), Level(2.5f), Level(3f)));

        traitCatalog.Add(CreateRuntimeTrait("health_regen", "자동 수복 젤", "체력이 천천히 재생됩니다.", TraitCategory.StatBuff, TraitRarity.Rare, TraitEffectKind.HealthRegen, 5,
            Level(0.5f), Level(0.8f), Level(1.1f), Level(1.5f), Level(2f)));

        traitCatalog.Add(CreateRuntimeTrait("heal_on_kill", "흡혈 매트릭스", "적 처치 시 체력을 회복합니다.", TraitCategory.StatBuff, TraitRarity.Rare, TraitEffectKind.HealOnKill, 5,
            Level(2f), Level(3f), Level(4f), Level(5f), Level(7f)));

        traitCatalog.Add(CreateRuntimeTrait("shield", "위상 보호막", "피격 1회를 막는 보호막을 얻거나 강화합니다.", TraitCategory.StatBuff, TraitRarity.Epic, TraitEffectKind.Shield, 4,
            Level(18f, 3f), Level(18f, 3f), Level(18f, 3f), Level(18f, 3f)));

        traitCatalog.Add(CreateRuntimeTrait("slow_aura", "중력 덫", "주변 적의 이동 속도를 늦춥니다.", TraitCategory.Aura, TraitRarity.Rare, TraitEffectKind.SlowAura, 1,
            Level(0.25f, 0.10f, 0.2f, 0f, 4f, 1)));

        traitCatalog.Add(CreateRuntimeTrait("instant_kill", "처형 탄두", "일반 적을 일정 확률로 즉시 처치합니다.", TraitCategory.Special, TraitRarity.Epic, TraitEffectKind.InstantKill, 5,
            Level(0f, 0f, 0f, 0.01f),
            Level(0f, 0f, 0f, 0.02f),
            Level(0f, 0f, 0f, 0.03f),
            Level(0f, 0f, 0f, 0.04f),
            Level(0f, 0f, 0f, 0.05f)));
    }

    private static TraitLevelData Level(float value, float secondaryValue = 0f, float duration = 0f, float probability = 0f, float radius = 0f, int maxStacks = 1)
    {
        return new TraitLevelData
        {
            value = value,
            secondaryValue = secondaryValue,
            duration = duration,
            probability = probability,
            radius = radius,
            maxStacks = maxStacks
        };
    }

    private static TraitData CreateRuntimeTrait(string id, string displayName, string description, TraitCategory category, TraitRarity rarity, TraitEffectKind effectKind, int maxLevel, params TraitLevelData[] levels)
    {
        TraitData trait = ScriptableObject.CreateInstance<TraitData>();
        trait.traitId = id;
        trait.displayName = displayName;
        trait.description = description;
        trait.category = category;
        trait.rarity = rarity;
        trait.effectKind = effectKind;
        trait.maxLevel = maxLevel;
        trait.levels = new List<TraitLevelData>(levels);
        return trait;
    }

    private static string GetLocalizedDisplayName(TraitData trait)
    {
        if (trait == null)
            return "알 수 없는 능력";

        switch (trait.traitId)
        {
            case "fire_projectile": return "화염탄";
            case "ice_projectile": return "빙결탄";
            case "damage": return "과충전 탄약";
            case "attack_speed": return "고속 약실";
            case "move_speed": return "전투 자극제";
            case "magnet": return "중력 수집기";
            case "max_health": return "강화 생체장갑";
            case "armor": return "도금 전투복";
            case "health_regen": return "자동 수복 젤";
            case "heal_on_kill": return "흡혈 매트릭스";
            case "shield": return "위상 보호막";
            case "slow_aura": return "중력 덫";
            case "instant_kill": return "처형 탄두";
            default: return string.IsNullOrEmpty(trait.displayName) ? "알 수 없는 능력" : trait.displayName;
        }
    }

    private static string GetLocalizedDescription(TraitData trait)
    {
        if (trait == null)
            return string.Empty;

        switch (trait.traitId)
        {
            case "fire_projectile": return "탄환 속성을 화염으로 교체합니다.";
            case "ice_projectile": return "탄환 속성을 얼음으로 교체합니다.";
            case "damage": return "무기 피해량이 증가합니다.";
            case "attack_speed": return "무기의 연사 속도가 증가합니다.";
            case "move_speed": return "이동 속도가 증가합니다.";
            case "magnet": return "경험치 흡입 범위가 증가합니다.";
            case "max_health": return "최대 체력이 증가합니다.";
            case "armor": return "받는 피해가 고정 감소합니다.";
            case "health_regen": return "체력이 천천히 재생됩니다.";
            case "heal_on_kill": return "적 처치 시 체력을 회복합니다.";
            case "shield": return "피격 1회를 막는 보호막을 얻거나 강화합니다.";
            case "slow_aura": return "주변 적의 이동 속도를 늦춥니다.";
            case "instant_kill": return "일반 적을 일정 확률로 즉시 처치합니다.";
            default: return trait.description;
        }
    }
}
