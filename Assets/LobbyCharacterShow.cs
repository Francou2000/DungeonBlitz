using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class LobbyCharacterShow : MonoBehaviourPunCallbacks
{
    public bool is_claimed = false;
    public int my_place;
    string player_name;
    [SerializeField] TextMeshProUGUI my_name;

    void Start()
    {
        //int player_number = PhotonNetwork.LocalPlayer.ActorNumber;
        //if (player_number != my_place + 1 )
        //my_name.text = LobbyManager.Instance.slots_used[my_place];
        //my_name = GetComponentInChildren<TextMeshProUGUI>();
        player_name = PhotonNetwork.NickName;
        SetCharacter(player_name);
    }

    void UpdateUI()
    {
        my_name.text = player_name;
    }

    public void SetCharacter(string new_name)
    {
        photonView.RPC("AddCharacter", RpcTarget.All, new_name);
    }

    public void RemoveCharacter()
    {
        is_claimed = false;
        player_name = "";
        UpdateUI();
    }
    [PunRPC]
    public void AddCharacter(string new_name)
    {
        is_claimed = true;
        //player_name = new_name.ToString(); Wazel changes
        int idPlayer = PhotonNetwork.LocalPlayer.ActorNumber;
        player_name = PhotonNetwork.NickName;
        UpdateUI();
        //LobbyManager.Instance.slots_used[new_name - 1] = new_name.ToString(); Wazel changes
        LobbyManager.Instance.slots_used[idPlayer - 1] = player_name;
        Debug.Log("RPC SLOT USED " + (idPlayer-1)+ " new name - 1");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        int player_number = PhotonNetwork.LocalPlayer.ActorNumber;
        if (player_number != my_place)
        {
            my_name.text = LobbyManager.Instance.slots_used[my_place];
        }
        else
        {
            my_name.text = player_number.ToString();
        }
        Debug.LogWarning("ON ENTERED ROOM");
    }

}
