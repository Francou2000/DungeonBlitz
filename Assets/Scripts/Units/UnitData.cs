using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "unitData", menuName = "Units/New Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Base Info")]
    public string unitName;
    public int maxHP;
    public int performance;
    public int affinity;
    public int armor;
    public int magicResistance;
    public int strength;
    public int magicPower;
    public int actionsPerTurn;
    public int reactionsPerTurn;

    [Header("Abilities")]
    public List<UnitAbility> abilities = new List<UnitAbility>();
}
