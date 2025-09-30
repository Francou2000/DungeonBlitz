using UnityEngine;

public class TileMapManager : MonoBehaviour
{
    public static TileMapManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    [SerializeField] Camera my_cam;
    [SerializeField] Vector2 actual_tile;

    public Vector2 Actual_tile => actual_tile;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mouse_pos = my_cam.ScreenToWorldPoint(Input.mousePosition);
        actual_tile = new Vector2(Mathf.FloorToInt(mouse_pos.x), Mathf.FloorToInt(mouse_pos.y));
    }
}
