using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SlidingUi : MonoBehaviour
{
    [SerializeField] Transform closePosition;
    [SerializeField] Transform openPosition;
    [SerializeField] float moveSpeed;
    Vector3 target;

    bool is_mooving;
    bool is_open;
    Button my_button;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        is_mooving = false;
        is_open = false;
        my_button = GetComponent<Button>();
        my_button.onClick.AddListener(OpenCloseUI);
        target = closePosition.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (!is_mooving) return;
        Vector2 dir = is_open ? new Vector2(1, 0) : new Vector2(-1, 0);
        transform.Translate(dir * moveSpeed);
        if (transform.position.x < closePosition.position.x || transform.position.x > openPosition.position.x)
        {
            transform.position = is_open ? openPosition.position : closePosition.position;
            is_mooving = false;
        }
    }

    public void OpenCloseUI()
    {
        is_mooving = true;
        is_open = !is_open;
        if (target == closePosition.position) { target = openPosition.position; }
        if (target == openPosition.position) { target = closePosition.position; }
    }
}
