using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class SetUnitDataButton : MonoBehaviour
{
    [SerializeField] UnitData my_data;
    [SerializeField] TextMeshProUGUI my_stats;
    [SerializeField] TextMeshProUGUI my_pop;
    [SerializeField] TextMeshProUGUI my_cost;
    [SerializeField] TextMeshProUGUI my_details;
    [SerializeField] Image my_visual;


    void Start()
    {
        if (my_data != null)
        {
            SetCost();
            SetDetails();
            SetPop();
            SetStats();
            SetVisual();
        }
        
    }

    void SetVisual()
    {
        my_visual.sprite = my_data.portrait_foto;
    }

    void SetStats()
    {
        int[] stats = new int[8]
        {
            my_data.maxHP,
            my_data.performance,
            my_data.affinity,
            my_data.baseAdrenaline,
            my_data.strength,
            my_data.magicPower,
            my_data.armor,
            my_data.magicResistance
        };

        my_stats.text = my_data.name + "\n" +
                        "Lp:\t" + stats[0] + "\tPerf:\t" + stats[1] + "\n" +
                        "Aff:\t" + stats[2] + "\tAdr:\t" + stats[3] + "\n" +
                        "Str:\t" + stats[4] + "\tMP:\t" + stats[5] + "\n" +
                        "Ar:\t" + stats[6] + "\tMR:\t" + stats[7] + "\n";
    }

    void SetCost()
    {
        //int pop_cost = my_data.pop_cost;
        // my_cost.text = my_data.pop_cost + " pop";
    }

    void SetPop()
    {
        my_pop.text = my_data.pop_cost + " pop";
    }

    void SetDetails()
    {
        //my_pop = my_data
    }
}
