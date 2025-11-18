using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Script que permite desconectar rápidamente de la sala y volver al menú principal
/// presionando F10 o la tecla "°" (tilde/backtick)
/// </summary>
public class QuickDisconnectController : MonoBehaviourPunCallbacks
{
    [Header("Configuración")]
    [SerializeField] private bool enableF10 = true;
    [SerializeField] private bool enableTilde = true;
    [SerializeField] private KeyCode tildeKey = KeyCode.BackQuote; // Tecla "°" o tilde

    private bool isDisconnecting = false;

    private void Update()
    {
        // No procesar input si ya se está desconectando
        if (isDisconnecting) return;

        // Verificar si se presionó F10
        if (enableF10 && Input.GetKeyDown(KeyCode.F10))
        {
            DisconnectToMainMenu();
        }
        // Verificar si se presionó la tecla tilde/backtick ("°")
        else if (enableTilde && Input.GetKeyDown(tildeKey))
        {
            DisconnectToMainMenu();
        }
    }

    /// <summary>
    /// Desconecta al jugador de la sala y lo lleva al menú principal
    /// </summary>
    private void DisconnectToMainMenu()
    {
        if (isDisconnecting)
        {
            Debug.LogWarning("[QuickDisconnectController] Ya se está procesando una desconexión.");
            return;
        }

        Debug.Log("[QuickDisconnectController] Iniciando desconexión rápida al menú principal...");
        isDisconnecting = true;

        // Limpiar UnitLoaderController si existe
        CleanupUnitLoader();

        // Si estamos en una sala, salir de ella
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            Debug.Log("[QuickDisconnectController] Saliendo de la sala de Photon...");
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            // Si no estamos en una sala, ir directamente al main menu
            Debug.Log("[QuickDisconnectController] No estamos en una sala, cargando main menu directamente...");
            LoadMainMenu();
        }
    }

    /// <summary>
    /// Limpia el UnitLoaderController si existe
    /// </summary>
    private void CleanupUnitLoader()
    {
        var loader = UnitLoaderController.Instance;
        if (loader != null)
        {
            Debug.Log("[QuickDisconnectController] Limpiando UnitLoaderController...");
            Destroy(loader.gameObject);
        }
    }

    /// <summary>
    /// Carga la escena del menú principal
    /// </summary>
    private void LoadMainMenu()
    {
        if (SceneLoaderController.Instance != null)
        {
            SceneLoaderController.Instance.LoadNextLevel(Scenes.MainMenu);
        }
        else
        {
            Debug.LogError("[QuickDisconnectController] SceneLoaderController.Instance es null. No se puede cargar el main menu.");
            // Fallback: intentar desconectar completamente
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
        }
        isDisconnecting = false;
    }

    // === PHOTON CALLBACKS ===

    public override void OnLeftRoom()
    {
        Debug.Log("[QuickDisconnectController] Salido de la sala. Cargando menú principal...");
        LoadMainMenu();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[QuickDisconnectController] Desconectado de Photon: {cause}. Cargando menú principal...");
        LoadMainMenu();
    }
}

