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
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) { Cleanup(); return; }

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
    }
}
