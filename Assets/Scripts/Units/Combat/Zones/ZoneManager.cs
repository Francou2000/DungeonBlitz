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

    void Update()
    {
        // GC expired zones 
        double now = PhotonNetwork.Time;
        for (int i = _zones.Count - 1; i >= 0; i--)
        {
            var z = _zones[i];
            if (!z) { _zones.RemoveAt(i); continue; }
            if (z.IsExpired(now))
            {
                Destroy(z.gameObject);
                _zones.RemoveAt(i);
            }
        }
    }

    // === Authoritative API ===
    public void SpawnCircleZone(ZoneKind kind, Vector3 center, float radius, float durationSec)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        double expiresAt = PhotonNetwork.Time + Mathf.Max(0.1f, durationSec);
        _view.RPC(nameof(RPC_CreateCircleZone), RpcTarget.All, (int)kind, center, radius, expiresAt);
    }

    [PunRPC]
    void RPC_CreateCircleZone(int kindInt, Vector3 center, float radius, double expiresAt)
    {
        var kind = (ZoneKind)kindInt;
        var go = new GameObject($"{(ZoneKind)kind}Zone");
        ZoneBase zone = null;

        switch (kind)
        {
            case ZoneKind.Negative: zone = go.AddComponent<NegativeZone>(); break;
            case ZoneKind.Frozen: zone = go.AddComponent<FrozenZone>(); break;
            default:
                Debug.LogWarning($"[ZoneManager] Unknown zone kind {kind}");
                Destroy(go);
                return;
        }

        zone.Init(kind, center, radius, expiresAt);
        _zones.Add(zone);
    }
    public void SpawnStormCrossing(Vector3 a, Vector3 b, float width, float durationSec,
                               int ownerFaction, int allyHasteDuration, int enemyDamage, int shockChance)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        double expiresAt = PhotonNetwork.Time + Mathf.Max(0.1f, durationSec);
        _view.RPC(nameof(RPC_CreateStormCrossing), RpcTarget.All,
            a, b, width, expiresAt, ownerFaction, allyHasteDuration, enemyDamage, shockChance);
    }

    [PunRPC]
    void RPC_CreateStormCrossing(Vector3 a, Vector3 b, float width, double expiresAt,
                                 UnitFaction ownerFaction, int allyHasteDur, int enemyDmg, int shockChance)
    {
        var go = new GameObject("StormCrossingZone");
        var z = go.AddComponent<StormCrossingZone>();
        z.InitSegment(a, b, width, ownerFaction, allyHasteDur, enemyDmg, shockChance, expiresAt);
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
            if (z.IsExpired(PhotonNetwork.Time)) continue;
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
            // Optional: trigger only once per move even if multiple zones overlap
            // break;
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
}
