using UnityEngine;

public class UnitHighlighter : MonoBehaviour
{
    private Unit hoveredUnit;

    void Update()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

        Unit hitUnit = hit.collider != null ? hit.collider.GetComponent<Unit>() : null;

        if (hoveredUnit != null && hoveredUnit != hitUnit)
        {
            hoveredUnit.View.SetHighlighted(false);
            hoveredUnit = null;
        }

        if (hitUnit != null && hitUnit != hoveredUnit)
        {
            hitUnit.View.SetHighlighted(true);
            hoveredUnit = hitUnit;
        }
    }
}
