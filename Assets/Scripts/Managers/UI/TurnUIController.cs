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

    public void UpdateTurnUI(int turnNumber, UnitFaction currentFaction, float currentTimeRemaining)
    {
        turnNumberText.text = $"Turn: {turnNumber}";
        turnOwnerText.text = $"Current: {currentFaction}";

        // Time left this turn
        timerText.text = $"Turn Time Left: {Mathf.CeilToInt(currentTimeRemaining)}s";

        // Update total time pools
        float heroTime = TurnManager.Instance?.GetTimePool(UnitFaction.Hero) ?? 0f;
        float monsterTime = TurnManager.Instance?.GetTimePool(UnitFaction.Monster) ?? 0f;

        heroTimeText.text = $"Hero Time: {FormatTime(heroTime)}";
        monsterTimeText.text = $"Monster Time: {FormatTime(monsterTime)}";
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60);
        int sec = Mathf.FloorToInt(seconds % 60);
        return $"{minutes:D2}:{sec:D2}";
    }
}
