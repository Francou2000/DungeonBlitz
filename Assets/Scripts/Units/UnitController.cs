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
        Debug.Log("[Controller] Attack logic would run here");
        
        if (selectedAbility == null) return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
        if (hit.collider != null)
        {
            Unit targetUnit = hit.collider.GetComponent<Unit>();
            if (targetUnit != null && targetUnit != unit)
            {
                if (selectedAbility.requiresAnxietyThreshold &&
                    unit.Model.Anxiety < selectedAbility.anxietyThreshold)
                {
                    Debug.Log("Not enough anxiety for this ability.");
                    return;
                }

                Debug.Log($"{unit.Model.UnitName} uses {selectedAbility.abilityName}!");

                for (int i = 0; i < selectedAbility.hits; i++)
                {
                    int attackStat = unit.Model.Strength + selectedAbility.baseDamage;
                    targetUnit.Model.TakePhysicalDamage(attackStat);
                }

                unit.Model.SpendAction();
                unit.Model.AddAnxiety(5); //each attack raises anxiety for now
                ActionUI.Instance.ClearAction();
                unit.View.PlayAnimation("Attack"); //Placeholder for later

                selectedAbility = null; // Reset after use
            }
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
