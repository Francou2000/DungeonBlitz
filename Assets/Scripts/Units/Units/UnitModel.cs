using System.Collections.Generic;
using UnityEngine;

public class UnitModel : MonoBehaviour
{
    [Header("Static Data")]
    [SerializeField] private UnitData unitData;

    [Header("Runtime Values")]
    private int currentHP;
    private int currentActions;
    private int currentReactions;
    private Unit thisUnit;
    private int adrenaline;
    private bool isInAdrenalineState = false;

    [Header("Movement Modifiers")]
    private readonly List<float> movementBuffs = new();
    private readonly List<float> movementDebuffs = new();

    public List<UnitAbility> Abilities { get; private set; } = new();
    public List<UnitAbility> AdrenalineAbilities { get; private set; } = new();

    public StatusEffectHandler statusHandler;

    [Header("Public Getters")]
    public string UnitName => unitData.unitName;
    public UnitFaction Faction => unitData.faction;

    public int MaxHP => unitData.maxHP;
    public int CurrentHP => currentHP;

    public int MaxActions => unitData.actionsPerTurn;
    public int CurrentActions => currentActions;

    public int MaxReactions => unitData.reactionsPerTurn;
    public int CurrentReactions => currentReactions;

    public int Performance => unitData.performance + statusHandler.GetStatBonus(StatModifier.Performance);
    public int Affinity => unitData.affinity + statusHandler.GetStatBonus(StatModifier.Affinity);
    public int Armor => unitData.armor + statusHandler.GetStatBonus(StatModifier.Armor);
    public int MagicResistance => unitData.magicResistance + statusHandler.GetStatBonus(StatModifier.MagicResistance);
    public int Strength => unitData.strength + statusHandler.GetStatBonus(StatModifier.Strength);
    public int MagicPower => unitData.magicPower + statusHandler.GetStatBonus(StatModifier.MagicPower);

    public int Adrenaline => adrenaline;
    public int AdrenalineThreshold => unitData.adrenalineThreshold;
    public int MaxAdrenaline => unitData.maxAdrenaline;
    public bool IsInAdrenalineState => isInAdrenalineState;

    public bool IsTrainingDummy => unitData.isTrainingDummy;
    public bool HasAttackOfOpportunity => unitData.hasAttackOfOpportunity;

    private Dictionary<string, int> _resources;

    // Initialization
    public void Initialize()
    {
        // Set runtime values from static config
        currentHP = MaxHP;
        currentActions = MaxActions;
        currentReactions = MaxReactions;
        adrenaline = unitData.baseAdrenaline;

        // Copy ability lists from SO to runtime
        Abilities = new List<UnitAbility>(unitData.abilities);
        AdrenalineAbilities = new List<UnitAbility>(unitData.adrenalineAbilities);

        statusHandler = GetComponent<StatusEffectHandler>();
        if (statusHandler == null)
            Debug.LogWarning("[UnitModel] No StatusEffectHandler found on this unit.");

        EnsureResources();
        InitializeStartingResources();
        
        CheckAdrenalineState();
    }

    private void InitializeStartingResources()
    {
        foreach (var res in unitData.startingResources)
            SetRes(res.key, res.amount);

        if (!string.IsNullOrEmpty(unitData.startingForm))
            SetState("Form", unitData.startingForm);

        if (!string.IsNullOrEmpty(unitData.startingWeapon))
            SetState("Weapon", unitData.startingWeapon);
    }

    public void ResetTurn()
    {
        currentActions = MaxActions;
        currentReactions = MaxReactions;

        thisUnit = GetComponent<Unit>();
        if (statusHandler != null)
        {
            // Fires per-unit when a faction’s turn begins
            statusHandler.OnStartTurn(thisUnit); // 'unit' = your Unit/owner reference; if you store it as a field, pass that                                                   
            // If you tick durations here, do it AFTER OnStartTurn: (future proof)                                   
            // statusHandler.TickEffectsAtTurnStart(); 
        }

        CheckAdrenalineState();
    }

