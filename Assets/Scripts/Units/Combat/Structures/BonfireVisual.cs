using UnityEngine;

public class BonfireVisual : MonoBehaviour
{
    public StructureBase bound;              // set by manager
    public Transform aura;                   // assign the 'Aura' child
    Vector3 _baseScale = Vector3.one;

    void Start()
    {
        if (aura) _baseScale = aura.localScale;
        ApplyRadius();
    }

    void Update() { ApplyRadius(); }

    void ApplyRadius()
    {
        if (!bound || !aura) return;
        // radius = diameter in local units 
        float d = bound.Radius * 2f;
        aura.localScale = _baseScale * d;
    }
}
