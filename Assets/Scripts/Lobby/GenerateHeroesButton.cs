using System;
using TMPro;
using UnityEngine;

public class GenerateHeroesButton : MonoBehaviour
{

    [SerializeField] GameObject button_to_spawn;

    void Start()
    {
        foreach (HeroesList heroe in Enum.GetValues(typeof(HeroesList)))
        {

            //if ((int)heroe == 0) continue;
            var new_button = Instantiate(button_to_spawn, transform);
            new_button.GetComponent<SelectHeroeButton>().my_heroe = heroe;
            //new_button.GetComponent<SelectHeroeButton>().state_to_change = DC_State.PLACING_UNIT;
            new_button.GetComponentInChildren<TextMeshProUGUI>().text = Enum.GetName(typeof(HeroesList), heroe);
        }
        
    }

}
