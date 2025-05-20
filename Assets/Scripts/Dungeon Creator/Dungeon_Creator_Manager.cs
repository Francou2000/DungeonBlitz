using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class Dungeon_Creator_Manager : MonoBehaviour
{
    public static Dungeon_Creator_Manager Instance;
    
    Vector3 mouse_position      = new Vector3();
    public GameObject[] map_list;
    Units selected_unit         = Units.NONE;
    public GameObject[] unit_list;
    Traps selected_trap         = Traps.NONE;
    public GameObject[] trap_list;
    public Transform spawn_point;
    public DC_State dc_state           = DC_State.NONE;
    Playable_Map finished_map   = new Playable_Map();
    public UnityEvent PlaceSelected = new UnityEvent();

    public Units Selected_unit  { get { return selected_unit;   }   set { selected_unit = value; }  }
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
        map_list[(int)finished_map.Actual_map].gameObject.SetActive(true);
    }

    void Update()
    {
        // mouse_position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            on_mouse_clicked();
        }
        
    }

    void on_mouse_clicked()
    {
        mouse_position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (dc_state == DC_State.PLACING_UNIT)
        {
            DC_Unit new_unit = new DC_Unit();
            new_unit.unit_type = selected_unit;
            new_unit.pos = mouse_position;
            finished_map.AddUnit(new_unit);
            PlaceSelected.Invoke();
        }
        if (dc_state == DC_State.PLAICING_TRAP)
        {
            DC_Trap trap = new DC_Trap();
            trap.trap_type = selected_trap;
            trap.pos = mouse_position;
            finished_map.AddTrap(trap);
            PlaceSelected.Invoke();
        }
    }
    public void change_map(Maps new_map) {
        map_list[(int)finished_map.Actual_map].gameObject.SetActive(false);
        finished_map.SetMap(new_map);
        map_list[(int)finished_map.Actual_map].gameObject.SetActive(true);
    }
    public void select_unit(Units new_unit, DC_State new_state = DC_State.PLACING_UNIT)
    {
        selected_unit = new_unit;
        Instantiate(unit_list[(int)selected_unit], spawn_point);
        dc_state = new_state;
    }
    public void select_trap(Traps new_trap, DC_State new_state = DC_State.PLAICING_TRAP)
    {
        selected_trap = new_trap;
        Instantiate(trap_list[(int)selected_trap], spawn_point);
        dc_state = new_state;
    }
    void save_map() { }

}




public enum DC_State { NONE, BUTTON, PLACING_UNIT, PLAICING_TRAP}
public enum Maps { NONE, MAP_NAME1, MAP_NAME2}
public enum Units { NONE, UNIT_NAME1, UNIT_NAME2}
public enum Traps { NONE, TRAP_NAME1, TRAP_NAME2}

public class Playable_Map
{
    Maps map = Maps.NONE;
    List<DC_Unit> units = new List<DC_Unit>();
    List<DC_Trap> traps = new List<DC_Trap>();

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
    public Units unit_type;
    public Vector3 pos;
}
public struct DC_Trap
{
    public Traps trap_type;
    public Vector3 pos;
}