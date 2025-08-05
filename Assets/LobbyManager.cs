using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager Instance;

    [SerializeField] string selection_scene;
    //[SerializeField] string heroe_scene;
    [SerializeField] Button ready_button;

    public string[] slots_used = new string[5];
    public TextMeshProUGUI[] slots_text = new TextMeshProUGUI[5];

    public GameObject start_game_button;
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
        slots_used[0] = "DM name";
        slots_used[1] = "P1 name";
        slots_used[2] = "P2 name";
        slots_used[3] = "P3 name";
        slots_used[4] = "P4 name";
        CheckPlayerName();
        if (!PhotonNetwork.IsMasterClient) { photonView.RPC("GetReadyState", RpcTarget.MasterClient); }
        else { start_game_button.SetActive(true); }
    }

    public void CheckPlayerName()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            slots_used[0] = PhotonNetwork.NickName;
            slots_text[0].text = PhotonNetwork.NickName;
        }
        else
        {
            slots_used[0] = PhotonNetwork.MasterClient.NickName;
            slots_text[0].text = PhotonNetwork.MasterClient.NickName;
            int playernumber = PhotonNetwork.LocalPlayer.ActorNumber;
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.ActorNumber > playernumber) continue;
                string player_name = player.NickName;
                slots_used[player.ActorNumber - 1] = player_name;
                slots_text[player.ActorNumber - 1].text = player_name;
            }
            string playername = PhotonNetwork.NickName;
            // slots_used[playernumber - 1] = playername;
            // slots_text[playernumber - 1].text = playername;
            Debug.Log("Player number lobby manager " + playernumber);
            
            photonView.RPC("AddCharacter", RpcTarget.All, playernumber,playername);

        }
        // Debug.Log("player local " + PlayerPrefs.GetString("playerNickname"));
        // Debug.Log("player photon " + PhotonNetwork.NickName);
        // Debug.Log("player ID  " + PhotonNetwork.LocalPlayer.ActorNumber);
        // Debug.Log("player count  " + PhotonNetwork.CountOfPlayers);
        
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
    // Update is called once per frame
    void Update()
    {
        
    }

    
    public bool[] player_ready = new bool[5];
    
    [PunRPC]
    public void ReadyPlayer(int playerID)
    {
        player_ready[playerID - 1] = !player_ready[playerID - 1];
        foreach (bool is_ready in player_ready)
        {
            if (!is_ready) return;
        }
        start_game_button.GetComponent<Button>().interactable = true;
    }
    [PunRPC]
    public void PlayerLeaveRoom(int playerID)
    {
        slots_used[playerID - 1] = "";
        slots_text[playerID - 1].text = "";
        player_ready[playerID - 1] = false;
    }
    [PunRPC]
    public void AddCharacter(int playerID,string playerNick)
    {
       
        slots_used[playerID - 1] = playerNick;
        slots_text[playerID - 1].text = playerNick;
        
        Debug.Log("RPC SLOT USED " + (playerID-1)+ " new name - 1");
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
        SceneManager.LoadScene(selection_scene);
    }
}
