using UnityEngine;

public class Follow_Mouse_for_Placing : MonoBehaviour
{
    [SerializeField] bool draw_gizmos;
    public float radius;
    public LayerMask standable_layer;

    public bool follow_mouse = true;
    public GameObject selected_menu;

    public Monsters my_monster_type;
    Dungeon_Creator_Manager creator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        creator = Dungeon_Creator_Manager.Instance;
        creator.PlaceSelected.AddListener(place_selected);

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
        if (creator.actual_prefab != this.gameObject) return;
        Collider2D[] collitions = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D col in collitions)
        {
            if (col.gameObject == this.gameObject) continue;
            Map_Zone_Data map_zone_Data = col.GetComponent<Map_Zone_Data>();
            if (map_zone_Data == null) continue;
            if (!map_zone_Data.is_standaple) { return; }
        }

        selected_menu.SetActive(false);
        follow_mouse = false;
        creator.dc_state = DC_State.BUTTON;
        creator.actual_prefab = null;
        creator.used_total_unit_badge += 2 ^ ((int)my_monster_type - 1);
        creator.used_units_badge[(int)my_monster_type - 1]++;
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

