using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class APPipsSpatialView : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite activePipSprite;
    [SerializeField] private Sprite inactivePipSprite;

    [Header("Layout")]
    [SerializeField] private RectTransform pipsContainer;
    [SerializeField] private Image pipPrefab;
    [SerializeField] private float pipSpacing = 4f;
    [SerializeField] private float pipSize = 10f;

    [Header("Behavior")]    
    [SerializeField] private bool hideWhenZero = false;

    private List<Image> pips = new();

    private void Reset()
    {
        pipsContainer = (RectTransform)transform;
    }

    public void Set(int current, int max)
    {
        current = Mathf.Max(0, current);
        max = Mathf.Max(0, max);

        EnsurePipCount(max);
        UpdatePips(current, max);

        if(hideWhenZero)
            gameObject.SetActive(max > 0);
    }   

    private void EnsurePipCount(int max)
    {
        Debug.Log("Max pips: " + max);
        // add
        while (pips.Count < max)
        {
            var image = Instantiate(pipPrefab, pipsContainer);
            var rt = (RectTransform)image.transform;
            rt.sizeDelta = new Vector2(pipSize, pipSize);
            pips.Add(image);
        }

        // remove
        while (pips.Count > max)
        {
            var image = pips[^1];
            pips.RemoveAt(pips.Count - 1);
            if (image) Destroy(image.gameObject);
        }

        // update layout
        var hg = pipsContainer.GetComponent<HorizontalLayoutGroup>();
        if(hg)
        {
            hg.spacing = pipSpacing;
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.childForceExpandWidth = hg.childForceExpandHeight = false;   
        }
    }

    private void UpdatePips(int current, int max)
    {
        for (int i = 0; i < pips.Count; i++)
        {
            var image = pips[i];
            if (!image) continue;

            bool isFilled = i < current;
            image.sprite = isFilled ? activePipSprite : inactivePipSprite;
        }
    }
}
