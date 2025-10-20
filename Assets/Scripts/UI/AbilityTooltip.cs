using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityTooltip : MonoBehaviour
{
    public static AbilityTooltip Instance;

    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text body;

    [Header("Placement")]
    [SerializeField] Vector2 screenOffset = new Vector2(18f, -12f);

    RectTransform _rt;
    Canvas _canvas;
    CanvasGroup _cg;

    void Awake()
    {
        Instance = this;
        _rt = transform as RectTransform;
        _canvas = GetComponentInParent<Canvas>();
        _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        // Never block input
        var img = GetComponent<Image>();
        if (img) img.raycastTarget = false;
        _cg.blocksRaycasts = false;

        gameObject.SetActive(false);
    }

    public static void Show(UnitAbility ab, Vector3 screenPos)
    {
        if (!Instance) Instance = FindFirstObjectByType<AbilityTooltip>(UnityEngine.FindObjectsInactive.Include);
        if (!Instance || Instance._rt == null) return;

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

        Instance.PositionAt(screenPos + (Vector3)Instance.screenOffset);
        if (!Instance.gameObject.activeSelf) Instance.gameObject.SetActive(true);
        Instance._cg.alpha = 1f;
    }

    // Called while still hovering to only reposition (don’t toggle active)
    public static void Move(Vector3 screenPos)
    {
        if (!Instance || !Instance._rt) return;
        Instance.PositionAt(screenPos + (Vector3)Instance.screenOffset);
    }

    public static void Hide()
    {
        if (!Instance) return;
        Instance.gameObject.SetActive(false);
    }

    void PositionAt(Vector3 screenPos)
    {
        var canvasRT = (RectTransform)_canvas.transform;
        var cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPos, cam, out var local))
        {
            _rt.anchoredPosition = ClampToCanvas(local, canvasRT.rect, _rt.rect.size);
        }
    }

    Vector2 ClampToCanvas(Vector2 pos, Rect canvasRect, Vector2 size)
    {
        var half = size * 0.5f;
        float x = Mathf.Clamp(pos.x, canvasRect.xMin + half.x, canvasRect.xMax - half.x);
        float y = Mathf.Clamp(pos.y, canvasRect.yMin + half.y, canvasRect.yMax - half.y);
        return new Vector2(x, y);
    }
}