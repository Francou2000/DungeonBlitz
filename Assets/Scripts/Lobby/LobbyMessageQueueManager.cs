using Photon.Pun;
using UnityEngine;

public class LobbyMessageQueueManager : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        // Reactivar la cola de mensajes cuando el lobby esté completamente cargado
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMessageQueueRunning)
        {
            Debug.Log("[LobbyMessageQueueManager] Reactivando cola de mensajes en el lobby");
            PhotonNetwork.IsMessageQueueRunning = true;
        }
    }

    public override void OnJoinedRoom()
    {
        // Asegurar que la cola de mensajes esté activa cuando se une a la sala
        if (!PhotonNetwork.IsMessageQueueRunning)
        {
            Debug.Log("[LobbyMessageQueueManager] Reactivando cola de mensajes después de unirse a la sala");
            PhotonNetwork.IsMessageQueueRunning = true;
        }
    }
}