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
    }

    void UpdateUI()
    {
        my_name.text = player_name;
    }

    public void SetCharacter(int new_name)
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
    public void AddCharacter(int new_name)
    {
        is_claimed = true;
        player_name = new_name.ToString();
        UpdateUI();
        LobbyManager.Instance.slots_used[new_name - 1] = new_name.ToString();
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
    }

}
