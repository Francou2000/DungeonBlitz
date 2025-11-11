using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHighlighter : Button
{
    TextHighlighter text_effect;
    public TextHighlighter My_text { set { text_effect = value; } }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant); // deja que el Sprite Swap funcione normal

        if (text_effect == null)
            return;

        switch (state)
        {
            case SelectionState.Normal:
                text_effect.MainText();
                break;
            case SelectionState.Highlighted:
                text_effect.HoverText();
                break;
            case SelectionState.Pressed:
                text_effect.PressedText();
                break;
            case SelectionState.Disabled:
                break;
            default:
                break;
        }
    }

    public void ResetStateTransition()
    {
        DoStateTransition(SelectionState.Normal, true);
    }
}

