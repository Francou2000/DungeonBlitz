using UnityEngine;

public sealed class Bonfire : StructureBase
{
    public int HealPerTick = 5;   // per ally at start of their turn

    public static Bonfire Create(Vector3 pos, UnitFaction faction, int ownerViewId,
                                    float healPerTick, float radius, int durationTurns)
    {
        var go = new GameObject("Bonfire");
        var s = go.AddComponent<Bonfire>();

        s.HealPerTick = Mathf.CeilToInt(Mathf.Max(0f, healPerTick));

        s.Init(
            kind: StructureKind.Bonfire,
            faction: faction,
            ownerViewId: ownerViewId,
            pos: pos,
            hp: 1,
            radius: Mathf.Max(0.1f, radius),
            cover: CoverType.None,
            remainingTurns: durationTurns,
            tickEveryTurn: false
        );
        return s;
    }
}
