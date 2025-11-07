using SpatialUI;
using UnityEngine;

public class StatusIconSpawner : MonoBehaviour
{
    public StatusIconsWorld iconsPrefab;
    StatusIconsWorld _instance;

    void Start()
    {
        if (!iconsPrefab) { Debug.LogWarning("[StatusIconSpawner] Missing prefab"); return; }

        var u = GetComponent<Unit>() ?? GetComponentInChildren<Unit>() ?? GetComponentInParent<Unit>();
        if (!u) { Debug.LogWarning("[StatusIconSpawner] No Unit found"); return; }

        Transform parent = SpatialUIManager.SpatialUICanvas ? SpatialUIManager.SpatialUICanvas.transform : null;
        _instance = Instantiate(iconsPrefab, parent);
        _instance.gameObject.SetActive(true);
        _instance.Bind(u);
        Debug.Log($"[StatusIconSpawner] Spawned for {u.name}");
    }

    void OnDestroy()
    {
        if (_instance) Destroy(_instance.gameObject);
    }
}
