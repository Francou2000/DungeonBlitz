using System.Collections.Generic;
using UnityEngine;

public class ElementalistController : UnitController
{
    [Header("Elementalist Specific")]
    private ElementalAlignment currentAlignment = ElementalAlignment.Fire;
    private List<GameObject> activePillars = new List<GameObject>();
    private GameObject activeBonfire = null;
    private GameObject activeStormCrossing = null;
    
    private enum ElementalAlignment
    {
        Fire,
        Frost,
        Lightning
    }
    
    protected override void Start()
    {
        base.Start();
        
        // Initialize with 0 ice pillars
        model.SetRes("IcePillars", 0);
        
        // Start with Fire alignment
        currentAlignment = ElementalAlignment.Fire;
    }
    
    protected override void HandleAdrenalineStateEntered()
    {
        base.HandleAdrenalineStateEntered();
        
        // Gain magic power buff when entering adrenaline state
        var mpBuff = new StatusEffect
        {
            effectName = "Elemental Resonance",
            type = StatusEffectType.Buff,
            modifier = StatModifier.MagicPower,
            amount = 5,
            duration = -1 // Permanent while in adrenaline state
        };
        
        GetComponent<StatusEffectHandler>()?.ApplyEffect(mpBuff);
    }
    
    protected override void HandleAdrenalineStateExited()
    {
        base.HandleAdrenalineStateExited();
        
        // Remove adrenaline buffs
        GetComponent<StatusEffectHandler>()?.RemoveStatusEffectByName("Elemental Resonance");
    }
    
    protected override void OnAbilityExecuted(UnitAbility ability, Unit target)
    {
        base.OnAbilityExecuted(ability, target);
        
        switch (ability.abilityName)
        {
            case "Elementalist Staff":
                ExecuteElementalistStaff(target);
                break;
            case "Elemental Harmonization":
                ExecuteElementalHarmonization();
                break;
            case "Fire Bolt":
                ExecuteFireBolt(target);
                break;
            case "Restoration":
                ExecuteRestoration(target);
                break;
            case "Incandescent":
                ExecuteIncandescent();
                break;
            case "Ice Shard":
                ExecuteIceShard(target);
                break;
            case "Ice Pillar":
                ExecuteIcePillar(target?.transform.position ?? transform.position + Vector3.right * 3f);
                break;
            case "Lightning Bolt":
                ExecuteLightningBolt(target);
                break;
            case "Storm Crossing":
                ExecuteStormCrossing(target?.transform.position ?? transform.position + Vector3.right * 3f);
                break;
            case "Restoration Bonfire":
                ExecuteRestorationBonfire(target?.transform.position ?? transform.position + Vector3.right * 3f);
                break;
            case "Glacial Shield":
                ExecuteGlacialShield(target);
                break;
            case "Blink":
                ExecuteBlink(target?.transform.position ?? transform.position + Vector3.right * 5f);
                break;
        }
    }
    
    private void ExecuteElementalistStaff(Unit target)
    {
        if (target == null) return;
        
        var ability = GetAbilityByName("Elementalist Staff");
        if (ability == null) return;
        
        int damage = ability.CalculateTotalDamage(model, target.Model);
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
    }
    
    private void ExecuteElementalHarmonization()
    {
        // Cycle through elemental alignments
        switch (currentAlignment)
        {
            case ElementalAlignment.Fire:
                currentAlignment = ElementalAlignment.Frost;
                break;
            case ElementalAlignment.Frost:
                currentAlignment = ElementalAlignment.Lightning;
                break;
            case ElementalAlignment.Lightning:
                currentAlignment = ElementalAlignment.Fire;
                break;
        }
        
        Debug.Log($"{model.UnitName} changes elemental alignment to {currentAlignment}");
        
        // Remove previous alignment buffs and apply new ones
        RemoveAlignmentBuffs();
        ApplyAlignmentBuffs();
        
        // Gain adrenaline for harmonization
        model.AddAdrenaline(5, "elemental harmonization");
    }
    
    private void RemoveAlignmentBuffs()
    {
        var statusHandler = GetComponent<StatusEffectHandler>();
        if (statusHandler != null)
        {
            statusHandler.RemoveStatusEffectByName("Fire Alignment");
            statusHandler.RemoveStatusEffectByName("Frost Alignment");
            statusHandler.RemoveStatusEffectByName("Lightning Alignment");
        }
    }
    
