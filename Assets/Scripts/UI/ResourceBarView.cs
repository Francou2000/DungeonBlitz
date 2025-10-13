using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceBarView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fill;        // Image -> Type=Filled, Method=Horizontal
    [SerializeField] private TMP_Text text;     // e.g., "120/180" or "45 ADR"
    [SerializeField] private Gradient colorByPct;
    [SerializeField] private bool showFraction = true;
    [SerializeField] private string suffix = "";  // e.g. "ADR"

    public void Set(int current, int max)
    {
        max = Mathf.Max(1, max);
        float pct = Mathf.Clamp01((float)current / max);
        if (fill)
        {
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = pct;
            if (colorByPct != null) fill.color = colorByPct.Evaluate(pct);
        }
        if (text)
        {
            text.text = showFraction ? $"{current}/{max}" : $"{current}{(string.IsNullOrEmpty(suffix) ? "" : $" {suffix}")}";
        }
    }
}