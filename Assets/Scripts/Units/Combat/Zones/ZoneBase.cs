using UnityEngine;
public enum ZoneKind
{
    Negative = 0,
    Frozen = 1,
    StormCrossing = 2
}

public class ZoneBase : MonoBehaviour
{
    [HideInInspector] public ZoneKind Kind;
    [HideInInspector] public Vector3 Center;
    [HideInInspector] public float Radius;
    [HideInInspector] public int RemainingTurns;      // 0 => infinite
    [HideInInspector] public int OwnerViewId = -1;

    public bool IsExpired() => RemainingTurns == 0;

    public virtual void Init(ZoneKind kind, Vector3 center, float radius, int remainingTurns, int ownerViewId = -1)
    {
        Kind = kind;
        Center = center;
        Radius = Mathf.Max(0f, radius);
        RemainingTurns = Mathf.Max(0, remainingTurns);  // 0 can mean “immediate GC” (we remove in manager)
        OwnerViewId = ownerViewId;
        transform.position = center;
        transform.localScale = Vector3.one;
        gameObject.name = $"{Kind}Zone";
    }

    public bool Contains(Vector3 point)
    {
        return Vector3.Distance(point, Center) <= Radius + 0.01f;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Kind == ZoneKind.Negative ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(Center == Vector3.zero ? transform.position : Center, Radius);
    }
}
