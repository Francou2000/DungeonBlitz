using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

public class UnitController : MonoBehaviourPun
{
    public Unit unit;
    private bool isMoving = false;
    private UnitAbility selectedAbility;

    public bool isControllable = true;

    private UnitMovement movement;

    public UnitMovement Movement => movement;

    public static UnitController ActiveUnit { get;  set; }

    public Transform dmgPopUp;

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

    public void Initialize(Unit unit)
    {
        this.unit = unit;
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

    public void TryMove()
    {
        if (!unit.Model.CanAct() || !photonView.IsMine) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;

        photonView.RPC("RPC_TryMove", RpcTarget.All, mouseWorld.x, mouseWorld.y);
    }

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

        // Calculate hit logic
        int affinity = unit.Model.Affinity;
        int flankCount = CombatCalculator.CountFlankingAllies(targetUnit, unit);
        bool isFlanked = flankCount > 0;
        bool hasMediumCover, hasHeavyCover;

        CombatCalculator.CheckCover(
            transform.position,
            targetUnit.transform.position,
            out hasMediumCover,
            out hasHeavyCover
        );

        float hitChance = CombatCalculator.GetHitChance(
            selectedAbility.baseHitChance,
            affinity,
            flankCount,
            isFlanked,
            hasMediumCover,
            hasHeavyCover
        );

        float roll = Random.Range(0f, 100f);
        if (roll > hitChance)
        {
            Debug.Log($"[Attack] Missed! Rolled {roll:F1} vs {hitChance:F1}");
            ActionUsed();
            return;
        }

        Debug.Log($"[Attack] {unit.Model.UnitName} hits {targetUnit.Model.UnitName}!");

        int attackStat = selectedAbility.damageSource == DamageSourceType.Strength
            ? unit.Model.Strength
            : unit.Model.MagicPower;

        int defense = selectedAbility.damageSource == DamageSourceType.Strength
            ? targetUnit.Model.Armor
            : targetUnit.Model.MagicResistance;

        int totalDamage = 0;
        DamageType damageType = selectedAbility.damageSource == DamageSourceType.Strength ? DamageType.Physical : DamageType.Magical;

        for (int i = 0; i < selectedAbility.hits; i++)
        {
            float damage = CombatCalculator.CalculateDamage(selectedAbility.baseDamage, attackStat, defense);
            totalDamage += Mathf.RoundToInt(damage);
        }

        // Apply status effects locally (optional to sync later)
        var handler = targetUnit.GetComponent<StatusEffectHandler>();
        if (handler != null && selectedAbility.appliedEffects != null)
        {
            foreach (var effect in selectedAbility.appliedEffects)
                handler.ApplyEffect(effect);
        }

        // Sync damage
        int targetID = targetUnit.GetComponent<PhotonView>().ViewID;
        photonView.RPC(nameof(RPC_ApplyDamage), RpcTarget.All, targetID, totalDamage, (int)damageType);

        //Adrenaline mechanic
        unit.Model.AddAdrenaline(5);

        ActionUsed();
    }

    private void ActionUsed()
    {
        unit.Model.SpendAction(selectedAbility.actionCost);

        selectedAbility = null;
        SetAction(UnitAction.None);

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
            ActionUI.Instance.ClearAction();
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
        UnitGotDamaged(damageAmount, damageType);

    }

    public void UnitGotDamaged(int dmg_amount, DamageType dmg_type)
    {
        unit.View.PlayOneShotAnimation(AnimationName.Hit);
        //TODO: Add damage ui feedback
        unit.Model.TakeDamage(dmg_amount, dmg_type);
        Vector3 poUP_position = Camera.main.WorldToScreenPoint(dmgPopUp.position);
        FeedbackDisplayManager.Instance.showDmgFeedback(dmg_amount, poUP_position);
    }
    public void UnitDied()
    {
        unit.View.DeadAnimation();
        unit.Model.Die();
    }
}
