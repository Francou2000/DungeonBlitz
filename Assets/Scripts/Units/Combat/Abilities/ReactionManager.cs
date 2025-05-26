using UnityEngine;

public static class ReactionManager
{
    public static void TryTriggerReactions(Unit movingUnit, Vector3 oldPosition)
    {
        Debug.Log($"[Reaction] Checking opportunity attacks on move by {movingUnit.Model.UnitName}");

        var allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);

        foreach (var enemy in allUnits)
        {
            if (enemy == movingUnit) continue;
            if (enemy.Model.Faction == movingUnit.Model.Faction) continue;

            //Check if unit has reactions left
            if (!enemy.Model.CanReact())
            {
                Debug.Log($"[Reaction] {enemy.Model.UnitName} has no reactions left.");
                continue;
            }


            // Loop enemy reactions
            foreach (var reaction in enemy.Model.Abilities)
            {
                if (!reaction.isReaction || reaction.reactionTrigger != ReactionTrigger.OnEnemyLeavesRange)
                    continue;

                float oldDistance = Vector2.Distance(enemy.transform.position, oldPosition);
                float newDistance = Vector2.Distance(enemy.transform.position, movingUnit.transform.position);

                Debug.Log($"[Reaction] {enemy.Model.UnitName} | Old: {oldDistance:F2}, New: {newDistance:F2}, Range: {reaction.range}");

                if (oldDistance <= reaction.range && newDistance > reaction.range)
                {
                    Debug.Log($"[Reaction] {enemy.Model.UnitName} triggers opportunity attack on {movingUnit.Model.UnitName}!");

                    float damage = CombatCalculator.CalculateDamage(
                        reaction.baseDamage,
                        enemy.Model.Strength,
                        movingUnit.Model.Armor
                    );

                    movingUnit.Model.TakeDamage(Mathf.RoundToInt(damage), DamageType.Physical); // You can generalize damage type too
                    enemy.Model.SpendReaction(); // Optional
                }
            }
        }
    }
}