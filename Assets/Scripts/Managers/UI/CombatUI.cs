using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
                Debug.Log($"Selected ability: {ability.abilityName}");
                abilityPanel.SetActive(false);  // hide once chosen
                //ActionUI.Instance.ClearAction(); // clear basic action UI
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
}
