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

    void Awake() { _cam = Camera.main; }
    public void Bind(Unit u) { targetUnit = u; Hook(); }

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
    }

    void OnApplied(StatusEffect e)
    {
        var t = e.type;
        if (_active.ContainsKey(t)) { Bump(_active[t]); return; }

        var sprite = library ? library.Get(t) : null;
        if (!sprite) return; // no icon configured → silent skip

        var img = Instantiate(iconPrefab, container);
        img.sprite = sprite;
        img.gameObject.SetActive(true);
        _active[t] = img;
    }

    void OnRemoved(StatusEffect e)
    {
        var t = e.type;
        if (_active.TryGetValue(t, out var img) && img) Destroy(img.gameObject);
        _active.Remove(t);
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
}