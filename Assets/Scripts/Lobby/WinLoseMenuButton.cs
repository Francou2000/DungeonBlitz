using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinLoseMenuButton : MonoBehaviourPunCallbacks
{
    public Scenes mainMenu_sceneName;

    Button my_button;
    
    void Start()
    {
        my_button = GetComponent<Button>();
        my_button.onClick.AddListener(ReturnToMainMenu);
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("[WinLoseMenuButton] Returning to main menu...");
        
        // Salir de la sala de Photon
        PhotonNetwork.LeaveRoom();
        
        // La transición de escena se manejará en el callback OnLeftRoom
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[WinLoseMenuButton] Successfully left room, loading main menu...");
        
        // Una vez que hemos salido de la sala, cargar el menú principal
        SceneLoaderController.Instance.LoadNextLevel(mainMenu_sceneName);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[WinLoseMenuButton] Disconnected: {cause}");
        
        // En caso de desconexión, también cargar el menú principal
        SceneLoaderController.Instance.LoadNextLevel(mainMenu_sceneName);
    }
}
