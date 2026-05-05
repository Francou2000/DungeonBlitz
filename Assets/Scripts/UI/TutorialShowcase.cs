using UnityEngine;
using UnityEngine.UI;

public class TutorialShowcase : MonoBehaviour
{
    [SerializeField] GameObject _thisUI;
    [SerializeField] GameObject _tutorialUI;

    [SerializeField] private bool _isCreate;
    AudioManager audioManager;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(ShowTutorial);
    }

    private void OnEnable()
    {
        audioManager = AudioManager.Instance;
        if (_isCreate)
        {
            if (audioManager.is_first_create_tutorial)
            {
                ShowTutorial();
                audioManager.is_first_create_tutorial = false;
            }
        }
        else
        {
            if (audioManager.is_first_join_tutorial)
            {
                ShowTutorial();
                audioManager.is_first_join_tutorial = false;
            }

        }
    }


    void ShowTutorial()
    {
        _tutorialUI.SetActive(true);
        _thisUI.SetActive(false);
    }
}
