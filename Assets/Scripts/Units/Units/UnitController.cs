using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitController : MonoBehaviour
{
    public Unit unit;
    private bool isMoving = false;
    private UnitAbility selectedAbility;

    public static UnitController ActiveUnit { get; private set; }

    [Header("Move Range Indicator")]
    [SerializeField] private GameObject rangeIndicatorPrefab;
    private GameObject rangeIndicatorInstance;

    public void Initialize(Unit unit)
    {
        this.unit = unit;
        ActiveUnit = this;
    }

    void Update()
    {
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
        if (!unit.Model.CanAct())
            return;

        //Get clicked world position
        Vector3 clickWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        clickWorld.z = transform.position.z;

        //Compute max allowed distance this turn
        float maxDistance = unit.Model.Performance * unit.Model.MoveDistanceFactor;

        //Clamp targetPos to within range
        Vector3 origin = transform.position;
        Vector3 dir = clickWorld - origin;
        if (dir.magnitude > maxDistance)
        {
            dir.Normalize();
            clickWorld = origin + dir * maxDistance;
            Debug.Log($"[Controller] Click beyond range, clamped to {clickWorld}");
        }

        float moveSpeed = unit.Model.GetMovementSpeed();
        Debug.Log($"[Controller] Moving to {clickWorld} at speed {moveSpeed}");

        StartCoroutine(MoveToPosition(clickWorld, moveSpeed));
    }

    private IEnumerator MoveToPosition(Vector3 targetPos, float moveSpeed)
    {
        isMoving = true;

        Vector3 direction = (targetPos - transform.position).normalized;
        unit.View.SetFacingDirection(direction);
        unit.View.PlayAnimation("Move");

        //Smooth movement
        while (Vector3.Distance(transform.position, targetPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        //Snap exactly
        transform.position = targetPos;

        //Spend action & reset state
        unit.Model.SpendAction();
        isMoving = false;
        unit.View.PlayAnimation("Idle");
        ActionUI.Instance.ClearAction();
    }

    public void ShowMoveRange()
    {
        if (rangeIndicatorInstance == null)
        {
            rangeIndicatorInstance = Instantiate(
                rangeIndicatorPrefab,
                transform.position,
                Quaternion.identity,
                transform  // parent under the unit
            );
        }
        float range = unit.Model.Performance * unit.Model.MoveDistanceFactor;
        // Since the sprite’s radius is 0.5 units, scale = range*2
        rangeIndicatorInstance.transform.localScale = new Vector3(range * 2f, range * 2f, 1f);
        rangeIndicatorInstance.SetActive(true);
    }

    public void HideMoveRange()
    {
        if (rangeIndicatorInstance != null)
            rangeIndicatorInstance.SetActive(false);
    }

    public void TryAttack()
    {
        if (!unit.Model.CanAct() || unit.Model.CurrentActions < selectedAbility.actionCost)
        {
            Debug.Log("Not enough actions to use this ability.");
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
                Debug.Log("Target out of range");
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
                Debug.Log($"Missed! Rolled {roll:F1} vs {hitChance:F1}");
            }
            else
            {
                Debug.Log($"{unit.Model.UnitName} hits {targetUnit.Model.UnitName}!");

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
