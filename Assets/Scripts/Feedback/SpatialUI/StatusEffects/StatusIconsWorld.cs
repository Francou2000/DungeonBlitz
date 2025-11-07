using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class StatusIconsWorld : MonoBehaviour
{
    [Header("Binding")]
    [SerializeField] Unit targetUnit;                       // bound at runtime
    [SerializeField] StatusIconLibrary library;             // assign in prefab
    [SerializeField] Image iconPrefab;                      // small square Image
    [SerializeField] RectTransform container;               // HorizontalLayoutGroup

    [Header("Layout")]
    public Vector3 worldOffset = new(0f, 2.05f, 0f);        // a bit above health bar
    public float extraHeight = 0f;                          // if using renderer bounds
    public bool useRendererBounds = true;

    Camera _cam;
    Transform _follow;
    Renderer _rend;
    readonly Dictionary<StatusType, Image> _active = new();

    float _refreshTimer;

    void Awake() { _cam = Camera.main; }
    public void Bind(Unit u)
    {
        targetUnit = u;
        Debug.Log($"[StatusIconsWorld] Bind -> {u.name}");
        Hook();
    }

    void OnEnable() { Hook(); }
    void OnDisable() { Unhook(); }

    void Hook()
    {
        if (!targetUnit) return;
        _follow = targetUnit.transform;
        _rend = targetUnit.GetComponentInChildren<Renderer>();
        var sc = targetUnit.GetComponent<StatusComponent>();
        if (!sc) return;

        // subscribe
        sc.OnEffectApplied += OnApplied;         
        sc.OnEffectRemoved += OnRemoved;         

        // paint current
        foreach (var e in sc.ActiveEffects) OnApplied(e);
    }

    void Unhook()
    {
        if (!targetUnit) return;
        var sc = targetUnit.GetComponent<StatusComponent>();
        if (sc != null)
        {
            sc.OnEffectApplied -= OnApplied;
            sc.OnEffectRemoved -= OnRemoved;
        }
        foreach (var kv in _active) if (kv.Value) Destroy(kv.Value.gameObject);
        _active.Clear();
    }

    void LateUpdate()
    {
        if (!_follow) { gameObject.SetActive(false); return; }
        Vector3 p = _follow.position;
        if (useRendererBounds && _rend) { var b = _rend.bounds; p = new Vector3(b.center.x, b.max.y, 0f); }
        p.y += worldOffset.y + extraHeight; p.z = 0f;
        transform.position = p;

        _refreshTimer += Time.deltaTime;
        if (_refreshTimer > 0.25f) { _refreshTimer = 0f; RefreshFromList(); }
    }

    void OnApplied(StatusEffect e)
    {
        Debug.Log($"[StatusIconsWorld] OnApplied {e.type} -> {targetUnit?.name}");
        var t = e.type;
        if (_active.ContainsKey(t)) { Bump(_active[t]); return; }

        var sprite = library ? library.Get(t) : null;
        if (!sprite) { Debug.Log($"[StatusIconsWorld] No sprite for {t}"); return; }

        var img = Instantiate(iconPrefab, container);
        img.sprite = sprite;
        img.gameObject.SetActive(true);
        _active[t] = img;
    }

    void OnRemoved(StatusEffect e)
    {
        Debug.Log($"[StatusIconsWorld] OnRemoved {e.type} -> {targetUnit?.name}");
        if (_active.TryGetValue(e.type, out var img) && img) Destroy(img.gameObject);
        _active.Remove(e.type);
    }

    void Bump(Image img)
    {
        if (!img) return;
        StopCoroutine(nameof(BumpCo));
        StartCoroutine(BumpCo(img.rectTransform));
    }

    System.Collections.IEnumerator BumpCo(RectTransform rt)
    {
        float t = 0f;
        const float dur = 0.12f;
        Vector3 a = Vector3.one * 1.15f;
        Vector3 b = Vector3.one;

        rt.localScale = a;
        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            rt.localScale = Vector3.Lerp(a, b, u);
            yield return null;
        }
        rt.localScale = b;
    }

    public void RefreshFromList()
    {
        if (!targetUnit) return;
        var sc = targetUnit.GetComponent<StatusComponent>();
        if (sc == null) return;

        // build current set
        var wanted = new HashSet<StatusType>();
        foreach (var e in sc.ActiveEffects) wanted.Add(e.type);

        // remove stale
        var toRemove = new List<StatusType>();
        foreach (var kv in _active) if (!wanted.Contains(kv.Key)) toRemove.Add(kv.Key);
        foreach (var t in toRemove) { if (_active.TryGetValue(t, out var img) && img) Destroy(img.gameObject); _active.Remove(t); }

        // add missing
        foreach (var e in sc.ActiveEffects)
        {
            if (_active.ContainsKey(e.type)) continue;
            var sprite = library ? library.Get(e.type) : null;
            if (!sprite) { Debug.Log($"[Icons] No sprite mapped for {e.type}"); continue; }
            var img = Instantiate(iconPrefab, container);
            img.sprite = sprite;
            img.gameObject.SetActive(true);
            _active[e.type] = img;
            Debug.Log($"[Icons] Refresh add {e.type} -> {targetUnit.name}");
        }
    }
}