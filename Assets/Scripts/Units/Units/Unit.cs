using System;
using UnityEngine;

public enum UnitFaction
{
    Hero,
    Monster,
    Test
}

public class Unit : MonoBehaviour
{
    public UnitModel Model { get; private set; }
    public UnitView View { get; private set; }
    public UnitController Controller { get; private set; }

    public UnitFaction Faction { get; private set; }

    // Events for ability execution
    public System.Action<UnitAbility, Unit> OnAbilityUsed;
    public System.Action<int, DamageType, Unit> OnDamageTaken;
    public System.Action OnTurnStarted;
    public System.Action OnTurnEnded;

    // Main entry point for unit behavior
    void Awake()
    {
        // Fetch and link components
        Model = GetComponent<UnitModel>();
        View = GetComponent<UnitView>();
        Controller = GetComponent<UnitController>();

        // Initialize the MVC pieces
        Model.Initialize();
        View.Initialize(this);
        Controller.Initialize(this);
        
        // Set faction from model
        Faction = Model.Faction;
        
        // Subscribe to model events
        SubscribeToModelEvents();
    }
    
    private void SubscribeToModelEvents()
    {
        // Subscribe to adrenaline state changes
        // This would be implemented with proper events in a full system
    }

    public bool IsAlly(Unit other)
    {
        return this.Faction == other.Faction;
    }
    
    public bool IsEnemy(Unit other)
    {
        return this.Faction != other.Faction;
    }
    
    // Ability execution
    public bool CanUseAbility(UnitAbility ability)
    {
        return Model.CanUseAbility(ability);
    }
    
    public void UseAbility(UnitAbility ability, Unit target = null, Vector3 targetPosition = default)
    {
        if (!CanUseAbility(ability)) 
        {
            Debug.LogWarning($"{Model.UnitName} cannot use {ability.abilityName}");
            return;
        }
        
        // Spend costs
        Model.SpendAction(ability.actionCost);
        
        if (ability.adrenalineCost > 0)
        {
            Model.SpendAdrenaline(ability.adrenalineCost);
        }
        
        // Spend resources
        foreach (var cost in ability.resourceCosts)
        {
            Model.TryConsume(cost.key, cost.amount);
        }
        
        Debug.Log($"{Model.UnitName} uses {ability.abilityName}");
        
        // Notify controller to handle execution
        Controller.ExecuteAbility(ability, target, targetPosition);
        
        // Fire event
        OnAbilityUsed?.Invoke(ability, target);
    }
    
    // Turn management
    public void StartTurn()
    {
        Model.ResetTurn();
        Controller.OnTurnStart();
        OnTurnStarted?.Invoke();
        
        Debug.Log($"=== {Model.UnitName}'s Turn Started ===");
        Debug.Log($"Actions: {Model.CurrentActions}/{Model.MaxActions}, Adrenaline: {Model.Adrenaline}/{Model.MaxAdrenaline}");
    }
    
    public void EndTurn()
    {
        Controller.OnTurnEnd();
        OnTurnEnded?.Invoke();
        
        Debug.Log($"=== {Model.UnitName}'s Turn Ended ===");
    }
    
    // Damage handling
    public void TakeDamage(int damage, DamageType damageType, Unit attacker = null)
    {
        Model.TakeDamage(damage, damageType);
        Controller.OnDamageTaken(damage, damageType, attacker);
        OnDamageTaken?.Invoke(damage, damageType, attacker);
    }
    
    // Healing
    public void Heal(int amount, Unit healer = null)
    {
        Model.Heal(amount);
        Controller.OnHealed(amount, healer);
    }
    
    // Reaction system
    public void TriggerReaction(ReactionTrigger trigger, Unit triggeringUnit = null)
    {
        if (!Model.CanReact()) return;
        
        var reactions = Model.GetReactionsForTrigger(trigger);
        foreach (var reaction in reactions)
        {
            if (Model.CanUseAbility(reaction))
            {
                UseAbility(reaction, triggeringUnit);
                break; // Only use one reaction per trigger
            }
        }
    }
    
    // Attack of opportunity
    public void UseAttackOfOpportunity(Unit target)
    {
        if (!Model.HasAttackOfOpportunity || !Model.CanReact()) return;
        
        // Find a basic attack ability to use
        var abilities = Model.GetAvailableAbilities();
        var basicAttack = abilities.Find(a => a.abilityType == AbilityType.BasicAttack && Model.CanUseAbility(a));
        
        if (basicAttack != null)
        {
            Model.SpendReaction();
            UseAbility(basicAttack, target);
            Debug.Log($"{Model.UnitName} uses Attack of Opportunity on {target.Model.UnitName}!");
        }
    }
    
    // Status and state queries
    public bool IsAlive => Model.IsAlive();
    public bool IsInAdrenalineState => Model.IsInAdrenalineState;
    public bool CanAct => Model.CanAct();
    public bool CanReact => Model.CanReact();

    // Resource access
    public int GetResource(string resourceName) => Model.GetRes(resourceName);
    public void SetResource(string resourceName, int amount) => Model.SetRes(resourceName, amount);
    public void AddResource(string resourceName, int amount) => Model.AddRes(resourceName, amount);
    public bool ConsumeResource(string resourceName, int amount) => Model.TryConsume(resourceName, amount);
    
    // Promotion
    public void Promote()
    {
        Model.TryPromote();
        Debug.Log($"{Model.UnitName} has been promoted!");
    }
    
    // Debug info
    public void LogCurrentState()
    {
        Debug.Log($"=== {Model.UnitName} State ===");
        Debug.Log($"HP: {Model.CurrentHP}/{Model.MaxHP}");
        Debug.Log($"Actions: {Model.CurrentActions}/{Model.MaxActions}");
        Debug.Log($"Reactions: {Model.CurrentReactions}/{Model.MaxReactions}");
        Debug.Log($"Adrenaline: {Model.Adrenaline}/{Model.MaxAdrenaline} (Threshold: {Model.AdrenalineThreshold})");
        Debug.Log($"In Adrenaline State: {Model.IsInAdrenalineState}");
        Debug.Log($"Stats - STR: {Model.Strength}, MP: {Model.MagicPower}, ARM: {Model.Armor}, MR: {Model.MagicResistance}");
        
        // Log resources
        var resources = new string[] { "Spears", "PowerStacks", "DeadGoblins" };
        foreach (var resource in resources)
        {
            int amount = Model.GetRes(resource);
            if (amount > 0)
            {
                Debug.Log($"{resource}: {amount}");
            }
        }
    }
}
