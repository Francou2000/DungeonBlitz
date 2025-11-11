using UnityEngine;

public sealed class IcePillar : StructureBase
{
    public static IcePillar Create(Vector3 pos, UnitFaction faction, int ownerViewId, float hp, float radius, double expiresAt)
    {
        var go = new GameObject("IcePillar");
        var s = go.AddComponent<IcePillar>();

        s.Init(
            kind: StructureKind.IcePillar,
            faction: faction,
            ownerViewId: ownerViewId,
            pos: pos,
            hp: Mathf.CeilToInt(Mathf.Max(1f, hp)),
            radius: Mathf.Max(0.1f, radius),
            cover: CoverType.Medium,
            expiresAt: expiresAt
        );
        return s;
    }
}
