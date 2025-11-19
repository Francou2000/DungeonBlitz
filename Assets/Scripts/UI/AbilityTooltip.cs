using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityTooltip : MonoBehaviour
{
    public static AbilityTooltip Instance;

    [Header("UI")]
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text body;

    [Header("Placement")]
    [SerializeField] Vector2 screenOffset = new Vector2(18f, -12f); // small offset from cursor

    RectTransform rt;
    Canvas canvas;
    CanvasGroup cg;

    void Awake()
    {
        // Single instance
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        rt = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
        cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        // Never steal pointer input
        var img = GetComponent<Image>();
        if (img) img.raycastTarget = false;
        cg.blocksRaycasts = false;
        cg.alpha = 1f;

        gameObject.SetActive(false);
        Debug.Log($"[Tooltip] Awake under canvas: {canvas?.name ?? "NULL"}");
    }

    // ------- Public API -------

    public static void Show(UnitAbility ab, Vector3 screenPos)
    {
        if (!EnsureInstance()) return;

        // (Re)acquire current parent canvas in case the object was moved
        Instance.canvas = Instance.GetComponentInParent<Canvas>();
        if (!Instance.canvas)
        {
            Debug.LogWarning("[Tooltip] Parent Canvas missing.");
            return;
        }

        // Fill content
        if (ab == null)
        {
            Instance.title.text = "Move";
            Instance.body.text = "Spend AP to move within your range.";
        }
        else
        {
            Instance.title.text = ab.name;
            Instance.body.text = $"AP: {ab.actionCost}  Range: {ab.range}\n{ab.description}";
        }

        if (!Instance.gameObject.activeSelf) Instance.gameObject.SetActive(true);
        Instance.cg.alpha = 1f;
        Instance.cg.blocksRaycasts = false;
        Instance.transform.SetAsLastSibling(); // on top
    }

    public static void Move(Vector3 screenPos)
    {
        if (!EnsureInstance() || !Instance.canvas) return;
    }

    public static void Hide()
    {
        if (!EnsureInstance()) return;
        Instance.gameObject.SetActive(false);
    }

    // ------- Internals -------

    static bool EnsureInstance()
    {
        if (Instance) return true;


        Instance = Object.FindFirstObjectByType<AbilityTooltip>(FindObjectsInactive.Include);

        if (!Instance)
        {
            Debug.LogWarning("[Tooltip] AbilityTooltip not found in scene.");
            return false;
        }

        Instance.rt = Instance.transform as RectTransform;
        Instance.canvas = Instance.GetComponentInParent<Canvas>();
        Instance.cg = Instance.GetComponent<CanvasGroup>() ?? Instance.gameObject.AddComponent<CanvasGroup>();
        Instance.cg.blocksRaycasts = false;
        Instance.cg.alpha = 1f;

        // Never steal raycasts
        var img = Instance.GetComponent<Image>();
        if (img) img.raycastTarget = false;

        return true;
    }
}