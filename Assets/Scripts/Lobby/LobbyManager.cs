using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager Instance;

    [SerializeField] Scenes selection_scene;
    //[SerializeField] string heroe_scene;
    [SerializeField] Button ready_button;

    [SerializeField] TextMeshProUGUI lobby_name;

    public string[] slots_used = new string[5];
    [SerializeField] GameObject[] slots_portraits = new GameObject[5];
    
    // Mapear ActorNumber a índice de slot (0-4)
    private Dictionary<int, int> playerSlotMap = new Dictionary<int, int>();

    public GameObject start_game_button;
    [SerializeField] Scenes mainMenuScene = Scenes.MainMenu;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Limpiar todos los slots al iniciar
        playerSlotMap.Clear();
        
        for (int i = 0; i < slots_used.Length; i++)
        {
            slots_used[i] = "";
            player_ready[i] = false;
            
            if (slots_portraits[i] != null)
            {
                var textComponent = slots_portraits[i].GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = "-";
                }
                
                var imageComponent = slots_portraits[i].GetComponent<Image>();
                if (imageComponent != null)
                {
                    imageComponent.color = Color.red;
                }
            }
        }
        
        CheckPlayerName();
        ChangeLobbyName();
        
        if (!PhotonNetwork.IsMasterClient) 
        { 
            if (photonView != null)
            {
                photonView.RPC("GetReadyState", RpcTarget.MasterClient);
            }
        }
        else 
        { 
            if (start_game_button != null)
            {
                start_game_button.SetActive(true);
            }
        }
    }

    public void CheckPlayerName()
    {
        if (slots_portraits[0] == null) return;
        
        TextMeshProUGUI text_slot = slots_portraits[0].GetComponentInChildren<TextMeshProUGUI>();
        
        if (PhotonNetwork.IsMasterClient)
        {
            slots_used[0] = PhotonNetwork.NickName;
            // Asignar el slot 0 al DM
            playerSlotMap[PhotonNetwork.MasterClient.ActorNumber] = 0;
            
            if (text_slot != null)
            {
                text_slot.text = PhotonNetwork.NickName;
            }
        }
        else
        {
            if (PhotonNetwork.MasterClient != null)
            {
                slots_used[0] = PhotonNetwork.MasterClient.NickName;
                // Asignar el slot 0 al DM
                playerSlotMap[PhotonNetwork.MasterClient.ActorNumber] = 0;
                
                if (text_slot != null)
                {
                    text_slot.text = PhotonNetwork.MasterClient.NickName;
                }
            }
            
            int playernumber = PhotonNetwork.LocalPlayer.ActorNumber;
            
            // Validar que photonView esté disponible antes de usar RPCs
            if (photonView == null)
            {
                Debug.LogError("[LobbyManager] photonView is null, cannot sync players");
                return;
            }
            
            // Sincronizar todos los jugadores actuales en la sala
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.ActorNumber > playernumber) continue;
                photonView.RPC("AskForNewCharacter", RpcTarget.MasterClient, player.ActorNumber, player.NickName);
            }
            
            string playername = PhotonNetwork.NickName;
            Debug.Log($"[LobbyManager] Player number: {playernumber}, Player name: {playername}");
            
            photonView.RPC("AskForNewCharacter", RpcTarget.MasterClient, playernumber, playername);
        }
    }

    void ChangeLobbyName()
    {
        string name = PhotonNetwork.CurrentRoom.Name;
        string psw = PhotonNetwork.CurrentRoom.CustomProperties["pwd"].ToString();
        lobby_name.text = name + " (" + psw + ")";
    }
    public void debugButton()
    {
        Debug.Log("player ID  " + PhotonNetwork.LocalPlayer.ActorNumber);
        Debug.Log("player count  " + PhotonNetwork.CountOfPlayers);
        Debug.Log("Others:  ");
        if (PhotonNetwork.CountOfPlayers>1)
        {
            Debug.Log(PhotonNetwork.PlayerList[0].NickName);
            Debug.Log(PhotonNetwork.PlayerList[1].NickName);
        }
        
    }
    
    public bool[] player_ready = new bool[5];
    
    [PunRPC]
    public void ReadyPlayer(int playerID)
    {
        // Buscar el slot asignado a este jugador
        if (!playerSlotMap.ContainsKey(playerID))
        {
            Debug.LogWarning($"[LobbyManager] Player {playerID} not found in slot map. Cannot set ready state.");
            return;
        }
        
        int slotIndex = playerSlotMap[playerID];
        
        if (slotIndex < 0 || slotIndex >= player_ready.Length)
        {
            Debug.LogError($"[LobbyManager] Invalid slotIndex: {slotIndex} for playerID: {playerID}");
            return;
        }
        
        player_ready[slotIndex] = !player_ready[slotIndex];
        
        if (slots_portraits[slotIndex] != null)
        {
            var imageComponent = slots_portraits[slotIndex].GetComponent<Image>();
            if (imageComponent != null)
            {
                imageComponent.color = player_ready[slotIndex] ? Color.green : Color.red;
            }
        }
        
        // Verificar si todos están listos
        foreach (bool is_ready in player_ready)
        {
            if (!is_ready) return;
        }
        
        if (start_game_button != null)
        {
            var buttonComponent = start_game_button.GetComponent<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.interactable = true;
            }
        }
    }
    
    // Método auxiliar para asignar un slot disponible a un jugador
    private int GetAvailableSlot()
    {
        for (int i = 1; i < slots_used.Length; i++) // Slot 0 siempre es para el DM
        {
            if (string.IsNullOrEmpty(slots_used[i]))
            {
                return i;
            }
        }
        return -1; // No hay slots disponibles
    }
    [PunRPC]
    public void PlayerLeaveRoom(int playerID)
    {
        // Buscar el slot del jugador en el mapa
        if (!playerSlotMap.ContainsKey(playerID))
        {
            Debug.LogWarning($"[LobbyManager] Player {playerID} not found in slot map");
            return;
        }
        
        int slotIndex = playerSlotMap[playerID];
        
        if (slotIndex < 0 || slotIndex >= slots_used.Length)
        {
            Debug.LogError($"[LobbyManager] Invalid slotIndex: {slotIndex}");
            return;
        }
        
        // Limpiar el slot
        slots_used[slotIndex] = "";
        player_ready[slotIndex] = false;
        
        // Limpiar el mapeo
        playerSlotMap.Remove(playerID);
        
        if (slots_portraits[slotIndex] != null)
        {
            var textComponent = slots_portraits[slotIndex].GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = "-";
            }
            
            var imageComponent = slots_portraits[slotIndex].GetComponent<Image>();
            if (imageComponent != null)
            {
                imageComponent.color = Color.red;
            }
        }

        Debug.Log($"[LobbyManager] Player {playerID} left from slot {slotIndex}");
    }
    [PunRPC]
    public void AskForNewCharacter(int playerID, string playerNick)
    {
        // Buscar si el jugador ya tiene un slot asignado
        bool isReady = false;
        if (playerSlotMap.ContainsKey(playerID))
        {
            int existingSlot = playerSlotMap[playerID];
            isReady = player_ready[existingSlot];
        }
        
        // Enviar la información al master client para que asigne el slot
        photonView.RPC("AddCharacter", RpcTarget.All, playerID, playerNick, isReady);
    }
    [PunRPC]
    public void AddCharacter(int playerID, string playerNick, bool is_ready)
    {
        int slotIndex;
        
        // Si el jugador ya está en el mapa, usar su slot existente
        if (playerSlotMap.ContainsKey(playerID))
        {
            slotIndex = playerSlotMap[playerID];
            Debug.Log($"[LobbyManager] Player {playerID} already has slot {slotIndex}, updating info");
        }
        else
        {
            // Verificar si es el master client (DM) - siempre va en el slot 0
            if (PhotonNetwork.MasterClient != null && playerID == PhotonNetwork.MasterClient.ActorNumber)
            {
                slotIndex = 0;
                Debug.Log($"[LobbyManager] DM ({playerID}) assigned to slot 0");
            }
            else
            {
                // Buscar un slot disponible para el jugador
                slotIndex = GetAvailableSlot();
                
                if (slotIndex == -1)
                {
                    Debug.LogError($"[LobbyManager] No available slots for player {playerID}");
                    return;
                }
            }
            
            // Asignar el slot al jugador
            playerSlotMap[playerID] = slotIndex;
            Debug.Log($"[LobbyManager] Assigned slot {slotIndex} to player {playerID}");
        }
        
        // Actualizar la información del slot
        slots_used[slotIndex] = playerNick;
        
        if (slots_portraits[slotIndex] != null)
        {
            var textComponent = slots_portraits[slotIndex].GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = playerNick;
            }
            
            var imageComponent = slots_portraits[slotIndex].GetComponent<Image>();
            if (imageComponent != null)
            {
                imageComponent.color = is_ready ? Color.green : Color.red;
            }
        }

        Debug.Log($"[LobbyManager] Added character to slot {slotIndex}: {playerNick}");
    }
    [PunRPC]
    public void GetReadyState()
    {
        photonView.RPC("ShareReadyState", RpcTarget.All, player_ready);
    }
    [PunRPC]
    public void ShareReadyState(bool[] ready_list)
    {
        player_ready = ready_list;
    }
    [PunRPC]
    public void LoadGame()
    {
        // SceneManager.LoadScene(selection_scene);
        AudioManager.Instance.PlayStartGame();
        SceneLoaderController.Instance.LoadNextLevel(selection_scene);
    }

    // Callback cuando un jugador abandona la sala
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"[LobbyManager] Player {otherPlayer.ActorNumber} left the room");
        
        // Limpiar el slot del jugador que se fue
        int playerID = otherPlayer.ActorNumber;
        
        // Si el jugador era el master client (DM), expulsar a todos
        if (otherPlayer.IsMasterClient)
        {
            Debug.Log("[LobbyManager] DM abandoned the room! Returning all players to main menu.");
            
            // Expulsar a todos los jugadores al main menu
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
                SceneLoaderController.Instance.LoadNextLevel(mainMenuScene);
            }
            return;
        }
        
        // Si no es el master, limpiar su slot usando el mapeo
        if (playerSlotMap.ContainsKey(playerID))
        {
            int slotIndex = playerSlotMap[playerID];
            
            if (slotIndex >= 0 && slotIndex < slots_used.Length)
            {
                slots_used[slotIndex] = "";
                player_ready[slotIndex] = false;
                playerSlotMap.Remove(playerID);
                
                if (slots_portraits[slotIndex] != null)
                {
                    var textComponent = slots_portraits[slotIndex].GetComponentInChildren<TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        textComponent.text = "-";
                    }
                    
                    var imageComponent = slots_portraits[slotIndex].GetComponent<Image>();
                    if (imageComponent != null)
                    {
                        imageComponent.color = Color.red;
                    }
                }
                
                Debug.Log($"[LobbyManager] Cleaned up slot {slotIndex} for player {playerID}");
            }
        }
    }

    // Callback cuando el master client cambia
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        Debug.Log($"[LobbyManager] Master client switched to player {newMasterClient.ActorNumber}");
        
        // Si el master client cambió, el DM original se desconectó
        // Expulsar a todos al main menu
        Debug.Log("[LobbyManager] DM disconnected! Returning all players to main menu.");
        
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            SceneLoaderController.Instance.LoadNextLevel(mainMenuScene);
        }
    }

    // Callback cuando se desconecta del servidor
    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        Debug.Log($"[LobbyManager] Disconnected from server: {cause}");
        SceneLoaderController.Instance.LoadNextLevel(mainMenuScene);
    }
}
