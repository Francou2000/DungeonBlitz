using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class ChangeSceneWithButton : MonoBehaviourPunCallbacks
{
    Button my_button;
    [SerializeField] Scenes new_scene;
    public bool forceExit;
    
    void Start()
    {
        my_button = GetComponent<Button>();
        if (forceExit) my_button.onClick.AddListener(ReturnToMainMenu);
        else my_button.onClick.AddListener(ChangeScene);
    }

    void ChangeScene()
    {
        SceneLoaderController.Instance.LoadNextLevel(new_scene);
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
        SceneLoaderController.Instance.LoadNextLevel(Scenes.MainMenu);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[WinLoseMenuButton] Disconnected: {cause}");

        // En caso de desconexión, también cargar el menú principal
        SceneLoaderController.Instance.LoadNextLevel(Scenes.MainMenu);
    }
}
