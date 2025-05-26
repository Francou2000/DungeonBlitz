using System.Collections.Generic;
using UnityEngine;

public enum DamageSourceType
{
    Strength,
    MagicPower
}

public enum DamageType
{
    Physical,
    Magical
}
public enum ReactionTrigger
{
    None,
    OnEnemyEnterRange,
    OnEnemyAttack,
    OnAllyDamaged,
    OnEnemyLeavesRange
}

[System.Serializable]
public class UnitAbility 
{
    public string abilityName;

    [Range(0, 100)]
    public float baseHitChance = 100f;

    public float range = 3f;

    public int baseDamage;
    public DamageSourceType damageSource;

    public bool requiresAdrenalineThreshold;
    public int adrenalineThreshold;

    public int hits = 1; // Default is 1 hit

    public int actionCost = 1; //Default is 1 action

    public List<StatusEffect> appliedEffects = new List<StatusEffect>();

    public bool isReaction;
    public ReactionTrigger reactionTrigger;

    public DamageType GetDamageType()
    {
        return damageSource == DamageSourceType.Strength ? DamageType.Physical : DamageType.Magical;
    }
}
