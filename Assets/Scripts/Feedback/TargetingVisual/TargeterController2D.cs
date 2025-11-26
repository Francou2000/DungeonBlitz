using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.XR;

public class TargeterController2D : MonoBehaviour
{
    public Camera cam;
    public LayerMask groundMask;        // floor layer
    public RangeRing2D rangeRing;
    public CircleAoE2D circle;
    public LineAoE2D line;

    UnitController caster;
    UnitAbility ability;
    System.Action<Vector3, Vector3> onConfirm; // (pos, dir)

    private bool showingMove;
    private float moveRadius;

    bool _singlePreviewActive;
    UnitController _singlePrevCaster;
    UnitAbility _singlePrevAbility;


    public static TargeterController2D Instance { get; private set; }

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }


    public void Begin(UnitController c, UnitAbility a, System.Action<Vector3, Vector3> confirm)
    {
        caster = c; ability = a; onConfirm = confirm;
        enabled = true;

        Debug.Log($"[Targeter] BEGIN AIM: caster={caster?.name}, ability={ability?.abilityName}");

        if (!cam) cam = Camera.main;

        if (rangeRing) rangeRing.gameObject.SetActive(true);
        if (circle) circle.gameObject.SetActive(true);
        if (line) line.gameObject.SetActive(true);
    }

    void Update()
    {
        // Keep move range centered on caster if we're showing it
        if (showingMove && caster != null && rangeRing != null && rangeRing.gameObject.activeSelf)
        {
            var p = caster.transform.position; p.z = 0f;
            rangeRing.transform.position = p;
        }

        // Don’t aim if pointer is on UI
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
            return;

        // Right click / Esc cancels active aim (AoE/ground line), and also clears previews
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Cleanup();
            return;
        }

        // Two modes:
        //  - Aim mode: Begin(...) was called (AoE/ground abilities, including line zones)
        //  - Single-preview mode: BeginSinglePreview(...) was called (single-target, e.g. Piercing Shot)
        bool isAimMode = (onConfirm != null && caster != null && ability != null);
        bool isSinglePreview = (!isAimMode && _singlePreviewActive && _singlePrevCaster != null && _singlePrevAbility != null);

        if (!isAimMode && !isSinglePreview)
            return;

        if (!cam) cam = Camera.main;

        // Pick which caster/ability we are using this frame
        UnitController activeCaster = isAimMode ? caster : _singlePrevCaster;
        UnitAbility activeAbility = isAimMode ? ability : _singlePrevAbility;

        if (activeCaster == null || activeAbility == null)
            return;

        Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0f;

        // Clamp to range from caster
        Vector3 from = activeCaster.transform.position;
        Vector3 to = ClampToRange(from, mouse,
            activeAbility.areaType == AreaType.Line ? activeAbility.lineRange : activeAbility.range);

        // Range ring around caster
        if (rangeRing)
        {
            rangeRing.Draw(from,
                activeAbility.areaType == AreaType.Line ? activeAbility.lineRange : activeAbility.range);
            rangeRing.gameObject.SetActive(true);
        }

        Vector3 dir = (to - from);
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;

        // Draw shape
        if (activeAbility.areaType == AreaType.Circle || activeAbility.areaType == AreaType.Single)
        {
            if (circle)
            {
                float r = activeAbility.areaType == AreaType.Single ? 0.25f : activeAbility.aoeRadius;
                circle.Draw(to, r);
                circle.gameObject.SetActive(true);
            }
            if (line) line.gameObject.SetActive(false);
        }
        else if (activeAbility.areaType == AreaType.Line)
        {
            // In AIM MODE (Storm Crossing, etc) allow Q/E rotation.
            // In SINGLE-PREVIEW (Piercing Shot), direction is just caster→mouse (no Q/E)
            if (isAimMode)
            {
                if (Input.GetKey(KeyCode.Q))
                    dir = Quaternion.Euler(0, 0, 180 * Time.deltaTime) * dir;
                if (Input.GetKey(KeyCode.E))
                    dir = Quaternion.Euler(0, 0, -180 * Time.deltaTime) * dir;
            }

            if (line)
            {
                line.gameObject.SetActive(true);
                line.Draw(from, dir, activeAbility.lineRange);
            }

            if (circle) circle.gameObject.SetActive(false);
        }

        // Only AIM MODE consumes the left click and fires the confirm callback.
        if (isAimMode && Input.GetMouseButtonDown(0))
        {
            onConfirm?.Invoke(to, dir.normalized);
            Cleanup();
        }
    }

    Vector3 ClampToRange(Vector3 from, Vector3 to, float range)
    {
        Vector3 d = to - from; float m = d.magnitude;
        return m <= range ? to : from + d / m * range;
    }

    void Cleanup()
    {
        rangeRing.gameObject.SetActive(false);
        circle.gameObject.SetActive(false);
        line.gameObject.SetActive(false);
        enabled = false;

        showingMove = false;                // remove leftover move mode
        _singlePreviewActive = false;
        _singlePrevCaster = null;
        _singlePrevAbility = null;

        caster = null;
        ability = null;
        onConfirm = null;
    }

    public void ShowMoveRange(UnitController caster, float radius)
    {
        if (!rangeRing) return;
        if (!caster) return;

        var from = caster.transform.position;
        from.z = 0f;

        // Use the same code-path abilities use (ensures geometry+radius are valid)
        rangeRing.Draw(from, Mathf.Max(0.25f, radius));
        rangeRing.gameObject.SetActive(true);
    }

    public void HideMoveRange()
    {
        showingMove = false;                 // stop blocking ability previews
        if (rangeRing) rangeRing.gameObject.SetActive(false);

    }

    public void Cancel()
    {
        Debug.Log("[Targeter] CANCEL called");
        Cleanup();
    }

    // Begin showing a preview ring on caster (range) and a circle where the pointer/hover is.
    public void BeginSinglePreview(UnitController c, UnitAbility a)
    {
        enabled = true;                    // ensure Update() runs
        _singlePreviewActive = true;
        _singlePrevCaster = c;
        _singlePrevAbility = a;

        Debug.Log($"[Targeter] BEGIN SINGLE PREVIEW: caster={c?.name}, ability={a?.abilityName} " +
             $"(onConfirm still set? {onConfirm != null})");


        if (c == null || a == null) return;

        // Range ring around caster
        if (rangeRing)
        {
            var from = c.transform.position; from.z = 0f;
            float r = a.areaType == AreaType.Line ? a.lineRange : a.range;
            rangeRing.Draw(from, r);
            rangeRing.gameObject.SetActive(true);
        }

        if (circle) circle.gameObject.SetActive(false);
        // we don't force line on here; Update() will turn it on for line abilities
    }

    public void EndSinglePreview()
    {
        Debug.Log($"[Targeter] END SINGLE PREVIEW: caster={_singlePrevCaster?.name}, ability={_singlePrevAbility?.abilityName}");

        _singlePreviewActive = false;
        _singlePrevCaster = null;
        _singlePrevAbility = null;

        if (circle) circle.gameObject.SetActive(false);
        if (line) line.gameObject.SetActive(false);

        // Only hide the range ring if we're not showing move range
        if (rangeRing && !showingMove)
            rangeRing.gameObject.SetActive(false);

        // If we're not aiming anything and not showing move, we can sleep
        if (!showingMove && onConfirm == null)
            enabled = false;
    }

    // Move the preview circle (call from HUD/hover)
    public void UpdateSinglePreview(Vector3 worldPos)
    {
        // No inner impact circle for single-target preview
        // Keep this empty on purpose
    }

    // Helper radius for single-target impact visualization
    float GetSinglePreviewRadius(UnitAbility a)
    {
        if (a == null) return 0.25f;
        // Prefer aoeRadius if your single-target abilities have splash; otherwise a small dot
        return a.aoeRadius > 0f ? a.aoeRadius : 0.25f;
    }
}
