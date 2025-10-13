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

    void Update()
    {
        // simple timer binding (replace with event if TurnManager exposes one)
        if (timerText && TurnManager.Instance != null)
            timerText.text = Mathf.CeilToInt(TurnManager.Instance.RemainingTime).ToString();
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

        // wire model events (add these events in UnitModel if missing)
        m.OnHealthChanged += (c, x) => hpBar?.Set(c, x);
        m.OnAdrenalineChanged += (c, x) => adrBar?.Set(c, x);
        m.OnActionPointsChanged += (c, x) => { apPips?.SetMax(x); apPips?.SetCurrent(c); };
    }

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
            view.OnHover = ShowTooltip;
            view.OnUnhover = HideTooltip;
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
        foreach (var v in _active) v.SetSelected(v.Ability == _selectedAbility);
    }

    // ----- Interactions -----
    void OnActionClicked(ActionButtonView view)
    {
        _selectedAbility = view.Ability;
        UpdateSelectedHighlight();

        controller.SetSelectedAbility(_selectedAbility);

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

    void ShowTooltip(ActionButtonView v, UnityEngine.EventSystems.PointerEventData _)
    {
        // TODO
    }

    void HideTooltip()
    {
        //TODO
    }

    // ----- Footer actions -----
    void EndTurn()
    {
        TurnManager.Instance?.RequestEndTurn();
    }

    void TogglePause()
    {
        //TODO
    }
}
