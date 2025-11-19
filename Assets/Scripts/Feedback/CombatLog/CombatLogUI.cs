using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CombatLogUI : MonoBehaviour
{
    public static CombatLogUI Instance { get; private set; }

    [SerializeField] private TMP_Text logText;
    [SerializeField] private int maxLines = 10;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
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
        var lines = new List<string>();

        lines.Add(message);

        while (lines.Count > maxLines)
        {
            lines.RemoveAt(0);
        }

        if(logText != null)
            logText.text = string.Join("\n", lines);
    }
}
