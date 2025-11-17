using UnityEngine;

public enum DisconnectReason
{
    None,
    DMDisconnected,
    TooManyPlayersDisconnected
}

public class DisconnectInfoManager : MonoBehaviour
{
    public static DisconnectInfoManager Instance { get; private set; }

    private DisconnectReason currentReason = DisconnectReason.None;
    private string disconnectMessage = "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetDisconnectReason(DisconnectReason reason, string message = "")
    {
        currentReason = reason;
        disconnectMessage = message;
        Debug.Log($"[DisconnectInfoManager] Disconnect reason set: {reason} - {message}");
    }

    public DisconnectReason GetDisconnectReason()
    {
        return currentReason;
    }

    public string GetDisconnectMessage()
    {
        if (!string.IsNullOrEmpty(disconnectMessage))
        {
            return disconnectMessage;
        }

        // Mensajes por defecto según la razón
        switch (currentReason)
        {
            case DisconnectReason.DMDisconnected:
                return "El Dungeon Master se ha desconectado. La partida ha terminado.";
            case DisconnectReason.TooManyPlayersDisconnected:
                return "Demasiados jugadores se han desconectado. La partida ha terminado.";
            default:
                return "";
        }
    }

    public bool HasDisconnectReason()
    {
        return currentReason != DisconnectReason.None;
    }

    public void ClearDisconnectReason()
    {
        currentReason = DisconnectReason.None;
        disconnectMessage = "";
    }
}

