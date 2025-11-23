using System.Collections.Generic;
using UnityEngine;

public enum DamageType
{
    Physical = 0,
    Magical = 1,
    Fire = 2,
    Frost = 3,
    Electric = 4,
    //  Mixed (cosmetic only; mitigation is done per-split on the master)
    Mixed = 5
}
public enum ReactionTrigger
{
    None,
    OnEnemyEnterRange,
    OnEnemyAttack,
    OnAllyDamaged,
    OnEnemyLeavesRange
}

public enum AbilityType
{
    BasicAttack,
    ClassAction,
    AdrenalineAction,
    Miscellaneous,
    Reaction
}

public enum AreaType 
{ 
    Single, 
    Line, 
    Circle 
}

public enum EffectTarget { Self, HitTarget }

public enum EffectId { Enraged, Bleed, Taunt, Barrier, Incandescent, Root, Shock, Burn, Freeze, Buff, Debuff }

public enum StructureTargeting { None, Enemy, Ally, Any }

[System.Serializable]
public struct ResourceCost { public string key; public int amount; }

[System.Serializable]
public class AbilityEffectDirective
{
    public EffectId effect = EffectId.Bleed;      // what to apply
    public EffectTarget target = EffectTarget.HitTarget; // self or per-hit target
    [Range(0, 100)] public int chancePct = 100;    // roll per application
    public int duration = 1;                      // general duration
    public int amount = 0;                        // barrierHP, bleedOnMove, buff amount, etc.
    public Stat stat = Stat.None;                 // used for Buff/Debuff
}

[System.Serializable]
public class UnitAbility : ScriptableObject
{
    [Header("Core")]
    public string abilityName;
    public string description;
    public AbilityType abilityType = AbilityType.BasicAttack;
    public Sprite icon;

    [Header("Costs & Requirements")]
    [Range(0, 100)]
    public float baseHitChance = 100f;
    public int actionCost = 1;
    public int adrenalineCost = 0;


    public List<ResourceCost> resourceCosts = new List<ResourceCost>();

    public List<AbilityEffectDirective> effects = new List<AbilityEffectDirective>();

    // Require the caster to have this adrenaline threshold
    public int minAdrenaline;

    [Header("State Change (optional)")]
    // Require a specific state (e.g., "Form:Fire", "Weapon:Bow")
    public List<string> requiredStates = new List<string>();
    // If true, casting this ability will set a stance/state on the caster.
    public bool changesState = false;
    // Example: key="Weapon", value="Bow"  OR  key="Form", value="Fire"
    public string stateKey;
    public string stateValue;

    // Tags that the *caster* must have active (from StatusEffectHandler)
    public List<string> requiredTags = new List<string>();

    // Tags that the *target* must have active (from StatusEffectHandler)
    public List<string> requiredTargetTags = new List<string>();

    [Header("Adrenaline Requirements")]
    public bool requiresAdrenalineThreshold;
    public int adrenalineThreshold;

    [Header("Area / Targeting")]
    public AreaType areaType = AreaType.Single;
    public int range = 1;                   // used when areaType == Single
    public float aoeRadius = 0f;            // used when areaType == Circle
    public int lineMaxTargets = 1;          // used when areaType == Line
    public int lineRange = 10;              // used when areaType == Line
    public float lineWidth = 0f;
    public float lineAlignmentTolerance = 0.75f; // how strictly units must align with the line (dot to direction)

    [Header("Structures (targeting)")]
    public bool allowTargetStructures = false;
    public StructureTargeting structureTargets = StructureTargeting.None;


    [Header("Advanced Damage")]
    public bool isMixedDamage = false;      // Paladin Smite style (phys + magic)
    [Range(0, 100)] public int mixedPhysicalPercent = 50; // rest goes to magical
    [Range(0, 100)] public int lineCollateralPercent = 50; // damage % for non-primary targets on a line
    [Range(0, 200)] public int bonusPerMissingHpPercent = 0; // % of missing HP added as bonus damage (Shadow kit)

