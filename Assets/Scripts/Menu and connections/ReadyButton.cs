using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReadyButton : MonoBehaviourPunCallbacks
{
    Button my_button;
    TextMeshProUGUI my_text;
    bool is_ready = false;

    public bool is_test = false;
    public string test_level;
    void Start()
    {
        my_button = GetComponent<Button>();
        my_text = GetComponentInChildren<TextMeshProUGUI>();

        my_button.onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        if (is_test)
        {
            PhotonNetwork.LoadLevel(test_level);
        }
        else
        {
            // SwapReadiness();
            LobbyManager.Instance.photonView.RPC("ReadyPlayer", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }


    void SwapReadiness()
    {
        if (is_ready) {
            is_ready = false;
            my_text.text = "Ready";
        }
        else
        {
            is_ready = true;
            my_text.text = "Cancel";
        }
    }
}
