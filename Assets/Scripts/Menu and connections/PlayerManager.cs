using Photon.Pun;
using UnityEngine;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    [SerializeField] LobbyCharacterShow[] characterList;
        

    public void CheckEmptyCharacter()
    {
        foreach (var character in characterList)
        {
            if (character.is_claimed) continue;
            ClaimCharacter(character);
        }

    }

    void ClaimCharacter(LobbyCharacterShow char_to_claim)
    {
        char_to_claim.SetCharacter(PhotonNetwork.LocalPlayer.ActorNumber.ToString());
    }
}
