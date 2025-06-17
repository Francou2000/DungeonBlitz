using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReadyButton : MonoBehaviour
{
    Button my_button;
    TextMeshProUGUI my_text;
    bool is_ready = false;


    void Start()
    {
        my_button = GetComponent<Button>();
        my_text = GetComponentInChildren<TextMeshProUGUI>();

        my_button.onClick.AddListener(SwapReadiness);
    }

    void SwapReadiness()
    {
        if (is_ready) {
            is_ready = false;
        }
        else
        {
            is_ready = true;
        }
    }
}
