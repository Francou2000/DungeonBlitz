using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneLoaderController : MonoBehaviour
{
    private bool sceneLoadInProgress = false;
    public static SceneLoaderController Instance;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(Instance);
        }
        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMessageQueueRunning)
        {
            Debug.Log($"[SceneLoaderController] Resuming Photon message queue on scene load: {scene.name}");
            PhotonNetwork.IsMessageQueueRunning = true;
        }

        sceneLoadInProgress = false;
    }

    //[SerializeField] Scenes scene_to_load2;
    //public UnityEvent<Scenes> scene_to_load1 = new UnityEvent<Scenes>();

    [SerializeField] Animator _animator;
    [SerializeField] float transition_time;

    public void LoadNextLevel(Scenes scene_to_load)
    {
        if (sceneLoadInProgress)
        {
            Debug.LogWarning($"[SceneLoaderController] Scene load already in progress. Ignoring request for {scene_to_load}.");
            return;
        }

        sceneLoadInProgress = true;
        StartCoroutine(LoadLevel(scene_to_load));
        // SceneManager.LoadScene((int)scene_to_load - 1);
    }

    IEnumerator LoadLevel(Scenes scene_to_load)
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log($"[SceneLoaderController] Pausing Photon message queue before scene load: {scene_to_load}");
            PhotonNetwork.IsMessageQueueRunning = false;
        }

        _animator.SetTrigger("Start");
        
        yield return new WaitForSeconds(transition_time);

        // Instance = null;
        PhotonNetwork.LoadLevel((int)scene_to_load);
        // SceneManager.LoadScene((int)scene_to_load - 1);
    }

}

public enum Scenes
{
    NONE                = 15,
    Loading_Connection  = 0,
    MainMenu            = 1,
    Lobby               = 2,
    SampleScene         = 3,
    UnitsSelection      = 4,
    Heroes_WinScreen    = 5,
    DM_WinScreen        = 6,
}
