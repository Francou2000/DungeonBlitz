using Photon.Pun;
using UnityEngine;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    [SerializeField] LobbyCharacterShow[] characterList;

    private void Start()
    {
        ClaimCharacter();
    }

    void ClaimCharacter()
    {
        //int player_number = PhotonNetwork.LocalPlayer.ActorNumber;
        //characterList[player_number - 1].SetCharacter(player_number);
    }
}
