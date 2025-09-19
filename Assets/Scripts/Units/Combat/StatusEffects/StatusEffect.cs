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

    // knobs interpreted by the handler
    public int damagePerTurn = 0;         // damage per tick (start of turn)
    public int barrierHP = 0;             // flat absorb pool added on apply
    public int nextTurnActionsDelta;      // e.g., -1 for Shocked/Frozen
    public int actionsBonus;              // e.g., +1 for Haste (immediate)
    public float moveSpeedBuff;           // +% movement speed (0.25 = +25%)
    public float moveSpeedDebuff;         // -% movement speed (0.5 = -50%)
    public float healingMultiplier = 1f;  // e.g., Poison -> 0.5f (50% healing)

    // Bleed special
    public bool bleedBonusOnMove;
    public int bleedMoveBonus;

    // Taunt special
    public int targetLockViewId;          // PV id of the taunter

    // Hook flags: the effect can respond to lifecycle/combat events
    public bool onApply, onExpire, onStartTurn, onEndTurn, onMove, onBeforeHit, onAfterHit;
}