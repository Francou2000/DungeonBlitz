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

        if (logText != null)
            logText.text = string.Join("\n", _lines);
    }
}

