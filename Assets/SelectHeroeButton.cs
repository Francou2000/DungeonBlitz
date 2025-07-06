using UnityEngine;
using UnityEngine.UI;

public class SelectHeroeButton : MonoBehaviour
{
    public HeroesList my_heroe;

    Button my_button;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        my_button = GetComponent<Button>();
        my_button.onClick.AddListener(ChangeHeroe);
    }

    public void ChangeHeroe()
    {
        HeroeSelectionController.instance.UpdateHeroeData(my_heroe);
    }
}
