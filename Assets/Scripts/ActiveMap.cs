using UnityEngine;

public class ActiveMap : MonoBehaviour
{
    [SerializeField] GameObject[] map_list;
    public bool is_game;

    private void Start()
    {
        if (is_game)
        {
            var unit_l = UnitLoaderController.Instance;
            if (unit_l != null)
            {
                ActivateMap(unit_l.playable_Map.Actual_map);
            }
            else
            {
                ActivateMap(Maps.MAP_NAME1);
            }
        }
    }

    public void ActivateMap(Maps map)
    {
        for (int i = 0; i < map_list.Length; i++)
        {
            map_list[i].SetActive(i == (int)map - 1);
        }
    }
}
