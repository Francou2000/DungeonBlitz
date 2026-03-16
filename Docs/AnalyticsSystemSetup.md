# DungeonBlitz Analytics System: How It Works + Editor Setup

This document explains the analytics foundation added in this branch and exactly how to configure it in Unity Editor.

## 1) Architecture overview

The system is split into four layers:

1. **`AnalyticsBootstrap`**  
   Initializes Unity Services + Analytics once at startup, tracks readiness, and avoids crashes if initialization fails.

2. **`GameAnalyticsService`**  
   Central API for all analytics events. It validates payloads, applies multiplayer authority rules, deduplicates one-shot events, and queues events until analytics is ready.

3. **`MatchAnalyticsSession`**  
   In-memory per-match aggregator that stores player/goblin runtime stats and flushes summary events at match end.

4. **`AnalyticsGameplayAdapter`**  
   Gameplay-facing hook layer (`OnMatchStarted`, `OnHeroDied`, `OnItemPurchased`, etc.) so gameplay code never talks to UGS SDK directly.

This keeps analytics modular, easy to expand, and decoupled from UI.

---

## 2) Runtime flow (what happens during play)

### A) Startup
- `AnalyticsBootstrap` auto-creates itself with `RuntimeInitializeOnLoadMethod`.
- It calls `UnityServices.InitializeAsync()` and starts data collection.
- If initialization fails, the game continues safely; analytics events can be queued until ready.

### B) Match start
- The host/MasterClient calls `OnMatchStarted` through the adapter.
- `GameAnalyticsService.TrackMatchStarted(...)` stores a new `MatchAnalyticsSession` and sends `match_started`.

### C) During match
Gameplay hooks feed the adapter:
- Hero spawn -> `RegisterHeroSpawn`
- Goblin spawn -> `RegisterGoblinSpawn`
- Ability cast -> `player_used_ability`
- Damage events -> accumulate hero/goblin damage in session
- Death events -> `player_died` / `goblin_died`
- DM selection -> `dm_unit_selection`
- Purchases -> `item_purchased`

### D) Match end
- Host/MasterClient calls `OnMatchEnded`.
- Service sends `match_ended`.
- It flushes summary events once per match:
  - one `player_stats` per tracked player
  - one `goblin_stats` per tracked goblin
- Session is marked flushed to prevent duplicates.

---

## 3) Multiplayer duplication strategy

To avoid duplicate submissions from every client:

- `GameAnalyticsService` uses **authority-only emission** when in Photon room:
  - only MasterClient sends analytics (`PhotonNetwork.IsMasterClient`)
- It uses event dedupe keys for one-shot events (`match_started`, `match_ended`, `player_died`, `goblin_died`, summary events).
- Match summary flush is guarded by `Session.IsFlushed`.

This ensures match-level KPIs are emitted once per match.

---

## 4) What is already integrated in this project

The current code already invokes hooks in these systems:

- **Match start / end**: `TurnManager`
- **Hero spawn**: `HeroSpawner`
- **Goblin spawn**: `MonsterSpawner`
- **Ability + damage + death attribution**: `Unit`
- **DM unit selection**: `DC_Manager.ShowUnit`
- **Item purchased**: `HeroesShopManager.PurchaseItem`

---

## 5) Unity Editor setup checklist

## 5.1 Install/resolve packages
1. Open **Window > Package Manager**.
2. Confirm these dependencies are present (from `manifest.json`):
   - `com.unity.services.core`
   - `com.unity.services.analytics`
3. Let Unity finish package import/compilation.

## 5.2 Link Unity project to UGS
1. Open **Edit > Project Settings > Services** (or **Window > General > Services** depending Unity version).
2. Sign in with Unity account.
3. Link/select the correct Unity project.

## 5.3 Enable Analytics service
1. In Services dashboard, enable **Analytics** for this project environment.
2. Use same environment for playtests (dev/staging/prod consistency matters).

## 5.4 Create custom events in Unity Dashboard
Create these exact event names and parameter schemas:

1. `match_started`
   - `match_id` (string)
   - `timestamp` (number)
   - `level_combination_id` (string)
   - `dm_player_id` (string)
   - `hero_count` (number)
   - `is_online_match` (boolean)

2. `match_ended`
   - `match_id` (string)
   - `start_time` (number)
   - `end_time` (number)
   - `duration_seconds` (number)
   - `winner` (string)
   - `is_successful` (boolean)
   - `level_combination_id` (string)
   - `heroes_alive_at_end` (number)
   - `goblins_alive_at_end` (number)

