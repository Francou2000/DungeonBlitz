using Photon.Pun;
using UnityEngine;

public sealed class SummonedUnit : MonoBehaviourPun
{
    public UnitFaction OwnerFaction = UnitFaction.Monster;
    public int OwnerCasterViewId;

    public void InitPermanent(int ownerCasterViewId, UnitFaction ownerFaction)
    {
        OwnerCasterViewId = ownerCasterViewId;
        OwnerFaction = ownerFaction;
    }

    void OnEnable() { TurnManager.OnTurnBegan += HandleTurnBegan; }
    void OnDisable() { TurnManager.OnTurnBegan -= HandleTurnBegan; }

    void HandleTurnBegan(UnitFaction starting)
    {
        // Kept only if you later need owner-turn hooks.
    }
}
