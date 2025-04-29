using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitController : MonoBehaviour
{
    public Unit unit;
    private bool isMoving = false;
    private UnitAbility selectedAbility;

    public void Initialize(Unit unit)
    {
        this.unit = unit;
    }

    void Update()
    {
        if (isMoving) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            switch (ActionUI.Instance.GetCurrentAction())
            {
                case UnitAction.Move:
                    TryMove();
                    break;
                case UnitAction.Attack:
                    TryAttack();
                    break;
                default:
                    // No action selected, do nothing
                    break;
            }
        }
    }

    public void TryMove()
    {
        if (!unit.Model.CanAct())
            return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;
        float moveSpeed = unit.Model.GetMovementSpeed();

        StartCoroutine(MoveToPosition(mouseWorldPos, moveSpeed));
    }

    private IEnumerator MoveToPosition(Vector3 targetPos, float moveSpeed)
    {
        isMoving = true;

        Vector3 direction = (targetPos - transform.position).normalized;
        unit.View.SetFacingDirection(direction);
        unit.View.PlayAnimation("Move");

        while (Vector3.Distance(transform.position, targetPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;

        unit.Model.SpendAction();
        isMoving = false;

        unit.View.PlayAnimation("Idle");

        ActionUI.Instance.ClearAction(); // After moving, clear action
    }

    public void TryAttack()
    {
        if (!unit.Model.CanAct() || unit.Model.CurrentActions < selectedAbility.actionCost)
        {
            Debug.Log("Not enough actions to use this ability.");
            return;
        }

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
}
