using Photon.Pun;
using UnityEngine;

public sealed class SummonedUnit : MonoBehaviourPun
{
    public double ExpiresAt;
    public int OwnerCasterViewId;

    public void Init(double expiresAt, int ownerCasterViewId)
    {
        ExpiresAt = expiresAt;
        OwnerCasterViewId = ownerCasterViewId;
    }

    void Update()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.Time >= ExpiresAt)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
