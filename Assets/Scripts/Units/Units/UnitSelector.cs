using Photon.Pun;
using UnityEngine;

public class UnitSelector : MonoBehaviourPun
{
    private UnitController currentActiveUnit;

    private void Start()
    {
        StartCoroutine(DelayedAssign());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Check if clicked on a UI element
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            Vector3 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 clickPos2D = new Vector2(clickPos.x, clickPos.y);

            RaycastHit2D hit = Physics2D.Raycast(clickPos2D, Vector2.zero);
            if (hit.collider != null)
            {
                Unit clickedUnit = hit.collider.GetComponent<Unit>();
                if (clickedUnit != null && clickedUnit.Controller != null && clickedUnit.Controller.isControllable && clickedUnit.Model.Faction == UnitFaction.Monster && PhotonNetwork.IsMasterClient)
                {
                    SetActiveUnit(clickedUnit.Controller);
                }
            }
        }
    }

    private System.Collections.IEnumerator DelayedAssign()
    {
        yield return new WaitForSeconds(0.5f); // wait for units to be instantiated

        AssignLocalHero();
    }

    void SetActiveUnit(UnitController newUnit)
    {
        if (currentActiveUnit != null)
        {
            currentActiveUnit.unit.View.SetHighlighted(false);
        }

        currentActiveUnit = newUnit;
        UnitController.ActiveUnit = newUnit;

        currentActiveUnit.unit.View.SetHighlighted(true);

        Debug.Log($"[Selector] Active unit is now: {currentActiveUnit.unit.Model.UnitName}");
    }

    private void AssignLocalHero()
    {
        int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        UnitController[] allUnits = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);

        foreach (var unit in allUnits)
        {
            if (!unit.isControllable) continue;
            if (!unit.photonView.IsMine) continue;

            UnitController.ActiveUnit = unit;

            Debug.Log($"[HeroSelector] Assigned hero unit to player {myActorNumber}: {unit.unit.Model.UnitName}");
        }
    }
}