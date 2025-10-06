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

    public void SpawnSummons(Unit caster, UnitAbility ab, Vector3 center)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (caster == null || ab == null || !ab.spawnsSummons) return;

        var casterPV = caster.GetComponent<PhotonView>();
        if (!casterPV) return;

        double expires = PhotonNetwork.Time + Mathf.Max(1f, ab.summonDuration);

        int n = Mathf.Max(1, ab.summonCount);
        float r = 1.25f;

        for (int i = 0; i < n; i++)
        {
            float ang = (360f / n) * i * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * r;

            // Broadcast creation; each client instantiates their own networked object
            _view.RPC(nameof(RPC_CreateSummon), RpcTarget.All,
                ab.summonPrefabName,
                pos,
                expires,
                casterPV.ViewID
            );
        }
    }

    [PunRPC]
    void RPC_CreateSummon(string prefabName, Vector3 pos, double expiresAt, int ownerCasterVid)
    {
        GameObject go = PhotonNetwork.Instantiate(prefabName, pos, Quaternion.identity, 0);

        // Attach lifetime marker
        var su = go.AddComponent<SummonedUnit>();
        su.Init(expiresAt, ownerCasterVid);

        // No stat/faction overrides here: prefab already points to the reduced UnitData
    }
}
