using UnityEngine;

public class GoblinController : UnitController
{
    [Header("Goblin Specific")]
    private bool hasUsedGoblinFlurry = false;
    
    protected override void HandleAdrenalineStateEntered()
    {
        base.HandleAdrenalineStateEntered();
        
        // Gain low affinity buff when entering adrenaline state
        var affinityBuff = new StatusEffect
        {
            effectName = "Adrenaline Rush",
            type = StatusEffectType.Buff,
            modifier = StatModifier.Affinity,
            amount = 5,
            duration = -1 // Permanent while in adrenaline state
        };
        
        GetComponent<StatusEffectHandler>()?.ApplyEffect(affinityBuff);
    }
    
    protected override void HandleAdrenalineStateExited()
    {
        base.HandleAdrenalineStateExited();
        
        // Remove adrenaline buffs
        GetComponent<StatusEffectHandler>()?.RemoveStatusEffectByName("Adrenaline Rush");
    }
    
    protected override void OnAbilityExecuted(UnitAbility ability, Unit target)
    {
        base.OnAbilityExecuted(ability, target);
        
        switch (ability.abilityName)
        {
            case "Goblin Slash":
                ExecuteGoblinSlash(target);
                break;
            case "Goblin Stab":
                ExecuteGoblinStab(target);
                break;
            case "Goblin Flurry":
                ExecuteGoblinFlurry(target);
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
        
        // Gain adrenaline for hitting enemies
        if (target.Faction != unit.Faction)
        {
            model.AddAdrenaline(3, "hitting enemy with Goblin Slash");
        }
    }
    
    private void ExecuteGoblinStab(Unit target)
    {
        if (target == null) return;
        
        var ability = GetAbilityByName("Goblin Stab");
        if (ability == null) return;
        
        int damage = ability.CalculateTotalDamage(model, target.Model);
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        // Apply leg stab slow effect
        var slowEffect = new StatusEffect
        {
            effectName = "Leg Stab",
            type = StatusEffectType.Debuff,
            modifier = StatModifier.Performance,
            amount = -5,
            duration = 2,
            tags = { "MovementReduced" }
        };
        
        target.GetComponent<StatusEffectHandler>()?.ApplyEffect(slowEffect);
        
        // Gain adrenaline for hitting enemies
        if (target.Faction != unit.Faction)
        {
            model.AddAdrenaline(3, "hitting enemy with Goblin Stab");
        }
    }
    
    private void ExecuteGoblinFlurry(Unit target)
    {
        if (target == null || !model.IsInAdrenalineState) return;
        
        var ability = GetAbilityByName("Goblin Flurry");
        if (ability == null) return;
        
        // First attack
        int damage1 = ability.CalculateTotalDamage(model, target.Model);
        bool firstHit = Random.Range(0f, 100f) <= ability.baseHitChance;
        
        if (firstHit)
        {
            target.Model.ApplyDamageWithBarrier(damage1, ability.GetDamageType());
            
            // Second attack only if first hits
            int damage2 = Mathf.RoundToInt(damage1 * 0.5f);
            target.Model.ApplyDamageWithBarrier(damage2, ability.GetDamageType());
            
            Debug.Log($"{model.UnitName} hits with both attacks of Goblin Flurry!");
            
            // Extra adrenaline for successful flurry
            if (target.Faction != unit.Faction)
            {
                model.AddAdrenaline(5, "successful Goblin Flurry");
            }
        }
        else
        {
            Debug.Log($"{model.UnitName}'s Goblin Flurry misses!");
        }
        
        hasUsedGoblinFlurry = true;
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        hasUsedGoblinFlurry = false;
    }

    // Check if goblin can use follow-up abilities after flurry
    public bool CanUseFlurryFollowUp()
    {
        return hasUsedGoblinFlurry;
    }

    public override void OnDamageTaken(int damage, DamageType damageType, Unit attacker)
    {
        base.OnDamageTaken(damage, damageType, attacker);
        
        // Gain adrenaline when taking damage
        model.AddAdrenaline(5, "taking damage");
        
        // Gain more adrenaline when LP is low (15%)
        if (model.CurrentHP <= model.MaxHP * 0.15f)
        {
            model.AddAdrenaline(15, "low health");
        }
    }
}