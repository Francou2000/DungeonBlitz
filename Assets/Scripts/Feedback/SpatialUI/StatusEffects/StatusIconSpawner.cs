using SpatialUI;
using UnityEngine;

public class StatusIconSpawner : MonoBehaviour
{
    public StatusIconsWorld iconsPrefab;
    StatusIconsWorld _instance;

    void Start()
    {
        if (!iconsPrefab) { Debug.LogWarning("[StatusIconSpawner] Missing prefab."); return; }
        var u = GetComponent<Unit>();
        if (!u) return;

        Transform parent = SpatialUIManager.SpatialUICanvas ? SpatialUIManager.SpatialUICanvas.transform : null;
        _instance = Instantiate(iconsPrefab, parent);
        _instance.Bind(u);
    }

    void OnDestroy()
    {
        if (_instance) Destroy(_instance.gameObject);
    }
}
