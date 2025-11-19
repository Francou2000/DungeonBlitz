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
    [HideInInspector] public int NetId = -1;

    public bool IsExpired() => RemainingTurns == 0;

    private LineRenderer _ring;
    [SerializeField] private int _segments = 64;     // visual smoothness
    [SerializeField] private float _lineWidth = 0.06f;

    private void BuildOrUpdateRing()
    {
        if (_ring == null)
        {
            var go = new GameObject("Ring");
            go.transform.SetParent(transform, false);
            _ring = go.AddComponent<LineRenderer>();
            _ring.loop = true;
            _ring.useWorldSpace = false;
            _ring.widthMultiplier = _lineWidth;
            _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.receiveShadows = false;

            // simple material
            _ring.material = new Material(Shader.Find("Sprites/Default"));

            // color per zone kind
            var g = new Gradient();
            Color c = Color.white;
            switch (Kind)
            {
                case ZoneKind.Negative: c = new Color(0.3f, 1f, 0.8f, 0.9f); break; // mint/teal
                case ZoneKind.Frozen: c = new Color(0.6f, 0.9f, 1f, 0.9f); break; // icy blue
                case ZoneKind.StormCrossing: c = new Color(1f, 0.9f, 0.3f, 0.9f); break; // yellow (border only on subclass)
            }
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a, 1f) });
            _ring.colorGradient = g;
        }

        // (Re)build circle points
        _ring.positionCount = _segments;
        float step = Mathf.PI * 2f / _segments;
        for (int i = 0; i < _segments; i++)
        {
            float a = step * i;
            _ring.SetPosition(i, new Vector3(Mathf.Cos(a) * Radius, Mathf.Sin(a) * Radius, 0f));
        }
    }

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

        BuildOrUpdateRing();
    }

    private void LateUpdate()
    {
        // keep ring centered if something moves the root
        if (_ring != null) _ring.transform.localPosition = Vector3.zero;
    }


    public bool Contains(Vector3 point)
    {
        return Vector3.Distance(point, Center) <= Radius + 0.01f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position == Vector3.zero ? Center : transform.position, Radius);
    }
}
