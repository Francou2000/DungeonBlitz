using System.Collections;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class HeroSpawner : MonoBehaviourPunCallbacks
{
    public Transform[] spawnPoints; // Assign in Inspector (0�3 for heroes)
    public GameObject[] heroPrefabs; // Index must match heroes_data index

    void Start()
    {
        Debug.Log($"[HeroSpawner] Start() running on Actor={PhotonNetwork.LocalPlayer.ActorNumber} IsMaster={PhotonNetwork.IsMasterClient}");

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[HeroSpawner] Not connected or not in room.");
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[HeroSpawner] Master client, skipping hero spawn.");
            return;
        }

        // Usar el método que calcula el índice basado en la posición en la lista de jugadores
        // en lugar del ActorNumber directo, para manejar correctamente jugadores que se reconectan
        int playerIndex = GetHeroPlayerIndex();
        if (playerIndex < 0)
        {
            Debug.LogError($"[HeroSpawner] No se pudo determinar el índice del héroe para el jugador {PhotonNetwork.LocalPlayer.ActorNumber}");
            return;
        }

        if (playerIndex >= UnitLoaderController.Instance.heroes.Length)
        {
            Debug.LogError($"[HeroSpawner] Índice de héroe fuera de rango: {playerIndex}, máximo permitido: {UnitLoaderController.Instance.heroes.Length - 1}");
            return;
        }

        UnitData data = UnitLoaderController.Instance.heroes[playerIndex].my_data;
        if (data == null)
        {
            Debug.LogError($"[HeroSpawner] No UnitData para el jugador en índice {playerIndex} (ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}). " +
                          $"Esto puede ocurrir si el jugador no seleccionó un héroe antes de que comenzara el juego.");
            return;
        }
        Debug.Log("Found hero: " + data.name);

        GameObject prefab = FindPrefabFor(data);
        if (prefab != null)
        {
            Vector3 pos = spawnPoints[playerIndex].position;
            GameObject obj = PhotonNetwork.Instantiate(prefab.name, pos, Quaternion.identity);
            Debug.Log($"[HeroSpawner] Spawned {prefab.name} for player {playerIndex + 1}");

            obj.GetComponent<UnitModel>().AddItems(UnitLoaderController.Instance.heroes[playerIndex].my_items);

            if (obj.TryGetComponent(out UnitController controller) && controller.photonView.IsMine)
            {
                UnitController.ActiveUnit = controller;
                Debug.Log("[HeroSpawner] Assigned player unit as active unit: " + controller.unit.Model.UnitName);
            }            
        }
        else
        {
            Debug.LogError("[HeroSpawner] Could not find prefab for " + data.unitName);
        }

        StartCoroutine(WaitForTurnManager(playerIndex));
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

    /// <summary>
    /// Obtiene el índice del héroe basado en la posición del jugador local en la lista de jugadores
    /// (excluyendo al DM). Esto es más robusto que usar ActorNumber - 2 porque funciona
    /// correctamente cuando los jugadores se reconectan y obtienen nuevos ActorNumbers.
    /// </summary>
    int GetHeroPlayerIndex()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogError("[HeroSpawner] No conectado a Photon o no en sala. No se puede determinar el índice del héroe.");
            return -1;
        }

        // Obtener la lista de jugadores ordenada por ActorNumber
        var players = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
        int heroIndex = 0;

        // Buscar el jugador local y contar cuántos héroes hay antes de él (excluyendo al DM)
        bool found = false;
        foreach (var player in players)
        {
            if (player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                // Si es el DM, no debería estar aquí (ya se validó antes), pero por seguridad:
                if (player.IsMasterClient)
                {
                    Debug.LogError("[HeroSpawner] El jugador local es el DM, no debería estar spawneando héroes.");
                    return -1;
                }
                
                found = true;
                break;
            }
            
            // Solo contar héroes (no el DM) que están antes del jugador local
            if (!player.IsMasterClient)
            {
                heroIndex++;
            }
        }

        if (!found)
        {
            Debug.LogError($"[HeroSpawner] No se encontró el jugador local (ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}) en la lista de jugadores.");
            return -1;
        }

        Debug.Log($"[HeroSpawner] Índice de héroe determinado: {heroIndex} para jugador ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");
        return heroIndex;
    }

    private IEnumerator WaitForTurnManager(int idx)
    {
        while (TurnManager.Instance == null || TurnManager.Instance.GetComponent<PhotonView>() == null)
            yield return null;

        PhotonView tmView = TurnManager.Instance.GetComponent<PhotonView>();

        // announce how many heroes to expect 
        int totalHeroes = spawnPoints != null ? spawnPoints.Length : 0;
        tmView.RPC(nameof(TurnManager.RPC_SetExpectedHeroes), RpcTarget.All, totalHeroes);

        // mark this particular hero index as ready
        tmView.RPC(nameof(TurnManager.RPC_HeroeGotInstanciated), RpcTarget.All, idx);
    }
}