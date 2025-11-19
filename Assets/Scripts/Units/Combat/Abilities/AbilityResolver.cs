using DebugTools;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
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
        if (ability.actionCost > 0 && caster.Model.CurrentActions < ability.actionCost)
        {
            reason = "Not enough action points";
            Debug.Log($"[CanCast] FAIL: not enough AP: has={caster.Model.CurrentActions}, needs={ability.actionCost}");
            return false;
        }


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
        if (ability.requiredStates != null && ability.requiredStates.Count > 0)
        {
            var model = caster.GetComponent<UnitModel>();
            foreach (var req in ability.requiredStates)
            {
                if (string.IsNullOrWhiteSpace(req)) continue;

                // Expect "Key:Value" (e.g., "Form:Fire", "Weapon:Bow")
                var parts = req.Split(':');
                if (parts.Length != 2) { Debug.LogWarning($"[CanCast] Bad requiredStates entry '{req}' in {ability.abilityName}"); return false; }

                string key = parts[0].Trim();
                string val = parts[1].Trim();

                string cur = model.GetState(key);  // uses UnitModel states API
                if (!string.Equals(cur, val, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[CanCast] Fails state gate for {ability.abilityName}: need {key}={val}, has {key}={cur}");
                    return false; // gate fails
                }
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
                if (!HasTag(targets[0], tag))
                {
                    reason = $"Target must have {tag}";
                    Debug.Log($"[Cast] FAIL: {reason}");
                    return false;
                }
            }
        }

        // --- Tags on caster ---
        if (ability.requiredTags != null)
        {
            foreach (var tag in ability.requiredTags)
            {
                if (!HasTag(caster, tag))
                {
                    reason = $"Requires {tag}";
                    Debug.Log($"[Cast] FAIL: {reason}");
                    return false;
                }
            }
        }

        // --- Taunt gate --- 
        if (HasTag(caster, "Taunted") && targets != null && targets.Length > 0 && targets[0] != null)
        {
            int tv = targets[0].GetComponent<PhotonView>()?.ViewID ?? -1;
            var scCaster = caster.GetComponent<StatusComponent>();
            if (scCaster != null && !scCaster.IsTauntedTo(tv))
            {
                reason = "Taunted: must target the taunter";
                Debug.Log($"[Cast] FAIL: {reason}");
                return false;
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

    private void ResolveHealingAndShields(UnitController casterCtrl, UnitAbility ability,
                                          List<Unit> computedTargets, int abilityIndex)
    {
        var outIds = new List<int>(computedTargets.Count);
        var outHeals = new List<int>(computedTargets.Count);
        var outBarriers = new List<int>(computedTargets.Count);

        foreach (var t in computedTargets)
        {
            if (t == null || t.Model == null) continue;

            int heal = 0;
            if (ability.healsTarget)
            {
                // use the target-aware calculator (flat + % of missing, clamped)
                heal = ability.ComputeHealAmount(casterCtrl.unit.Model, t.Model);
            }

            int barrier = ability.grantsBarrier ? Mathf.Max(0, ability.barrierAmount) : 0;

            if (heal > 0 || barrier > 0)
            {
                outIds.Add(t.GetComponent<PhotonView>()?.ViewID ?? -1);
                outHeals.Add(heal);
                outBarriers.Add(barrier);
                Debug.Log($"[Resolve-Heal/Shield] pending -> {t.name}: heal={heal}, barrier={barrier}");
            }
        }

        // Spend AP/resources/adrenaline on MASTER (authoritative)
        var ab = casterCtrl.unit.Model.Abilities[abilityIndex];
        casterCtrl.unit.Model.SpendAction(ab.actionCost);
        if (ab.resourceCosts != null)
            foreach (var cost in ab.resourceCosts)
                casterCtrl.unit.Model.TryConsume(cost.key, cost.amount);
        if (ab.adrenalineCost > 0)
            casterCtrl.unit.Model.SpendAdrenaline(ab.adrenalineCost);

        BroadcastAPSync(casterCtrl);


        // Broadcast to ALL clients so each applies exactly once
        _view.RPC(nameof(RPC_ApplyHealingToClient), RpcTarget.All,
            casterCtrl.photonView.ViewID, abilityIndex,
            outIds.ToArray(), outHeals.ToArray(), outBarriers.ToArray());

        Debug.Log($"[Resolve-Heal/Shield] Broadcast {outIds.Count} targets for {ability.abilityName}.");
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
            Debug.LogError($"[RPC_RequestCast] FAIL: abilityIndex={abilityIndex} out of range (0-{list.Count - 1})");
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

        // ---- BLINK: instant teleport inside a circle centered on the caster ----
        if (ability.grantsMovement && ability.isTeleport)
        {
            Debug.Log($"[Blink] ENTER {ability.abilityName} aimPos={aimPos}");

            // Compute Blink radius = half of your move distance per action.
            // Prefer UnitMovement helper; fall back to Model speed * time.
            var mv = casterCtrl.GetComponent<UnitMovement>();
            float fullMove = (mv != null)
                ? mv.GetMaxWorldRadius()
                : casterCtrl.unit.Model.GetMovementSpeed() * casterCtrl.unit.Model.MoveTimeBase;  // fallback

            float blinkRadius = Mathf.Max(0.1f, fullMove * 0.5f);

            // Clamp aimPos to that circle (center is caster)
            Vector3 center = casterCtrl.transform.position;
            Vector3 dest = aimPos; dest.z = center.z;
            Vector3 d = dest - center;
            if (d.sqrMagnitude > blinkRadius * blinkRadius)
                dest = center + d.normalized * blinkRadius;

            Debug.Log($"[Blink] fullMove={fullMove} radius={blinkRadius} dest={dest}");

            // Spend costs ONCE on master
            casterCtrl.unit.Model.SpendAction(ability.actionCost);
            if (ability.resourceCosts != null)
                foreach (var cost in ability.resourceCosts)
                    casterCtrl.unit.Model.TryConsume(cost.key, cost.amount);
            if (ability.adrenalineCost > 0)
                casterCtrl.unit.Model.SpendAdrenaline(ability.adrenalineCost);

            BroadcastAPSync(casterCtrl);

            // Teleport on ALL clients
            _view.RPC(nameof(RPC_TeleportUnit), RpcTarget.All,
                casterCtrl.photonView.ViewID, dest);

            ResolveBlinkAoE(casterCtrl, ability, dest, abilityIndex);
            Debug.Log("[Blink] Teleport RPC sent + AoE resolved (if any)");

            return; // Blink does not do damage/heal itself
        }


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

        // --- collect structures (optional per ability) ---
        var structTargets = new List<StructureBase>();
        if (ability.allowTargetStructures && StructureManager.Instance)
        {
            var casterFaction = casterCtrl.unit.Model.Faction;

            if (ability.areaType == AreaType.Single)
            {
                // try picking the nearest structure around the aim position if no unit primary
                if (primaryTarget == null)
                {
                    var s = StructureManager.Instance.RaycastStructureSingle(
                        aimPos, 4f, ability.structureTargets, casterFaction);
                    if (s != null) structTargets.Add(s);
                }
            }
            else if (ability.areaType == AreaType.Circle)
            {
                var center = (primaryTarget != null) ? primaryTarget.transform.position : aimPos;
                center.z = 0f;
                structTargets.AddRange(
                    StructureManager.Instance.GetStructuresInCircle(
                        center, ability.aoeRadius, null, ability.structureTargets, casterFaction));
            }
            else if (ability.areaType == AreaType.Line)
            {
                var origin = casterCtrl.transform.position;
                Vector3 dir =
                    (primaryTarget != null) ? (primaryTarget.transform.position - origin) :
                    (aimDir.sqrMagnitude > 1e-5f) ? aimDir :
                    (aimPos - origin);

                var a = origin;
                var b = origin + dir.normalized * ability.lineRange;
                float halfWidth = (ability.aoeRadius > 0f ? ability.aoeRadius : 0.5f);

                structTargets.AddRange(
                    StructureManager.Instance.GetStructuresNearSegment(
                        a, b, halfWidth, ability.structureTargets, casterFaction));
            }
        }

        CombatLog.Resolve(traceId, $"Targets: final={computedTargets.Count} area={ability.areaType}");

        // Mostrar el nombre de la habilidad sobre el caster
        if (casterCtrl != null && casterCtrl.unit != null)
        {
            CombatFeedbackUI.ShowAbilityName(casterCtrl.unit, ability.abilityName);
        }

        // -------- PER-TARGET ROUTING: heal allies, damage enemies --------

        // --- If this ability does not deal damage or healing, just log its use once (buffs, stances, etc.) ---
        bool hasAnyDamage = DealsAnyDamage(ability);
        bool hasAnyHealOrBar = ability.healsTarget || ability.grantsBarrier;
        bool spawnsZone = ability.spawnsZone;
        bool spawnsStructure = IsStructureAbility(ability, out _);
        bool changesState = ability.changesState;

        if (PhotonNetwork.IsMasterClient &&
            !hasAnyDamage && !hasAnyHealOrBar &&
            !spawnsZone && !spawnsStructure &&
            !changesState)
        {
            string casterName = GetCasterDisplayName(casterCtrl);
            string abilityName = ability.abilityName;

            string msg;
            if (computedTargets.Count > 0 && computedTargets[0] != null)
            {
                var t = computedTargets[0];
                string targetName =
                    (t.Model != null && !string.IsNullOrEmpty(t.Model.UnitName))
                    ? t.Model.UnitName
                    : t.name;

                msg = $"{casterName} used {abilityName} on {targetName}.";
            }
            else
            {
                msg = $"{casterName} used {abilityName}.";
            }

            _view.RPC(nameof(RPC_CombatLogMessage), RpcTarget.All, msg);
        }

        // Batches to send
        var healIds = new List<int>();
        var healVals = new List<int>();
        var barriVals = new List<int>();

        var dmgIds = new List<int>();
        var hitsList = new List<bool>();
        var dmgList = new List<int>();
        var dtypeList = new List<int>();
        var procsList = new List<byte>();

        bool canDealDamage = DealsAnyDamage(ability);
        bool canHeal = ability.healsTarget;

        int idx = 0;
        foreach (var tgt in computedTargets)
        {
            if (tgt == null || tgt.Model == null) { idx++; continue; }

            bool isAlly = (tgt.Model.Faction == casterCtrl.unit.Model.Faction);

            // Heal allies if this ability can heal
            if (isAlly && canHeal)
            {
                // LOG ability flags + target relation
                Debug.Log($"[HealBranch] ability={ability.abilityName} healsTarget={ability.healsTarget} " +
                          $"ally={isAlly} healAmtField={ability.healAmount} healPctField={ability.healPercentage} " +
                          $"grantsBarrier={ability.grantsBarrier} barrierAmt={ability.barrierAmount}");

                // Compute heal using TARGET'S missing HP
                int missing = Mathf.Max(0, tgt.Model.MaxHP - tgt.Model.CurrentHP);
                int heal = ability.ComputeHealAmount(casterCtrl.unit.Model, tgt.Model);
                int barrier = ability.grantsBarrier ? Mathf.Max(0, ability.barrierAmount) : 0;

                Debug.Log($"[HealMath] target={tgt.name} HP={tgt.Model.CurrentHP}/{tgt.Model.MaxHP} " +
                          $"missing={missing} -> heal={heal} barrier={barrier}");

                if (heal > 0 || barrier > 0)
                {
                    // Use the Unit's PhotonView (not the controller PV)
                    int unitPv = tgt.GetComponent<PhotonView>()?.ViewID ?? -1;
                    healIds.Add(unitPv);
                    healVals.Add(heal);
                    barriVals.Add(barrier);
                    Debug.Log($"[Resolve] enqueue heal -> {tgt.name}: pv={unitPv} heal={heal} barrier={barrier}");
                }
                else
                {
                    Debug.LogWarning($"[HealBranch] SKIP: heal==0 && barrier==0 (ability probably has no heal values set)");
                }

                idx++;
                continue; // do not attempt to damage allies
            }

            // Damage enemies if the ability has damage
            if (!isAlly && canDealDamage)
            {
                // ---- Your current damage pipeline (cover, hit chance, damage calc, procs) ----
                bool hasMediumCover, hasHeavyCover;
                CombatCalculator.CheckCover(
                    casterCtrl.unit.transform.position,
                    tgt.transform.position,
                    out hasMediumCover,
                    out hasHeavyCover
                );

                float dist = Vector3.Distance(casterCtrl.unit.transform.position, tgt.transform.position);
                bool meleeAttack = (ability.areaType == AreaType.Single) && (dist <= MeleeRangeMeters);
                if (meleeAttack) { hasMediumCover = false; hasHeavyCover = false; }

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

                bool hit = (UnityEngine.Random.Range(0f, 100f) <= hitchance);
                int damage = 0;
                DamageType dtype = ability.damageSource;

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
                        dtype = DamageType.Mixed;
                    }
                    else
                    {
                        bool isPhysical = (ability.damageSource == DamageType.Physical);
                        int attackerStat = isPhysical ? casterCtrl.unit.Model.Strength : casterCtrl.unit.Model.MagicPower;
                        int defenderStat = isPhysical ? tgt.Model.Armor : tgt.Model.MagicResistance;

                        damage = Mathf.RoundToInt(
                            CombatCalculator.CalculateDamage(ability.baseDamage, attackerStat, defenderStat)
                        );

                        if (ability.damageSource == DamageType.Fire) dtype = DamageType.Fire;
                        else if (ability.damageSource == DamageType.Frost) dtype = DamageType.Frost;
                        else if (ability.damageSource == DamageType.Electric) dtype = DamageType.Electric;
                        else dtype = isPhysical ? DamageType.Physical : DamageType.Magical;
                    }

                    if (ability.bonusPerMissingHpPercent > 0)
                        damage = CombatCalculator.ApplyMissingHpBonus(damage, tgt, ability.bonusPerMissingHpPercent);

                    if (ability.areaType == AreaType.Line && idx > 0 && ability.lineCollateralPercent < 100)
                        damage = CombatCalculator.ApplyCollateralPercent(damage, ability.lineCollateralPercent);
                }

                // Elemental proc chance based on final damage
                byte proc = 0;
                if (hit && (dtype == DamageType.Fire || dtype == DamageType.Frost || dtype == DamageType.Electric))
                {
                    float procChance = Mathf.Clamp(damage * 2.5f, 0f, 100f);
                    if (UnityEngine.Random.Range(0f, 100f) < procChance)
                        proc = (byte)(dtype == DamageType.Fire ? 1 : dtype == DamageType.Frost ? 2 : 3);
                }

                dmgIds.Add(tgt.GetComponent<PhotonView>()?.ViewID ?? -1);
                hitsList.Add(hit);
                dmgList.Add(Mathf.Max(0, damage));
                dtypeList.Add((int)dtype);
                procsList.Add(proc);
            }

            idx++;
        }

        // Spend costs ONCE (server-authoritative)
        var abilities = casterCtrl.unit.Model.Abilities;
        var ab = abilities[abilityIndex];
        casterCtrl.unit.Model.SpendAction(ab.actionCost);
        if (ab.resourceCosts != null)
            foreach (var cost in ab.resourceCosts)
                casterCtrl.unit.Model.TryConsume(cost.key, cost.amount);
        if (ab.adrenalineCost > 0)
            casterCtrl.unit.Model.SpendAdrenaline(ab.adrenalineCost);
        foreach (var s in structTargets)
            HitStructureAuth(ability, casterCtrl, s);

        if (PhotonNetwork.IsMasterClient && ab != null && ab.changesState && !string.IsNullOrEmpty(ab.stateKey))
        {
            var model = casterCtrl.unit.Model;
            model.SetState(ab.stateKey, ab.stateValue);  // fires OnStateChanged locally (HUD refresh)

            // Mirror to all other clients so their models match and HUD updates
            _view.RPC(nameof(RPC_SetUnitState), RpcTarget.Others,
                casterCtrl.photonView.ViewID, ab.stateKey, ab.stateValue);

            Debug.Log($"[Resolver] Stance set on cast: {ab.stateKey} = {ab.stateValue}");

            // --- Combat log for forms / stances ---
            string casterName = GetCasterDisplayName(casterCtrl);
            string abilityName = ab.abilityName;
            string msg = $"{casterName} used {abilityName}: {ab.stateKey} = {ab.stateValue}.";

            _view.RPC(nameof(RPC_CombatLogMessage), RpcTarget.All, msg);
        }

        BroadcastAPSync(casterCtrl);

        if (IsStructureAbility(ab, out var skind))
        {
            // Decide world center: for ground AoE use aimPos; if you prefer, use primary target pos when present
            Vector3 center = aimPos;
            if (ab.areaType == AreaType.Circle && (primaryTarget != null))
                center = primaryTarget.transform.position;
            center.z = 0f;

            var spec = BuildSpec(casterCtrl.unit, ab, skind);

            // Preferred: Master calls manager (it RPCs to all)
            SpawnStructure_Master(spec, center);

            Debug.Log($"[Structure] {skind} spawned by {casterCtrl.name} at {center}");
            // Structures don’t need the heal/damage pipeline; skip sending those RPCs if they’re empty.
            return;
        }

        if (PhotonNetwork.IsMasterClient && ZoneManager.Instance && ability.spawnsZone)
        {
            string casterName = GetCasterDisplayName(casterCtrl);
            string abilityName = ability.abilityName;
            string zoneName = ability.zoneKind.ToString();

            string zoneMsg = $"{casterName} used {abilityName}, creating a {zoneName} zone.";
            _view.RPC(nameof(RPC_CombatLogMessage), RpcTarget.All, zoneMsg);

            var center = aimPos; center.z = 0f;
            float r = Mathf.Max(0.1f, ability.zoneRadius);
            int durTurns = Mathf.Max(1, Mathf.RoundToInt(ability.zoneDuration));

            switch (ability.zoneKind)
            {
                case ZoneKind.Negative:
                    {
                        int ownerVid = casterCtrl.photonView.ViewID;
                        bool isGreater = (ability.abilityName != null &&
                                          ability.abilityName.ToLowerInvariant().Contains("greater"));
                        if (isGreater)
                            ZoneManager.Instance.ReplaceAnyNegativeZone(ownerVid, center, r, durTurns);
                        else
                            ZoneManager.Instance.SpawnCircleZone(ZoneKind.Negative, center, r, durTurns, ownerVid);
                        break;
                    }
                case ZoneKind.Frozen:
                    ZoneManager.Instance.SpawnCircleZone(ZoneKind.Frozen, center, r, durTurns);
                    break;

                case ZoneKind.StormCrossing:
                    {
                        Vector3 origin = casterCtrl.transform.position;
                        Vector3 dir = (aimDir.sqrMagnitude > 1e-5f) ? aimDir : (aimPos - origin);
                        if (dir.sqrMagnitude < 1e-5f) dir = Vector3.right;
                        dir.z = 0f; dir.Normalize();

                        float length = Mathf.Max(1f, ability.lineRange);
                        float width = Mathf.Max(0.25f, ability.aoeRadius);

                        Vector3 a = center - dir * (length * 0.5f);
                        Vector3 b = center + dir * (length * 0.5f);

                        int ownerFaction = (int)casterCtrl.unit.Model.Faction;
                        ZoneManager.Instance.SpawnStormCrossing(
                            a, b, width, durTurns,
                            ownerFaction, /*allyHasteDur*/ 0,
                            /*enemyDamage*/ Mathf.Max(0, ability.baseDamage + casterCtrl.unit.Model.MagicPower),
                            /*shockChance*/ 0
                        );

                        float halfW = width * 0.5f;

                        ResolveStormCrossingInitialHit(
                            casterCtrl,
                            a, b,
                            halfW,
                            ability.baseDamage + casterCtrl.unit.Model.MagicPower,
                            ability.damageSource
                        );
                        break;
                    }
            }

            return;
        }

        // Broadcast heals first
        if (healIds.Count > 0)
        {
            Debug.Log($"[HealBroadcast] OUT count={healIds.Count} ids=[{string.Join(",", healIds)}] " +
                      $"vals=[{string.Join(",", healVals)}] barriers=[{string.Join(",", barriVals)}]");
            _view.RPC(nameof(RPC_ApplyHealingToClient), RpcTarget.All,
                casterCtrl.photonView.ViewID, abilityIndex,
                healIds.ToArray(), healVals.ToArray(), barriVals.ToArray());
        }

        // Broadcast damage second (your existing damage RPC)
        if (ability.spawnsSummons && SummonManager.Instance != null)
        {
            SummonManager.Instance.SpawnAriseSummons(casterCtrl.unit, ability);
        }

        if (dmgIds.Count > 0)
        {
            _view.RPC(nameof(RPC_ResolveAbility_Area), RpcTarget.All,
                casterCtrl.photonView.ViewID, abilityIndex,
                dmgIds.ToArray(),         // targets
                hitsList.ToArray(),       // hits
                dmgList.ToArray(),        // damages
                dtypeList.ToArray(),      // damage types
                procsList.ToArray()       // elemental procs
            );
        }

        return;
    }

    [PunRPC]
    private void RPC_ResolveAbility_Area(int casterViewId, int abilityIndex,
                                         int[] targetViewIds, bool[] hits, int[] damages,
                                         int[] damageTypes, byte[] elementalProcs)
    {
        Debug.Log($"[RPC_ResolveArea] ENTER isMaster={PhotonNetwork.IsMasterClient} casterViewId={casterViewId} abilityIndex={abilityIndex} targets={(targetViewIds != null ? targetViewIds.Length : 0)}");

        var casterCtrl = FindByView<UnitController>(casterViewId);

        var traceId = CombatLog.NewTraceId();

        // Mostrar el nombre de la habilidad sobre el caster
        if (casterCtrl != null && casterCtrl.unit != null)
        {
            var list = casterCtrl.unit.Model.Abilities;
            if (abilityIndex >= 0 && abilityIndex < list.Count)
            {
                CombatFeedbackUI.ShowAbilityName(casterCtrl.unit, list[abilityIndex].abilityName);
            }
        }

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
                    int beforeHp = target.Model.CurrentHP;
                    target.Model.ApplyDamageWithBarrier(dmg, (DamageType)dtype);
                    int dealt = Mathf.Max(0, beforeHp - target.Model.CurrentHP);

                    if (dealt > 0 && ZoneManager.Instance != null)
                    {
                        var tPV = target.Controller?.photonView;
                        if (tPV) ZoneManager.Instance.CancelNegativeZonesOfOwner(tPV.ViewID);
                    }

                    Debug.Log($"[Resolve] MASTER applied {dealt} HP dmg to {target.name} (type={(DamageType)dtype})");

                    // --- Combat log for MASTER client ---
                    string casterName = GetCasterDisplayName(casterCtrl);
                    string targetName = (target.Model != null && !string.IsNullOrEmpty(target.Model.UnitName))
                        ? target.Model.UnitName
                        : target.name;

                    UnitAbility ability = null;
                    if (casterCtrl != null && casterCtrl.unit != null && casterCtrl.unit.Model != null)
                    {
                        var list = casterCtrl.unit.Model.Abilities;
                        if (abilityIndex >= 0 && abilityIndex < list.Count)
                            ability = list[abilityIndex];
                    }

                    string abilityName = ability != null ? ability.abilityName : "Unknown Ability";
                    string damageTypeName = ((DamageType)dtype).ToString();

                    if (dealt > 0)
                    {
                        CombatLogUI.Log(
                            $"{casterName} used {abilityName} on {targetName}: HIT for {dealt} {damageTypeName} damage.");
                    }
                    else
                    {
                        CombatLogUI.Log(
                            $"{casterName} used {abilityName} on {targetName}: MISS");
                    }

                    // Local popup on master
                    var u = target.GetComponent<Unit>();
                    if (u)
                    {
                        if (dealt > 0)
                        {
                            // Obtener la habilidad utilizada para el efecto de daño
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

            // Elemental proc -> status (MASTER only)
            if (PhotonNetwork.IsMasterClient && elementalProcs != null && i < elementalProcs.Length)
            {
                var sc = target.GetComponent<StatusComponent>();
                if (sc != null)
                {
                    switch (elementalProcs[i])
                    {
                        case 1: sc.Apply(EffectLibrary.Burn(casterCtrl.photonView.ViewID, 2)); break; // Fire
                        case 2: sc.Apply(EffectLibrary.Freeze(1)); break;                              // Frost
                        case 3: sc.Apply(EffectLibrary.Shock(1)); break;                               // Electric
                    }
                }
            }
        }

        // --- route ability-driven status effects (self + on-hit) ---
        if (casterCtrl != null)
        {
            UnitAbility ability = null;
            var list = casterCtrl.unit.Model.Abilities;
            if (abilityIndex >= 0 && abilityIndex < list.Count) ability = list[abilityIndex];

            if (ability != null)
            {
                // build UnitController list from target ids (keep order aligned with hit flags)
                var targetsUC = new List<UnitController>(targetViewIds?.Length ?? 0);
                if (targetViewIds != null)
                {
                    for (int t = 0; t < targetViewIds.Length; t++)
                    {
                        var uc = FindByView<UnitController>(targetViewIds[t]);
                        if (uc != null) targetsUC.Add(uc);
                    }
                }

                RouteAbilityEffects(casterCtrl, targetsUC, ability, hits);
            }
        }

        // Handle completely missed attacks (no hit at all)
        if (hits != null && targetViewIds != null)
        {
            // Get caster + ability for log text
            string casterName = GetCasterDisplayName(casterCtrl);
            UnitAbility missAbility = null;
            if (casterCtrl != null && casterCtrl.unit != null)
            {
                var list = casterCtrl.unit.Model.Abilities;
                if (abilityIndex >= 0 && abilityIndex < list.Count)
                    missAbility = list[abilityIndex];
            }
            string abilityName = missAbility != null ? missAbility.abilityName : "Unknown Ability";

            for (int i = 0; i < targetViewIds.Length; i++)
            {
                bool missedCompletely = (i < hits.Length && !hits[i]);
                if (!missedCompletely) continue;

                var target = FindByView<Unit>(targetViewIds[i]);
                if (target != null)
                {
                    var u = target.GetComponent<Unit>();
                    if (u != null)
                    {
                        CombatFeedbackUI.ShowMiss(u);
                        if (AudioManager.Instance != null)
                            AudioManager.Instance.PlayEvadeSoundByUnitType(u);

                        string targetName =
                            (target.Model != null && !string.IsNullOrEmpty(target.Model.UnitName))
                            ? target.Model.UnitName
                            : target.name;

                        CombatLogUI.Log(
                            $"{casterName} used {abilityName} on {targetName}: MISS");
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

        var casterCtrl = FindByView<UnitController>(casterViewId);
        UnitAbility ability = null;
        if (casterCtrl != null && casterCtrl.unit != null && casterCtrl.unit.Model != null)
        {
            var list = casterCtrl.unit.Model.Abilities;
            if (abilityIndex >= 0 && abilityIndex < list.Count)
            {
                ability = list[abilityIndex];
            }
        }

        string casterName = GetCasterDisplayName(casterCtrl);
        string targetName = (target.Model != null && !string.IsNullOrEmpty(target.Model.UnitName)) ? target.Model.UnitName : target.name;
        string abilityName = (ability != null) ? ability.abilityName : "Unknown Ability";
        string damageTypeName = ((DamageType)damageType).ToString();

        int beforeHp = target.Model.CurrentHP;
        target.Model.ApplyDamageWithBarrier(damage, (DamageType)damageType);
        int dealt = Mathf.Max(0, beforeHp - target.Model.CurrentHP);

        Debug.Log($"[AbilityRPC] Applied {dealt} HP damage to {target.name} (type={(DamageType)damageType})");


        // --- Popup on every client ---
        var u = target.GetComponent<Unit>();
        if (u)
        {
            if (dealt > 0)
            {
                if (casterCtrl != null && abilityIndex >= 0 && abilityIndex < casterCtrl.unit.Model.Abilities.Count)
                {
                    ability = casterCtrl.unit.Model.Abilities[abilityIndex];
                }



                CombatFeedbackUI.ShowHit(u, dealt, (DamageType)damageType, false, casterCtrl?.unit, ability);
                // Play attack sound when hit connects
                if (AudioManager.Instance != null)
                {
                    if (casterCtrl != null && abilityIndex >= 0 && abilityIndex < casterCtrl.unit.Model.Abilities.Count)
                    {
                        var list = casterCtrl.unit.Model.Abilities;
                        AudioManager.Instance.PlayAttackSound(list[abilityIndex].abilityName);
                    }
                }

                CombatLogUI.Log(
                    $"{casterName} used {abilityName} on target {targetName}: HIT for {dealt} {damageTypeName} damage.");
            }
            else
            {
                CombatFeedbackUI.ShowMiss(u);
                // Play evade sound when attack misses
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayEvadeSoundByUnitType(u);

                CombatLogUI.Log(
                    $"{casterName} used {abilityName} on {targetName}: MISS");
            }
        }
    }

    [PunRPC]
    private void RPC_ApplyHealingToClient(int casterViewId, int abilityIndex,
                                      int[] targetViewIds, int[] heals, int[] barriers)
    {
        Debug.Log($"[RPC_ApplyHealing] ENTER targets={(targetViewIds == null ? 0 : targetViewIds.Length)}");

        // Caster + ability info
        var casterCtrl = FindByView<UnitController>(casterViewId);
        UnitAbility ability = null;
        string casterName = "Unknown";

        if (casterCtrl != null && casterCtrl.unit != null)
        {
            casterName = GetCasterDisplayName(casterCtrl);

            var list = casterCtrl.unit.Model.Abilities;
            if (abilityIndex >= 0 && abilityIndex < list.Count)
            {
                ability = list[abilityIndex];
                CombatFeedbackUI.ShowAbilityName(casterCtrl.unit, ability.abilityName);
            }
        }

        string abilityName = ability != null ? ability.abilityName : "Unknown Ability";

        int n = (targetViewIds == null) ? 0 : targetViewIds.Length;
        for (int i = 0; i < n; i++)
        {
            var pv = PhotonView.Find(targetViewIds[i]);
            if (pv == null)
            {
                Debug.LogWarning($"[RPC_ApplyHealing] PV NOT FOUND id={targetViewIds[i]}");
                continue;
            }

            var unit = pv.GetComponent<Unit>() ?? pv.GetComponentInChildren<Unit>() ?? pv.GetComponentInParent<Unit>();
            if (unit == null || unit.Model == null)
            {
                Debug.LogWarning($"[RPC_ApplyHealing] NO Unit on PV id={targetViewIds[i]} go={pv.gameObject.name}");
                continue;
            }

            int heal = (heals != null && i < heals.Length) ? heals[i] : 0;
            int barrier = (barriers != null && i < barriers.Length) ? barriers[i] : 0;

            string targetName =
                (unit.Model != null && !string.IsNullOrEmpty(unit.Model.UnitName))
                ? unit.Model.UnitName
                : unit.name;

            if (heal > 0)
            {
                int missing = Mathf.Max(0, unit.Model.MaxHP - unit.Model.CurrentHP);
                heal = Mathf.Min(heal, missing);   // safety clamp
                unit.Model.Heal(heal);
                CombatFeedbackUI.ShowHeal(unit, heal);
                Debug.Log($"[RPC_ApplyHealing] Healed {unit.name} for {heal}");
            }

            if (barrier > 0)
            {
                var sc = unit.GetComponent<StatusComponent>();
                if (sc != null) sc.Apply(EffectLibrary.Barrier(barrier, 2));
                Debug.Log($"[RPC_ApplyHealing] Barrier {barrier} to {unit.name}");
            }

            // Log only if something actually happened
            if (heal > 0 || barrier > 0)
            {
                string msg;
                if (heal > 0 && barrier > 0)
                    msg = $"{casterName} used {abilityName} on {targetName}: HEAL {heal}, BARRIER {barrier}.";
                else if (heal > 0)
                    msg = $"{casterName} used {abilityName} on {targetName}: HEAL {heal}.";
                else
                    msg = $"{casterName} used {abilityName} on {targetName}: BARRIER {barrier}.";

                CombatLogUI.Log(msg);
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

    private static bool HasHeal(UnitAbility ab)
    {
        return ab != null && ab.healsTarget;
    }

    private static bool HasBarrier(UnitAbility ab)
    {
        return ab != null && ab.grantsBarrier;
    }

    private void BroadcastAPSync(UnitController casterCtrl)
    {
        if (casterCtrl == null || casterCtrl.unit == null || casterCtrl.unit.Model == null) return;

        int unitPvId = casterCtrl.GetComponent<PhotonView>()?.ViewID ?? -1;
        var m = casterCtrl.unit.Model;

        _view.RPC(nameof(RPC_SyncAP), RpcTarget.All, unitPvId, m.CurrentActions, m.MaxActions);
    }

    [PunRPC]
    private void RPC_SyncAP(int unitViewId, int current, int max)
    {
        var pv = PhotonView.Find(unitViewId);
        if (pv == null) { Debug.LogWarning($"[RPC_SyncAP] PV not found id={unitViewId}"); return; }

        var unit = pv.GetComponent<Unit>() ?? pv.GetComponentInChildren<Unit>() ?? pv.GetComponentInParent<Unit>();
        if (unit == null || unit.Model == null) { Debug.LogWarning($"[RPC_SyncAP] No Unit/Model on PV={unitViewId} ({pv.gameObject.name})"); return; }

        unit.Model.NetSetActions(current, max); // fires OnActionPointsChanged on every client
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
             || ab.useTargetMissingHealth;
    }

    public void ResolveDamageServerOnly(UnitController target, int amount, DamageType type, int casterViewId = -1, int abilityIndex = -1)
    {
        if (!Photon.Pun.PhotonNetwork.IsMasterClient) return;
        if (target == null || target.model == null) return;

        // Apply on master using your existing barrier path
        int dealt = target.model.ApplyDamageWithBarrier(amount, type);
        Debug.Log($"[V2] MASTER dealt {dealt} ({type}) to {target.name} (server-only helper)");

        // Mirror to other clients (use -1 when there is no specific ability; RPC handles it)
        photonView.RPC(nameof(RPC_ApplyDamageToClient), RpcTarget.Others,
                       target.photonView.ViewID, amount, (int)type, casterViewId, abilityIndex);
    }

    public int ResolveHealingServerOnly(UnitController target, int amount, int casterViewId = -1, int abilityIndex = -1)
    {
        if (!Photon.Pun.PhotonNetwork.IsMasterClient) return 0;
        if (target == null || target.model == null) return 0;

        // Use your existing heal path
        int before = target.model.CurrentHP;
        target.model.Heal(amount);
        int applied = target.model.CurrentHP - before;
        Debug.Log($"[V2] MASTER healed {applied} on {target.name} (server-only helper)");

        // Mirror to other clients using your array-based RPC
        photonView.RPC(nameof(RPC_ApplyHealingToClient), RpcTarget.Others,
                       casterViewId, abilityIndex,
                       new int[] { target.photonView.ViewID },   // targets
                       new int[] { applied },                    // heals
                       new int[] { 0 }                           // barriers
                       );
        return applied;
    }

    private static bool HasTag(Unit u, string tag)
    {
        if (u == null) return false;
        var sc = u.GetComponent<StatusComponent>();
        if (sc == null || string.IsNullOrEmpty(tag)) return false;

        switch (tag)
        {
            case "Taunt":
            case "Taunted": return sc.Has(StatusType.Taunt);
            case "Frozen": return sc.Has(StatusType.Freeze);
            case "Rooted": return sc.Has(StatusType.Root);
            case "Barrier": return sc.Has(StatusType.Barrier);
            case "Incandescent": return sc.Has(StatusType.Incandescent);
            case "Enraged": return sc.Has(StatusType.Enraged);
            case "Bleeding": return sc.Has(StatusType.Bleed);
            case "Haste": return sc.Has(StatusType.Haste);
            case "Shocked": return sc.Has(StatusType.Shock);
            default: return false;
        }
    }

    // Apply to one unit (MASTER only)
    void ApplyEffect(UnitController target, StatusEffect e)
    {
        if (!PhotonNetwork.IsMasterClient || target == null || e == null) return;
        var sc = target.GetComponent<StatusComponent>() ?? target.gameObject.AddComponent<StatusComponent>();
        Debug.Log($"[Apply] to={target.name} type={e.type} barrierHP={e.barrierHP} dur={e.remainingTurns}");
        sc.Apply(e);
    }

    // Turn one directive row into a concrete runtime effect
    StatusEffect MapDirectiveToEffect(AbilityEffectDirective d, UnitController caster)
    {
        Debug.Log($"[Map] {d.effect} target={d.target} dur={d.duration} amt={d.amount} chance={d.chancePct}");
        switch (d.effect)
        {
            case EffectId.Enraged: return EffectLibrary.Enraged(d.duration);
            case EffectId.Bleed: return EffectLibrary.Bleed(d.amount > 0 ? d.amount : 6);
            case EffectId.Taunt: return EffectLibrary.Taunt(caster.photonView.ViewID, d.duration);
            case EffectId.Barrier: return EffectLibrary.Barrier(Mathf.Max(1, d.amount), d.duration);
            case EffectId.Incandescent: return EffectLibrary.Incandescent(d.duration);
            case EffectId.Root: return EffectLibrary.Root(d.duration);
            case EffectId.Shock: return EffectLibrary.Shock(d.duration);
            case EffectId.Burn: return EffectLibrary.Burn(caster.photonView.ViewID, Mathf.Max(1, d.duration));
            case EffectId.Freeze: return EffectLibrary.Freeze(d.duration);
            case EffectId.Buff: return EffectLibrary.Buff(d.stat, d.amount, d.duration);
            case EffectId.Debuff: return EffectLibrary.Debuff(d.stat, d.amount, d.duration);
            default: return null;
        }
    }

    // Route all directives for this ability: do self first, then per-hit targets
    void RouteAbilityEffects(UnitController caster, List<UnitController> targets, UnitAbility ability, bool[] hitFlags)
    {
        if (!PhotonNetwork.IsMasterClient || ability == null) return;
        int n = ability.effects != null ? ability.effects.Count : 0;
        Debug.Log($"[Router] ability={ability.abilityName} directives={n}");
        if (n == 0) return;

        // Self once
        foreach (var row in ability.effects)
        {
            if (row == null || row.target != EffectTarget.Self) continue;
            if (UnityEngine.Random.Range(1, 101) > row.chancePct) continue;
            var eff = MapDirectiveToEffect(row, caster);
            ApplyEffect(caster, eff);
        }

        // Per-hit targets
        if (targets == null || targets.Count == 0) return;
        for (int i = 0; i < targets.Count; i++)
        {
            bool wasHit = (hitFlags == null || hitFlags.Length == 0) || (i < hitFlags.Length && hitFlags[i]);
            if (!wasHit) continue;

            var t = targets[i];
            foreach (var row in ability.effects)
            {
                if (row == null || row.target != EffectTarget.HitTarget) continue;
                if (UnityEngine.Random.Range(1, 101) > row.chancePct) continue;
                var eff = MapDirectiveToEffect(row, caster);
                ApplyEffect(t, eff);
            }
        }
    }

    // --- receivers set the model state, which triggers HUD via OnStateChanged ---
    [PunRPC]
    void RPC_SetUnitState(int unitViewId, string key, string value)
    {
        var uc = FindByView<UnitController>(unitViewId);
        if (uc != null && uc.unit != null)
        {
            uc.unit.Model.SetState(key, value); // UnitModel should invoke OnStateChanged internally
            Debug.Log($"[RPC_SetUnitState] {uc.name}: {key}={value}");
        }
    }

    void SpawnStructure_Master(StructureSpec s, Vector3 center)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (StructureManager.Instance == null) return;

        switch (s.kind)
        {
            case StructureKind.IcePillar:
                StructureManager.Instance.SpawnIcePillar(center, s.faction, s.ownerViewId, s.hp, s.radius, s.durationTurns);
                break;

            case StructureKind.Bonfire:
                StructureManager.Instance.SpawnBonfire(center, s.faction, s.ownerViewId, s.healPerTick, s.radius, s.durationTurns);
                break;
        }
    }

    bool IsStructureAbility(UnitAbility ab, out StructureKind kind)
    {
        kind = StructureKind.None;
        if (ab == null) return false;

        // Preferred: from SO flags
        if (ab.spawnsStructure && ab.structureKind != StructureKind.None)
        {
            kind = ab.structureKind;
            return true;
        }

        // Fallback: quick name sniff until SOs are updated
        var n = (ab.abilityName ?? "").ToLowerInvariant();
        if (n.Contains("ice") && n.Contains("pillar")) { kind = StructureKind.IcePillar; return true; }
        if (n.Contains("bonfire")) { kind = StructureKind.Bonfire; return true; }
        return false;
    }

    struct StructureSpec
    {
        public StructureKind kind;
        public float hp;
        public int healPerTick;
        public float radius;
        public int ownerViewId;
        public UnitFaction faction;
        public int durationTurns;
    }

    StructureSpec BuildSpec(Unit caster, UnitAbility ab, StructureKind kind)
    {
        var pv = caster?.GetComponent<PhotonView>();
        var m = caster?.GetComponent<UnitModel>();

        var spec = new StructureSpec
        {
            kind = kind,
            ownerViewId = pv ? pv.ViewID : -1,
            faction = m ? m.Faction : UnitFaction.Hero,
            hp = ab.structureHP,
            healPerTick = (int)ab.structureHeal,
            radius = ab.structureRadius,
            durationTurns = Mathf.RoundToInt(ab.structureDuration), // treat SO field as TURNS
        };
        return spec;
    }

    private int ComputeDamageVsStructure(UnitAbility ab, Unit caster)
    {
        // Simple: reuse your normal formula without defender stats
        int dmg = ab.baseDamage;
        if (ab.damageSource == DamageType.Physical) dmg += caster.Model.Strength;
        else if (ab.damageSource == DamageType.Magical) dmg += caster.Model.MagicPower;

        dmg = Mathf.RoundToInt(dmg * Mathf.Max(0f, ab.damageMultiplier)) + Mathf.Max(0, ab.bonusDamage);
        return Mathf.Max(0, dmg);
    }

    private void HitStructureAuth(UnitAbility ab, UnitController casterCtrl, StructureBase s)
    {
        if (s == null || StructureManager.Instance == null) return;

        int dmg = ComputeDamageVsStructure(ab, casterCtrl.unit);
        if (dmg <= 0) return;

        var sm = StructureManager.Instance;
        if (Photon.Pun.PhotonNetwork.IsMasterClient)
        {
            sm.DamageStructure(s.NetId, dmg); // applies + mirrors
        }
        else
        {
            // ask master to apply
            sm.photonView.RPC(nameof(StructureManager.RPC_RequestStructureHit),
                              Photon.Pun.RpcTarget.MasterClient,
                              s.NetId, dmg);
        }
    }

    [PunRPC]
    void RPC_TeleportUnit(int unitViewId, Vector3 dest)
    {
        Debug.Log($"[BlinkRPC] Teleport unitViewId={unitViewId} → {dest}");

        var uc = FindByView<UnitController>(unitViewId);
        if (uc == null || uc.unit == null) return;

        var u = uc.unit;
        var from = u.transform.position;

        // Snap instantly
        u.transform.position = new Vector3(dest.x, dest.y, from.z);

        // Zone crossing trigger (use same hook as walking)
        if (PhotonNetwork.IsMasterClient && ZoneManager.Instance != null)
            ZoneManager.Instance.HandleOnMove(u, from, dest);

        // Status hooks similar to movement
        u.GetComponent<StatusComponent>()?.OnMoved();

        // Little bit of juice
        u.View.SetFacingDirection((dest - from).normalized);
        u.View.PlayMoveLandNet();
    }

    private void ResolveBlinkAoE(UnitController casterCtrl, UnitAbility ability, Vector3 center, int abilityIndex)
    {
        // Only the master decides who gets hit
        if (!PhotonNetwork.IsMasterClient) return;
        if (ability == null) return;
        if (ability.aoeRadius <= 0f) return;          // Blink with no AoE = pure movement
        if (!DealsAnyDamage(ability)) return;         // safety: no damage fields set

        var caster = casterCtrl.unit;
        if (caster == null || caster.Model == null) return;

        // Find units in radius around the landing point
        var computedTargets = CombatCalculator.GetUnitsInRadius(
                center,
                ability.aoeRadius,
                casterCtrl.unit
            );

        if (computedTargets == null || computedTargets.Count == 0)
            return;

        foreach (var target in computedTargets)
        {
            if (target == null || target.Model == null) continue;
            if (!target.Model.IsAlive()) continue;

            // Optional: Blink AoE should only hurt enemies
            if (target.Model.Faction == caster.Model.Faction)
                continue;

            // Negative Zone protection: attacker outside & target inside ⇒ no damage
            if (ZoneManager.Instance != null &&
                ZoneManager.Instance.IsTargetProtectedByNegativeZone(
                    caster.transform.position,
                    target.transform.position))
            {
                // Show miss popup for feedback
                if (target.Controller != null)
                    photonView.RPC(nameof(RPC_ShowMissPopup), RpcTarget.All, target.Controller.photonView.ViewID);
                continue;
            }

            // Compute damage number using your helper (stat-scaled)
            int dmg = ComputeDamageFromAbility(ability, caster, vsStructure: false);
            if (dmg <= 0) continue;

            // Apply on master + mirror to others using existing helper
            if (target.Controller != null)
            {
                ResolveDamageServerOnly(
                    target.Controller,
                    dmg,
                    ability.damageSource,
                    casterCtrl.photonView.ViewID,
                    abilityIndex
                );
            }

            // If the target actually took real damage, cancel its Negative Zones (your existing hook)
            if (ZoneManager.Instance != null && target.Controller != null)
            {
                var tPV = target.Controller.photonView;
                if (tPV) ZoneManager.Instance.CancelNegativeZonesOfOwner(tPV.ViewID);
            }
        }
    }

    private int ComputeDamageFromAbility(UnitAbility ability, Unit caster, bool vsStructure)
    {
        if (ability == null || caster == null) return 0;

        // Mixed damage route
        if (ability.isMixedDamage)
        {
            int strength = caster.Model.Strength;
            int mpower = caster.Model.MagicPower;

            return Mathf.Max(0, CombatCalculator.CalculateMixedDamage(
                ability.baseDamage,
                strength, /*armor*/ 0,
                mpower,   /*mr*/    0,
                ability.mixedPhysicalPercent
            ));
        }

        // Single-source (Physical or Magical or Elemental mapped to Phys/Mag)
        bool isPhysical = (ability.damageSource == DamageType.Physical);
        bool isMagical = (ability.damageSource == DamageType.Magical
                           || ability.damageSource == DamageType.Fire
                           || ability.damageSource == DamageType.Frost
                           || ability.damageSource == DamageType.Electric);

        int attackerStat = isPhysical ? caster.Model.Strength : caster.Model.MagicPower;
        int defenderStat = 0;

        int dmg = Mathf.RoundToInt(
            CombatCalculator.CalculateDamage(ability.baseDamage, attackerStat, defenderStat)
        );

        // Multipliers / flat bonus parity with your main path
        if (ability.bonusPerMissingHpPercent > 0 && caster != null)
            dmg = CombatCalculator.ApplyMissingHpBonus(dmg, caster, ability.bonusPerMissingHpPercent);

        if (ability.damageMultiplier > 0f)
            dmg = Mathf.RoundToInt(dmg * ability.damageMultiplier);

        if (ability.bonusDamage > 0)
            dmg += ability.bonusDamage;

        return Mathf.Max(0, dmg);
    }

    private void ResolveStormCrossingInitialHit(UnitController casterCtrl, Vector3 A, Vector3 B, float halfWidth, int damage, DamageType dtype)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var units = FindObjectsByType<Unit>(FindObjectsSortMode.None);

        foreach (var u in units)
        {
            if (u == null || !u.Model.IsAlive()) continue;
            if (u == casterCtrl.unit) continue;  // don't hit caster

            // Faction check (Storm Crossing damages ENEMIES by design)
            if (u.Model.Faction == casterCtrl.unit.Model.Faction) continue;

            if (StormCrossingZone.IsCrossing(A, B, halfWidth, u.transform.position, u.transform.position))
            {
                // Apply damage using your master-authoritative damage resolver
                ResolveDamageServerOnly(
                    u.Controller,
                    damage,
                    dtype,
                    casterCtrl.photonView.ViewID,
                    /* abilityIndex */ -1
                );
            }
        }
    }

    private static string GetCasterDisplayName(UnitController casterCtrl)
    {
        if (casterCtrl == null || casterCtrl.unit == null) return "Unknown";

        var owner = casterCtrl.photonView != null ? casterCtrl.photonView.Owner : null;
        if (owner != null && !string.IsNullOrEmpty(owner.NickName))
            return owner.NickName;

        if (casterCtrl.unit != null)
            return casterCtrl.unit.Model.UnitName;

        return casterCtrl.name;
    }

    [PunRPC]
    void RPC_CombatLogMessage(string message)
    {
        CombatLogUI.Log(message);
    }
}

