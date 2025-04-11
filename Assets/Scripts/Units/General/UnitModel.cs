using UnityEngine;

public class UnitModel : MonoBehaviour
{
    [Header("Base Stats")]
    public string unitName;
    public int maxHP;
    public int strength;
    public int magicPower;
    public int physicalDefense;
    public int magicalDefense;
    public int speed;
    public int actionsPerTurn = 2;

    [Header("Runtime Values")]
    public int currentHP;
    public int currentActions;

    public void Initialize(Unit unit)
    {
        currentHP = maxHP;
        currentActions = actionsPerTurn;
    }

    public void ResetTurn()
    {
        currentActions = actionsPerTurn;
    }

    public bool CanAct()
    {
        return currentActions > 0;
    }

    public void SpendAction()
    {
        if (currentActions > 0)
            currentActions--;
    }
}
