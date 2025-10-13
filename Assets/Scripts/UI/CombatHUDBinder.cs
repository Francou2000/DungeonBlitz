using UnityEngine;

public class CombatHUDBinder : MonoBehaviour
{
    [SerializeField] CombatHUD hud;

    void OnEnable()
    {
        TurnManager.OnActiveControllerChanged += HandleActiveChanged;
    }

    void OnDisable()
    {
        TurnManager.OnActiveControllerChanged -= HandleActiveChanged;
    }

    void HandleActiveChanged(UnitController ctrl)
    {
        if (!hud || !ctrl) return;
        hud.Bind(ctrl);
    }
}
