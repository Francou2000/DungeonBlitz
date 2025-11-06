/// Runtime instance applied to a unit (authoritative on master).
[System.Serializable]
public sealed class StatusEffect
{
    public StatusType type;
    public string name;              // display/debug
    public int sourceViewId;         // who applied it (for Burn tick calc / taunt lock)
    public int remainingTurns;       // decremented at end or start as specified (see handler)
    public int aux;                  // general counter (e.g., Burn ticks)
    public int barrierHP;            // barrier pool for this instance

    // Buff/Debuff specifics
    public Stat stat;
    public int amount;               // integer delta (e.g., +2 STR, -2 Armor). For Performance, treat as %
    public bool isDebuff;            // for UI tagging

    // Action/AP deltas (Haste/Shock)
    public int nextTurnActionsDelta; // applied at start of *owner’s* turn, lasts that turn

    // Movement control
    public bool root;                // Root prevents Move
    public int moveSpeedDeltaPct;    // +/-

    // Healing modifier (Poison-like, if you add it later)
    public float healMultiplier = 1f;

    // Bleed on move
    public int bleedDamageOnMove;

    // Taunt lock target
    public int targetLockViewId;

    // Incandescent tag (adds fire rider in your damage resolver)
    public bool incandescent;

    // Convenience factories clone “templates”; do not share instances.
    public StatusEffect Clone() => (StatusEffect)this.MemberwiseClone();
}
