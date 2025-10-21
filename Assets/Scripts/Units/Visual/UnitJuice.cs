using System.Collections;
using UnityEngine;

public class UnitJuice : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] Transform gfxRoot;

    [Header("Squash/Stretch")]
    [SerializeField] float moveSquashAmount = 0.12f;   // 12%
    [SerializeField] float moveSquashTime = 0.08f;
    [SerializeField] float landReboundAmt = 0.10f;
    [SerializeField] float landReboundTime = 0.10f;

    [Header("Attack")]
    [SerializeField] float punchAmt = 0.15f;
    [SerializeField] float punchTime = 0.10f;

    [Header("Hit/Miss")]
    [SerializeField] float hitNudgeDist = 0.12f;
    [SerializeField] float hitNudgeTime = 0.06f;

    Vector3 baseScale;
    Vector3 baseLocalPos;
    Coroutine playing;

    void Awake()
    {
        if (!gfxRoot) gfxRoot = transform;
        baseScale = gfxRoot.localScale;
        baseLocalPos = gfxRoot.localPosition;
    }

    public void MoveStartSquash(Vector2 moveDir)
    {
        Play(CO_SquashStretch(moveDir, moveSquashAmount, moveSquashTime));
    }

    public void MoveLandRebound()
    {
        Play(CO_Rebound(landReboundAmt, landReboundTime));
    }

    public void AttackPunch(Vector2 facing)
    {
        Play(CO_Punch(facing.normalized, punchAmt, punchTime));
    }

    public void HitNudge(Vector2 fromAttacker)
    {
        // push away from attacker
        Play(CO_Nudge((-fromAttacker).normalized, hitNudgeDist, hitNudgeTime));
    }

    public void MissDodge(Vector2 fromAttacker)
    {
        // sidestep (90°) relative to attacker direction
        Vector2 right = new Vector2(-fromAttacker.y, fromAttacker.x).normalized;
        Play(CO_Nudge(right, hitNudgeDist, hitNudgeTime));
    }

    void Play(IEnumerator routine)
    {
        if (playing != null) StopCoroutine(playing);
        playing = StartCoroutine(Wrap(routine));
    }

    IEnumerator Wrap(IEnumerator routine)
    {
        yield return StartCoroutine(routine);
        // restore
        gfxRoot.localScale = baseScale;
        gfxRoot.localPosition = baseLocalPos;
        playing = null;
    }

    IEnumerator CO_SquashStretch(Vector2 dir, float amt, float time)
    {
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / time;
            float k = Mathf.Sin(t * Mathf.PI * 0.5f);   // ease-out
            float sx = 1f + amt * k;                    // stretch forward
            float sy = 1f - amt * 0.75f * k;            // squash down
            gfxRoot.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
            yield return null;
        }
    }

    IEnumerator CO_Rebound(float amt, float time)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / time;
            float k = Mathf.Sin(t * Mathf.PI);          // up and back
            float sx = 1f - amt * 0.5f * k;
            float sy = 1f + amt * k;
            gfxRoot.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
            yield return null;
        }
    }

    IEnumerator CO_Punch(Vector2 dir, float amt, float time)
    {
        Vector3 startPos = baseLocalPos;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / time;
            float k = Mathf.Sin(t * Mathf.PI); // out/back
            gfxRoot.localPosition = startPos + (Vector3)(dir * amt * 0.2f * k);
            float sx = 1f + amt * 0.7f * k;
            float sy = 1f - amt * 0.5f * k;
            gfxRoot.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
            yield return null;
        }
    }

    IEnumerator CO_Nudge(Vector2 dir, float dist, float time)
    {
        Vector3 start = baseLocalPos;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / time;
            float k = Mathf.Sin(t * Mathf.PI); // out/back
            gfxRoot.localPosition = start + (Vector3)(dir * dist * k);
            yield return null;
        }
    }
}