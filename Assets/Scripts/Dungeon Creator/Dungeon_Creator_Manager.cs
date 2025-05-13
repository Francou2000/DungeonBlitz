using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UIElements;

public class Dungeon_Creator_Manager : MonoBehaviour
{
    
    Vector3 mouse_position = new Vector3();
    //List<map> map_list = new List<map>();
    Maps selected_map = Maps.NONE;
    List<DC_Unit> unit_llist = new List<DC_Unit>();
    Units selected_unit = Units.NONE;
    List<DC_Trap> trap_list = new List<DC_Trap>();
    Traps selected_trap = Traps.NONE;
    DC_State dc_state = DC_State.NONE;
    Playable_Map finished_map = new Playable_Map();

    public Maps Selected_map { get { return selected_map; } set { selected_map = value; } }
    public Units Selected_unit { get { return selected_unit; } set { selected_unit = value; } }
    public Traps Selected_trap { get { return selected_trap; } set { selected_trap = value; } }
    public DC_State DC_state { get { return dc_state; } set { dc_state = value; } }


    void Start()
    {
        
    }

    void Update()
    {
        mouse_position = Input.mousePosition;
    }

    void on_mouse_clicked()
    {
        if (dc_state == DC_State.PLACING_UNIT)
        {
            DC_Unit new_unit = new DC_Unit();
            new_unit.unit_type = selected_unit;
            new_unit.pos = mouse_position;
            finished_map.AddUnit(new_unit);
        }
        if (dc_state == DC_State.PLAICING_TRAP)
        {
            DC_Trap trap = new DC_Trap();
            trap.trap_type = selected_trap;
            trap.pos = mouse_position;
            finished_map.AddTrap(trap);
        }
    }
    void add_unit()
    {
        //finished_map.units.Add(selected_unit);
    }
    void remove_unit() { }
    void add_trap() { }
    void remove_trap() { }
    void change_map(Maps new_map)
    {
        selected_map = new_map;
    }
    void save_map() { }

}




public enum DC_State { BUTTON, PLACING_UNIT, PLAICING_TRAP, NONE}
public enum Maps {MAP_NAME1, MAP_NAME2, ETC, NONE}
public enum Units {UNIT_NAME1, UNIT_NAME2, ETC, NONE}
public enum Traps {TRAP_NAME1, TRAP_NAME2, ETC, NONE}

public struct Playable_Map
{
    Maps map;
    List<DC_Unit> units;
    List<DC_Trap> traps;

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