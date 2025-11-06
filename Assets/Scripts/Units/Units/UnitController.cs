using System.Collections;
using System.Collections.Generic;
using DebugTools;
//using Mono.Cecil;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitController : MonoBehaviourPun
{
    public Unit unit;
    public UnitModel model;
    private bool isMoving = false;
    private UnitAbility selectedAbility;

    private Vector3? cachedAimPos;
    private Vector3? cachedAimDir;

    // Targeting system
    private bool isWaitingForTarget = false;
    private UnitAbility pendingAbility = null;
    private Vector3? pendingTargetPosition = null;

    public bool isControllable = true;

    private UnitMovement movement;

    public UnitMovement Movement => movement;

    private static UnitController activeUnit;
    public static UnitController ActiveUnit
    {
        get => activeUnit;
        set
        {
            if (activeUnit == value) return;
            activeUnit = value;

            // Notify the HUD path 
            TurnManager.OnActiveControllerChanged?.Invoke(activeUnit);
        }
    }

    private bool isCasting;
    private Coroutine unlockCastCo;

    void Awake()
    {
        unit = GetComponent<Unit>();

        if (model == null)
            model = GetComponent<UnitModel>();

        if (isControllable)
        {
            movement = GetComponent<UnitMovement>();
            if (movement == null)
                Debug.LogError("[UnitController] No UnitMovement found on this controllable unit!");
        }
    }

    public virtual void Initialize(Unit unit)
    {
        this.unit = unit;
        this.model = unit.Model;
    }
    
    void Update()
    {
        if (!isControllable) 
        {
            //Debug.Log($"[UnitController] {name} not controllable");
            return;
        }
        
        if (ActiveUnit != this) 
        {
            //Debug.Log($"[UnitController] {name} not active unit (active: {ActiveUnit?.name})");
            return;
        }

        if (isMoving) 
        {
            Debug.Log($"[UnitController] {name} is moving");
            return;
        }

        if (TurnManager.Instance != null && !TurnManager.Instance.IsCurrentTurn(unit))
        {
            //Debug.Log($"[UnitController] {name} not current turn (current: {TurnManager.Instance?.currentTurn})");
            return;
        }

        if (!photonView.IsMine) 
        {
            //Debug.Log($"[UnitController] {name} photonView not mine");
            return;
        }

        // Avoid handling input when clicking on UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log($"[UnitController] {name} clicking on UI");
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"[UnitController] {name} mouse clicked, current action: {GetCurrentAction()}");
            
            if (isWaitingForTarget)
            {
                HandleTargetSelection();
            }
            else
            {
                switch (GetCurrentAction())
                {
                    case UnitAction.Move: TryMove(); break;
                    case UnitAction.Attack: TryAttack(); break;
                }
            }
        }
        
        if (Input.GetMouseButtonDown(1)) // Right click to cancel targeting
        {
            if (isWaitingForTarget)
            {
                CancelTargeting();
            }
        }
    }
    
    // New ability execution system
    public virtual void ExecuteAbility(UnitAbility ability, Unit target, Vector3 targetPosition = default)
    {
        if (ability == null) return;
        if (isCasting) return;          //  block duplicates
        if (TurnManager.Instance != null)
        {
            if (!TurnManager.Instance.IsCurrentTurn(unit)) return;
        }
        if (!photonView.IsMine) return;
        if (!unit.Model.CanAct()) return;

        if (!PhotonNetwork.IsMasterClient && ability.resourceCosts != null)
        {
            foreach (var cost in ability.resourceCosts)
            {
                // This triggers UnitModel.OnResourceChanged, which your CombatHUD listens to.
                unit.Model.TryConsume(cost.key, cost.amount);
            }
        }

        BeginCastLock();                //  lock until we finish queuing the RPC

        // Aim from cache (Targeter2D) → else from provided targetPosition → else zero
        Vector3 aimPos = cachedAimPos.HasValue ? cachedAimPos.Value
                        : (targetPosition != default ? targetPosition : Vector3.zero);
        Vector3 aimDir = cachedAimDir.HasValue ? cachedAimDir.Value : Vector3.zero;

        Unit[] targetsArg;

        switch (ability.areaType)
        {
            case AreaType.Single:
                // Send the clicked unit if any (explicit single target)
                targetsArg = (target != null) ? new Unit[] { target } : System.Array.Empty<Unit>();
                break;

            case AreaType.Line:
                // Line needs a primary to define the line; send the clicked unit if we have one
                targetsArg = (target != null) ? new Unit[] { target } : System.Array.Empty<Unit>();
                break;

            case AreaType.Circle:
            default:
                // AoE/Ground target are center-only; resolver will collect victims from aimPos
                targetsArg = System.Array.Empty<Unit>();
                break;
        }

        var traceId = CombatLog.NewTraceId();
        CombatLog.Cast(traceId, $"Request by {CombatLog.Short(gameObject)} " +
            $"ability={ability?.name} turn={TurnManager.Instance?.turnNumber} owner={photonView?.OwnerActorNr}");

        AbilityResolver.Instance.RequestCast(this, ability, targetsArg, aimPos, aimDir, traceId);
        EndCastLockSoon();               // release lock next frame

        // clear cached aim after firing
        ClearAimCache();
    }

    protected virtual void OnAbilityExecuted(UnitAbility ability, Unit target)
    {
        // Base implementation - handle common ability effects
        if (ability.healsTarget && target != null)
        {
            int healAmount = ability.CalculateHealAmount(model);
            target.Heal(healAmount, unit);
        }

        // Apply status effects
        if (target != null && ability.appliedEffects.Count > 0)
        {
            var statusHandler = target.GetComponent<StatusEffectHandler>();
            if (statusHandler != null)
            {
                foreach (var effect in ability.appliedEffects)
                {
                    if (Random.Range(0f, 100f) <= ability.statusEffectChance)
                    {
                        statusHandler.ApplyEffect(effect);
                    }
                }
            }
        }

        // Grant barriers
        if (ability.grantsBarrier && target != null)
        {
            var barrierEffect = new StatusEffect
            {
                effectName = "Barrier",
                type = StatusEffectType.Buff,
                barrierHP = ability.barrierAmount,
                duration = 5,
                tags = { "Barrier" }
            };
            
            target.GetComponent<StatusEffectHandler>()?.ApplyEffect(barrierEffect);
        }
    }

    // Turn management hooks
    public virtual void OnTurnStart()
    {
        // Override in specialized controllers
    }

    public virtual void OnTurnEnd()
    {
        // Override in specialized controllers
    }

    // Damage handling hooks
    public virtual void OnDamageTaken(int damage, DamageType damageType, Unit attacker)
    {
        // Override in specialized controllers for special reactions
    }

    public virtual void OnHealed(int amount, Unit healer)
    {
        // Override in specialized controllers
    }

    // Adrenaline state hooks
    protected virtual void HandleAdrenalineStateEntered()
    {
        Debug.Log($"{model.UnitName} enters adrenaline state - applying buffs");
        // Override in specialized controllers for unit-specific adrenaline effects
    }

    protected virtual void HandleAdrenalineStateExited()
    {
        Debug.Log($"{model.UnitName} exits adrenaline state - removing buffs");
        // Override in specialized controllers
    }


    public void TryMove()
    {
        if (TurnManager.Instance != null && !TurnManager.Instance.IsCurrentTurn(unit))
            return;

        if (!unit.Model.CanAct() || !photonView.IsMine) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;

        photonView.RPC("RPC_TryMove", RpcTarget.All, mouseWorld.x, mouseWorld.y);
    }

    // Legacy attack system (keeping for compatibility)
    public void TryAttack()
    {
        AudioManager.Instance.PlayButtonSound();
        Debug.Log("Trying to attack with ability: " + selectedAbility.abilityName);
        if (!unit.Model.CanAct() || selectedAbility == null || !photonView.IsMine)
            return;

        Vector3 clickWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(clickWorld, Vector2.zero);

        if (hit.collider == null) return;

        var targetUnit = hit.collider.GetComponent<Unit>();
        if (targetUnit == null || unit == targetUnit) return;

        if (!CombatCalculator.IsInRange(transform.position, targetUnit.transform.position, selectedAbility.range))
        {
            Debug.Log("[Attack] Target out of range");
            return;
        }

        unit.UseAbility(selectedAbility, targetUnit);
        ActionUsed();
    }

    private void ActionUsed()
    {
        selectedAbility = null;
        SetAction(UnitAction.None);

        //Old UI system
        if (ActionUI.Instance != null)
            ActionUI.Instance.ClearAction();       
    }

    public void SetSelectedAbility(UnitAbility ability)
    {
        selectedAbility = ability;
    }

    private static UnitAction currentAction = UnitAction.None;
    public static void SetAction(UnitAction action) => currentAction = action;
    public static UnitAction GetCurrentAction() => currentAction;

    [PunRPC]
    public void RPC_TryMove(float x, float y)
    {
        Vector3 target = new Vector3(x, y, 0f);
        isMoving = true;

        movement.MoveTo(target, () =>
        {
            UnitController.SetAction(UnitAction.None);
            if (TargeterController2D.Instance)
                TargeterController2D.Instance.HideMoveRange();

            isMoving = false;
        });
    }

    [PunRPC]
    public void RPC_ApplyDamage(int targetViewID, int damageAmount, int damageTypeInt)
    {
        PhotonView targetView = PhotonView.Find(targetViewID);
        if (targetView == null)
        {
            Debug.LogWarning("[Attack] Target PhotonView not found.");
            return;
        }

        Unit targetUnit = targetView.GetComponent<Unit>();
        if (targetUnit == null)
        {
            Debug.LogWarning("[Attack] Target Unit not found.");
            return;
        }

        DamageType damageType = (DamageType)damageTypeInt;
        targetUnit.Model.ApplyDamageWithBarrier(damageAmount, damageType);
    }

    // Helper method for specialized controllers
    protected UnitAbility GetAbilityByName(string abilityName)
    {
        var abilities = model.GetAvailableAbilities();
        return abilities.Find(a => a.abilityName == abilityName);
    }

    // Check if unit should monitor adrenaline state changes
    private bool wasInAdrenalineState = false;
    
    protected virtual void LateUpdate()
    {
        // Monitor adrenaline state changes
        if (model != null)
        {
            bool currentlyInAdrenalineState = model.IsInAdrenalineState;
            
            if (!wasInAdrenalineState && currentlyInAdrenalineState)
            {
                HandleAdrenalineStateEntered();
            }
            else if (wasInAdrenalineState && !currentlyInAdrenalineState)
            {
                HandleAdrenalineStateExited();
            }
            
            wasInAdrenalineState = currentlyInAdrenalineState;
        }
    }

    // Aiming

    public void CacheAim(Vector3 pos, Vector3 dir)
    {
        cachedAimPos = pos;
        cachedAimDir = dir;
    }

    private void ClearAimCache()
    {
        cachedAimPos = null;
        cachedAimDir = null;
    }

    // Targeting system methods
    public void StartTargeting(UnitAbility ability)
    {
        if (TurnManager.Instance != null && !TurnManager.Instance.IsCurrentTurn(unit))
            return;

        isWaitingForTarget = true;
        pendingAbility = ability;
        pendingTargetPosition = null;
        
        Debug.Log($"[UnitController] Started targeting for {ability.abilityName}");
        
        if (ability.alliesOnly)
        {
            LogAlliesInRange(ability.range);
        }
    }

    private void HandleTargetSelection()
    {
        if (pendingAbility == null) return;

        Vector3 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 clickPos2D = new Vector2(clickPos.x, clickPos.y);

        RaycastHit2D hit = Physics2D.Raycast(clickPos2D, Vector2.zero);
        
        if (hit.collider != null)
        {
            Unit clickedUnit = hit.collider.GetComponent<Unit>();
            if (clickedUnit != null)
            {
                HandleUnitTarget(clickedUnit);
                return;
            }
        }

        // If no unit clicked, use ground position
        HandleGroundTarget(clickPos);
    }

    private void HandleUnitTarget(Unit target)
    {
        if (pendingAbility.alliesOnly)
        {
            if (target.Model.Faction == unit.Model.Faction)
            {
                Debug.Log($"[UnitController] Selected ally target: {target.name}");
                ExecuteAbility(pendingAbility, target);
                CancelTargeting();
            }
            else
            {
                Debug.Log($"[UnitController] Invalid target: {target.name} is not an ally");
            }
        }
        else if (pendingAbility.enemiesOnly)
        {
            if (target.Model.Faction != unit.Model.Faction)
            {
                Debug.Log($"[UnitController] Selected enemy target: {target.name}");
                ExecuteAbility(pendingAbility, target);
                CancelTargeting();
            }
            else
            {
                Debug.Log($"[UnitController] Invalid target: {target.name} is not an enemy");
            }
        }
    }

    private void HandleGroundTarget(Vector3 position)
    {
        if (pendingAbility.groundTarget)
        {
            Debug.Log($"[UnitController] Selected ground target at: {position}");
            ExecuteAbility(pendingAbility, null, position);
            CancelTargeting();
        }
        else
        {
            Debug.Log($"[UnitController] Ground targeting not allowed for this ability");
        }
    }

    public void CancelTargeting()
    {
        // kill single-target waiters
        isWaitingForTarget = false;
        pendingAbility = null;
        pendingTargetPosition = null;

        // clear aim cached by Targeter2D (AoE/Line)
        ClearAimCache();

        // exit move mode
        SetAction(UnitAction.None);

        // hide HUD/targeter visuals if this unit was showing them
        if (TargeterController2D.Instance)
            TargeterController2D.Instance.HideMoveRange();
    }

    private void LogAlliesInRange(int range)
    {
        var allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        var allies = new List<Unit>();
        
        foreach (var unit in allUnits)
        {
            if (unit != this.unit && unit.Model.Faction == this.unit.Model.Faction && unit.Model.IsAlive())
            {
                float distance = Vector3.Distance(this.unit.transform.position, unit.transform.position);
                if (distance <= range)
                {
                    allies.Add(unit);
                }
            }
        }
        
        Debug.Log($"[UnitController] Allies in range ({range}): {allies.Count}");
        foreach (var ally in allies)
        {
            float distance = Vector3.Distance(this.unit.transform.position, ally.transform.position);
            Debug.Log($"  - {ally.name} (HP: {ally.Model.CurrentHP}/{ally.Model.MaxHP}, Distance: {distance:F1})");
        }
    }

    private IEnumerator UnlockCastingNextFrame()
    {
        yield return null;      // one-frame debounce
        isCasting = false;
        unlockCastCo = null;
    }
    private void BeginCastLock()
    {
        if (unlockCastCo != null) StopCoroutine(unlockCastCo);
        isCasting = true;
    }
    private void EndCastLockSoon()
    {
        if (unlockCastCo != null) StopCoroutine(unlockCastCo);
        unlockCastCo = StartCoroutine(UnlockCastingNextFrame());
    }
}
