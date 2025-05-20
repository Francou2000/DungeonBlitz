public enum StatusEffectType
{
    Buff,
    Debuff,
    Condition
}

public enum StatModifier
{
    Strength,
    MagicPower,
    Armor,
    MagicResistance,
    Performance,
    Affinity
}

[System.Serializable]
public class StatusEffect
{
    public string effectName;
    public StatusEffectType type;
    public StatModifier modifier;
    public int amount; // e.g. +2 Strength, -1 Armor
    public int duration; // in turns
    public bool isStackable;
}