    // Adrenaline System
    public void AddAdrenaline(int amount, string reason = "")
    {
        int oldAdrenaline = adrenaline;
        adrenaline = Mathf.Clamp(adrenaline + amount, 0, MaxAdrenaline);
        
        if (amount > 0 && !string.IsNullOrEmpty(reason))
        {
            Debug.Log($"{UnitName} gains {amount} adrenaline ({reason}). Total: {adrenaline}/{MaxAdrenaline}");
        }
        
        CheckAdrenalineState();
    }

    public void SpendAdrenaline(int amount)
    {
        adrenaline = Mathf.Max(0, adrenaline - amount);
        CheckAdrenalineState();
    }

    public void ResetAdrenaline()
    {
        adrenaline = 0;
        isInAdrenalineState = false;
        OnExitAdrenalineState();
    }

    private void CheckAdrenalineState()
    {
        bool wasInAdrenalineState = isInAdrenalineState;
        isInAdrenalineState = adrenaline >= AdrenalineThreshold;
        
        if (!wasInAdrenalineState && isInAdrenalineState)
        {
            OnEnterAdrenalineState();
        }
        else if (wasInAdrenalineState && !isInAdrenalineState)
        {
            OnExitAdrenalineState();
        }
    }

    protected virtual void OnEnterAdrenalineState()
    {
        Debug.Log($"{UnitName} enters adrenaline state!");
        // Notify other systems that this unit entered adrenaline state
        // Unit-specific adrenaline effects should be handled in UnitController
    }

    protected virtual void OnExitAdrenalineState()
    {
        Debug.Log($"{UnitName} exits adrenaline state!");
        // Notify other systems that this unit exited adrenaline state
    }

    // Action/Reactions
    public bool CanAct() => currentActions > 0;
    public void SpendAction(int amount = 1) => currentActions = Mathf.Max(0, currentActions - amount);

    public bool CanReact() => currentReactions > 0;
    public void SpendReaction(int amount = 1) => currentReactions = Mathf.Max(0, currentReactions - amount);

    // Ability System
    public bool CanUseAbility(UnitAbility ability)
    {
        // Check action cost
        if (currentActions < ability.actionCost) return false;
        
        // Check adrenaline requirements
        if (ability.requiresAdrenalineThreshold && adrenaline < ability.adrenalineThreshold) return false;
        
        // Check adrenaline cost
        if (ability.adrenalineCost > adrenaline) return false;
        
        // Check resource requirements
        foreach (var cost in ability.resourceCosts)
        {
            if (GetRes(cost.key) < cost.amount) return false;
        }
        
        return true;
    }

    public List<UnitAbility> GetAvailableAbilities()
    {
        var availableAbilities = new List<UnitAbility>(Abilities);
        
        // Add adrenaline abilities if in adrenaline state
        if (isInAdrenalineState)
        {
            availableAbilities.AddRange(AdrenalineAbilities);
        }
        
        return availableAbilities;
    }

    // Resources (components that units need to perform an action beside actions, ie, spears for hobgoblin)
    private void EnsureResources()
    {
        if (_resources == null) _resources = new Dictionary<string, int>(8);
    }

    public int GetRes(string key)
    {
        EnsureResources();
        return _resources.TryGetValue(key, out var v) ? v : 0;
    }

    public void SetRes(string key, int value)
    {
        EnsureResources();
        _resources[key] = Mathf.Max(0, value);
    }

    public void AddRes(string key, int delta)
    {
        EnsureResources();
        _resources[key] = Mathf.Max(0, GetRes(key) + delta);
    }

    public bool TryConsume(string key, int amount)
    {
        EnsureResources();
        var cur = GetRes(key);
        if (cur < amount) return false;
        _resources[key] = cur - amount;
        return true;
    }

    // States (weapons, forms, etc)
    private Dictionary<string, string> _states;

    private void EnsureStates()
    {
        if (_states == null) _states = new Dictionary<string, string>(4);
    }

    public string GetState(string key)
    {
        EnsureStates();
        return _states.TryGetValue(key, out var val) ? val : null;
    }

    public void SetState(string key, string value)
    {
        EnsureStates();
        _states[key] = value;
    }

