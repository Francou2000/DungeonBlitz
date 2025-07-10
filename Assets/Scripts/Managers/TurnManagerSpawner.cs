using Photon.Pun;
using UnityEngine;

public class TurnManagerSpawner : MonoBehaviourPunCallbacks
{
    public GameObject turnManagerPrefab;

    private bool spawned = false;

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            TrySpawnTurnManager();
        }
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            TrySpawnTurnManager();
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            TrySpawnTurnManager();
        }
    }

    private void TrySpawnTurnManager()
    {
        if (spawned) return;

        GameObject existing = GameObject.FindWithTag("TurnManager");
        if (existing != null) return;

        PhotonNetwork.InstantiateRoomObject(turnManagerPrefab.name, Vector3.zero, Quaternion.identity);
        spawned = true;

        Debug.Log("[TurnManagerSpawner] TurnManager instantiated.");
    }
}
