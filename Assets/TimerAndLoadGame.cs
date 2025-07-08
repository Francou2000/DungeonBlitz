using Photon.Pun;
using UnityEngine;

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
            Destroy(this.gameObject);
        }
    }

    public GameObject DM_dungeon_creator;
    public GameObject HEROE_selection;


    public string game_scene_name;

    public float preparation_time_limit;
    public float time;

    void Start()
    {
        time = 0;
        if (PhotonNetwork.IsMasterClient)
        {
            DM_dungeon_creator.SetActive(true);
        }
        else
        {
            HEROE_selection.SetActive(true);
        }
    }


    void Update()
    {
        if (PhotonNetwork.MasterClient != PhotonNetwork.LocalPlayer) return;
        time += Time.deltaTime;
        if (time > preparation_time_limit)
        {
            LoadGame();
        }
    }


    public void LoadGame()
    {
        PhotonNetwork.LoadLevel(game_scene_name);
    }
}
