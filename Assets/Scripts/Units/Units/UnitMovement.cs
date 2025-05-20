using System.Collections;
using UnityEngine;

public class UnitMovement : MonoBehaviour
{
    private Unit unit;
    private GameObject rangeIndicator;

    [SerializeField] private GameObject rangeIndicatorPrefab;
    private void Awake()
    {
        unit = GetComponent<Unit>();
    }

    public void ShowRange()
    {
        if (rangeIndicator == null)
        {
            rangeIndicator = Instantiate(
                rangeIndicatorPrefab,
                unit.transform.position,
                Quaternion.identity
            );
        }

        float radius = unit.Model.Performance * unit.Model.MoveDistanceFactor;
        rangeIndicator.transform.position = unit.transform.position;
        rangeIndicator.transform.localScale = Vector3.one * radius * 2f;
        rangeIndicator.SetActive(true);
    }

    public void HideRange()
    {
        if (rangeIndicator != null)
            rangeIndicator.SetActive(false);
    }

    public void MoveTo(Vector3 mouseWorld, System.Action onFinish)
    {
        Vector3 clampedTarget = ClampToRange(mouseWorld, unit.transform.position);
        float speed = unit.Model.GetMovementSpeed();

        unit.StartCoroutine(MoveCoroutine(clampedTarget, speed, onFinish));
    }

    private Vector3 ClampToRange(Vector3 target, Vector3 origin)
    {
        float range = unit.Model.Performance * unit.Model.MoveDistanceFactor;
        Vector3 dir = target - origin;

        return dir.magnitude > range ? origin + dir.normalized * range : target;
    }

    private IEnumerator MoveCoroutine(Vector3 targetPos, float speed, System.Action onFinish)
    {
        unit.View.SetFacingDirection((targetPos - transform.position).normalized);
        unit.View.PlayAnimation("Move");

        while (Vector3.Distance(transform.position, targetPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;

        unit.Model.SpendAction();
        unit.View.PlayAnimation("Idle");

        onFinish?.Invoke();
    }
}
