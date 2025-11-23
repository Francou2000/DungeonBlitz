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

    [Header("Public Getters")]
    public string UnitName => unitData.unitName;
    public UnitFaction Faction => unitData.faction;

    public int MaxHP => unitData.maxHP;
    public int CurrentHP => currentHP;

    public int MaxActions => unitData.actionsPerTurn;
    public int CurrentActions => currentActions;

    public int MaxReactions => unitData.reactionsPerTurn;
    public int CurrentReactions => currentReactions;

    public float Performance
    {
        get
        {
            var sc = GetComponent<StatusComponent>();
            float basePerf = unitData.performance;

            if (sc == null)
                return basePerf;

            // For Performance, amount is treated as % (see StatusEffect comment)
            int deltaPct = sc.GetStatDelta(Stat.Performance);
            return basePerf * (1f + deltaPct / 100f);
        }
    }

    public int Affinity
    {
        get
        {
            var sc = GetComponent<StatusComponent>();
            int delta = sc ? sc.GetStatDelta(Stat.Affinity) : 0;
            return unitData.affinity + delta;
        }
    }

    public int Armor
    {
        get
        {
            var sc = GetComponent<StatusComponent>();
            int delta = sc ? sc.GetStatDelta(Stat.Armor) : 0;
            return unitData.armor + delta;
        }
    }

    public int MagicResistance
    {
        get
        {
            var sc = GetComponent<StatusComponent>();
            int delta = sc ? sc.GetStatDelta(Stat.MagicRes) : 0;
            return unitData.magicResistance + delta;
        }
    }

    public int Strength
    {
        get
        {
            var sc = GetComponent<StatusComponent>();
            int delta = sc ? sc.GetStatDelta(Stat.Strength) : 0;
            return unitData.strength + delta;
        }
    }

    public int MagicPower
    {
        get
        {
            var sc = GetComponent<StatusComponent>();
            int delta = sc ? sc.GetStatDelta(Stat.MagicPower) : 0;
            return unitData.magicPower + delta;
        }
    }

    public int Adrenaline => adrenaline;
    public int AdrenalineThreshold => unitData.adrenalineThreshold;
    public int MaxAdrenaline => unitData.maxAdrenaline;
    public bool IsInAdrenalineState => isInAdrenalineState;

    public bool IsTrainingDummy => unitData.isTrainingDummy;
    public bool HasAttackOfOpportunity => unitData.hasAttackOfOpportunity;

    private Dictionary<string, int> _resources;

    public event System.Action<int, int> OnHealthChanged;
    public event System.Action<int, int> OnAdrenalineChanged;
    public event System.Action<int, int> OnActionPointsChanged;
    public event System.Action<string, string> OnStateChanged;
    public Sprite Portrait => unitData != null ? unitData.portrait_foto : null;

    [Header("Resource UI")]
    [SerializeField] private string primaryResourceKey;   // e.g. "Spears", "Power"
    [SerializeField] private Sprite primaryResourceIcon;
    [SerializeField] private List<FormResourceIcon> formResourceIcons = new List<FormResourceIcon>();

    private string currentFormId;


    public string PrimaryResourceKey => primaryResourceKey;


    [System.Serializable]
    public class FormResourceIcon
    {
        public string formId;   // e.g. "Fire", "Frost", "Lightning"
        public Sprite icon;     // icon to use in that form
    }

    public Sprite PrimaryResourceIcon
    {
        get
        {
            // Look for an override for this form
            if (!string.IsNullOrEmpty(currentFormId))
            {
                for (int i = 0; i < formResourceIcons.Count; i++)
                {
                    var entry = formResourceIcons[i];
                    if (entry != null && entry.icon != null && entry.formId == currentFormId)
                        return entry.icon;
                }
            }
            // Fallback
            return primaryResourceIcon;
        }
    }

    public string CurrentFormId => currentFormId;

    // Fired whenever the form changes (so HUD can refresh icon)
    public event System.Action<string> OnFormChanged;

    public void SetCurrentForm(string formId)
    {
        if (currentFormId == formId) return;
        currentFormId = formId;
        OnFormChanged?.Invoke(formId);
    }

    // Initialization
    public void Initialize()
    {
        // Set runtime values from static config
        currentHP = MaxHP;
        currentActions = MaxActions;
        currentReactions = MaxReactions;
        adrenaline = unitData.baseAdrenaline;

        OnHealthChanged?.Invoke(currentHP, MaxHP);
        OnAdrenalineChanged?.Invoke(adrenaline, MaxAdrenaline);
        OnActionPointsChanged?.Invoke(currentActions, MaxActions);

        // Copy ability lists from SO to runtime
        Abilities = new List<UnitAbility>(unitData.abilities);

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
        thisUnit = GetComponent<Unit>();
        var sc = GetComponent<StatusComponent>();

        // Let statuses tick/start-of-turn effects run on MASTER
        sc?.OnTurnBegan();

        // Now ask the status component how many actions we really get
        if (sc != null)
            currentActions = GetMaxActionsThisTurn();
        else
            currentActions = MaxActions;

        currentReactions = MaxReactions;

        CheckAdrenalineState();
    }

    // Adrenaline System
    public void AddAdrenaline(int amount, string reason = "")
    {
        int oldAdrenaline = adrenaline;
        adrenaline = Mathf.Clamp(adrenaline + amount, 0, MaxAdrenaline);
        OnAdrenalineChanged?.Invoke(adrenaline, MaxAdrenaline);

        if (amount > 0 && !string.IsNullOrEmpty(reason))
        {
            Debug.Log($"{UnitName} gains {amount} adrenaline ({reason}). Total: {adrenaline}/{MaxAdrenaline}");
        }
        
        CheckAdrenalineState();
    }

    public void SpendAdrenaline(int amount)
    {
        adrenaline = Mathf.Max(0, adrenaline - amount);
        OnAdrenalineChanged?.Invoke(adrenaline, MaxAdrenaline);
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
    public void SpendAction(int amount = 1)
    {
        currentActions = Mathf.Max(0, currentActions - amount);
        OnActionPointsChanged?.Invoke(currentActions, MaxActions);
    }
    public void SetCurrentActions(int v)
    {
        currentActions = Mathf.Max(0, v);
        OnActionPointsChanged?.Invoke(currentActions, MaxActions);
    }
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
        OnResourceChanged?.Invoke(key, _resources[key]);
    }

    public void AddRes(string key, int delta)
    {
        EnsureResources();
        _resources[key] = Mathf.Max(0, GetRes(key) + delta);
        OnResourceChanged?.Invoke(key, _resources[key]);
    }

    public event System.Action<string, int> OnResourceChanged;

    public IReadOnlyDictionary<string, int> GetAllResources()
    {
        EnsureResources();
        // return a safe copy/snapshot
        return new Dictionary<string, int>(_resources);
    }

    public bool TryConsume(string key, int amount)
    {
        EnsureResources();
        var cur = GetRes(key);
        if (cur < amount) return false;
        _resources[key] = cur - amount;
        OnResourceChanged?.Invoke(key, _resources[key]);
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
        if (_states.TryGetValue(key, out var old) && old == value)
            return;

        _states[key] = value;

        if (key == "Form")
        {
            SetCurrentForm(value);   // this will also fire OnFormChanged
        }

        OnStateChanged?.Invoke(key, value);
        Debug.Log($"[State] {UnitName} -> {key} = {value}");
    }

    // Summons

    public void OverrideUnitData(UnitData newData)
    {
        if (newData == null) return;
        unitData = newData;
        Initialize(); // call whatever you already use to rebuild cached values/UI
    }

    // Movement Speed
    public float MoveDistanceFactor = 4f;
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
        OnHealthChanged?.Invoke(currentHP, MaxHP);

        Debug.Log($"{UnitName} took {amount} damage! HP: {currentHP}/{MaxHP}");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        var sc = GetComponent<StatusComponent>();
        if (sc != null) amount = Mathf.FloorToInt(amount * sc.GetHealingMultiplierThisTurn());

        int oldHP = currentHP;
        currentHP = Mathf.Min(MaxHP, currentHP + Mathf.Max(0, amount));
        int actualHealing = currentHP - oldHP;

        if (actualHealing > 0)
            Debug.Log($"{UnitName} heals {actualHealing} HP. HP: {currentHP}/{MaxHP}");

        OnHealthChanged?.Invoke(currentHP, MaxHP);
    }

    public int ApplyDamageWithBarrier(int incoming, DamageType type)
    {
        if (incoming <= 0) return 0;

        Debug.Log($"[Damage] {UnitName} incoming={incoming} type={type}");
        var sc = GetComponent<StatusComponent>();
        if (sc != null) incoming = sc.AbsorbWithBarrier(incoming, type);
        Debug.Log($"[Damage] {UnitName} post-barrier={incoming}");

        if (incoming <= 0) return 0;          // check the updated amount
        TakeDamage(incoming, type);            // deal the updated amount
        return incoming;                       // return the updated amount
    }

    // Apply AP from network (called by RPC on all clients).
    public void NetSetActions(int current, int max)
    {
        int clamped = Mathf.Clamp(current, 0, this.MaxActions);
        if (clamped != this.CurrentActions || max != this.MaxActions)
        {
            currentActions = clamped;
            OnActionPointsChanged?.Invoke(this.CurrentActions, this.MaxActions);
        }
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

    public int GetMaxActionsThisTurn() //  helper for HUD/turn calc
    {
        var sc = GetComponent<StatusComponent>();
        int delta = sc ? sc.GetAPDeltaForThisTurn() : 0;
        return Mathf.Max(0, MaxActions + delta);
    }

    public float GetPerformanceForMove() //  freeze penalty hook
    {
        var sc = GetComponent<StatusComponent>();
        float perf = unitData.performance;
        if (sc && sc.Has(StatusType.Freeze)) perf *= 0.5f;
        return perf;
    }
}
