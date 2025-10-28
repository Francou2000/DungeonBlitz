using System.Collections;
using UnityEngine;
using TMPro;

public class TurnAnnouncementUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform banner;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Settings")]
    [SerializeField] private float slideDuration = 1.0f;
    [SerializeField] private float stayDuration = 1.5f;
    [SerializeField] private Vector2 offLeft = new Vector2(-1000f, 0f);
    [SerializeField] private Vector2 center = new Vector2(0f, 0f);
    [SerializeField] private Vector2 offRight = new Vector2(1000f, 0f);
    [SerializeField] private Color heroesColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] private Color monsterColor = new Color(1f, 0.3f, 0.3f);

    private Coroutine _routine;

    private void OnEnable()
    {
        TurnManager.OnTurnBegan += HandleTurnBegan;
    }

    private void OnDisable()
    {
        TurnManager.OnTurnBegan -= HandleTurnBegan;
    }

    private void HandleTurnBegan(UnitFaction faction)
    {
        // Always make sure the GameObject is active for the animation
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        // Restart animation each time
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(AnimateBanner(faction));
    }

    private IEnumerator AnimateBanner(UnitFaction faction)
    {
        // Prepare text and color
        label.text = faction == UnitFaction.Hero ? "HEROES' TURN" : "MONSTER'S TURN";
        label.color = faction == UnitFaction.Hero ? heroesColor : monsterColor;

        // Start from left
        banner.anchoredPosition = offLeft;
        var cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        // Slide in
        float t = 0;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            banner.anchoredPosition = Vector2.Lerp(offLeft, center, Mathf.SmoothStep(0, 1, t / slideDuration));
            yield return null;
        }

        yield return new WaitForSeconds(stayDuration);

        // Slide out
        t = 0;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            banner.anchoredPosition = Vector2.Lerp(center, offRight, Mathf.SmoothStep(0, 1, t / slideDuration));
            yield return null;
        }

        // Hide but keep object active so next turn works
        cg.alpha = 0f;
        banner.anchoredPosition = offLeft;
    }

}
