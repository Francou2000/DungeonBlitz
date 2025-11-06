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
    public int pop_cost = -1;
    
    [Header("Core Stats")]
    public int maxHP;
    public float performance;
    public int affinity;
    public int armor;
    public int magicResistance;
    public int strength;
    public int magicPower;
    
    [Header("Action System")]
    public int actionsPerTurn;
    public int reactionsPerTurn;
    
    [Header("Adrenaline System")]
    public int baseAdrenaline = 0;
    public int adrenalineThreshold = 30;
    public int maxAdrenaline = 100;

    [Header("Abilities")]
    public List<UnitAbility> abilities = new List<UnitAbility>();
 
    public struct StartingResource
    {
        public string key;
        public int amount;
    }

    [Header("Resources")]
    public List<StartingResource> startingResources = new List<StartingResource>();
    public string startingForm;   // e.g., "Fire", "Frost", "Lightning"
    public string startingWeapon; // e.g., "Bow", "Dagger"

    [Header("Flags")]
    public bool isTrainingDummy = false;

    [Header("Promotion")]
    public bool isPromotable;
    public UnitData promotedForm;
    
    [Header("Special Mechanics")]
    public bool hasAttackOfOpportunity = false;
    public List<string> specialTags = new List<string>();
}

[System.Serializable]
public class UnitResource
{
    public string resourceName;
    public int amount;
    public int maxAmount;
}

public enum HeroesList
{
    Paladin,
    Sorcerer,
    Rogue,
    Elementalist,
    None
}
