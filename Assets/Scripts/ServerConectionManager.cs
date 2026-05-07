using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerConectionManager : MonoBehaviourPunCallbacks
{
    public static ServerConectionManager Instance { get; private set; }

    private bool isHandlingReturnToMenu = false;
    private int transitionOpId = 0;

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
        int opId = ++transitionOpId;

        if (isHandlingReturnToMenu)
        {
            LogState(opId, source, "ignored_already_in_progress");
            return;
        }

        isHandlingReturnToMenu = true;
        LogState(opId, source, "start_return_to_menu");

        var loader = UnitLoaderController.Instance;
        if (loader != null)
        {
            Destroy(loader.gameObject);
            LogState(opId, source, "unit_loader_destroyed");
        }

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            LogState(opId, source, "leave_room");
            PhotonNetwork.LeaveRoom();
            return;
        }

        if (PhotonNetwork.IsConnected)
        {
            LogState(opId, source, "disconnect");
            PhotonNetwork.Disconnect();
            return;
        }

        LoadMainMenuAndReconnect(opId, source);
    }

    public override void OnLeftRoom()
    {
        if (!isHandlingReturnToMenu) return;
        int opId = transitionOpId;
        LogState(opId, "OnLeftRoom", "disconnect_after_leave");
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        int opId = transitionOpId == 0 ? ++transitionOpId : transitionOpId;
        LogState(opId, "OnDisconnected", $"cause={cause}");

        if (!isHandlingReturnToMenu)
        {
            isHandlingReturnToMenu = true;
        }

        LoadMainMenuAndReconnect(opId, "OnDisconnected");
    }

    public void EnsureConnectedFromMainMenu(string source)
    {
        if (isHandlingReturnToMenu) return;
        if (!PhotonNetwork.IsConnected)
        {
            int opId = ++transitionOpId;
            LogState(opId, source, "ensure_connected_connect_using_settings");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    private void LoadMainMenuAndReconnect(int opId, string source)
    {
        if (SceneLoaderController.Instance != null)
        {
            LogState(opId, source, "load_main_menu");
            SceneLoaderController.Instance.LoadNextLevel(Scenes.MainMenu);
        }

        if (!PhotonNetwork.IsConnected)
        {
            LogState(opId, source, "reconnect_using_settings");
            PhotonNetwork.ConnectUsingSettings();
        }

        isHandlingReturnToMenu = false;
        LogState(opId, source, "complete");
    }

    private void LogState(int opId, string source, string step)
    {
        var scene = SceneManager.GetActiveScene().name;
        Debug.Log($"[ServerConectionManager] op={opId} src={source} step={step} scene={scene} connected={PhotonNetwork.IsConnected} inRoom={PhotonNetwork.InRoom}");
    }
}
