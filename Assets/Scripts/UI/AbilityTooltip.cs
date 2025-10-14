using UnityEngine;
using TMPro;

public class AbilityTooltip : MonoBehaviour
{
    public static AbilityTooltip Instance;

    [Header("Refs")]
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text body;

    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false); // start hidden
    }

    public static void Show(UnitAbility ab, Vector3 screenPos)
    {
        if (!Instance) return;

        if (ab == null)
        {
            Instance.title.text = "Move";
            Instance.body.text = "Spend AP to move. Click a valid destination within range.";
        }
        else
        {
            Instance.title.text = ab.name;
            Instance.body.text =
                $"Cost: {ab.actionCost}\n" +
                $"Range: {ab.range}\n" +
                $"{ab.description}";
        }

        Instance.transform.position = screenPos;
        Instance.gameObject.SetActive(true);
    }

    public static void Hide()
    {
        if (Instance) Instance.gameObject.SetActive(false);
    }
}
