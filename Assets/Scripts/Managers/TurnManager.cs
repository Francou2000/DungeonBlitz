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

    private double turnStartTime; // PhotonNetwork.Time is in seconds, synced

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

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

            DecideFirstTurn();
        }
    }

    void Update()
    {
        if (gamePaused) return;

        float elapsed = (float)(PhotonNetwork.Time - turnStartTime);
        float remaining = Mathf.Max(0f, timePool[currentTurn] - elapsed);

        if (PhotonNetwork.IsMasterClient && remaining <= 0f)
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
    private void RPC_SyncTurn(UnitFaction newTurn, int newTurnNumber, float newTime, double startTime)
    {
        currentTurn = newTurn;
        turnNumber = newTurnNumber;
        timePool[currentTurn] = newTime;
        turnStartTime = startTime;

        TurnUIController.Instance.UpdateTurnUI(turnNumber, currentTurn, newTime, timePool);
    }



    // === TURN FLOW ===

    private void DecideFirstTurn()
    {
        UnitFaction starting = Random.value < 0.5f ? UnitFaction.Hero : UnitFaction.Monster;
        Debug.Log($"[TurnManager] Coin flip! {starting} will start.");

        double start = PhotonNetwork.Time;
        photonView.RPC(nameof(RPC_SyncTurn), RpcTarget.All, starting, turnNumber, timePool[starting], start);
    }

    private void AdvanceTurn()
    {
        float elapsed = (float)(PhotonNetwork.Time - turnStartTime);
        timePool[currentTurn] = Mathf.Max(0f, timePool[currentTurn] - elapsed);

        currentTurn = GetOpposingFaction(currentTurn);
        turnNumber++;

        if (currentTurn == UnitFaction.Hero)
        {
            foreach (var key in heroPlayerReady.Keys)
                heroPlayerReady[key] = false;
        }

        ResetUnitsForFaction(currentTurn);

        double start = PhotonNetwork.Time;
        photonView.RPC(nameof(RPC_SyncTurn), RpcTarget.All, currentTurn, turnNumber, timePool[currentTurn], start);
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
    }
}