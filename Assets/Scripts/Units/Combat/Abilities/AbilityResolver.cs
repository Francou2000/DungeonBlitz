using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public sealed class AbilityResolver : MonoBehaviourPun
{
    public static AbilityResolver Instance { get; private set; }
    PhotonView _view;

    const float MeleeRangeMeters = 1.5f; // 1 tile (tweak if your grid differs)

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

        // --- Resource costs ---
        if (ability.resourceCosts != null)
        {
            foreach (var cost in ability.resourceCosts)
            {
                if (caster.Model.GetRes(cost.key) < cost.amount)
                {
                    reason = $"Need {cost.amount} {cost.key}";
                    return false;
                }
            }
        }

        // --- Adrenaline threshold ---
        if (ability.minAdrenaline > 0 && caster.Model.Adrenaline < ability.minAdrenaline)
        {
            reason = $"Need ≥ {ability.minAdrenaline} adrenaline";
            return false;
        }

        // --- State requirements (Form, Weapon, etc.) ---
        if (ability.requiredStates != null)
        {
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
        }

        // --- Tags on caster ---
        if (ability.requiredTags != null)
        {
            foreach (var tag in ability.requiredTags)
            {
                if (!caster.Model.statusHandler.HasTag(tag))
                {
                    reason = $"Requires {tag}";
                    return false;
                }
            }
        }

        if (caster.Model.statusHandler.HasTag("Taunted")
            && targets != null && targets.Length > 0 && targets[0] != null)
        {
            int tv = targets[0].GetComponent<PhotonView>()?.ViewID ?? -1;
            if (!caster.Model.statusHandler.IsTauntedTo(tv))
            {
                reason = "Taunted: must target the taunter";
                return false;
            }
        }

        // --- Targeting filters (only enforced if the fields exist on your UnitAbility) ---
        bool groundTarget = ability.groundTarget;   // if you haven't added these fields yet, add them to UnitAbility
        bool selfOnly = ability.selfOnly;
        bool alliesOnly = ability.alliesOnly;
        bool enemiesOnly = ability.enemiesOnly;

        // Ground-target: allow no unit target; otherwise we need a unit target
        if (!groundTarget)
        {
            if (targets == null || targets.Length == 0 || targets[0] == null)
            {
                reason = "No target";
                return false;
            }
        }

        // Self-only must target the caster
        if (selfOnly)
        {
            if (targets == null || targets.Length == 0 || targets[0] != caster)
            {
                reason = "Must target self";
                return false;
            }
        }

        // Allies/Enemies-only (when a unit target is provided)
        if (targets != null && targets.Length > 0 && targets[0] != null)
        {
            var t = targets[0];
            if (alliesOnly && t.Model.Faction != caster.Model.Faction)
            { reason = "Allies only"; return false; }
            if (enemiesOnly && t.Model.Faction == caster.Model.Faction)
            { reason = "Enemies only"; return false; }
        }

        // --- Tags on target ---
        if (targets != null && targets.Length > 0 && targets[0] != null && ability.requiredTargetTags != null)
        {
            foreach (var tag in ability.requiredTargetTags)
            {
                if (!targets[0].Model.statusHandler.HasTag(tag))
                {
                    reason = $"Target must have {tag}";
                    return false;
                }
            }
        }

        // --- Range ---
        if (!groundTarget && targets != null && targets.Length > 0 && targets[0] != null)
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
        Unit primaryTarget = (targets != null && targets.Length > 0) ? targets[0] : null;

        if (ability.areaType == AreaType.Single)
        {
            if (primaryTarget != null) computedTargets.Add(primaryTarget);
        }
        else if (ability.areaType == AreaType.Circle)
        {
            // Center on the first provided target (for now). Ground target can be added later.
            var center = (primaryTarget != null) ? primaryTarget.transform.position : casterCtrl.unit.transform.position;
            computedTargets = CombatCalculator.GetUnitsInRadius(center, ability.aoeRadius, casterCtrl.unit);
        }
        else if (ability.areaType == AreaType.Line)
        {
            if (primaryTarget != null)
            {
                computedTargets = CombatCalculator.GetLineTargets(
                    casterCtrl.unit, primaryTarget,
                    ability.range, // use generic range
                    Mathf.Max(1, ability.lineMaxTargets),
                    Mathf.Clamp01(ability.lineAlignmentTolerance)
                );
            }
        }

        // Faction filtering for AoE/Line (Single already validated in CanCast)
        if (computedTargets.Count > 0)
        {
            bool allowAllies = !ability.enemiesOnly;
            bool allowEnemies = !ability.alliesOnly;

            var filtered = new List<Unit>(computedTargets.Count);
            foreach (var u in computedTargets)
            {
                bool isAlly = (u.Model.Faction == casterCtrl.unit.Model.Faction);
                if ((isAlly && allowAllies) || (!isAlly && allowEnemies))
                    filtered.Add(u);
            }
            computedTargets = filtered;
        }

        // Prepare per-target results
        var outIds = new List<int>(computedTargets.Count);
        var outHits = new List<bool>(computedTargets.Count);
        var outDamages = new List<int>(computedTargets.Count);
        var outTypes = new List<DamageType>(computedTargets.Count); // true enum for RPC
        var outProcs = new List<byte>(computedTargets.Count);       // 0=None,1=Burn,2=Freeze,3=Shock

        int idx = 0;
        foreach (var tgt in computedTargets)
        {
            // Cover check
            bool hasMediumCover, hasHeavyCover;
            CombatCalculator.CheckCover(
                casterCtrl.unit.transform.position,
                tgt.transform.position,
                out hasMediumCover,
                out hasHeavyCover
            );

            // Melee ignores cover (single-target melee distance check)
            float dist = Vector3.Distance(casterCtrl.unit.transform.position, tgt.transform.position);
            bool meleeAttack = (ability.areaType == AreaType.Single) && (dist <= MeleeRangeMeters);
            if (meleeAttack) { hasMediumCover = false; hasHeavyCover = false; }

            // Flanking
            int flankCount = CombatCalculator.CountFlankingAllies(tgt, casterCtrl.unit);
            bool isFlanked = flankCount > 0;

            // Attacker affinity bonus
            int attackerAffinity = casterCtrl.unit.Model.Affinity;

            float hitchance = CombatCalculator.GetHitChance(
                ability.baseHitChance,
                attackerAffinity,
                flankCount,
                isFlanked,
                hasMediumCover,
                hasHeavyCover
            );

            bool hit = (Random.Range(0f, 100f) <= hitchance);
            int damage = 0;
            DamageType dtype = ability.damageSource; // default to ability’s configured source

            if (hit)
            {
                if (ability.isMixedDamage)
                {
                    int strength = casterCtrl.unit.Model.Strength;
                    int mpower = casterCtrl.unit.Model.MagicPower;
                    int armor = tgt.Model.Armor;
                    int mr = tgt.Model.MagicResistance;

                    damage = CombatCalculator.CalculateMixedDamage(
                        ability.baseDamage, strength, armor, mpower, mr, ability.mixedPhysicalPercent
                    );
                    dtype = DamageType.Mixed; // cosmetic; mitigation already applied in sum
                }
                else
                {
                    bool isPhysical = (ability.damageSource == DamageType.Physical);
                    int attackerStat = isPhysical ? casterCtrl.unit.Model.Strength : casterCtrl.unit.Model.MagicPower;
                    int defenderStat = isPhysical ? tgt.Model.Armor : tgt.Model.MagicResistance;

                    damage = Mathf.RoundToInt(
                        CombatCalculator.CalculateDamage(ability.baseDamage, attackerStat, defenderStat)
                    );

                    // Keep elemental type if declared
                    if (ability.damageSource == DamageType.Fire) dtype = DamageType.Fire;
                    else if (ability.damageSource == DamageType.Frost) dtype = DamageType.Frost;
                    else if (ability.damageSource == DamageType.Electric) dtype = DamageType.Electric;
                    else dtype = isPhysical ? DamageType.Physical : DamageType.Magical;
                }

                // Missing HP bonus (if configured)
                if (ability.bonusPerMissingHpPercent > 0)
                    damage = CombatCalculator.ApplyMissingHpBonus(damage, tgt, ability.bonusPerMissingHpPercent);

                // Line collateral for non-primary targets
                if (ability.areaType == AreaType.Line && idx > 0 && ability.lineCollateralPercent < 100)
                    damage = CombatCalculator.ApplyCollateralPercent(damage, ability.lineCollateralPercent);
            }

            // Elemental proc per spec: chance = 2.5 × final damage (clamped 0..100)
            byte proc = 0;
            if (hit && (dtype == DamageType.Fire || dtype == DamageType.Frost || dtype == DamageType.Electric))
            {
                float procChance = Mathf.Clamp(damage * 2.5f, 0f, 100f);
                if (Random.Range(0f, 100f) < procChance)
                    proc = (byte)(dtype == DamageType.Fire ? 1 : dtype == DamageType.Frost ? 2 : 3);
            }

            outIds.Add(tgt.GetComponent<PhotonView>()?.ViewID ?? -1);
            outHits.Add(hit);
            outDamages.Add(Mathf.Max(0, damage));
            outTypes.Add(dtype);
            outProcs.Add(proc);
            idx++;
        }

        // Broadcast batched result arrays — single authoritative resolve path
        _view.RPC(nameof(RPC_ResolveAbility_Area), RpcTarget.All,
            casterViewId, abilityIndex,
            outIds.ToArray(),
            outHits.ToArray(),
            outDamages.ToArray(),
            outTypes.ToArray(),
            outProcs.ToArray()
        );
    }

    [PunRPC]
    private void RPC_ResolveAbility_Area(int casterViewId, int abilityIndex,
                                         int[] targetViewIds, bool[] hits, int[] damages,
                                         DamageType[] damageTypes, byte[] elementalProcs)
    {
        var casterCtrl = FindByView<UnitController>(casterViewId);

        // Spend AP/resources/adrenaline deterministically
        if (casterCtrl != null)
        {
            var list = casterCtrl.unit.Model.Abilities;
            if (abilityIndex >= 0 && abilityIndex < list.Count)
            {
                var ab = list[abilityIndex];
                casterCtrl.unit.Model.SpendAction(ab.actionCost);

                if (ab.resourceCosts != null)
                {
                    foreach (var cost in ab.resourceCosts)
                        casterCtrl.unit.Model.TryConsume(cost.key, cost.amount);
                }

                if (ab.adrenalineCost > 0)
                    casterCtrl.unit.Model.SpendAdrenaline(ab.adrenalineCost);
            }
        }

        // Optional: Summons/Structures/Zones after spending (kept as before – summons example)
        if (casterCtrl != null)
        {
            var list = casterCtrl.unit.Model.Abilities;
            if (abilityIndex >= 0 && abilityIndex < list.Count)
            {
                var ab = list[abilityIndex];

                if (ab.spawnsSummons && SummonManager.Instance != null)
                {
                    Vector3 center = casterCtrl.unit.transform.position;
                    if (targetViewIds != null && targetViewIds.Length > 0)
                    {
                        var primary = FindByView<Unit>(targetViewIds[0]);
                        if (primary) center = primary.transform.position;
                    }
                    SummonManager.Instance.SpawnSummons(casterCtrl.unit, ab, center);
                }

                // (If you also have spawnsZone/Structure fields, trigger them here similarly)
            }
        }

        // Apply per-target results
        int count = (targetViewIds != null) ? targetViewIds.Length : 0;
        for (int i = 0; i < count; i++)
        {
            var target = FindByView<Unit>(targetViewIds[i]);
            if (target == null) continue;

            // Negative Zone protection: attacker outside & target inside ⇒ zero damage
            if (ZoneManager.Instance != null &&
                casterCtrl != null &&
                ZoneManager.Instance.IsTargetProtectedByNegativeZone(
                    casterCtrl.unit.transform.position,
                    target.transform.position))
            {
                // Keep 'hit' as-is; just nullify damage
                if (hits != null && i < hits.Length && hits[i] && damages != null && i < damages.Length)
                    damages[i] = 0;
            }

            // Damage application
            if (hits != null && i < hits.Length && hits[i] &&
                damages != null && i < damages.Length && damages[i] > 0 &&
                damageTypes != null && i < damageTypes.Length)
            {
                target.Model.ApplyDamageWithBarrier(damages[i], damageTypes[i]);
            }

            // Elemental proc → status
            if (elementalProcs != null && i < elementalProcs.Length)
            {
                switch (elementalProcs[i])
                {
                    case 1: target.Model.statusHandler?.ApplyEffect(EffectLibrary.Burning(1)); break;
                    case 2: target.Model.statusHandler?.ApplyEffect(EffectLibrary.Frozen(1)); break;
                    case 3: target.Model.statusHandler?.ApplyEffect(EffectLibrary.Shocked(1)); break;
                }
            }
        }

        // Apply attached effects from the ability — ONLY on hit
        if (casterCtrl != null && targetViewIds != null && targetViewIds.Length > 0)
        {
            var list = casterCtrl.unit.Model.Abilities;
            if (abilityIndex >= 0 && abilityIndex < list.Count)
            {
                var ab = list[abilityIndex];
                if (ab.appliedEffects != null && ab.appliedEffects.Count > 0)
                {
                    for (int i = 0; i < targetViewIds.Length; i++)
                    {
                        if (hits == null || i >= hits.Length || !hits[i]) continue;

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
