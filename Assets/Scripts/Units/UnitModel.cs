using System.Collections.Generic;
using UnityEngine;

public class UnitModel : MonoBehaviour
{
    [Header("Base Stats")]
    public string unitName;
    public int maxHP;
    public int currentHP;

    public int performance; // Speed and movement
    public int affinity;    // Weapon hit bonus (future use)
    public int armor;       // Physical defense
    public int magicResistance; // Magic defense
    public int strength;    // Physical attack modifier
    public int magicPower;  // Magic attack modifier
    public int anxiety;     // Special behavior trigger

    public int actionsPerTurn;
    public int currentActions;

    public int reactionsPerTurn;
    public int currentReactions;

    public float moveDistFactor = 10f; // m per Perf point
    public float moveTimeBase = 6f;    // base duration
    public List<float> movementBuffs = new();
    public List<float> movementDebuffs = new();

    public bool isTrainingDummy = false; // differentiate for targeting

    private Unit unit;
    public UnitData unitData; // Drag and drop in Inspector
    public List<UnitAbility> abilities = new List<UnitAbility>();

    public void Initialize(Unit unit)
    {
        this.unit = unit;

        maxHP = unitData.maxHP;
        currentHP = maxHP;
        performance = unitData.performance;
        affinity = unitData.affinity;
        armor = unitData.armor;
        magicResistance = unitData.magicResistance;
        strength = unitData.strength;
        magicPower = unitData.magicPower;
        actionsPerTurn = unitData.actionsPerTurn;
        currentActions = actionsPerTurn;
        reactionsPerTurn = unitData.reactionsPerTurn;
        currentReactions = reactionsPerTurn;
        anxiety = 0;

        abilities = new List<UnitAbility>(unitData.abilities);
    }

    public void ResetTurn()
    {
        currentActions = actionsPerTurn;
        currentReactions = reactionsPerTurn;
    }

    public bool CanAct()
    {
        return currentActions > 0;
    }

    public bool CanReact()
    {
        return currentReactions > 0;
    }

    public void SpendAction()
    {
        if (currentActions > 0)
            currentActions--;
    }

    public void SpendReaction()
    {
        if (currentReactions > 0)
            currentReactions--;
    }

    public float GetMovementSpeed()
    {
        float distance = performance * moveDistFactor;

        float multiplier = 1f;

        foreach (var buff in movementBuffs)
            multiplier *= (1f + buff);

        foreach (var debuff in movementDebuffs)
            multiplier *= (1f - debuff);

        float moveTime = moveTimeBase / multiplier;

        return distance / moveTime; // units per second
    }

    public void TakePhysicalDamage(int baseDamage)
    {
        int finalDamage = Mathf.FloorToInt(baseDamage * 100f / (100f + armor));
        ApplyDamage(finalDamage);
    }

    public void TakeMagicDamage(int baseDamage)
    {
        int finalDamage = Mathf.FloorToInt(baseDamage * 100f / (100f + magicResistance));
        ApplyDamage(finalDamage);
    }

    private void ApplyDamage(int amount)
    {
        currentHP -= amount;
        currentHP = Mathf.Max(0, currentHP);

        Debug.Log($"{unitName} takes {amount} damage. HP left: {currentHP}");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{unitName} has died!");
        Destroy(unit.gameObject);
    }
}
