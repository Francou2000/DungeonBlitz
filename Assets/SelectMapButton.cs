using UnityEngine;
using UnityEngine.UI;

public class SelectMapButton : MonoBehaviour
{
    public Maps my_map;

    Button my_button;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        my_button = GetComponent<Button>();
        my_button.onClick.AddListener(ChangeHeroe);
    }

    public void ChangeHeroe()
    {
        SlectedMapController.instance.UpdateMapData(my_map);
    }
}
