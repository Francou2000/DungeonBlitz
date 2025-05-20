using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitController : MonoBehaviour
{
    public Unit unit;
    private bool isMoving = false;
    private UnitAbility selectedAbility;

    public bool isControllable = true;

    public UnitMovement movement;

    public static UnitController ActiveUnit { get; private set; }

    [Header("Move Range Indicator")]
    [SerializeField] private GameObject rangeIndicatorPrefab;
    private GameObject rangeIndicatorInstance;

    public void Initialize(Unit unit)
    {
        this.unit = unit;
        movement = GetComponent<UnitMovement>();
        ActiveUnit = this;
    }

    void Update()
    {
        if (!isControllable) return; //prevent input if not allowed

        if (isMoving) return;

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
        if (!unit.Model.CanAct()) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;

        isMoving = true;

        movement.MoveTo(mouseWorld, () =>
        {
            ActionUI.Instance.ClearAction();
            isMoving = false;
        });
    }

    public void TryAttack()
    {
        if (!unit.Model.CanAct() || unit.Model.CurrentActions < selectedAbility.actionCost)
        {
            Debug.Log("[Attack] Not enough actions to use this ability.");
            return;
        }
        
        if (selectedAbility == null) return;

        Vector3 clickWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(clickWorld, Vector2.zero);

        if (hit.collider != null)
        {
            var targetUnit = hit.collider.GetComponent<Unit>();
            if (targetUnit == null || selectedAbility == null) return;

            // Range check
            if (!CombatCalculator.IsInRange(
                    transform.position,
                    targetUnit.transform.position,
                    selectedAbility.range))
            {
                Debug.Log("[Attack] Target out of range");
                return;
            }

            // Build hit chance inputs
            int affinity = unit.Model.Affinity;
            int flankCount = CombatCalculator.CountFlankingAllies(targetUnit, unit);
            bool isFlanked = flankCount > 0;
            bool hasMediumCover, hasHeavyCover;

            CombatCalculator.CheckCover(
                attackerPos: transform.position,
                targetPos: targetUnit.transform.position,
                out hasMediumCover,
                out hasHeavyCover
            );

            float hitChance = CombatCalculator.GetHitChance(
                selectedAbility.baseHitChance,
                affinity,
                flankCount,
                isFlanked,
                hasMediumCover,
                hasHeavyCover);

            // Roll to hit
            float roll = Random.value * 100f;
            if (roll > hitChance)
            {
                Debug.Log($"[Attack] Missed! Rolled {roll:F1} vs {hitChance:F1}");
            }
            else
            {
                Debug.Log($"[Attack] {unit.Model.UnitName} hits {targetUnit.Model.UnitName}!");

                int attackStat = selectedAbility.damageSource switch
                {
                    DamageSourceType.Strength => unit.Model.Strength,
                    DamageSourceType.MagicPower => unit.Model.MagicPower,
                    _ => 0
                };

                int defense = selectedAbility.damageSource == DamageSourceType.Strength
                    ? targetUnit.Model.Armor
                    : targetUnit.Model.MagicResistance;

                for (int i = 0; i < selectedAbility.hits; i++)
                {
                    float damage = CombatCalculator.CalculateDamage(
                        selectedAbility.baseDamage,
                        attackStat,
                        defense
                    );

                    DamageType type = selectedAbility.damageSource == DamageSourceType.Strength
                        ? DamageType.Physical
                        : DamageType.Magical;

                    targetUnit.Model.TakeDamage(Mathf.RoundToInt(damage), type);
                }
            }

            // Consume action & adrenaline
            unit.Model.SpendAction(selectedAbility.actionCost);
            unit.Model.AddAdrenaline(5);

            // Clear state
            selectedAbility = null;
            ActionUI.Instance.ClearAction();
        }
    }

    public void SetSelectedAbility(UnitAbility ability)
    {
        selectedAbility = ability;
    }

    private static UnitAction currentAction = UnitAction.None;
    public static void SetAction(UnitAction action) => currentAction = action;
    public static UnitAction GetCurrentAction() => currentAction;
}
