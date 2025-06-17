using TMPro;
using UnityEngine;

public class LobbyCharacterShow : MonoBehaviour
{
    public bool is_claimed = false;
    string player_name;
    TextMeshProUGUI my_name;

    void Start()
    {
        my_name = GetComponent<TextMeshProUGUI>();
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
