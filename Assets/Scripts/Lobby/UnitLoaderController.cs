using NUnit.Framework;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class UnitLoaderController : MonoBehaviourPunCallbacks
{
    public static UnitLoaderController Instance;

    public bool[] players_ready = new bool[5];
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeArrays();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        foreach (ItemData item in items)
        {
            dic_items[item.ItemID] = item;
        }
    }
    /// <summary>
    /// Inicializa los arrays para evitar datos residuales
    /// </summary>
    private void InitializeArrays()
    {
        // Inicializar el array de heroes
        for (int i = 0; i < heroes.Length; i++)
        {
            heroes[i] = new HeroeInformation(null, 0, 0, new List<ItemData>());
        }
        
        // Inicializar el array de players_ready
        for (int i = 0; i < players_ready.Length; i++)
        {
            players_ready[i] = false;
        }
        
        Debug.Log("[UnitLoaderController] Arrays inicializados correctamente.");
    }

    /// <summary>
    /// Obtiene el índice del array heroes basado en la posición del jugador en la lista de jugadores
    /// Usa la posición relativa en lugar del ActorNumber para ser más robusto
    /// Retorna -1 si el índice está fuera de rango o el jugador no se encuentra
    /// </summary>
    public int GetHeroIndex(int client_id)
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[UnitLoaderController] No conectado a Photon o no en sala. Usando cálculo basado en ActorNumber.");
            // Fallback al método antiguo si no estamos conectados
            int index = client_id - 2;
            if (index < 0 || index >= heroes.Length)
            {
                Debug.LogError($"[UnitLoaderController] Índice de héroe fuera de rango. client_id: {client_id}, índice calculado: {index}, rango válido: 0-{heroes.Length - 1}");
                return -1;
            }
            return index;
        }

        // Obtener la lista de jugadores ordenada por ActorNumber
        var players = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
        
        // Buscar el jugador y contar cuántos héroes hay antes de él (excluyendo al DM)
        int heroPosition = 0;
        bool found = false;

        foreach (var player in players)
        {
            if (player.ActorNumber == client_id)
            {
                // Si es el DM, no tiene índice de héroe
                if (player.IsMasterClient)
                {
                    Debug.LogError($"[UnitLoaderController] GetHeroIndex: El client_id {client_id} es el DM, no tiene índice de héroe.");
                    return -1;
                }
                
                found = true;
                break;
            }
            
            // Solo contar héroes (no el DM) que están antes del jugador buscado
            if (!player.IsMasterClient)
            {
                heroPosition++;
            }
        }

        if (!found)
        {
            Debug.LogError($"[UnitLoaderController] GetHeroIndex: No se encontró el jugador con client_id {client_id} en la lista de jugadores.");
            return -1;
        }

        // Validar que el índice esté en rango
        if (heroPosition < 0 || heroPosition >= heroes.Length)
        {
            int totalHeroes = players.Count(p => !p.IsMasterClient);
            Debug.LogError($"[UnitLoaderController] Índice de héroe fuera de rango. client_id: {client_id}, posición: {heroPosition}, rango válido: 0-{heroes.Length - 1}, total héroes en sala: {totalHeroes}. " +
                          $"El sistema está diseñado para máximo {heroes.Length} héroes.");
            return -1;
        }
        
        Debug.Log($"[UnitLoaderController] GetHeroIndex: client_id {client_id} -> índice héroe {heroPosition} (posición {heroPosition + 1} en lista de héroes)");
        return heroPosition;
    }

    /// <summary>
    /// Obtiene el índice del array players_ready basado en la posición del jugador en la lista
    /// Retorna -1 si el índice está fuera de rango o el jugador no se encuentra
    /// </summary>
    private int GetPlayerReadyIndex(int client_id)
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[UnitLoaderController] No conectado a Photon o no en sala. Usando cálculo basado en ActorNumber.");
            // Fallback al método antiguo si no estamos conectados
            int index = client_id - 1;
            if (index < 0 || index >= players_ready.Length)
            {
                Debug.LogError($"[UnitLoaderController] Índice de player_ready fuera de rango. client_id: {client_id}, índice calculado: {index}, rango válido: 0-{players_ready.Length - 1}");
                return -1;
            }
            return index;
        }

        // Obtener la lista de jugadores ordenada por ActorNumber
        var players = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
        int position = 0;
        bool found = false;

        // Encontrar la posición del jugador en la lista (incluyendo al DM)
        foreach (var player in players)
        {
            if (player.ActorNumber == client_id)
            {
                found = true;
                break;
            }
            position++;
        }

        if (!found)
        {
            Debug.LogError($"[UnitLoaderController] GetPlayerReadyIndex: No se encontró el jugador con client_id {client_id} en la lista de jugadores.");
            return -1;
        }

        // Validar que el índice esté en rango
        if (position < 0 || position >= players_ready.Length)
        {
            Debug.LogError($"[UnitLoaderController] Índice de player_ready fuera de rango. client_id: {client_id}, posición: {position}, rango válido: 0-{players_ready.Length - 1}, total jugadores: {players.Length}. " +
                          $"El sistema está diseñado para máximo {players_ready.Length} jugadores (1 DM + {players_ready.Length - 1} héroes).");
            return -1;
        }
        
        Debug.Log($"[UnitLoaderController] GetPlayerReadyIndex: client_id {client_id} -> índice ready {position} (posición {position + 1} en lista de jugadores)");
        return position;
    }

    public float dm_remaining_time;
    public float heroes_remaining_time;

    public Playable_Map playable_Map = new Playable_Map();
    public UnitData[] playable_heroes;

    [SerializeField] public HeroeInformation[] heroes = new HeroeInformation[4];
    [SerializeField] UnitData[] heroes_data;
    [SerializeField] UnitData[] goblins_data;

    public int lvl = 1;

    [SerializeField] ItemData[] items;
    Dictionary<int, ItemData> dic_items = new Dictionary<int, ItemData>();
    [PunRPC]
    public void AddHeroe(HeroesList heroe, int client_id)
    {
        // Validar que el client_id sea válido (debe ser un héroe, no el DM)
        if (client_id <= 1)
        {
            Debug.LogError($"[UnitLoaderController] AddHeroe: client_id inválido. El client_id debe ser >= 2 (DM es 1). Recibido: {client_id}");
            return;
        }

        // Obtener índices de forma segura
        int heroIndex = GetHeroIndex(client_id);
        int readyIndex = GetPlayerReadyIndex(client_id);
        
        if (heroIndex < 0 || readyIndex < 0)
        {
            Debug.LogError($"[UnitLoaderController] AddHeroe: No se pudo obtener índices válidos para client_id: {client_id}");
            return;
        }

        // Validar que el índice del héroe esté en rango
        int heroeDataIndex = (int)heroe;
        if (heroeDataIndex < 0 || heroeDataIndex >= heroes_data.Length)
        {
            Debug.LogError($"[UnitLoaderController] AddHeroe: Índice de héroe fuera de rango. heroe: {heroe}, índice: {heroeDataIndex}, rango válido: 0-{heroes_data.Length - 1}");
            return;
        }

        // Limpiar datos previos del slot antes de asignar nuevos
        heroes[heroIndex] = new HeroeInformation(heroes_data[heroeDataIndex], 0, 0, new List<ItemData>());
        players_ready[readyIndex] = true;
        
        Debug.Log($"[UnitLoaderController] Héroe agregado: {heroe} para jugador client_id: {client_id} (índice héroe: {heroIndex}, índice ready: {readyIndex})");
        
        CheckIfStart(false);
    }

    [PunRPC]
    public void AddMapDM(Maps map, int[] unitInts, Vector3[] spawned_units_pos, bool[] is_unit_spawned, int[] trapInts, Vector3[] spawned_traps_pos, bool[] is_trap_spawned)
    {
        Monsters[] spawned_units_name = unitInts.Select(i => (Monsters)i).ToArray();
        Traps[] spawned_traps_name = trapInts.Select(i => (Traps)i).ToArray();
        Debug.Log($"[AddMapDM] Received {spawned_units_name.Length} monster slots from Dungeon Creator");
        playable_Map.Reset();
        playable_Map.SetMap(map);
        for (int i = 0; i < spawned_units_name.Length; i++)
        {
            Debug.Log($"[AddMapDM] Unit[{i}] - {spawned_units_name[i]} | IsSpawned: {is_unit_spawned[i]}");
            if (!is_unit_spawned[i]) continue;

            int unitIndex = (int)spawned_units_name[i] - 1;

            if (unitIndex < 0 || unitIndex >= goblins_data.Length)
            {
                Debug.LogError($"[AddMapDM] Invalid monster index: {unitIndex}");
                continue;
            }

            DC_Unit new_unit = new DC_Unit();
            new_unit.unit_type = goblins_data[(int)spawned_units_name[i] - 1];
            new_unit.pos = spawned_units_pos[i];
            playable_Map.AddUnit(new_unit);

            Debug.Log($"[AddMapDM] Added monster: {new_unit.unit_type.unitName} at {new_unit.pos}");
        }

        for (int i = 0; i < spawned_traps_name.Length; i++)
        {
            if (!is_trap_spawned[i]) continue;
            DC_Trap new_unit = new DC_Trap();
            new_unit.trap_type = spawned_traps_name[i];
            new_unit.pos = spawned_traps_pos[i];
            playable_Map.AddTrap(new_unit);
        }

        Debug.Log($"[AddMapDM] Total monsters added: {playable_Map.UNITS.Count}");
        players_ready[0] = true;
        // CheckIfStart();
    }

    [PunRPC]
    public void DM_SelectMap(int map)
    {
        playable_Map.Reset();
        playable_Map.SetMap((Maps)map);
    }

    [PunRPC]
    public void DM_AddUnitsToMap(Vector3[] units)   //x,y are the positions in the tile. z is the unit id
    {
        playable_Map.RemoveAllUnits();
        foreach (Vector3 unit in units)
        {
            DC_Unit new_unit = new DC_Unit(new Vector2(unit.x, unit.y), goblins_data[(int)unit.z - 1]);

            playable_Map.AddUnit(new_unit);
        }
        players_ready[0] = false;
    }

    [PunRPC]
    public void DM_AddTrapsToMap(Vector3[] traps)   //x,y are the positions in the tile. z is the trap id
    {
        playable_Map.RemoveAllTraps();
        foreach (Vector3 trap in traps)
        {
            DC_Trap new_trap = new DC_Trap(new Vector2(trap.x, trap.y), (Traps)trap.z);

            playable_Map.AddTrap(new_trap);
        }
    }


    [PunRPC]
    public void CheckIfStart(bool isMC)
    {
        if (isMC) players_ready[0] = true;
        foreach (bool ready in players_ready)
        {
            if (!ready) return;
        }
        Debug.Log(playable_Map.UNITS.Count());
        TimerAndLoadGame.instance.LoadGame();
    }

    [PunRPC]
    public void AddItemToHeroe(int playerID, int itemID)
    {
        // Validar que el playerID sea válido (debe ser un héroe, no el DM)
        if (playerID <= 1)
        {
            Debug.LogError($"[UnitLoaderController] AddItemToHeroe: playerID inválido. El playerID debe ser >= 2 (DM es 1). Recibido: {playerID}");
            return;
        }

        int heroIndex = GetHeroIndex(playerID);
        if (heroIndex < 0)
        {
            Debug.LogError($"[UnitLoaderController] AddItemToHeroe: No se pudo obtener índice válido para playerID: {playerID}");
            return;
        }

        // Asegurarse de que la lista de items esté inicializada
        if (heroes[heroIndex].my_items == null)
        {
            heroes[heroIndex] = new HeroeInformation(
                heroes[heroIndex].my_data,
                heroes[heroIndex].actual_health,
                heroes[heroIndex].volatile_time_left,
                new List<ItemData>()
            );
        }

        heroes[heroIndex].my_items.Add(dic_items[itemID]);
        Debug.Log($"[UnitLoaderController] Item agregado al jugador {playerID} (índice héroe: {heroIndex})");
    }
    [PunRPC]
    public void SpendHeroeSeconds(int amount)
    {
        heroes_remaining_time -= amount;
    }

    [PunRPC]
    public void SpendDMSeconds(int amount)
    {
        dm_remaining_time -= amount;
    }

    // === PHOTON CALLBACKS ===

    /// <summary>
    /// Se llama cuando un jugador abandona la sala
    /// Limpia los datos del jugador que se fue para evitar conflictos cuando se reconecta
    /// </summary>
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        int client_id = otherPlayer.ActorNumber;
        Debug.Log($"[UnitLoaderController] Jugador {client_id} abandonó la sala. Limpiando sus datos...");

        // Si es el DM, no hacer nada (el DM no está en el array de heroes)
        if (client_id == 1)
        {
            Debug.Log("[UnitLoaderController] El DM abandonó la sala. Limpiando estado del DM.");
            players_ready[0] = false;
            return;
        }

        // Limpiar datos del héroe que se fue
        int heroIndex = GetHeroIndex(client_id);
        int readyIndex = GetPlayerReadyIndex(client_id);

        if (heroIndex >= 0)
        {
            // Resetear el héroe a valores por defecto
            heroes[heroIndex] = new HeroeInformation(null, 0, 0, new List<ItemData>());
            Debug.Log($"[UnitLoaderController] Datos del héroe en índice {heroIndex} limpiados.");
        }

        if (readyIndex >= 0)
        {
            // Marcar como no listo
            players_ready[readyIndex] = false;
            Debug.Log($"[UnitLoaderController] Estado 'ready' del jugador en índice {readyIndex} limpiado.");
        }

        Debug.Log($"[UnitLoaderController] Limpieza completada para jugador {client_id}.");
    }

    /// <summary>
    /// Se llama cuando un nuevo jugador entra a la sala
    /// Puede ser útil para sincronizar datos si es necesario
    /// </summary>
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log($"[UnitLoaderController] Nuevo jugador {newPlayer.ActorNumber} entró a la sala.");
        // Aquí podrías agregar lógica adicional si necesitas sincronizar datos con el nuevo jugador
    }
}

[Serializable]
public struct HeroeInformation
{
    public UnitData my_data;
    public int actual_health;
    public int volatile_time_left;
    public List<ItemData> my_items;

    public HeroeInformation(UnitData my_data, int actual_health, int volatile_time_left, List<ItemData> my_items = null)
    {
        this.my_data = my_data;
        this.actual_health = actual_health;
        this.volatile_time_left = volatile_time_left;
        this.my_items = my_items;
    }
}
