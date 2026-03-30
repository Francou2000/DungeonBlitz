using Photon.Pun.Demo.SlotRacer.Utils;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CombatLogUI : MonoBehaviour
{
    public static CombatLogUI Instance { get; private set; }

    [SerializeField] private TMP_Text logText;
    [SerializeField] private int maxLines = 10;

    private readonly List<string> _lines = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        if (logText != null)
        {
            logText.enableWordWrapping = true;
            logText.overflowMode = TextOverflowModes.Overflow;
        }
    }
    
    public static void Log(string message)
    {
        if(string.IsNullOrEmpty(message))
            return;

        if (Instance == null)
        {
            Debug.LogWarning("CombatLogUI instance not found.");
            return;
        }

        Instance.AddLine(message);
    }

    private void AddLine(string message)
    {
        _lines.Add(message);

        while (_lines.Count > maxLines)
            _lines.RemoveAt(0);

        TrimOldLinesUntilFits();
    }

    private void TrimOldLinesUntilFits()
    {
        if (logText == null || _lines.Count == 0)
            return;

        int firstLineToKeep = 0;
        while (firstLineToKeep < _lines.Count - 1)
        {
            string candidate = string.Join("\n", _lines.GetRange(firstLineToKeep, _lines.Count - firstLineToKeep));
            if (FitsInLogBox(candidate))
                break;

            firstLineToKeep++;
        }

        if (firstLineToKeep > 0)
            _lines.RemoveRange(0, firstLineToKeep);

        logText.text = string.Join("\n", _lines);
    }

    private bool FitsInLogBox(string text)
    {
        Rect rect = logText.rectTransform.rect;
        Vector4 margin = logText.margin;
        float availableWidth = Mathf.Max(0f, rect.width - margin.x - margin.z);
        float availableHeight = Mathf.Max(0f, rect.height - margin.y - margin.w);

        Vector2 preferred = logText.GetPreferredValues(text, availableWidth, float.PositiveInfinity);
        return preferred.y <= availableHeight;
    }
}
