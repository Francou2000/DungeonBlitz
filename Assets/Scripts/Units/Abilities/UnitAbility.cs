using UnityEngine;

[System.Serializable]
public class UnitAbility 
{
    public string abilityName;
    public int baseDamage;
    public bool requiresAnxietyThreshold;
    public int anxietyThreshold;
    public int hits = 1; // Default is 1 hit
    public int actionCost = 1; //Default is 1 action
}
