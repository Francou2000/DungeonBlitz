using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }
    private PhotonView _view;

    private int _nextZoneId = 1;
    private readonly Dictionary<int, ZoneBase> _byId = new();

    private readonly List<ZoneBase> _zones = new List<ZoneBase>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _view = GetComponent<PhotonView>() ?? gameObject.AddComponent<PhotonView>();
    }

    void OnEnable() { TurnManager.OnTurnBegan += HandleTurnBegan; }
    void OnDisable() { TurnManager.OnTurnBegan -= HandleTurnBegan; }

    void HandleTurnBegan(UnitFaction activeFaction)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        for (int i = _zones.Count - 1; i >= 0; i--)
        {
            var z = _zones[i];
            if (!z) { _zones.RemoveAt(i); continue; }

            // If zone has an owner, only tick on that owner's faction turn
            if (z.OwnerViewId != -1)
            {
                var pv = PhotonView.Find(z.OwnerViewId);
                var owner = pv ? pv.GetComponent<UnitModel>() : null;
                if (owner != null && owner.Faction != activeFaction)
                    continue; // not this owner's turn -> don't decrement
            }

            if (z.RemainingTurns > 0) z.RemainingTurns--;
            if (z.RemainingTurns == 0)
            {
                DestroyZoneAuth(z); // replicated destroy
            }
        }
    }

    // === Authoritative API ===
    public void SpawnCircleZone(ZoneKind kind, Vector3 center, float radius, int durationTurns, int ownerViewId = -1)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        int netId = _nextZoneId++;
        _view.RPC(nameof(RPC_CreateCircleZone), RpcTarget.All,
            netId, (int)kind, center, radius, durationTurns, ownerViewId);
    }

    [PunRPC]
    void RPC_CreateCircleZone(int netId, int kindInt, Vector3 center, float radius, int durationTurns, int ownerViewId)
    {
        var kind = (ZoneKind)kindInt;
        var go = new GameObject($"{kind}Zone");
        ZoneBase zone = null;
        switch (kind)
        {
            case ZoneKind.Negative: zone = go.AddComponent<NegativeZone>(); break;
            case ZoneKind.Frozen: zone = go.AddComponent<FrozenZone>(); break;
            default: Debug.LogWarning($"[ZoneManager] Unknown zone kind {kind}"); Destroy(go); return;
        }
        zone.NetId = netId;
        zone.Init(kind, center, radius, durationTurns, ownerViewId);
        _zones.Add(zone);
        _byId[netId] = zone;
    }

    public void SpawnStormCrossing(Vector3 a, Vector3 b, float width, int durationTurns,
                                   int ownerFaction, int allyHasteDuration, int enemyDamage, int shockChance)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        int netId = _nextZoneId++;
        _view.RPC(nameof(RPC_CreateStormCrossing), RpcTarget.All,
            netId, a, b, width, durationTurns, ownerFaction, allyHasteDuration, enemyDamage, shockChance);
    }

    [PunRPC]
    void RPC_CreateStormCrossing(int netId, Vector3 a, Vector3 b, float width, int durationTurns,
                                 UnitFaction ownerFaction, int allyHasteDur, int enemyDmg, int shockChance)
    {
        var go = new GameObject("StormCrossingZone");
        var z = go.AddComponent<StormCrossingZone>();
        z.NetId = netId;
        z.InitSegment(a, b, width, ownerFaction, allyHasteDur, enemyDmg, shockChance, durationTurns);
        _zones.Add(z);
        _byId[netId] = z;
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
    }

    [PunRPC]
    void RPC_ApplyStormEnemy(int unitViewId, int damage, int shockChance)
    {
        var u = PhotonView.Find(unitViewId)?.GetComponent<Unit>();
        if (u == null) return;

        if (damage > 0) u.Model.ApplyDamageWithBarrier(damage, DamageType.Magical); // lightning ? magical

        if (shockChance > 0 && Random.Range(0f, 100f) <= shockChance) return;
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
                DestroyZoneAuth(z);
        }
    }

    public void ReplaceAnyNegativeZone(int ownerViewId, Vector3 center, float radius, int durationTurns)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        for (int i = _zones.Count - 1; i >= 0; i--)
        {
            var z = _zones[i];
            if (z && z.Kind == ZoneKind.Negative)
                DestroyZoneAuth(z);
        }
        SpawnCircleZone(ZoneKind.Negative, center, radius, durationTurns, ownerViewId);
    }

    void DestroyZoneAuth(ZoneBase z)
    {
        if (!PhotonNetwork.IsMasterClient || z == null) return;

        int id = z.NetId;
        _byId.Remove(id);
        _zones.Remove(z);
        Destroy(z.gameObject);

        _view.RPC(nameof(RPC_DestroyZone), RpcTarget.Others, id);
    }

    [PunRPC]
    void RPC_DestroyZone(int netId)
    {
        if (_byId.TryGetValue(netId, out var z) && z)
        {
            _byId.Remove(netId);
            _zones.Remove(z);
            Destroy(z.gameObject);
        }
    }
}
