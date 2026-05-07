using UnityEngine;
using UnityEngine.UI;

public class ChangeSceneWithButton : MonoBehaviour
{
    private Button myButton;

    [SerializeField] private Scenes new_scene;
    [SerializeField] private bool forceExit;

    private void Start()
    {
        myButton = GetComponent<Button>();
        if (myButton == null)
        {
            Debug.LogError("[ChangeSceneWithButton] Missing Button component.");
            enabled = false;
            return;
        }

        if (forceExit)
        {
            myButton.onClick.AddListener(ReturnToMainMenu);
        }
        else
        {
            myButton.onClick.AddListener(ChangeScene);
        }
    }

    private void OnDestroy()
    {
        if (myButton == null) return;

        if (forceExit)
        {
            myButton.onClick.RemoveListener(ReturnToMainMenu);
        }
        else
        {
            myButton.onClick.RemoveListener(ChangeScene);
        }
    }

    private void ChangeScene()
    {
        if (SceneLoaderController.Instance == null)
        {
            Debug.LogError("[ChangeSceneWithButton] SceneLoaderController.Instance is null.");
            return;
        }

        SceneLoaderController.Instance.LoadNextLevel(new_scene);
    }

    public void ReturnToMainMenu()
    {
        SceneLoaderController.Instance.LoadNextLevel(Scenes.MainMenu);
    }
}
