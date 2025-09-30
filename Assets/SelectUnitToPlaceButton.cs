using UnityEngine;
using UnityEngine.UI;

public class SelectUnitToPlaceButton : MonoBehaviour
{
    Button my_button;
    [SerializeField] Monsters my_unit;
    void Start()
    {
        my_button = GetComponent<Button>();
        my_button.onClick.AddListener(SetUnit);
    }

    public void SetUnit()
    {
        DC_Manager.instance.ShowUnit(my_unit);
    }
}
