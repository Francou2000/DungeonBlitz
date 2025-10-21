using UnityEngine;
using UnityEngine.UI;

public class SlectedMapController : MonoBehaviour
{
    public static SlectedMapController instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [SerializeField] Image[] buttons_border;
    [SerializeField] Maps actual_map;

    [SerializeField] Button next_button;
    [SerializeField] ActiveMap map_activation;
    [SerializeField] GameObject tile_highlighter;
    [SerializeField] GameObject grid;
    [SerializeField] GameObject background;

    private void Start()
    {
        next_button.onClick.AddListener(SetMapToUse);
    }
    public void UpdateMapData(Maps map_id)
    {
        for (int i = 0; i < buttons_border.Length; i++)
        {
            if (i == (int)map_id -1) { buttons_border[i].color = Color.green; }
            else { buttons_border[i].color = Color.red; }
        }
        actual_map = map_id;
        next_button.interactable = actual_map != Maps.NONE;
    }

    public void SetMapToUse()
    {
        UnitLoaderController controller = UnitLoaderController.Instance;
        grid.SetActive(true);
        tile_highlighter.SetActive(true);
        background.SetActive(false);
        if (controller.playable_Map.Actual_map == actual_map) return;
        // Debug.Log("AAAAAAAAAAAAAAAAAAAAA");
        controller.photonView.RPC("DM_SelectMap", Photon.Pun.RpcTarget.All,(int)actual_map);

        //Remove Units instance
        DC_Manager.instance.RemoveAllUnitsFromList();
        //Traps instance


        map_activation.ActivateMap(actual_map);

        //sound
        AudioManager.Instance.PlayButtonSound();
    }
    public void HideMap()
    {
        tile_highlighter.SetActive(false);
        grid.SetActive(false);
        background.SetActive(true);

    }
}
