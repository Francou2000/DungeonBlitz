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
    private int adrenaline;

    [Header("Movement Modifiers")]
    private readonly List<float> movementBuffs = new();
    private readonly List<float> movementDebuffs = new();

    public List<UnitAbility> Abilities { get; private set; } = new();

    private StatusEffectHandler statusHandler;


    [Header("Public Getters")]
    public string UnitName => unitData.unitName;
    public UnitFaction Faction => unitData.faction;

    UnitController my_controller;

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

    public bool IsTrainingDummy => unitData.isTrainingDummy;

    // Initialization

    public void Initialize()
    {
        // Set runtime values from static config
        currentHP = MaxHP;
        currentActions = MaxActions;
        currentReactions = MaxReactions;
        adrenaline = unitData.baseAdrenaline;

        // Copy ability list from SO to runtime
        Abilities = new List<UnitAbility>(unitData.abilities);

        statusHandler = GetComponent<StatusEffectHandler>();
        if (statusHandler == null)
            Debug.LogWarning("[UnitModel] No StatusEffectHandler found on this unit.");

    }

    public void ResetTurn()
    {
        currentActions = MaxActions;
        currentReactions = MaxReactions;
    }
    private void Start()
    {
        my_controller = GetComponent<UnitController>();
    }

    // Action/Reactions

    public bool CanAct() => currentActions > 0;
    public void SpendAction(int amount = 1) => currentActions = Mathf.Max(0, currentActions - amount);

    public bool CanReact() => currentReactions > 0;
    public void SpendReaction(int amount = 1) => currentReactions = Mathf.Max(0, currentReactions - amount);

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
        ApplyDamage(amount);
    }

    private void ApplyDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        Debug.Log($"{UnitName} took {amount} damage!");

        if (currentHP <= 0) { my_controller.UnitDied(); }
    }

    public void Die()
    {
        Debug.Log($"{UnitName} has died.");
        Destroy(gameObject);
    }

    // Adrenaline

    public void AddAdrenaline(int amount) => adrenaline += amount;
    public void ResetAdrenaline() => adrenaline = 0;

    //Reaction

    public IEnumerable<UnitAbility> GetReactionsForTrigger(ReactionTrigger trigger)
    {
        foreach (var ability in Abilities)
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

        // Swap data
        unitData = unitData.promotedForm;

        // Reinitialize with new stats/abilities
        Initialize();
    }
}
