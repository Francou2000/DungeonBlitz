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
        }

        UpdateTurnUI();
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient || gamePaused) return;

        timePool[currentTurn] -= Time.deltaTime;
        timePool[currentTurn] = Mathf.Max(0f, timePool[currentTurn]);

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
        currentTurn = newTurn;
        turnNumber = newTurnNumber;
        timePool[currentTurn] = newTime;

        UpdateTurnUI();
    }

    // === TURN FLOW ===

    private void AdvanceTurn()
    {
        currentTurn = GetOpposingFaction(currentTurn);
        turnNumber++;

        if (currentTurn == UnitFaction.Hero)
        {
            foreach (var key in heroPlayerReady.Keys)
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