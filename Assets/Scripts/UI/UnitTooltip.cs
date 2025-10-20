using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitTooltip : MonoBehaviour
{
    public static UnitTooltip Instance;

    [Header("UI Refs")]
    [SerializeField] Image portrait;
    [SerializeField] Slider hpSlider;
    [SerializeField] TMP_Text hpText;
    [SerializeField] TMP_Text armorText;
    [SerializeField] TMP_Text mrText;

    // cached
    Unit unit;
    UnitModel model;
    CanvasGroup cg;

    void Awake()
    {
        // single instance
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;   // never block mouse
        cg.alpha = 1f;

        // background image should not block either
        var img = GetComponent<Image>();
        if (img) img.raycastTarget = false;

        gameObject.SetActive(false);
    }

    // ------------------ Public API ------------------

    public static void Show(Unit unit)
    {
        if (!EnsureInstance()) return;
        if (!unit)
        {
            Hide();
            return;
        }

        // Model: prefer Unit.model, fallback to controller.model
        UnitModel m = unit.Controller.model != null ? unit.Controller.model
                        : (unit.Controller ? unit.Controller.model : null);
        if (!m)
        {
            Debug.LogWarning("[UnitTooltip] No UnitModel on unit " + unit.name);
            Hide();
            return;
        }

        Instance.unit = unit;
        Instance.model = m;

        // Portrait (we previously added UnitModel.Portrait; fallback to data.portrait_foto if you haven't)
        Sprite spr = null;
        try { spr = m.Portrait; } catch { /* getter might not exist yet */ }
        if (!spr)
        {
            // fallback: if your UnitData has portrait_foto
            var dataField = m.GetType().GetField("unitData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var data = dataField != null ? dataField.GetValue(m) : null;
            if (data != null)
            {
                var pf = data.GetType().GetField("portrait_foto");
                if (pf != null) spr = pf.GetValue(data) as Sprite;
            }
        }
        if (Instance.portrait)
        {
            Instance.portrait.sprite = spr;
            Instance.portrait.enabled = spr != null;
            Instance.portrait.preserveAspect = true;
        }

        // HP
        int curHP = SafeGetInt(m, "CurrentHP", m.GetType().GetProperty("CurrentHP")?.GetValue(m));
        int maxHP = SafeGetInt(m, "MaxHP", m.GetType().GetProperty("MaxHP")?.GetValue(m));
        if (Instance.hpSlider)
        {
            Instance.hpSlider.minValue = 0;
            Instance.hpSlider.maxValue = Mathf.Max(1, maxHP);
            Instance.hpSlider.value = Mathf.Clamp(curHP, 0, maxHP);
        }
        if (Instance.hpText)
        {
            Instance.hpText.text = $"{curHP}/{maxHP}";
        }

        // Armor / MagicRes (adapt names to your model)
        int armor = TryStat(m, "Armor");
        int mr = TryStat(m, "MagicRes");     // or "MagicResistance", etc.

        if (Instance.armorText) Instance.armorText.text = $"Ar: {armor}";
        if (Instance.mrText) Instance.mrText.text = $"MR: {mr}";

        // Show panel
        if (!Instance.gameObject.activeSelf) Instance.gameObject.SetActive(true);
        Instance.transform.SetAsLastSibling(); // on top in HUD
        // Debug.Log($"[UnitTooltip] Show {unit.name} OK");
    }

    public static void Hide()
    {
        if (!EnsureInstance()) return;
        Instance.gameObject.SetActive(false);
        Instance.unit = null;
        Instance.model = null;
    }

    // ------------------ Helpers ------------------

    static bool EnsureInstance()
    {
        if (Instance) return true;

        Instance = Object.FindFirstObjectByType<UnitTooltip>(FindObjectsInactive.Include);

        if (!Instance)
        {
            Debug.LogWarning("[UnitTooltip] Instance not found in scene.");
            return false;
        }

        Instance.cg = Instance.GetComponent<CanvasGroup>() ?? Instance.gameObject.AddComponent<CanvasGroup>();
        Instance.cg.blocksRaycasts = false;
        var img = Instance.GetComponent<Image>();
        if (img) img.raycastTarget = false;
        return true;
    }

    static int SafeGetInt(object obj, string label, object val)
    {
        if (val == null) { Debug.LogWarning($"[UnitTooltip] {label} is null"); return 0; }
        if (val is int i) return i;
        if (val is float f) return Mathf.RoundToInt(f);
        int parsed;
        if (int.TryParse(val.ToString(), out parsed)) return parsed;
        return 0;
    }

    static int TryStat(UnitModel m, string propName)
    {
        var p = m.GetType().GetProperty(propName);
        if (p != null && p.PropertyType == typeof(int)) return (int)p.GetValue(m);

        var f = m.GetType().GetField(propName);
        if (f != null)
        {
            var v = f.GetValue(m);
            if (v is int i) return i;
        }
        return 0;
    }
}