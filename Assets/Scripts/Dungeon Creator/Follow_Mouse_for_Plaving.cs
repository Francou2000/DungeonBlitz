using UnityEngine;

public class Follow_Mouse_for_Placing : MonoBehaviour
{
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
        gameObject.GetComponent<Follow_Mouse_for_Placing>().enabled = false;
        //transform.parent = null;
    }
}
