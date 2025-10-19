using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChangeMenuButton : MonoBehaviour
{
    [SerializeField] GameObject menuToActive;
    [SerializeField] GameObject menuToDeactive;
    [SerializeField] TMP_InputField nickName = null;
    Button my_button;

    
    void Start()
    {
        my_button = GetComponent<Button>();
        // if (my_button == null) Debug.Log("ASDASDADS");
        my_button.onClick.AddListener(ChangeMenuUI);
        if (nickName != null) { nickName.onValueChanged.AddListener(activeButtons); }
    }
    void ChangeMenuUI()
    {
        // Debug.Log("BBBBBBBBBBBBBB");
        menuToActive.SetActive(true);
        menuToDeactive.SetActive(false);
    }

    void activeButtons(string value)
    {
        Debug.Log("sdfasfd");
        my_button.interactable = value != "";
    }
}
