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

    private int _nextStructureId = 1;
    private readonly Dictionary<int, StructureBase> _byId = new Dictionary<int, StructureBase>();

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

    // ---- SPAWN (Master) ----
    public void SpawnIcePillar(Vector3 pos, UnitFaction faction, int ownerViewId, float hp, float radius, int durationTurns)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // limit 2 per owner: remove oldest by insertion order (we'll just scan for the first pillar of owner)
        int count = 0; int idxToRemove = -1;
        for (int i = 0; i < _all.Count; i++)
        {
            var s = _all[i] as IcePillar;
            if (s && s.OwnerViewId == ownerViewId)
            {
                if (idxToRemove < 0) idxToRemove = i;
                count++;
            }
        }
        if (count >= 2 && idxToRemove >= 0)
        {
            var old = _all[idxToRemove]; if (old) Destroy(old.gameObject); _all.RemoveAt(idxToRemove);
        }

        _view.RPC(nameof(RPC_CreateIcePillar), RpcTarget.All,
                  pos, (int)faction, ownerViewId, hp, radius, durationTurns);
    }

    public void SpawnBonfire(Vector3 pos, UnitFaction faction, int ownerViewId, int healPerTick, float radius, int durationTurns)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        _view.RPC(nameof(RPC_CreateBonfire), RpcTarget.All, pos, (int)faction, ownerViewId, healPerTick, radius, durationTurns);
    }

    [PunRPC]
    void RPC_CreateIcePillar(Vector3 pos, int factionInt, int ownerViewId, float hp, float radius, int durationTurns)
    {
        var s = IcePillar.Create(pos, (UnitFaction)factionInt, ownerViewId, hp, radius, durationTurns);
        _all.Add(s);
        s.NetId = _nextStructureId++;
        _byId[s.NetId] = s;

        if (icePillarPrefab && s)
        {
            var vis = Instantiate(icePillarPrefab, s.transform);
            vis.transform.localPosition = Vector3.zero;

            if (!s.GetComponent<HealthBarSpawner>())
                s.gameObject.AddComponent<HealthBarSpawner>();
        }
    }

    [PunRPC]
    void RPC_CreateBonfire(Vector3 pos, int factionInt, int ownerViewId, int healPerTick, float radius, int durationTurns)
    {
        var s = Bonfire.Create(pos, (UnitFaction)factionInt, ownerViewId, healPerTick, radius, durationTurns);
        _all.Add(s);
        s.NetId = _nextStructureId++;
        _byId[s.NetId] = s;

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
        if (!PhotonNetwork.IsMasterClient) return;

        // 1) Turn-based lifetime tick + cleanup
        for (int i = _all.Count - 1; i >= 0; i--)
        {
            var s = _all[i];
            if (!s) { _all.RemoveAt(i); continue; }
            s.OnTurnBegan(factionStartingTurn); // may destroy itself
            if (!s) { _all.RemoveAt(i); }
        }

        // 2) Bonfire healing (now we just use what's left in _all)
        var bonfires = new List<Bonfire>();
        for (int i = 0; i < _all.Count; i++)
        {
            var s = _all[i];
            if (!s || s.Kind != StructureKind.Bonfire) continue;
            if (s.Faction != factionStartingTurn) continue;
            var b = s as Bonfire;
            if (b != null) bonfires.Add(b);
        }
        if (bonfires.Count == 0) return;

        var units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        if (units == null || units.Length == 0) return;

        var healByUnitViewId = new Dictionary<int, int>(32);

        foreach (var u in units)
        {
            if (u == null || u.Model == null) continue;
            if (u.Model.Faction != factionStartingTurn) continue;
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
                var pv = u.GetComponent<PhotonView>() ??
                         u.GetComponentInParent<PhotonView>() ??
                         u.GetComponentInChildren<PhotonView>();
                if (pv != null) healByUnitViewId[pv.ViewID] = totalHeal;
            }
        }

        foreach (var kvp in healByUnitViewId)
            _view.RPC(nameof(RPC_HealUnit), RpcTarget.All, kvp.Key, kvp.Value);
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

    public void UnregisterStructure(int id, StructureBase s)
    {
        if (_byId.TryGetValue(id, out var curr) && curr == s)
            _byId.Remove(id);
    }

    public IEnumerable<StructureBase> GetStructuresInCircle(Vector3 center, float radius, UnitFaction? factionFilter, StructureTargeting targeting, UnitFaction casterFaction)
    {
        float r2 = radius * radius;
        foreach (var kv in _byId)
        {
            var s = kv.Value;
            if (!s) continue;
            // faction filter
            bool okFaction = targeting switch
            {
                StructureTargeting.None => false,
                StructureTargeting.Enemy => s.Faction != casterFaction,
                StructureTargeting.Ally => s.Faction == casterFaction,
                StructureTargeting.Any => true,
                _ => false
            };
            if (!okFaction) continue;

            // distance check to structure center (you can expand to collider if needed)
            if ((s.transform.position - center).sqrMagnitude <= r2)
                yield return s;
        }
    }

    // distance from point to segment for line AOE
    public IEnumerable<StructureBase> GetStructuresNearSegment(Vector3 a, Vector3 b, float radius, StructureTargeting targeting, UnitFaction casterFaction)
    {
        float rr = radius * radius;
        foreach (var kv in _byId)
        {
            var s = kv.Value;
            if (!s) continue;

            bool okFaction = targeting switch
            {
                StructureTargeting.None => false,
                StructureTargeting.Enemy => s.Faction != casterFaction,
                StructureTargeting.Ally => s.Faction == casterFaction,
                StructureTargeting.Any => true,
                _ => false
            };
            if (!okFaction) continue;

            Vector3 p = s.transform.position;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, (b - a)) / (b - a).sqrMagnitude);
            Vector3 proj = a + t * (b - a);
            if ((p - proj).sqrMagnitude <= rr)
                yield return s;
        }
    }

    public StructureBase RaycastStructureSingle(Vector3 worldPoint, float maxPickDistance, StructureTargeting targeting, UnitFaction casterFaction)
    {
        // choose the nearest structure within small radius around click
        const float pickRadius = 0.6f;
        StructureBase best = null;
        float bestD2 = (maxPickDistance > 0 ? maxPickDistance * maxPickDistance : float.MaxValue);

        foreach (var kv in _byId)
        {
            var s = kv.Value;
            if (!s) continue;

            bool okFaction = targeting switch
            {
                StructureTargeting.None => false,
                StructureTargeting.Enemy => s.Faction != casterFaction,
                StructureTargeting.Ally => s.Faction == casterFaction,
                StructureTargeting.Any => true,
                _ => false
            };
            if (!okFaction) continue;

            float d2 = (s.transform.position - worldPoint).sqrMagnitude;
            if (d2 <= Mathf.Min(pickRadius * pickRadius, bestD2))
            {
                bestD2 = d2; best = s;
            }
        }
        return best;
    }

    public void DamageStructure(int netId, int amount)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (_byId.TryGetValue(netId, out var s) && s)
        {
            s.ApplyDamage(amount);
            _view.RPC(nameof(RPC_OnStructureDamaged), RpcTarget.Others, netId, amount);
            if (!s) // destroyed in ApplyDamage
                _view.RPC(nameof(RPC_OnStructureDestroyed), RpcTarget.Others, netId);
        }
    }

    [PunRPC]
    void RPC_OnStructureDamaged(int netId, int amount)
    {
        if (_byId.TryGetValue(netId, out var s) && s) s.ApplyDamage(amount);
    }

    [PunRPC]
    void RPC_OnStructureDestroyed(int netId)
    {
        if (_byId.TryGetValue(netId, out var s) && s) Destroy(s.gameObject);
    }

    [PunRPC]
    public void RPC_RequestStructureHit(int netId, int damage)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        DamageStructure(netId, damage);
    }
}
