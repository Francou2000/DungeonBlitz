using System.Collections.Generic;
using System.Linq;
using DebugTools;
using Photon.Pun;
using UnityEngine;

public sealed class AbilityResolver : MonoBehaviourPun
{
    public static AbilityResolver Instance { get; private set; }
    PhotonView _view;

    const float MeleeRangeMeters = 1.5f; // 1 tile (tweak if the grid differs)

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _view = GetComponent<PhotonView>() ?? gameObject.AddComponent<PhotonView>();
        Debug.Log("[AbilityResolver] v2 RPC (aimPos/aimDir) LOADED on " + (PhotonNetwork.IsMasterClient ? "MASTER" : "CLIENT"));
    }

    public static bool CanCast(Unit caster, UnitAbility ability, Unit[] targets, out string reason)
    {
        reason = null;
        Debug.Log($"[CanCast] Checking caster={caster?.name} ability={ability?.abilityName} actions={caster?.Model?.CurrentActions}/{caster?.Model?.MaxActions}");
        
        if (caster == null || ability == null) { reason = "No caster/ability"; Debug.Log($"[Cast] FAIL: {reason}"); return false; }
        if (!caster.Model.CanAct()) { reason = "No actions left"; Debug.Log($"[Cast] FAIL: {reason}"); return false; }

        // --- Resource costs ---
        if (ability.resourceCosts != null)
        {
            foreach (var cost in ability.resourceCosts)
            {
                if (caster.Model.GetRes(cost.key) < cost.amount)
                {
                    reason = $"Need {cost.amount} {cost.key}";
                    Debug.Log($"[Cast] FAIL: {reason}");
                    return false;
                }
            }
        }

        // --- Adrenaline threshold ---
        if (ability.minAdrenaline > 0 && caster.Model.Adrenaline < ability.minAdrenaline)
        {
            reason = $"Need ≥ {ability.minAdrenaline} adrenaline";
            Debug.Log($"[Cast] FAIL: {reason}");
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
                        Debug.Log($"[Cast] FAIL: {reason}");
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
                    Debug.Log($"[Cast] FAIL: {reason}");
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
                Debug.Log($"[Cast] FAIL: {reason}");
                return false;
            }
        }

        // --- Targeting filters (only enforced if the fields exist on your UnitAbility) ---

        // Handle different targeting types
        bool groundTarget = ability.groundTarget;
        bool selfOnly = ability.selfOnly;
        bool alliesOnly = ability.alliesOnly;
        bool enemiesOnly = ability.enemiesOnly;

        // 1) Self-only: ok without an explicit target (we'll auto-target caster later)
        if (selfOnly)
        {
            Debug.Log("[CanCast] Self-only ability; explicit unit not required.");
        }
        // 2) Allies-only: if this is a *single-target* ally spell, we do require a unit
        else if (alliesOnly && ability.areaType == AreaType.Single)
        {
            if (targets == null || targets.Length == 0 || targets[0] == null)
            {
                reason = "No ally target selected";
                Debug.Log($"[Cast] FAIL: {reason}");
                return false;
            }
        }
        // 3) Ground/AoE/Line: can be cast without an explicit unit (position/aim will be used)
        else if (groundTarget || ability.areaType == AreaType.Circle || ability.areaType == AreaType.Line)
        {
            Debug.Log("[CanCast] Area/ground ability; explicit unit not required.");
        }
        // 4) Everything else (true single-target): must have a unit or we may auto-target
        else
        {
            if (targets == null || targets.Length == 0 || targets[0] == null)
            {
                if (ability.areaType == AreaType.Single && ability.range > 0)
                {
                    Debug.Log("[CanCast] Single-target without target -> will auto-target nearest enemy.");
                    // Auto-target happens later during resolution
                }
                else
                {
                    reason = "No target";
                    Debug.Log($"[Cast] FAIL: {reason}");
                    return false;
                }
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
                    Debug.Log($"[Cast] FAIL: {reason}");
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
                Debug.Log($"[Cast] FAIL: {reason}");
                return false;
            }
        }

        return true;
    }

    public void RequestCast(UnitController casterCtrl, UnitAbility ability, Unit[] targets, Vector3 aimPos, Vector3 aimDir, string traceId)
    {
        // Log detailed target information
        string targetInfo = "";
        Debug.Log($"[RequestCast] SEND aimPos={aimPos} aimDir={aimDir}");

        if (targets != null && targets.Length > 0)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    targetInfo += $"Target{i}: {targets[i].name} (HP:{targets[i].Model.CurrentHP}/{targets[i].Model.MaxHP}, Faction:{targets[i].Model.Faction})";
                    if (i < targets.Length - 1) targetInfo += ", ";
                }
                else
                {
                    targetInfo += $"Target{i}: NULL";
                    if (i < targets.Length - 1) targetInfo += ", ";
                }
            }
        }
        else
        {
            targetInfo = "No targets";
        }
        
        Debug.Log($"[RequestCast] ENTER caster={casterCtrl?.name} ability={ability?.abilityName} targets={targets?.Length} traceId={traceId}");
        Debug.Log($"[RequestCast] TARGET INFO: {targetInfo}");
        
        if (casterCtrl == null || ability == null) 
        {
            Debug.LogError($"[RequestCast] FAIL: casterCtrl={casterCtrl} ability={ability}");
            return;
        }

        int abilityIndex = casterCtrl.unit.Model.Abilities.IndexOf(ability);
        if (abilityIndex < 0) 
        {
            Debug.LogError($"[RequestCast] FAIL: ability not found in caster's abilities list. ability={ability?.abilityName}");
            return;
        }

        Debug.Log($"[RequestCast] Sending RPC to MasterClient. casterViewId={casterCtrl.photonView.ViewID} abilityIndex={abilityIndex}");
        Debug.Log($"[RequestCast] IsMasterClient={PhotonNetwork.IsMasterClient} LocalPlayer={PhotonNetwork.LocalPlayer.ActorNumber}");
        
        int[] targetViewIds = PackTargets(targets);
        _view.RPC(nameof(RPC_RequestCast), RpcTarget.MasterClient,
            casterCtrl.photonView.ViewID, abilityIndex, targetViewIds, aimPos, aimDir);
    }

    [PunRPC]
    private void RPC_RequestCast(int casterViewId, int abilityIndex, int[] targetViewIds, Vector3 aimPos, Vector3 aimDir, PhotonMessageInfo info)
    {
        Debug.Log($"[RPC_RequestCast] ENTER casterViewId={casterViewId} abilityIndex={abilityIndex} isMaster={PhotonNetwork.IsMasterClient} sender={info.Sender.ActorNumber}");
        Debug.Log($"[RPC_RequestCast] RECV aimPos={aimPos} aimDir={aimDir}");
        if (!PhotonNetwork.IsMasterClient) 
        {
            Debug.Log($"[RPC_RequestCast] Not master client, ignoring");
            return;
        }

        var casterCtrl = FindByView<UnitController>(casterViewId);
        if (casterCtrl == null) 
        {
            Debug.LogError($"[RPC_RequestCast] FAIL: casterCtrl not found for viewId={casterViewId}");
            return;
        }

        Debug.Log($"[RPC_RequestCast] Found caster: {casterCtrl.name}");

        // Ability lookup from caster's list
        var list = casterCtrl.unit.Model.Abilities;
        if (abilityIndex < 0 || abilityIndex >= list.Count) 
        {
            Debug.LogError($"[RPC_RequestCast] FAIL: abilityIndex={abilityIndex} out of range (0-{list.Count-1})");
            return;
        }
        var ability = list[abilityIndex];
        
        Debug.Log($"[RPC_RequestCast] Found ability: {ability.abilityName}");

        var targets = UnpackTargets<Unit>(targetViewIds);
        Debug.Log($"[RPC_RequestCast] Unpacked targets: {targets?.Length}");
        
        if (!CanCast(casterCtrl.unit, ability, targets, out string reason)) 
        {
            Debug.LogError($"[RPC_RequestCast] FAIL: CanCast failed - {reason}");
            return;
        }

        Debug.Log($"[RPC_RequestCast] CanCast passed, proceeding with ability resolution");

        // Auto-targeting based on ability type
        if (targets == null || targets.Length == 0 || targets[0] == null)
        {
            if (ability.selfOnly)
            {
                // Self-only abilities: target the caster
                Debug.Log($"[RPC_RequestCast] Self-only ability, targeting caster");
                targets = new Unit[] { casterCtrl.unit };
            }
            else if (ability.areaType == AreaType.Single && ability.range > 0)
            {
                // Single-target abilities: auto-target nearest enemy
                Debug.Log($"[RPC_RequestCast] Auto-targeting nearest enemy for {ability.abilityName}");
                targets = FindNearestEnemyTargets(casterCtrl.unit, ability.range, 1);
                if (targets != null && targets.Length > 0)
                {
                    Debug.Log($"[RPC_RequestCast] Auto-targeted: {targets[0].name}");
                }
                else
                {
                    Debug.LogError($"[RPC_RequestCast] No enemies found in range for auto-targeting");
                    return;
                }
            }
        }

        var traceId = CombatLog.NewTraceId();
        CombatLog.Cast(traceId, $"Validate caster={casterCtrl?.name}#{casterViewId} ability={ability?.name} targets={targets?.Length}");

        // Build target list according to area type
        var computedTargets = new List<Unit>();
        Unit primaryTarget = (targets != null && targets.Length > 0) ? targets[0] : null;

        // Clamp Z for 2D
        aimPos.z = 0f;

        // SINGLE
        if (ability.areaType == AreaType.Single)
        {
            if (primaryTarget != null) computedTargets.Add(primaryTarget);
        }

        // CIRCLE (AoE) — use clicked aimPos if no primary was sent
        else if (ability.areaType == AreaType.Circle)
        {
            var center = (primaryTarget != null) ? primaryTarget.transform.position : aimPos;
            var cols = Physics2D.OverlapCircleAll(center, ability.aoeRadius);
            computedTargets = CombatCalculator.GetUnitsInRadius(center, ability.aoeRadius, casterCtrl.unit);
            Debug.Log($"[AoE] rawColliders={cols.Length} center={center} r={ability.aoeRadius}");
        }

        // LINE — if no primary, derive one near the click so your existing helper can work
        else if (ability.areaType == AreaType.Line)
        {
            Vector3 origin = casterCtrl.transform.position;

            // Pick a direction: primary target first, else aimDir, else aimPos
            Vector3 dir;
            if (primaryTarget != null)
                dir = (primaryTarget.transform.position - origin);
            else if (aimDir.sqrMagnitude > 1e-5f)
                dir = aimDir;
            else
                dir = (aimPos - origin);

            // Choose half-width: prefer ability.lineWidth if you added it, otherwise reuse aoeRadius
            float halfWidth =
                (ability.lineAlignmentTolerance > 0f ? ability.lineAlignmentTolerance * 0.5f :
                 ability.aoeRadius > 0f ? ability.aoeRadius :
                 0.5f); // sensible default

            computedTargets = CombatCalculator.GetLineTargetsByWidth(
                casterCtrl.unit,
                origin,
                dir,
                ability.range,
                halfWidth,
                Mathf.Max(1, ability.lineMaxTargets)
            );
        }

        CombatLog.Resolve(traceId, $"Targets: final={computedTargets.Count} area={ability.areaType}");

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
        var outTypes = new List<int>(computedTargets.Count); // true enum for RPC
        var outProcs = new List<byte>(computedTargets.Count);       // 0=None,1=Burn,2=Freeze,3=Shock

        int idx = 0;
        foreach (var tgt in computedTargets)
        {
            // --- Skip damage branch if this ability doesn't deal any ---
            bool doDamage = DealsAnyDamage(ability);
            if (!doDamage)
            {
                Debug.Log($"[Resolve] {ability.abilityName} has no damage component; skipping damage phase.");
                break;   // leave the loop early since nothing to compute
            }

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
            outTypes.Add((int)dtype);
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
                                         int[] damageTypes, byte[] elementalProcs)
    {
        Debug.Log($"[RPC_ResolveArea] ENTER isMaster={PhotonNetwork.IsMasterClient} casterViewId={casterViewId} abilityIndex={abilityIndex} targets={(targetViewIds != null ? targetViewIds.Length : 0)}");

        var casterCtrl = FindByView<UnitController>(casterViewId);

        var traceId = CombatLog.NewTraceId();

        // --- JUICE: caster punch toward primary target (runs on ALL clients) ---
        if (casterCtrl != null)
        {
            // infer the primary target (first in list), if any
            Unit primary = null;
            if (targetViewIds != null && targetViewIds.Length > 0)
                primary = FindByView<Unit>(targetViewIds[0]);

            Vector2 face = Vector2.right;
            if (primary != null)
                face = (primary.transform.position - casterCtrl.unit.transform.position).normalized;

            // broadcast punch
            casterCtrl.unit.View.PlayAttackNet(face);
        }


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

        // Summons/Structures/Zones after spending (kept as before – summons example)
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
            }
        }

        // Apply per-target results
        int count = (targetViewIds != null) ? targetViewIds.Length : 0;
        for (int i = 0; i < count; i++)
        {
            var target = FindByView<Unit>(targetViewIds[i]);
            if (target == null) continue;

            // --- JUICE: target feedback on hit/miss (runs on ALL clients) ---
            bool didHit = (hits != null && i < hits.Length && hits[i]);
            Vector2 fromAttacker = Vector2.zero;
            if (casterCtrl != null)
                fromAttacker = (target.transform.position - casterCtrl.unit.transform.position).normalized;

            if (didHit)
                target.View.PlayHitNet(fromAttacker);
            else
                target.View.PlayMissNet(fromAttacker);

            // Negative Zone protection: attacker outside & target inside ⇒ zero damage
            if (ZoneManager.Instance != null &&
                casterCtrl != null &&
                ZoneManager.Instance.IsTargetProtectedByNegativeZone(
                    casterCtrl.unit.transform.position,
                    target.transform.position))
            {
                // Keep 'hit' as-is; just nullify damage
                if (hits != null && i < hits.Length && hits[i] && damages != null && i < damages.Length)
                {
                    CombatFeedbackUI.ShowMiss(target.GetComponent<Unit>());
                    // Play evade sound when protected by negative zone
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayEvadeSoundByUnitType(target.GetComponent<Unit>());
                    damages[i] = 0;
                }
            }

            Debug.Log($"[RPC_ResolveArea] pre-damage target={target?.name} hit={(hits != null && i < hits.Length && hits[i])} " +
          $"dmg={(damages != null && i < damages.Length ? damages[i] : -1)} " +
          $"dtype={(damageTypes != null && i < damageTypes.Length ? ((DamageType)damageTypes[i]).ToString() : "n/a")}");

            // Damage application (MASTER applies, then mirror to others)
            if (hits != null && i < hits.Length && hits[i] &&
                damages != null && i < damages.Length && damages[i] > 0 &&
                damageTypes != null && i < damageTypes.Length)
            {
                int dmg = damages[i];
                int dtype = damageTypes[i];
                int targetId = target.Controller.photonView.ViewID;

                if (PhotonNetwork.IsMasterClient)
                {
                    // Apply on master
                    var dealt = target.Model.ApplyDamageWithBarrier(dmg, (DamageType)dtype);
                    Debug.Log($"[Resolve] MASTER applied {dealt} HP dmg to {target.name} (type={(DamageType)dtype})");

                    // Local popup on master
                    var u = target.GetComponent<Unit>();
                    if (u)
                    {
                        if (dealt > 0) 
                        {
                            // Obtener la habilidad utilizada para el efecto de daño
                            var ability = casterCtrl.unit.Model.Abilities[abilityIndex];
                            CombatFeedbackUI.ShowHit(u, dealt, (DamageType)dtype, false, casterCtrl.unit, ability);
                            // Play attack sound when hit connects
                            if (AudioManager.Instance != null)
                            {
                                //AudioManager.Instance.PlayStabSound(); 
                                var list = casterCtrl.unit.Model.Abilities;
                                AudioManager.Instance.PlayAttackSound(list[abilityIndex].abilityName);
                            }
                        }
                        else 
                        {
                            CombatFeedbackUI.ShowMiss(u);
                            // Play evade sound when attack misses
                            if (AudioManager.Instance != null)
                                AudioManager.Instance.PlayEvadeSoundByUnitType(u);
                        }
                    }

                    // Mirror to other clients (they'll apply and show popups in RPC_ApplyDamageToClient)
                    _view.RPC(nameof(RPC_ApplyDamageToClient), RpcTarget.Others, targetId, dmg, dtype, casterCtrl.photonView.ViewID, abilityIndex);
                }
                else
                {
                    // Non-master does not apply damage locally; it waits for RPC_ApplyDamageToClient
                    Debug.Log($"[Resolve] Non-master computed damage locally (ignored). Waiting for MASTER RPC. target={target.name} dmg={dmg}");
                }
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

        // Handle completely missed attacks (no hit at all)
        if (hits != null && targetViewIds != null)
        {
            for (int i = 0; i < targetViewIds.Length; i++)
            {
                if (i < hits.Length && !hits[i]) // Attack completely missed
                {
                    var target = FindByView<Unit>(targetViewIds[i]);
                    if (target != null)
                    {
                        CombatFeedbackUI.ShowMiss(target.GetComponent<Unit>());
                        // Play evade sound when attack completely misses
                        if (AudioManager.Instance != null)
                            AudioManager.Instance.PlayEvadeSoundByUnitType(target.GetComponent<Unit>());
                    }
                }
            }
        }
    }

    [PunRPC]
    void RPC_ApplyDamageToClient(int targetViewId, int damage, int damageType, int casterViewId, int abilityIndex)
    {
        var pv = PhotonView.Find(targetViewId);
        if (pv == null) return;

        var target = pv.GetComponent<Unit>();
        if (target == null || target.Model == null) return;

        var dealt = target.Model.ApplyDamageWithBarrier(damage, (DamageType)damageType);
        Debug.Log($"[AbilityRPC] Applied {dealt} HP damage to {target.name} (type={(DamageType)damageType})");

        // --- Popup on every client ---
        var u = target.GetComponent<Unit>();
        if (u)
        {
            if (dealt > 0) 
            {
                // Obtener el atacante y la habilidad para el efecto de daño
                var casterCtrl = FindByView<UnitController>(casterViewId);
                UnitAbility ability = null;
                if (casterCtrl != null && abilityIndex >= 0 && abilityIndex < casterCtrl.unit.Model.Abilities.Count)
                {
                    ability = casterCtrl.unit.Model.Abilities[abilityIndex];
                }
                
                CombatFeedbackUI.ShowHit(u, dealt, (DamageType)damageType, false, casterCtrl?.unit, ability);
                // Play attack sound when hit connects
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayStabSound();
                    // var list = casterCtrl.unit.Model.Abilities;
                    // AudioManager.Instance.PlayAttackSound(list[abilityIndex].abilityName);
                }
            }
            else 
            {
                CombatFeedbackUI.ShowMiss(u);
                // Play evade sound when attack misses
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayEvadeSoundByUnitType(u);
            }
        }
    }

    [PunRPC]
    void RPC_ShowMissPopup(int targetViewId)
    {
        var pv = PhotonView.Find(targetViewId);
        if (pv == null) return;
        var u = pv.GetComponent<Unit>();
        if (u) CombatFeedbackUI.ShowMiss(u);
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

    // Helper function to find nearest enemy targets for auto-targeting
    private Unit[] FindNearestEnemyTargets(Unit caster, int range, int maxTargets)
    {
        var allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        var enemies = new List<Unit>();
        
        foreach (var unit in allUnits)
        {
            if (unit != caster && unit.Model.Faction != caster.Model.Faction && unit.Model.IsAlive())
            {
                float distance = Vector3.Distance(caster.transform.position, unit.transform.position);
                if (distance <= range)
                {
                    enemies.Add(unit);
                }
            }
        }
        
        // Sort by distance and return up to maxTargets
        enemies.Sort((a, b) => 
        {
            float distA = Vector3.Distance(caster.transform.position, a.transform.position);
            float distB = Vector3.Distance(caster.transform.position, b.transform.position);
            return distA.CompareTo(distB);
        });
        
        return enemies.Take(maxTargets).ToArray();
    }

    private Unit FindNearestEnemyToPoint(Unit caster, Vector3 point, float maxRange)
    {
        Unit best = null;
        float bestDist = float.MaxValue;

        foreach (var u in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            if (u == null || u == caster) continue;
            if (u.Model.Faction == caster.Model.Faction) continue; // enemies only
            float d = Vector3.Distance(point, u.transform.position);
            if (d <= maxRange + 0.01f && d < bestDist)
            {
                bestDist = d;
                best = u;
            }
        }
        return best;
    }

    // -------------------- DAMAGE GUARD --------------------
    private static bool DealsAnyDamage(UnitAbility ab)
    {
        if (ab == null) return false;

        // Treat as "no damage" unless one of these knobs is explicitly non-zero or active
        return ab.baseDamage > 0
             || ab.bonusDamage > 0
             || ab.damageMultiplier > 0f
             || ab.isMixedDamage
             || ab.bonusPerMissingHpPercent > 0
             || ab.useTargetMissingHealth
             || ab.healsTarget;
    }
}
