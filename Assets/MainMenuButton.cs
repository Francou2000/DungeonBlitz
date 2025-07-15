using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuButton : MonoBehaviourPunCallbacks
{
    public string mainMenu_sceneName;

    Button my_button;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        my_button = GetComponent<Button>();
        my_button.onClick.AddListener(ReturnToMainMenu);
    }

    public void ReturnToMainMenu()
    {
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene(mainMenu_sceneName);
    }
}
