using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DebugTools;
using Photon.Pun.Demo.Procedural;

public class CombatUI : MonoBehaviour
{
    public static CombatUI Instance { get; private set; }

    [Header("References")]
    public GameObject abilityPanel;         
    public GameObject abilityButtonPrefab;  
    public Transform abilityListContainer;  

    List<GameObject> spawnedButtons = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    /// Populate and show the abilities for the active unit.
    public void ShowAbilities(UnitController controller)
    {
        HideAbilities();

        if (controller == null) return;
        var abilities = controller.unit.Model.Abilities; // from your UnitModel

        foreach (var ability in abilities)
        {
            GameObject btnObj = Instantiate(abilityButtonPrefab, abilityListContainer);
            spawnedButtons.Add(btnObj);

            // Set the button text
            var label = btnObj.GetComponentInChildren<TMP_Text>();
            label.text = $"{ability.abilityName} ({ability.actionCost} AP)";

            // Hook up the click
            var button = btnObj.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                controller.SetSelectedAbility(ability);

                if (NeedsTargeting(ability))
                {
                    // Guard: make sure targeter exists in scene
                    if (!TargeterController2D.Instance)
                    {
                        Debug.LogWarning("[Targeter] TargeterController2D not found in scene. " +
                                         "Place the Targeter2D prefab and assign cam/groundMask/rangeRing/circle/line.");
                        return;
                    }

                    var traceId = CombatLog.NewTraceId();
                    CombatLog.Targeting(traceId, $"Begin targeting for {ability.name} (range={ability.range}, aoe={ability.aoeRadius})", controller);

                    TargeterController2D.Instance.Begin(
                        c: controller,
                        a: ability,
                        confirm: (aimPos, aimDir) =>
                        {
                            // Cache aim and execute; AoE uses empty target array internally
                            controller.CacheAim(aimPos, aimDir);
                            controller.ExecuteAbility(ability, null, aimPos);
                        }
                    );
                }
                else
                {
                    // Single-target abilities or self buffs that don't need the targeter
                    controller.ExecuteAbility(ability, null);
                }
            });
        }

        abilityPanel.SetActive(true);
    }

    public void HideAbilities()
    {
        foreach (var b in spawnedButtons) Destroy(b);
        spawnedButtons.Clear();
        abilityPanel.SetActive(false);
    }

    private static bool NeedsTargeting(UnitAbility a)
    {
        if (a == null) return false;

        // Ground/positional or directional abilities must open the targeter.
        bool positional =
            a.groundTarget                          // drop on terrain
            || a.areaType == AreaType.Circle        // choose center
            || a.areaType == AreaType.Line;         // choose direction/line

        // Most Single-target abilities are chosen by clicking a unit in the scene,
        // not by the targeter. Keep them false here.
        return positional;
    }

}
