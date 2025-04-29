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
    private int anxiety;

    [Header("Movement Modifiers")]
    private readonly List<float> movementBuffs = new();
    private readonly List<float> movementDebuffs = new();

    public List<UnitAbility> Abilities { get; private set; } = new();


    [Header("Public Getters")]
    public string UnitName => unitData.unitName;
    public int MaxHP => unitData.maxHP;
    public int CurrentHP => currentHP;

    public int MaxActions => unitData.actionsPerTurn;
    public int CurrentActions => currentActions;

    public int MaxReactions => unitData.reactionsPerTurn;
    public int CurrentReactions => currentReactions;

    public int Performance => unitData.performance;
    public int Affinity => unitData.affinity;
    public int Armor => unitData.armor;
    public int MagicResistance => unitData.magicResistance;
    public int Strength => unitData.strength;
    public int MagicPower => unitData.magicPower;

    public int Anxiety => anxiety;

    public bool IsTrainingDummy => unitData.isTrainingDummy;

    // === Initialization ===

    public void Initialize()
    {
        currentHP = MaxHP;
        currentActions = MaxActions;
        currentReactions = MaxReactions;
        anxiety = 0;

        Abilities = new List<UnitAbility>(unitData.abilities);
    }

    public void ResetTurn()
    {
        currentActions = MaxActions;
        currentReactions = MaxReactions;
    }

    // === Action/Reactions ===

    public bool CanAct() => currentActions > 0;
    public void SpendAction(int amount = 1) => currentActions = Mathf.Max(0, currentActions - amount);

    public bool CanReact() => currentReactions > 0;
    public void SpendReaction(int amount = 1) => currentReactions = Mathf.Max(0, currentReactions - amount);

    // === Movement Speed ===

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

    // === Damage Handling ===

    public void TakePhysicalDamage(int rawDamage)
    {
        int reduced = Mathf.FloorToInt(rawDamage * 100f / (100f + Armor));
        ApplyDamage(reduced);
    }

    public void TakeMagicDamage(int rawDamage)
    {
        int reduced = Mathf.FloorToInt(rawDamage * 100f / (100f + MagicResistance));
        ApplyDamage(reduced);
    }

    private void ApplyDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{UnitName} has died.");
        Destroy(gameObject);
    }

    // === Anxiety ===

    public void AddAnxiety(int amount) => anxiety += amount;
    public void ResetAnxiety() => anxiety = 0;
}
