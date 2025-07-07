using System;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{
    [SerializeField] TMP_InputField createInput;
    [SerializeField] TMP_InputField joinInput;
    [SerializeField] private TMP_InputField nicknameInput,createRoomInput,joinRoomInput;
    [SerializeField] private Button createButton, JoinButton;
    
    [SerializeField] string rommScene;
    private bool roomID;
    private void Start()
    {
        nicknameInput.onValueChanged.AddListener(OnNicknameChanged);
        createRoomInput.onValueChanged.AddListener(OnRoomIDChanged);
        joinRoomInput.onValueChanged.AddListener(OnRoomIDChanged);
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
            Debug.Log(nick+" se unio a la room");
            PlayerPrefs.SetString("playerNickname",nick);
            PhotonNetwork.NickName = nick;
            PhotonNetwork.CreateRoom(createInput.text);
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
            PhotonNetwork.JoinRoom(joinInput.text);
        }
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel(rommScene);
    }
    
}
