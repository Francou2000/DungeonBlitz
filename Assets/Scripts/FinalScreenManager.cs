using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class FinalScreenManager : MonoBehaviourPunCallbacks
{
    public int retry_accepted = 0;
    int retry_needed = 5;

    public TextMeshProUGUI retry_txt;

    void Start()
    {
        
    }

    public void WantToRetry()
    {
        photonView.RPC("TryRetry", RpcTarget.All);
    }

    [PunRPC]
    public void TryRetry()
    {
        retry_accepted++;
        retry_txt.text = "Play Again (" + retry_accepted + "/" + retry_needed + ")";
        if (retry_accepted == retry_needed)
        {
            SceneLoaderController.Instance.LoadNextLevel(Scenes.Lobby);
        }
    }

    
    public void ForceDisconnect()
    {
        photonView.RPC("ReturnToMainMenu", RpcTarget.All);
    }

    [PunRPC]
    public void ReturnToMainMenu()
    {
        Debug.Log("[WinLoseMenuButton] Returning to main menu...");

        var loader1 = UnitLoaderController.Instance;
        if (loader1 != null) Destroy(loader1.gameObject);

        // Salir de la sala de Photon
        PhotonNetwork.LeaveRoom();

        // La transición de escena se manejará en el callback OnLeftRoom
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[WinLoseMenuButton] Successfully left room, loading main menu...");

        // Una vez que hemos salido de la sala, cargar el menú principal
        SceneLoaderController.Instance.LoadNextLevel(Scenes.MainMenu);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[WinLoseMenuButton] Disconnected: {cause}");

        // En caso de desconexión, también cargar el menú principal
        SceneLoaderController.Instance.LoadNextLevel(Scenes.MainMenu);
    }

}
