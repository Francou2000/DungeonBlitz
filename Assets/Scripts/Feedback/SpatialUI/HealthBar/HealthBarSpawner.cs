using SpatialUI;
using UnityEngine;

public class HealthBarSpawner : MonoBehaviour
{
    public HealthBarWorld healthBarPrefab;    
    HealthBarWorld _instance;

    void Start()
    {
        if (!healthBarPrefab) { Debug.LogWarning("[HealthBarSpawner] Missing prefab."); return; }
        var u = GetComponent<Unit>();
        if (!u) return;

        Transform parent = SpatialUIManager.SpatialUICanvas ? SpatialUIManager.SpatialUICanvas.transform : null;
        _instance = Instantiate(healthBarPrefab, parent);  // parent under the single world canvas
        _instance.Bind(u);
    }

    void OnDestroy()
    {
        if (_instance) Destroy(_instance.gameObject);
    }
}