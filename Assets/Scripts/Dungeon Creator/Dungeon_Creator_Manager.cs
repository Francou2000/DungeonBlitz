using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class Dungeon_Creator_Manager : MonoBehaviour
{
    public static Dungeon_Creator_Manager Instance;
    
    Vector3 mouse_position      = new Vector3();
    public GameObject[] map_list;
    Monsters selected_unit         = Monsters.NONE;
    public GameObject[] unit_list;
    Traps selected_trap         = Traps.NONE;
    public GameObject[] trap_list;
    public Transform spawn_point;
    public GameObject menu_for_selection;
    public DC_State dc_state           = DC_State.NONE;
    Playable_Map finished_map       = new Playable_Map();
    public UnityEvent PlaceSelected = new UnityEvent();

    public GameObject actual_prefab;
    

    public Monsters Selected_unit  { get { return selected_unit;   }   set { selected_unit = value; }  }
    public Traps Selected_trap  { get { return selected_trap;   }   set { selected_trap = value; }  }
    public DC_State DC_state    { get { return dc_state;        }   set { dc_state = value;      }  }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    void Start()
    {
        finished_map.SetMap(Maps.MAP_NAME1);
        Instantiate(map_list[(int)finished_map.Actual_map], spawn_point);
    }

    void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0)) on_mouse_clicked();
        
    }

    void on_mouse_clicked()
    {
        mouse_position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (dc_state == DC_State.PLACING_UNIT)
        {
            PlaceSelected.Invoke();
            //actual_prefab = null;
        }
        else if (dc_state == DC_State.PLAICING_TRAP)
        {
            PlaceSelected.Invoke();
            //actual_prefab = null;
        }
    }
    public void change_map(Maps new_map) {
        DC_Elements_Data[] selected_elements = spawn_point.GetComponentsInChildren<DC_Elements_Data>();
        for (int i = selected_elements.Length - 1; i > -1; i--)
        {
            Destroy(selected_elements[i].gameObject);
        }

        Instantiate(map_list[(int)new_map], spawn_point);
    }
    public void select_unit(Monsters new_unit, DC_State new_state = DC_State.PLACING_UNIT)
    {
        Destroy(actual_prefab);
        selected_unit = new_unit;
        actual_prefab = Instantiate(unit_list[(int)selected_unit], spawn_point);
        actual_prefab.GetComponent<Follow_Mouse_for_Placing>().selected_menu = menu_for_selection;
        dc_state = new_state;
    }
    public void select_trap(Traps new_trap, DC_State new_state = DC_State.PLAICING_TRAP)
    {
        Destroy(actual_prefab);
        selected_trap = new_trap;
        actual_prefab = Instantiate(trap_list[(int)selected_trap], spawn_point);
        actual_prefab.GetComponent<Follow_Mouse_for_Placing>().selected_menu = menu_for_selection;
        dc_state = new_state;
    }
    public void save_map() 
    {
        Transform[] selected_elements = spawn_point.GetComponentsInChildren<Transform>();
        Maps spawned_map =  Maps.NONE;

        Monsters[] spawned_units_name = new Monsters[30];
        Vector3[] spawned_units_pos = new Vector3[30];
        bool[] is_unit_spawned = new bool[30];
        int unit_idx = 0;
        Traps[] spawned_traps_name = new Traps[30];
        Vector3[] spawned_traps_pos = new Vector3[30];
        bool[] is_trap_spawned = new bool[30];
        int trap_idx = 0;

        foreach (Transform this_element in selected_elements)
        {
            GameObject element = this_element.gameObject;
            if (element.activeInHierarchy) continue;
            DC_Elements_Data data = element.GetComponent<DC_Elements_Data>();
            if (data.map != Maps.NONE)
            {
                spawned_map = data.map;
                //finished_map.SetMap(data.map);
            }
            if (data.unit != Monsters.NONE)
            {
                spawned_units_name[unit_idx] = data.unit;
                spawned_units_pos[unit_idx] = element.transform.position;
                is_unit_spawned[unit_idx] = true;
                unit_idx++;
            }
            if (data.trap != Traps.NONE)
            {
                spawned_traps_name[trap_idx] = data.trap;
                spawned_traps_pos[trap_idx] = element.transform.position;
                is_trap_spawned[trap_idx] = true;
                trap_idx++;

            }
        }

        int[] unitInts = spawned_units_name.Select(u => (int)u).ToArray();
        int[] trapInts = spawned_traps_name.Select(u => (int)u).ToArray();

        UnitLoaderController.Instance.photonView.RPC("AddMapDM", Photon.Pun.RpcTarget.All, spawned_map, unitInts, spawned_units_pos, is_unit_spawned, trapInts, spawned_traps_pos, is_trap_spawned);
    }

}




public enum DC_State { NONE, BUTTON, PLACING_UNIT, PLAICING_TRAP}
public enum Maps { NONE, MAP_NAME1, MAP_NAME2}
[Serializable]
public enum Monsters { NONE, GOBLIN, HOBGOBLIN, SHAMAN, CHAMPION}
[Serializable]
public enum Traps { NONE, /*TRAP_NAME1, TRAP_NAME2*/}

public class Playable_Map
{
    Maps map = Maps.NONE;
    List<DC_Unit> units = new List<DC_Unit>();
    List<DC_Trap> traps = new List<DC_Trap>();

    public Maps MAP => map;
    public List<DC_Unit> UNITS => units;
    public List<DC_Trap> TRAPS => traps;
    public Maps Actual_map => map;
    public void SetMap(Maps new_map)
    {
        map = new_map;
    }
    public void SetUnits(List<DC_Unit> new_units)
    {
        units = new_units;
    }

    public void AddUnit(DC_Unit new_unit)
    {
        units.Add(new_unit);
    }
    public void RemoveUnit(DC_Unit old_unit)
    {
        units.Remove(old_unit);
    }

    public void SetTraps(List<DC_Trap> new_traps)
    {
        traps = new_traps;
    }
    public void AddTrap(DC_Trap new_trap)
    {
        traps.Add(new_trap);
    }
    public void RemoveTrap(DC_Trap old_trap)
    {
        traps.Remove(old_trap);
    }
}

public struct DC_Unit
{
    public UnitData unit_type;
    public Vector3 pos;
}
public struct DC_Trap
{
    public Traps trap_type;
    public Vector3 pos;
}