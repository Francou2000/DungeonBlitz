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

    UnitLoaderController _unitControler;

    public static System.Action<int[], bool[]> OnHeroReadySnapshot;

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
        _unitControler = UnitLoaderController.Instance;
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

            // push initial snapshot so lights paint correctly on all clients
            BroadcastHeroReady();

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
        if (!ChangeLevel) return;

        if (timePool[currentTurn] <= 0f)
        {
            Debug.Log($"[TurnManager] Time's up! {GetOpposingFaction(currentTurn)} wins by timeout");
            if (currentTurn == UnitFaction.Hero)
            {
                EndGame(GetOpposingFaction(currentTurn));
            }
            else
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    ChangeLevel = false;
                    _unitControler.lvl += 1;
                    photonView.RPC(nameof(NextLevel), RpcTarget.All, _unitControler.lvl);
                    Debug.Log($"[TurnManager] Time is up! Advancing levels.");
                }
            }

        }
        else if (IsFactionDefeated(GetOpposingFaction(currentTurn)))
        {
            Debug.Log($"[TurnManager] Faction defeated!");
            if (_unitControler.lvl == 3)
            {
                EndGame(currentTurn); // current faction wins
                Debug.Log($"[TurnManager] Faction defeated! {currentTurn} wins by elimination");

            }
            else
            {
                if (currentTurn == UnitFaction.Monster)
                {
                    EndGame(currentTurn);
                    Debug.Log($"[TurnManager] Faction defeated! DM wins by elimination");
                }
                else
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        ChangeLevel = false;
                        _unitControler.lvl += 1;
                        photonView.RPC(nameof(NextLevel), RpcTarget.All, _unitControler.lvl);
                        Debug.Log($"[TurnManager] Faction defeated! Advancing levels.");
                    }
                }
            }

        }
    }

    // === PUBLIC ===

    [PunRPC]
    public void NextLevel(int lvl)
    {
        _unitControler.lvl = lvl;
        _unitControler.dm_remaining_time = timePool[UnitFaction.Monster];
        _unitControler.heroes_remaining_time = timePool[UnitFaction.Hero];
        SceneLoaderController.Instance.LoadNextLevel(Scenes.UnitsSelection);
    }

    bool ChangeLevel = true;

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
        return unit != null && unit.Model != null && unit.Model.Faction == currentTurn;
    }

    // === RPCs ===

    [PunRPC]
    private void RPC_PlayerEndedTurn(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (currentTurn != UnitFaction.Hero || !heroPlayerReady.ContainsKey(actorNumber)) return;

        heroPlayerReady[actorNumber] = true;
        Debug.Log($"[TurnManager] Player {actorNumber} is ready.");

        // update everyone’s lights right away
        BroadcastHeroReady();

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

        // (Master only): ensure a clean snapshot at turn start
        if (PhotonNetwork.IsMasterClient)
            BroadcastHeroReady();

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

        AudioManager.Instance.PlaySFX(SoundName.NextTurn);

    }

    private void AdvanceTurn()
    {
        // End-of-turn decay for the faction that is finishing its turn
        ResolveEndOfTurnForFaction(currentTurn);

        currentTurn = GetOpposingFaction(currentTurn);
        turnNumber++;
        turnTimer = 0f;

        Debug.Log($"[TurnManager] Advancing to Turn {turnNumber}, Faction: {currentTurn}");

        if (currentTurn == UnitFaction.Hero)
        {
            var keys = new List<int>(heroPlayerReady.Keys);
            foreach (var key in keys)
                heroPlayerReady[key] = false;

            // broadcast fresh snapshot (all false) at the start of Heroes' turn
            BroadcastHeroReady();
        }

        AudioManager.Instance.PlaySFX(SoundName.NextTurn);

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

    // === PHOTON CALLBACKS ===
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[TurnManager] Player {otherPlayer.ActorNumber} left the room");

        // Remove from hero ready tracking
        if (heroPlayerReady.ContainsKey(otherPlayer.ActorNumber))
        {
            heroPlayerReady.Remove(otherPlayer.ActorNumber);

            // rebroadcast to drop the light for the departed player
            if (PhotonNetwork.IsMasterClient)
                BroadcastHeroReady();
        }

        // Check if master client left
        if (otherPlayer.IsMasterClient)
        {
            Debug.Log($"[TurnManager] Master client left! Returning to main menu.");

            // Guardar la causa de desconexión
            EnsureDisconnectInfoManager();
            if (DisconnectInfoManager.Instance != null)
            {
                DisconnectInfoManager.Instance.SetDisconnectReason(DisconnectReason.DMDisconnected);
            }

            // Ir directamente al main menu en lugar de la pantalla de victoria
            ReturnToMainMenu();
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

        if (remainingHeroes <= 2)
        {
            Debug.Log($"[TurnManager] Only {remainingHeroes} heroes remaining! Returning to main menu.");

            // Guardar la causa de desconexión
            EnsureDisconnectInfoManager();
            if (DisconnectInfoManager.Instance != null)
            {
                DisconnectInfoManager.Instance.SetDisconnectReason(
                    DisconnectReason.TooManyPlayersDisconnected,
                    $"Solo quedan {remainingHeroes} jugador(es). Se requieren al menos 3 jugadores para continuar."
                );
            }

            // Ir directamente al main menu en lugar de la pantalla de victoria
            ReturnToMainMenu();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[TurnManager] Master client switched to {newMasterClient.ActorNumber}");

        // Guardar la causa de desconexión (el DM original se desconectó)
        EnsureDisconnectInfoManager();
        if (DisconnectInfoManager.Instance != null)
        {
            DisconnectInfoManager.Instance.SetDisconnectReason(DisconnectReason.DMDisconnected);
        }

        // Ir directamente al main menu en lugar de la pantalla de victoria
        ReturnToMainMenu();
    }

    // Método helper para asegurar que DisconnectInfoManager existe
    private void EnsureDisconnectInfoManager()
    {
        if (DisconnectInfoManager.Instance == null)
        {
            GameObject managerObj = new GameObject("DisconnectInfoManager");
            managerObj.AddComponent<DisconnectInfoManager>();
        }
    }

    // Método para volver al main menu cuando hay una desconexión
    private void ReturnToMainMenu()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        // Cargar el main menu
        SceneLoaderController.Instance.LoadNextLevel(Scenes.MainMenu);
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

    private void BroadcastHeroReady()
    {
        // Build a stable, sorted list of hero actors (exclude master/DM)
        var heroes = new System.Collections.Generic.List<int>();
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber != PhotonNetwork.MasterClient.ActorNumber)
                heroes.Add(p.ActorNumber);
        }
        heroes.Sort();

        int n = heroes.Count;
        var actors = new int[n];
        var states = new bool[n];
        for (int i = 0; i < n; i++)
        {
            int actor = heroes[i];
            actors[i] = actor;
            states[i] = heroPlayerReady.TryGetValue(actor, out bool rdy) && rdy;
        }

        // Local event for this client’s UI
        OnHeroReadySnapshot?.Invoke(actors, states);

        // Network broadcast so everyone updates
        photonView.RPC(nameof(RPC_SyncHeroReady), RpcTarget.Others, actors, states);
    }

    [PunRPC]
    private void RPC_SyncHeroReady(int[] actors, bool[] states)
    {
        // Defensive mirror (keeps local dict in sync even on non-master)
        if (actors != null && states != null && actors.Length == states.Length)
        {
            for (int i = 0; i < actors.Length; i++)
                heroPlayerReady[actors[i]] = states[i];
        }
        OnHeroReadySnapshot?.Invoke(actors, states);
    }

    // Is it the local player's side turn to perform actions?
    public bool IsLocalPlayersTurnForActions()
    {
        // Heroes' turn → any non-master hero can act
        if (currentTurn == UnitFaction.Hero)
            return !PhotonNetwork.IsMasterClient;

        // Monster's turn → only the master/DM acts
        if (currentTurn == UnitFaction.Monster)
            return PhotonNetwork.IsMasterClient;

        return false;
    }

    private void ResolveEndOfTurnForFaction(UnitFaction faction)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (var unit in Object.FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            if (unit.Model != null && unit.Model.Faction == faction)
            {
                unit.GetComponent<StatusComponent>()?.OnTurnEnded();
            }
        }
    }
}