using Photon.Pun;
using UnityEngine;

public class HeroSpawner : MonoBehaviourPunCallbacks
{
    public Transform[] spawnPoints; // Assign in Inspector (0–3 for heroes)
    public GameObject[] heroPrefabs; // Index must match heroes_data index

    void Start()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return;

        if (PhotonNetwork.IsMasterClient) return; // Master client shouldn't spawn heroes

        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 2;      // GetHeroPlayerIndex();
        if (playerIndex < 0 || playerIndex >= 4) return;

        UnitData data = UnitLoaderController.Instance.playable_heroes[playerIndex];
        if (data == null)
        {
            Debug.LogError("[HeroSpawner] No UnitData for player index " + playerIndex);
            return;
        }
        Debug.Log("Found hero: " + data.name);

        GameObject prefab = FindPrefabFor(data);
        if (prefab != null)
        {
            Vector3 pos = spawnPoints[playerIndex].position;
            PhotonNetwork.Instantiate(prefab.name, pos, Quaternion.identity);
            Debug.Log($"[HeroSpawner] Spawned {prefab.name} for player {playerIndex + 1}");
        }
        else
        {
            Debug.LogError("[HeroSpawner] Could not find prefab for " + data.unitName);
        }
    }

    GameObject FindPrefabFor(UnitData data)
    {
        foreach (var prefab in heroPrefabs)
        {
            var unitDataHolder = prefab.GetComponent<UnitModel>();
            if (unitDataHolder != null && unitDataHolder.UnitName == data.unitName)
                return prefab;
        }
        return null;
    }

    int GetHeroPlayerIndex()
    {
        int index = 0;
        foreach (var player in PhotonNetwork.PlayerList)
        {
            // Skip DM (assumed to be the MasterClient)
            if (player == PhotonNetwork.MasterClient) continue;

            if (player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
                return index;

            index++;
        }

        Debug.LogError("[HeroSpawner] Could not determine hero player index.");
        return -1;
    }
}