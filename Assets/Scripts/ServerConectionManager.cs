using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;

public class ServerConectionManager : MonoBehaviourPunCallbacks
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);

        var loader1 = UnitLoaderController.Instance;
        if (loader1 != null ) Destroy(loader1);

        PhotonNetwork.ConnectUsingSettings();
        SceneLoaderController.Instance.LoadNextLevel(Scenes.MainMenu);
    }   
}
