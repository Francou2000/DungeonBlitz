using UnityEngine;

public class CombatUI : MonoBehaviour
{
    public UnitController activeUnit;

    public void OnAbilityButtonPressed(int index)
    {
        var abilities = activeUnit.unit.Model.Abilities;
        if (index < abilities.Count)
        {
            activeUnit.SetSelectedAbility(abilities[index]);
            ActionUI.Instance.SetAction(UnitAction.Attack); // activate attack mode
        }
    }
}
