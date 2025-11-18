using UnityEngine;

public class StormCrossingZone : ZoneBase
{
    // Segment endpoints (XZ plane)
    public Vector3 A;
    public Vector3 B;
    public float halfWidth;

    // Owner faction (compare to unit.Model.Faction)
    public UnitFaction ownerFaction;      

    // Effects
    public int allyHasteDuration = 1;
    public int enemyDamage = 10;
    [Range(0, 100)] public int enemyShockChance = 25;

    private LineRenderer _border;
    [SerializeField] private float _borderWidth = 0.06f;

    public void InitSegment(Vector3 a, Vector3 b, float width, UnitFaction ownerFaction,
                            int hasteDur, int dmg, int shockChance, int remainingTurns)
    {
        A = a; B = b;
        halfWidth = Mathf.Max(0.05f, width * 0.5f);
        this.ownerFaction = ownerFaction;
        allyHasteDuration = Mathf.Max(0, hasteDur);
        enemyDamage = Mathf.Max(0, dmg);
        enemyShockChance = Mathf.Clamp(shockChance, 0, 100);
        base.Init(ZoneKind.StormCrossing, (a + b) * 0.5f, width, remainingTurns);
        name = "StormCrossingZone";

        // Storm uses its own rectangle — hide the circle ring built by base

        var ring = transform.Find("Ring");
        if (ring) ring.gameObject.SetActive(false);

        BuildOrUpdateBorder();
    }

    // Movement crossing test (XZ)
    public bool IsCrossing(Vector3 from, Vector3 to)
    {
        Vector2 A2 = new Vector2(A.x, A.z);
        Vector2 B2 = new Vector2(B.x, B.z);
        Vector2 d = (B2 - A2);
        float len = d.magnitude;
        if (len < 1e-4f) return false;
        d /= len;

        Vector2 P0 = new Vector2(from.x, from.z) - A2;
        Vector2 P1 = new Vector2(to.x, to.z) - A2;

        // signed distance to line via 2D cross
        float s0 = (d.x * P0.y - d.y * P0.x);
        float s1 = (d.x * P1.y - d.y * P1.x);

        // distances from strip center
        float dist0 = Mathf.Abs(s0);
        float dist1 = Mathf.Abs(s1);

        // Must pass through the strip (within halfWidth) AND change side (cross sign)
        bool insideStripAtLeastOnce = (dist0 <= halfWidth) || (dist1 <= halfWidth);
        bool crossesSides = (s0 == 0f || s1 == 0f) ? insideStripAtLeastOnce : (s0 * s1 < 0f);

        if (!crossesSides) return false;

        // also require projection to be within segment extents
        float t0 = Vector2.Dot(P0, d); // along-line coordinate (A=0..len)
        float t1 = Vector2.Dot(P1, d);
        bool spansSegment = (Mathf.Max(t0, t1) >= 0f) && (Mathf.Min(t0, t1) <= len);

        return spansSegment && insideStripAtLeastOnce;
    }

    private void BuildOrUpdateBorder()
    {
        if (_border == null)
        {
            var go = new GameObject("Border");
            go.transform.SetParent(transform, false);
            _border = go.AddComponent<LineRenderer>();
            _border.loop = true;
            _border.useWorldSpace = true;
            _border.widthMultiplier = _borderWidth;
            _border.material = new Material(Shader.Find("Sprites/Default"));

            var g = new Gradient();
            Color c = new Color(1f, 0.9f, 0.3f, 0.95f); // electric yellow
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a, 1f) });
            _border.colorGradient = g;
        }

        // rectangle corners around segment A-B with halfWidth
        Vector3 dir = (B - A);
        if (dir.sqrMagnitude < 1e-6f) dir = Vector3.right;
        dir.Normalize();
        Vector3 n = new Vector3(-dir.y, dir.x, dir.z) * halfWidth;

        Vector3 p0 = A + n;
        Vector3 p1 = B + n;
        Vector3 p2 = B - n;
        Vector3 p3 = A - n;

        _border.positionCount = 4;
        _border.SetPositions(new[] { p0, p1, p2, p3 });
    }
}
