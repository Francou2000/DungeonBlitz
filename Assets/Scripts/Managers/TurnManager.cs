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

    [SerializeField] private bool startPaused = true;
    private bool gamePaused;

    private bool[] heroesInstantiated;
    private int expectedHeroes = 0;

    private float postSetupGrace = 1f;  // seconds to wait before elimination checks

    private float turnTimer = 0f;
    private float syncCooldown = 0f;

    public float RemainingTime => GetTimePool(currentTurn);

    public static System.Action<int, UnitFaction, float> OnTurnUI;

    public static System.Action<UnitController> OnActiveControllerChanged;
    public static event System.Action<UnitFaction> OnTurnBegan;


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

        gamePaused = startPaused;
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

        StartCoroutine(DeferredUnpauseFallback());

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

        if (postSetupGrace > 0f) { postSetupGrace -= Time.deltaTime; return; }

        // === CHECK WIN CONDITION ===
        if (timePool[currentTurn] <= 0f)
        {
            Debug.Log($"[TurnManager] Time's up! {GetOpposingFaction(currentTurn)} wins by timeout");
            EndGame(GetOpposingFaction(currentTurn));
        }
        else if (IsFactionDefeated(GetOpposingFaction(currentTurn)))
        {
            Debug.Log($"[TurnManager] Faction defeated! {currentTurn} wins by elimination");
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

        ResetUnitsForFaction(currentTurn);

        OnTurnBegan?.Invoke(currentTurn);

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
        Debug.Log($"[TurnManager] RPC_HeroeGotInstanciated on Actor={PhotonNetwork.LocalPlayer.ActorNumber} idx={idx}");

        // Defensive init in case the spawner didn't set expected count yet
        if (heroesInstantiated == null || heroesInstantiated.Length == 0)
        {
            int inferred = Mathf.Max(idx + 1, 1);
            expectedHeroes = inferred;
            heroesInstantiated = new bool[expectedHeroes];
            Debug.LogWarning($"[TurnManager] Expected hero count was not set. Inferring {expectedHeroes} from first index.");
        }

        if (idx >= 0 && idx < heroesInstantiated.Length)
            heroesInstantiated[idx] = true;

        // Check if all expected heroes are ready
        for (int i = 0; i < heroesInstantiated.Length; i++)
            if (!heroesInstantiated[i]) return;

        gamePaused = false;
        syncCooldown = 0f; // force an immediate UI/time sync tick
        UpdateTurnUI();
        Debug.Log($"[TurnManager] All heroes instantiated – game unpaused.");
        TryUnpauseIfReady($"Hero {idx} ready");
    }

    [PunRPC]
    public void RPC_SetExpectedHeroes(int count)
    {
        expectedHeroes = Mathf.Max(0, count);
        heroesInstantiated = new bool[expectedHeroes];
        Debug.Log($"[TurnManager] Expecting {expectedHeroes} heroes.");
        TryUnpauseIfReady("SetExpectedHeroes");
    }


    // === TURN FLOW ===
    private void DecideFirstTurn()
    {
        UnitFaction starting = Random.value < 0.5f ? UnitFaction.Hero : UnitFaction.Monster;
        //UnitFaction starting = UnitFaction.Hero; // For testing purposes, Heroes always start first
        //UnitFaction starting = UnitFaction.Monster; // For testing purposes, Monster always start first
        float timeLeft = timePool[starting];

        Debug.Log($"[TurnManager] Coin flip! {starting} will start.");

        photonView.RPC(nameof(RPC_SyncTurn), RpcTarget.All, starting, turnNumber, timeLeft);
        OnTurnBegan?.Invoke(starting);
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
        OnTurnBegan?.Invoke(currentTurn);

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
        // Primero, marcar automáticamente como listos a los jugadores con unidades muertas
        MarkDeadPlayersAsReady();
        
        // Verificar que todos los jugadores estén listos
        foreach (var ready in heroPlayerReady.Values)
        {
            if (!ready) return false;
        }
        return true;
    }

    private void MarkDeadPlayersAsReady()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Obtener todas las unidades hero vivas en la escena
        var allUnits = UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        
        // Crear un conjunto con los actor numbers de jugadores que tienen unidades vivas
        HashSet<int> playersWithAliveUnits = new HashSet<int>();
        
        foreach (var unit in allUnits)
        {
            if (unit.Faction != UnitFaction.Hero)
                continue;
            
            // Obtener el PhotonView de la unidad
            PhotonView pv = unit.GetComponent<PhotonView>();
            if (pv == null) continue;
            
            // Obtener el actor number del dueño de esta unidad
            int ownerActorNumber = pv.OwnerActorNr != 0 ? pv.OwnerActorNr : pv.ControllerActorNr;
            
            // Marcar que este jugador tiene una unidad viva
            playersWithAliveUnits.Add(ownerActorNumber);
        }
        
        // Marcar como listos automáticamente a los jugadores hero que no tienen unidades vivas
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber == PhotonNetwork.MasterClient.ActorNumber)
                continue;
            
            // Si el jugador no tiene unidad viva y está en el diccionario
            if (!playersWithAliveUnits.Contains(player.ActorNumber) && heroPlayerReady.ContainsKey(player.ActorNumber))
            {
                if (!heroPlayerReady[player.ActorNumber])
                {
                    Debug.Log($"[TurnManager] Marcando player {player.ActorNumber} como listo automáticamente (unidad muerta).");
                    heroPlayerReady[player.ActorNumber] = true;
                }
            }
        }
    }

    private UnitFaction GetOpposingFaction(UnitFaction faction)
    {
        return faction == UnitFaction.Hero ? UnitFaction.Monster : UnitFaction.Hero;
    }

    private bool IsFactionDefeated(UnitFaction faction)
    {
        var allUnits = UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        int aliveUnits = 0;
        
        foreach (var unit in allUnits)
        {
            if (unit.Model.Faction == faction && unit.Model.IsAlive())
            {
                aliveUnits++;
            }
        }
        
        Debug.Log($"[TurnManager] Faction {faction} has {aliveUnits} alive units");
        return aliveUnits == 0;
    }

    private void EndGame(UnitFaction winner)
    {
        //if (gamePaused) return;
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

    // === PHOTON CALLBACKS ===
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[TurnManager] Player {otherPlayer.ActorNumber} left the room");
        
        // Remove from hero ready tracking
        if (heroPlayerReady.ContainsKey(otherPlayer.ActorNumber))
        {
            heroPlayerReady.Remove(otherPlayer.ActorNumber);
        }
        
        // Check if master client left
        if (otherPlayer.IsMasterClient)
        {
            Debug.Log($"[TurnManager] Master client left! Heroes win by default");
            EndGame(UnitFaction.Hero);
            return;
        }
        
        // Check if too many heroes left
        int remainingHeroes = 0;
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber != PhotonNetwork.MasterClient.ActorNumber)
            {
                remainingHeroes++;
            }
        }
        
        if (remainingHeroes < 2)
        {
            Debug.Log($"[TurnManager] Only {remainingHeroes} heroes remaining! DM wins by default");
            EndGame(UnitFaction.Monster);
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[TurnManager] Master client switched to {newMasterClient.ActorNumber}");
        
        // If we become the new master client, take over the game state
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[TurnManager] We are now the master client");
        }
    }

    private void TryUnpauseIfReady(string reason = "")
    {
        if (!Photon.Pun.PhotonNetwork.IsMasterClient) return;

        // If there’s no hero gating, or all expected heroes are ready, unpause
        bool allHeroesReady = (expectedHeroes == 0 && heroesInstantiated == null)
                              || (heroesInstantiated != null && System.Array.TrueForAll(heroesInstantiated, x => x));

        if (allHeroesReady && gamePaused)
        {
            gamePaused = false;
            syncCooldown = 0f;
            UpdateTurnUI();
            Debug.Log($"[TurnManager] Unpaused ({reason}).");
        }
    }

    private System.Collections.IEnumerator DeferredUnpauseFallback()
    {
        yield return null;
        TryUnpauseIfReady("Start fallback");
    }
}