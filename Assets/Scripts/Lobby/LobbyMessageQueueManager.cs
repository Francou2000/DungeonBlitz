using Photon.Pun;
using UnityEngine;

public class LobbyMessageQueueManager : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        // #region agent log
        DebugSessionNdjson.Write("H2", "LobbyMessageQueueManager.Start", "enter",
            $"{{\"msgQueue\":{(PhotonNetwork.IsMessageQueueRunning ? "true" : "false")},\"inRoom\":{(PhotonNetwork.InRoom ? "true" : "false")},\"scene\":\"{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Replace("\"", "'")}\"}}");
        // #endregion
        // Reactivar la cola de mensajes cuando el lobby esté completamente cargado
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMessageQueueRunning)
        {
            Debug.Log("[LobbyMessageQueueManager] Reactivando cola de mensajes en el lobby");
            PhotonNetwork.IsMessageQueueRunning = true;
        }
        // #region agent log
        DebugSessionNdjson.Write("H2", "LobbyMessageQueueManager.Start", "after_enable",
            $"{{\"msgQueue\":{(PhotonNetwork.IsMessageQueueRunning ? "true" : "false")}}}");
        // #endregion
    }

    public override void OnJoinedRoom()
    {
        // #region agent log
        DebugSessionNdjson.Write("H2", "LobbyMessageQueueManager.OnJoinedRoom", "enter",
            $"{{\"msgQueue\":{(PhotonNetwork.IsMessageQueueRunning ? "true" : "false")}}}");
        // #endregion
        // Asegurar que la cola de mensajes esté activa cuando se une a la sala
        if (!PhotonNetwork.IsMessageQueueRunning)
        {
            Debug.Log("[LobbyMessageQueueManager] Reactivando cola de mensajes después de unirse a la sala");
            PhotonNetwork.IsMessageQueueRunning = true;
        }
    }
}