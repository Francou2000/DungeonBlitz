using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CombatHUD : MonoBehaviour
{
    [Header("Bind at runtime")]
    [SerializeField] private UnitController controller;  // active units controller

    [Header("Top Bar")]
    [SerializeField] private ResourceBarView hpBar;
    [SerializeField] private ResourceBarView adrBar;
    [SerializeField] private APPipsView apPips;

    [Header("Action Grid")]
    [SerializeField] private Transform gridContainer;       // GridLayoutGroup parent
    [SerializeField] private GameObject actionButtonPrefab; // prefab with ActionButtonView
    [SerializeField] private Button pageLeft;
    [SerializeField] private Button pageRight;
    [SerializeField] private int buttonsPerPage = 10;

    [Header("Footer")]
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private TMP_Text timerText;

    [Header("Icons")]
    [SerializeField] private Sprite moveIcon; 
    [SerializeField] private Image portraitImage;

    // state
    private readonly List<ActionButtonView> _pool = new();
    private readonly List<ActionButtonView> _active = new();
    private int pageStart = 0;
    private UnitAbility selectedAbility;
    private UnitModel boundModel;

    ActionButtonView hoveredView;

    void Awake()
    {
        if (pageLeft) pageLeft.onClick.AddListener(() => Page(-VisibleAbilitySlots()));
        if (pageRight) pageRight.onClick.AddListener(() => Page(+VisibleAbilitySlots()));
        if (endTurnButton) endTurnButton.onClick.AddListener(EndTurn);
        if (pauseButton) pauseButton.onClick.AddListener(TogglePause);
    }

    private void Start()
    {
        TurnManager.OnActiveControllerChanged += HandleActiveUnitChanged;
    }

    private void HandleActiveUnitChanged(UnitController newUnit)
    {
        Bind(newUnit);
    }

    private void OnDestroy()
    {
        TurnManager.OnActiveControllerChanged -= HandleActiveUnitChanged;
        UnbindCurrentUnit();
        ClearGrid(); // final cleanup
    }

    public void Bind(UnitController ctrl)
    {
        if (controller == ctrl) return;

        UnbindCurrentUnit();
        ClearGrid();

        controller = ctrl;
        pageStart = 0;
        RefreshPortrait();
        RebuildBars();
        RebuildGrid();
        RefreshBars();
    }

    private void UnbindCurrentUnit()
    {
        if (controller != null)
        {
            controller = null;
        }

        if (boundModel != null)
        {
            boundModel.OnHealthChanged -= OnHP;
            boundModel.OnAdrenalineChanged -= OnADR;
            boundModel.OnActionPointsChanged -= OnAP;
            boundModel = null;
        }
    }

    void OnEnable()
    {
        TurnManager.OnTurnUI += OnTurnUi;
        TurnManager.OnTurnBegan += OnTurnBegan;
    }
    void OnDisable()
    {
        TurnManager.OnTurnUI -= OnTurnUi;
        TurnManager.OnTurnBegan -= OnTurnBegan;
    }
    void OnTurnUi(int turn, UnitFaction side, float remaining)
    {
        if (timerText) timerText.text = Mathf.CeilToInt(remaining).ToString();
    }

    // ----- Bars -----
    void RebuildBars()
    {
        if (controller?.model == null) return;
        var m = controller.model;
        boundModel = m;

        hpBar?.Set(m.CurrentHP, m.MaxHP);
        adrBar?.Set(m.Adrenaline, m.MaxAdrenaline);  
        apPips?.SetMax(m.MaxActions);
        apPips?.SetCurrent(m.CurrentActions);

        // clear stale subscriptions first (in case Bind() called multiple times)
        m.OnHealthChanged -= OnHP;
        m.OnAdrenalineChanged -= OnADR;
        m.OnActionPointsChanged -= OnAP;

        m.OnHealthChanged += OnHP;
        m.OnAdrenalineChanged += OnADR;
        m.OnActionPointsChanged += OnAP;
    }

    void OnHP(int cur, int max) { hpBar?.Set(cur, max); }
    void OnADR(int cur, int max) { adrBar?.Set(cur, max); }
    void OnAP(int cur, int max) { apPips?.SetMax(max); apPips?.SetCurrent(cur); }

    void RefreshBars()
    {
        if (controller?.model == null) return;
        var m = controller.model;
        hpBar?.Set(m.CurrentHP, m.MaxHP);
        adrBar?.Set(m.Adrenaline, m.MaxAdrenaline);
        apPips?.SetMax(m.MaxActions);
        apPips?.SetCurrent(m.CurrentActions);
    }

    // ----- Grid -----
    void RebuildGrid()
    {
        if (controller == null)
        {
            Debug.LogWarning("[CombatHUD] No controller bound.");
            return;
        }

        if (controller.model == null)
        {
            Debug.LogWarning($"[CombatHUD] UnitController {controller.name} has no model.");
            return;
        }

        if (gridContainer == null || actionButtonPrefab == null)
        {
            Debug.LogWarning("[CombatHUD] Grid container or prefab not assigned.");
            return;
        }

        ClearGrid();
        ClearGrid();

        //  Move button (always 1 slot)
        var moveBtn = GetButton();
        moveBtn.transform.SetParent(gridContainer, false);
        moveBtn.BindMove(moveIcon, apCost: 1);              
        moveBtn.OnClick = OnActionClicked;
        moveBtn.OnHover = OnActionHover;
        moveBtn.OnUnhover = OnActionUnhover;
        _active.Add(moveBtn);

        //  Abilities (reserve space for Move)
        var abilities = controller.model.GetAvailableAbilities();
        if (abilities == null) { UpdatePaging(0); UpdateSelectedHighlight(); return; }

        int slotsForAbilities = VisibleAbilitySlots(); // page capacity excluding Move

        int remaining = Mathf.Max(0, abilities.Count - pageStart);
        int count = Mathf.Min(slotsForAbilities, remaining);
        for (int i = 0; i < count; i++)
        {
            var ab = abilities[pageStart + i];
            var view = GetButton();
            view.transform.SetParent(gridContainer, false);
            int apCost = ab.actionCost;
            Sprite icon = ab.icon;                  
            view.Bind(ab, icon, apCost);
            view.OnClick = OnActionClicked;
            view.OnHover = OnActionHover;
            view.OnUnhover = OnActionUnhover;
            _active.Add(view);
        }

        UpdateSelectedHighlight();
        UpdatePaging(abilities.Count);
    }

    void ClearGrid()
    {
        // Hide tooltip if any
        OnActionUnhover();

        // Deactivate and clear active views
        foreach (var v in _active)
        {
            // Remove button listeners to prevent accidental accumulation (if ActionButtonView uses Button)
            var btn = v.GetComponent<Button>();
            if (btn) btn.onClick.RemoveAllListeners();

            v.gameObject.SetActive(false);
        }
        _active.Clear();
    }

    ActionButtonView GetButton()
    {
        foreach (var v in _pool)
            if (!v.gameObject.activeSelf) { v.gameObject.SetActive(true); return v; }

        // instantiate directly under gridContainer so pooling/layout is stable
        var go = gridContainer != null
            ? Instantiate(actionButtonPrefab, gridContainer, false)
            : Instantiate(actionButtonPrefab);

        var view = go.GetComponent<ActionButtonView>();
        _pool.Add(view);
        go.SetActive(true);
        return view;
    }

    void Page(int delta)
    {
        var abilities = controller.model.GetAvailableAbilities();
        if (abilities == null || abilities.Count == 0) return;

        int cap = VisibleAbilitySlots();
        pageStart = Mathf.Clamp(pageStart + delta, 0, Mathf.Max(0, abilities.Count - cap));
        RebuildGrid();
    }

    void UpdatePaging(int total)
    {
        int cap = VisibleAbilitySlots();
        if (pageLeft) pageLeft.interactable = (pageStart > 0);
        if (pageRight) pageRight.interactable = (pageStart + cap < total);
    }

    int VisibleAbilitySlots()
    {
        return Mathf.Max(0, buttonsPerPage - 1);
    }

    void UpdateSelectedHighlight()
    {
        var act = UnitController.GetCurrentAction();  // static
        foreach (var v in _active)
            v.SetSelected(v.IsMove ? act == UnitAction.Move
                                    : v.Ability == selectedAbility);
    }

    // ----- Interactions -----
    void OnActionClicked(ActionButtonView view)
    {
        if (view.IsMove)
        {
            UnitController.SetAction(UnitAction.Move);

            float radius = (controller && controller.Movement)
                ? controller.Movement.GetMaxWorldRadius()
                : 3f;

            if (TargeterController2D.Instance)
                TargeterController2D.Instance.ShowMoveRange(controller, radius);

            return;
        }

        selectedAbility = view.Ability;
        UpdateSelectedHighlight();

        UnitController.SetAction(UnitAction.None);
        if (TargeterController2D.Instance)
            TargeterController2D.Instance.HideMoveRange();

        controller.SetSelectedAbility(selectedAbility);

        // AREA FIRST (Circle/Line/Ground) → Targeter2D
        if (NeedsAreaTargeting(selectedAbility))
        {
            if (!TargeterController2D.Instance) { Debug.LogWarning("No TargeterController2D"); return; }

            TargeterController2D.Instance.Begin(
                c: controller,
                a: selectedAbility,
                confirm: (center, dir) =>
                {
                    controller.CacheAim(center, dir);
                    controller.ExecuteAbility(selectedAbility, null, center);
                }
            );
            return;
        }

        // then unit-target (non-area singles)
        if (RequiresUnitTarget(selectedAbility))
        {
            if (selectedAbility.selfOnly) { controller.ExecuteAbility(selectedAbility, controller.unit); return; }
            controller.StartTargeting(selectedAbility);
            return;
        }

        // instant
        controller.ExecuteAbility(selectedAbility, null);
    }

    // true for single-target unit selection (self/ally/enemy)
    bool RequiresUnitTarget(UnitAbility a)
    {
        if (a == null) return false;
        if (NeedsAreaTargeting(a)) return false;   // prevent AoE from ever taking the unit path
        return a.selfOnly || a.alliesOnly || a.enemiesOnly;
    }

    // true for AoE, line or ground target
    bool NeedsAreaTargeting(UnitAbility a)
    {
        if (a == null) return false;
        return a.areaType == AreaType.Circle || a.areaType == AreaType.Line || a.groundTarget;
    }

    void OnActionHover(ActionButtonView v, UnityEngine.EventSystems.PointerEventData e)
    {
        if (hoveredView != v)
        {
            hoveredView = v;
            AbilityTooltip.Show(v.IsMove ? null : v.Ability, e.position);
        }
        else
        {
            AbilityTooltip.Move(e.position); // just reposition, no re-show
        }
    }

    void OnActionUnhover()
    {
        hoveredView = null;
        AbilityTooltip.Hide();
    }

    private Sprite GetPortrait(UnitController c)
    {
        return (c != null && c.model != null) ? c.model.Portrait : null;
    }

    private void RefreshPortrait()
    {
        if (!portraitImage) return;

        var spr = GetPortrait(controller);
        portraitImage.sprite = spr;
        portraitImage.enabled = spr != null;
    }

    private void OnTurnBegan(UnitFaction side)
    {   
        if (controller != null && controller.model != null &&
            controller.model.Faction == side)
        {
            // AP gets reset in TurnManager.ResetUnitsForFaction ? reflect it here
            RefreshBars();
            // If your buttons display cooldown/charges, just in case later
            // RebuildGrid();
        }
    }

    // ----- Footer actions -----
    void EndTurn()
    {   
        if (TargeterController2D.Instance)
            TargeterController2D.Instance.HideMoveRange();
        TurnManager.Instance?.RequestEndTurn();
    }

    void TogglePause()
    {
        //TODO
    }
}
