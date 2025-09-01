using Photon.Pun;
using Photon.Realtime;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{
    [SerializeField] TMP_InputField createRoomName, createRoomPsw;
    [SerializeField] TMP_InputField joinRoomName, joinRoomPsw;
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button createButton, JoinButton;
    
    [SerializeField] string rommScene;
    private bool roomID;
    private void Start()
    {
        nicknameInput.onValueChanged.AddListener(OnNicknameChanged);
        createRoomName.onValueChanged.AddListener(OnRoomIDChanged);
        joinRoomName.onValueChanged.AddListener(OnRoomIDChanged);
        JoinButton.interactable = false;
        createButton.interactable = false;
    }

    public void OnNicknameChanged(string value)
    {
            createButton.interactable = !string.IsNullOrWhiteSpace(value);
            JoinButton.interactable = !string.IsNullOrWhiteSpace(value);
    }

    public void OnRoomIDChanged(string value)
    {
        roomID = !string.IsNullOrWhiteSpace(value);
    }

    public void CreateRoom()
    {
        if (roomID)
        {
            string nick = nicknameInput.text.Trim().ToUpper();
            //Debug.Log(nick+" se unio a la room");
            PlayerPrefs.SetString("playerNickname",nick);
            PhotonNetwork.NickName = nick;

            // Create room logic
            RoomOptions options = new RoomOptions();
            options.MaxPlayers = 5;  // máximo de jugadores
            options.IsVisible = true;
            options.IsOpen = true;

            // Agregamos una propiedad personalizada para la contraseña
            options.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable()
            {
                { "pwd", createRoomPsw.text }
            };

            // Indicamos qué propiedades son visibles en el lobby
            options.CustomRoomPropertiesForLobby = new string[] { "pwd" };

            // Guardamos la contraseña que quiere usar el cliente
            PlayerPrefs.SetString("AttemptedPwd", createRoomPsw.text);

            // Creamos la room
            PhotonNetwork.CreateRoom(createRoomName.text, options, TypedLobby.Default);
        }
    }

    public void JoinRoom()
    {
        if (roomID)
        {
            string nick = nicknameInput.text.Trim().ToUpper();
            Debug.Log(nick+" se unio a la room");
            PlayerPrefs.SetString("playerNickname",nick);
            PhotonNetwork.NickName = nick;

            // Join room logic
            // Intentamos unirnos a la sala por nombre
            PhotonNetwork.JoinRoom(joinRoomName.text);

            // Guardamos la contraseña que quiere usar el cliente
            PlayerPrefs.SetString("AttemptedPwd", joinRoomPsw.text);
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Intentando entrar a la sala: " + PhotonNetwork.CurrentRoom.Name);

        // Verificamos si la contraseña coincide
        string attemptedPwd = PlayerPrefs.GetString("AttemptedPwd", "");
        string roomPwd = PhotonNetwork.CurrentRoom.CustomProperties["pwd"].ToString();

        if (attemptedPwd != roomPwd)
        {
            Debug.LogWarning("Contraseña incorrecta, te expulsamos.");
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            Debug.Log("Contaseña correcta, entraste a la sala: " + PhotonNetwork.CurrentRoom.Name);
            PhotonNetwork.LoadLevel(rommScene);
        }
    }
    
}
