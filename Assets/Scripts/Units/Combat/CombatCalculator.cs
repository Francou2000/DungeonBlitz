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

        if (!hasHeavyCover && !hasMediumCover)
            Debug.Log("[Cover] No cover detected");
    }
}
