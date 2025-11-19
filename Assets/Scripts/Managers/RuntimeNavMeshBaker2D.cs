using UnityEngine;
using NavMeshPlus.Components;

public class RuntimeNavMeshBaker2D : MonoBehaviour
{
    [SerializeField] private NavMeshSurface surface;

    private bool _built = false;

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
        if (_built) return;                 // only once, on first turn

        if (surface == null)
        {
            Debug.LogError("[NavMesh] Surface not assigned on RuntimeNavMeshBaker2D.");
            _built = true;
            return;
        }

        // clear old data, useful if you ever reuse the same object for another round.
        surface.RemoveData();

        surface.BuildNavMesh();
        _built = true;

        Debug.Log("[NavMesh] Built at first turn for current map.");
    }
}