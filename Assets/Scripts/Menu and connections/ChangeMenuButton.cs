using UnityEngine;
using UnityEngine.UI;

public class ChangeMenuButton : MonoBehaviour
{
    [SerializeField] GameObject menuToActive;
    [SerializeField] GameObject menuToDeactive;
    Button my_button;

    
    void Start()
    {
        my_button = GetComponent<Button>();
        my_button.onClick.AddListener(ChangeMenuUI);
    }
    void ChangeMenuUI()
    {
        menuToActive.SetActive(true);
        menuToDeactive.SetActive(false);
    }
}
