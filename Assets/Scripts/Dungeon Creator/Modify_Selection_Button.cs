using UnityEngine;
using UnityEngine.UI;

public class Modify_Selection_Button : MonoBehaviour
{
    public bool should_move = false;
    public bool should_remove = false;
    Button my_button;

    public GameObject actual_selection;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        my_button = GetComponent<Button>();

        if (should_move) my_button.onClick.AddListener(MoveSelection);
        if (should_remove) my_button.onClick.AddListener(RemoveSelection);
    }

    public void MoveSelection()
    {
        actual_selection.GetComponent<Follow_Mouse_for_Placing>().follow_mouse = true;
        DC_Elements_Data dC_Elements_Data = actual_selection.GetComponent<DC_Elements_Data>();
        if (dC_Elements_Data.unit != Units.NONE) Dungeon_Creator_Manager.Instance.dc_state = DC_State.PLACING_UNIT;
        if (dC_Elements_Data.trap != Traps.NONE) Dungeon_Creator_Manager.Instance.dc_state = DC_State.PLAICING_TRAP;
        Dungeon_Creator_Manager.Instance.actual_prefab = actual_selection;
    }

    public void RemoveSelection()
    {
        Destroy(actual_selection);
        transform.parent.gameObject.SetActive(false);
    }
}
