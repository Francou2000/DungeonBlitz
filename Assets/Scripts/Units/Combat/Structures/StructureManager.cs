using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public sealed class StructureManager : MonoBehaviourPun
{
    public static StructureManager Instance { get; private set; }
    PhotonView _view;
    readonly List<StructureBase> _all = new List<StructureBase>();

    public GameObject icePillarPrefab;
    public GameObject bonfirePrefab;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _view = GetComponent<PhotonView>() ?? gameObject.AddComponent<PhotonView>();

        TurnManager.OnTurnBegan += HandleTurnBegan;
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.OnTurnBegan -= HandleTurnBegan;
    }

    void Update()
    {
        double now = PhotonNetwork.Time;
        for (int i = _all.Count - 1; i >= 0; i--)
        {
            var s = _all[i];
            if (!s || s.IsExpired(now)) { if (s) Destroy(s.gameObject); _all.RemoveAt(i); }
        }
    }

    // ---- SPAWN (Master) ----
    public void SpawnIcePillar(Vector3 pos, UnitFaction faction, int ownerViewId, float hp, float radius, float durationSec)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // limit 2 per owner: remove oldest if needed
        int count = 0; int oldestIdx = -1; double oldestT = double.MaxValue;
        for (int i = 0; i < _all.Count; i++)
        {
            var s = _all[i] as IcePillar;
            if (s && s.OwnerViewId == ownerViewId)
            {
                count++;
                if (s.ExpiresAt < oldestT) { oldestT = s.ExpiresAt; oldestIdx = i; }
            }
        }
        if (count >= 2 && oldestIdx >= 0)
        {
            var old = _all[oldestIdx]; if (old) Destroy(old.gameObject); _all.RemoveAt(oldestIdx);
        }

        double expiresAt = PhotonNetwork.Time + Mathf.Max(0.1f, durationSec);
        _view.RPC(nameof(RPC_CreateIcePillar), RpcTarget.All,
                  pos, (int)faction, ownerViewId, hp, radius, expiresAt);
    }

    public void SpawnBonfire(Vector3 pos, UnitFaction faction, int ownerViewId, int healPerTick, float radius, float durationSec)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        double expiresAt = PhotonNetwork.Time + Mathf.Max(0.1f, durationSec);
        _view.RPC(nameof(RPC_CreateBonfire), RpcTarget.All, pos, (int)faction, ownerViewId, healPerTick, radius, expiresAt);
    }

    [PunRPC]
    void RPC_CreateIcePillar(Vector3 pos, int factionInt, int ownerViewId, float hp, float radius, double expiresAt)
    {
        var s = IcePillar.Create(
            pos,
            (UnitFaction)factionInt,
            ownerViewId,
            hp,
            radius,
            expiresAt
        );
        _all.Add(s);

        if (icePillarPrefab && s)
        {
            var vis = Instantiate(icePillarPrefab, s.transform);
            vis.transform.localPosition = Vector3.zero;

            if (!s.GetComponent<HealthBarSpawner>())
                s.gameObject.AddComponent<HealthBarSpawner>();
        }
    }


    [PunRPC]
    void RPC_CreateBonfire(Vector3 pos, int factionInt, int ownerViewId, int healPerTick, float radius, double expiresAt)
    {
        var s = Bonfire.Create(pos, (UnitFaction)factionInt, ownerViewId, healPerTick, radius, expiresAt);
        _all.Add(s);

        if (bonfirePrefab && s)
        {
            var vis = Instantiate(bonfirePrefab, s.transform);
            vis.transform.localPosition = Vector3.zero;

            var drv = vis.GetComponentInChildren<BonfireVisual>() ?? vis.AddComponent<BonfireVisual>();
            drv.bound = s;
            var aura = vis.transform.Find("Aura");
            if (aura) drv.aura = aura;

            if (!s.GetComponent<HealthBarSpawner>())
                s.gameObject.AddComponent<HealthBarSpawner>();
        }
    }

    // ---- QUERIES ----
    public bool HasStructureCoverBetween(Vector3 from, Vector3 to, out bool hasMedium, out bool hasHeavy)
    {
        hasMedium = hasHeavy = false;
        var dir = to - from; float len = dir.magnitude; if (len < 0.0001f) return false;
        dir /= len;

        for (int i = 0; i < _all.Count; i++)
        {
            var s = _all[i];
            if (!s || s.Cover == CoverType.None) continue;

            // treat as sphere radius 0.5–0.75 around structure position
            float r = Mathf.Max(0.45f, s.Radius);
            // distance from line segment to center
            var ap = s.transform.position - from;
            float t = Mathf.Clamp(Vector3.Dot(ap, dir), 0f, len);
            float dist = Vector3.Distance(from + dir * t, s.transform.position);

            if (dist <= r + 0.01f)
            {
                if (s.Cover == CoverType.Heavy) hasHeavy = true; else hasMedium = true;
            }
        }
        return hasMedium || hasHeavy;
    }

    public void DamageStructuresNear(Vector3 worldPos, float radius, int amount)
    {
        for (int i = 0; i < _all.Count; i++)
        {
            var s = _all[i];
            if (!s) continue;
            if (Vector3.Distance(s.transform.position, worldPos) <= radius + s.Radius)
                s.TakeDamage(amount);
        }
    }

    private void HandleTurnBegan(UnitFaction factionStartingTurn)
    {
        // Only the master authoritatively computes the tick and mirrors it to everyone.
        if (!PhotonNetwork.IsMasterClient) return;

        // Gather all active bonfires of THIS faction
        // (_all is already tracking every StructureBase we spawn)
        var bonfires = new List<Bonfire>();
        for (int i = 0; i < _all.Count; i++)
        {
            var s = _all[i];
            if (!s || s.Kind != StructureKind.Bonfire) continue;
            if (s.Faction != factionStartingTurn) continue;

            var b = s as Bonfire;
            if (b != null && !b.IsExpired(Photon.Pun.PhotonNetwork.Time))
                bonfires.Add(b);
        }

        if (bonfires.Count == 0) return;

        // Find all alive units of the same faction
        var units = UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        if (units == null || units.Length == 0) return;

        // Compute total heal per unit (avoid double-healing if standing in overlap)
        // !!! bonfire heals STACK additively if overlapping. !!!
        // If you prefer "no stacking", replace the += with Mathf.Max(...)
        var healByUnitViewId = new Dictionary<int, int>(32);

        foreach (var u in units)
        {
            if (u == null || u.Model == null) continue;
            if (u.Model.Faction != factionStartingTurn) continue;  // heal only allies at their own turn
            if (!u.Model.IsAlive()) continue;

            int totalHeal = 0;
            for (int i = 0; i < bonfires.Count; i++)
            {
                var bf = bonfires[i];
                if (!bf) continue;
                if (bf.IsInRange(u)) totalHeal += Mathf.Max(0, bf.HealPerTick);
            }

            if (totalHeal > 0)
            {
                var pv = u.GetComponent<Photon.Pun.PhotonView>() ??
                         u.GetComponentInParent<Photon.Pun.PhotonView>() ??
                         u.GetComponentInChildren<Photon.Pun.PhotonView>();
                if (pv != null) healByUnitViewId[pv.ViewID] = totalHeal;
            }
        }

        // Broadcast heals (each client applies locally in RPC_HealUnit)
        foreach (var kvp in healByUnitViewId)
        {
            _view.RPC(nameof(RPC_HealUnit), Photon.Pun.RpcTarget.All, kvp.Key, kvp.Value);
        }
    }

    [PunRPC] 
    void RPC_HealUnit(int targetViewId, int amount)
    {
        var pv = PhotonView.Find(targetViewId);
        if (pv == null) return;
        var unit = pv.GetComponent<Unit>();
        if (unit == null || unit.Model == null) return;

        unit.Model.Heal(amount); // local HP update + UI on every client
    }
}
