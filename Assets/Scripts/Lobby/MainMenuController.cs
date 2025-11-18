using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class MainMenuController : MonoBehaviourPunCallbacks
{
    [Header("Disconnect Info Panel")]
    [SerializeField] private GameObject disconnectInfoPanel;
    [SerializeField] private TextMeshProUGUI disconnectInfoText;

    [Header("Logo Screen")]
    [SerializeField] private GameObject logoGameObject;
    [SerializeField] private bool showLogoOnStart = true;

    private bool isWaitingForInput = false;

    private void Start()
    {
        // Limpiar UnitLoaderController si existe (por si hay una desconexión previa)
        CleanupUnitLoader();

        // Verificar conexión a Photon y reconectar si es necesario
        CheckAndReconnectPhoton();

        // Mostrar u ocultar el logo según el booleano configurado en el editor
        if (logoGameObject != null)
        {
            logoGameObject.SetActive(showLogoOnStart);
            isWaitingForInput = showLogoOnStart;
        }

        // Verificar si hay una razón de desconexión
        if (DisconnectInfoManager.Instance != null && DisconnectInfoManager.Instance.HasDisconnectReason())
        {
            ShowDisconnectInfo();
        }
        else
        {
            // Asegurarse de que el panel esté oculto si no hay razón
            if (disconnectInfoPanel != null)
            {
                disconnectInfoPanel.SetActive(false);
            }
        }
    }

    private void Update()
    {
        // Esperar a que el usuario presione cualquier tecla o botón del mouse
        if (isWaitingForInput)
        {
            // Detectar cualquier input del teclado
            if (Input.anyKeyDown)
            {
                HideLogoAndShowMenu();
            }
            // Detectar clicks del mouse
            else if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                HideLogoAndShowMenu();
            }
        }
    }

    private void HideLogoAndShowMenu()
    {
        if (logoGameObject != null)
        {
            logoGameObject.SetActive(false);
        }
        isWaitingForInput = false;
    }

    private void ShowDisconnectInfo()
    {
        if (disconnectInfoPanel == null)
        {
            Debug.LogWarning("[MainMenuController] Disconnect Info Panel no está asignado en el inspector");
            return;
        }

        if (disconnectInfoText == null)
        {
            Debug.LogWarning("[MainMenuController] Disconnect Info Text no está asignado en el inspector");
            return;
        }

        // Obtener el mensaje de desconexión
        string message = DisconnectInfoManager.Instance.GetDisconnectMessage();
        
        // Establecer el texto y activar el panel
        disconnectInfoText.text = message;
        disconnectInfoPanel.SetActive(true);

        Debug.Log($"[MainMenuController] Mostrando panel de desconexión: {message}");
    }

    // Método público para cerrar el panel (puede ser llamado por un botón)
    public void CloseDisconnectInfo()
    {
        if (disconnectInfoPanel != null)
        {
            disconnectInfoPanel.SetActive(false);
        }

        // Limpiar la razón de desconexión
        if (DisconnectInfoManager.Instance != null)
        {
            DisconnectInfoManager.Instance.ClearDisconnectReason();
        }
    }

    /// <summary>
    /// Limpia el UnitLoaderController si existe, similar a FinalScreenManager
    /// </summary>
    private void CleanupUnitLoader()
    {
        var loader = UnitLoaderController.Instance;
        if (loader != null)
        {
            Debug.Log("[MainMenuController] Limpiando UnitLoaderController...");
            Destroy(loader.gameObject);
        }
    }

    /// <summary>
    /// Verifica la conexión a Photon y reconecta si es necesario
    /// </summary>
    private void CheckAndReconnectPhoton()
    {
        // Verificar si no está conectado o si está en un estado de error
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("[MainMenuController] No conectado a Photon. Intentando reconectar...");
            
            // Si está en una sala, salir primero
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
            
            // Intentar reconectar
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (PhotonNetwork.InRoom)
        {
            // Si está conectado pero aún en una sala, salir de la sala
            Debug.Log("[MainMenuController] Aún en una sala. Saliendo de la sala...");
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            Debug.Log("[MainMenuController] Conectado a Photon correctamente.");
        }
    }

    /// <summary>
    /// Método público para limpiar recursos y volver al estado inicial del menú
    /// Similar a ReturnToMainMenu del FinalScreenManager
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("[MainMenuController] Limpiando recursos y volviendo al menú principal...");

        // Limpiar UnitLoaderController
        CleanupUnitLoader();

        // Salir de la sala si está en una
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        // Si no está conectado, intentar reconectar
        else if (!PhotonNetwork.IsConnected)
        {
            CheckAndReconnectPhoton();
        }
    }

    // === PHOTON CALLBACKS ===

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[MainMenuController] Desconectado de Photon: {cause}");

        // Limpiar UnitLoaderController cuando hay una desconexión
        CleanupUnitLoader();

        // Intentar reconectar automáticamente
        Debug.Log("[MainMenuController] Intentando reconectar a Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[MainMenuController] Conectado al servidor maestro de Photon.");
        
        // Unirse al lobby automáticamente
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[MainMenuController] Unido al lobby de Photon.");
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[MainMenuController] Salido de la sala de Photon.");
    }
}


