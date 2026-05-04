using UnityEngine;
using UnityEngine.UI;

public class TutorialShowcase : MonoBehaviour
{
    [SerializeField] GameObject _thisUI;
    [SerializeField] GameObject _tutorialUI;

    [SerializeField] private bool _isFirstTime = true;


    void Start()
    {
        GetComponent<Button>().onClick.AddListener(ShowTutorial);
    }

    private void OnEnable()
    {
        if (_isFirstTime)
        {
            ShowTutorial();
            _isFirstTime = false;
        }
    }


    void ShowTutorial()
    {
        _tutorialUI.SetActive(true);
        _thisUI.SetActive(false);
    }
}
