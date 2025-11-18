using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Item Data/ New Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public int ItemID;
    public Sprite sprite;
    public HeroesList tailored_heroe;

    [Header("Shop Info")]
    public Rarity rarity;
    public int cost;
    public bool is_unique;

    [Header("Effect")]
    public bool change_stats;
    public bool change_actions;
    public bool add_actions;
    public bool consumable;

    [Header("Stats")]
    public int maxHP;
    [Range(-2, 2)]
    public float performance;
    public int affinity;
    public int armor;
    public int magicResistance;
    public int strength;
    public int magicPower;

    [Header("Abilities added")]
    // public UnitAbility[] consumable_effect;
    public UnitAbility new_ability;

}


public enum Rarity
{
    COMMON,
    UNCOMMON,
    RARE,
    EPIC,
    LEGENDARY,
}
