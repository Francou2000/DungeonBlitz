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
             $"{model.unitName}\n" +
        $"HP: {model.currentHP} / {model.maxHP}\n" +
        $"AC: {model.currentActions} / {model.actionsPerTurn}\n" +
        $"RC: {model.currentReactions} / {model.reactionsPerTurn}\n" +
        $"Perf: {model.performance}   Aff: {model.affinity}\n" +
        $"STR: {model.strength}   MP: {model.magicPower}\n" +
        $"AR: {model.armor}   MR: {model.magicResistance}\n" +
        $"Anxiety: {model.anxiety}";
    }
}