    [Header("Damage")]
    public DamageType damageSource = DamageType.Physical;
    public int baseDamage = 0;
    public float damageMultiplier = 1f;
    public int bonusDamage = 0;
    public int hits = 1;
    
    [Header("Special Damage")]
    public bool useTargetMissingHealth = false;
    public float missingHealthPercentage = 0f;
    public bool canChain = false;
    public float chainRange = 0f;
    /*
    [Header("Status Effects")]
    public List<StatusEffect> appliedEffects = new List<StatusEffect>();
    public float statusEffectChance = 100f;*/

    [Header("Healing & Support")]
    public bool healsTarget = false;
    public int healAmount = 0;
    public float healPercentage = 0f;
    public bool grantsBarrier = false;
    public int barrierAmount = 0;

    [Header("Movement & Positioning")]
    public bool grantsMovement = false;
    public float movementRange = 0f;
    public bool isMovementFree = false;
    public bool isTeleport = false;

    [Header("Spawns (optional)")]
    public bool spawnsSummons = false;
    public GameObject summonPrefab;
    public int summonCount = 0;
    public string summonPrefabName = "";   // fallback if you prefer name-based

    public bool spawnsStructure = false; // if you added structures earlier
    public StructureKind structureKind = StructureKind.None;
    public int structureHP;
    public float structureHeal;
    public float structureDuration;
    public float structureRadius;

    public bool spawnsZone = false; // if you added zones earlier
    public ZoneKind zoneKind;
    public float zoneRadius;
    public float zoneDuration;

    [Header("Reactions")]
    public bool isReaction;
    public ReactionTrigger reactionTrigger;

    [Header("Conditional Effects")]
    public bool hasConditionalEffect = false;
    public string conditionTag = "";
    public bool reducesActionCostOnCondition = false;
    public int reducedActionCost = 0;

    [Header("Targeting Filters")]
    public bool selfOnly;
    public bool alliesOnly;
    public bool enemiesOnly;
    public bool groundTarget;


    public DamageType GetDamageType()
    {
        return damageSource == DamageType.Physical ? DamageType.Physical : DamageType.Magical;
    }
    
    public int GetBaseDamageValue()
    {
        return baseDamage;
    }
    
    public int CalculateTotalDamage(UnitModel caster, UnitModel target = null)
    {
        int damage = GetBaseDamageValue();
        
        // Add stat bonus
        if (damageSource == DamageType.Physical)
        {
            damage += caster.Strength;
        }
        else if (damageSource == DamageType.Magical)
        {
            damage += caster.MagicPower;
        }
        
        // Apply multiplier and bonus
        damage = Mathf.RoundToInt(damage * damageMultiplier) + bonusDamage;
        
        // Add missing health damage if applicable
        if (useTargetMissingHealth && target != null)
        {
            int missingHealth = target.MaxHP - target.CurrentHP;
            int missingHealthDamage = Mathf.RoundToInt(missingHealth * missingHealthPercentage);
            damage += missingHealthDamage;
        }
        
        return damage;
    }

    public int ComputeHealAmount(UnitModel caster, UnitModel target)
    {
        if (!healsTarget || target == null) return 0;

        // Missing-HP based healing:
        // "healPercentage" is interpreted as PERCENT OF MISSING HP (e.g., 25 = 25% of missing).
        int missing = Mathf.Max(0, target.MaxHP - target.CurrentHP);

        // Base flat heal, if any (keeps your ScriptableObject knobs working)
        int heal = Mathf.Max(0, healAmount);

        // Percent of MISSING HP (as you described for Healing Prayer)
        if (healPercentage > 0f)
            heal += Mathf.CeilToInt(missing * (healPercentage / 100f));


        // Never overheal past what's missing
        heal = Mathf.Min(heal, missing);

        return heal;
    }

    // Legacy shim so old calls compile (uses caster as both if target is unknown)
    public int CalculateHealAmount(UnitModel caster)
    {
        return ComputeHealAmount(caster, caster);
    }

    public int GetEffectiveActionCost(UnitModel caster, bool conditionMet = false)
    {
        if (reducesActionCostOnCondition && conditionMet)
        {
            return reducedActionCost;
        }
        return actionCost;
    }
}
