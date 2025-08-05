using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuButton : MonoBehaviourPunCallbacks
{
    public string mainMenu_sceneName;

    Button my_button;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        my_button = GetComponent<Button>();
        my_button.onClick.AddListener(ReturnToMainMenu);
    }

    public void ReturnToMainMenu()
    {
        LobbyManager.Instance.photonView.RPC("PlayerLeaveRoom", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene(mainMenu_sceneName);
    }

    //public override void OnLeftRoom()
    //{
    //    base.OnLeftRoom();
    //}
    public virtual void OnPlayerLeftRoom(Player otherPlayer)
    {
        LobbyManager.Instance.PlayerLeaveRoom(otherPlayer.ActorNumber);
    }




}
