using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpdateHeroeInfo_Selector : MonoBehaviour
{
    [SerializeField] UnitData unit_data;
    [SerializeField] TextMeshProUGUI stats_text;
    [SerializeField] TextMeshProUGUI name_text;
    Button my_button;
    string[] stats;

    void Start()
    {
        my_button = GetComponent<Button>();
        my_button.onClick.AddListener(ChangeInfo);
        stats = new string[8]
        {
            unit_data.maxHP             == 0 ? "None" : unit_data.maxHP             <= 15   ? "Low" : unit_data.maxHP             <= 25  ? "Medium" : unit_data.maxHP             <= 35  ? "High" : "Very High",
            unit_data.performance       == 0 ? "None" : unit_data.performance       <= 1    ? "Low" : unit_data.performance       <= 1.4 ? "Medium" : unit_data.performance       <= 1.8 ? "High" : "Very High",
            "None",
            unit_data.armor             == 0 ? "None" : unit_data.armor             <= 20   ? "Low" : unit_data.armor             <= 40  ? "Medium" : unit_data.armor             <= 60  ? "High" : "Very High",
            unit_data.magicResistance   == 0 ? "None" : unit_data.magicResistance   <= 20   ? "Low" : unit_data.magicResistance   <= 40  ? "Medium" : unit_data.magicResistance   <= 60  ? "High" : "Very High",
            unit_data.strength          == 0 ? "None" : unit_data.strength          <= 3    ? "Low" : unit_data.strength          <= 6   ? "Medium" : unit_data.strength          <= 8   ? "High" : "Very High",
            unit_data.magicPower        == 0 ? "None" : unit_data.magicPower        <= 3    ? "Low" : unit_data.magicPower        <= 6   ? "Medium" : unit_data.magicPower        <= 8   ? "High" : "Very High",
            "None"
        };
    }
    void ChangeInfo()
    {
        name_text.text = unit_data.name;
        stats_text.text =    "LifePoints: \t\t"  + stats[0] + "\n" +
                            "Performance: \t"   + stats[1] + "\n" +
                            "Affinity: \t\t"    + stats[2] + "\n" +
                            "Armor: \t\t"       + stats[3] + "\n" +
                            "Magic Resist: \t"  + stats[4] + "\n" +
                            "Strenght: \t\t"    + stats[5] + "\n" +
                            "Magic Power: \t"   + stats[6] + "\n" +
                            "Adrenaline: \t\t"  + stats[7];
    }

}
