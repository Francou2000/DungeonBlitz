using Photon.Pun;
using TMPro;
using UnityEngine;

public class LobbyCharacterShow : MonoBehaviourPunCallbacks
{
    public bool is_claimed = false;
    string player_name;
    [SerializeField] TextMeshProUGUI my_name;

    void Start()
    {
        //my_name = GetComponentInChildren<TextMeshProUGUI>();
    }

    void UpdateUI()
    {
        my_name.text = player_name;
    }

    public void SetCharacter(string new_name)
    {
        is_claimed = true;
        player_name = new_name;
        UpdateUI();
    }

    public void RemoveCharacter()
    {
        is_claimed = false;
        player_name = "";
        UpdateUI();
    }
}
