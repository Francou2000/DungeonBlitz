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

    [PunRPC]
    public void AddHeroe(HeroesList heroe, int client_id)
    {
        playable_heroes[client_id - 2] = heroes_data[(int)heroe];
        players_ready[client_id - 2] = true;
        CheckIfStart();
    }

    [PunRPC]
    public void AddMapDM(Maps map, int[] unitInts, Vector3[] spawned_units_pos, bool[] is_unit_spawned, int[] trapInts, Vector3[] spawned_traps_pos, bool[] is_trap_spawned)
    {
        Units[] spawned_units_name = unitInts.Select(i => (Units)i).ToArray();
        Traps[] spawned_traps_name = trapInts.Select(i => (Traps)i).ToArray();
        playable_Map.SetMap(map);
        for (int i = 0; i < spawned_units_name.Length; i++)
        {
            if (!is_unit_spawned[i]) continue;
            DC_Unit new_unit = new DC_Unit();
            new_unit.unit_type = spawned_units_name[i];
            new_unit.pos = spawned_units_pos[i];
            playable_Map.AddUnit(new_unit);
        }
        for (int i = 0; i < spawned_traps_name.Length; i++)
        {
            if (!is_trap_spawned[i]) continue;
            DC_Trap new_unit = new DC_Trap();
            new_unit.trap_type = spawned_traps_name[i];
            new_unit.pos = spawned_traps_pos[i];
            playable_Map.AddTrap(new_unit);
        }

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

