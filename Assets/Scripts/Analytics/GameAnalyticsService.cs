using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Unity.Services.Analytics;

/// <summary>
/// Single entry point for all gameplay analytics events.
/// Responsibilities:
/// - validate payloads
/// - enforce authority rules (avoid duplicate events in multiplayer)
/// - queue events if analytics is not ready yet
/// - expose typed TrackX methods for gameplay callers
/// </summary>
public sealed class GameAnalyticsService
{
    public static GameAnalyticsService Instance { get; } = new();

    private readonly Queue<Dictionary<string, object>> _pending = new();
    private readonly HashSet<string> _dedupeKeys = new();

    public readonly MatchAnalyticsSession Session = new();

    private GameAnalyticsService() { }

    private static long NowUnixMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static bool IsBootstrapReady => AnalyticsBootstrap.Instance != null && AnalyticsBootstrap.Instance.IsReady;

    private static bool ShouldEmitAuthorityOnly()
    {
        return !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
    }

    private void RecordOrQueue(string eventName, Dictionary<string, object> payload, string dedupeKey = null)
    {
        if (string.IsNullOrWhiteSpace(eventName) || payload == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(dedupeKey))
        {
            if (_dedupeKeys.Contains(dedupeKey))
            {
                return;
            }
            _dedupeKeys.Add(dedupeKey);
        }

        payload["time_stamp"] = payload.ContainsKey("time_stamp") ? payload["time_stamp"] : NowUnixMs;

        if (!IsBootstrapReady)
        {
            payload["__eventName"] = eventName;
            _pending.Enqueue(payload);
            return;
        }

        var customEvent = new CustomEvent(eventName);
        foreach (var kv in payload)
        {
            if (kv.Key == "__eventName") continue;
            customEvent[kv.Key] = kv.Value;
        }

        AnalyticsService.Instance.RecordEvent(customEvent);
    }

    public void FlushQueuedEvents()
    {
        if (!IsBootstrapReady)
        {
            return;
        }

        while (_pending.Count > 0)
        {
            var payload = _pending.Dequeue();
            if (!payload.TryGetValue("__eventName", out var evtNameObj) || evtNameObj is not string evtName)
            {
                continue;
            }

            payload.Remove("__eventName");
            RecordOrQueue(evtName, payload);
        }

        AnalyticsService.Instance.Flush();
    }

    public void TrackMatchStarted(string matchId, string levelCombinationId, string dmPlayerId, int heroCount, bool isOnlineMatch)
    {
        if (!ShouldEmitAuthorityOnly()) return;
        if (string.IsNullOrWhiteSpace(matchId) || string.IsNullOrWhiteSpace(levelCombinationId)) return;

        long startMs = NowUnixMs;
        Session.Begin(matchId, levelCombinationId, startMs);

        RecordOrQueue("match_started", new Dictionary<string, object>
        {
            ["match_id"] = matchId,
            ["time_stamp"] = startMs,
            ["level_combination_id"] = levelCombinationId,
            ["dm_player_id"] = string.IsNullOrWhiteSpace(dmPlayerId) ? "unknown_dm" : dmPlayerId,
            ["hero_count"] = Mathf.Max(0, heroCount),
            ["is_online_match"] = isOnlineMatch,
        }, dedupeKey: $"match_started:{matchId}");
    }

    public void TrackMatchEnded(string winner, bool isSuccessful, int heroesAliveAtEnd, int goblinsAliveAtEnd)
    {
        if (!ShouldEmitAuthorityOnly()) return;
        if (!Session.IsActive || Session.IsFlushed || string.IsNullOrWhiteSpace(Session.MatchId)) return;

        long endMs = NowUnixMs;
        int durationSeconds = Mathf.Max(0, (int)((endMs - Session.StartUnixMs) / 1000));

        RecordOrQueue("match_ended", new Dictionary<string, object>
        {
            ["match_id"] = Session.MatchId,
            ["start_time"] = Session.StartUnixMs,
            ["end_time"] = endMs,
            ["duration_seconds"] = durationSeconds,
            ["winner"] = winner,
            ["is_successful"] = isSuccessful,
            ["level_combination_id"] = Session.LevelCombinationId,
            ["heroes_alive_at_end"] = Mathf.Max(0, heroesAliveAtEnd),
            ["goblins_alive_at_end"] = Mathf.Max(0, goblinsAliveAtEnd),
        }, dedupeKey: $"match_ended:{Session.MatchId}");

        FlushMatchSummaries(runResult: isSuccessful ? "success" : "failure", endUnixMs: endMs);
        Session.MarkFlushed();
    }

