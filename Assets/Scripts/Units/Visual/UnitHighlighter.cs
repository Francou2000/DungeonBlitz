using UnityEngine;
using UnityEngine.EventSystems;

public class UnitHighlighter : MonoBehaviour
{
    [SerializeField] LayerMask unitMask = ~0;

    private Unit hoveredUnit;

    void Update()
    {
        // Don’t hover through the HUD
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
        {
            ClearHover();
            return;
        }

        var cam = Camera.main;
        if (!cam) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 p = new Vector2(mouseWorld.x, mouseWorld.y);

        // OverlapPoint is the correct 2D “hover” test
        Collider2D hit = Physics2D.OverlapPoint(p, unitMask);
        Unit hitUnit = null;
        if (hit)
        {
            // Collider may be on a child
            hitUnit = hit.GetComponent<Unit>() ?? hit.GetComponentInParent<Unit>();
        }

        // Exit old
        if (hoveredUnit && hoveredUnit != hitUnit)
        {
            hoveredUnit.View.SetHighlighted(false);
            UnitTooltip.Hide();
            hoveredUnit = null;
        }

        // Enter new
        if (hitUnit && hitUnit != hoveredUnit)
        {
            hoveredUnit = hitUnit;
            hoveredUnit.View.SetHighlighted(true);
            Debug.Log($"[Hover?Tooltip] Show {hitUnit.name}");
            UnitTooltip.Show(hitUnit);
        }
    }

    void ClearHover()
    {
        if (!hoveredUnit) return;
        hoveredUnit.View.SetHighlighted(false);
        UnitTooltip.Hide();
        hoveredUnit = null;
    }
}
