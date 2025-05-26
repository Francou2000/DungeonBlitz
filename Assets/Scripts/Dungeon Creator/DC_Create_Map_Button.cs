using System;
using TMPro;
using Unity.VisualScripting.ReorderableList.Element_Adder_Menu;
using UnityEngine;

public class DC_Create_Map_Button : MonoBehaviour
{

    [SerializeField] GameObject button_to_spawn;

    void Start()
    {
        foreach (Maps mapa in Enum.GetValues(typeof(Maps)))
        {
            if ((int)mapa == 0) continue;
            var new_button = Instantiate(button_to_spawn, transform);
            new_button.GetComponent<DC_Buttons>().map_to_select = mapa;
            new_button.GetComponentInChildren<TextMeshProUGUI>().text = Enum.GetName(typeof(Maps), mapa);
        }
    }

}
