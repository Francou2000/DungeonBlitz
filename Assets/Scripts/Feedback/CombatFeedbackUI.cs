using SpatialUI;
using UnityEngine;

public static class CombatFeedbackUI
{
    public static void ShowHit(Unit target, float dmg, DamageType dmgType, bool isCrit)
    {
        if (!target) return;
        SpatialUIManager.Instance.ShowDamage(
            target.transform,    
            dmg,
            MapDamageType(dmgType),
            isCrit
        );
    }

    public static void ShowHit(Unit target, float dmg, DamageType dmgType, bool isCrit, Unit attacker, UnitAbility ability)
    {
        if (!target) return;
        
        // Mostrar el texto de daño
        SpatialUIManager.Instance.ShowDamage(
            target.transform,    
            dmg,
            MapDamageType(dmgType),
            isCrit
        );

        // Mostrar el efecto visual de daño
        if (DamageEffectManager.Instance != null)
        {
            DamageEffectManager.ShowDamageEffectStatic(target, attacker, ability, Mathf.RoundToInt(dmg));
        }
        
        // Reproducir sonido de hurt cuando se recibe daño
        if (dmg > 0 && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHurtSoundByUnitType(target);
        }
    }

    public static void ShowMiss(Unit target)
    {
        if (!target) return;
        SpatialUIManager.Instance.ShowMiss(target.transform);
    }

    public static void ShowHeal(Unit target, float amount)
    {
        if (!target) return;
        SpatialUIManager.Instance.ShowHeal(target.transform, amount);
    }

    public static void ShowAbilityName(Unit caster, string abilityName)
    {
        if (!caster) return;
        if (SpatialUIManager.Instance != null)
        {
            SpatialUIManager.Instance.ShowAbilityName(caster.transform, abilityName);
        }
    }

    private static DamageUiType MapDamageType(DamageType t)
    {
        switch (t)
        {
            case DamageType.Physical: return DamageUiType.Physical;
            case DamageType.Magical: return DamageUiType.Magical;
            case DamageType.Fire: return DamageUiType.Fire;
            case DamageType.Frost: return DamageUiType.Frost;
            case DamageType.Electric: return DamageUiType.Electric;
            default: return DamageUiType.Physical;
        }
    }
}
