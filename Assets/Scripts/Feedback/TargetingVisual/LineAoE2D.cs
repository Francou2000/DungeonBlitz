using UnityEngine;
[RequireComponent(typeof(LineRenderer))]
public class LineAoE2D : MonoBehaviour
{
    LineRenderer lr;
    void Awake() { lr = GetComponent<LineRenderer>(); lr.useWorldSpace = true; }
    public void Draw(Vector3 origin, Vector3 dir, float length)
    {
        dir = dir.sqrMagnitude < 0.0001f ? Vector3.right : dir.normalized;
        lr.positionCount = 2;
        lr.SetPosition(0, origin);
        lr.SetPosition(1, origin + dir * length);
    }
}
