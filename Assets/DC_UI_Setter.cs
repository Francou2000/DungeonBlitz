using UnityEngine;

public class DC_UI_Setter : MonoBehaviour
{
    int lvl;
    UnitLoaderController unitController;


    [Header("Lvl 1")]
    public GameObject background;
    public GameObject MapSelector;

    [Header("Lvl 2 - 3")]
    public GameObject TileP;
    public GameObject Shop;
    public ActiveMap activeMap;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        unitController = UnitLoaderController.Instance;
        lvl = unitController.lvl;

        if ( lvl == 1)
        {
            background.SetActive(true);
            MapSelector.SetActive(true);
        }
        else
        {
            TileP.SetActive(true);
            Shop.SetActive(true);
            activeMap.ActivateMap(unitController.playable_Map.Actual_map);
        }
    }

}