    public void TrackPlayerDied(string playerId, string playerClass, string killerType, string killerId)
    {
        if (!ShouldEmitAuthorityOnly()) return;
        if (!Session.IsActive || string.IsNullOrWhiteSpace(Session.MatchId) || string.IsNullOrWhiteSpace(playerId)) return;

        long deathMs = NowUnixMs;
        var player = Session.UpsertPlayer(playerId, playerClass, Session.StartUnixMs);
        Session.MarkPlayerDeath(playerId, deathMs);

        RecordOrQueue("player_died", new Dictionary<string, object>
        {
            ["match_id"] = Session.MatchId,
            ["player_id"] = playerId,
            ["player_class"] = player.PlayerClass,
            ["spawn_time"] = player.SpawnUnixMs,
            ["death_time"] = deathMs,
            ["time_alive"] = player.ComputeTimeAliveSeconds(deathMs),
            ["killer_type"] = string.IsNullOrWhiteSpace(killerType) ? "unknown" : killerType,
            ["killer_id"] = string.IsNullOrWhiteSpace(killerId) ? "unknown" : killerId,
        }, dedupeKey: $"player_died:{Session.MatchId}:{playerId}");
    }

    public void TrackPlayerStats(string playerId, string runResult, long endUnixMs)
    {
        if (!ShouldEmitAuthorityOnly()) return;
        if (!Session.Players.TryGetValue(playerId, out var player)) return;

        RecordOrQueue("player_stats", new Dictionary<string, object>
        {
            ["match_id"] = Session.MatchId,
            ["player_id"] = player.PlayerId,
            ["player_class"] = player.PlayerClass,
            ["damage_done"] = player.DamageDone,
            ["kills"] = player.Kills,
            ["time_alive"] = player.ComputeTimeAliveSeconds(endUnixMs),
            ["spawn_time"] = player.SpawnUnixMs,
            ["death_time"] = player.DeathUnixMs ?? endUnixMs,
            ["run_result"] = runResult,
        }, dedupeKey: $"player_stats:{Session.MatchId}:{player.PlayerId}");
    }

    public void TrackAbilityUsed(string playerId, string playerClass, string abilityName, string targetType, int? turnNumber)
    {
        // Player action events can be authority-only to avoid N clients sending same action.
        if (!ShouldEmitAuthorityOnly()) return;
        if (!Session.IsActive || string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(abilityName)) return;

        Session.UpsertPlayer(playerId, playerClass, Session.StartUnixMs);
        Session.AddAbilityUse(playerId, abilityName);

        var payload = new Dictionary<string, object>
        {
            ["match_id"] = Session.MatchId,
            ["player_id"] = playerId,
            ["player_class"] = string.IsNullOrWhiteSpace(playerClass) ? "Unknown" : playerClass,
            ["ability_name"] = abilityName,
            ["time_stamp"] = NowUnixMs,
        };

        if (!string.IsNullOrWhiteSpace(targetType)) payload["target_type"] = targetType;
        if (turnNumber.HasValue) payload["turn_number"] = turnNumber.Value;

        RecordOrQueue("player_used_ability", payload);
    }

    public void TrackGoblinDied(string goblinId, string goblinType, string killerPlayerId, string killerClass)
    {
        if (!ShouldEmitAuthorityOnly()) return;
        if (!Session.IsActive || string.IsNullOrWhiteSpace(goblinId)) return;

        long deathMs = NowUnixMs;
        var goblin = Session.UpsertGoblin(goblinId, goblinType, Session.StartUnixMs);
        Session.MarkGoblinDeath(goblinId, deathMs, killerClass);

        RecordOrQueue("goblin_died", new Dictionary<string, object>
        {
            ["match_id"] = Session.MatchId,
            ["goblin_id"] = goblin.GoblinId,
            ["goblin_type"] = goblin.GoblinType,
            ["spawn_time"] = goblin.SpawnUnixMs,
            ["death_time"] = deathMs,
            ["time_alive"] = goblin.ComputeTimeAliveSeconds(deathMs),
            ["killer_player_id"] = string.IsNullOrWhiteSpace(killerPlayerId) ? "unknown" : killerPlayerId,
            ["killer_class"] = string.IsNullOrWhiteSpace(killerClass) ? "unknown" : killerClass,
        }, dedupeKey: $"goblin_died:{Session.MatchId}:{goblinId}");
    }

