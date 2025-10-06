using UnityEngine;

public class GoblinShamanController : UnitController
{
    [Header("Goblin Shaman Specific")]
    private GameObject activeBarrierZone = null;
    private GameObject activeCurseZone = null;
    
    protected override void HandleAdrenalineStateEntered()
    {
        base.HandleAdrenalineStateEntered();
        
        // Gain low MP buff when entering adrenaline state
        var mpBuff = new StatusEffect
        {
            effectName = "Shamanic Focus",
            type = StatusEffectType.Buff,
            modifier = StatModifier.MagicPower,
            amount = 5,
            duration = -1 // Permanent while in adrenaline state
        };
        
        GetComponent<StatusEffectHandler>()?.ApplyEffect(mpBuff);
    }
    
    protected override void HandleAdrenalineStateExited()
    {
        base.HandleAdrenalineStateExited();
        
        // Remove adrenaline buffs
        GetComponent<StatusEffectHandler>()?.RemoveStatusEffectByName("Shamanic Focus");
    }
    
    protected override void OnAbilityExecuted(UnitAbility ability, Unit target)
    {
        base.OnAbilityExecuted(ability, target);
        
        switch (ability.abilityName)
        {
            case "Shamanic Strike":
                ExecuteShamanicStrike(target);
                break;
            case "Barrier Zone":
                ExecuteBarrierZone(target?.transform.position ?? transform.position);
                break;
            case "Curse Zone":
                ExecuteCurseZone(target?.transform.position ?? transform.position);
                break;
            case "Shamanic Ritual":
                ExecuteShamanicRitual();
                break;
            case "Elemental Storm":
                ExecuteElementalStorm(target?.transform.position ?? transform.position);
                break;
        }
        
        // Gain adrenaline for supporting allies
        if (ability.abilityName == "Barrier Zone" || ability.abilityName == "Shamanic Ritual")
        {
            model.AddAdrenaline(8, "supporting allies");
        }
        
        // Gain adrenaline for area control
        if (ability.abilityName == "Curse Zone" || ability.abilityName == "Elemental Storm")
        {
            model.AddAdrenaline(5, "area control");
        }
    }
    
    private void ExecuteShamanicStrike(Unit target)
    {
        if (target == null) return;
        
        var ability = GetAbilityByName("Shamanic Strike");
        if (ability == null) return;
        
        int damage = ability.CalculateTotalDamage(model, target.Model);
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        // Gain adrenaline for hitting enemies
        if (target.Faction != unit.Faction)
        {
            model.AddAdrenaline(3, "hitting enemy with Shamanic Strike");
        }
    }
    
    private void ExecuteBarrierZone(Vector3 position)
    {
        // Remove existing barrier zone if any
        if (activeBarrierZone != null)
        {
            Destroy(activeBarrierZone);
        }
        
        // Create barrier zone (simplified - in full implementation would create actual zone)
        Debug.Log($"{model.UnitName} creates Barrier Zone at {position}");
        
        // Apply barrier to allies in the zone
        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var targetUnit in allUnits)
        {
            if (targetUnit.Faction == unit.Faction && 
                Vector3.Distance(position, targetUnit.transform.position) <= 3f)
            {
                var barrierEffect = new StatusEffect
                {
                    effectName = "Shamanic Barrier",
                    type = StatusEffectType.Buff,
                    barrierHP = 8 + model.MagicPower,
                    duration = 5,
                    tags = { "Barrier", "ShamanicZone" }
                };
                
                targetUnit.GetComponent<StatusEffectHandler>()?.ApplyEffect(barrierEffect);
            }
        }
        
