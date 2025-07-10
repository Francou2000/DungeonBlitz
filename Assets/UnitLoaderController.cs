using NUnit.Framework;
using Photon.Pun;
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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Playable_Map playable_Map = new Playable_Map();
    public UnitData[] playable_heroes;

    [SerializeField] UnitData[] heroes_data;
    [SerializeField] UnitData[] goblins_data;

    [PunRPC]
    public void AddHeroe(HeroesList heroe, int client_id)
    {
        playable_heroes[client_id - 2] = heroes_data[(int)heroe];
        players_ready[client_id - 1] = true;
        CheckIfStart();
    }

    [PunRPC]
    public void AddMapDM(Maps map, int[] unitInts, Vector3[] spawned_units_pos, bool[] is_unit_spawned, int[] trapInts, Vector3[] spawned_traps_pos, bool[] is_trap_spawned)
    {
        Monsters[] spawned_units_name = unitInts.Select(i => (Monsters)i).ToArray();
        Traps[] spawned_traps_name = trapInts.Select(i => (Traps)i).ToArray();
        Debug.Log($"[AddMapDM] Received {spawned_units_name.Length} monster slots from Dungeon Creator");
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
        CheckIfStart();
    }

    void CheckIfStart()
    {
        foreach (bool ready in players_ready)
        {
            if (!ready) return;
        }
        TimerAndLoadGame.instance.LoadGame();
    }
}

