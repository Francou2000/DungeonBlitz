using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject optionsUI;          // Panel principal de pausa
    [SerializeField] private GameObject optionsUISound;     // Panel de opciones de sonido
    
    [Header("Buttons")]
    [SerializeField] private Button soundButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button backButton;
    
    [Header("Scene Settings")]
    [SerializeField] private Scenes mainMenuScene = Scenes.MainMenu;
    
    private bool isPaused = false;
    
    private void Start()
    {
        // Configurar listeners de botones
        if (soundButton != null)
            soundButton.onClick.AddListener(OnSoundButtonClicked);
        
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);
        
        // Inicializar UI oculta
        if (optionsUI != null)
            optionsUI.SetActive(false);
        
        if (optionsUISound != null)
            optionsUISound.SetActive(false);
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        
        if (optionsUI != null)
        {
            optionsUI.SetActive(isPaused);
            
            // Si estamos cerrando el menú, también cerrar el submenú de sonido
            if (!isPaused && optionsUISound != null)
            {
                optionsUISound.SetActive(false);
            }
        }
        
        // Pausar/despausar el juego
        //Time.timeScale = isPaused ? 0f : 1f;
    }
    
    public void ShowPauseMenu()
    {
        if (!isPaused)
            TogglePause();
    }
    
    public void HidePauseMenu()
    {
        if (isPaused)
            TogglePause();
    }
    
    private void OnSoundButtonClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonSound();
        
        // Mostrar el menú de sonido y ocultar el menú principal
        if (optionsUISound != null)
            optionsUISound.SetActive(true);
        
        if (optionsUI != null)
            optionsUI.SetActive(false);
    }
    
    private void OnMainMenuButtonClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonSound();
        
        // Confirmar antes de salir (opcional)
        Debug.Log("[PauseMenu] Leaving room and returning to main menu...");
        
        // Abandonar la sala de Photon
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            // Si no estamos en una room, simplemente cargar el menú principal
            ReturnToMainMenu();
        }
    }
    
    private void OnBackButtonClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonSound();
        
        // Si estamos en el submenú de sonido, volver al menú principal
        if (optionsUISound != null && optionsUISound.activeSelf)
        {
            optionsUISound.SetActive(false);
            if (optionsUI != null)
                optionsUI.SetActive(true);
        }
        else
        {
            // Si estamos en el menú principal, cerrar el menú de pausa
            HidePauseMenu();
        }
    }
    
    public override void OnLeftRoom()
    {
        Debug.Log("[PauseMenu] Successfully left room, loading main menu...");
        ReturnToMainMenu();
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[PauseMenu] Disconnected: {cause}");
        ReturnToMainMenu();
    }
    
    private void ReturnToMainMenu()
    {
        // Reanudar el juego antes de cambiar de escena
        //Time.timeScale = 1f;
        
        if (SceneLoaderController.Instance != null)
        {
            SceneLoaderController.Instance.LoadNextLevel(mainMenuScene);
        }
    }
    
    private void OnDestroy()
    {
        // Asegurar que el tiempo vuelva a la normalidad
        //Time.timeScale = 1f;
    }
}

