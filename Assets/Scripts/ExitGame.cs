using System;
using UnityEngine;
using UnityEngine.UI;

public class ExitGame : MonoBehaviour
{
    Button my_button;


    void Start()
    {
        my_button = GetComponent<Button>();
        // if (my_button == null) Debug.Log("ASDASDADS");
        my_button.onClick.AddListener(Exit);
    }
    void Exit()
    {
        Application.Quit();
    }
}
