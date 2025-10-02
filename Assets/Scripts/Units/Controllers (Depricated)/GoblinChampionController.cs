using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoblinChampionController : UnitController
{
    [Header("Goblin Champion Specific")]
    private bool isInRageState = false;
    private List<Unit> tauntedEnemies = new List<Unit>();
    
    protected override void Start()
    {
        base.Start();
        
        // Initialize with 0 dead goblins
        model.SetRes("DeadGoblins", 0);
    }
    
    protected override void HandleAdrenalineStateEntered()
    {
        base.HandleAdrenalineStateEntered();
        
        // Gain armor buff when entering adrenaline state
        var armorBuff = new StatusEffect
        {
            effectName = "Champion's Resilience",
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
        GetComponent<StatusEffectHandler>()?.RemoveStatusEffectByName("Champion's Resilience");
    }
    
    protected override void OnAbilityExecuted(UnitAbility ability, Unit target)
    {
        base.OnAbilityExecuted(ability, target);
        
        switch (ability.abilityName)
        {
            case "Champion Strike":
                ExecuteChampionStrike(target);
                break;
            case "Rage":
                ExecuteRage();
                break;
            case "Taunt":
                ExecuteTaunt(target);
                break;
            case "Goblin Fury":
                ExecuteGoblinFury();
                break;
            case "Devastating Charge":
                ExecuteDevastatingCharge(target);
                break;
        }
    }
    
    private void ExecuteChampionStrike(Unit target)
    {
        if (target == null) return;
        
        var ability = GetAbilityByName("Champion Strike");
        if (ability == null) return;
        
        int damage = ability.CalculateTotalDamage(model, target.Model);
        
        // Double damage if in rage state
        if (isInRageState)
        {
            damage *= 2;
            Debug.Log($"{model.UnitName} deals double damage due to Rage!");
        }
        
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        // Gain adrenaline for hitting enemies
        if (target.Faction != unit.Faction)
        {
            model.AddAdrenaline(5, "hitting enemy with Champion Strike");
        }
    }
    
    private void ExecuteRage()
    {
        if (isInRageState)
        {
            Debug.Log($"{model.UnitName} is already in Rage state!");
            return;
        }
        
        isInRageState = true;
        Debug.Log($"{model.UnitName} enters Rage state - double damage for 3 turns!");
        
        // Apply rage buff
        var rageBuff = new StatusEffect
        {
            effectName = "Champion Rage",
            type = StatusEffectType.Buff,
            modifier = StatModifier.Strength,
            amount = 10,
            duration = 3,
            tags = { "Rage", "ChampionBuff" }
        };
        
        GetComponent<StatusEffectHandler>()?.ApplyEffect(rageBuff);
        
        // Schedule rage end
        StartCoroutine(EndRageAfterTurns(3));
    }
    
    private IEnumerator EndRageAfterTurns(int turns)
    {
        for (int i = 0; i < turns; i++)
        {
            yield return new WaitUntil(() => !model.CanAct()); // Wait for turn to end
            yield return new WaitUntil(() => model.CanAct()); // Wait for next turn to start
        }
        
        isInRageState = false;
        GetComponent<StatusEffectHandler>()?.RemoveStatusEffectByName("Champion Rage");
        Debug.Log($"{model.UnitName} exits Rage state.");
    }
    
    private void ExecuteTaunt(Unit target)
    {
        if (target == null || target.Faction == unit.Faction) return;
        
        Debug.Log($"{model.UnitName} taunts {target.Model.UnitName}!");
        
        // Apply taunt effect
        var tauntEffect = new StatusEffect
        {
            effectName = "Taunted",
            type = StatusEffectType.Debuff,
            modifier = StatModifier.Affinity,
            amount = -10, // Reduced accuracy against other targets
            duration = 2,
            tags = { "Taunted", "MentalEffect" }
        };
        
        target.GetComponent<StatusEffectHandler>()?.ApplyEffect(tauntEffect);
        
        // Add to taunted list
        if (!tauntedEnemies.Contains(target))
        {
            tauntedEnemies.Add(target);
        }
        
        // Gain adrenaline for taunting
        model.AddAdrenaline(5, "taunting enemy");
    }
    
    private void ExecuteGoblinFury()
    {
        if (!model.IsInAdrenalineState) return;
        
        int deadGoblins = model.GetRes("DeadGoblins");
        if (deadGoblins == 0)
        {
            Debug.Log($"{model.UnitName} cannot use Goblin Fury - no dead goblins to avenge!");
            return;
        }
        
        Debug.Log($"{model.UnitName} channels Goblin Fury for {deadGoblins} dead goblins!");
        
        // Grant massive temporary buffs based on dead goblins
        int bonusStrength = deadGoblins * 5;
        int bonusArmor = deadGoblins * 3;
        
        var furyStrength = new StatusEffect
        {
            effectName = "Fury Strength",
            type = StatusEffectType.Buff,
            modifier = StatModifier.Strength,
            amount = bonusStrength,
            duration = 3,
            tags = { "Fury", "Vengeance" }
        };
        
        var furyArmor = new StatusEffect
        {
            effectName = "Fury Armor",
            type = StatusEffectType.Buff,
            modifier = StatModifier.Armor,
            amount = bonusArmor,
            duration = 3,
            tags = { "Fury", "Vengeance" }
        };
        
        var statusHandler = GetComponent<StatusEffectHandler>();
        if (statusHandler != null)
        {
            statusHandler.ApplyEffect(furyStrength);
            statusHandler.ApplyEffect(furyArmor);
        }
        
        // Heal based on dead goblins
        int healAmount = deadGoblins * 10;
        model.Heal(healAmount);
        
        Debug.Log($"{model.UnitName} gains +{bonusStrength} STR, +{bonusArmor} ARM, and heals {healAmount} HP!");
    }
    
    private void ExecuteDevastatingCharge(Unit target)
    {
        if (target == null || !model.IsInAdrenalineState) return;
        
        Debug.Log($"{model.UnitName} performs Devastating Charge on {target.Model.UnitName}!");
        
        // Move to target (simplified)
        Vector3 chargeDirection = (target.transform.position - transform.position).normalized;
        Vector3 chargePosition = target.transform.position - chargeDirection * 1f;
        transform.position = chargePosition;
        
        // Deal heavy damage
        var ability = GetAbilityByName("Devastating Charge");
        if (ability == null) return;
        
        int damage = ability.CalculateTotalDamage(model, target.Model);
        
        // Bonus damage based on distance traveled (simplified)
        int bonusDamage = 10;
        damage += bonusDamage;
        
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        // Apply knockback effect
        var knockbackEffect = new StatusEffect
        {
            effectName = "Knockback",
            type = StatusEffectType.Debuff,
            modifier = StatModifier.Performance,
            amount = -5,
            duration = 1,
            tags = { "Knockback", "Stunned" }
        };
        
        target.GetComponent<StatusEffectHandler>()?.ApplyEffect(knockbackEffect);
        
        Debug.Log($"Devastating Charge deals {damage} damage and knocks back {target.Model.UnitName}!");
    }
    
    public override void OnDamageTaken(int damage, DamageType damageType, Unit attacker)
    {
        base.OnDamageTaken(damage, damageType, attacker);
        
        // Gain adrenaline when taking damage (tanks gain more)
        model.AddAdrenaline(8, "taking damage as tank");
        
        // Additional adrenaline if HP is low
        if (model.CurrentHP <= model.MaxHP * 0.25f)
        {
            model.AddAdrenaline(10, "low health determination");
        }
    }
    
    // Monitor allied goblin deaths
    void Update()
    {
        // In a full implementation, this would be handled by events
        // For now, we simulate counting dead goblins
        MonitorGoblinDeaths();
    }
    
    private void MonitorGoblinDeaths()
    {
        // This is a simplified implementation
        // In a real game, this would be triggered by unit death events
        var allGoblins = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        int deadCount = 0;
        
        foreach (var goblin in allGoblins)
        {
            if (goblin.Faction == unit.Faction && 
                !goblin.IsAlive && 
                goblin != unit &&
                (goblin.name.Contains("Goblin") || goblin.name.Contains("HobGoblin")))
            {
                deadCount++;
            }
        }
        
        model.SetRes("DeadGoblins", deadCount);
    }
    
    // Champion-specific reaction: Protect allies
    public void ProtectAlly(Unit ally, Unit attacker)
    {
        if (!model.CanReact() || ally.Faction != unit.Faction) return;
        
        float distance = Vector3.Distance(transform.position, ally.transform.position);
        if (distance <= 3f) // Close enough to protect
        {
            Debug.Log($"{model.UnitName} protects {ally.Model.UnitName} from {attacker.Model.UnitName}!");
            
            // Use reaction to taunt the attacker
            if (model.CanReact())
            {
                model.SpendReaction();
                ExecuteTaunt(attacker);
            }
        }
    }
    
    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        
        // Clean up taunted enemies list (remove dead/invalid units)
        tauntedEnemies.RemoveAll(enemy => enemy == null || !enemy.IsAlive);
    }
    
    // Clean up when unit dies
    void OnDestroy()
    {
        StopAllCoroutines(); // Stop rage timer
        tauntedEnemies.Clear();
    }
}