    public void TrackGoblinStats(string goblinId, long endUnixMs)
    {
        if (!ShouldEmitAuthorityOnly()) return;
        if (!Session.Goblins.TryGetValue(goblinId, out var goblin)) return;

        RecordOrQueue("goblin_stats", new Dictionary<string, object>
        {
            ["match_id"] = Session.MatchId,
            ["goblin_id"] = goblin.GoblinId,
            ["goblin_type"] = goblin.GoblinType,
            ["damage_dealt"] = goblin.DamageDealt,
            ["time_alive"] = goblin.ComputeTimeAliveSeconds(endUnixMs),
            ["killer_class"] = string.IsNullOrWhiteSpace(goblin.KillerClass) ? "unknown" : goblin.KillerClass,
        }, dedupeKey: $"goblin_stats:{Session.MatchId}:{goblin.GoblinId}");
    }

    public void TrackDMUnitSelection(string goblinType, int quantity)
    {
        if (!ShouldEmitAuthorityOnly()) return;
        if (!Session.IsActive || string.IsNullOrWhiteSpace(goblinType)) return;

        RecordOrQueue("dm_unit_selection", new Dictionary<string, object>
        {
            ["match_id"] = Session.MatchId,
            ["goblin_type"] = goblinType,
            ["quantity"] = Mathf.Max(1, quantity),
            ["time_stamp"] = NowUnixMs,
        });
    }

    public void TrackItemPurchased(string playerId, string playerClass, string itemId, int quantity)
    {
        if (!ShouldEmitAuthorityOnly()) return;
        if (!Session.IsActive || string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(itemId)) return;

        RecordOrQueue("item_purchased", new Dictionary<string, object>
        {
            ["match_id"] = Session.MatchId,
            ["player_id"] = playerId,
            ["player_class"] = string.IsNullOrWhiteSpace(playerClass) ? "Unknown" : playerClass,
            ["item_id"] = itemId,
            ["quantity"] = Mathf.Max(1, quantity),
            ["time_stamp"] = NowUnixMs,
        });
    }

    public void RegisterHeroSpawn(string playerId, string playerClass)
    {
        if (!ShouldEmitAuthorityOnly()) return;
        if (!Session.IsActive || string.IsNullOrWhiteSpace(playerId)) return;

        Session.UpsertPlayer(playerId, playerClass, NowUnixMs);
    }

    public void RegisterGoblinSpawn(string goblinId, string goblinType)
    {
        if (!ShouldEmitAuthorityOnly()) return;
        if (!Session.IsActive || string.IsNullOrWhiteSpace(goblinId)) return;

        Session.UpsertGoblin(goblinId, goblinType, NowUnixMs);
    }

    public void RegisterHeroDamageDone(string playerId, int amount)
    {
        if (!ShouldEmitAuthorityOnly()) return;
        Session.AddPlayerDamage(playerId, amount);
    }

    public void RegisterHeroKill(string playerId)
    {
        if (!ShouldEmitAuthorityOnly()) return;
        Session.AddPlayerKill(playerId);
    }

    public void RegisterGoblinDamageDealt(string goblinId, int amount)
    {
        if (!ShouldEmitAuthorityOnly()) return;
        Session.AddGoblinDamage(goblinId, amount);
    }

    private void FlushMatchSummaries(string runResult, long endUnixMs)
    {
        foreach (var player in Session.Players.Values)
        {
            TrackPlayerStats(player.PlayerId, runResult, endUnixMs);
        }

        foreach (var goblin in Session.Goblins.Values)
        {
            TrackGoblinStats(goblin.GoblinId, endUnixMs);
        }

        if (IsBootstrapReady)
        {
            AnalyticsService.Instance.Flush();
        }
    }
}
