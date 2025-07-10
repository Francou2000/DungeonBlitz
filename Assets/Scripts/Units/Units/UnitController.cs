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
        clickWorld.z = 0f;

        photonView.RPC(nameof(RPC_TryAttack), RpcTarget.All, clickWorld.x, clickWorld.y, selectedAbility.abilityName);
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
    public void RPC_TryAttack(float x, float y, string abilityName)
    {
        Vector3 clickWorld = new Vector3(x, y, 0);
        RaycastHit2D hit = Physics2D.Raycast(clickWorld, Vector2.zero);

        if (hit.collider == null) return;

        var targetUnit = hit.collider.GetComponent<Unit>();
        if (targetUnit == null || unit == targetUnit) return;

        var ability = unit.Model.Abilities.Find(a => a.abilityName == abilityName);
        if (ability == null) return;

        if (!CombatCalculator.IsInRange(transform.position, targetUnit.transform.position, ability.range))
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
            ability.baseHitChance,
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
            return;
        }

        Debug.Log($"[Attack] {unit.Model.UnitName} hits {targetUnit.Model.UnitName}!");

        int attackStat = ability.damageSource == DamageSourceType.Strength
            ? unit.Model.Strength
            : unit.Model.MagicPower;

        int defense = ability.damageSource == DamageSourceType.Strength
            ? targetUnit.Model.Armor
            : targetUnit.Model.MagicResistance;

        for (int i = 0; i < ability.hits; i++)
        {
            float damage = CombatCalculator.CalculateDamage(ability.baseDamage, attackStat, defense);
            DamageType type = ability.damageSource == DamageSourceType.Strength ? DamageType.Physical : DamageType.Magical;
            targetUnit.Model.TakeDamage(Mathf.RoundToInt(damage), type);
        }

        var handler = targetUnit.GetComponent<StatusEffectHandler>();
        if (handler != null && ability.appliedEffects != null)
        {
            foreach (var effect in ability.appliedEffects)
                handler.ApplyEffect(effect);
        }

        unit.Model.SpendAction(ability.actionCost);
        unit.Model.AddAdrenaline(5);
        ActionUI.Instance.ClearAction();
    }
}
