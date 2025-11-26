using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CombatHUD : MonoBehaviour
{
    [Header("Bind at runtime")]
    [SerializeField] public UnitController controller;  // active units controller

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

    [Header("Pause Menu")]
    [SerializeField] private PauseMenuController pauseMenuController;

    // state
    private readonly List<ActionButtonView> _pool = new();
    private readonly List<ActionButtonView> _active = new();
    private int pageStart = 0;
    private UnitAbility selectedAbility;
    private UnitModel boundModel;
    private bool _boundOnce;

    ActionButtonView hoveredView;
    private UnitController currentController;

    private bool _singlePreviewActive;
    private UnitAbility _singlePreviewAbility;

    [Header("Resource UI")]
    [SerializeField] private GameObject resourceRoot;
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TextMeshProUGUI resourceAmountText;

    [Header("Interactivity")]
    [SerializeField] private CanvasGroup rootCg;

    [Header("End Turn FX")]
    [SerializeField] private Image endTurnImage;   // leave null to use endTurnButton.image
    [SerializeField] private float endFlashSpeed = 4f;
    [SerializeField] private float endPulseScale = 1.1f;

    [SerializeField] private Image endTurnGlowImage;   
    [SerializeField] private float glowMaxAlpha = 0.8f;
    [SerializeField] private float glowScaleMultiplier = 1.3f;

    private bool endPulseActive;
    private bool endVisualsCached;
    private Color endOriginalColor;
    private Vector3 endOriginalScale;
    private Vector3 glowOriginalScale;


    void Awake()
    {
        if (pageLeft) pageLeft.onClick.AddListener(() => Page(-VisibleAbilitySlots()));
        if (pageRight) pageRight.onClick.AddListener(() => Page(+VisibleAbilitySlots()));
        if (endTurnButton) endTurnButton.onClick.AddListener(EndTurn);
        if (pauseButton) pauseButton.onClick.AddListener(TogglePause);

        CacheEndTurnVisuals();
    }

    private void Start()
    {
        TurnManager.OnActiveControllerChanged += HandleActiveUnitChanged;
        TurnManager.OnTurnUI += OnTurnUi;
        TurnManager.OnTurnBegan += OnTurnBegan;
    }

    void Update()
    {
        UpdateEndTurnFX();
    }

    private void HandleActiveUnitChanged(UnitController newUnit)
    {
        Bind(newUnit);
    }

    private void OnDestroy()
    {
        TurnManager.OnActiveControllerChanged -= HandleActiveUnitChanged;
        TurnManager.OnTurnUI -= OnTurnUi;
        TurnManager.OnTurnBegan -= OnTurnBegan;

        UnbindCurrentUnit();
        ClearGrid(); // final cleanup
    }

    public void Bind(UnitController ctrl)
    {
        if (controller == ctrl) return;
        Debug.Log($"[HUD] Bind to controller={ctrl?.name}");

        // Close any open targeting UI and pending modes
        if (TargeterController2D.Instance)
        {
            TargeterController2D.Instance.Cancel();          // existing cleanup
            TargeterController2D.Instance.EndSinglePreview(); // kill stale preview
            TargeterController2D.Instance.HideMoveRange();    // kill move ring
        }
        if (controller != null) controller.CancelTargeting();

        UnbindCurrentUnit();
        ClearGrid();

        controller = ctrl;
        controller.boundHud = this;
        pageStart = 0;

        StopEndTurnAttention();
        // Reset selection and previews when binding to a new unit
        selectedAbility = null;
        _singlePreviewActive = false;
        _singlePreviewAbility = null;
        hoveredView = null;
        AbilityTooltip.Hide();

        RefreshPortrait();
        RebuildBars();
        RebuildGrid();
        RefreshBars();
        RefreshInteractivity();
        RefreshButtons();
    }

    private void UnbindCurrentUnit()
    {
        if (boundModel != null)
        {
            boundModel.OnHealthChanged -= OnHP;
            boundModel.OnAdrenalineChanged -= OnADR;
            boundModel.OnActionPointsChanged -= OnAP;
            boundModel.OnResourceChanged -= OnResChanged;
            boundModel.OnFormChanged -= OnFormChanged;
            boundModel.OnStateChanged -= OnUnitStateChanged;
            boundModel = null;
        }
        controller = null;
        StopEndTurnAttention();
    }

    void OnEnable()
    {
        // If this HUD is dropped in-scene with a prebound controller, bind once
        if (!_boundOnce && controller != null)
        {
            _boundOnce = true;
            Bind(controller);
        }
        else
        {
            RefreshInteractivity();
        }
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
        RebuildResources(m);

        // clear stale subscriptions first (in case Bind() called multiple times)
        m.OnHealthChanged -= OnHP;
        m.OnAdrenalineChanged -= OnADR;
        m.OnActionPointsChanged -= OnAP;
        m.OnResourceChanged -= OnResChanged;
        boundModel.OnFormChanged -= OnFormChanged;
        m.OnStateChanged -= OnUnitStateChanged;


        m.OnHealthChanged += OnHP;
        m.OnAdrenalineChanged += OnADR;
        m.OnActionPointsChanged += OnAP;
        m.OnResourceChanged += OnResChanged;
        boundModel.OnFormChanged += OnFormChanged;
        m.OnStateChanged += OnUnitStateChanged;
    }

    void OnHP(int cur, int max) { hpBar?.Set(cur, max); }
    void OnADR(int cur, int max) { adrBar?.Set(cur, max); }
    void OnAP(int cur, int max)
    {
        apPips?.SetMax(max);
        apPips?.SetCurrent(cur);

        // Only grab attention when it's this unit's turn AND they have no AP
        bool isTurn = TurnManager.Instance != null &&
                      controller != null &&
                      TurnManager.Instance.IsCurrentTurn(controller.unit);

        if (isTurn && cur <= 0)
            StartEndTurnAttention();
        else
            StopEndTurnAttention();
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
        RefreshButtons();
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

    void OnUnitStateChanged(string key, string value)
    {
        RefreshButtons(); // instant re-gating
    }

    void RefreshButtons()
    {
        if (controller == null || controller.model == null) return;
        var list = controller.model.GetAvailableAbilities();
        if (list == null) return;

        for (int i = 0; i < _active.Count && i < list.Count; i++)
        {
            var view = _active[i];
            if (view == null) continue;

            // Move button stays enabled according to global interactivity
            if (view.IsMove) continue;

            var ab = view.Ability;
            var btn = view.GetComponent<Button>();

            if (ab == null || btn == null)
                continue;

            // Ask the resolver if this ability is currently legal
            bool can = AbilityResolver.CanCast(controller.unit, ab, null, out var reason);

            // DEBUG: log only for Replenish Spears (or everything if you want)
            if (ab.abilityName == "Replenish Spears")
            {
                Debug.Log(
                    $"[HUD] Gate ability='{ab.abilityName}' " +
                    $"unit={controller.name} can={can} " +
                    $"reason='{reason}' " +
                    $"AP={controller.model.CurrentActions}/{controller.model.MaxActions} " +
                    $"ADR={controller.model.Adrenaline} " +
                    $"Spears={controller.model.GetRes("Power")}"
                );
            }
            btn.interactable = can && IsUsableNow();
            }
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

        // allow last page to be partially filled
        int maxStart = Mathf.Max(0, abilities.Count - 1);

        pageStart = Mathf.Clamp(pageStart + delta, 0, maxStart);
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
        if (!IsUsableNow())
        {
            Debug.Log($"[HUD] CLICK blocked by IsUsableNow for {view.Ability?.abilityName}");
            return;
        }

        Debug.Log($"[HUD] CLICK: view={(view.IsMove ? "\\Move\\" : view.Ability?.abilityName)} " +
              $"controller={controller?.name}, selectedBefore={selectedAbility?.abilityName}");

        // --- MOVE BUTTON LOGIC ---
        if (view.IsMove)
        {
            // Toggle move mode
            if (UnitController.GetCurrentAction() == UnitAction.Move)
            {
                // Turn move off
                UnitController.SetAction(UnitAction.None);
                if (TargeterController2D.Instance)
                    TargeterController2D.Instance.HideMoveRange();
            }
            else
            {
                // Turn move on
                UnitController.SetAction(UnitAction.Move);

                if (TargeterController2D.Instance && controller != null && controller.Movement != null)
                {
                    float radius = controller.Movement.GetMaxWorldRadius();
                    TargeterController2D.Instance.ShowMoveRange(controller, radius);
                }
            }

            // When choosing Move we clear any selected ability
            selectedAbility = null;
            UpdateSelectedHighlight();
            return;
        }

        // --- ABILITY BUTTON LOGIC (unchanged behavior) ---

        selectedAbility = view.Ability;
        UpdateSelectedHighlight();

        // Leave move mode when selecting an ability
        UnitController.SetAction(UnitAction.None);
        if (TargeterController2D.Instance)
            TargeterController2D.Instance.HideMoveRange();

        if (TargeterController2D.Instance)
        {
            TargeterController2D.Instance.Cancel();       
        }
        controller.CancelTargeting();                     
        controller.ClearAimCache();                       


        controller.SetSelectedAbility(selectedAbility);

        if (TargeterController2D.Instance)
        {
            TargeterController2D.Instance.EndSinglePreview();               // stop previous ability preview
            TargeterController2D.Instance.BeginSinglePreview(controller, selectedAbility);
            _singlePreviewActive = true;
            _singlePreviewAbility = selectedAbility;

            Debug.Log($"[HUD] CLICK - started preview: caster={controller?.name}, ability={selectedAbility?.abilityName}");
        }

        // 1) AoE / line-zone / ground → Targeter2D aim mode
        if (NeedsAreaTargeting(selectedAbility))
        {
            if (!TargeterController2D.Instance)
            {
                Debug.LogWarning("[CombatHUD] No TargeterController2D in scene");
                return;
            }

            TargeterController2D.Instance.Begin(
                c: controller,
                a: selectedAbility,
                confirm: (center, dir) =>
                {
                    controller.CacheAim(center, dir);
                    controller.ExecuteAbility(selectedAbility, null, center);
                    RefreshInteractivity();
                }
            );
            return;
        }

        // 2) Single-target / line-attack (e.g. Piercing Shot) → unit targeting
        if (RequiresUnitTarget(selectedAbility))
        {
            if (selectedAbility.selfOnly)
            {
                controller.ExecuteAbility(selectedAbility, controller.unit);
                RefreshInteractivity();
                return;
            }

            controller.StartTargeting(selectedAbility);

            if (TargeterController2D.Instance)
            {
                TargeterController2D.Instance.BeginSinglePreview(controller, selectedAbility);
                _singlePreviewActive = true;
                _singlePreviewAbility = selectedAbility;
            }
            return;
        }

        // 3) Instant / no-target
        controller.ExecuteAbility(selectedAbility, null);
        RefreshInteractivity();

    }

    // true for single-target unit selection (self/ally/enemy)
   bool RequiresUnitTarget(UnitAbility a)
    {
        if (a == null) return false;
        if (NeedsAreaTargeting(a)) return false; // AoE/ground/zone handled elsewhere

        bool needsUnitFlags = a.selfOnly || a.alliesOnly || a.enemiesOnly;

        // "True" line attack: Line + not ground + no zone
        bool isTrueLineAttack =
            a.areaType == AreaType.Line &&
            !a.groundTarget &&
            !a.spawnsZone;

        return needsUnitFlags || isTrueLineAttack;
    }

    // true for AoE, line or ground target
    bool NeedsAreaTargeting(UnitAbility a)
    {
        if (a == null) return false;

        // Circle AoE
        if (a.areaType == AreaType.Circle) return true;

        // Ground abilities (any shape)
        if (a.groundTarget) return true;

        // Line that spawns a zone (Storm Crossing etc.)
        if (a.areaType == AreaType.Line && a.spawnsZone)
            return true;

        // "True" line attacks (Piercing Shot) are NOT area-targeted here
        return false;
    }

    void OnActionHover(ActionButtonView v, UnityEngine.EventSystems.PointerEventData e)
    {
        // ── Tooltip ────────────────────────────────────────────────
        if (hoveredView != v)
        {
            hoveredView = v;
            AbilityTooltip.Show(v.IsMove ? null : v.Ability, e.position);
        }
        else
        {
            AbilityTooltip.Move(e.position);
        }

        if (TargeterController2D.Instance == null || controller == null)
            return;

        Debug.Log($"[HUD] HOVER: view={(v.IsMove ? "\\Move\\" : v.Ability?.abilityName)} " +
              $"controller={controller?.name}, selected={selectedAbility?.abilityName}");

        // ── MOVE button preview ─────────────────────────────────────
        if (v.IsMove)
        {
            if (controller.Movement != null)
            {
                float radius = controller.Movement.GetMaxWorldRadius();
                TargeterController2D.Instance.ShowMoveRange(controller, radius);
            }
            return;
        }

        var ability = v.Ability;
        if (ability == null) return;

        // ── Skip preview for self-only or no-target abilities ───────
        if (ability.selfOnly)
            return;

        if (!RequiresUnitTarget(ability) && !NeedsAreaTargeting(ability))
            return;

        // ── Correct preview for all real abilities ──────────────────
        TargeterController2D.Instance.BeginSinglePreview(controller, ability);
        _singlePreviewActive = true;
        _singlePreviewAbility = ability;
    }

    void OnActionUnhover()
    {
        var prev = hoveredView;
        hoveredView = null;
        AbilityTooltip.Hide();

        Debug.Log($"[HUD] UNHOVER: prev={(prev?.IsMove == true ? "Move" : prev?.Ability?.abilityName)} " +
              $"controller={controller?.name}, selected={selectedAbility?.abilityName}");

        if (TargeterController2D.Instance == null || controller == null)
            return;

        // If leaving move button
        if (prev != null && prev.IsMove)
        {
            if (UnitController.GetCurrentAction() != UnitAction.Move)
                TargeterController2D.Instance.HideMoveRange();
            return;
        }

        // If leaving an ability button
        if (prev != null && prev.Ability != null)
        {
            // Do not kill preview for the *selected* ability
            if (selectedAbility == prev.Ability)
                return;

            // End hover-preview
            OnAbilityHoverExit();
            _singlePreviewActive = false;
            _singlePreviewAbility = null;
        }
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
        RefreshInteractivity();
    }

    private bool IsUsableNow()
    {
        if (controller == null || controller.model == null) return false;
        if (TurnManager.Instance == null) return false;
        if (!TurnManager.Instance.IsCurrentTurn(controller.unit)) return false;
        if (controller.photonView == null || !controller.photonView.IsMine) return false;
        if (!controller.model.CanAct()) return false;
        return true;
    }


    private void RefreshInteractivity()
    {
        bool canUse = IsUsableNow();

        if (rootCg != null)
        {
            rootCg.interactable = canUse;
            rootCg.blocksRaycasts = canUse;
            rootCg.alpha = canUse ? 1f : 0.6f;
        }

        foreach (var v in _active)
        {
            var btn = v.GetComponent<Button>();
            if (btn) btn.interactable = canUse;
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
        // Intentar encontrar el PauseMenuController si no está asignado
        if (pauseMenuController == null)
        {
            // Buscar también en GameObjects inactivos
            var controllers = Resources.FindObjectsOfTypeAll<PauseMenuController>();
            if (controllers != null && controllers.Length > 0)
            {
                pauseMenuController = controllers[0];
            }

            if (pauseMenuController == null)
            {
                Debug.LogWarning("[CombatHUD] PauseMenuController no encontrado en la escena. Por favor agrega el componente PauseMenuController a un GameObject.");
                return;
            }
        }

        pauseMenuController.TogglePause();
    }

    private void RebuildResources(UnitModel model)
    {
        if (resourceRoot == null) return;

        if (model == null)
        {
            resourceRoot.SetActive(false);
            return;
        }

        var key = model.PrimaryResourceKey;

        // If this unit doesn't use a primary resource, hide the panel
        if (string.IsNullOrEmpty(key))
        {
            resourceRoot.SetActive(false);
            return;
        }

        resourceRoot.SetActive(true);

        // Icon
        if (resourceIcon != null)
        {
            resourceIcon.sprite = model.PrimaryResourceIcon;
            resourceIcon.enabled = (model.PrimaryResourceIcon != null);
        }

        // Amount (show even if 0)
        if (resourceAmountText != null)
        {
            int amount = model.GetRes(key);
            resourceAmountText.text = amount.ToString();
        }
    }

    private void OnResChanged(string key, int cur)
    {
        // Only care about the bound unit's primary resource
        if (boundModel == null) return;
        if (key != boundModel.PrimaryResourceKey) return;

        RebuildResources(boundModel);
    }

    private void OnFormChanged(string newFormId)
    {
        if (boundModel == null) return;
        // Icon depends on form, so just rebuild
        RebuildResources(boundModel);
    }

    void CacheEndTurnVisuals()
    {
        if (endTurnButton != null && endTurnImage == null)
            endTurnImage = endTurnButton.image;

        if (endTurnImage != null && !endVisualsCached)
        {
            endOriginalColor = endTurnImage.color;
            endOriginalScale = endTurnImage.rectTransform.localScale;

            if (endTurnGlowImage != null)
            {
                glowOriginalScale = endTurnGlowImage.rectTransform.localScale;
                // start hidden
                var c = endTurnGlowImage.color;
                endTurnGlowImage.color = new Color(c.r, c.g, c.b, 0f);
            }

            endVisualsCached = true;
        }
    }

    void UpdateEndTurnFX()
    {
        if (!endPulseActive || endTurnImage == null || !endVisualsCached)
            return;

        // 0 -> 1 -> 0 ping-pong over time
        float phase = (Mathf.Sin(Time.unscaledTime * endFlashSpeed) + 1f) * 0.5f;

        // Icon pulse (size only)
        float scaleFactor = Mathf.Lerp(1f, endPulseScale, phase);
        var rt = endTurnImage.rectTransform;
        rt.localScale = endOriginalScale * scaleFactor;

        // Normalized 0..1 between base and max size
        float norm = Mathf.InverseLerp(1f, endPulseScale, scaleFactor);

        // Glow image: fade + grow/shrink
        if (endTurnGlowImage != null)
        {
            var glowRT = endTurnGlowImage.rectTransform;

            // Slightly bigger than the icon
            float glowScale = Mathf.Lerp(1f, glowScaleMultiplier, norm);
            glowRT.localScale = glowOriginalScale * glowScale;

            // Alpha from 0 -> max -> 0
            var c = endTurnGlowImage.color;
            c.a = norm * glowMaxAlpha;
            endTurnGlowImage.color = c;
        }

        // You can keep the main icon color fixed now
        endTurnImage.color = endOriginalColor;
    }

    void StartEndTurnAttention()
    {
        CacheEndTurnVisuals();
        endPulseActive = true;
    }

    void StopEndTurnAttention()
    {
        endPulseActive = false;

        if (endTurnImage != null && endVisualsCached)
        {
            endTurnImage.color = endOriginalColor;
            endTurnImage.rectTransform.localScale = endOriginalScale;
        }

        if (endTurnGlowImage != null && endVisualsCached)
        {
            var c = endTurnGlowImage.color;
            c.a = 0f;
            endTurnGlowImage.color = c;
            endTurnGlowImage.rectTransform.localScale = glowOriginalScale;
        }
    }

    // Called when mouse enters an ability button
    private void OnAbilityHoverEnter(UnitAbility ability)
    {
        if (TargeterController2D.Instance != null && controller != null && ability != null)
        {
            TargeterController2D.Instance.BeginSinglePreview(controller, ability);
        }
    }

    // Called when mouse exits an ability button
    private void OnAbilityHoverExit()
    {
        if (TargeterController2D.Instance != null)
        {
            TargeterController2D.Instance.EndSinglePreview();
        }
    }

    // Called by UnitController after an ability actually fires
    public void ClearSelectedAbilityIf(UnitController caster, UnitAbility ability)
    {
        Debug.Log($"[HUD] ClearSelectedAbilityIf: hudController={controller?.name}, " +
              $"caster={caster?.name}, ability={ability?.abilityName}, " +
              $"selected={selectedAbility?.abilityName}");


        // Only react if this HUD is bound to that unit
        if (controller != caster) return;
        if (ability == null) return;

        // Only clear if this was the ability that was selected on this HUD
        if (selectedAbility != ability) return;

        selectedAbility = null;
        UpdateSelectedHighlight();

        // Kill any single preview / aim mode
        if (TargeterController2D.Instance != null)
        {
            TargeterController2D.Instance.EndSinglePreview();
            TargeterController2D.Instance.Cancel();
        }

        _singlePreviewActive = false;
        _singlePreviewAbility = null;

        // Also clear hover state & tooltip, just in case
        hoveredView = null;
        AbilityTooltip.Hide();
    }
}
