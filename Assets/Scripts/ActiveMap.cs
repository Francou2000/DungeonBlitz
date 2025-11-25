using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ActiveMap : MonoBehaviour
{
    [SerializeField] GameObject[] map_list;
    public bool is_game;

    [SerializeField] Vector2[] non_standable_map1;
    [SerializeField] Vector2[] non_standable_map2;
    [SerializeField] Vector2[] non_standable_map3;
    [SerializeField] Vector2[] non_standable_map4; 
    List<Vector2[]> non_standable = new List<Vector2[]>();

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

        non_standable.Add(non_standable_map1);
        non_standable.Add(non_standable_map2);
        non_standable.Add(non_standable_map3);
        non_standable.Add(non_standable_map4);
    }

    public void ActivateMap(Maps map)
    {
        for (int i = 0; i < map_list.Length; i++)
        {
            if (i == (int)map - 1)
            {
                map_list[i].SetActive(true);
                DC_Manager.instance.Non_standable_tile = non_standable[i];
            }
            else
            {
                map_list[i].SetActive(false);
            }
        }
    }
}
