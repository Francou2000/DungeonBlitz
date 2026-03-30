using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class ServerConectionManager : MonoBehaviourPunCallbacks
{
    private bool isRecovering;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);

        if (isRecovering)
        {
            Debug.LogWarning($"[ServerConectionManager] Duplicate disconnect callback ignored: {cause}");
            return;
        }

        isRecovering = true;
        Debug.LogWarning($"[ServerConectionManager] Disconnected from Photon: {cause}. Starting recovery flow.");

        var loader = UnitLoaderController.Instance;
        if (loader != null)
        {
            Destroy(loader.gameObject);
        }

        PhotonNetwork.IsMessageQueueRunning = true;

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

        if (SceneLoaderController.Instance != null)
        {
            SceneLoaderController.Instance.LoadNextLevel(Scenes.MainMenu);
        }
        else
        {
            Debug.LogError("[ServerConectionManager] SceneLoaderController.Instance is null. Cannot load MainMenu.");
        }

        isRecovering = false;
    }
}
