using UnityEngine;
using UnityEngine.UI;

public class HealthBarWorld : MonoBehaviour
{
    [Header("Binding")]
    public Unit targetUnit;                 // assign via Bind() or Inspector
    public Image fill;                      // foreground fill
    public Image back;                      // optional background image

    [Header("Positioning")]
    public Vector3 worldOffset = new Vector3(0f, 1.6f, 0f);  // height above head
    public bool useRendererBounds = true;   // place above sprite bounds if available
    public float extraHeight = 0.2f;        // extra above bounds

    [Header("Visuals")]
    public Gradient colorByPct;             // optional: color by HP%
    public bool hideWhenFull = false;
    public bool hideWhenDead = true;

    Camera _cam;
    Transform _follow;
    Renderer _rend;       // any child renderer (for bounds)
    int _lastHP = -1, _lastMax = -1;

    void Awake()
    {
        _cam = Camera.main;
        // Ensure we are under a world-space canvas
        var canvas = GetComponentInParent<Canvas>();
        if (!canvas) Debug.LogWarning("[HealthBarWorld] No parent Canvas found. Place bars under a world-space Canvas.");
    }

    public void Bind(Unit u)
    {
        targetUnit = u;
        if (!targetUnit) return;

        _follow = u.transform;
        _rend = u.GetComponentInChildren<Renderer>();

        // subscribe to model event (fires on ALL clients when RPC applies HP)
        if (u.Model != null)
            u.Model.OnHealthChanged += OnHealthChanged;

        ForceRefresh();
    }

    void OnHealthChanged(int current, int max) => UpdateFill(current, max);

    void LateUpdate()
    {
        if (!targetUnit || !_follow) { gameObject.SetActive(false); return; }

        // Poll if no event
        var hp = targetUnit.Model?.CurrentHP ?? 0;
        var mx = targetUnit.Model?.MaxHP ?? 1;
        if (hp != _lastHP || mx != _lastMax) UpdateFill(hp, mx);

        // Position above head (URP 2D: XY, keep Z=0)
        Vector3 anchor = _follow.position;
        if (useRendererBounds && _rend)
        {
            var b = _rend.bounds;
            anchor = new Vector3(b.center.x, b.max.y + extraHeight, 0f);
        }
        else
        {
            anchor += worldOffset;
            anchor.z = 0f;
        }
        transform.position = anchor;
    }

    void UpdateFill(int hp, int max)
    {
        _lastHP = hp; _lastMax = Mathf.Max(1, max);
        if (!fill) return;

        float pct = Mathf.Clamp01((float)hp / _lastMax);
        fill.fillAmount = pct;

        if (colorByPct != null)
            fill.color = colorByPct.Evaluate(pct);

        // Simple visibility rules
        if (hideWhenDead && hp <= 0) { SetVisible(false); return; }
        if (hideWhenFull) { SetVisible(pct < 0.999f); } else SetVisible(true);
    }

    void ForceRefresh()
    {
        var hp = targetUnit?.Model?.CurrentHP ?? 0;
        var mx = targetUnit?.Model?.MaxHP ?? 1;
        UpdateFill(hp, mx);
    }

    void SetVisible(bool v)
    {
        if (back) back.enabled = v;
        if (fill) fill.enabled = v;
    }

    void OnDestroy()
    {
        if (targetUnit && targetUnit.Model != null)
            targetUnit.Model.OnHealthChanged -= OnHealthChanged;
    }
}