    private void ApplyAlignmentBuffs()
    {
        StatusEffect alignmentBuff = null;
        
        switch (currentAlignment)
        {
            case ElementalAlignment.Fire:
                alignmentBuff = new StatusEffect
                {
                    effectName = "Fire Alignment",
                    type = StatusEffectType.Buff,
                    modifier = StatModifier.MagicPower,
                    amount = 3,
                    duration = -1,
                    tags = { "Fire", "Elemental" }
                };
                break;
                
            case ElementalAlignment.Frost:
                alignmentBuff = new StatusEffect
                {
                    effectName = "Frost Alignment",
                    type = StatusEffectType.Buff,
                    modifier = StatModifier.MagicResistance,
                    amount = 5,
                    duration = -1,
                    tags = { "Frost", "Elemental" }
                };
                break;
                
            case ElementalAlignment.Lightning:
                alignmentBuff = new StatusEffect
                {
                    effectName = "Lightning Alignment",
                    type = StatusEffectType.Buff,
                    modifier = StatModifier.Performance,
                    amount = 3,
                    duration = -1,
                    tags = { "Lightning", "Elemental" }
                };
                break;
        }
        
        if (alignmentBuff != null)
        {
            GetComponent<StatusEffectHandler>()?.ApplyEffect(alignmentBuff);
        }
    }
    
    // Fire Alignment Abilities
    private void ExecuteFireBolt(Unit target)
    {
        if (target == null || currentAlignment != ElementalAlignment.Fire) return;
        
        var ability = GetAbilityByName("Fire Bolt");
        if (ability == null) return;
        
        int damage = ability.CalculateTotalDamage(model, target.Model);
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        // Apply burning chance
        if (Random.Range(0f, 100f) <= 30f)
        {
            var burnEffect = new StatusEffect
            {
                effectName = "Fire Bolt Burn",
                type = StatusEffectType.Debuff,
                damagePerTurn = 3,
                duration = 2,
                tags = { "Burning", "Fire" }
            };
            
            target.GetComponent<StatusEffectHandler>()?.ApplyEffect(burnEffect);
        }
    }
    
    private void ExecuteRestoration(Unit target)
    {
        if (currentAlignment != ElementalAlignment.Fire) return;
        
        if (target == null || target.Faction != unit.Faction)
            target = unit; // Can heal self
        
        var ability = GetAbilityByName("Restoration");
        if (ability == null) return;
        
        int healAmount = ability.CalculateHealAmount(model);
        target.Heal(healAmount, unit);
        
        Debug.Log($"{model.UnitName} restores {healAmount} HP to {target.Model.UnitName}");
        
        // Gain adrenaline for healing
        model.AddAdrenaline(5, "healing ally");
    }
    
    private void ExecuteIncandescent()
    {
        if (currentAlignment != ElementalAlignment.Fire) return;
        
        Debug.Log($"{model.UnitName} grants Incandescent to nearby allies!");
        
        // Grant incandescent buff to two nearest allies
        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        var allies = new List<Unit>();
        
        foreach (var target in allUnits)
        {
            if (target.Faction == unit.Faction && target != unit && target.IsAlive &&
                Vector3.Distance(transform.position, target.transform.position) <= 4f)
            {
                allies.Add(target);
            }
        }
        
        // Sort by distance and take closest 2
        allies.Sort((a, b) => Vector3.Distance(transform.position, a.transform.position)
                              .CompareTo(Vector3.Distance(transform.position, b.transform.position)));
        
        int count = 0;
        foreach (var ally in allies)
        {
            if (count >= 2) break;
            
            var incandescentEffect = new StatusEffect
            {
                effectName = "Incandescent",
                type = StatusEffectType.Buff,
                modifier = StatModifier.Strength,
                amount = 5,
                duration = 3,
                tags = { "Incandescent", "Fire" }
            };
            
            ally.GetComponent<StatusEffectHandler>()?.ApplyEffect(incandescentEffect);
            Debug.Log($"{ally.Model.UnitName} gains Incandescent!");
            
            count++;
        }
        
        // Gain adrenaline for supporting allies
        model.AddAdrenaline(8, "granting Incandescent");
    }
    
    // Frost Alignment Abilities
    private void ExecuteIceShard(Unit target)
    {
        if (target == null || currentAlignment != ElementalAlignment.Frost) return;
        
        var ability = GetAbilityByName("Ice Shard");
        if (ability == null) return;
        
        int damage = ability.CalculateTotalDamage(model, target.Model);
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        // Apply slow chance
        if (Random.Range(0f, 100f) <= 40f)
        {
            var slowEffect = new StatusEffect
            {
                effectName = "Ice Slow",
                type = StatusEffectType.Debuff,
                modifier = StatModifier.Performance,
                amount = -3,
                duration = 2,
                tags = { "Slowed", "Frost" }
            };
            
            target.GetComponent<StatusEffectHandler>()?.ApplyEffect(slowEffect);
        }
    }
    
