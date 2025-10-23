using System.Collections.Generic;
using UnityEngine;

public static class CombatCalculator 
{
    // Summary
    // Calculates final hit chance as:
    // BaseChance + AttackerAffinity + FlankBonus - CoverPenalty
    // Clamped between 0 and 100.

    public static float GetHitChance(
        float baseChance,
        int attackerAffinity,
        int flankCount,
        bool isFlanked,       // whether target is flanked (flankCount > 0)
        bool hasMediumCover,
        bool hasHeavyCover)
    {
        float chance = baseChance + attackerAffinity;

        // Flank bonus: +10% per flanking unit
        if (isFlanked)
            chance += flankCount * 10f;

        // Cover penalty: medium = -20%, heavy = -40%
        if (hasHeavyCover) chance -= 40f;
        else if (hasMediumCover) chance -= 20f;

        return Mathf.Clamp(chance, 0f, 100f);
    }

    // Summary
    // Calculates final damage:
    // (RawDamage + Strength or MagicPower) * 100 / (100 + Defense)

    public static float CalculateDamage(
        int rawDamage,
        int attackerStat,      // Strength for physical, MagicPower for magical
        int defenderDefense)   // Armor or MagicResistance
    {
        float total = rawDamage + attackerStat;
        return total * 100f / (100f + defenderDefense);
    }

    // Summary
    // Checks whether target is within an ability’s range.

    public static bool IsInRange(
        Vector3 origin,
        Vector3 target,
        float maxRange)
    {
        return Vector3.Distance(origin, target) <= maxRange;
    }

    public static int CountFlankingAllies(Unit target, Unit attacker)
    {
        int count = 0;
        float flankRange = 1.2f; // Units within this radius are considered adjacent

        foreach (Unit potentialFlanker in Object.FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            if (potentialFlanker == target || potentialFlanker == attacker)
                continue;

            if (!attacker.IsAlly(potentialFlanker))
                continue;

            float distance = Vector3.Distance(potentialFlanker.transform.position, target.transform.position);
            if (distance <= flankRange)
            {
                count++;
                Debug.Log($"[Flanking] {potentialFlanker.Model.UnitName} is flanking {target.Model.UnitName}");
            }
        }

        Debug.Log($"[Flanking] Total flanking units: {count}");
        return count;
    }

    public static void CheckCover(
        Vector2 attackerPos,
        Vector2 targetPos,
        out bool hasMediumCover,
        out bool hasHeavyCover)
    {
        hasMediumCover = false;
        hasHeavyCover = false;

        Vector2 direction = (targetPos - attackerPos).normalized;
        float distance = Vector2.Distance(attackerPos, targetPos);

        // Check for obstacles using raycast
        RaycastHit2D[] hit = Physics2D.RaycastAll(attackerPos, direction, distance);
        foreach (RaycastHit2D collider in hit)
        {
            if (collider.collider == null)
                continue;

            string layerName = LayerMask.LayerToName(collider.collider.gameObject.layer);

            if (layerName == "HeavyCover")
            {
                hasHeavyCover = true;
                hasMediumCover = false; // override medium cover
                Debug.Log("[Cover] Target has heavy cover — ignoring medium cover");
                break; // early exit, we don’t need to keep checking
            }

            if (layerName == "MediumCover" && !hasHeavyCover)
            {
                hasMediumCover = true;
                Debug.Log("[Cover] Target has medium cover");
            }
        }

        Vector3 to = new Vector3(3f, 3f, 0f);

        if (StructureManager.Instance != null)
        {
            bool sMed, sHeavy;
            if (StructureManager.Instance.HasStructureCoverBetween(direction, to, out sMed, out sHeavy))
            {
                hasHeavyCover = hasHeavyCover || sHeavy;
                hasMediumCover = hasMediumCover || sMed;
            }
        }

        if (!hasHeavyCover && !hasMediumCover)
            Debug.Log("[Cover] No cover detected");
    }

