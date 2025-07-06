using UnityEngine;
using UnityEngine.UI;

public class ReadyHeroeSelection : MonoBehaviour
{
    Button my_button;
    public UnitData actual_unit;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        my_button = GetComponent<Button>();
        my_button.onClick.AddListener(SelectionReady);
    }

    public void SelectionReady()
    {
        UnitLoaderController.Instance.AddHeroe(actual_unit, 0);//usar numero de cliente o algo así para el segundo valor
        //UI Feedback
    }
}
