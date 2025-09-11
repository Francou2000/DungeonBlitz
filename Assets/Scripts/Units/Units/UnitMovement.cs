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

    //Shows a visual indicator of the movement range
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

    //Moves to the given world position, clamped to range. Calls onFinish when done
    public void MoveTo(Vector3 mouseWorld, System.Action onFinish)
    {
        Vector3 oldPosition = unit.transform.position;

        Vector3 clampedTarget = ClampToRange(mouseWorld, oldPosition);
        float speed = unit.Model.GetMovementSpeed();

        unit.StartCoroutine(MoveCoroutine(clampedTarget, oldPosition, speed, () =>
        {
            //After finishing movement, check for reactions
            ReactionManager.Instance?.TryTriggerReactions(unit, oldPosition);
            onFinish?.Invoke();
        }));
    }

    //Ensures the target is within move range
    private Vector3 ClampToRange(Vector3 target, Vector3 origin)
    {
        float range = unit.Model.Performance * unit.Model.MoveDistanceFactor;
        Vector3 dir = target - origin;

        return dir.magnitude > range ? origin + dir.normalized * range : target;
    }

    private IEnumerator MoveCoroutine(Vector3 targetPos, Vector3 oldPosition, float speed, System.Action onFinish)
    {
        unit.View.SetFacingDirection((targetPos - transform.position).normalized);
        unit.View.PlayAnimation("Move");

        while (Vector3.Distance(transform.position, targetPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }
        
        var from = oldPosition;   
        var to = transform.position; 

        if (unit != null && unit.Model?.statusHandler != null)
        {
            unit.Model.statusHandler.OnMove(unit, from, to);
        }

        // (re)enable reactions call here as well, immediately after move:
        ReactionManager.Instance?.TryTriggerReactions(unit, from);

        unit.Model.SpendAction();
        unit.View.PlayAnimation("Idle");

        onFinish?.Invoke();
    }
}
