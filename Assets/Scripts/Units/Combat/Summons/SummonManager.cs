using Photon.Pun;
using UnityEngine;

public sealed class SummonManager : MonoBehaviourPun
{
    public static SummonManager Instance { get; private set; }
    PhotonView _view;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _view = GetComponent<PhotonView>() ?? gameObject.AddComponent<PhotonView>();
    }

    public void SpawnAriseSummons(Unit caster, UnitAbility ab)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (caster == null || ab == null || !ab.spawnsSummons) return;

        // Resolve prefab name: prefer the dragged prefab, else the text field
        string prefabName = null;
        if (ab.summonPrefab != null) prefabName = ab.summonPrefab.name;
        else if (!string.IsNullOrEmpty(ab.summonPrefabName)) prefabName = ab.summonPrefabName;

        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogError("[SummonManager] No summon prefab set on ability.");
            return;
        }

        int n = Mathf.Max(2, ab.summonCount); // Arise should be 2
        Vector3 center = caster.transform.position;

        for (int i = 0; i < n; i++)
        {
            Vector3 pos = FindFreePositionNear(caster.transform, i, 1.2f, 12);
            PhotonNetwork.Instantiate(prefabName, pos, Quaternion.identity, 0);
        }
    }

    // Finds a nearby spot not directly on top of the caster or another collider
    Vector3 FindFreePositionNear(Transform casterT, int index, float radius, int attempts)
    {
        Vector3 center = casterT.position;

        // first candidate on a ring
        float baseAngle = (360f / Mathf.Max(1, attempts)) * index * Mathf.Deg2Rad;
        Vector3 baseDir = new Vector3(Mathf.Cos(baseAngle), Mathf.Sin(baseAngle), 0f);
        Vector3 candidate = center + baseDir * radius;

        for (int a = 0; a < attempts; a++)
        {
            float ang = (a == 0) ? 0f : (a * 360f / attempts);
            Vector3 dir = Quaternion.Euler(0, 0, ang) * (candidate - center);
            Vector3 p = center + dir;

            // 2D overlap check
            var hits = Physics2D.OverlapCircleAll(p, 0.35f);
            bool blocked = false;
            foreach (var h in hits)
            {
                if (!h) continue;

                // ignore caster's own colliders (same transform or children)
                if (h.transform == casterT || h.transform.IsChildOf(casterT)) continue;

                blocked = true;
                break;
            }

            if (!blocked) return new Vector3(p.x, p.y, 0f);
        }

        // Fallback: slight offset from center so it’s not exactly on top
        return center + new Vector3(0.6f, 0f, 0f);
    }
}
