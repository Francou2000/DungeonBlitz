using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }
    private PhotonView _view;

    private readonly List<ZoneBase> _zones = new List<ZoneBase>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _view = GetComponent<PhotonView>() ?? gameObject.AddComponent<PhotonView>();
    }

    void OnEnable() { TurnManager.OnTurnBegan += HandleTurnBegan; }
    void OnDisable() { TurnManager.OnTurnBegan -= HandleTurnBegan; }

    void HandleTurnBegan(UnitFaction _)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        for (int i = _zones.Count - 1; i >= 0; i--)
        {
            var z = _zones[i];
            if (!z) { _zones.RemoveAt(i); continue; }
            if (z.RemainingTurns > 0) z.RemainingTurns--;
            if (z.RemainingTurns == 0)
            {
                Destroy(z.gameObject);
                _zones.RemoveAt(i);
            }
        }
    }

    // === Authoritative API ===
    public void SpawnCircleZone(ZoneKind kind, Vector3 center, float radius, int durationTurns, int ownerViewId = -1)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        _view.RPC(nameof(RPC_CreateCircleZone), RpcTarget.All, (int)kind, center, radius, durationTurns, ownerViewId);
    }

    [PunRPC]
    void RPC_CreateCircleZone(int kindInt, Vector3 center, float radius, int durationTurns, int ownerViewId)
    {
        var kind = (ZoneKind)kindInt;
        var go = new GameObject($"{(ZoneKind)kind}Zone");
        ZoneBase zone = null;
        switch (kind)
        {
            case ZoneKind.Negative: zone = go.AddComponent<NegativeZone>(); break;
            case ZoneKind.Frozen: zone = go.AddComponent<FrozenZone>(); break;
            default: Debug.LogWarning($"[ZoneManager] Unknown zone kind {kind}"); Destroy(go); return;
        }
        zone.Init(kind, center, radius, durationTurns, ownerViewId);
        _zones.Add(zone);
    }

    public void SpawnStormCrossing(Vector3 a, Vector3 b, float width, int durationTurns,
                               int ownerFaction, int allyHasteDuration, int enemyDamage, int shockChance)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        _view.RPC(nameof(RPC_CreateStormCrossing), RpcTarget.All,
            a, b, width, durationTurns, ownerFaction, allyHasteDuration, enemyDamage, shockChance);
    }

    [PunRPC]
    void RPC_CreateStormCrossing(Vector3 a, Vector3 b, float width, int durationTurns,
                                 UnitFaction ownerFaction, int allyHasteDur, int enemyDmg, int shockChance)
    {
        var go = new GameObject("StormCrossingZone");
        var z = go.AddComponent<StormCrossingZone>();
        z.InitSegment(a, b, width, ownerFaction, allyHasteDur, enemyDmg, shockChance, durationTurns);
        _zones.Add(z);
    }

    // Master decides on movement, broadcasts effect application
    public void HandleOnMove(Unit self, Vector3 from, Vector3 to)
    {
        if (self == null) return;

        // Existing Frozen/Negative handling can stay elsewhere; this focuses on StormCrossing
        if (!PhotonNetwork.IsMasterClient) return;

        var view = self.GetComponent<PhotonView>();
        if (view == null) return;

        for (int i = 0; i < _zones.Count; i++)
        {
            var z = _zones[i] as StormCrossingZone;
            if (z == null) continue;
            if (z == null || z.IsExpired()) continue;
            if (!z.IsCrossing(from, to)) continue;

            bool isAlly = (self.Model.Faction == z.ownerFaction);
            if (isAlly)
            {
                _view.RPC(nameof(RPC_ApplyHasteFromStorm), RpcTarget.All, view.ViewID, z.allyHasteDuration);
            }
            else
            {
                _view.RPC(nameof(RPC_ApplyStormEnemy), RpcTarget.All,
                    view.ViewID, z.enemyDamage, z.enemyShockChance);
            }
            
            break;
        }
    }

    [PunRPC]
    void RPC_ApplyHasteFromStorm(int unitViewId, int hasteDuration)
    {
        var u = PhotonView.Find(unitViewId)?.GetComponent<Unit>();
        if (u == null) return;

       // u.Model.statusHandler?.ApplyEffect(EffectLibrary.Haste(hasteDuration));
    }

    [PunRPC]
    void RPC_ApplyStormEnemy(int unitViewId, int damage, int shockChance)
    {
        var u = PhotonView.Find(unitViewId)?.GetComponent<Unit>();
        if (u == null) return;

        if (damage > 0) u.Model.ApplyDamageWithBarrier(damage, DamageType.Magical); // lightning ? magical

        if (shockChance > 0 && Random.Range(0f, 100f) <= shockChance) return;
     //       u.Model.statusHandler?.ApplyEffect(EffectLibrary.Shock(1));
    }

    // === Queries ===
    public bool IsInsideZone(Vector3 point, ZoneKind kind)
    {
        for (int i = 0; i < _zones.Count; i++)
        {
            var z = _zones[i];
            if (z && z.Kind == kind && z.Contains(point))
                return true;
        }
        return false;
    }

    public bool IsTargetProtectedByNegativeZone(Vector3 attackerPos, Vector3 targetPos)
    {
        bool attackerInside = false;
        bool targetInside = false;

        for (int i = 0; i < _zones.Count; i++)
        {
            var z = _zones[i];
            if (!z || z.Kind != ZoneKind.Negative) continue;

            if (z.Contains(attackerPos)) attackerInside = true;
            if (z.Contains(targetPos)) targetInside = true;

            if (targetInside && !attackerInside) return true; // outside?inside rule
        }
        return false;
    }

    public void CancelNegativeZonesOfOwner(int ownerViewId)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        for (int i = _zones.Count - 1; i >= 0; i--)
        {
            var z = _zones[i];
            if (z && z.Kind == ZoneKind.Negative && z.OwnerViewId == ownerViewId)
            {
                Destroy(z.gameObject);
                _zones.RemoveAt(i);
            }
        }
    }

    public void ReplaceAnyNegativeZone(int ownerViewId, Vector3 center, float radius, int durationTurns)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        for (int i = _zones.Count - 1; i >= 0; i--)
        {
            if (_zones[i] && _zones[i].Kind == ZoneKind.Negative)
            {
                Destroy(_zones[i].gameObject);
                _zones.RemoveAt(i);
            }
        }
        SpawnCircleZone(ZoneKind.Negative, center, radius, durationTurns, ownerViewId);
    }
}
