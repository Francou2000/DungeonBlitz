using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CombatHUD : MonoBehaviour
{
    [Header("Bind at runtime")]
    [SerializeField] private UnitController controller;  // active unit�s controller

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

    ActionButtonView hoveredView;

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
        RefreshPortrait();
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

        int count = Mathf.Min(buttonsPerPage, Mathf.Max(0, abilities.Count - pageStart));
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
        pageStart = Mathf.Clamp(pageStart + delta, 0, Mathf.Max(0, abilities.Count - buttonsPerPage));
        RebuildGrid();
    }

    void UpdatePaging(int total)
    {
        if (pageLeft) pageLeft.interactable = (pageStart > 0);
        if (pageRight) pageRight.interactable = (pageStart + buttonsPerPage < total);
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

        if (NeedsTargeting(selectedAbility))
        {
            // Use the UI-driven targeter flow we already established
            if (!TargeterController2D.Instance)
            {
                Debug.LogWarning("[CombatHUD] TargeterController2D not found in scene.");
                return;
            }

            TargeterController2D.Instance.Begin(
                c: controller,
                a: selectedAbility,
                confirm: (center, dir) =>
                {
                    controller.CacheAim(center, dir);
                    controller.ExecuteAbility(selectedAbility, null, center);
                }
            );
        }
        else
        {
            controller.ExecuteAbility(selectedAbility, null);
        }
    }

    bool NeedsTargeting(UnitAbility a)
    {
        if (a == null) return false;
        return a.areaType == AreaType.Circle || a.areaType == AreaType.Line ;
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
