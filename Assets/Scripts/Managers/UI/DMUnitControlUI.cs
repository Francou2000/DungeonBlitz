using UnityEngine;
using UnityEngine.UI;

public class DMUnitControlUI : MonoBehaviour
{
    public Button promoteButton;

    void Start()
    {
        promoteButton.onClick.AddListener(OnPromoteButtonClicked);
    }

    void Update()
    {
        promoteButton.interactable = UnitController.ActiveUnit != null
            && UnitController.ActiveUnit.unit.Model.Faction == UnitFaction.Monster
;
    }

    void OnPromoteButtonClicked()
    {
        var activeUnit = UnitController.ActiveUnit.unit;
        if (activeUnit == null)
        {
            Debug.LogWarning("[DM UI] No active unit selected.");
            return;
        }

        if (activeUnit.Model.Faction == UnitFaction.Monster)
        {
            activeUnit.Model.TryPromote();
        }
        else
        {
            Debug.LogWarning("[DM UI] Only DM-controlled units can be promoted.");
        }
    }
}
