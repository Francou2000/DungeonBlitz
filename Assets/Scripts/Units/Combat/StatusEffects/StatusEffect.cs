using System;
using UnityEngine;

public enum StatusEffectType
{
    Buff,
    Debuff,
    Condition
}

public enum StatModifier
{
    Strength,
    Armor,
    MagicPower,
    MagicResistance,
    Performance,
    Affinity
}

[Serializable]
public class StatusEffect
{
    public string effectName;
    public StatusEffectType type;
    public StatModifier modifier;  // which stat it changes
    public int amount;             // positive or negative
    public int duration;           // in turns

    //whether multiple stacks are allowed
    public bool isStackable = false;
}