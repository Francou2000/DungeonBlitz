using System.Collections.Generic;
using UnityEngine;

public class StatusEffectHandler : MonoBehaviour
{
    private Unit unit;
    private List<StatusEffect> activeEffects = new();

    private void Awake()
    {
        unit = GetComponent<Unit>();
    }

    public void ApplyEffect(StatusEffect effect)
    {
        if (!effect.isStackable)
        {
            var existing = activeEffects.Find(e => e.effectName == effect.effectName);
            if (existing != null)
            {
                existing.duration = Mathf.Max(existing.duration, effect.duration);
                return;
            }
        }

        activeEffects.Add(effect);
        Debug.Log($"[{unit.Model.UnitName}] Gained effect: {effect.effectName}");
    }

    public void TickEffects()
    {
        //TODO make it to have 1 tick at the beggining or end of a turn (need turn system)
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            activeEffects[i].duration--;

            if (activeEffects[i].duration <= 0)
            {
                Debug.Log($"[{unit.Model.UnitName}] Effect expired: {activeEffects[i].effectName}");
                activeEffects.RemoveAt(i);
            }
        }
    }

    public int GetStatModifier(StatModifier stat)
    {
        int total = 0;
        foreach (var e in activeEffects)
        {
            if (e.modifier == stat)
                total += e.amount;
        }
        return total;
    }

    public List<StatusEffect> GetAllActiveEffects()
    {
        return new List<StatusEffect>(activeEffects);
    }
}