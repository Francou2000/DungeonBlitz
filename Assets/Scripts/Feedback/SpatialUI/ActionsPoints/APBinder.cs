using UnityEngine;
using System.Collections;

public class APBinder : MonoBehaviour
{
    [Header("Target (auto if not set)")]
    [SerializeField] private Unit unit;         // will auto-find if null
    [SerializeField] private UnitModel model;   // derived from Unit if null

    [Header("UI")]
    [SerializeField] private APPipsSpatialView apPips;

    private bool _wired;

    // Public explicit binding (called by HealthBarWorld.Bind)
    public void Bind(Unit u)
    {
        unit = u;
        model = (u != null) ? u.Model : null;
        if (isActiveAndEnabled) Rewire(); // subscribe + paint now
    }

    private void Awake()
    {
        TryResolveTarget();  // be proactive
    }

    private void OnEnable()
    {
        // In case Unit/Model aren’t ready this frame (spawn order), retry next frame once
        if (model == null) StartCoroutine(DelayedResolveAndWire());
        else Rewire();

        TurnManager.OnTurnBegan += OnTurnBeganRepaint;
    }

    private void OnDisable()
    {
        Unwire();

        TurnManager.OnTurnBegan -= OnTurnBeganRepaint;
    }


    private IEnumerator DelayedResolveAndWire()
    {
        yield return null; // one frame
        TryResolveTarget();
        Rewire();
    }

    private void TryResolveTarget()
    {
        // 1) From assigned Unit
        if (unit != null && model == null)
            model = unit.Model;

        // 2) Find Unit upward (typical: world bar under Unit)
        if (unit == null)
            unit = GetComponentInParent<Unit>(true);

        if (unit != null && model == null)
            model = unit.Model;

        // 3) Fallbacks
        if (model == null) model = GetComponentInParent<UnitModel>(true);
        if (model == null) model = GetComponentInChildren<UnitModel>(true);

        // 4) PhotonView proximity fallback
        if (model == null)
        {
            var pv = GetComponentInParent<Photon.Pun.PhotonView>(true);
            if (pv)
            {
                var u = pv.GetComponent<Unit>() ?? pv.GetComponentInChildren<Unit>(true) ?? pv.GetComponentInParent<Unit>(true);
                if (u) { unit = u; model = u.Model; }
            }
        }
    }

    private void Rewire()
    {
        if (_wired) Unwire();
        if (model == null)
        {
            Debug.LogWarning($"[APBinder] No UnitModel found for {name}. Assign Unit/Model or place under a Unit.");
            return;
        }

        // Immediate paint so we’re correct even if we missed the first event
        apPips?.Set(model.CurrentActions, model.MaxActions);

        model.OnActionPointsChanged += OnAP;
        _wired = true;
    }

    private void Unwire()
    {
        if (!_wired) return;
        if (model != null) model.OnActionPointsChanged -= OnAP;
        _wired = false;
    }

    private void OnAP(int cur, int max)
    {
        apPips?.Set(cur, max);
    }

    // Force a repaint when a new turn begins (AP was reset in TurnManager)
    private void OnTurnBeganRepaint(UnitFaction side)
    {
        if (model == null) return;

        // Only repaint when it's this unit's faction turn (same logic as CombatHUD)
        if (model.Faction == side)
        {
            apPips?.Set(model.CurrentActions, model.MaxActions);
        }
    }

}