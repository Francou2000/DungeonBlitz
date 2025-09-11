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
        if (caster == null || ability == null) { reason = "No caster/ability"; return false; }
        if (!caster.Model.CanAct()) { reason = "No actions left"; return false; }

        // Resource costs
        foreach (var cost in ability.resourceCosts)
        {
            if (caster.Model.GetRes(cost.key) < cost.amount)
            {
                reason = $"Need {cost.amount} {cost.key}";
                return false;
            }
        }

        // Adrenaline threshold
        if (ability.minAdrenaline > 0 && caster.Model.Adrenaline < ability.minAdrenaline)
        {
            reason = $"Need ≥ {ability.minAdrenaline} adrenaline";
            return false;
        }

        // State requirements (Form, Weapon)
        foreach (var state in ability.requiredStates)
        {
            var parts = state.Split(':'); // e.g., "Form:Fire"
            if (parts.Length == 2)
            {
                var current = caster.Model.GetState(parts[0]);
                if (current != parts[1])
                {
                    reason = $"Requires {parts[0]} = {parts[1]}";
                    return false;
                }
            }
        }

        // Tags on caster
        foreach (var tag in ability.requiredTags)
            if (!caster.Model.statusHandler.HasTag(tag))
            {
                reason = $"Requires {tag}";
                return false;
            }

        // Tags on target
        if (targets != null && targets.Length > 0 && targets[0] != null)
        {
            foreach (var tag in ability.requiredTargetTags)
                if (!targets[0].Model.statusHandler.HasTag(tag))
                {
                    reason = $"Target must have {tag}";
                    return false;
                }
        }

        // Range
        if (targets != null && targets.Length > 0 && targets[0] != null)
        {
            var t = targets[0];
            if (!CombatCalculator.IsInRange(caster.transform.position, t.transform.position, ability.range))
            {
                reason = "Out of range";
                return false;
            }
        }

        return true;
    }

    public void RequestCast(UnitController casterCtrl, UnitAbility ability, Unit[] targets)
    {
        if (casterCtrl == null || ability == null) return;

        int abilityIndex = casterCtrl.unit.Model.Abilities.IndexOf(ability);
        if (abilityIndex < 0) return;

        int[] targetViewIds = PackTargets(targets);
        _view.RPC(nameof(RPC_RequestCast), RpcTarget.MasterClient,
            casterCtrl.photonView.ViewID, abilityIndex, targetViewIds);
    }

    [PunRPC]
    private void RPC_RequestCast(int casterViewId, int abilityIndex, int[] targetViewIds, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var casterCtrl = FindByView<UnitController>(casterViewId);
        if (casterCtrl == null) return;

        // Ability lookup from caster's list
        var list = casterCtrl.unit.Model.Abilities;
        if (abilityIndex < 0 || abilityIndex >= list.Count) return;
        var ability = list[abilityIndex];

        var targets = UnpackTargets<Unit>(targetViewIds);
        if (!CanCast(casterCtrl.unit, ability, targets, out _)) return;

        // Build target list according to area type
        var computedTargets = new List<Unit>();

        if (ability.areaType == AreaType.Single)
        {
            var t = (targets != null && targets.Length > 0) ? targets[0] : null;
            if (t != null) computedTargets.Add(t);
        }
        else if (ability.areaType == AreaType.Circle)
        {
            // Center on the first provided target (or you can allow ground-targeting later)
            var center = ((targets != null && targets.Length > 0) ? targets[0].transform.position : casterCtrl.unit.transform.position);
            computedTargets = CombatCalculator.GetUnitsInRadius(center, ability.aoeRadius, casterCtrl.unit);
        }
        else if (ability.areaType == AreaType.Line)
        {
            var t = (targets != null && targets.Length > 0) ? targets[0] : null;
            if (t != null)
            {
                computedTargets = CombatCalculator.GetLineTargets(
                    casterCtrl.unit, t, ability.lineRange,
                    Mathf.Max(1, ability.lineMaxTargets),
                    Mathf.Clamp01(ability.lineAlignmentTolerance)
                );
            }
        }

        // Prepare per-target results
        var outIds = new List<int>(computedTargets.Count);
        var outHits = new List<bool>(computedTargets.Count);
        var outDamages = new List<int>(computedTargets.Count);
        var outTypes = new List<int>(computedTargets.Count); // 0=phys,1=mag,2=mixed (cosmetic)

        int idx = 0;
        foreach (var tgt in computedTargets)
        {
            bool hasMediumCover, hasHeavyCover;
            CombatCalculator.CheckCover(
                casterCtrl.unit.transform.position,
                tgt.transform.position,
                out hasMediumCover,
                out hasHeavyCover
            );

            int flankCount = CombatCalculator.CountFlankingAllies(tgt, casterCtrl.unit);
            bool isFlanked = flankCount > 0;

            int attackerAffinity = casterCtrl.unit.Model.Affinity;
            float hitchance = CombatCalculator.GetHitChance(
                ability.baseHitChance,
                attackerAffinity,
                flankCount,
                isFlanked,
                hasMediumCover,
                hasHeavyCover
            );

            bool hitProb = (Random.Range(0f, 100f) <= hitchance);
            int damageTotal = 0;
            int dtype = 0;

            if (hitProb)
            {
                if (ability.isMixedDamage)
                {
                    int strength = casterCtrl.unit.Model.Strength;
                    int mpower = casterCtrl.unit.Model.MagicPower;
                    int armor = tgt.Model.Armor;
                    int mr = tgt.Model.MagicResistance;

                    damageTotal = CombatCalculator.CalculateMixedDamage(
                        ability.baseDamage, strength, armor, mpower, mr, ability.mixedPhysicalPercent
                    );
                    dtype = 2; // mixed (cosmetic)
                }
                else
                {
                    bool isPhysical = ability.damageSource == DamageType.Physical;
                    int attackerStat = isPhysical ? casterCtrl.unit.Model.Strength : casterCtrl.unit.Model.MagicPower;
                    int defenderStat = isPhysical ? tgt.Model.Armor : tgt.Model.MagicResistance;
                    damageTotal = Mathf.RoundToInt(CombatCalculator.CalculateDamage(ability.baseDamage, attackerStat, defenderStat));
                    dtype = isPhysical ? 0 : 1;
                }

                // HP-missing bonus (Shadow kit)
                if (ability.bonusPerMissingHpPercent > 0)
                    damageTotal = CombatCalculator.ApplyMissingHpBonus(damageTotal, tgt, ability.bonusPerMissingHpPercent);

                // Line collateral for non-primary targets
                if (ability.areaType == AreaType.Line && idx > 0 && ability.lineCollateralPercent < 100)
                    damageTotal = CombatCalculator.ApplyCollateralPercent(damageTotal, ability.lineCollateralPercent);
            }

            outIds.Add(tgt.GetComponent<PhotonView>()?.ViewID ?? -1);
            outHits.Add(hitProb);
            outDamages.Add(Mathf.Max(0, damageTotal));
            outTypes.Add(dtype);
            idx++;
        }

        // Spend AP on ALL in Resolve (not here). Broadcast batched result arrays.
        _view.RPC(nameof(RPC_ResolveAbility_Area), RpcTarget.All,
            casterViewId, abilityIndex,
            outIds.ToArray(),
            outHits.ToArray(),
            outDamages.ToArray(),
            outTypes.ToArray()
        );

        var target = (targets != null && targets.Length > 0) ? targets[0] : null;

        bool hit = false;
        int damage = 0;
        DamageType damageType = ability.damageSource;
        if (target != null)
        {
            // cover
            CombatCalculator.CheckCover(
                casterCtrl.unit.transform.position,
                target.transform.position,
                out bool hasMediumCover,
                out bool hasHeavyCover
            );

            // flanking
            int flankCount = CombatCalculator.CountFlankingAllies(target, casterCtrl.unit);
            bool isFlanked = flankCount > 0;

            // attacker affinity bonus (if you track it via StatusEffectHandler or stats; fallback 0)
            int attackerAffinity = casterCtrl.unit.Model.Affinity;

            // hitchance is 0..100
            float hitchance = CombatCalculator.GetHitChance(
                ability.baseHitChance,          // baseChance from ability
                attackerAffinity,               // affinity bonus
                flankCount,                     // +10 per flanker
                isFlanked,
                hasMediumCover,
                hasHeavyCover
            );

            // roll against 0..100
            hit = (Random.Range(0f, 100f) <= hitchance);

            if (hit)
            {
                // damage source: Strength (physical) vs MagicPower (magical)
                bool isPhysical = ability.damageSource == DamageType.Physical;

                int attackerStat = isPhysical ? casterCtrl.unit.Model.Strength : casterCtrl.unit.Model.MagicPower;
                int defenderStat = isPhysical ? target.Model.Armor : target.Model.MagicResistance;

                float result = CombatCalculator.CalculateDamage(
                    ability.baseDamage,        // raw/base damage from ability
                    attackerStat,
                    defenderStat
                );
                damage = Mathf.Max(0, Mathf.RoundToInt(result));
            }   
        }

        // IMPORTANT: Do NOT spend AP here (to avoid double). Spend in Resolve on ALL clients.
        _view.RPC(nameof(RPC_ResolveAbility), RpcTarget.All,
            casterViewId,
            abilityIndex,
            target != null ? target.GetComponent<PhotonView>()?.ViewID ?? -1 : -1,
            hit,
            damage,
            damageType
        );
    }

    [PunRPC]
    private void RPC_ResolveAbility(int casterViewId, int abilityIndex, int targetViewId, bool hit, int damage, DamageType damageType)
    {
        var casterCtrl = FindByView<UnitController>(casterViewId);
        var target = FindByView<Unit>(targetViewId);

        // Spend AP deterministically on all
        if (casterCtrl != null)
        {
            var list = casterCtrl.unit.Model.Abilities;
            if (abilityIndex >= 0 && abilityIndex < list.Count)
            {
                var ab = list[abilityIndex];
                casterCtrl.unit.Model.SpendAction(ab.actionCost);

                // Deduct resources
                foreach (var cost in ab.resourceCosts)
                    casterCtrl.unit.Model.TryConsume(cost.key, cost.amount);

                // Adrenaline consumption (if you model it that way)
                if (ab.minAdrenaline > 0)
                    casterCtrl.unit.Model.SpendAdrenaline(ab.minAdrenaline);
            }
        }

        // Damage
        if (target != null && hit && damage > 0)
        {
            target.Model.ApplyDamageWithBarrier(damage, damageType);
        }

        // Apply attached status effects using your handler + ability fields
        if (casterCtrl != null)
        {
            var list = casterCtrl.unit.Model.Abilities;
            if (abilityIndex >= 0 && abilityIndex < list.Count)
            {
                var ab = list[abilityIndex];

                if (target != null && ab.appliedEffects != null && ab.appliedEffects.Count > 0)
                {
                    var handler = target.GetComponent<StatusEffectHandler>();
                    if (handler != null)
                    {
                        foreach (var eff in ab.appliedEffects)
                        {
                            // chance is in 0..100 per your UnitController
                            if (Random.Range(0f, 100f) <= ab.statusEffectChance)
                            {
                                handler.ApplyEffect(eff);
                            }
                        }
                    }
                }
            }
        }
    }

    [PunRPC]
    private void RPC_ResolveAbility_Area(int casterViewId, int abilityIndex,
                                     int[] targetViewIds, bool[] hits, int[] damages, DamageType[] damageTypes)
    {
        var casterCtrl = FindByView<UnitController>(casterViewId);

        // Spend AP deterministically
        if (casterCtrl != null)
        {
            var list = casterCtrl.unit.Model.Abilities;
            if (abilityIndex >= 0 && abilityIndex < list.Count)
            {
                var ab = list[abilityIndex];
                casterCtrl.unit.Model.SpendAction(ab.actionCost);

                // Resource costs & adrenaline consumption 
                foreach (var cost in ab.resourceCosts)
                    casterCtrl.unit.Model.TryConsume(cost.key, cost.amount);
                if (ab.adrenalineCost > 0)
                    casterCtrl.unit.Model.SpendAdrenaline(ab.adrenalineCost);
            }
        }

        // Apply results per target
        int count = targetViewIds != null ? targetViewIds.Length : 0;
        for (int i = 0; i < count; i++)
        {
            var target = FindByView<Unit>(targetViewIds[i]);
            if (target == null || !hits[i] || damages[i] <= 0) continue;

            target.Model.ApplyDamageWithBarrier(damages[i], damageTypes[i]);
        }

        // TO TALK: apply attached effects from the ability to the PRIMARY target only, or to all — your choice.
        // If to all targets:
        if (casterCtrl != null)
        {
            var list = casterCtrl.unit.Model.Abilities;
            if (abilityIndex >= 0 && abilityIndex < list.Count)
            {
                var ab = list[abilityIndex];
                if (ab.appliedEffects != null && ab.appliedEffects.Count > 0 && targetViewIds != null)
                {
                    for (int i = 0; i < targetViewIds.Length; i++)
                    {
                        var target = FindByView<Unit>(targetViewIds[i]);
                        if (target == null) continue;

                        var handler = target.GetComponent<StatusEffectHandler>();
                        if (handler == null) continue;

                        foreach (var eff in ab.appliedEffects)
                        {
                            if (Random.Range(0f, 100f) <= ab.statusEffectChance)
                                handler.ApplyEffect(eff);
                        }
                    }
                }
            }
        }
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