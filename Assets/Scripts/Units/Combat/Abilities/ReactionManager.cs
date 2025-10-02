using System;
using System.Collections.Generic;
using UnityEngine;

public class ReactionManager : MonoBehaviour
{
    public static ReactionManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) 
        {
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
    }

    public void TryTriggerReactions(Unit mover, Vector3 fromPos)
    {
        if (mover == null) return;

        foreach (var enemy in FindThreateningEnemies(mover))
        {
            if (!enemy.Model.CanReact()) continue;

            // Look for a defined reaction ability
            var reactAbility = GetReactionAbility(enemy);
            if (reactAbility != null)
            {
                // Route through resolver, authoritative
                AbilityResolver.Instance.RequestCast(
                    enemy.Controller,
                    reactAbility,
                    new Unit[] { mover },
                    enemy.transform.position,
                    enemy.transform.position,
                    string.Empty
                );
                enemy.Model.SpendReaction();
                continue;
            }

            //PerformOpportunityAttack(enemy, mover);
            enemy.Model.SpendReaction();
        }
    }

    UnitAbility GetReactionAbility(Unit enemy)
    {
        foreach (var ab in enemy.Model.Abilities)
            if (ab.isReaction) return ab;
        return null;
    }

    List<Unit> FindThreateningEnemies(Unit mover)
    {
        var enemies = new List<Unit>();
        var fromPos = transform.position;
        foreach (var u in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            if (u == mover) continue;
            if (u.Model.Faction == mover.Model.Faction) continue;

            float dist = Vector3.Distance(u.transform.position, fromPos); 
            if (dist < 1.5f) enemies.Add(u);
        }
        return enemies;
    }

    /*
    void PerformOpportunityAttack(Unit enemy, Unit mover)
    {
        var basic = enemy.Model.GetBasicAttackAbility();
        if (basic != null)
        {
            AbilityResolver.Instance.RequestCast(enemy.Controller, basic, new Unit[] { mover });
        }
    }*/
}