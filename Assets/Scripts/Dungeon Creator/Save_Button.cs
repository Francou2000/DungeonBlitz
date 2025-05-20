using UnityEngine;
using UnityEngine.UI;

public class Save_Button : MonoBehaviour
{

    Button my_button;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        my_button = GetComponent<Button>();
            my_button.onClick.AddListener(save_map);
    }

    void save_map()
    {
        Dungeon_Creator_Manager.Instance.save_map();
    }
}
