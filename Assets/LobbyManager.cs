using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] string dm_scene;
    [SerializeField] string heroe_scene;
    [SerializeField] Button ready_button;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
