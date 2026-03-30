using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{
    [Header("Create Room")]
    [SerializeField] private TMP_InputField createRoomName;
    [SerializeField] private TMP_InputField createRoomPsw;

    [Header("Join Room")]
    [SerializeField] private TMP_InputField joinRoomName;
    [SerializeField] private TMP_InputField joinRoomPsw;

    [Header("Shared")]
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button createButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Scenes roomScene;

    private const string PasswordPropertyKey = "pwd";
    private const string AttemptedPasswordKey = "AttemptedPwd";

    private void Start()
    {
        if (nicknameInput != null) nicknameInput.onValueChanged.AddListener(OnInputChanged);
        if (createRoomName != null) createRoomName.onValueChanged.AddListener(OnInputChanged);
        if (joinRoomName != null) joinRoomName.onValueChanged.AddListener(OnInputChanged);

        RefreshButtonState();
    }

    private void OnDestroy()
    {
        if (nicknameInput != null) nicknameInput.onValueChanged.RemoveListener(OnInputChanged);
        if (createRoomName != null) createRoomName.onValueChanged.RemoveListener(OnInputChanged);
        if (joinRoomName != null) joinRoomName.onValueChanged.RemoveListener(OnInputChanged);
    }

    private void OnInputChanged(string _)
    {
        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        bool hasNickname = !string.IsNullOrWhiteSpace(nicknameInput != null ? nicknameInput.text : string.Empty);

        if (createButton != null)
            createButton.interactable = hasNickname && !string.IsNullOrWhiteSpace(createRoomName != null ? createRoomName.text : string.Empty);

        if (joinButton != null)
            joinButton.interactable = hasNickname && !string.IsNullOrWhiteSpace(joinRoomName != null ? joinRoomName.text : string.Empty);
    }

    public void CreateRoom()
    {
        if (createButton != null && !createButton.interactable) return;

        string roomName = createRoomName != null ? createRoomName.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(roomName))
        {
            Debug.LogWarning("[CreateAndJoinRooms] Room name is empty. Cannot create room.");
            return;
        }

        if (!TryApplyNickname()) return;

        string attemptedPwd = createRoomPsw != null ? createRoomPsw.text : string.Empty;
        PlayerPrefs.SetString(AttemptedPasswordKey, attemptedPwd);

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 5,
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties = new Hashtable
            {
                { PasswordPropertyKey, attemptedPwd }
            },
            CustomRoomPropertiesForLobby = new[] { PasswordPropertyKey }
        };

        PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
    }

    public void JoinRoom()
    {
        if (joinButton != null && !joinButton.interactable) return;

        string roomName = joinRoomName != null ? joinRoomName.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(roomName))
        {
            Debug.LogWarning("[CreateAndJoinRooms] Room name is empty. Cannot join room.");
            return;
        }

        if (!TryApplyNickname()) return;

        // Persist password BEFORE JoinRoom call so callback always reads latest value.
        string attemptedPwd = joinRoomPsw != null ? joinRoomPsw.text : string.Empty;
        PlayerPrefs.SetString(AttemptedPasswordKey, attemptedPwd);

        PhotonNetwork.JoinRoom(roomName);
    }

    private bool TryApplyNickname()
    {
        string nick = nicknameInput != null ? nicknameInput.text.Trim().ToUpperInvariant() : string.Empty;
        if (string.IsNullOrWhiteSpace(nick))
        {
            Debug.LogWarning("[CreateAndJoinRooms] Nickname is empty. Cannot continue.");
            return false;
        }

        PlayerPrefs.SetString("playerNickname", nick);
        PhotonNetwork.NickName = nick;
        return true;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[CreateAndJoinRooms] Joined room: " + PhotonNetwork.CurrentRoom?.Name);

        // Pause queue during scene transition only after validation passes.
        string attemptedPwd = PlayerPrefs.GetString(AttemptedPasswordKey, string.Empty);

        string roomPwd = string.Empty;
        if (PhotonNetwork.CurrentRoom != null
            && PhotonNetwork.CurrentRoom.CustomProperties != null
            && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PasswordPropertyKey, out object roomPwdObj)
            && roomPwdObj != null)
        {
            roomPwd = roomPwdObj.ToString();
        }

        if (!string.Equals(attemptedPwd, roomPwd, System.StringComparison.Ordinal))
        {
            Debug.LogWarning("[CreateAndJoinRooms] Invalid room password. Leaving room.");
            PhotonNetwork.IsMessageQueueRunning = true;
            PhotonNetwork.LeaveRoom();
            return;
        }

        Debug.Log("[CreateAndJoinRooms] Password validated. Loading room scene.");
        PhotonNetwork.IsMessageQueueRunning = false;
        SceneLoaderController.Instance.LoadNextLevel(roomScene);
    }

    public override void OnLeftRoom()
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        RefreshButtonState();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        Debug.LogError($"[CreateAndJoinRooms] CreateRoom failed ({returnCode}): {message}");
        RefreshButtonState();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        Debug.LogError($"[CreateAndJoinRooms] JoinRoom failed ({returnCode}): {message}");
        RefreshButtonState();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        Debug.LogWarning($"[CreateAndJoinRooms] Disconnected from Photon: {cause}");
        RefreshButtonState();
    }
}
