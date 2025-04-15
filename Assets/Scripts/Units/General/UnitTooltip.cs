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
            $"STR: {model.strength}  SPD: {model.speed}\n" +
            $"DEF: {model.physicalDefense}  MAG DEF: {model.magicalDefense}";
    }
}
