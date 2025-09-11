using System.Collections.Generic;
using UnityEngine;

public class SorcererController : UnitController
{
    [Header("Sorcerer Specific")]
    private bool isChanneling = false;
    private string channeledSpell = "";
    private int channelTurnsRemaining = 0;
    
    protected override void Start()
    {
        base.Start();
        
        // Initialize with 0 power stacks
        model.SetRes("PowerStacks", 0);
    }
    
    protected override void HandleAdrenalineStateEntered()
    {
        base.HandleAdrenalineStateEntered();
        
        // Gain magic power buff when entering adrenaline state
        var mpBuff = new StatusEffect
        {
            effectName = "Arcane Mastery",
            type = StatusEffectType.Buff,
            modifier = StatModifier.MagicPower,
            amount = 10,
            duration = -1 // Permanent while in adrenaline state
        };
        
        GetComponent<StatusEffectHandler>()?.ApplyEffect(mpBuff);
    }
    
    protected override void HandleAdrenalineStateExited()
    {
        base.HandleAdrenalineStateExited();
        
        // Remove adrenaline buffs
        GetComponent<StatusEffectHandler>()?.RemoveStatusEffectByName("Arcane Mastery");
    }
    
    protected override void OnAbilityExecuted(UnitAbility ability, Unit target)
    {
        base.OnAbilityExecuted(ability, target);
        
        switch (ability.abilityName)
        {
            case "Sorcerer Staff":
                ExecuteSorcererStaff(target);
                break;
            case "Magic Missile":
                ExecuteMagicMissile(target);
                break;
            case "Power Build":
                ExecutePowerBuild();
                break;
            case "Channel Spell":
                ExecuteChannelSpell(ability);
                break;
            case "Fireball":
                ExecuteFireball(target?.transform.position ?? transform.position + Vector3.right * 5f);
                break;
            case "Lightning Storm":
                ExecuteLightningStorm(target?.transform.position ?? transform.position + Vector3.right * 5f);
                break;
            case "Meteor":
                ExecuteMeteor(target?.transform.position ?? transform.position + Vector3.right * 5f);
                break;
        }
    }
    
    private void ExecuteSorcererStaff(Unit target)
    {
        if (target == null) return;
        
        var ability = GetAbilityByName("Sorcerer Staff");
        if (ability == null) return;
        
        int damage = ability.CalculateTotalDamage(model, target.Model);
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        // Gain power stack for basic attack
        AddPowerStack("staff attack");
    }
    
    private void ExecuteMagicMissile(Unit target)
    {
        if (target == null) return;
        
        var ability = GetAbilityByName("Magic Missile");
        if (ability == null) return;
        
        int baseDamage = ability.CalculateTotalDamage(model, target.Model);
        
        // Add power stack bonus damage
        int powerStacks = model.GetRes("PowerStacks");
        int bonusDamage = powerStacks * 3; // Each stack adds 3 damage
        
        int totalDamage = baseDamage + bonusDamage;
        target.Model.ApplyDamageWithBarrier(totalDamage, ability.GetDamageType());
        
        Debug.Log($"Magic Missile: {baseDamage} base + {bonusDamage} power bonus = {totalDamage} total damage!");
        
        // Gain power stack for casting spell
        AddPowerStack("casting Magic Missile");
        
        // Gain adrenaline for hitting enemies
        if (target.Faction != unit.Faction)
        {
            model.AddAdrenaline(5, "hitting with spell");
        }
    }
    
    private void ExecutePowerBuild()
    {
        Debug.Log($"{model.UnitName} focuses arcane energy!");
        
        // Gain 2 power stacks immediately
        AddPowerStack("power build");
        AddPowerStack("power build");
        
        // Gain temporary magic power buff
        var focusBuff = new StatusEffect
        {
            effectName = "Arcane Focus",
            type = StatusEffectType.Buff,
            modifier = StatModifier.MagicPower,
            amount = 5,
            duration = 3,
            tags = { "Arcane", "Focus" }
        };
        
        GetComponent<StatusEffectHandler>()?.ApplyEffect(focusBuff);
        
        // Gain adrenaline for building power
        model.AddAdrenaline(8, "building arcane power");
    }
    
