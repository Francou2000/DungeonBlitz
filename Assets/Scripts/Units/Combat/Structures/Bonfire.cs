using UnityEngine;

public sealed class Bonfire : StructureBase
{
    public int HealPerTick = 5;   // per ally at start of their turn

    public static Bonfire Create(Vector3 pos, UnitFaction faction, int ownerViewId,
                                 int healPerTick, float radius, double expiresAt)
    {
        var go = new GameObject("Bonfire");
        var s = go.AddComponent<Bonfire>();
        s.HealPerTick = Mathf.Max(0, healPerTick);
        s.Init(StructureKind.Bonfire, faction, ownerViewId, pos, hp: 1, radius: radius,
               cover: CoverType.None, expiresAt: expiresAt);
        return s;
    }
}
