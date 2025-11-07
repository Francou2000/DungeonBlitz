using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "StatusIconLibrary", menuName = "UI/Status Icon Library")]
public class StatusIconLibrary : ScriptableObject
{
    [System.Serializable] public struct Entry { public StatusType type; public Sprite icon; }

    [SerializeField] private List<Entry> entries = new();
    private Dictionary<StatusType, Sprite> _cache;

    void OnEnable() { BuildCache(); }
    void OnValidate() { BuildCache(); }

    void BuildCache()
    {
        _cache = new();
        foreach (var e in entries) if (e.icon) _cache[e.type] = e.icon;
    }

    public Sprite Get(StatusType t) => (_cache != null && _cache.TryGetValue(t, out var s)) ? s : null;
}