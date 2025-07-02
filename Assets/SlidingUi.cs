using UnityEngine;
using UnityEngine.UI;

public class SlidingUi : MonoBehaviour
{
    [SerializeField] Vector2 closePosition;
    [SerializeField] Vector2 openPosition;
    bool is_open;
    Button my_button;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        is_open = false;
        my_button = GetComponent<Button>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OpenCloseUI()
    {

    }
}
