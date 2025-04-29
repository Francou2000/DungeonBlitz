using UnityEngine;

public class Unit : MonoBehaviour
{
    public UnitModel Model { get; private set; }
    public UnitView View { get; private set; }
    public UnitController Controller { get; private set; }

    void Awake()
    {
        Model = GetComponent<UnitModel>();
        View = GetComponent<UnitView>();
        Controller = GetComponent<UnitController>();

        
        Model.Initialize();
        View.Initialize(this);
        Controller.Initialize(this);
    }
}
