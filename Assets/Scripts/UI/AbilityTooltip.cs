using UnityEngine;
using TMPro;

public class AbilityTooltip : MonoBehaviour
{
    public static AbilityTooltip Instance;
    [SerializeField] TMP_Text label;

    void Awake() { Instance = this; gameObject.SetActive(false); }

    public static void Show(UnitAbility ab, Vector3 worldPos)
    {
        if (!Instance) return;
        Instance.label.text = $"{ab.name}\nCost: {ab.actionCost}\nRange: {ab.range}";
        Instance.transform.position = worldPos;
        Instance.gameObject.SetActive(true);
    }
    public static void Hide() { if (Instance) Instance.gameObject.SetActive(false); }
}
