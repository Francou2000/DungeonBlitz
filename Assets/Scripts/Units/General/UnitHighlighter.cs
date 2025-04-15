using Unity.VisualScripting;
using UnityEngine;

public class UnitHighlighter : MonoBehaviour
{
    private Unit hoveredUnit;

    public GameObject tooltipPrefab;
    private UnitTooltip currentTooltip;


    void Update()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
        Unit hitUnit = hit.collider != null ? hit.collider.GetComponent<Unit>() : null;

        if (hoveredUnit != null && hoveredUnit != hitUnit)
        {
            hoveredUnit.View.SetHighlighted(false);
            Destroy(currentTooltip?.gameObject);
            currentTooltip = null;
            hoveredUnit = null;
        }

        if (hitUnit != null && hitUnit != hoveredUnit)
        {
            hoveredUnit = hitUnit;
            hoveredUnit.View.SetHighlighted(true);

            GameObject tooltipObj = Instantiate(tooltipPrefab, FindFirstObjectByType<Canvas>().transform);
            currentTooltip = tooltipObj.GetComponent<UnitTooltip>();
            currentTooltip.AttachToUnit(hoveredUnit);
        }
    }
}
