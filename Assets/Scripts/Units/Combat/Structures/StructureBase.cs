using UnityEngine;
public enum StructureKind { None, IcePillar, Bonfire }
public enum CoverType { None = 0, Medium = 1, Heavy = 2 }

public class StructureBase : MonoBehaviour
{
    [HideInInspector] public StructureKind Kind;
    [HideInInspector] public UnitFaction Faction;   // who owns it
    [HideInInspector] public int OwnerViewId;       // owner Unit (for limits)

    [HideInInspector] public float MaxHP;
    [HideInInspector] public float HP;
    [HideInInspector] public float Radius;          // for auras/size
    [HideInInspector] public CoverType Cover = CoverType.None;

    [HideInInspector] public int RemainingTurns = 0;
    [HideInInspector] public bool TickEveryTurn = false;

    public int NetId { get; internal set; }  // assigned by StructureManager when spawning


    public virtual void Init(StructureKind kind, UnitFaction faction, int ownerViewId,
                            Vector3 pos, float hp, float radius, CoverType cover,
                            int remainingTurns, bool tickEveryTurn = false)
    {
        Kind = kind; Faction = faction; OwnerViewId = ownerViewId;
        transform.position = pos;
        MaxHP = Mathf.Max(1, hp);
        HP = MaxHP;
        Radius = Mathf.Max(0f, radius);
        Cover = cover;
        RemainingTurns = remainingTurns;
        TickEveryTurn = tickEveryTurn;
        gameObject.name = $"{kind}({Faction})";
    }

    public virtual void OnTurnBegan(UnitFaction startingFaction)
    {
        if (RemainingTurns <= 0) return; // infinite or not using duration
        if (TickEveryTurn || startingFaction == Faction)
        {
            RemainingTurns--;
            if (RemainingTurns <= 0) Destroy(gameObject);
        }
    }

    public void TakeDamage(int amount)
    {
        HP = Mathf.Max(0, HP - Mathf.Max(0, amount));
        if (HP == 0) Destroy(gameObject);
    }

    public bool IsInRange(Unit u)
    {
        if (u == null) return false;
        return Vector3.Distance(u.transform.position, this.transform.position) <= Radius;
    }

    public bool IsAlive() => this != null && HP > 0f;

    public void ApplyDamage(int amount)
    {
        HP = Mathf.Max(0, HP - Mathf.Max(0, amount));
        if (HP <= 0) Destroy(gameObject);
    }

    void OnDestroy()
    {
        var mgr = StructureManager.Instance;
        if (mgr) mgr.UnregisterStructure(NetId, this);
    }
}
