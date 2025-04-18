using UnityEngine;
using UnityEngine.UI;

public enum UnitAction
{
    None,
    Move,
    Attack
}

public class ActionUI : MonoBehaviour
{
    public static ActionUI Instance { get; private set; }

    public Button moveButton;
    public Button attackButton;
    public Button cancelButton;

    private UnitAction currentAction = UnitAction.None;

    private Color normalColor = Color.white;
    private Color selectedColor = Color.green;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        moveButton.onClick.AddListener(() => SetAction(UnitAction.Move));
        attackButton.onClick.AddListener(() => SetAction(UnitAction.Attack));
        cancelButton.onClick.AddListener(ClearAction);
    }

    public void SetAction(UnitAction action)
    {
        currentAction = action;
        UpdateButtonVisuals();
    }

    public UnitAction GetCurrentAction()
    {
        return currentAction;
    }

    public void ClearAction()
    {
        currentAction = UnitAction.None;
        UpdateButtonVisuals();
    }

    private void UpdateButtonVisuals()
    {
        UpdateButtonColor(moveButton, currentAction == UnitAction.Move);
        UpdateButtonColor(attackButton, currentAction == UnitAction.Attack);
    }

    private void UpdateButtonColor(Button button, bool isSelected)
    {
        ColorBlock colors = button.colors;

        colors.normalColor = isSelected ? selectedColor : normalColor;
        colors.highlightedColor = isSelected ? selectedColor : normalColor;
        colors.selectedColor = isSelected ? selectedColor : normalColor;

        button.colors = colors;
    }
}
