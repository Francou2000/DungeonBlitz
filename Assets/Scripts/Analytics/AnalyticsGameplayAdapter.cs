using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Thin adapter used by gameplay systems.
/// Keep gameplay code calling these methods and avoid direct coupling to analytics backend code.
/// </summary>
public sealed class AnalyticsGameplayAdapter : MonoBehaviour
{
    public static AnalyticsGameplayAdapter Instance { get; private set; }

    public void InitializeIfNeeded()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private static string GetDefaultMatchId()
    {
        string room = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "offline";
        return $"{room}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    }

    private static string GetDmPlayerId()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.MasterClient != null)
        {
            return PhotonNetwork.MasterClient.UserId ?? PhotonNetwork.MasterClient.ActorNumber.ToString();
        }

        return "local_dm";
    }

    // Where to fire: once when combat/match scene is ready and teams are finalized.
    // Data expected: stable match id, level combination id, hero count and online/offline state.
    // Why: anchors all per-match KPIs and ties all later events to one match_id.
    public void OnMatchStarted(string matchId, string levelCombinationId, int heroCount, bool isOnlineMatch)
    {
        string safeMatchId = string.IsNullOrWhiteSpace(matchId) ? GetDefaultMatchId() : matchId;
        string safeLevel = string.IsNullOrWhiteSpace(levelCombinationId) ? "unknown_level_combo" : levelCombinationId;
        GameAnalyticsService.Instance.TrackMatchStarted(safeMatchId, safeLevel, GetDmPlayerId(), heroCount, isOnlineMatch);
    }

    // Where to fire: once when a winner is decided.
    // Data expected: winner side, success flag, alive counts.
    // Why: core match outcome KPIs (DM winrate, success/failure, duration).
    public void OnMatchEnded(string winner, bool isSuccessful, int heroesAlive, int goblinsAlive)
    {
        GameAnalyticsService.Instance.TrackMatchEnded(winner, isSuccessful, heroesAlive, goblinsAlive);
    }

    // Where to fire: when a hero actor/unit is spawned.
    // Data expected: stable player id + class/archetype.
    // Why: enables time-alive and class performance KPIs.
    public void OnHeroSpawned(string playerId, string playerClass)
    {
        GameAnalyticsService.Instance.RegisterHeroSpawn(playerId, playerClass);
    }

    // Where to fire: when hero death is confirmed by authoritative side.
    // Data expected: victim id/class and optional killer data.
    // Why: supports time alive + death attribution.
    public void OnHeroDied(string playerId, string playerClass, string killerType, string killerId)
    {
        GameAnalyticsService.Instance.TrackPlayerDied(playerId, playerClass, killerType, killerId);
    }

    // Where to fire: whenever a hero successfully deals damage.
    // Data expected: source hero id and damage amount.
    // Why: supports damage per hero KPIs.
    public void OnHeroDealtDamage(string playerId, int amount)
    {
        GameAnalyticsService.Instance.RegisterHeroDamageDone(playerId, amount);
    }

    // Where to fire: when a hero kill is confirmed.
    // Data expected: killer id/class + victim goblin id/type.
    // Why: supports kills per hero and goblin kill ownership KPIs.
    public void OnHeroKilledGoblin(string playerId, string playerClass, string goblinId, string goblinType)
    {
        GameAnalyticsService.Instance.RegisterHeroKill(playerId);
        GameAnalyticsService.Instance.TrackGoblinDied(goblinId, goblinType, playerId, playerClass);
    }

    // Where to fire: on each validated ability cast.
    // Data expected: player/class, ability name and optional target/turn.
    // Why: identifies most-used and under-used abilities.
    public void OnAbilityUsed(string playerId, string playerClass, string abilityName, string targetType, int turnNumber)
    {
        GameAnalyticsService.Instance.TrackAbilityUsed(playerId, playerClass, abilityName, targetType, turnNumber);
    }

    // Where to fire: when a goblin/enemy unit is spawned.
    // Data expected: goblin id + type.
    // Why: required for accurate goblin time-alive computations.
    public void OnGoblinSpawned(string goblinId, string goblinType)
    {
        GameAnalyticsService.Instance.RegisterGoblinSpawn(goblinId, goblinType);
    }

    // Where to fire: whenever a goblin deals damage.
    // Data expected: goblin id and damage amount.
    // Why: supports enemy damage contribution KPIs.
    public void OnGoblinDealtDamage(string goblinId, int amount)
    {
        GameAnalyticsService.Instance.RegisterGoblinDamageDealt(goblinId, amount);
    }

    // Where to fire: when goblin death is confirmed by authority.
    // Data expected: goblin id/type + optional killer metadata.
    // Why: supports goblin lifetime + kill attribution by hero class.
    public void OnGoblinDied(string goblinId, string goblinType, string killerPlayerId, string killerClass)
    {
        GameAnalyticsService.Instance.TrackGoblinDied(goblinId, goblinType, killerPlayerId, killerClass);
    }

    // Where to fire: on each DM selection action (prefer authority side only).
    // Data expected: selected goblin type + quantity.
    // Why: DM composition and quantity KPI.
    public void OnDMSelectedUnit(string goblinType, int quantity)
    {
        GameAnalyticsService.Instance.TrackDMUnitSelection(goblinType, quantity);
    }

    // Where to fire: when purchase is accepted/consumed (not button click intent).
    // Data expected: buyer id/class and item id.
    // Why: economy KPIs by class + item popularity.
    public void OnItemPurchased(string playerId, string playerClass, string itemId, int quantity)
    {
        GameAnalyticsService.Instance.TrackItemPurchased(playerId, playerClass, itemId, quantity);
    }

    public static AnalyticsGameplayAdapter TryGet()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Instance = FindAnyObjectByType<AnalyticsGameplayAdapter>();
        if (Instance == null && AnalyticsBootstrap.Instance != null)
        {
            Instance = AnalyticsBootstrap.Instance.GetComponent<AnalyticsGameplayAdapter>();
        }

        return Instance;
    }

    public static string ResolvePlayerId(Player player)
    {
        if (player == null) return "unknown_player";
        return !string.IsNullOrWhiteSpace(player.UserId) ? player.UserId : player.ActorNumber.ToString();
    }
}
