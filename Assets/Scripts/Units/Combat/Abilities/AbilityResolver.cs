using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public sealed class AbilityResolver : MonoBehaviourPun
{
    public static AbilityResolver Instance { get; private set; }
    PhotonView _view;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _view = GetComponent<PhotonView>() ?? gameObject.AddComponent<PhotonView>();
    }

    public static bool CanCast(Unit caster, UnitAbility ability, Unit[] targets, out string reason)
    {
        reason = null;
        if (caster == null || ability == null) { reason = "No caster/ability."; return false; }
        if (!caster.Model.CanAct()) { reason = "No actions."; return false; }
        return true;
    }

    public void RequestCast(UnitController casterCtrl, UnitAbility ability, Unit[] targets)
    {
        if (casterCtrl == null || ability == null) return;
        var ids = PackTargets(targets);

        // Use index instead of non-existent ability.Id
        int abilityIndex = casterCtrl.unit.Model.Abilities.IndexOf(ability);
        _view.RPC(nameof(RPC_RequestCast), RpcTarget.MasterClient, casterCtrl.photonView.ViewID, abilityIndex, ids);
    }

    [PunRPC]
    void RPC_RequestCast(int casterViewId, int abilityIndex, int[] targetViewIds, PhotonMessageInfo info)
    {
        var casterCtrl = FindByView<UnitController>(casterViewId);
        UnitAbility ability = null;
        if (casterCtrl != null &&
            abilityIndex >= 0 && abilityIndex < casterCtrl.unit.Model.Abilities.Count)
            ability = casterCtrl.unit.Model.Abilities[abilityIndex];

        var targets = UnpackTargets<Unit>(targetViewIds);

        // TODO: validate + compute via CombatCalculator & effects; for now echo no-op
        _view.RPC(nameof(RPC_ResolveAbility), RpcTarget.All, new byte[] { 1 });
    }

    [PunRPC]
    void RPC_ResolveAbility(byte[] blob)
    {
        // TODO: parse deterministic result and apply on all clients
    }

    // ---- helpers ----
    int[] PackTargets(Unit[] targets)
    {
        if (targets == null) return new int[0];
        var list = new List<int>(targets.Length);
        foreach (var t in targets)
        {
            if (t == null) continue;
            var view = t.GetComponent<PhotonView>();
            if (view != null) list.Add(view.ViewID);
        }
        return list.ToArray();
    }

    T[] UnpackTargets<T>(int[] viewIds) where T : Component
    {
        if (viewIds == null) return new T[0];
        var list = new List<T>(viewIds.Length);
        foreach (var id in viewIds)
        {
            var v = PhotonView.Find(id);
            if (v)
            {
                var c = v.GetComponent<T>();
                if (c) list.Add(c);
            }
        }
        return list.ToArray();
    }

    T FindByView<T>(int viewId) where T : Component
    {
        var v = PhotonView.Find(viewId);
        return v ? v.GetComponent<T>() : null;
    }
}