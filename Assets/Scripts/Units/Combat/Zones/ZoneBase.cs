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
    [HideInInspector] public double ExpiresAt; // PhotonNetwork.Time comparison; use Time.time if offline

    public bool IsExpired(double now) => now >= ExpiresAt;

    public virtual void Init(ZoneKind kind, Vector3 center, float radius, double expiresAt)
    {
        Kind = kind;
        Center = center;
        Radius = Mathf.Max(0f, radius);
        ExpiresAt = expiresAt;
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
