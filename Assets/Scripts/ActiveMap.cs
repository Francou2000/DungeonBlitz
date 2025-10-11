using UnityEngine;

public class ActiveMap : MonoBehaviour
{
    [SerializeField] GameObject[] map_list;

    public void ActivateMap(Maps map)
    {
        for (int i = 0; i < map_list.Length; i++)
        {
            map_list[i].SetActive(i == (int)map - 1);
        }
    }
}
