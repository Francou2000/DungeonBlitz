using UnityEngine;
using UnityEngine.EventSystems;

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

        if (!cam) cam = Camera.main;

        if (rangeRing) rangeRing.gameObject.SetActive(true);
        if (circle) circle.gameObject.SetActive(true);
        if (line) line.gameObject.SetActive(true);
    }

    void Update()
    {
        if (showingMove && caster != null && rangeRing != null && rangeRing.gameObject.activeSelf)
        {
            var p = caster.transform.position; p.z = 0f;
            rangeRing.transform.position = p;
        }

        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) { Cleanup(); return; }

        if (caster == null || ability == null) return;

        Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0;

        // Clamp to range from caster
        Vector3 from = caster.transform.position;
        Vector3 to = ClampToRange(from, mouse, ability.areaType == AreaType.Line ? ability.lineRange : ability.range);

        rangeRing.Draw(from, ability.areaType == AreaType.Line ? ability.lineRange : ability.range);

        Vector3 dir = (to - from); if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;

        // Draw shape
        if (ability.areaType == AreaType.Circle || ability.areaType == AreaType.Single)
        {
            float r = ability.areaType == AreaType.Single ? 0.25f : ability.aoeRadius;
            circle.Draw(to, r);
        }
        else if (ability.areaType == AreaType.Line)
        {
            // rotate with Q/E
            if (Input.GetKey(KeyCode.Q)) dir = Quaternion.Euler(0, 0, 180 * Time.deltaTime) * dir;
            if (Input.GetKey(KeyCode.E)) dir = Quaternion.Euler(0, 0, -180 * Time.deltaTime) * dir;
            line.Draw(from, dir, ability.lineRange);
        }

        if (Input.GetMouseButtonDown(0))
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

        _singlePreviewActive = false;
        _singlePrevCaster = null;
        _singlePrevAbility = null;
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
        if (rangeRing) rangeRing.gameObject.SetActive(false);
    }

    public void Cancel()
    {
        Cleanup();
    }

    // Begin showing a preview ring on caster (range) and a circle where the pointer/hover is.
    public void BeginSinglePreview(UnitController c, UnitAbility a)
    {
        _singlePreviewActive = true;
        _singlePrevCaster = c;
        _singlePrevAbility = a;

        if (c == null || a == null) return;

        // Range ring around caster
        if (rangeRing)
        {
            var from = c.transform.position; from.z = 0f;
            float r = a.areaType == AreaType.Line ? a.lineRange : a.range; // same rule you use in Update()
            rangeRing.Draw(from, r);
            rangeRing.gameObject.SetActive(true);
        }

        if (circle) circle.gameObject.SetActive(false);
    }

    // Move the preview circle (call from HUD/hover)
    public void UpdateSinglePreview(Vector3 worldPos)
    {
        // No inner impact circle for single-target preview
        // Keep this empty on purpose
    }

    // Stop preview without touching “move range” mode
    public void EndSinglePreview()
    {
        _singlePreviewActive = false;
        _singlePrevCaster = null;
        _singlePrevAbility = null;

        if (circle) circle.gameObject.SetActive(false);

        // Only hide the range ring if we're not showing move range
        if (rangeRing && !showingMove)
            rangeRing.gameObject.SetActive(false);
    }

    // Helper radius for single-target impact visualization
    float GetSinglePreviewRadius(UnitAbility a)
    {
        if (a == null) return 0.25f;
        // Prefer aoeRadius if your single-target abilities have splash; otherwise a small dot
        return a.aoeRadius > 0f ? a.aoeRadius : 0.25f;
    }
}
