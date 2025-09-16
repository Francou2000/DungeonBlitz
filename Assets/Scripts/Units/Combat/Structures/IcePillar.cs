using UnityEngine;

public sealed class IcePillar : StructureBase
{
    public static IcePillar Create(Vector3 pos, UnitFaction faction, int ownerViewId,
                                   int hp, double expiresAt)
    {
        var go = new GameObject("IcePillar");
        var s = go.AddComponent<IcePillar>();
        s.Init(StructureKind.IcePillar, faction, ownerViewId, pos, hp, 0.5f, CoverType.Medium, expiresAt);
        return s;
    }
}
