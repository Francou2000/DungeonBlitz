using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ChangeButtonSize : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] float bigSize = 600f;
    [SerializeField] float resizeSpeed = 1f;
    [SerializeField] float moveSpeed = 1f;
    [SerializeField] RectTransform bigSizePosition;

    Vector2 originalPosition;
    Vector2 originalSize;
    RectTransform rectTransform;


    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalSize = rectTransform.sizeDelta;
        originalPosition = rectTransform.position;
    }

    

    public void MakeButtonBig()
    {
        StartCoroutine(MoveAndSizeUp(false));
    }
    public void MakeButtonSmall()
    {
        StopAllCoroutines();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        MakeButtonBig();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        MakeButtonSmall();
    }

    public IEnumerator MoveAndSizeUp(bool moveUp)
    {
        float actualSize = rectTransform.sizeDelta.y;

        if (moveUp)
        {
            while (actualSize < bigSize)
            {
                if (rectTransform.position.y < bigSizePosition.position.y)
                {
                    rectTransform.Translate(transform.up * moveSpeed);
                }
                else { rectTransform.position = bigSizePosition.position; }

                actualSize += Time.deltaTime * resizeSpeed;
                rectTransform.sizeDelta = new Vector2(originalSize.x, actualSize);

                yield return new WaitForEndOfFrame();
            }
        }
        else
        {
            while (actualSize < bigSize)
            {
                if (rectTransform.position.y > bigSizePosition.position.y)
                {
                    rectTransform.Translate(-transform.up * moveSpeed);
                }
                else { rectTransform.position = bigSizePosition.position; }

                actualSize += Time.deltaTime * resizeSpeed;
                rectTransform.sizeDelta = new Vector2(originalSize.x, actualSize);

                yield return new WaitForEndOfFrame();
            }
        }
    }
}
