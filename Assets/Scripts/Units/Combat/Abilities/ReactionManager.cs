using UnityEngine;

public static class ReactionManager
{
    public static void TryTriggerReactions(Unit movingUnit, Vector3 oldPosition)
    {
        Debug.Log($"[Reaction] Checking opportunity attacks on move by {movingUnit.Model.UnitName}");

        var allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);

        foreach (var enemy in enemies)
        {
            if (!enemy.Model.CanReact()) continue;

            // Prefer a defined reaction ability
            var reactAbility = GetReactionAbility(enemy);
            if (reactAbility != null)
            {
                // (Later) validate range/trigger type, then execute via AbilityResolver
                AbilityResolver.Instance.RequestCast(
                    enemy.Controller,
                    reactAbility,
                    new Unit[] { mover }
                );
                enemy.Model.SpendReaction();
                continue;
            }

            // Fallback: your current OA logic (physical hit)
            PerformOpportunityAttack(enemy, mover);
            enemy.Model.SpendReaction();
        }
    }
}