    private void ExecuteChannelSpell(UnitAbility channeledAbility)
    {
        if (isChanneling)
        {
            Debug.Log($"{model.UnitName} is already channeling a spell!");
            return;
        }
        
        // Start channeling based on the spell being channeled
        string spellName = "Fireball"; // Default, would be determined by UI selection
        
        // Different spells have different channel times
        int channelTime = spellName switch
        {
            "Fireball" => 1,
            "Lightning Storm" => 2,
            "Meteor" => 3,
            _ => 1
        };
        
        isChanneling = true;
        channeledSpell = spellName;
        channelTurnsRemaining = channelTime;
        
        Debug.Log($"{model.UnitName} begins channeling {spellName} ({channelTime} turns)!");
        
        // Apply channeling effect
        var channelingEffect = new StatusEffect
        {
            effectName = "Channeling",
            type = StatusEffectType.Buff,
            modifier = StatModifier.MagicPower,
            amount = 5, // Bonus while channeling
            duration = channelTime,
            tags = { "Channeling", "Concentration" }
        };
        
        GetComponent<StatusEffectHandler>()?.ApplyEffect(channelingEffect);
    }
    
    private void ExecuteFireball(Vector3 targetPosition)
    {
        Debug.Log($"{model.UnitName} casts Fireball at {targetPosition}!");
        
        int powerStacks = model.GetRes("PowerStacks");
        
        // Base fireball damage
        int baseDamage = 15; // Medium damage
        int totalDamage = baseDamage + model.MagicPower + (powerStacks * 2);
        
        // Hit all enemies in area
        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var target in allUnits)
        {
            if (target.Faction != unit.Faction && 
                Vector3.Distance(targetPosition, target.transform.position) <= 2f)
            {
                target.Model.ApplyDamageWithBarrier(totalDamage, DamageType.Magical);
                
                // Apply burning effect
                var burnEffect = new StatusEffect
                {
                    effectName = "Burning",
                    type = StatusEffectType.Debuff,
                    damagePerTurn = 5 + powerStacks,
                    duration = 3,
                    tags = { "Burning", "Fire" }
                };
                
                target.GetComponent<StatusEffectHandler>()?.ApplyEffect(burnEffect);
            }
        }
        
        // Consume power stacks
        model.SetRes("PowerStacks", 0);
        Debug.Log($"Fireball consumes {powerStacks} power stacks for enhanced effect!");
        
