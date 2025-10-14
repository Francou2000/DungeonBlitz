using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnTimerUI : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] Image background;
    [SerializeField] Sprite heroTurnSprite;
    [SerializeField] Sprite monsterTurnSprite;

    [Header("Texts")]
    [SerializeField] TMP_Text heroPoolText;     // left side: team pool for Heroes
    [SerializeField] TMP_Text monsterPoolText;  // right side: team pool for Monsters

    [Header("Fallbacks (only used if TM doesn't expose values)")]
    [SerializeField] float defaultTurnDuration = 30f;
    [SerializeField] float defaultMaxHeroPool = 180f;
    [SerializeField] float defaultMaxMonsterPool = 180f;

    UnitFaction _lastFaction = (UnitFaction)(-1);

    void OnEnable() { Refresh(force: true); }
    void Update() { Refresh(force: false); }

    void Refresh(bool force)
    {
        var tm = TurnManager.Instance;
        if (!tm) return;

        //  Swap background sprite by active faction
        if (force || tm.currentTurn != _lastFaction)
        {
            _lastFaction = tm.currentTurn;
            if (background)
                background.sprite = (_lastFaction == UnitFaction.Hero) ? heroTurnSprite : monsterTurnSprite;
        }

        //  Team pool numbers (MM:SS) — visible to everyone
        float heroPool = TurnManager.Instance?.GetTimePool(UnitFaction.Hero) ?? 0f;    // current remaining
        float monsterPool = TurnManager.Instance?.GetTimePool(UnitFaction.Monster) ?? 0f; // current remaining

        if (heroPoolText) heroPoolText.text = ToMMSS(heroPool);
        if (monsterPoolText) monsterPoolText.text = ToMMSS(monsterPool);
    }

    static string ToMMSS(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        int s = Mathf.CeilToInt(seconds);
        int m = s / 60;
        int r = s % 60;
        return $"{m:0}:{r:00}";
    }
}
