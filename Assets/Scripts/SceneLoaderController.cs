using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneLoaderController : MonoBehaviour
{
    public static SceneLoaderController Instance;
    private bool _isLoadingScene;
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
        if (_isLoadingScene) return;
        StartCoroutine(LoadLevel(scene_to_load));
        // SceneManager.LoadScene((int)scene_to_load - 1);
    }

    IEnumerator LoadLevel(Scenes scene_to_load)
    {
        _isLoadingScene = true;

        _animator.SetTrigger("Start");
        
        yield return new WaitForSeconds(transition_time);

        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene((int)scene_to_load);
            yield break;
        }

        // Keep clients synchronized in multiplayer transitions.
        PhotonNetwork.AutomaticallySyncScene = true;

        // In-room transitions should be driven only by the master client.
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
        {
            yield break;
        }

        PhotonNetwork.LoadLevel((int)scene_to_load);
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
