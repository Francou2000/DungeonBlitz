using System;
using TMPro;
using UnityEngine;

public class DC_Create_Unit_Trap_Button : MonoBehaviour
{
    [SerializeField] GameObject button_to_spawn;

    void Start()
    {
        foreach (Monsters unit in Enum.GetValues(typeof(Monsters)))
        {
            if ((int)unit == 0) continue;
            var new_button = Instantiate(button_to_spawn, transform);
            new_button.GetComponent<DC_Buttons>().unit_to_select = unit;
            new_button.GetComponent<DC_Buttons>().state_to_change = DC_State.PLACING_UNIT;
            new_button.GetComponentInChildren<TextMeshProUGUI>().text = Enum.GetName(typeof(Monsters), unit);
        }
        
        foreach (Traps trap in Enum.GetValues(typeof(Traps)))
        {
            if ((int)trap == 0) continue;
            var new_button = Instantiate(button_to_spawn, transform);
            new_button.GetComponent<DC_Buttons>().trap_to_select = trap;
            new_button.GetComponent<DC_Buttons>().state_to_change = DC_State.PLAICING_TRAP;
            new_button.GetComponentInChildren<TextMeshProUGUI>().text = Enum.GetName(typeof(Traps), trap);
        }
    }
}
