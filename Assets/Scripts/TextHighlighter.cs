using TMPro;
using UnityEngine;

public class TextHighlighter : MonoBehaviour
{
    ButtonHighlighter buttonHighlighter;
    TextMeshProUGUI textMeshPro;

    private Material mat;

    public Color hoverColor;
    public float hoverIntensity;
    void Start()
    {
        textMeshPro = GetComponent<TextMeshProUGUI>();
        buttonHighlighter = GetComponentInParent<ButtonHighlighter>();
        buttonHighlighter.My_text = this;


        textMeshPro.fontMaterial = new Material(textMeshPro.fontSharedMaterial);
        mat = textMeshPro.fontMaterial;
        mat.EnableKeyword("UNDERLAY_ON");
        MainText();
    }

    public void MainText()
    {
        mat.SetColor(ShaderUtilities.ID_UnderlayColor, Color.green);
    }

    public void HoverText()
    {
        mat.SetColor(ShaderUtilities.ID_UnderlayColor, hoverColor + new Color(0, 0, 0, hoverIntensity));
    }

    public void PressedText()
    {
        mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 1, 0, 0));
    }
}
