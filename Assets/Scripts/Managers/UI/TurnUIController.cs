using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TurnUIController : MonoBehaviour
{
    public static TurnUIController Instance { get; private set; }

    [Header("UI References")]
    public TMP_Text turnNumberText;
    public TMP_Text turnOwnerText;
    public TMP_Text timerText;

    [Header("Time Pools")]
    public TMP_Text heroTimeText;
    public TMP_Text monsterTimeText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void UpdateTurnUI(int turnNumber, UnitFaction currentFaction, float currentTimeRemaining, Dictionary<UnitFaction, float> timePools)
    {
        turnNumberText.text = $"Turn: {turnNumber}";
        turnOwnerText.text = $"Current: {currentFaction}";

        // Time left in this turn
        timerText.text = $"Turn Time Left: {Mathf.CeilToInt(currentTimeRemaining)}s";

        // Show total time pool for each faction using the synced dictionary
        if (timePools.TryGetValue(UnitFaction.Hero, out float heroTime))
            heroTimeText.text = $"Hero Time: {FormatTime(heroTime)}";
        else
            heroTimeText.text = "Hero Time: --:--";

        if (timePools.TryGetValue(UnitFaction.Monster, out float monsterTime))
            monsterTimeText.text = $"Monster Time: {FormatTime(monsterTime)}";
        else
            monsterTimeText.text = "Monster Time: --:--";
    }

    private string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{mins:00}:{secs:00}";
    }
}
