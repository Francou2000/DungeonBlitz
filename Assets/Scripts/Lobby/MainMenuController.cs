using UnityEngine;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Disconnect Info Panel")]
    [SerializeField] private GameObject disconnectInfoPanel;
    [SerializeField] private TextMeshProUGUI disconnectInfoText;

    private void Start()
    {
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
}

