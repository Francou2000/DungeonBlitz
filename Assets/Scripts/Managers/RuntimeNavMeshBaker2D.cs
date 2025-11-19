using UnityEngine;
using NavMeshPlus.Components;

public class RuntimeNavMeshBaker2D : MonoBehaviour
{
    [SerializeField] private NavMeshSurface surface;

    private void OnEnable()
    {
        TurnManager.OnTurnBegan += HandleTurnBegan;
    }

    private void OnDisable()
    {
        TurnManager.OnTurnBegan -= HandleTurnBegan;
    }

    private void HandleTurnBegan(UnitFaction faction)
    {
        if (surface == null)
        {
            Debug.LogError("[NavMesh] Surface not assigned on RuntimeNavMeshBaker2D.");
            return;
        }

        // Rebuild navmesh at the start of EVERY turn on THIS client
        surface.RemoveData();
        surface.BuildNavMesh();
    }
}