    // ---- Area queries ----

    // Collect enemy units within radius of 'center'
    public static List<Unit> GetUnitsInRadius(Vector3 center, float radius, Unit caster)
    {
        var result = new List<Unit>();
        if (radius <= 0f || caster == null) return result;

        // 2D safety
        center.z = 0f;

        // Grab any colliders, then map up to Unit (handles child colliders too)
        var cols = Physics2D.OverlapCircleAll(center, radius);
        if (cols == null || cols.Length == 0) return result;

        var seen = new HashSet<Unit>();
        foreach (var col in cols)
        {
            if (col == null) continue;

            // be tolerant: the collider may be on a child
            var uc = col.GetComponentInParent<UnitController>();
            var u = uc != null ? uc.unit : null;
            if (u == null || u == caster) continue;

            if (seen.Add(u))
                result.Add(u);
        }
        return result;
    }

    // Collect enemy units roughly along the line from 'origin' towards 'directionTo',
    // limited by range and maxTargets. Uses a dot check to ensure alignment.
    public static List<Unit> GetLineTargets(Unit caster, Unit primaryTarget, float range, int maxTargets, float alignmentTolerance)
    {
        var result = new List<Unit>();
        if (caster == null || primaryTarget == null) return result;

        var origin = caster.transform.position;
        var dir = (primaryTarget.transform.position - origin).normalized;
        if (dir.sqrMagnitude < 1e-6f) return result;

        // Scan all units once; direction test keeps it cheap enough for small counts
        var all = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        var candidates = new List<(Unit u, float dist)>();

        foreach (var u in all)
        {
            if (u == null || u == caster) continue;

            var to = u.transform.position - origin;
            var dist = to.magnitude;
            if (dist > range + 0.01f) continue;

            var align = Vector3.Dot(dir, to.normalized);
            if (align >= Mathf.Clamp01(alignmentTolerance))
                candidates.Add((u, dist));
        }

        candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

        int cap = Mathf.Max(1, maxTargets);
        for (int i = 0; i < candidates.Count && result.Count < cap; i++)
            result.Add(candidates[i].u);

        return result;
    }

    // ---- Advanced damage ----

    // Mixed damage: split into physical and magical, mitigate each separately, then sum.
    public static int CalculateMixedDamage(int baseDamage, int attackerStrength, int defenderArmor,
                                           int attackerMagic, int defenderMR, int physicalPercent)
    {
        physicalPercent = Mathf.Clamp(physicalPercent, 0, 100);
        int magicalPercent = 100 - physicalPercent;

        int physBase = Mathf.RoundToInt(baseDamage * (physicalPercent / 100f));
        int magBase = baseDamage - physBase;

        float physOut = CalculateDamage(physBase, attackerStrength, defenderArmor);
        float magOut = CalculateDamage(magBase, attackerMagic, defenderMR);

        return Mathf.Max(0, Mathf.RoundToInt(physOut + magOut));
    }

    // Apply collateral reduction for non-primary line targets
    public static int ApplyCollateralPercent(int damage, int percent)
    {
        return Mathf.Max(0, Mathf.RoundToInt(damage * Mathf.Clamp01(percent / 100f)));
    }

    // Add bonus based on target's missing HP (if you expose MaxHP/CurrentHP)
    public static int ApplyMissingHpBonus(int damage, Unit target, int bonusPerMissingHpPercent)
    {
        if (bonusPerMissingHpPercent <= 0 || target == null) return damage;

        // Try to read from your model; if you name it differently, adjust here.
        int maxHp = target.Model.MaxHP;       // <- ensure your model exposes MaxHP
        int curHp = target.Model.CurrentHP;   // <- ensure your model exposes CurrentHP

        int missing = Mathf.Max(0, maxHp - curHp);
        int bonus = Mathf.RoundToInt(missing * (bonusPerMissingHpPercent / 100f));
        return Mathf.Max(0, damage + bonus);
    }
}
