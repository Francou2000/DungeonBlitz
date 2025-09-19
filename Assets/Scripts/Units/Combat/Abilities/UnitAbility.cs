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

public enum CastDuration
{
    Fast,
    Medium,
    Slow
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

[System.Serializable]
public class UnitAbility 
{
    [Header("Basic Info")]
    public string abilityName;
    public string description;
    public AbilityType abilityType = AbilityType.BasicAttack;

    [Header("Costs & Requirements")]
    [Range(0, 100)]
    public float baseHitChance = 100f;
    public int actionCost = 1;
    public int adrenalineCost = 0;

    [System.Serializable]
    public struct ResourceCost { public string key; public int amount; }

    public List<ResourceCost> resourceCosts = new List<ResourceCost>();

    // Require the caster to have this adrenaline threshold
    public int minAdrenaline;

    // Require a specific state (e.g., "Form:Fire", "Weapon:Bow")
    public List<string> requiredStates = new List<string>();

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
    public float lineAlignmentTolerance = 0.75f; // how strictly units must align with the line (dot to direction)

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

    [Header("Timing & Animation")]
    public CastDuration castDuration = CastDuration.Medium;

    [Header("Status Effects")]
    public List<StatusEffect> appliedEffects = new List<StatusEffect>();
    public float statusEffectChance = 100f;

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

    [Header("Summoning")]
    [Header("Summon (optional)")]
    public bool spawnsSummons;
    public string summonPrefabName = "DeadGoblin";  // must match a Resources/Photon prefab name
    public int summonCount = 2;
    public float summonDuration = 20f;              // lifetime in seconds (game time)
    public int summonMaxHP = 12;
    public int summonActionsPerTurn = 1;
    public int summonStrength = 4;                  
    public int summonMagicPower = 0;
    public int summonArmor = 0;
    public int summonMagicRes = 0;

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
    
    public int CalculateHealAmount(UnitModel caster)
    {
        int heal = healAmount;
        
        if (healPercentage > 0)
        {
            heal += Mathf.RoundToInt(caster.MaxHP * healPercentage);
        }
        
        // Some healing abilities scale with magic power
        if (damageSource == DamageType.Magical)
        {
            heal += Mathf.RoundToInt(caster.MagicPower * 0.3f);
        }
        
        return heal;
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
