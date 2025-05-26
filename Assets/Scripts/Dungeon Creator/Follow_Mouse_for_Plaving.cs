using UnityEngine;

public class Follow_Mouse_for_Placing : MonoBehaviour
{
    [SerializeField] bool draw_gizmos;
    public float radius;
    public LayerMask standable_layer;

    public bool follow_mouse = true;
    public GameObject selected_menu;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Dungeon_Creator_Manager.Instance.PlaceSelected.AddListener(place_selected);

    }

    // Update is called once per frame
    void Update()
    {
        if (follow_mouse)
        {
            Vector2 mousePosition = Input.mousePosition;
            mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
            transform.position = mousePosition;
        }
        
    }

    void place_selected()
    {
        Collider2D[] collitions = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D col in collitions)
        {
            if (col.gameObject == this.gameObject) continue;
            Map_Zone_Data map_zone_Data = col.GetComponent<Map_Zone_Data>();
            if (map_zone_Data == null) continue;
            if (!map_zone_Data.is_standaple) return;
        }

        selected_menu.SetActive(false);
        follow_mouse = false;
        Dungeon_Creator_Manager.Instance.dc_state = DC_State.BUTTON;
    }

    private void OnMouseDown()
    {
        if (follow_mouse) return;

        selected_menu.SetActive(true);
        var buttons = selected_menu.GetComponentsInChildren<Modify_Selection_Button>();
        foreach (var button in buttons)
        {
            button.actual_selection = this.gameObject;
        }
        Debug.Log("¡Objeto seleccionado: " + gameObject.name + "!");
        // Aquí puedes poner lógica personalizada
    }

    //-------------------------------
    //----------GIZMOS---------------
    //-------------------------------

    private void OnDrawGizmos()
    {
        if (!draw_gizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
        
    }

}

