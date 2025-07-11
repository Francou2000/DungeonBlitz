using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "unitData", menuName = "Units/New Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Base Info")]
    public string unitName;
    public Sprite full_body_foto;
    public Sprite portrait_foto;
    public UnitFaction faction;
    public HeroesList heroe_id;
    public int maxHP;
    public int performance;
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

public enum HeroesList
{
    Paladin,
    Rogue,
    Elementalist,
    Sorcerer,
}
