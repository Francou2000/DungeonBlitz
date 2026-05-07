using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    bool loadGameInitiated;

    void Start()
    {
        time = 0;
        if (PhotonNetwork.IsMasterClient)
        {
            DM_dungeon_creator.SetActive(true);
            if (UnitLoaderController.Instance.lvl > 1) heroes_shop_controller.SetActive(true);
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
                heroes_shop_controller.SetActive(true);
            }
            
        }
        // #region agent log
        bool s1 = slider1 != null && slider1.gameObject.activeInHierarchy;
        bool s2 = slider2 != null && slider2.gameObject.activeInHierarchy;
        DebugSessionNdjson.Write("H4", "TimerAndLoadGame.Start", "shop_timer_ui_after_ui",
            $"{{\"lvl\":{UnitLoaderController.Instance.lvl},\"isMaster\":{(PhotonNetwork.IsMasterClient ? "true" : "false")},\"slider1Active\":{(s1 ? "true" : "false")},\"slider2Active\":{(s2 ? "true" : "false")},\"shopActive\":{(heroes_shop != null && heroes_shop.activeSelf ? "true" : "false")},\"shopCtrlActive\":{(heroes_shop_controller != null && heroes_shop_controller.activeSelf ? "true" : "false")},\"scene\":\"{SceneManager.GetActiveScene().name.Replace("\"", "'")}\"}}");
        // #endregion
    }


    void Update()
    {
        if (PhotonNetwork.MasterClient != PhotonNetwork.LocalPlayer) return;
        if (loadGameInitiated) return;
        time += Time.deltaTime;
        float sliderValue = time / preparation_time_limit;
        slider1.value = sliderValue;
        slider2.value = sliderValue;
        if (time > preparation_time_limit)
            LoadGame();
    }

    /// <summary>
    /// Solo el master debe llamar a <see cref="SceneLoaderController.LoadNextLevel"/> cuando
    /// <see cref="PhotonNetwork.AutomaticallySyncScene"/> está activo; el resto de clientes
    /// cargan la escena vía sincronización de sala.
    /// </summary>
    public void LoadGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (loadGameInitiated) return;
        loadGameInitiated = true;
        SceneLoaderController.Instance.LoadNextLevel(game_scene_name);
    }
}
