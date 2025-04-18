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

    public bool isTrainingDummy = false; // To try things on

    private Unit unit;

    public void Initialize(Unit unit)
    {
        this.unit = unit;
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

    public void TakeDamage(int amount)
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
