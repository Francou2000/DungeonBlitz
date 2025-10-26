using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitPlaceholderIntection : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    Vector2 tile_position;
    Monsters my_unit;
    [SerializeField] GameObject editMenu;
    DC_Manager manager = DC_Manager.instance;
    public Vector2 Tile_pos => tile_position - new Vector2(0.5f, 0.5f);
    public GameObject EditMenu  { get { return editMenu; } set { editMenu = value; }  }

    public bool is_selected = false;

    private void Start()
    {
        manager.resetUnits.AddListener(Remove);
        manager.moseClick.AddListener(OnMouseClick);
    }


    public void OnMouseClick(Vector2 pos)
    {
        is_selected = false;
        if (tile_position != pos) return;
        manager.Unit_to_update = this;
        manager.actualUnit = my_unit;
        is_selected = true;
        editMenu.SetActive(true);
        Vector2 screen_pos = Camera.main.WorldToScreenPoint(pos + new Vector2(0, -0.5f));
        editMenu.GetComponent<RectTransform>().position = screen_pos;
    }

    public void SetPlaceHolder(Vector2 pos, Sprite sp, Monsters actualUnit)
    {
        transform.position = pos;
        tile_position = pos;
        my_unit = actualUnit;
        ChangeSprite(sp);
    }


    void ChangeSprite(Sprite sp)
    {
        spriteRenderer.sprite = sp;
    }

    public void Remove()
    {
        Destroy(this.gameObject);
    }
}
