using System.Collections.Generic;
using UnityEngine;

public class StatusEffectHandler : MonoBehaviour
{
    private List<StatusEffect> activeEffects = new List<StatusEffect>();

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
            isStackable = newEffect.isStackable
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

    //Expose a read‐only list for UI display
    public List<StatusEffect> GetActiveEffects() => new List<StatusEffect>(activeEffects);

    // Hooks (safe no-ops until we add behaviors)
    public void OnApply(Unit self, StatusEffect e) { }
    public void OnExpire(Unit self, StatusEffect e) { }
    public void OnStartTurn(Unit self) { }
    public void OnEndTurn(Unit self) { }
    public void OnMove(Unit self, Vector3 from, Vector3 to) { }
   // public void OnBeforeHit(Unit attacker, Unit target, ref CombatContext ctx) { }
   // public void OnAfterHit(Unit attacker, Unit target, CombatResult result) { }
}
