using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RogueController : UnitController
{
    [Header("Rogue Specific")]
    private bool isInShadows = false;
    private bool hasDualWielded = false;
    private List<Unit> bleedingEnemies = new List<Unit>();
    
    protected override void HandleAdrenalineStateEntered()
    {
        base.HandleAdrenalineStateEntered();
        
        // Gain affinity buff when entering adrenaline state
        var affinityBuff = new StatusEffect
        {
            effectName = "Shadow Focus",
            type = StatusEffectType.Buff,
            modifier = StatModifier.Affinity,
            amount = 10,
            duration = -1 // Permanent while in adrenaline state
        };
        
        GetComponent<StatusEffectHandler>()?.ApplyEffect(affinityBuff);
    }
    
    protected override void HandleAdrenalineStateExited()
    {
        base.HandleAdrenalineStateExited();
        
        // Remove adrenaline buffs
        GetComponent<StatusEffectHandler>()?.RemoveStatusEffectByName("Shadow Focus");
    }
    
    protected override void OnAbilityExecuted(UnitAbility ability, Unit target)
    {
        base.OnAbilityExecuted(ability, target);
        
        switch (ability.abilityName)
        {
            case "Rogue Blade":
                ExecuteRogueBlade(target);
                break;
            case "Dual Wield":
                ExecuteDualWield(target);
                break;
            case "Shadow Step":
                ExecuteShadowStep(target?.transform.position ?? transform.position + Vector3.right * 3f);
                break;
            case "Bleeding Cut":
                ExecuteBleedingCut(target);
                break;
            case "Shadow Assault":
                ExecuteShadowAssault(target);
                break;
            case "Execute":
                ExecuteExecute(target);
                break;
        }
    }
    
    private void ExecuteRogueBlade(Unit target)
    {
        if (target == null) return;
        
        var ability = GetAbilityByName("Rogue Blade");
        if (ability == null) return;
        
        int damage = ability.CalculateTotalDamage(model, target.Model);
        
        // Bonus damage if attacking from shadows or flanking
        if (isInShadows || IsFlankingTarget(target))
        {
            damage = Mathf.RoundToInt(damage * 1.3f); // 30% bonus
            Debug.Log($"{model.UnitName} attacks from advantage for bonus damage!");
        }
        
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        // Gain adrenaline for hitting enemies
        if (target.Faction != unit.Faction)
        {
            model.AddAdrenaline(3, "hitting enemy");
        }
    }
    
    private void ExecuteDualWield(Unit target)
    {
        if (target == null || hasDualWielded) return;
        
        var ability = GetAbilityByName("Dual Wield");
        if (ability == null) return;
        
        Debug.Log($"{model.UnitName} attacks with both weapons!");
        
        // First attack (main hand)
        int damage1 = ability.CalculateTotalDamage(model, target.Model);
        target.Model.ApplyDamageWithBarrier(damage1, ability.GetDamageType());
        
        // Second attack (off hand) - reduced damage
        int damage2 = Mathf.RoundToInt(damage1 * 0.7f);
        target.Model.ApplyDamageWithBarrier(damage2, ability.GetDamageType());
        
        Debug.Log($"Dual Wield: {damage1} + {damage2} damage!");
        
        hasDualWielded = true;
        
        // Gain adrenaline for dual wielding
        if (target.Faction != unit.Faction)
        {
            model.AddAdrenaline(8, "dual wield combo");
        }
    }
    
    private void ExecuteShadowStep(Vector3 targetPosition)
    {
        Debug.Log($"{model.UnitName} uses Shadow Step!");
        
        // Teleport to target position
        Vector3 oldPosition = transform.position;
        transform.position = targetPosition;
        
        // Enter shadows for 2 turns
        isInShadows = true;
        
        var shadowEffect = new StatusEffect
        {
            effectName = "In Shadows",
            type = StatusEffectType.Buff,
            modifier = StatModifier.Affinity,
            amount = 15, // Harder to hit
            duration = 2,
            tags = { "Stealth", "Shadow" }
        };
        
        GetComponent<StatusEffectHandler>()?.ApplyEffect(shadowEffect);
        
        // Schedule shadow exit
        StartCoroutine(ExitShadowsAfterTurns(2));
        
        // Gain adrenaline for mobility
        model.AddAdrenaline(5, "shadow step");
        
        Debug.Log($"{model.UnitName} enters the shadows and gains stealth!");
    }
    
    private IEnumerator ExitShadowsAfterTurns(int turns)
    {
        for (int i = 0; i < turns; i++)
        {
            yield return new WaitUntil(() => !model.CanAct()); // Wait for turn to end
            yield return new WaitUntil(() => model.CanAct()); // Wait for next turn to start
        }
        
        isInShadows = false;
        GetComponent<StatusEffectHandler>()?.RemoveStatusEffectByName("In Shadows");
        Debug.Log($"{model.UnitName} emerges from the shadows.");
    }
    
    private void ExecuteBleedingCut(Unit target)
    {
        if (target == null || target.Faction == unit.Faction) return;
        
        var ability = GetAbilityByName("Bleeding Cut");
        if (ability == null) return;
        
        int damage = ability.CalculateTotalDamage(model, target.Model);
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        // Apply bleeding effect
        var bleedEffect = new StatusEffect
        {
            effectName = "Bleeding",
            type = StatusEffectType.Debuff,
            damagePerTurn = 5 + Mathf.RoundToInt(model.Strength * 0.3f),
            duration = 3,
            tags = { "Bleeding", "DoT" }
        };
        
        target.GetComponent<StatusEffectHandler>()?.ApplyEffect(bleedEffect);
        
        // Add to bleeding enemies list
        if (!bleedingEnemies.Contains(target))
        {
            bleedingEnemies.Add(target);
        }
        
        Debug.Log($"{target.Model.UnitName} is now bleeding!");
        
        // Gain adrenaline for applying bleed
        model.AddAdrenaline(5, "inflicting bleeding");
    }
    
    private void ExecuteShadowAssault(Unit target)
    {
        if (target == null || !model.IsInAdrenalineState) return;
        
        Debug.Log($"{model.UnitName} performs Shadow Assault on {target.Model.UnitName}!");
        
        var ability = GetAbilityByName("Shadow Assault");
        if (ability == null) return;
        
        // Teleport behind target
        Vector3 behindTarget = target.transform.position - target.transform.right * 2f;
        transform.position = behindTarget;
        
        // Multiple strikes
        for (int i = 0; i < 3; i++)
        {
            int damage = ability.CalculateTotalDamage(model, target.Model);
            
            // Each strike has decreasing damage
            damage = Mathf.RoundToInt(damage * (1f - i * 0.2f));
            
            target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
            Debug.Log($"Shadow Assault strike {i + 1}: {damage} damage!");
        }
        
        // Apply shadow mark
        var shadowMark = new StatusEffect
        {
            effectName = "Shadow Mark",
            type = StatusEffectType.Debuff,
            modifier = StatModifier.Armor,
            amount = -10,
            duration = 3,
            tags = { "Shadow", "Vulnerable" }
        };
        
        target.GetComponent<StatusEffectHandler>()?.ApplyEffect(shadowMark);
    }
    
    private void ExecuteExecute(Unit target)
    {
        if (target == null || !model.IsInAdrenalineState || target.Faction == unit.Faction) return;
        
        // Check if target is low on health (25% or less)
        float healthPercentage = (float)target.Model.CurrentHP / target.Model.MaxHP;
        
        if (healthPercentage > 0.25f)
        {
            Debug.Log($"Execute failed - {target.Model.UnitName} has too much health ({healthPercentage:P0})!");
            return;
        }
        
        Debug.Log($"{model.UnitName} executes {target.Model.UnitName}!");
        
        var ability = GetAbilityByName("Execute");
        if (ability == null) return;
        
        // Calculate execute damage (high base + missing health)
        int baseDamage = ability.CalculateTotalDamage(model, target.Model);
        int missingHealth = target.Model.MaxHP - target.Model.CurrentHP;
        int totalDamage = baseDamage + missingHealth;
        
        target.Model.ApplyDamageWithBarrier(totalDamage, ability.GetDamageType());
        
        Debug.Log($"Execute deals {totalDamage} damage ({baseDamage} base + {missingHealth} missing health)!");
        
        // If target dies, gain massive adrenaline boost
        if (!target.IsAlive)
        {
            model.AddAdrenaline(25, "successful execution");
            Debug.Log($"{model.UnitName} gains massive adrenaline from successful execution!");
        }
    }
    
    // Check if rogue is flanking the target
    private bool IsFlankingTarget(Unit target)
    {
        if (target == null) return false;
        
        // Simple flanking check: is rogue behind or to the side of target?
        Vector3 toRogue = (transform.position - target.transform.position).normalized;
        Vector3 targetFacing = target.transform.right; // Assuming right is forward
        
        float dot = Vector3.Dot(toRogue, targetFacing);
        return dot > 0.3f; // Flanking if coming from side or behind
    }

    public override void OnDamageTaken(int damage, DamageType damageType, Unit attacker)
    {
        base.OnDamageTaken(damage, damageType, attacker);
        
        // Gain adrenaline when taking damage
        model.AddAdrenaline(3, "taking damage");
        
        // If in shadows, chance to avoid some damage
        if (isInShadows && Random.Range(0f, 100f) <= 30f)
        {
            Debug.Log($"{model.UnitName} partially avoids damage due to shadows!");
            // This would need to be handled in the damage calculation system
        }
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        hasDualWielded = false;
        
        // Check bleeding enemies for adrenaline gain
        int bleedingCount = 0;
        foreach (var enemy in bleedingEnemies)
        {
            if (enemy != null && enemy.IsAlive)
            {
                var statusHandler = enemy.GetComponent<StatusEffectHandler>();
                if (statusHandler != null && statusHandler.HasStatusEffect("Bleeding"))
                {
                    bleedingCount++;
                }
            }
        }
        
        if (bleedingCount > 0)
        {
            model.AddAdrenaline(bleedingCount * 2, $"{bleedingCount} enemies bleeding");
        }
        
        // Clean up bleeding list
        bleedingEnemies.RemoveAll(enemy => enemy == null || !enemy.IsAlive);
    }
    
    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        hasDualWielded = false;
    }
    
    // Rogue's reaction: Shadow Strike (counter-attack)
    public void UseShadowStrike(Unit attacker)
    {
        if (!model.CanReact() || attacker.Faction == unit.Faction) return;
        
        if (isInShadows) // Only available while in shadows
        {
            Debug.Log($"{model.UnitName} counter-attacks with Shadow Strike!");
            
            model.SpendReaction();
            
            // Quick counter-attack
            var basicAttack = GetAbilityByName("Rogue Blade");
            if (basicAttack != null)
            {
                int damage = basicAttack.CalculateTotalDamage(model, attacker.Model);
                damage = Mathf.RoundToInt(damage * 1.5f); // Bonus damage for surprise
                attacker.Model.ApplyDamageWithBarrier(damage, basicAttack.GetDamageType());
                
                Debug.Log($"Shadow Strike deals {damage} surprise damage!");
                
                // Gain adrenaline for successful counter
                model.AddAdrenaline(8, "shadow strike counter");
            }
        }
    }
    
    // Clean up when unit dies
    void OnDestroy()
    {
        StopAllCoroutines(); // Stop shadow timers
        bleedingEnemies.Clear();
    }
}