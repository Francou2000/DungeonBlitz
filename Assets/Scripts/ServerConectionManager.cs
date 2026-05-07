using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class ServerConectionManager : MonoBehaviourPunCallbacks
{
    public static ServerConectionManager Instance { get; private set; }

    private bool isHandlingReturnToMenu = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RequestReturnToMainMenu(string source)
    {
        if (isHandlingReturnToMenu)
        {
            Debug.Log($"[ServerConectionManager] Return to menu already in progress. Ignoring from {source}.");
            return;
        }

        isHandlingReturnToMenu = true;
        Debug.Log($"[ServerConectionManager] RequestReturnToMainMenu from {source}");

        var loader = UnitLoaderController.Instance;
        if (loader != null)
        {
            Destroy(loader.gameObject);
        }

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            return;
        }

        LoadMainMenuAndReconnect();
    }

    public override void OnLeftRoom()
    {
        if (!isHandlingReturnToMenu) return;
        Debug.Log("[ServerConectionManager] OnLeftRoom while returning to menu. Disconnecting...");
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[ServerConectionManager] OnDisconnected: {cause}");

        if (!isHandlingReturnToMenu)
        {
            isHandlingReturnToMenu = true;
        }

        LoadMainMenuAndReconnect();
    }

    private void LoadMainMenuAndReconnect()
    {
        if (SceneLoaderController.Instance != null)
        {
            SceneLoaderController.Instance.LoadNextLevel(Scenes.MainMenu);
        }

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

        isHandlingReturnToMenu = false;
    }
}
