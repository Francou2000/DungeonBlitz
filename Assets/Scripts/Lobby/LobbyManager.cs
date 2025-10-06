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

    [SerializeField] TextMeshProUGUI lobby_name;

    public string[] slots_used = new string[5];
    [SerializeField] GameObject[] slots_portraits = new GameObject[5];

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
        ChangeLobbyName();
        if (!PhotonNetwork.IsMasterClient) { photonView.RPC("GetReadyState", RpcTarget.MasterClient); }
        else { start_game_button.SetActive(true); }
    }

    public void CheckPlayerName()
    {
        TextMeshProUGUI text_slot = slots_portraits[0].GetComponentInChildren<TextMeshProUGUI>();
        if (PhotonNetwork.IsMasterClient)
        {
            slots_used[0] = PhotonNetwork.NickName;
            text_slot.text = PhotonNetwork.NickName;
        }
        else
        {
            slots_used[0] = PhotonNetwork.MasterClient.NickName;
            text_slot.text = PhotonNetwork.MasterClient.NickName;
            int playernumber = PhotonNetwork.LocalPlayer.ActorNumber;
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.ActorNumber > playernumber) continue;
                photonView.RPC("AskForNewCharacter", RpcTarget.MasterClient, player.ActorNumber, player.NickName);
                //slots_used[player.ActorNumber - 1] = player_name;
                //slots_portraits[player.ActorNumber - 1].GetComponentInChildren<TextMeshProUGUI>().text = player_name;
            }
            string playername = PhotonNetwork.NickName;
            // slots_used[playernumber - 1] = playername;
            // slots_text[playernumber - 1].text = playername;
            Debug.Log("Player number lobby manager " + playernumber);
            
            photonView.RPC("AskForNewCharacter", RpcTarget.MasterClient, playernumber,playername);

        }
        // Debug.Log("player local " + PlayerPrefs.GetString("playerNickname"));
        // Debug.Log("player photon " + PhotonNetwork.NickName);
        // Debug.Log("player ID  " + PhotonNetwork.LocalPlayer.ActorNumber);
        // Debug.Log("player count  " + PhotonNetwork.CountOfPlayers);
        
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
    // Update is called once per frame
    void Update()
    {
        
    }

    
    public bool[] player_ready = new bool[5];
    
    [PunRPC]
    public void ReadyPlayer(int playerID)
    {
        player_ready[playerID - 1] = !player_ready[playerID - 1];
        slots_portraits[playerID - 1].GetComponent<Image>().color = player_ready[playerID - 1] ? Color.green : Color.red;
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
        slots_portraits[playerID - 1].GetComponentInChildren<TextMeshProUGUI>().text = "-";
        // slots_portraits[playerID - 1].GetComponentInChildren<Image>().sprite = portrait;
        slots_portraits[playerID - 1].GetComponent<Image>().color = Color.red;

        player_ready[playerID - 1] = false;
    }
    [PunRPC]
    public void AskForNewCharacter(int playerID, string playerNick)
    {
        photonView.RPC("AddCharacter", RpcTarget.All, playerID, playerNick, player_ready[playerID - 1]);
    }
    [PunRPC]
    public void AddCharacter(int playerID,string playerNick, bool is_ready)
    {
       
        slots_used[playerID - 1] = playerNick;
        slots_portraits[playerID - 1].GetComponentInChildren<TextMeshProUGUI>().text = playerNick;
        // slots_portraits[playerID - 1].GetComponentInChildren<Image>().sprite = portrait;
        slots_portraits[playerID - 1].GetComponent<Image>().color = is_ready ? Color.green : Color.red;


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
