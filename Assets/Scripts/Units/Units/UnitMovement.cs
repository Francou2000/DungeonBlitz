using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class UnitMovement : MonoBehaviour
{
    private Unit unit;
    [SerializeField] private float fallbackMaxMoveWorld = 3f;
    [SerializeField] private float navMeshMaxDistance = 5f; // how far we search for navmesh from target

    private int walkableMask;

    private void Awake()
    {
        unit = GetComponent<Unit>();

        int walkableArea = NavMesh.GetAreaFromName("Walkable");
        if (walkableArea < 0)
        {
            Debug.LogError("[UnitMovement] NavMesh area 'Walkable' not found. Check Navigation Areas.");
            walkableMask = NavMesh.AllAreas;
        }
        else
        {
            walkableMask = 1 << walkableArea;
        }
    }

    private void Start()
    {
        unit = GetComponent<Unit>();

        if (NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
        {
            Vector3 p = hit.position;
            p.z = transform.position.z; // keep your visual plane
            transform.position = p;
        }
    }

    //Moves to the given world position, clamped to range. Calls onFinish when done
    public void MoveTo(Vector3 mouseWorld, System.Action onFinish)
    {
        Vector3 oldPosition = unit.transform.position;

        // clamp the desired point to the move radius (this matches your move targeter)
        float maxMoveDist = GetMaxWorldRadius();
        Vector3 desiredWithinRadius = ClampToMoveRange(oldPosition, mouseWorld);

        // status check
        var sc = GetComponent<StatusComponent>();
        if (sc != null && sc.IsRooted())
        {
            Debug.Log("[Move] Blocked by Root");
            onFinish?.Invoke();
            return;
        }

        // Build a NavMesh path to the clamped point and clamp along that path
        if (!TryBuildClampedPath(oldPosition, desiredWithinRadius, maxMoveDist, out var pathCorners))
        {
            // no valid path or no distance to move
            Debug.Log("[Move] No valid NavMesh path");
            onFinish?.Invoke();
            return;
        }

        pathCorners = CleanPath(pathCorners);

        if (pathCorners == null || pathCorners.Length <= 1)
        {
            onFinish?.Invoke();
            return;
        }

        float speed = unit.Model.GetMovementSpeed();
        Vector3 firstDir = (pathCorners[1] - pathCorners[0]);
        if (firstDir.sqrMagnitude > 0.0001f)
        {
            unit.View.SetFacingDirection(firstDir);
        }
        unit.View.PlayMoveStartNet(firstDir);

        unit.StartCoroutine(MoveAlongPathCoroutine(pathCorners, oldPosition, speed, () =>
        {
            ReactionManager.Instance?.TryTriggerReactions(unit, oldPosition);
            onFinish?.Invoke();
        }));
    }

    // --- PATH BUILDING -------------------------------------------------------

    // Builds a NavMesh path from "from" to "target" and clamps it to maxDistance
    // along the path. Returns the list of corners to follow.
    private bool TryBuildClampedPath(Vector3 from, Vector3 target, float maxDistance, out Vector3[] clampedCorners)
    {
        clampedCorners = null;

        // sample on ANY area (AllAreas) so we always get a triangle near the unit
        if (!SampleOnNav(from, true, out var fromNav))
        {
            return false;
        }

        // must be on Walkable only (so we don't end inside water)
        if (!SampleOnNav(target, false, out var targetNav))
        {
            return false;
        }

        var path = new NavMeshPath();

        // allow Complete and Partial paths (clicking on water should give Partial to the bank)
        if (!NavMesh.CalculatePath(fromNav, targetNav, walkableMask, path))
        {
            return false;
        }

        var raw = path.corners;
        if (raw == null || raw.Length == 0)
        {
            return false;
        }

        // We want to walk from the CURRENT position along this path,
        // limited to maxDistance.
        List<Vector3> result = new List<Vector3>();
        result.Add(from); // starting point

        float remaining = maxDistance;
        Vector3 current = from;

        for (int i = 0; i < raw.Length; i++)
        {
            Vector3 next = raw[i];
            float segLen = Vector3.Distance(current, next);

            if (segLen <= 0.0001f)
            {
                current = next;
                continue;
            }

            if (remaining <= 0f)
                break;

            if (segLen <= remaining)
            {
                // we can take this whole segment
                result.Add(next);
                remaining -= segLen;
                current = next;
            }
            else
            {
                // we can only go partway along this segment
                float t = remaining / segLen;
                Vector3 partial = Vector3.Lerp(current, next, t);
                result.Add(partial);
                remaining = 0f;
                current = partial;
                break;
            }
        }

        if (result.Count <= 1)
            return false; // nowhere to go

        clampedCorners = result.ToArray();
        return true;
    }

    // Samples a point on the Walkable NavMesh near "source".
    private bool SampleOnWalkable(Vector3 source, out Vector3 result)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(source, out hit, navMeshMaxDistance, walkableMask))
        {
            result = hit.position;
            return true;
        }

        result = source;
        return false;
    }

    // --- COROUTINE -----------------------------------------------------------

    private IEnumerator MoveAlongPathCoroutine(Vector3[] corners, Vector3 oldPosition, float speed, System.Action onFinish)
    {
        int index = 1; // 0 is the starting point (current position)

        while (index < corners.Length)
        {
            Vector3 targetPos = corners[index];

            Vector3 segDir = (targetPos - transform.position);
            if (segDir.sqrMagnitude > 0.0001f)
            {
                unit.View.SetFacingDirection(segDir);
            }

            int safety = 0;
            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                Vector3 before = transform.position;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

                if ((Time.frameCount % 10) == 0) // log every ~10 frames
                {
                    float dist = Vector3.Distance(transform.position, targetPos);
                }

                safety++;
                if (safety > 2000)
                {
                    break;
                }

                yield return null;
            }

            index++;
        }

        var from = oldPosition;
        var to = transform.position;

        ZoneManager.Instance?.HandleOnMove(unit, from, to);
        GetComponent<StatusComponent>()?.OnMoved();
        ReactionManager.Instance?.TryTriggerReactions(unit, from);

        unit.Model.SpendAction();
        unit.View.PlayMoveLandNet();

        Debug.Log($"[Move] Coroutine END for {unit.name} at t={Time.time}, finalPos={transform.position}");

        onFinish?.Invoke();
    }


    // UTILS
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

    private bool SampleOnNav(Vector3 source, bool anyArea, out Vector3 result)
    {
        NavMeshHit hit;
        int mask = anyArea ? NavMesh.AllAreas : walkableMask;

        // use a generous radius so small offsets in Y or X/Z don’t break it
        if (NavMesh.SamplePosition(source, out hit, navMeshMaxDistance, mask))
        {
            result = hit.position;
            return true;
        }

        result = source;
        return false;
    }

    private Vector3[] CleanPath(Vector3[] corners)
    {
        if (corners == null || corners.Length <= 1)
            return corners;

        List<Vector3> result = new List<Vector3>(corners.Length);

        // Use the unit's current Z so we don't get "into the screen" segments
        float planeZ = transform.position.z;

        // First corner is our current position (clamped to plane)
        Vector3 last = new Vector3(corners[0].x, corners[0].y, planeZ);
        result.Add(last);

        const float minSqrDist = 0.01f; // ~0.1 units

        for (int i = 1; i < corners.Length; i++)
        {
            Vector3 p = new Vector3(corners[i].x, corners[i].y, planeZ);

            // Skip tiny segments that don't actually move us
            if ((p - last).sqrMagnitude < minSqrDist)
                continue;

            result.Add(p);
            last = p;
        }

        return result.ToArray();
    }
}
