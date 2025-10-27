using UnityEngine;
using System.Collections;

public class APBinder : MonoBehaviour
{
    [Header("Target (auto if not set)")]
    [SerializeField] private Unit unit;        // if you drag Unit here, we’ll use it
    [SerializeField] private UnitModel model;  // will be derived from Unit if left empty

    [Header("UI")]
    [SerializeField] private APPipsSpatialView apPips;       

    // --- Public explicit binding (if you want to call this from spawner/Unit) ---
    public void Bind(Unit u)
    {
        unit = u;
        model = (u != null) ? u.Model : null;
        if (isActiveAndEnabled) Rewire();
    }

    private void Awake()
    {
        // Try to resolve immediately
        TryResolveTarget();
    }

    private void OnEnable()
    {
        // If still not found (spawn order / world bar spawned before Unit), retry next frame
        if (model == null) StartCoroutine(DelayedResolveAndWire());
        else Rewire();
    }

    private void OnDisable()
    {
        Unwire();
    }

    private IEnumerator DelayedResolveAndWire()
    {
        // One-frame delay to allow Unit/Model to exist
        yield return null;
        TryResolveTarget();
        Rewire();
    }

    private void TryResolveTarget()
    {
        // 1) If Unit already assigned, derive model from it
        if (unit != null)
            model = unit.Model;

        // 2) Try to find Unit in parents first (typical case: world bar under Unit)
        if (unit == null)
            unit = GetComponentInParent<Unit>(true);

        if (model == null && unit != null)
            model = unit.Model;

        // 3) Fallbacks — different project layouts:
        if (model == null) model = GetComponentInParent<UnitModel>(true);
        if (model == null) model = GetComponentInChildren<UnitModel>(true);

        // 4) Photon-based fallback (if the bar sits under a PV but not the Unit):
        if (model == null)
        {
            var pv = GetComponentInParent<Photon.Pun.PhotonView>(true);
            if (pv != null)
            {
                // Look for a Unit attached to the same PV object or its relatives
                unit = pv.GetComponent<Unit>() ?? pv.GetComponentInChildren<Unit>(true) ?? pv.GetComponentInParent<Unit>(true);
                if (unit != null) model = unit.Model;
            }
        }

        // 5) Last resort: search up the root for a Unit nearest in transform hierarchy
        if (model == null)
        {
            var root = transform.root;
            unit = root.GetComponentInChildren<Unit>(true);
            if (unit != null) model = unit.Model;
        }
    }

    private void Rewire()
    {
        Unwire();
        if (model == null) { Debug.LogWarning($"[UnitWorldBarBinder] No UnitModel found for {name}. Assign Unit or Model explicitly."); return; }

        // Initial paint
        apPips?.Set(model.CurrentActions, model.MaxActions);

        // Subscribe
        model.OnActionPointsChanged += OnAP;
    }

    private void Unwire()
    {
        if (model == null) return;
        model.OnActionPointsChanged -= OnAP;
    }

    private void OnAP(int cur, int max) => apPips?.Set(cur, max);
}