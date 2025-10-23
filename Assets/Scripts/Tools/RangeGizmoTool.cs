using UnityEngine;

[System.Serializable]
public class RangeVisualization
{
    [Header("Range Settings")]
    public float range = 1f;
    public Color rangeColor = Color.yellow;
    public bool showRange = true;
    
    [Header("Line Range Settings")]
    public float lineRange = 3f;
    public Color lineColor = Color.red;
    public bool showLineRange = true;
    
    [Header("Circle Range Settings")]
    public float circleRadius = 2f;
    public Color circleColor = Color.blue;
    public bool showCircleRange = true;
}

public class RangeGizmoTool : MonoBehaviour
{
    [Header("Range Visualizations")]
    public RangeVisualization[] ranges = new RangeVisualization[1];
    
    [Header("Settings")]
    public bool showInGame = false;
    public float gizmoAlpha = 0.3f;
    
    void OnDrawGizmos()
    {
        if (!showInGame) return;
        
        Vector3 center = transform.position;
        
        foreach (var range in ranges)
        {
            if (range.showRange)
            {
                // Dibujar rango básico (círculo)
                Gizmos.color = new Color(range.rangeColor.r, range.rangeColor.g, range.rangeColor.b, gizmoAlpha);
                Gizmos.DrawWireSphere(center, range.range);
                
                // Dibujar círculo sólido para mejor visualización
                Gizmos.color = new Color(range.rangeColor.r, range.rangeColor.g, range.rangeColor.b, gizmoAlpha * 0.1f);
                Gizmos.DrawSphere(center, range.range);
            }
            
            if (range.showLineRange)
            {
                // Dibujar rango de línea (hacia adelante)
                Gizmos.color = new Color(range.lineColor.r, range.lineColor.g, range.lineColor.b, gizmoAlpha);
                Vector3 lineEnd = center + transform.right * range.lineRange;
                Gizmos.DrawLine(center, lineEnd);
                
                // Dibujar flecha al final
                Vector3 arrowSize = Vector3.right * 0.2f;
                Gizmos.DrawLine(lineEnd, lineEnd - arrowSize + Vector3.up * 0.1f);
                Gizmos.DrawLine(lineEnd, lineEnd - arrowSize - Vector3.up * 0.1f);
            }
            
            if (range.showCircleRange)
            {
                // Dibujar rango de círculo (AoE)
                Gizmos.color = new Color(range.circleColor.r, range.circleColor.g, range.circleColor.b, gizmoAlpha);
                Gizmos.DrawWireSphere(center, range.circleRadius);
                
                // Dibujar círculo sólido para mejor visualización
                Gizmos.color = new Color(range.circleColor.r, range.circleColor.g, range.circleColor.b, gizmoAlpha * 0.1f);
                Gizmos.DrawSphere(center, range.circleRadius);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Mostrar siempre cuando está seleccionado
        Vector3 center = transform.position;
        
        foreach (var range in ranges)
        {
            if (range.showRange)
            {
                Gizmos.color = range.rangeColor;
                Gizmos.DrawWireSphere(center, range.range);
            }
            
            if (range.showLineRange)
            {
                Gizmos.color = range.lineColor;
                Vector3 lineEnd = center + transform.right * range.lineRange;
                Gizmos.DrawLine(center, lineEnd);
                
                // Flecha
                Vector3 arrowSize = Vector3.right * 0.2f;
                Gizmos.DrawLine(lineEnd, lineEnd - arrowSize + Vector3.up * 0.1f);
                Gizmos.DrawLine(lineEnd, lineEnd - arrowSize - Vector3.up * 0.1f);
            }
            
            if (range.showCircleRange)
            {
                Gizmos.color = range.circleColor;
                Gizmos.DrawWireSphere(center, range.circleRadius);
            }
        }
    }
    
    [ContextMenu("Add New Range")]
    void AddNewRange()
    {
        System.Array.Resize(ref ranges, ranges.Length + 1);
        ranges[ranges.Length - 1] = new RangeVisualization();
    }
    
    [ContextMenu("Remove Last Range")]
    void RemoveLastRange()
    {
        if (ranges.Length > 1)
        {
            System.Array.Resize(ref ranges, ranges.Length - 1);
        }
    }
}
