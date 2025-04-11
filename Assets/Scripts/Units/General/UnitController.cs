using System.Collections;
using UnityEngine;

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
        if (Input.GetMouseButtonDown(0)) // Left mouse click
        {
            TryMove();
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
    }
}
