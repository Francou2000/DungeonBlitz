using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "unitData", menuName = "Units/New Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Base Info")]
    public string unitName;
    public UnitFaction faction;
    public int maxHP;
    public float performance;
    public int affinity;
    public int armor;
    public int magicResistance;
    public int strength;
    public int magicPower;
    public int actionsPerTurn;
    public int reactionsPerTurn;
    public int baseAdrenaline = 0;

    [Header("Abilities")]
    public List<UnitAbility> abilities = new List<UnitAbility>();

    [Header("Flags")]
    public bool isTrainingDummy = false;

    [Header("Promotion")]
    public bool isPromotable;
    public UnitData promotedForm;
}
