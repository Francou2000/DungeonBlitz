using System.Collections.Generic;
using UnityEngine;

public class PaladinController : UnitController
{
    [Header("Paladin Specific")]
    private List<Unit> linkedAllies = new List<Unit>();
    private bool hasUsedHolyShield = false;
    
    protected override void HandleAdrenalineStateEntered()
    {
        base.HandleAdrenalineStateEntered();
        
        // Gain armor buff when entering adrenaline state
        var armorBuff = new StatusEffect
        {
            effectName = "Divine Protection",
            type = StatusEffectType.Buff,
            modifier = StatModifier.Armor,
            amount = 8,
            duration = -1 // Permanent while in adrenaline state
        };
        
        GetComponent<StatusEffectHandler>()?.ApplyEffect(armorBuff);
    }
    
    protected override void HandleAdrenalineStateExited()
    {
        base.HandleAdrenalineStateExited();
        
        // Remove adrenaline buffs
        GetComponent<StatusEffectHandler>()?.RemoveStatusEffectByName("Divine Protection");
    }
    
    protected override void OnAbilityExecuted(UnitAbility ability, Unit target)
    {
        base.OnAbilityExecuted(ability, target);
        
        switch (ability.abilityName)
        {
            case "Paladin Sword":
                ExecutePaladinSword(target);
                break;
            case "Smite":
                ExecuteSmite(target);
                break;
            case "Link":
                ExecuteLink(target);
                break;
            case "Holy Healing":
                ExecuteHolyHealing(target);
                break;
            case "Holy Shield":
                ExecuteHolyShield();
                break;
            case "Divine Intervention":
                ExecuteDivineIntervention(target);
                break;
        }
    }
    
    private void ExecutePaladinSword(Unit target)
    {
        if (target == null) return;
        
        var ability = GetAbilityByName("Paladin Sword");
        if (ability == null) return;
        
        int damage = ability.CalculateTotalDamage(model, target.Model);
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        // Gain adrenaline for hitting enemies
        if (target.Faction != unit.Faction)
        {
            model.AddAdrenaline(3, "hitting enemy");
        }
    }
    
    private void ExecuteSmite(Unit target)
    {
        if (target == null || target.Faction == unit.Faction) return;
        
        var ability = GetAbilityByName("Smite");
        if (ability == null) return;
        
        // Calculate base damage
        int damage = ability.CalculateTotalDamage(model, target.Model);
        
        // Check if target has status effects for bonus damage
        var statusHandler = target.GetComponent<StatusEffectHandler>();
        if (statusHandler != null && statusHandler.HasAnyStatusEffect())
        {
            damage = Mathf.RoundToInt(damage * 1.5f); // 50% more damage
            Debug.Log($"{model.UnitName} deals bonus Smite damage to {target.Model.UnitName} (has status effects)!");
        }
        
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        // Gain adrenaline for using smite
        model.AddAdrenaline(8, "smiting evil");
    }
    
    private void ExecuteLink(Unit target)
    {
        if (target == null || target.Faction != unit.Faction || target == unit) return;
        
        // Check if already linked
        if (linkedAllies.Contains(target))
        {
            Debug.Log($"{target.Model.UnitName} is already linked to {model.UnitName}!");
            return;
        }
        
        // Maximum 2 links
        if (linkedAllies.Count >= 2)
        {
            Debug.Log($"{model.UnitName} already has maximum links (2)!");
            return;
        }
        
        linkedAllies.Add(target);
        Debug.Log($"{model.UnitName} creates a holy link with {target.Model.UnitName}!");
        
        // Apply link effect to both units
        var linkEffect = new StatusEffect
        {
            effectName = "Holy Link",
            type = StatusEffectType.Buff,
            modifier = StatModifier.MagicResistance,
            amount = 5,
            duration = -1, // Permanent until broken
            tags = { "HolyLink", "Divine" }
        };
        
        GetComponent<StatusEffectHandler>()?.ApplyEffect(linkEffect);
        target.GetComponent<StatusEffectHandler>()?.ApplyEffect(linkEffect);
        
        // Subscribe to ally damage events
        target.OnDamageTaken += OnLinkedAllyTakesDamage;
        
        // Gain adrenaline for protecting allies
        model.AddAdrenaline(5, "linking with ally");
    }
    
    private void ExecuteHolyHealing(Unit target)
    {
        if (target == null || target.Faction != unit.Faction) return;
        
        var ability = GetAbilityByName("Holy Healing");
        if (ability == null) return;
        
        int healAmount = ability.CalculateHealAmount(model);
        target.Heal(healAmount, unit);
        
        // Remove one negative status effect
        var statusHandler = target.GetComponent<StatusEffectHandler>();
        if (statusHandler != null)
        {
            var debuffs = statusHandler.GetStatusEffects().FindAll(effect => effect.type == StatusEffectType.Debuff);
            if (debuffs.Count > 0)
            {
                statusHandler.RemoveStatusEffect(debuffs[0]);
                Debug.Log($"Holy Healing removes {debuffs[0].effectName} from {target.Model.UnitName}!");
            }
        }
        
        Debug.Log($"{model.UnitName} heals {target.Model.UnitName} for {healAmount} HP!");
        
        // Gain adrenaline for healing
        model.AddAdrenaline(5, "healing ally");
    }
    