        // TODO: In full implementation, create persistent zone object
    }
    
    private void ExecuteCurseZone(Vector3 position)
    {
        // Remove existing curse zone if any
        if (activeCurseZone != null)
        {
            Destroy(activeCurseZone);
        }
        
        Debug.Log($"{model.UnitName} creates Curse Zone at {position}");
        
        // Apply curse to enemies in the zone
        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var targetUnit in allUnits)
        {
            if (targetUnit.Faction != unit.Faction && 
                Vector3.Distance(position, targetUnit.transform.position) <= 3f)
            {
                var curseEffect = new StatusEffect
                {
                    effectName = "Shamanic Curse",
                    type = StatusEffectType.Debuff,
                    modifier = StatModifier.MagicResistance,
                    amount = -5,
                    duration = 3,
                    tags = { "Curse", "ShamanicZone" }
                };
                
                targetUnit.GetComponent<StatusEffectHandler>()?.ApplyEffect(curseEffect);
                
                // Deal initial damage
                int damage = 5 + model.MagicPower;
                targetUnit.Model.ApplyDamageWithBarrier(damage, DamageType.Magical);
            }
        }
        
        // TODO: In full implementation, create persistent zone object
    }
    
    private void ExecuteShamanicRitual()
    {
        if (!model.IsInAdrenalineState) return;
        
        Debug.Log($"{model.UnitName} performs Shamanic Ritual - enhancing all goblins!");
        
        // Grant buffs to all goblin allies
        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var targetUnit in allUnits)
        {
            if (targetUnit.Faction == unit.Faction && 
                Vector3.Distance(transform.position, targetUnit.transform.position) <= 6f)
            {
                // Grant strength buff
                var strengthBuff = new StatusEffect
                {
                    effectName = "Ritual Strength",
                    type = StatusEffectType.Buff,
                    modifier = StatModifier.Strength,
                    amount = 5,
                    duration = 3,
                    tags = { "Ritual", "ShamanicBuff" }
                };
                
                // Grant magic power buff
                var mpBuff = new StatusEffect
                {
                    effectName = "Ritual Power",
                    type = StatusEffectType.Buff,
                    modifier = StatModifier.MagicPower,
                    amount = 5,
                    duration = 3,
                    tags = { "Ritual", "ShamanicBuff" }
                };
                
                var statusHandler = targetUnit.GetComponent<StatusEffectHandler>();
                if (statusHandler != null)
                {
                    statusHandler.ApplyEffect(strengthBuff);
                    statusHandler.ApplyEffect(mpBuff);
                }
                
                // Heal allies
                targetUnit.Heal(10, unit);
            }
        }
    }
    
    private void ExecuteElementalStorm(Vector3 position)
    {
        if (!model.IsInAdrenalineState) return;
        
        Debug.Log($"{model.UnitName} summons Elemental Storm at {position}!");
        
        // Deal damage to all enemies in a large area
        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var targetUnit in allUnits)
        {
            if (targetUnit.Faction != unit.Faction && 
                Vector3.Distance(position, targetUnit.transform.position) <= 4f)
            {
                // Calculate storm damage
                int baseDamage = 15; 
                int totalDamage = baseDamage + model.MagicPower;
                
                targetUnit.Model.ApplyDamageWithBarrier(totalDamage, DamageType.Magical);
                
                // Apply random elemental effects
                int randomEffect = Random.Range(0, 3);
                StatusEffect elementalEffect = null;
                
                switch (randomEffect)
                {
                    case 0: // Fire - Burning
                        elementalEffect = new StatusEffect
                        {
                            effectName = "Storm Burn",
                            type = StatusEffectType.Debuff,
                            damagePerTurn = 3,
                            duration = 3,
                            tags = { "Burning", "Elemental" }
                        };
                        break;
                        
                    case 1: // Ice - Slow
                        elementalEffect = new StatusEffect
                        {
                            effectName = "Storm Freeze",
                            type = StatusEffectType.Debuff,
                            modifier = StatModifier.Performance,
                            amount = -6,
                            duration = 2,
                            tags = { "Frozen", "Elemental" }
                        };
                        break;
                        
                    case 2: // Lightning - Shock
                        elementalEffect = new StatusEffect
                        {
                            effectName = "Storm Shock",
                            type = StatusEffectType.Debuff,
                            modifier = StatModifier.Performance,
                            amount = -10,
                            duration = 1,
                            tags = { "Shocked", "Elemental" }
                        };
                        break;
                }
                
                if (elementalEffect != null)
                {
                    targetUnit.GetComponent<StatusEffectHandler>()?.ApplyEffect(elementalEffect);
                }
            }
        }
    }

    public override void OnDamageTaken(int damage, DamageType damageType, Unit attacker)
    {
        base.OnDamageTaken(damage, damageType, attacker);
        
        // Gain adrenaline when taking damage
        model.AddAdrenaline(3, "taking damage");
        
        // Gain adrenaline when using supportive abilities
        // This would be tracked in the ability execution methods
    }
    
    // Clean up zones when unit dies
    void OnDestroy()
    {
        if (activeBarrierZone != null)
        {
            Destroy(activeBarrierZone);
        }
        
        if (activeCurseZone != null)
        {
            Destroy(activeCurseZone);
        }
    }
}