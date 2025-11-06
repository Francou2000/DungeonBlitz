public enum StatusType
{
    Enraged,
    Bleed,
    Taunt,
    Barrier,
    Incandescent,
    Root,
    Haste,
    Shock,
    Burn,
    Freeze,
    // Generic stat mods:
    Buff,   // positive modifier
    Debuff  // negative modifier
}

// Which stat is modified by Buff/Debuff
public enum Stat
{
    None,
    Strength,
    MagicPower,
    Armor,
    MagicRes,
    Performance,
    Affinity
}
