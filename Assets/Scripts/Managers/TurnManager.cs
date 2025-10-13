using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TurnManager : MonoBehaviourPunCallbacks
{
    public static TurnManager Instance { get; private set; }

    [Header("Turn Settings")]
    public float initialTimePool = 300f;

    public UnitFaction currentTurn = UnitFaction.Hero;
    public int turnNumber = 1;

    private Dictionary<UnitFaction, float> timePool = new();
    private Dictionary<int, bool> heroPlayerReady = new();

    private PhotonView view;
    public bool gamePaused = true;
    public bool[] has_been_instanciated = new bool[4];

    private float turnTimer = 0f;
    private float syncCooldown = 0f;

    public float RemainingTime => GetTimePool(currentTurn);

    public static System.Action<int, UnitFaction, float> OnTurnUI;

    public static System.Action<UnitController> OnActiveControllerChanged;


    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("[TurnManager] Duplicate detected. Destroying...");
            Destroy(gameObject);
            return;
        }

        Debug.Log($"[TurnManager][Awake] Instance set on Actor={PhotonNetwork.LocalPlayer.ActorNumber} IsMaster={PhotonNetwork.IsMasterClient} View={GetComponent<PhotonView>()?.ViewID}");
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
        if (!PhotonNetwork.IsMasterClient || gamePaused)
            return;
        
        float delta = Time.deltaTime;
        turnTimer += delta;
        timePool[currentTurn] -= delta;
        timePool[currentTurn] = Mathf.Max(0f, timePool[currentTurn]);

        syncCooldown -= delta;
        if (syncCooldown <= 0f)
        {
            photonView.RPC(nameof(RPC_UpdateTimer), RpcTarget.Others, turnTimer, timePool[currentTurn]);
            UpdateTurnUI();
            syncCooldown = 1f;        
        }


        // === CHECK WIN CONDITION ===
        if (timePool[currentTurn] <= 0f)
        {
            EndGame(GetOpposingFaction(currentTurn));
        }
        else if (IsFactionDefeated(GetOpposingFaction(currentTurn)))
        {
            EndGame(currentTurn); // current faction wins
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

    // Single entry point for the UI: routes to the proper request based on role/turn
    public void RequestEndTurn()
    {
        if (currentTurn == UnitFaction.Hero)
            RequestHeroEndTurn();
        else if (currentTurn == UnitFaction.Monster)
            RequestMonsterEndTurn();
    }

    public void RequestHeroEndTurn()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_PlayerEndedTurn), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            Debug.Log($"[TurnManager] RequestHeroEndTurn sent by {PhotonNetwork.LocalPlayer.ActorNumber}");
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

    // include sender info to know who invoked the RPC
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
    private void RPC_UpdateTimer(float syncedTurnTime, float syncedTimePool, PhotonMessageInfo info)
    {
        // If the client doesn't have the timePool key for currentTurn, log it
        if (!timePool.ContainsKey(currentTurn))
        {
            // Try to create missing keys (defensive)
            if (!timePool.ContainsKey(UnitFaction.Hero)) timePool[UnitFaction.Hero] = initialTimePool;
            if (!timePool.ContainsKey(UnitFaction.Monster)) timePool[UnitFaction.Monster] = initialTimePool;
        }

        turnTimer = syncedTurnTime;
        timePool[currentTurn] = syncedTimePool;

        Debug.Log($"[RPC_UpdateTimer] On Actor={PhotonNetwork.LocalPlayer.ActorNumber} IsMaster={PhotonNetwork.IsMasterClient} Sender={info.Sender.ActorNumber}");

        UpdateTurnUI();
    }

    [PunRPC]
    private void RPC_LoadEndScene(string sceneName)
    {
        PhotonNetwork.LoadLevel(sceneName);
    }

    [PunRPC]
    public void RPC_HeroeGotInstanciated(int idx)
    {
        Debug.Log($"[TurnManager] RPC_HeroeGotInstanciated CALLED on Actor={PhotonNetwork.LocalPlayer.ActorNumber} idx={idx}");

        has_been_instanciated[idx] = true;

        foreach (bool check in has_been_instanciated)
        {
            if (!check) return; // Still waiting for some heroes
        }

        // All heroes have been instantiated — unpause the game
        gamePaused = false;
        Debug.Log($"[TurnManager] All heroes instantiated — game unpaused on Actor={PhotonNetwork.LocalPlayer.ActorNumber}");
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
        // Notify end-of-turn for ALL units of the faction that is ending now
        foreach (var unit in UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            if (unit.Model.Faction == currentTurn && unit.Model.statusHandler != null)
            {
                unit.Model.statusHandler.OnEndTurn(unit);
            }
        }

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

    private bool IsFactionDefeated(UnitFaction faction)
    {
        foreach (var unit in UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            if (unit.Model.Faction == faction && unit.Model.IsAlive())
            {
                return false;
            }
        }
        return true;
    }

    private void EndGame(UnitFaction winner)
    {
        if (gamePaused) return;
        gamePaused = true;

        Debug.Log($"[TurnManager] {winner} wins!");

        // MasterClient tells everyone to load the win scene
        if (PhotonNetwork.IsMasterClient)
        {
            string sceneName = (winner == UnitFaction.Hero) ? "Heroes_WinScreen" : "DM_WinScreen";
            photonView.RPC(nameof(RPC_LoadEndScene), RpcTarget.All, sceneName);
        }
    }

    // === UI ===

    void UpdateTurnUI()
    {
        float remaining = timePool.ContainsKey(currentTurn) ? timePool[currentTurn] : 0f;
        OnTurnUI?.Invoke(turnNumber, currentTurn, remaining);
    }

    /* Old UI System
    private void UpdateTurnUI()
    {
        float remaining = timePool.ContainsKey(currentTurn) ? timePool[currentTurn] : 0f;
        TurnUIController.Instance.UpdateTurnUI(turnNumber, currentTurn, remaining);
    }
    */
}