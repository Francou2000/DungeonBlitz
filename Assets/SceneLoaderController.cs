using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneLoaderController : MonoBehaviour
{
    public static SceneLoaderController Instance;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(Instance);
        }
        Instance = this;
    }

    //[SerializeField] Scenes scene_to_load2;
    //public UnityEvent<Scenes> scene_to_load1 = new UnityEvent<Scenes>();

    [SerializeField] Animator _animator;
    [SerializeField] float transition_time;

    public void LoadNextLevel(Scenes scene_to_load)
    {
        StartCoroutine(LoadLevel(scene_to_load));
        // SceneManager.LoadScene((int)scene_to_load - 1);
    }

    IEnumerator LoadLevel(Scenes scene_to_load)
    {
        _animator.SetTrigger("Start");
        
        yield return new WaitForSeconds(transition_time);

        PhotonNetwork.LoadLevel((int)scene_to_load - 1);
        // SceneManager.LoadScene((int)scene_to_load - 1);
    }

}

public enum Scenes
{
    NONE,
    Loading_Connection,
    MainMenu,
    Lobby,
    SampleScene,
    UnitsSelection,
    Heroes_WinScreen,
    DM_WinScreen,
}
