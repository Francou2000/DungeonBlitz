using System.Collections.Generic;
using UnityEngine;

public static class EffectLibrary
{
    // --- Stateless builders (clone per apply) ---

    public static StatusEffect Bleeding(int duration = 2, int perHitBonus = 0)
    {
        return new StatusEffect
        {
            duration = duration,
            isStackable = true,
            maxStacks = 5,
            tags = new List<string> { "Bleeding" },
            //tiny stat tweak; most of bleeding is "on end turn" damage 
            onEndTurn = false, // we keep no-op for now (damage later)
        };
    }

    public static StatusEffect Frozen(int duration = 1)
    {
        return new StatusEffect
        {
            duration = duration,
            isStackable = false,
            tags = new List<string> { "Frozen" },
            // Could gate movement/attacks via tags; hook behaviors later
        };
    }

    public static StatusEffect Shocked(int duration = 1)
    {
        return new StatusEffect
        {
            duration = duration,
            isStackable = true,
            maxStacks = 3,
            tags = new List<string> { "Shocked" },
            // Use OnBeforeHit later to reduce hit or add chance to be stunned, etc.
            onBeforeHit = false
        };
    }

    public static StatusEffect Slow(int duration = 1)
    {
        return new StatusEffect
        {
            duration = duration,
            isStackable = false,
            tags = new List<string> { "Slow" },
            // Hook later to reduce move allowance
        };
    }

    public static StatusEffect Haste(int duration = 1)
    {
        return new StatusEffect
        {
            duration = duration,
            isStackable = false,
            tags = new List<string> { "Haste" },
            // Hook later to boost move/initiative
        };
    }

    public static StatusEffect Barrier(int hp, int duration = 2)
    {
        return new StatusEffect
        {
            duration = duration,
            isStackable = true,
            maxStacks = 99,
            barrierHP = Mathf.Max(0, hp),
            tags = new List<string> { "Barrier" }
        };
    }

    public static StatusEffect Enraged(int duration = 1)
    {
        return new StatusEffect
        {
            duration = duration,
            isStackable = false,
            tags = new List<string> { "Enraged" },
            // Later: OnBeforeHit += dmg%, OnMove maybe force target, etc.
        };
    }

    public static StatusEffect Taunt(int duration = 1)
    {
        return new StatusEffect
        {
            duration = duration,
            isStackable = false,
            tags = new List<string> { "Taunt" }
        };
    }

    public static StatusEffect Incandescent(int duration = 2)
    {
        return new StatusEffect
        {
            duration = duration,
            isStackable = false,
            tags = new List<string> { "Incandescent" }
        };
    }

    public static StatusEffect Apprehended(int duration = 1)
    {
        return new StatusEffect
        {
            duration = duration,
            isStackable = false,
            tags = new List<string> { "Apprehended" }
        };
    }
}