3. `player_died`
   - `match_id` (string)
   - `player_id` (string)
   - `player_class` (string)
   - `spawn_time` (number)
   - `death_time` (number)
   - `time_alive` (number)
   - `killer_type` (string)
   - `killer_id` (string)

4. `player_stats`
   - `match_id` (string)
   - `player_id` (string)
   - `player_class` (string)
   - `damage_done` (number)
   - `kills` (number)
   - `time_alive` (number)
   - `spawn_time` (number)
   - `death_time` (number)
   - `run_result` (string)

5. `player_used_ability`
   - `match_id` (string)
   - `player_id` (string)
   - `player_class` (string)
   - `ability_name` (string)
   - `timestamp` (number)
   - `target_type` (string, optional in analysis)
   - `turn_number` (number, optional in analysis)

6. `goblin_died`
   - `match_id` (string)
   - `goblin_id` (string)
   - `goblin_type` (string)
   - `spawn_time` (number)
   - `death_time` (number)
   - `time_alive` (number)
   - `killer_player_id` (string)
   - `killer_class` (string)

7. `goblin_stats`
   - `match_id` (string)
   - `goblin_id` (string)
   - `goblin_type` (string)
   - `damage_dealt` (number)
   - `time_alive` (number)
   - `killer_class` (string)

8. `dm_unit_selection`
   - `match_id` (string)
   - `goblin_type` (string)
   - `quantity` (number)
   - `timestamp` (number)

9. `item_purchased`
   - `match_id` (string)
   - `player_id` (string)
   - `player_class` (string)
   - `item_id` (string)
   - `quantity` (number)
   - `timestamp` (number)

## 5.5 Scene setup
No manual prefab placement is required because `AnalyticsBootstrap` self-creates before scene load.  
Optional: you may place an `AnalyticsBootstrap` object in a bootstrap scene for visibility, but do not keep duplicate instances.

## 5.6 Play mode verification
1. Enter Play mode as MasterClient/host.
2. Start a match and perform actions:
   - cast abilities
   - buy items
   - kill/lose units
   - finish match
3. Check Console for bootstrap init logs/warnings.
4. In dashboard, validate incoming events and parameters.

---

## 6) Common failure cases and fixes

1. **No events arrive**
   - Confirm project is linked to correct Unity Services project/environment.
   - Confirm Analytics is enabled in dashboard.
   - Confirm host path is actually executing (MasterClient).

2. **Compile errors for Unity.Services namespaces**
   - Re-open Package Manager and ensure `com.unity.services.core` + `com.unity.services.analytics` are installed.

3. **Duplicate events**
   - Verify only authority side emits match-level events.
   - Check if additional systems call adapter hooks redundantly.

4. **Missing fields in dashboard**
   - Ensure exact parameter names and expected types in event schema.

---

## 7) KPI mapping (quick reference)

- **DM vs Heroes wins** -> `match_ended.winner`
- **Average match duration** -> `match_ended.duration_seconds`
- **Match success/failure** -> `match_ended.is_successful`, `player_stats.run_result`
- **Level combination selected** -> `match_started.level_combination_id`
- **Hero time alive** -> `player_died.time_alive`, `player_stats.time_alive`
- **Kills per hero** -> `player_stats.kills`
- **Damage per hero** -> `player_stats.damage_done`
- **Most-used ability** -> aggregate `player_used_ability.ability_name` per player/class
- **Goblin time alive** -> `goblin_died.time_alive`, `goblin_stats.time_alive`
- **Goblin type/quantity selected** -> `dm_unit_selection.goblin_type`, `dm_unit_selection.quantity`
- **Goblin damage contribution** -> `goblin_stats.damage_dealt`
- **Goblin killer class** -> `goblin_died.killer_class`, `goblin_stats.killer_class`
- **Item purchases by class** -> `item_purchased.player_class`, `item_purchased.item_id`, `item_purchased.quantity`

---

## 8) Recommended next integration TODOs

If you add more gameplay systems, call adapter methods there rather than touching UGS SDK directly.

Suggested additions:
- Hook authoritative turn/round counters into `player_used_ability.turn_number` more consistently.
- Add explicit run segmentation tags (party composition, difficulty, map variant).
- Add lightweight debug UI for current analytics readiness + queue size in dev builds.
