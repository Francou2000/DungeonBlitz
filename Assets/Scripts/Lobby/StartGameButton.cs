using Photon.Pun;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class StartGameButton : MonoBehaviour
{
    Button my_button;
    void Start()
    {
        my_button = GetComponent<Button>();

        my_button.onClick.AddListener(LoadGame);
    }

    void LoadGame()
    {
        LobbyManager.Instance.photonView.RPC("LoadGame", RpcTarget.All);
    }
}
