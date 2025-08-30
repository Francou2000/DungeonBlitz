using System;
using System.Collections.Generic;
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

    //whether multiple stacks are allowed and how many
    public bool isStackable = false;
    public int maxStacks = 0;   

    // Simple tag system (e.g., "Bleeding", "Frozen", "Taunt", "Apprehended", "Incandescent")
    public List<string> tags = new List<string>();

    // absorbs damage before HP
    public int barrierHP = 0;

    // Hook flags: the effect can respond to lifecycle/combat events
    public bool onApply, onExpire, onStartTurn, onEndTurn, onMove, onBeforeHit, onAfterHit;
}