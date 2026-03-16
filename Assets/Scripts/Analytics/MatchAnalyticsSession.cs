using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// In-memory per-match aggregator. Keeps data normalized so gameplay systems can push tiny updates,
/// then summary events are emitted once when the match ends.
/// </summary>
public sealed class MatchAnalyticsSession
{
    public sealed class PlayerRuntimeStats
    {
        public string PlayerId;
        public string PlayerClass;
        public long SpawnUnixMs;
        public long? DeathUnixMs;
        public int DamageDone;
        public int Kills;
        public readonly Dictionary<string, int> AbilityUsage = new();

        public float ComputeTimeAliveSeconds(long fallbackEndUnixMs)
        {
            var end = DeathUnixMs ?? fallbackEndUnixMs;
            return Math.Max(0f, (end - SpawnUnixMs) / 1000f);
        }
    }

    public sealed class GoblinRuntimeStats
    {
        public string GoblinId;
        public string GoblinType;
        public long SpawnUnixMs;
        public long? DeathUnixMs;
        public int DamageDealt;
        public string KillerClass;

        public float ComputeTimeAliveSeconds(long fallbackEndUnixMs)
        {
            var end = DeathUnixMs ?? fallbackEndUnixMs;
            return Math.Max(0f, (end - SpawnUnixMs) / 1000f);
        }
    }

    public string MatchId { get; private set; }
    public string LevelCombinationId { get; private set; }
    public long StartUnixMs { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFlushed { get; private set; }

    private readonly Dictionary<string, PlayerRuntimeStats> _players = new();
    private readonly Dictionary<string, GoblinRuntimeStats> _goblins = new();

    public IReadOnlyDictionary<string, PlayerRuntimeStats> Players => _players;
    public IReadOnlyDictionary<string, GoblinRuntimeStats> Goblins => _goblins;

    public void Begin(string matchId, string levelCombinationId, long startUnixMs)
    {
        MatchId = matchId;
        LevelCombinationId = levelCombinationId;
        StartUnixMs = startUnixMs;
        IsActive = true;
        IsFlushed = false;

        _players.Clear();
        _goblins.Clear();
    }

    public void Reset()
    {
        MatchId = null;
        LevelCombinationId = null;
        StartUnixMs = 0;
        IsActive = false;
        IsFlushed = false;
        _players.Clear();
        _goblins.Clear();
    }

    public PlayerRuntimeStats UpsertPlayer(string playerId, string playerClass, long spawnUnixMs)
    {
        if (!_players.TryGetValue(playerId, out var runtime))
        {
            runtime = new PlayerRuntimeStats
            {
                PlayerId = playerId,
                PlayerClass = string.IsNullOrWhiteSpace(playerClass) ? "Unknown" : playerClass,
                SpawnUnixMs = spawnUnixMs,
            };
            _players[playerId] = runtime;
        }
        else
        {
            runtime.PlayerClass = string.IsNullOrWhiteSpace(playerClass) ? runtime.PlayerClass : playerClass;
            if (runtime.SpawnUnixMs <= 0)
            {
                runtime.SpawnUnixMs = spawnUnixMs;
            }
        }

        return runtime;
    }

    public GoblinRuntimeStats UpsertGoblin(string goblinId, string goblinType, long spawnUnixMs)
    {
        if (!_goblins.TryGetValue(goblinId, out var runtime))
        {
            runtime = new GoblinRuntimeStats
            {
                GoblinId = goblinId,
                GoblinType = string.IsNullOrWhiteSpace(goblinType) ? "Unknown" : goblinType,
                SpawnUnixMs = spawnUnixMs,
            };
            _goblins[goblinId] = runtime;
        }
        else
        {
            runtime.GoblinType = string.IsNullOrWhiteSpace(goblinType) ? runtime.GoblinType : goblinType;
            if (runtime.SpawnUnixMs <= 0)
            {
                runtime.SpawnUnixMs = spawnUnixMs;
            }
        }

        return runtime;
    }

    public void AddPlayerDamage(string playerId, int amount)
    {
        if (_players.TryGetValue(playerId, out var runtime))
        {
            runtime.DamageDone += Math.Max(0, amount);
        }
    }

    public void AddPlayerKill(string playerId)
    {
        if (_players.TryGetValue(playerId, out var runtime))
        {
            runtime.Kills += 1;
        }
    }

    public void AddAbilityUse(string playerId, string abilityName)
    {
        if (!_players.TryGetValue(playerId, out var runtime) || string.IsNullOrWhiteSpace(abilityName))
        {
            return;
        }

        if (!runtime.AbilityUsage.ContainsKey(abilityName))
        {
            runtime.AbilityUsage[abilityName] = 0;
        }

        runtime.AbilityUsage[abilityName] += 1;
    }

    public void MarkPlayerDeath(string playerId, long deathUnixMs)
    {
        if (_players.TryGetValue(playerId, out var runtime) && runtime.DeathUnixMs == null)
        {
            runtime.DeathUnixMs = deathUnixMs;
        }
    }

    public void AddGoblinDamage(string goblinId, int amount)
    {
        if (_goblins.TryGetValue(goblinId, out var runtime))
        {
            runtime.DamageDealt += Math.Max(0, amount);
        }
    }

    public void MarkGoblinDeath(string goblinId, long deathUnixMs, string killerClass)
    {
        if (_goblins.TryGetValue(goblinId, out var runtime))
        {
            if (runtime.DeathUnixMs == null)
            {
                runtime.DeathUnixMs = deathUnixMs;
            }

            if (!string.IsNullOrWhiteSpace(killerClass))
            {
                runtime.KillerClass = killerClass;
            }
        }
    }

    public string GetMostUsedAbility(string playerId)
    {
        if (!_players.TryGetValue(playerId, out var runtime) || runtime.AbilityUsage.Count == 0)
        {
            return string.Empty;
        }

        return runtime.AbilityUsage.OrderByDescending(kv => kv.Value).First().Key;
    }

    public void MarkFlushed()
    {
        IsFlushed = true;
        IsActive = false;
    }
}
