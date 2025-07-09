using UnityEngine;
using UnityEngine.UI;

public class DC_Buttons : MonoBehaviour
{
    public DC_State state_to_change;
    public Maps map_to_select;
    public Monsters unit_to_select;
    public Traps trap_to_select;
    public GameObject menu_to_active;
    public GameObject menu_to_deactive;
    Button my_button;


    void Start()
    {
        my_button = GetComponent<Button>();
        if (map_to_select != Maps.NONE)
        {
            my_button.onClick.AddListener(map_on_click_button);
        }
        else if (unit_to_select != Monsters.NONE)
        {
            my_button.onClick.AddListener(unit_on_click_button);
        }
        else if (trap_to_select != Traps.NONE)
        {
            Debug.Log("asldfkj");
            my_button.onClick.AddListener(trap_on_click_button);
        }
        else
        {
            my_button.onClick.AddListener(change_panel);
        }

    }

    void map_on_click_button()
    {
        Dungeon_Creator_Manager.Instance.change_map(map_to_select);
    }
    void unit_on_click_button()
    {
        Dungeon_Creator_Manager.Instance.select_unit(unit_to_select, state_to_change);
    }
    void trap_on_click_button()
    {
        Dungeon_Creator_Manager.Instance.select_trap(trap_to_select, state_to_change);
    }
    void change_panel()
    {
        menu_to_active.SetActive(true);
        menu_to_deactive.SetActive(false);
    }

}