    private void ExecuteHolyShield()
    {
        if (!model.IsInAdrenalineState || hasUsedHolyShield) return;
        
        Debug.Log($"{model.UnitName} activates Holy Shield!");
        
        // Grant massive barrier to self and linked allies
        int barrierAmount = 25 + model.MagicPower;
        
        var holyBarrier = new StatusEffect
        {
            effectName = "Holy Shield",
            type = StatusEffectType.Buff,
            barrierHP = barrierAmount,
            duration = 5,
            tags = { "HolyShield", "Divine" }
        };
        
        // Apply to self
        GetComponent<StatusEffectHandler>()?.ApplyEffect(holyBarrier);
        
        // Apply to linked allies
        foreach (var ally in linkedAllies)
        {
            if (ally != null && ally.IsAlive)
            {
                ally.GetComponent<StatusEffectHandler>()?.ApplyEffect(holyBarrier);
                Debug.Log($"{ally.Model.UnitName} receives Holy Shield ({barrierAmount} barrier)!");
            }
        }
        
        hasUsedHolyShield = true;
    }
    
    private void ExecuteDivineIntervention(Unit target)
    {
        if (target == null || !model.IsInAdrenalineState) return;
        
        Debug.Log($"{model.UnitName} uses Divine Intervention on {target.Model.UnitName}!");
        
        if (target.Faction == unit.Faction)
        {
            // Ally version: Full heal and remove all debuffs
            int fullHeal = target.Model.MaxHP - target.Model.CurrentHP;
            target.Heal(fullHeal, unit);
            
            var statusHandler = target.GetComponent<StatusEffectHandler>();
            if (statusHandler != null)
            {
                var debuffs = statusHandler.GetStatusEffects().FindAll(effect => effect.type == StatusEffectType.Debuff);
                foreach (var debuff in debuffs)
                {
                    statusHandler.RemoveStatusEffect(debuff);
                }
            }
            
            Debug.Log($"{target.Model.UnitName} is fully healed and cleansed!");
        }
        else
        {
            // Enemy version: Heavy damage and dispel buffs
            int damage = 40 + model.MagicPower; // Heavy damage
            target.Model.ApplyDamageWithBarrier(damage, DamageType.Magical);
            
            var statusHandler = target.GetComponent<StatusEffectHandler>();
            if (statusHandler != null)
            {
                var buffs = statusHandler.GetStatusEffects().FindAll(effect => effect.type == StatusEffectType.Buff);
                foreach (var buff in buffs)
                {
                    statusHandler.RemoveStatusEffect(buff);
                }
            }
            
            Debug.Log($"Divine Intervention deals {damage} damage to {target.Model.UnitName} and dispels buffs!");
        }
    }
    
    // Handle damage sharing with linked allies
    private void OnLinkedAllyTakesDamage(int damage, DamageType damageType, Unit attacker)
    {
        // Share 25% of damage taken by linked allies
        int sharedDamage = Mathf.RoundToInt(damage * 0.25f);
        
        if (sharedDamage > 0 && model.IsAlive())
        {
            Debug.Log($"{model.UnitName} shares {sharedDamage} damage through Holy Link!");
            model.ApplyDamageWithBarrier(sharedDamage, damageType);
            
            // Gain adrenaline for protecting allies
            model.AddAdrenaline(3, "sharing ally damage");
        }
    }

    public override void OnDamageTaken(int damage, DamageType damageType, Unit attacker)
    {
        base.OnDamageTaken(damage, damageType, attacker);
        
        // Gain adrenaline when taking damage (tanks gain more)
        model.AddAdrenaline(5, "taking damage as protector");
        
        // Gain extra adrenaline when protecting others
        if (linkedAllies.Count > 0)
        {
            model.AddAdrenaline(3, "protecting linked allies");
        }
    }

    public override void OnHealed(int amount, Unit healer)
    {
        base.OnHealed(amount, healer);
        
        // Share healing with linked allies (reduced amount)
        int sharedHeal = Mathf.RoundToInt(amount * 0.3f);
        
        foreach (var ally in linkedAllies)
        {
            if (ally != null && ally.IsAlive && ally != healer)
            {
                ally.Heal(sharedHeal, healer);
                Debug.Log($"{ally.Model.UnitName} receives {sharedHeal} shared healing through Holy Link!");
            }
        }
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        hasUsedHolyShield = false;
        
        // Clean up dead allies from links
        linkedAllies.RemoveAll(ally => ally == null || !ally.IsAlive);
    }
    
    // Paladin's reaction: Divine Protection
    public void UseDivineProtection(Unit protectedAlly, Unit attacker)
    {
        if (!model.CanReact() || protectedAlly.Faction != unit.Faction) return;
        
        float distance = Vector3.Distance(transform.position, protectedAlly.transform.position);
        if (distance <= 2f && linkedAllies.Contains(protectedAlly))
        {
            Debug.Log($"{model.UnitName} uses Divine Protection on {protectedAlly.Model.UnitName}!");
            
            model.SpendReaction();
            
            // Grant temporary damage reduction
            var protection = new StatusEffect
            {
                effectName = "Divine Protection",
                type = StatusEffectType.Buff,
                modifier = StatModifier.Armor,
                amount = 15,
                duration = 1,
                tags = { "Divine", "Protection" }
            };
            
            protectedAlly.GetComponent<StatusEffectHandler>()?.ApplyEffect(protection);
        }
    }
    
    // Clean up when unit dies
    void OnDestroy()
    {
        // Unsubscribe from linked ally events
        foreach (var ally in linkedAllies)
        {
            if (ally != null)
            {
                ally.OnDamageTaken -= OnLinkedAllyTakesDamage;
                
                // Remove link effects
                var statusHandler = ally.GetComponent<StatusEffectHandler>();
                statusHandler?.RemoveStatusEffectByName("Holy Link");
            }
        }
        
        linkedAllies.Clear();
    }
}