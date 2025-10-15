using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ActionButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private TMP_Text costText; // AP cost or ADR change

    public UnitAbility Ability { get; private set; }
    public System.Action<ActionButtonView> OnClick;
    public System.Action<ActionButtonView, PointerEventData> OnHover;
    public System.Action OnUnhover;

    public bool IsMove { get; private set; }

    public void Bind(UnitAbility ability, Sprite iconSprite, int apCost)
    {
        IsMove = false;
        Ability = ability;
        if (icon) icon.sprite = iconSprite;
        if (costText) costText.text = apCost > 0 ? apCost.ToString() : "";
        SetSelected(false);
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnClick?.Invoke(this));
        }
    }

    public void BindMove(Sprite iconSprite, int apCost = 0)
    {
        IsMove = true;
        Ability = null;
        if (icon) icon.sprite = iconSprite;
        if (costText) costText.text = apCost > 0 ? apCost.ToString() : ""; // show cost if you want
        SetSelected(false);
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnClick?.Invoke(this));
        }
    }

    public void SetSelected(bool v) { if (selectedFrame) selectedFrame.SetActive(v); }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e)
    {
        Debug.Log($"[Hover ENTER] {(IsMove ? "Move" : Ability?.name)}");
        OnHover?.Invoke(this, e);
    }

    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e)
    {
        Debug.Log($"[Hover EXIT]");
        OnUnhover?.Invoke();
    }
}
