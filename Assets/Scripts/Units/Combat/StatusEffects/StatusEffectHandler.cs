using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public class StatusEffectHandler : MonoBehaviour
{
    private List<StatusEffect> activeEffects = new List<StatusEffect>();
    private readonly HashSet<string> _tags = new HashSet<string>();

    // Transient per-turn deltas
    private int pendingNextTurnActionDelta = 0;
    private float accumMoveBuff = 0f;
    private float accumMoveDebuff = 0f;
    private float healingMultiplier = 1f; // multiply incoming healing


    //Apply a new effect; merges durations if non‐stackable
    public void ApplyEffect(StatusEffect newEffect)
    {
        // If not stackable, find an existing one and refresh its duration
        if (!newEffect.isStackable)
        {
            var existing = activeEffects.Find(e => e.effectName == newEffect.effectName);
            if (existing != null)
            {
                existing.duration = Mathf.Max(existing.duration, newEffect.duration);
                Debug.Log($"[Status] Refreshed {existing.effectName} to {existing.duration} turns");
                return;
            }
        }

        // Otherwise add a clone so we don’t share references
        var clone = new StatusEffect
        {
            effectName = newEffect.effectName,
            type = newEffect.type,
            modifier = newEffect.modifier,
            amount = newEffect.amount,
            duration = newEffect.duration,
            isStackable = newEffect.isStackable,
            barrierHP = newEffect.barrierHP,
            damagePerTurn = newEffect.damagePerTurn,
            tags = new List<string>(newEffect.tags ?? new List<string>())
        };
        activeEffects.Add(clone);
        Debug.Log($"[Status] Applied {clone.effectName} for {clone.duration} turns");
    }

    //Call at the start (or end) of each unit’s turn to tick down durations
    public void TickEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            activeEffects[i].duration--;
            if (activeEffects[i].duration <= 0)
            {
                Debug.Log($"[Status] Expired {activeEffects[i].effectName}");
                activeEffects.RemoveAt(i);
            }
        }
    }

    //Returns the total modifier for a given stat from all active effects
    public int GetStatBonus(StatModifier stat)
    {
        int sum = 0;
        foreach (var e in activeEffects)
            if (e.modifier == stat)
                sum += e.amount;
        return sum;
    }

    public bool HasTag(string tag)
    {
        foreach (var e in activeEffects)
            if (e.tags != null && e.tags.Contains(tag)) return true;
        return false;
    }

    public int GetBarrierHP() // total barrier pool (sum of effect barriers)
    {
        int total = 0;
        foreach (var e in activeEffects) total += Mathf.Max(0, e.barrierHP);
        return total;
    }

    public int ConsumeBarrier(int amount)
    {
        if (amount <= 0) return 0;
        int absorbed = 0;
        for (int i = 0; i < activeEffects.Count && amount > 0; i++)
        {
            var e = activeEffects[i];
            if (e.barrierHP <= 0) continue;
            int take = Mathf.Min(e.barrierHP, amount);
            e.barrierHP -= take;
            absorbed += take;
            amount -= take;
        }
        return absorbed;
    }

    public int ModifyIncomingHealing(int amount)
    {
        // Aggregate healing multiplier from active effects (computed each OnStartTurn)
        // Default 1.0 — Poison drops it to 0.5, etc.
        if (amount <= 0) return amount;
        float modified = amount * Mathf.Max(0f, healingMultiplier);
        return Mathf.RoundToInt(modified);
    }

    // Targeting constraints helper (for “Taunted”)
    public bool IsTauntedTo(int targetViewId)
    {
        if (!_tags.Contains("Taunted")) return false;
        for (int i = 0; i < activeEffects.Count; i++)
        {
            var e = activeEffects[i];
            if (e != null && e.targetLockViewId == targetViewId) return true;
        }
        return false;
    }

    //Expose a read‐only list for UI display
    public List<StatusEffect> GetActiveEffects() => new List<StatusEffect>(activeEffects);

    // Hooks (safe no-ops until we add behaviors)
    public void OnApply(Unit self, StatusEffect e) { }
    public void OnExpire(Unit self, StatusEffect e) { }
    public void OnStartTurn(Unit self)
    {
        // 1) Tick DoTs & build aggregates for this turn
        int totalDot = 0;
        pendingNextTurnActionDelta = 0;
        accumMoveBuff = 0f;
        accumMoveDebuff = 0f;
        healingMultiplier = 1f;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var e = activeEffects[i];
            if (e == null) { activeEffects.RemoveAt(i); continue; }

            // Accumulate DOT
            if (e.damagePerTurn > 0) totalDot += e.damagePerTurn;

            // Aggregate action delta for *next* turn
            pendingNextTurnActionDelta += e.nextTurnActionsDelta;

            // Movement modifiers (percentages)
            accumMoveBuff += e.moveSpeedBuff;
            accumMoveDebuff += e.moveSpeedDebuff;

            // Healing multiplier
            if (e.healingMultiplier > 0f) healingMultiplier *= e.healingMultiplier;

            // Decrement duration and clear expired
            e.duration--;
            if (e.duration <= 0)
            {
                // Remove its tags unless another copy still provides it
                if (e.tags != null)
                {
                    foreach (var t in e.tags)
                    {
                        bool stillProvided = false;
                        for (int k = 0; k < activeEffects.Count; k++)
                        {
                            if (activeEffects[k] != null && activeEffects[k].tags != null && activeEffects[k].tags.Contains(t))
                            {
                                stillProvided = true; break;
                            }
                        }
                        if (!stillProvided) _tags.Remove(t);
                    }
                }
                activeEffects.RemoveAt(i);
            }
        }

        // 2) Apply DOT
        if (totalDot > 0)
        {
            self.Model.TakeDamage(totalDot, DamageType.Magical); // DoTs as magical by default (tune per effect if needed)
        }

        // 3) Push movement modifiers into model for this turn
        if (accumMoveBuff != 0f) self.Model.AddMoveBuff(accumMoveBuff);
        if (accumMoveDebuff != 0f) self.Model.AddMoveDebuff(accumMoveDebuff);

        // 4) Apply “next turn” action deltas by modifying the model’s action count right now (we’re at start of this unit’s turn)
        if (pendingNextTurnActionDelta != 0)
        {
            // Reduce or increase current actions for this turn
            int clamped = Mathf.Max(0, self.Model.CurrentActions + pendingNextTurnActionDelta);
            // You may need a setter; if not, expose a small friend method to set currentActions.
            self.Model.SetCurrentActions(clamped); // <--- add a setter in UnitModel if you don’t have it
        }
    }

    public void OnEndTurn(Unit self) { }
    public void OnMove(Unit self, Vector3 from, Vector3 to)
    {
        // Bleed: extra damage if configured
        foreach (var e in activeEffects)
        {
            if (e == null) continue;
            if (e.bleedBonusOnMove && e.bleedMoveBonus > 0)
                self.Model.TakeDamage(e.bleedMoveBonus, DamageType.Physical);
        }

        // Zones (already added in earlier tasks)
        if (ZoneManager.Instance != null)
        {
            if (ZoneManager.Instance.IsInsideZone(to, ZoneKind.Frozen))
                ApplyEffect(EffectLibrary.Frozen(1));

            ZoneManager.Instance.HandleOnMove(self, from, to);
        }
    }

    // public void OnBeforeHit(Unit attacker, Unit target, ref CombatContext ctx) { }

    // Additional methods for compatibility with new controllers
    public void ApplyStatusEffect(StatusEffect effect, Unit caster = null)
    {
        ApplyEffect(effect);
    }

    public void RemoveStatusEffectByName(string effectName)
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].effectName == effectName)
            {
                Debug.Log($"[Status] Removed {effectName}");
                activeEffects.RemoveAt(i);
            }
        }
    }

    public void RemoveStatusEffect(StatusEffect effect)
    {
        if (activeEffects.Remove(effect))
        {
            Debug.Log($"[Status] Removed {effect.effectName}");
        }
    }

    public bool HasStatusEffect(string effectName)
    {
        return activeEffects.Exists(e => e.effectName == effectName);
    }

    public bool HasAnyStatusEffect()
    {
        return activeEffects.Count > 0;
    }

    public List<StatusEffect> GetStatusEffects()
    {
        return GetActiveEffects();
    }

}
