using UnityEngine;

public class HobGoblinController : UnitController
{
    [Header("HobGoblin Specific")]
    private bool hasUsedIncapacitate = false;
    
    protected override void Start()
    {
        base.Start();
        
        // Initialize with 3 spears
        model.SetRes("Spears", 3);
    }
    
    protected override void HandleAdrenalineStateEntered()
    {
        base.HandleAdrenalineStateEntered();
        
        // Gain low MP buff when entering adrenaline state
        var mpBuff = new StatusEffect
        {
            effectName = "Tactical Leadership",
            type = StatusEffectType.Buff,
            modifier = StatModifier.MagicPower,
            amount = 3,
            duration = -1 // Permanent while in adrenaline state
        };
        
        GetComponent<StatusEffectHandler>()?.ApplyEffect(mpBuff);
    }
    
    protected override void HandleAdrenalineStateExited()
    {
        base.HandleAdrenalineStateExited();
        
        // Remove adrenaline buffs
        GetComponent<StatusEffectHandler>()?.RemoveStatusEffectByName("Tactical Leadership");
    }
    
    protected override void OnAbilityExecuted(UnitAbility ability, Unit target)
    {
        base.OnAbilityExecuted(ability, target);
        
        switch (ability.abilityName)
        {
            case "Goblin Slash":
                ExecuteGoblinSlash(target);
                break;
            case "Spear Throw":
                ExecuteSpearThrow(target);
                break;
            case "Replenish":
                ExecuteReplenish();
                break;
            case "Incapacitate":
                ExecuteIncapacitate(target);
                break;
            case "Goblin Tactics":
                ExecuteGoblinTactics();
                break;
            case "Multiple Spear Throw":
                ExecuteMultipleSpearThrow(target);
                break;
        }
    }
    
    private void ExecuteGoblinSlash(Unit target)
    {
        if (target == null) return;
        
        var ability = GetAbilityByName("Goblin Slash");
        if (ability == null) return;
        
        int damage = ability.CalculateTotalDamage(model, target.Model);
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
    }
    
    private void ExecuteSpearThrow(Unit target)
    {
        if (target == null || !model.TryConsume("Spears", 1)) return;
        
        var ability = GetAbilityByName("Spear Throw");
        if (ability == null) return;
        
        int damage = ability.CalculateTotalDamage(model, target.Model);
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        // Gain adrenaline for using spear throw
        if (target.Faction != unit.Faction)
        {
            model.AddAdrenaline(5, "spear throw hit");
        }
        
        Debug.Log($"{model.UnitName} throws spear at {target.Model.UnitName}. Spears remaining: {model.GetRes("Spears")}");
    }
    
    private void ExecuteReplenish()
    {
        model.SetRes("Spears", 3);
        Debug.Log($"{model.UnitName} replenishes spears to maximum (3)");
    }
    
    private void ExecuteIncapacitate(Unit target)
    {
        if (target == null) return;
        
        var ability = GetAbilityByName("Incapacitate");
        if (ability == null) return;
        
        int damage = ability.CalculateTotalDamage(model, target.Model);
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        // Apply incapacitate effect
        var incapacitateEffect = new StatusEffect
        {
            effectName = "Incapacitated",
            type = StatusEffectType.Debuff,
            modifier = StatModifier.Performance,
            amount = -8,
            duration = 1,
            tags = { "Incapacitated" }
        };
        
        target.GetComponent<StatusEffectHandler>()?.ApplyEffect(incapacitateEffect);
        
        // Gain adrenaline for using incapacitate
        if (target.Faction != unit.Faction)
        {
            model.AddAdrenaline(8, "incapacitate hit");
        }
        
        hasUsedIncapacitate = true;
        Debug.Log($"{model.UnitName} can now use Goblin Flee (free movement)");
    }
    
    private void ExecuteGoblinTactics()
    {
        if (!model.IsInAdrenalineState) return;
        
        // Find all goblin allies in range and apply haste
        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var targetUnit in allUnits)
        {
            if (targetUnit.Faction == unit.Faction && 
                Vector3.Distance(transform.position, targetUnit.transform.position) <= 8f)
            {
                var hasteEffect = new StatusEffect
                {
                    effectName = "Tactical Haste",
                    type = StatusEffectType.Buff,
                    modifier = StatModifier.Performance,
                    amount = 5,
                    duration = 2,
                    tags = { "Haste" }
                };
                
                targetUnit.GetComponent<StatusEffectHandler>()?.ApplyEffect(hasteEffect);
            }
        }
        
        Debug.Log($"{model.UnitName} uses Goblin Tactics - all nearby goblins gain Haste!");
    }
    
    private void ExecuteMultipleSpearThrow(Unit target)
    {
        if (target == null || !model.IsInAdrenalineState || !model.TryConsume("Spears", 2)) return;
        
        var ability = GetAbilityByName("Multiple Spear Throw");
        if (ability == null) return;
        
        // First spear
        int damage = ability.CalculateTotalDamage(model, target.Model);
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        // Second spear (could target different enemy in full implementation)
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        Debug.Log($"{model.UnitName} throws multiple spears! Spears remaining: {model.GetRes("Spears")}");
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        hasUsedIncapacitate = false;
    }
    
    // Goblin Flee - free movement after incapacitate
    public bool CanUseGoblinFlee()
    {
        return hasUsedIncapacitate;
    }
    
    public void UseGoblinFlee(Vector3 targetPosition)
    {
        if (!CanUseGoblinFlee()) return;
        
        // Free movement without spending actions
        transform.position = targetPosition;
        Debug.Log($"{model.UnitName} uses Goblin Flee to escape!");
    }
}