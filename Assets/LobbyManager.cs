using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager Instance;

    [SerializeField] string dm_scene;
    [SerializeField] string heroe_scene;
    [SerializeField] Button ready_button;

    public string[] slots_used = new string[5];

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slots_used[0] = "DM name";
        slots_used[1] = "P1 name";
        slots_used[2] = "P2 name";
        slots_used[3] = "P3 name";
        slots_used[4] = "P4 name";

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ChangeSceneDM()
    {
        SceneManager.LoadScene(dm_scene);
    }

    public void ChangeSceneHEROE()
    {
        SceneManager.LoadScene(heroe_scene);
    }

}
