using UnityEngine;

/// Static constructors that return *new* runtime effects (cloneable if needed).
public static class EffectLibrary
{
    public static StatusEffect Enraged(int duration = 2) => new()
    {
        type = StatusType.Enraged,
        name = "Enraged",
        remainingTurns = duration,
        nextTurnActionsDelta = +1
    };

    public static StatusEffect Bleed(int heavyOnMove) => new()
    {
        type = StatusType.Bleed,
        name = "Bleeding",
        remainingTurns = 99,         // ends by rule if no move
        bleedDamageOnMove = heavyOnMove
    };

    public static StatusEffect Taunt(int casterViewId, int duration = 2) => new()
    {
        type = StatusType.Taunt,
        name = "Taunted",
        remainingTurns = duration,
        targetLockViewId = casterViewId
    };

    public static StatusEffect Buff(Stat stat, int amount, int duration) => new()
    {
        type = StatusType.Buff,
        name = $"Buff {stat}+{amount}",
        remainingTurns = duration,
        stat = stat,
        amount = amount,
        isDebuff = false
    };

    public static StatusEffect Debuff(Stat stat, int amount, int duration) => new()
    {
        type = StatusType.Debuff,
        name = $"Debuff {stat}{amount}",
        remainingTurns = duration,
        stat = stat,
        amount = Mathf.Abs(amount),
        isDebuff = true
    };

    public static StatusEffect Barrier(int hp, int duration = 2) => new()
    {
        type = StatusType.Barrier,
        name = "Barrier",
        remainingTurns = duration,
        barrierHP = Mathf.Max(1, hp)
    };

    public static StatusEffect Incandescent(int duration = 2) => new()
    {
        type = StatusType.Incandescent,
        name = "Incandescent",
        remainingTurns = duration,
        incandescent = true
    };

    public static StatusEffect Root(int duration = 1) => new()
    {
        type = StatusType.Root,
        name = "Rooted",
        remainingTurns = duration,
        root = true,
        moveSpeedDeltaPct = -100
    };

    public static StatusEffect Haste(int duration = 1) => new()
    {
        type = StatusType.Haste,
        name = "Haste",
        remainingTurns = duration,
        nextTurnActionsDelta = +1,
        moveSpeedDeltaPct = +20
    };

    public static StatusEffect Shock(int duration = 1) => new()
    {
        type = StatusType.Shock,
        name = "Shocked",
        remainingTurns = duration,
        nextTurnActionsDelta = -1
    };

    public static StatusEffect Burn(int sourceViewId, int ticks = 2) => new()
    {
        type = StatusType.Burn,
        name = "Burning",
        sourceViewId = sourceViewId,
        remainingTurns = ticks,
        aux = ticks // ticks left
    };

    public static StatusEffect Freeze(int duration = 1) => new()
    {
        type = StatusType.Freeze,
        name = "Frozen",
        remainingTurns = duration
        // Performance halving is implemented in your getters (see UnitModel patch)
    };
}
