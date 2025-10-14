using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CombatHUD : MonoBehaviour
{
    [Header("Bind at runtime")]
    [SerializeField] private UnitController controller;  // active unit’s controller

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

    // state
    private readonly List<ActionButtonView> _pool = new();
    private readonly List<ActionButtonView> _active = new();
    private int _pageStart = 0;
    private UnitAbility _selectedAbility;

    void Awake()
    {
        if (pageLeft) pageLeft.onClick.AddListener(() => Page(-buttonsPerPage));
        if (pageRight) pageRight.onClick.AddListener(() => Page(+buttonsPerPage));
        if (endTurnButton) endTurnButton.onClick.AddListener(EndTurn);
        if (pauseButton) pauseButton.onClick.AddListener(TogglePause);
    }

    public void Bind(UnitController ctrl)
    {
        controller = ctrl;
        RebuildBars();
        RebuildGrid();
        RefreshBars();
    }

    void OnEnable() { TurnManager.OnTurnUI += OnTurnUi; }
    void OnDisable() { TurnManager.OnTurnUI -= OnTurnUi; }
    void OnTurnUi(int turn, UnitFaction side, float remaining)
    {
        if (timerText) timerText.text = Mathf.CeilToInt(remaining).ToString();
    }

    // ----- Bars -----
    void RebuildBars()
    {
        if (controller?.model == null) return;
        var m = controller.model;
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
        if (!controller) return;
        ClearGrid();

        var moveBtn = GetButton();
        moveBtn.transform.SetParent(gridContainer, false);
        moveBtn.BindMove(moveIcon, apCost: 1);              
        moveBtn.OnClick = OnActionClicked;
        moveBtn.OnHover = OnActionHover;
        moveBtn.OnUnhover = OnActionUnhover;
        _active.Add(moveBtn);

        var abilities = controller.model.GetAvailableAbilities();  // adapt if your API differs
        if (abilities == null) return;

        int count = Mathf.Min(buttonsPerPage, Mathf.Max(0, abilities.Count - _pageStart));
        for (int i = 0; i < count; i++)
        {
            var ab = abilities[_pageStart + i];
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
        foreach (var v in _active) v.gameObject.SetActive(false);
        _active.Clear();
    }

    ActionButtonView GetButton()
    {
        foreach (var v in _pool) if (!v.gameObject.activeSelf) { v.gameObject.SetActive(true); return v; }
        var go = Instantiate(actionButtonPrefab);
        var view = go.GetComponent<ActionButtonView>();
        _pool.Add(view);
        go.SetActive(true);
        return view;
    }

    void Page(int delta)
    {
        var abilities = controller.model.GetAvailableAbilities();
        if (abilities == null || abilities.Count == 0) return;
        _pageStart = Mathf.Clamp(_pageStart + delta, 0, Mathf.Max(0, abilities.Count - buttonsPerPage));
        RebuildGrid();
    }

    void UpdatePaging(int total)
    {
        if (pageLeft) pageLeft.interactable = (_pageStart > 0);
        if (pageRight) pageRight.interactable = (_pageStart + buttonsPerPage < total);
    }

    void UpdateSelectedHighlight()
    {
        var act = UnitController.GetCurrentAction();  // static
        foreach (var v in _active)
            v.SetSelected(v.IsMove ? act == UnitAction.Move
                                    : v.Ability == _selectedAbility);
    }

    // ----- Interactions -----
    void OnActionClicked(ActionButtonView view)
    {
        if (view.IsMove)
        {
            _selectedAbility = null;
            UpdateSelectedHighlight();
            UnitController.SetAction(UnitAction.Move);

            // Choose a radius from your movement system
            float radius =
                controller && controller.Movement ? controller.model.GetMovementSpeed() : 3f;

            MoveRangePreview.Show(controller.transform, radius);
            return;
        }

        _selectedAbility = view.Ability;
        UpdateSelectedHighlight();

        controller.SetSelectedAbility(_selectedAbility);

        UnitController.SetAction(UnitAction.None);
        MoveRangePreview.HideStatic();

        if (NeedsTargeting(_selectedAbility))
        {
            // Use the UI-driven targeter flow we already established
            if (!TargeterController2D.Instance)
            {
                Debug.LogWarning("[CombatHUD] TargeterController2D not found in scene.");
                return;
            }

            TargeterController2D.Instance.Begin(
                c: controller,
                a: _selectedAbility,
                confirm: (center, dir) =>
                {
                    controller.CacheAim(center, dir);
                    controller.ExecuteAbility(_selectedAbility, null, center);
                }
            );
        }
        else
        {
            controller.ExecuteAbility(_selectedAbility, null);
        }
    }

    bool NeedsTargeting(UnitAbility a)
    {
        if (a == null) return false;
        return a.areaType == AreaType.Circle || a.areaType == AreaType.Line ;
    }

    void OnActionHover(ActionButtonView v, UnityEngine.EventSystems.PointerEventData e)
    {
        if (v.IsMove)
        {
            AbilityTooltip.Show(null, e.position);  // will show “Move” text
            return;
        }
        AbilityTooltip.Show(v.Ability, e.position);
    }

    void OnActionUnhover()
    {
        AbilityTooltip.Hide();
    }

    // ----- Footer actions -----
    void EndTurn()
    {
        TurnManager.Instance?.RequestEndTurn();
        MoveRangePreview.HideStatic();
    }

    void TogglePause()
    {
        //TODO
    }
}
