using UnityEngine;
[RequireComponent(typeof(LineRenderer))]
public class CircleAoE2D : MonoBehaviour
{
    public int segments = 64; LineRenderer lr;
    void Awake() { lr = GetComponent<LineRenderer>(); lr.loop = true; lr.useWorldSpace = true; }
    public void Draw(Vector3 center, float radius)
    {
        lr.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments * Mathf.PI * 2f;
            lr.SetPosition(i, center + new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t) * radius, 0));
        }
    }
}
