using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TurnManager : MonoBehaviourPun
{
    public static TurnManager Instance { get; private set; }

    [Header("Turn Settings")]
    public float initialTimePool = 300f;

    public UnitFaction currentTurn = UnitFaction.Hero;
    public int turnNumber = 1;

    private Dictionary<UnitFaction, float> timePool = new();
    private Dictionary<int, bool> heroPlayerReady = new();

    private PhotonView view;
    private bool gamePaused = false;

    private float turnTimer = 0f;
    private float syncCooldown = 0f;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("[TurnManager] Duplicate detected. Destroying...");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        view = GetComponent<PhotonView>();
    }

    void Start()
    {
        timePool[UnitFaction.Hero] = initialTimePool;
        timePool[UnitFaction.Monster] = initialTimePool;

        if (PhotonNetwork.IsMasterClient)
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.ActorNumber != PhotonNetwork.MasterClient.ActorNumber)
                {
                    heroPlayerReady[player.ActorNumber] = false;
                }
            }

            DecideFirstTurn(); // Only Master Client decides who goes first
        }

        UpdateTurnUI();
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient || gamePaused) return;

        float delta = Time.deltaTime;
        turnTimer += delta;
        timePool[currentTurn] -= delta;
        timePool[currentTurn] = Mathf.Max(0f, timePool[currentTurn]);

        syncCooldown -= delta;
        if (syncCooldown <= 0f)
        {
            photonView.RPC(nameof(RPC_UpdateTimer), RpcTarget.Others, turnTimer, timePool[currentTurn]);
            syncCooldown = 1f;
        }

        UpdateTurnUI();

        if (timePool[currentTurn] <= 0f)
        {
            EndGame(GetOpposingFaction(currentTurn));
        }
    }

    // === PUBLIC ===

    public bool CanEndTurnNow()
    {
        if (currentTurn == UnitFaction.Hero)
            return !PhotonNetwork.IsMasterClient;
        else if (currentTurn == UnitFaction.Monster)
            return PhotonNetwork.IsMasterClient;

        return false;
    }

    public float GetTimePool(UnitFaction faction)
    {
        return timePool.TryGetValue(faction, out float time) ? time : 0f;
    }

    public void RequestHeroEndTurn()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_PlayerEndedTurn), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    public void RequestMonsterEndTurn()
    {
        if (PhotonNetwork.IsMasterClient && currentTurn == UnitFaction.Monster)
        {
            AdvanceTurn();
        }
    }

    public bool IsCurrentTurn(Unit unit)
    {
        return unit.Model.Faction == currentTurn;
    }

    // === RPCs ===

    [PunRPC]
    private void RPC_PlayerEndedTurn(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (currentTurn != UnitFaction.Hero || !heroPlayerReady.ContainsKey(actorNumber)) return;

        heroPlayerReady[actorNumber] = true;
        Debug.Log($"[TurnManager] Player {actorNumber} is ready.");

        if (AllHeroesReady())
        {
            AdvanceTurn();
        }
    }

    [PunRPC]
    private void RPC_SyncTurn(UnitFaction newTurn, int newTurnNumber, float newTime)
    {
        Debug.Log($"[RPC_SyncTurn] Received Turn {newTurnNumber}, Faction: {newTurn}, Time: {newTime}");

        currentTurn = newTurn;
        turnNumber = newTurnNumber;
        timePool[currentTurn] = newTime;
        turnTimer = 0f;

        UpdateTurnUI();
    }

    [PunRPC]
    private void RPC_UpdateTimer(float syncedTurnTime, float syncedTimePool)
    {
        // If timePool doesn't exist for currentTurn, ignore the update
        if (!timePool.ContainsKey(currentTurn))
        {
            Debug.LogWarning($"[RPC_UpdateTimer] Missing timePool key for: {currentTurn}");
            return;
        }

        turnTimer = syncedTurnTime;
        timePool[currentTurn] = syncedTimePool;

        UpdateTurnUI();
    }

    // === TURN FLOW ===
    private void DecideFirstTurn()
    {
        UnitFaction starting = Random.value < 0.5f ? UnitFaction.Hero : UnitFaction.Monster;
        float timeLeft = timePool[starting];

        Debug.Log($"[TurnManager] Coin flip! {starting} will start.");

        photonView.RPC(nameof(RPC_SyncTurn), RpcTarget.All, starting, turnNumber, timeLeft);
    }

    private void AdvanceTurn()
    {
        currentTurn = GetOpposingFaction(currentTurn);
        turnNumber++;
        turnTimer = 0f;

        Debug.Log($"[TurnManager] Advancing to Turn {turnNumber}, Faction: {currentTurn}");

        if (currentTurn == UnitFaction.Hero)
        {
            var keys = new List<int>(heroPlayerReady.Keys);
            foreach (var key in keys)
                heroPlayerReady[key] = false;
        }

        ResetUnitsForFaction(currentTurn);

        float timeLeft = timePool[currentTurn];
        photonView.RPC(nameof(RPC_SyncTurn), RpcTarget.All, currentTurn, turnNumber, timeLeft);
    }

    private void ResetUnitsForFaction(UnitFaction faction)
    {
        foreach (var unit in Object.FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            if (unit.Model.Faction == faction)
                unit.Model.ResetTurn();
        }
    }

    private bool AllHeroesReady()
    {
        foreach (var ready in heroPlayerReady.Values)
        {
            if (!ready) return false;
        }
        return true;
    }

    private UnitFaction GetOpposingFaction(UnitFaction faction)
    {
        return faction == UnitFaction.Hero ? UnitFaction.Monster : UnitFaction.Hero;
    }

    private void EndGame(UnitFaction winner)
    {
        gamePaused = true;
        Debug.Log($"[TurnManager] {winner} wins! (Time Out)");
        // Show UI message, kick to lobby, etc.
    }

    // === UI ===

    private void UpdateTurnUI()
    {
        float remaining = timePool.ContainsKey(currentTurn) ? timePool[currentTurn] : 0f;
        TurnUIController.Instance.UpdateTurnUI(turnNumber, currentTurn, remaining);
    }
}