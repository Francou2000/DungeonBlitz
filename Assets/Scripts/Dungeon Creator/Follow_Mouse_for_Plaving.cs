using UnityEngine;

public class Follow_Mouse_for_Placing : MonoBehaviour
{
    [SerializeField] bool draw_gizmos;
    public float radius;
    public LayerMask standable_layer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Dungeon_Creator_Manager.Instance.PlaceSelected.AddListener(place_selected);


    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
        transform.position = mousePosition;
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

        gameObject.GetComponent<Follow_Mouse_for_Placing>().enabled = false;
        //CHANGE STATE
    }

    private void OnDrawGizmos()
    {
        if (!draw_gizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
        
    }

}

