using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitPlaceholderIntection : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    Vector2 tile_position;
    Monsters my_unit;
    [SerializeField] GameObject editMenu;
    [SerializeField] GameObject promoteMenu;
    [SerializeField] GameObject promoteText;
    bool is_promotionable;
    int promotion_cost;
    DC_Manager manager;
    public Vector2 Tile_pos => tile_position - new Vector2(0.5f, 0.5f);
    public GameObject EditMenu  { get { return editMenu; } set { editMenu = value; }  }
    public GameObject PromoteMenu { get { return promoteMenu; } set { promoteMenu = value; }  }
    public GameObject PromoteText { get { return promoteText; } set { promoteText = value; }  }
    public bool Is_promotionable { get { return is_promotionable; } set { is_promotionable = value; } }
    public int Promotion_cost { get { return promotion_cost; } set { promotion_cost = value; } }

    public bool is_selected = false;

    private void Start()
    {
        manager = DC_Manager.instance;
        manager.resetUnits.AddListener(Remove);
        //manager.moseClick.AddListener(OnMouseClick);
    }


    public void OnMouseClick(Vector2 pos)
    {
        is_selected = false;
        if (tile_position != pos) return;
        manager.Unit_to_update = this;
        manager.actualUnit = my_unit;
        is_selected = true;
        OpenEditMenu(pos);
    }

    void OpenEditMenu(Vector2 pos)
    {
        editMenu.SetActive(true);
        Vector2 screen_pos = Camera.main.WorldToScreenPoint(pos + new Vector2(0, -0.5f));
        editMenu.GetComponent<RectTransform>().position = screen_pos;

        if (!is_promotionable) return;
        if (promotion_cost > UnitLoaderController.Instance.dm_remaining_time) return;

        promoteMenu.SetActive(true);
        screen_pos = Camera.main.WorldToScreenPoint(pos + new Vector2(0, 0.5f));
        promoteMenu.GetComponent<RectTransform>().position = screen_pos;
        promoteText.GetComponent<TextMeshProUGUI>().text = "Use " + promotion_cost.ToString() + "s to promote";
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
