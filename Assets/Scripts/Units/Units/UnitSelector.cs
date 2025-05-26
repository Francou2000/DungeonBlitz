using UnityEngine;

public class UnitSelector : MonoBehaviour
{
    private UnitController currentActiveUnit;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Check if clicked on a UI element
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            Vector3 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 clickPos2D = new Vector2(clickPos.x, clickPos.y);

            RaycastHit2D hit = Physics2D.Raycast(clickPos2D, Vector2.zero);
            if (hit.collider != null)
            {
                Unit clickedUnit = hit.collider.GetComponent<Unit>();
                if (clickedUnit != null && clickedUnit.Controller != null && clickedUnit.Controller.isControllable && clickedUnit.Model.Faction == UnitFaction.Monster)
                {
                    SetActiveUnit(clickedUnit.Controller);
                }
            }
        }
    }

    void SetActiveUnit(UnitController newUnit)
    {
        if (currentActiveUnit != null)
        {
            currentActiveUnit.unit.View.SetHighlighted(false);
        }

        currentActiveUnit = newUnit;
        UnitController.ActiveUnit = newUnit;

        currentActiveUnit.unit.View.SetHighlighted(true);

        Debug.Log($"[Selector] Active unit is now: {currentActiveUnit.unit.Model.UnitName}");
    }
}