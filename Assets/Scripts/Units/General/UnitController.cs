using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitController : MonoBehaviour
{
    private Unit unit;
    private bool isMoving = false;


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

        StartCoroutine(MoveToPosition(mouseWorldPos, unit.Model.speed));
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
        if (!unit.Model.CanAct()) return;

        // Check if hovering over another unit
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
        if (hit.collider != null)
        {
            Unit targetUnit = hit.collider.GetComponent<Unit>();
            if (targetUnit != null && targetUnit != unit) // Make sure it's not attacking itself
            {
                if (!targetUnit.Model.isTrainingDummy)
                {
                    Debug.Log("Cannot attack a player-controlled unit.");
                    return;
                }

                int attackStat = unit.Model.strength;
                int defenseStat = targetUnit.Model.physicalDefense;
                int damage = Mathf.Max(0, attackStat - defenseStat); // No negative damage

                targetUnit.Model.TakeDamage(damage);

                unit.View.PlayAnimation("Attack");
                unit.Model.SpendAction();
                ActionUI.Instance.ClearAction();
            }
        }
    }
}
