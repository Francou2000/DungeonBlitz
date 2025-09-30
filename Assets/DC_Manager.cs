using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;

public class DC_Manager : MonoBehaviour
{
    public static DC_Manager instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    [SerializeField] UnitData[] unitDatas;


    List<Vector3> unitList = new List<Vector3>();
    List<DC_Trap> traps = new List<DC_Trap>();

    [SerializeField] GameObject unit_placeholder;
    [SerializeField] GameObject edit_menu;

    [SerializeField] int population_limit;
    List<Vector2> used_pos = new List<Vector2>();

    void Start()
    {
        
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 pos = TileMapManager.Instance.Actual_tile;
            switch (state)
            {
                case DC_State.PLACING_UNIT:
                    if (UpdateUnitOnList(pos, pos, actualUnit)) break;
                    GameObject new_unit = Instantiate(unit_placeholder, spawn.transform);
                    UnitPlaceholderIntection upi = new_unit.GetComponent<UnitPlaceholderIntection>();
                    upi.SetPlaceHolder(pos + new Vector2(0.5f, 0.5f), unitDatas[(int)actualUnit - 1].full_body_foto, actualUnit);
                    upi.EditMenu = edit_menu;
                    HideUnit();
                    break;
                case DC_State.PLAICING_TRAP:
                    break;
                case DC_State.MOVING_UNIT:
                    UpdateUnitOnList(Unit_Original_Pos, pos, actualUnit);
                    break;
                case DC_State.MOVING_TRAP:
                    break;
                default:
                    edit_menu.SetActive(false);
                    moseClick.Invoke(pos + new Vector2(0.5f, 0.5f));
                    break;
            }
        }
    }

    DC_State state;
    Vector2 unit_original_pos;
    [SerializeField] GameObject spawn;
    public UnityEvent resetUnits = new UnityEvent();
    UnitPlaceholderIntection unit_to_update;
    public UnityEvent<Vector2> moseClick = new UnityEvent<Vector2>();

    public DC_State State { get { return state; } set { state = value; } }
    public Vector2 Unit_Original_Pos { get { return unit_original_pos; } set { unit_original_pos = value; } }
    public UnitPlaceholderIntection Unit_to_update { set {  unit_to_update = value; } }




    public bool UpdateUnitOnList(Vector2 origin_pos, Vector2 new_pos, Monsters unit_id)
    {
        if (AddPosToUsed(new_pos)) return true;
        RemoveUnitFromList(origin_pos);
        AddUnitToList(new_pos, unit_id);
        AddUnitToController();
        return false;
    }

    public void AddUnitToList(Vector2 pos, Monsters unit_id)
    {
        Vector3 new_unit = new Vector3(pos.x, pos.y, (int)unit_id);
        unitList.Add(new_unit);
    }

    void AddUnitToController()
    {
        UnitLoaderController.Instance.photonView.RPC("DM_AddUnitsToMap", Photon.Pun.RpcTarget.All, unitList);
    }
    bool AddPosToUsed(Vector2 pos)
    {
        foreach (var unit in used_pos)
        {
            if (unit == pos) return true;
        }
        used_pos.Add(pos);
        return false;
    }

    public void RemoveUnitFromList(Vector2 pos)
    {
        int idx = 0;
        while (idx < unitList.Count) 
        {
            if (new Vector2(unitList[idx].x, unitList[idx].x) == pos)
            {
                unitList.RemoveAt(idx);
                return;
            }
            idx++;
        }
    }

    public void RemoveAllUnitsFromList()
    {
        unitList.Clear();
        resetUnits.Invoke();
    }

    public void RemoveUnitVisual(bool should_move)
    {
        RemoveUnitFromList(unit_to_update.Tile_pos);
        unit_to_update.Remove();
        if (!should_move) return;
        ShowUnit(actualUnit);
    }



    [SerializeField] SpriteRenderer unitPreShow;
    public Monsters actualUnit;
    public void ShowUnit(Monsters unit_id)
    {
        //Simplemente muestra el sprite de la unidad seleccionada
        Sprite unit_sprite = unitDatas[(int)unit_id - 1].full_body_foto;

        unitPreShow.sprite = unit_sprite;
        actualUnit = unit_id;
        state = DC_State.PLACING_UNIT;
    }

    public void HideUnit()
    {
        unitPreShow.sprite = null;
        actualUnit = Monsters.NONE;
        state = DC_State.NONE;
    }



}
