using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class APPipsView : MonoBehaviour
{
    [SerializeField] private GameObject pipPrefab;     // small Image as prefab
    [SerializeField] private Transform container;      // HorizontalLayoutGroup/Repositioner

    private readonly List<Image> _pips = new();

    public void SetMax(int max)
    {
        max = Mathf.Clamp(max, 0, 20);
        while (_pips.Count < max)
        {
            var go = Instantiate(pipPrefab, container ? container : transform);
            _pips.Add(go.GetComponent<Image>());
        }
        for (int i = 0; i < _pips.Count; i++)
            _pips[i].gameObject.SetActive(i < max);
    }

    public void SetCurrent(int current)
    {
        for (int i = 0; i < _pips.Count; i++)
            _pips[i].enabled = (i < current); // lit if enabled
    }
}
