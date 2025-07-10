using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class MonsterSpawner : MonoBehaviourPunCallbacks
{
    public GameObject[] monsterPrefabs; // Indexed the same way as goblins_data

    void Start()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        var monsterList = UnitLoaderController.Instance.playable_Map.UNITS;
        Debug.Log("[EnemySpawner] Total monsters to spawn: " + monsterList.Count);
        StartCoroutine(SpawnWhenReady());
    }

    private IEnumerator SpawnWhenReady()
    {
        // wait until the loader has at least one monster
        yield return new WaitUntil(() =>
            UnitLoaderController.Instance != null &&
            UnitLoaderController.Instance.playable_Map.UNITS.Count > 0
        );

        var units = UnitLoaderController.Instance.playable_Map.UNITS;
        Debug.Log($"[EnemySpawner] Now spawning {units.Count} monsters");

        foreach (var dcUnit in units)
        {
            // find the prefab by name
            GameObject prefab = null;
            foreach (var p in monsterPrefabs)
                if (p.GetComponent<UnitModel>()?.UnitName == dcUnit.unit_type.unitName)
                    prefab = p;

            if (prefab == null)
            {
                Debug.LogError($"[EnemySpawner] No prefab for {dcUnit.unit_type.unitName}");
                continue;
            }

            PhotonNetwork.Instantiate(prefab.name, dcUnit.pos, Quaternion.identity);
            Debug.Log($"[EnemySpawner] Spawned {prefab.name} at {dcUnit.pos}");
        }
    }
}