    // Movement Speed
    public float MoveDistanceFactor = 10f;
    public float MoveTimeBase = 6f;

    public float GetMovementSpeed()
    {
        float distance = Performance * MoveDistanceFactor;
        float multiplier = 1f;

        foreach (float buff in movementBuffs) multiplier *= (1f + buff);
        foreach (float debuff in movementDebuffs) multiplier *= (1f - debuff);

        float moveTime = MoveTimeBase / multiplier;
        return distance / moveTime;
    }

    public void AddMoveBuff(float percent) => movementBuffs.Add(percent);
    public void AddMoveDebuff(float percent) => movementDebuffs.Add(percent);
    public void ClearMovementModifiers()
    {
        movementBuffs.Clear();
        movementDebuffs.Clear();
    }

    // Damage Handling 
    public bool IsAlive()
    {
        return CurrentHP > 0;
    }

    public void TakeDamage(int amount, DamageType type)
    {
        int finalDamage = CalculateDamageReduction(amount, type);
        ApplyDamage(finalDamage);
        
        // Gain adrenaline when taking damage
        AddAdrenaline(Mathf.RoundToInt(finalDamage * 0.5f), "taking damage");
    }

    private int CalculateDamageReduction(int incomingDamage, DamageType type)
    {
        if (type == DamageType.Physical)
        {
            return Mathf.RoundToInt(incomingDamage * 100f / (100f + Armor));
        }
        else if (type == DamageType.Magical)
        {
            return Mathf.RoundToInt(incomingDamage * 100f / (100f + MagicResistance));
        }
        
        return incomingDamage;
    }

    private void ApplyDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        Debug.Log($"{UnitName} took {amount} damage! HP: {currentHP}/{MaxHP}");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        int oldHP = currentHP;
        currentHP = Mathf.Min(MaxHP, currentHP + amount);
        int actualHealing = currentHP - oldHP;
        
        if (actualHealing > 0)
        {
            Debug.Log($"{UnitName} heals {actualHealing} HP. HP: {currentHP}/{MaxHP}");
        }
    }

    public int ApplyDamageWithBarrier(int incoming, DamageType type)
    {
        if (incoming <= 0) return 0;
        int remaining = incoming;

        // Ask status handler how much barrier is available & consume it first
        if (statusHandler != null)
        {
            int absorbed = statusHandler.ConsumeBarrier(remaining);
            remaining -= absorbed;
        }

        if (remaining <= 0) return incoming; // fully absorbed

        // Apply the remainder to HP:
        TakeDamage(remaining, type);
        return incoming;
    }

    private void Die()
    {
        Debug.Log($"{UnitName} has died.");
        
        // Grant adrenaline to allies when this unit dies
        var allUnits = FindObjectsByType<UnitModel>(FindObjectsSortMode.None);
        foreach (var unit in allUnits)
        {
            if (unit.Faction == this.Faction && unit.IsAlive() && unit != this)
            {
                unit.AddAdrenaline(5, $"{UnitName} death");
            }
        }
        
        Destroy(gameObject);
    }

    //Reaction
    public IEnumerable<UnitAbility> GetReactionsForTrigger(ReactionTrigger trigger)
    {
        var allAbilities = GetAvailableAbilities();
        foreach (var ability in allAbilities)
        {
            if (ability.isReaction && ability.reactionTrigger == trigger)
                yield return ability;
        }
    }

    public void TryPromote()
    {
        if (!unitData.isPromotable || unitData.promotedForm == null)
        {
            Debug.Log($"[Promotion] {UnitName} is not promotable.");
            return;
        }

        Debug.Log($"[Promotion] {UnitName} is promoting to {unitData.promotedForm.unitName}");

        // Store current HP percentage for post-promotion healing
        float hpPercentage = (float)currentHP / MaxHP;
        
        // Swap data
        unitData = unitData.promotedForm;

        // Reinitialize with new stats/abilities but maintain HP percentage
        Initialize();
        currentHP = Mathf.RoundToInt(MaxHP * hpPercentage);
        
        // Add promotion bonuses
        AddAdrenaline(MaxAdrenaline, "promotion");
    }
}