    private void ExecuteIcePillar(Vector3 position)
    {
        if (currentAlignment != ElementalAlignment.Frost) return;
        
        int currentPillars = model.GetRes("IcePillars");
        if (currentPillars >= 2)
        {
            Debug.Log($"{model.UnitName} cannot create more Ice Pillars (max 2)!");
            return;
        }
        
        Debug.Log($"{model.UnitName} creates an Ice Pillar at {position}!");
        model.AddRes("IcePillars", 1);
        
        // Apply frost effects to nearby enemies
        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var target in allUnits)
        {
            if (target.Faction != unit.Faction && 
                Vector3.Distance(position, target.transform.position) <= 2f)
            {
                var frostEffect = new StatusEffect
                {
                    effectName = "Pillar Frost",
                    type = StatusEffectType.Debuff,
                    modifier = StatModifier.Performance,
                    amount = -5,
                    duration = 3,
                    tags = { "Frost", "IcePillar" }
                };
                
                target.GetComponent<StatusEffectHandler>()?.ApplyEffect(frostEffect);
            }
        }
        
        // TODO: Create actual pillar GameObject in full implementation
        Debug.Log($"Active Ice Pillars: {model.GetRes("IcePillars")}/2");
    }
    
    // Lightning Alignment Abilities
    private void ExecuteLightningBolt(Unit target)
    {
        if (target == null || currentAlignment != ElementalAlignment.Lightning) return;
        
        var ability = GetAbilityByName("Lightning Bolt");
        if (ability == null) return;
        
        int damage = ability.CalculateTotalDamage(model, target.Model);
        target.Model.ApplyDamageWithBarrier(damage, ability.GetDamageType());
        
        // Apply shock chance
        if (Random.Range(0f, 100f) <= 25f)
        {
            var shockEffect = new StatusEffect
            {
                effectName = "Lightning Shock",
                type = StatusEffectType.Debuff,
                modifier = StatModifier.Performance,
                amount = -6,
                duration = 1,
                tags = { "Shocked", "Lightning" }
            };
            
            target.GetComponent<StatusEffectHandler>()?.ApplyEffect(shockEffect);
        }
    }
    
    private void ExecuteStormCrossing(Vector3 position)
    {
        if (currentAlignment != ElementalAlignment.Lightning) return;
        
        Debug.Log($"{model.UnitName} creates Storm Crossing at {position}!");
        
        // Apply effects to units on the line
        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var target in allUnits)
        {
            if (Vector3.Distance(position, target.transform.position) <= 1f)
            {
                if (target.Faction == unit.Faction)
                {
                    // Grant haste to allies
                    var hasteEffect = new StatusEffect
                    {
                        effectName = "Storm Haste",
                        type = StatusEffectType.Buff,
                        modifier = StatModifier.Performance,
                        amount = 6,
                        duration = 2,
                        tags = { "Haste", "Lightning" }
                    };
                    
                    target.GetComponent<StatusEffectHandler>()?.ApplyEffect(hasteEffect);
                }
                else
                {
                    // Damage and potentially shock enemies
                    int damage = 8 + model.MagicPower;
                    target.Model.ApplyDamageWithBarrier(damage, DamageType.Magical);
                    
                    if (Random.Range(0f, 100f) <= 30f)
                    {
                        var shockEffect = new StatusEffect
                        {
                            effectName = "Storm Shocked",
                            type = StatusEffectType.Debuff,
                            modifier = StatModifier.Performance,
                            amount = -8,
                            duration = 1,
                            tags = { "Shocked", "Lightning" }
                        };
                        
                        target.GetComponent<StatusEffectHandler>()?.ApplyEffect(shockEffect);
                    }
                }
            }
        }
        
        // TODO: Create actual storm crossing GameObject in full implementation
    }
    
    // Adrenaline Abilities
    private void ExecuteRestorationBonfire(Vector3 position)
    {
        if (!model.IsInAdrenalineState || currentAlignment != ElementalAlignment.Fire) return;
        
        Debug.Log($"{model.UnitName} creates a Restoration Bonfire at {position}!");
        
        // Heal all allies in area immediately
        var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var target in allUnits)
        {
            if (target.Faction == unit.Faction && 
                Vector3.Distance(position, target.transform.position) <= 2f)
            {
                int healAmount = 10 + Mathf.RoundToInt(model.MagicPower * 0.3f);
                target.Heal(healAmount, unit);
            }
        }
        
        // TODO: Create persistent bonfire that heals each turn
    }
    
    private void ExecuteGlacialShield(Unit target)
    {
        if (!model.IsInAdrenalineState || currentAlignment != ElementalAlignment.Frost) return;
        
        if (target == null || target.Faction != unit.Faction)
            target = unit;
        
        int barrierAmount = 20 + Mathf.RoundToInt(model.MagicPower * 0.4f);
        
        var barrier = new StatusEffect
        {
            effectName = "Glacial Barrier",
            type = StatusEffectType.Buff,
            barrierHP = barrierAmount,
            duration = 5,
            tags = { "Barrier", "Frost" }
        };
        
        target.GetComponent<StatusEffectHandler>()?.ApplyEffect(barrier);
        
        Debug.Log($"{model.UnitName} grants {barrierAmount} Glacial Shield to {target.Model.UnitName}");
    }
    
    private void ExecuteBlink(Vector3 targetPosition)
    {
        if (!model.IsInAdrenalineState || currentAlignment != ElementalAlignment.Lightning) return;
        
        Debug.Log($"{model.UnitName} blinks to {targetPosition}!");
        
        // Teleport to target position
        transform.position = targetPosition;
        
        // Gain temporary speed boost
        var speedBuff = new StatusEffect
        {
            effectName = "Blink Speed",
            type = StatusEffectType.Buff,
            modifier = StatModifier.Performance,
            amount = 8,
            duration = 1,
            tags = { "Speed", "Lightning" }
        };
        
        GetComponent<StatusEffectHandler>()?.ApplyEffect(speedBuff);
    }
    
    public override void OnDamageTaken(int damage, DamageType damageType, Unit attacker)
    {
        base.OnDamageTaken(damage, damageType, attacker);
        
        // Gain adrenaline when taking damage
        model.AddAdrenaline(3, "taking damage");
        
        // Elemental feedback based on alignment
        switch (currentAlignment)
        {
            case ElementalAlignment.Fire:
                // Chance to burn attacker
                if (attacker != null && Random.Range(0f, 100f) <= 20f)
                {
                    var burnEffect = new StatusEffect
                    {
                        effectName = "Fire Feedback",
                        type = StatusEffectType.Debuff,
                        damagePerTurn = 3,
                        duration = 2,
                        tags = { "Burning", "Feedback" }
                    };
                    
                    attacker.GetComponent<StatusEffectHandler>()?.ApplyEffect(burnEffect);
                    Debug.Log($"{attacker.Model.UnitName} is burned by fire feedback!");
                }
                break;
                
            case ElementalAlignment.Frost:
                // Chance to slow attacker
                if (attacker != null && Random.Range(0f, 100f) <= 25f)
                {
                    var slowEffect = new StatusEffect
                    {
                        effectName = "Frost Feedback",
                        type = StatusEffectType.Debuff,
                        modifier = StatModifier.Performance,
                        amount = -4,
                        duration = 2,
                        tags = { "Slowed", "Feedback" }
                    };
                    
                    attacker.GetComponent<StatusEffectHandler>()?.ApplyEffect(slowEffect);
                    Debug.Log($"{attacker.Model.UnitName} is slowed by frost feedback!");
                }
                break;
                
            case ElementalAlignment.Lightning:
                // Chance to shock attacker
                if (attacker != null && Random.Range(0f, 100f) <= 30f)
                {
                    var shockEffect = new StatusEffect
                    {
                        effectName = "Lightning Feedback",
                        type = StatusEffectType.Debuff,
                        modifier = StatModifier.Performance,
                        amount = -6,
                        duration = 1,
                        tags = { "Shocked", "Feedback" }
                    };
                    
                    attacker.GetComponent<StatusEffectHandler>()?.ApplyEffect(shockEffect);
                    Debug.Log($"{attacker.Model.UnitName} is shocked by lightning feedback!");
                }
                break;
        }
    }
    
    public override void OnTurnStart()
    {
        base.OnTurnStart();
        
        // Apply alignment buff if not present
        ApplyAlignmentBuffs();
    }
    
    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        
        // Gain adrenaline for supporting allies
        var supportAbilitiesUsed = model.MaxActions - model.CurrentActions;
        if (supportAbilitiesUsed > 0)
        {
            model.AddAdrenaline(supportAbilitiesUsed * 2, "supporting team");
        }
    }
    
    // Clean up resources when unit dies
    void OnDestroy()
    {
        // Clean up created structures
        if (activeBonfire != null)
        {
            Destroy(activeBonfire);
        }
        
        if (activeStormCrossing != null)
        {
            Destroy(activeStormCrossing);
        }
        
        foreach (var pillar in activePillars)
        {
            if (pillar != null)
            {
                Destroy(pillar);
            }
        }
        
        activePillars.Clear();
    }
}