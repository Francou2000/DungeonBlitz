using UnityEngine;
using TMPro;

public class UnitTooltip : MonoBehaviour
{
    public TextMeshProUGUI text;
    private Unit unit;
    private Camera cam;

    public void AttachToUnit(Unit unit)
    {
        this.unit = unit;
        cam = Camera.main;
        UpdateStats();
    }

    void Update()
    {
        if (unit == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(unit.transform.position);
        transform.position = screenPos;

        UpdateStats();
    }

    void UpdateStats()
    {
        var model = unit.Model;

        text.text =
             $"{model.UnitName}\n" +
        $"HP: {model.CurrentHP} / {model.MaxHP}\n" +
        $"AC: {model.CurrentActions} / {model.MaxActions}\n" +
        $"RC: {model.CurrentReactions} / {model.MaxReactions}\n" +
        $"Perf: {model.Performance}   Aff: {model.Affinity}\n" +
        $"STR: {model.Strength}   MP: {model.MagicPower}\n" +
        $"AR: {model.Armor}   MR: {model.MagicResistance}\n" +
        $"Anxiety: {model.Anxiety}";
    }
}
