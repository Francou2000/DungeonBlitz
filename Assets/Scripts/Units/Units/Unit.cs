using UnityEngine;

public enum UnitFaction
{
    Hero,
    Monster,
    Test
}

public class Unit : MonoBehaviour
{
    public UnitModel Model { get; private set; }
    public UnitView View { get; private set; }
    public UnitController Controller { get; private set; }

    public UnitFaction Faction { get; private set; }

    // Main entry point for unit behavior
    void Awake()
    {
        // Fetch and link components
        Model = GetComponent<UnitModel>();
        View = GetComponent<UnitView>();
        Controller = GetComponent<UnitController>();

        // Initialize the MVC pieces
        Model.Initialize();
        View.Initialize(this);
        Controller.Initialize(this);
    }

    public bool IsAlly(Unit other)
    {
        return this.Faction == other.Faction;
    }
}
