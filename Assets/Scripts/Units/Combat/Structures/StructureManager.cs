using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public sealed class StructureManager : MonoBehaviourPun
{
    public static StructureManager Instance { get; private set; }
    PhotonView _view;
    readonly List<StructureBase> _all = new List<StructureBase>();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _view = GetComponent<PhotonView>() ?? gameObject.AddComponent<PhotonView>();
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
    public void SpawnIcePillar(Vector3 pos, UnitFaction faction, int ownerViewId, int hp, float duration)
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

        _view.RPC(nameof(RPC_CreateIcePillar), RpcTarget.All, pos, (int)faction, ownerViewId, hp,
                  PhotonNetwork.Time + Mathf.Max(0.1f, duration));
    }

    public void SpawnBonfire(Vector3 pos, UnitFaction faction, int ownerViewId, int healPerTick, float radius, float duration)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        _view.RPC(nameof(RPC_CreateBonfire), RpcTarget.All, pos, (int)faction, ownerViewId,
                  healPerTick, radius, PhotonNetwork.Time + Mathf.Max(0.1f, duration));
    }

    [PunRPC]
    void RPC_CreateIcePillar(Vector3 pos, int factionInt, int ownerViewId, int hp, double expiresAt)
    {
        var s = IcePillar.Create(pos, (UnitFaction)factionInt, ownerViewId, hp, expiresAt);
        _all.Add(s);
    }

    [PunRPC]
    void RPC_CreateBonfire(Vector3 pos, int factionInt, int ownerViewId, int healPerTick, float radius, double expiresAt)
    {
        var s = Bonfire.Create(pos, (UnitFaction)factionInt, ownerViewId, healPerTick, radius, expiresAt);
        _all.Add(s);
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

    // Start-of-turn aura tick (Bonfire)
    public void ApplyStartTurnAuras(Unit unit)
    {
        if (!unit) return;
        var pos = unit.transform.position;
        for (int i = 0; i < _all.Count; i++)
        {
            var b = _all[i] as Bonfire;
            if (!b) continue;
            if (b.Faction != unit.Model.Faction) continue;
            if (Vector3.Distance(b.transform.position, pos) <= b.Radius + 0.01f)
            {
                unit.Model.Heal(b.HealPerTick); // use your actual heal method
            }
        }
    }
}
