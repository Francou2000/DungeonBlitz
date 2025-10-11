using System.Collections;
using UnityEngine;

public class ShowUntInfo : MonoBehaviour
{
    RectTransform my_rectTransform;
    Vector2 original_pos;
    Vector2 open_pos;
    public float speed = 1;

    bool is_open = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        my_rectTransform = GetComponent<RectTransform>();
        original_pos = my_rectTransform.localPosition;
        open_pos = original_pos + new Vector2(330, 0);
        //my_rectTransform.localPosition = open_pos;
    }

    public void ToggleDetails()
    {
        if (is_open)
        {
            StopAllCoroutines();
            StartCoroutine(CloseDetails());
            is_open = !is_open;
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(OpenDetails());
            is_open = !is_open;
        }
    }

    public IEnumerator OpenDetails()
    {
        while (my_rectTransform.localPosition.x < open_pos.x)
        {
            my_rectTransform.Translate(new Vector2(1, 0) * speed);
            yield return new WaitForEndOfFrame();
        }
        my_rectTransform.localPosition = open_pos;
    }

    public IEnumerator CloseDetails()
    {
        while (my_rectTransform.localPosition.x > original_pos.x)
        {
            my_rectTransform.Translate(new Vector2(-1, 0) * speed);
            yield return new WaitForEndOfFrame();
        }
        my_rectTransform.localPosition = original_pos;
    }
}