        // Gain adrenaline for powerful spell
        model.AddAdrenaline(10, "casting Fireball");
    }
    
    private void ExecuteLightningStorm(Vector3 targetPosition)
    {
        if (!model.IsInAdrenalineState) return;
        
        Debug.Log($"{model.UnitName} unleashes Lightning Storm!");
        
        int powerStacks = model.GetRes("PowerStacks");
        int baseDamage = 20; // High damage
        int totalDamage = baseDamage + model.MagicPower + (powerStacks * 3);
        
        // Create multiple lightning strikes
        var allEnemies = new List<Unit>();
        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        foreach (var target in allUnits)
        {
            if (target.Faction != unit.Faction && target.IsAlive)
            {
                allEnemies.Add(target);
            }
        }
        
        // Hit up to 3 enemies with chain lightning
        int strikes = Mathf.Min(3 + powerStacks, allEnemies.Count);
        
        for (int i = 0; i < strikes; i++)
        {
            if (i < allEnemies.Count)
            {
                var target = allEnemies[i];
                int strikeDamage = Mathf.RoundToInt(totalDamage * (1f - i * 0.15f)); // Decreasing damage
                
                target.Model.ApplyDamageWithBarrier(strikeDamage, DamageType.Magical);
                
                // Apply shock effect
                var shockEffect = new StatusEffect
                {
                    effectName = "Shocked",
                    type = StatusEffectType.Debuff,
                    modifier = StatModifier.Performance,
                    amount = -8,
                    duration = 2,
                    tags = { "Shocked", "Lightning" }
                };
                
                target.GetComponent<StatusEffectHandler>()?.ApplyEffect(shockEffect);
                
                Debug.Log($"Lightning strike {i + 1} hits {target.Model.UnitName} for {strikeDamage} damage!");
            }
        }
        
        // Consume power stacks
        model.SetRes("PowerStacks", 0);
        
        // Gain massive adrenaline
        model.AddAdrenaline(15, "Lightning Storm devastation");
    }
    
    private void ExecuteMeteor(Vector3 targetPosition)
    {
        if (!model.IsInAdrenalineState) return;
        
        Debug.Log($"{model.UnitName} calls down a METEOR!");
        
        int powerStacks = model.GetRes("PowerStacks");
        int baseDamage = 40; // Heavy damage
        int totalDamage = baseDamage + (model.MagicPower * 2) + (powerStacks * 5);
        
        // Massive area of effect
        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var target in allUnits)
        {
            float distance = Vector3.Distance(targetPosition, target.transform.position);
            
            if (target.Faction != unit.Faction && distance <= 4f)
            {
                // Damage decreases with distance
                float damageMultiplier = 1f - (distance / 4f * 0.5f);
                int actualDamage = Mathf.RoundToInt(totalDamage * damageMultiplier);
                
                target.Model.ApplyDamageWithBarrier(actualDamage, DamageType.Magical);
                
                // Apply devastated effect
                var devastatedEffect = new StatusEffect
                {
                    effectName = "Devastated",
                    type = StatusEffectType.Debuff,
                    modifier = StatModifier.Armor,
                    amount = -10,
                    duration = 3,
                    tags = { "Devastated", "Meteor" }
                };
                
                target.GetComponent<StatusEffectHandler>()?.ApplyEffect(devastatedEffect);
                
                Debug.Log($"METEOR hits {target.Model.UnitName} for {actualDamage} damage!");
            }
        }
        
        // Consume ALL power stacks for maximum effect
        Debug.Log($"Meteor consumes all {powerStacks} power stacks!");
        model.SetRes("PowerStacks", 0);
        
        // Gain massive adrenaline
        model.AddAdrenaline(25, "METEOR devastation");
    }
    
    private void AddPowerStack(string reason)
    {
        int currentStacks = model.GetRes("PowerStacks");
        int maxStacks = 10; // Maximum power stacks
        
        if (currentStacks < maxStacks)
        {
            model.AddRes("PowerStacks", 1);
            Debug.Log($"{model.UnitName} gains power stack ({reason}). Total: {currentStacks + 1}/{maxStacks}");
            
            // Visual effect would go here in full implementation
        }
        else
        {
            Debug.Log($"{model.UnitName} is at maximum power stacks ({maxStacks})!");
        }
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        
        // Handle channeling
        if (isChanneling)
        {
            channelTurnsRemaining--;
            Debug.Log($"{model.UnitName} continues channeling {channeledSpell} ({channelTurnsRemaining} turns remaining)");
            
            if (channelTurnsRemaining <= 0)
            {
                CompleteChanneledSpell();
            }
        }
        
        // Gain power stack at start of turn if not at max
        if (model.GetRes("PowerStacks") < 10)
        {
            AddPowerStack("turn start");
        }
    }
    
    private void CompleteChanneledSpell()
    {
        Debug.Log($"{model.UnitName} completes channeling {channeledSpell}!");
        
        isChanneling = false;
        GetComponent<StatusEffectHandler>()?.RemoveStatusEffectByName("Channeling");
        
        // The channeled spell would be automatically cast here
        // For now, just give bonus power stacks
        AddPowerStack("completed channeling");
        AddPowerStack("completed channeling");
        
        channeledSpell = "";
        channelTurnsRemaining = 0;
    }

    public override void OnDamageTaken(int damage, DamageType damageType, Unit attacker)
    {
        base.OnDamageTaken(damage, damageType, attacker);
        
        // Gain adrenaline when taking damage
        model.AddAdrenaline(3, "taking damage");
        
        // Interrupt channeling if damaged significantly
        if (isChanneling && damage >= model.MaxHP * 0.15f)
        {
            Debug.Log($"{model.UnitName}'s channeling is interrupted by damage!");
            InterruptChanneling();
        }
        
        // Gain power stack when taking damage (arcane feedback)
        AddPowerStack("taking damage");
    }
    
    private void InterruptChanneling()
    {
        isChanneling = false;
        channeledSpell = "";
        channelTurnsRemaining = 0;
        
        GetComponent<StatusEffectHandler>()?.RemoveStatusEffectByName("Channeling");
        
        // Lose some power stacks due to interruption
        int currentStacks = model.GetRes("PowerStacks");
        int lostStacks = Mathf.Min(3, currentStacks);
        model.AddRes("PowerStacks", -lostStacks);
        
        Debug.Log($"Channeling interrupted! Lost {lostStacks} power stacks.");
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        
        // Sorcerer gains power stacks at end of turn if they didn't cast major spells
        if (!HasCastMajorSpellThisTurn())
        {
            AddPowerStack("turn end buildup");
        }
    }
    
    private bool HasCastMajorSpellThisTurn()
    {
        // This would track if major spells were cast this turn
        // For now, simplified check
        return model.CurrentActions < model.MaxActions;
    }
    
    // Clean up when unit dies
    void OnDestroy()
    {
        if (isChanneling)
        {
            InterruptChanneling();
        }
    }
}