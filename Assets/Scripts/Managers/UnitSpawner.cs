using UnityEngine;
using Photon.Pun;

public class UnitSpawner : MonoBehaviourPunCallbacks
{
    public Transform[] spawnPoints; // Assign 4 spawn points for heroes in inspector

    void Start()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer.CustomProperties["SelectedUnitID"] == null)
        {
            Debug.LogWarning("[Spawner] No unit selected.");
            return;
        }

        string unitID = PhotonNetwork.LocalPlayer.CustomProperties["SelectedUnitID"] as string;

        GameObject unitPrefab = Resources.Load<GameObject>($"Units/{unitID}");
        if (unitPrefab == null)
        {
            Debug.LogError($"[Spawner] Could not load prefab: Units/{unitID}");
            return;
        }

        // Decide spawn point (based on actor number or list)
        int index = Mathf.Clamp(PhotonNetwork.LocalPlayer.ActorNumber - 1, 0, spawnPoints.Length - 1);
        Vector3 spawnPos = spawnPoints[index].position;

        GameObject unitObj = PhotonNetwork.Instantiate($"Units/{unitID}", spawnPos, Quaternion.identity);

        // Optional: Set as controllable only by its owner
        var controller = unitObj.GetComponent<UnitController>();
        if (controller != null && unitObj.GetComponent<PhotonView>().IsMine)
        {
            controller.isControllable = true;
            Debug.Log("[Spawner] Spawned and assigned controllable unit.");
        }
    }
}
