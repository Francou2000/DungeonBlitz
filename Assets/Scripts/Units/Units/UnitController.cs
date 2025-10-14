using System.Collections;
using System.Collections.Generic;
using DebugTools;
using Mono.Cecil;
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

    public bool isControllable = true;

    private UnitMovement movement;

    public UnitMovement Movement => movement;

    private static UnitController _activeUnit;
    public static UnitController ActiveUnit
    {
        get => _activeUnit;
        set
        {
            if (_activeUnit == value) return;
            _activeUnit = value;

            // Notify the HUD path 
            TurnManager.OnActiveControllerChanged?.Invoke(_activeUnit);
        }
    }

    void Awake()
    {
        unit = GetComponent<Unit>();

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
        if (!isControllable || ActiveUnit != this )
            return;

        if (isMoving) return;

        if (TurnManager.Instance != null && !TurnManager.Instance.IsCurrentTurn(unit))
            return;

        if (!photonView.IsMine) return;

        // Avoid handling input when clicking on UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            switch (GetCurrentAction())
            {
                case UnitAction.Move: TryMove(); break;
                case UnitAction.Attack: TryAttack(); break;
            }
        }
    }
    
    // New ability execution system
    public virtual void ExecuteAbility(UnitAbility ability, Unit target, Vector3 targetPosition = default)
    {
        var traceId = CombatLog.NewTraceId();
        CombatLog.Cast(traceId, $"Request by {CombatLog.Short(gameObject)} " +
            $"ability={ability?.name} turn={TurnManager.Instance?.turnNumber} owner={photonView?.OwnerActorNr}");

        // Aim data:
        // - For AoE/Line: 'targetPosition' comes from CombatUI targeter, 'cachedAimDir' set by CacheAim().
        // - For Single/self: we don't need aimPos/aimDir.
        var isAoE = ability != null && (ability.areaType == AreaType.Circle || ability.areaType == AreaType.Line);

        var aimPos = isAoE
            ? targetPosition
            : (cachedAimPos ?? Vector3.zero);

        var aimDir = cachedAimDir ?? Vector3.zero;

        if (AbilityResolver.Instance != null)
        {
            var targetsArg = isAoE
                ? System.Array.Empty<Unit>()    // AoE/Line: resolver uses aimPos/aimDir
                : new Unit[] { target };        // Single: explicit target

            AbilityResolver.Instance.RequestCast(this, ability, targetsArg, aimPos, aimDir, traceId);
            ClearAimCache(); // prevent stale aim next time
            return;
        }

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
        if (!unit.Model.CanAct() || !photonView.IsMine) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;

        photonView.RPC("RPC_TryMove", RpcTarget.All, mouseWorld.x, mouseWorld.y);
    }

    // Legacy attack system (keeping for compatibility)
    public void TryAttack()
    {
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

        if (CombatUI.Instance != null)
            CombatUI.Instance.HideAbilities();
        
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
            // Exit move mode + hide any move preview
            UnitController.SetAction(UnitAction.None);
            MoveRangePreview.HideStatic();

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
}
