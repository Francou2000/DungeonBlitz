using Photon.Pun;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class TimerAndLoadGame : MonoBehaviourPunCallbacks
{
    public static TimerAndLoadGame instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(instance);
            instance = this;
        }
    }

    public GameObject DM_dungeon_creator;
    public GameObject HEROE_selection;
    public GameObject waiting_canvas;
    public GameObject heroes_shop;
    public GameObject heroes_shop_controller;

    public Scenes game_scene_name;

    public float preparation_time_limit;
    public float time;

    [SerializeField] Slider slider1;
    [SerializeField] Slider slider2;

    void Start()
    {
        time = 0;
        if (PhotonNetwork.IsMasterClient)
        {
            DM_dungeon_creator.SetActive(true);
            // if (UnitLoaderController.Instance.lvl > 1) heroes_shop_controller.SetActive(true);
        }
        else
        {
            if (UnitLoaderController.Instance.lvl == 1)
            {
                HEROE_selection.SetActive(true);
            }
            else
            {
                //TODO: Tienda
                //waiting_canvas.SetActive(true);
                heroes_shop.SetActive(true);
                // heroes_shop_controller.SetActive(true);
            }
            
        }
    }


    void Update()
    {
        if (PhotonNetwork.MasterClient != PhotonNetwork.LocalPlayer) return;
        time += Time.deltaTime;
        float sliderValue = time / preparation_time_limit;
        slider1.value = sliderValue;
        slider2.value = sliderValue;
        if (time > preparation_time_limit)
        {
            // LoadGame();
            photonView.RPC("LoadGame", RpcTarget.All);
        }
    }

    [PunRPC]
    public void LoadGame()
    {
        // PhotonNetwork.LoadLevel(game_scene_name);
        SceneLoaderController.Instance.LoadNextLevel(game_scene_name);
    }
}
