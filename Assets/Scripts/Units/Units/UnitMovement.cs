using System.Collections;
using UnityEngine;

public class UnitMovement : MonoBehaviour
{
    private Unit unit;
    [SerializeField] private float fallbackMaxMoveWorld = 3f;

    private void Awake()
    {
        unit = GetComponent<Unit>();
    }

    //Moves to the given world position, clamped to range. Calls onFinish when done
    public void MoveTo(Vector3 mouseWorld, System.Action onFinish)
    {
        Vector3 oldPosition = unit.transform.position;

        Vector3 clampedTarget = ClampToMoveRange(oldPosition, mouseWorld);
        float speed = unit.Model.GetMovementSpeed();

        Vector3 dir = (clampedTarget - oldPosition);
        unit.View.SetFacingDirection(dir);
        unit.View.PlayMoveStartNet(dir);

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
        unit.View.PlayMoveLandNet();

        onFinish?.Invoke();
    }

    private Vector3 ClampToMoveRange(Vector3 from, Vector3 mouseWorld)
    {
        Vector3 dir = mouseWorld - from;
        if (dir.sqrMagnitude <= 0.0001f) return from;

        float maxWorld = GetMaxWorldRadius();
        float dist = dir.magnitude;
        if (dist > maxWorld) dir = dir.normalized * maxWorld;

        return from + dir;
    }

    public float GetMaxWorldRadius()
    {
        // Fallback in case something is missing
        const float fallback = 3f;

        var m = unit != null ? unit.Model : null;
        if (m == null) return fallback;

        // Speed (units/sec) * action duration (sec) = units per action
        return m.GetMovementSpeed() * m.MoveTimeBase;
    }
}
