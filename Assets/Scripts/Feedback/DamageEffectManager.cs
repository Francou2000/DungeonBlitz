using UnityEngine;
using Photon.Pun;

public class DamageEffectManager : MonoBehaviourPunCallbacks
{
    public static DamageEffectManager Instance { get; private set; }

    [Header("Animation Prefabs")]
    [SerializeField] private GameObject basicMeleeEffectPrefab;
    [SerializeField] private GameObject basicRangedEffectPrefab;
    [SerializeField] private GameObject biteEffectPrefab;
    [SerializeField] private GameObject clawEffectPrefab;
    
    [Header("New VFX Prefabs")]
    [SerializeField] private GameObject arrowEffectPrefab;
    [SerializeField] private GameObject electricBoltEffectPrefab;
    [SerializeField] private GameObject fireBoltEffectPrefab;
    [SerializeField] private GameObject iceBoltEffectPrefab;
    [SerializeField] private GameObject healingEffectPrefab;
    [SerializeField] private GameObject iceShardEffectPrefab;
    [SerializeField] private GameObject javelineEffectPrefab;
    [SerializeField] private GameObject smiteEffectPrefab;
    [SerializeField] private GameObject thunderStrikeEffectPrefab;

    [Header("Effect Settings")]
    [SerializeField] private float effectOffset = 1.5f; // Distancia desde el centro de la unidad
    [SerializeField] private float effectDuration = 1.0f; // Duración de la animación
    [SerializeField] private float effectScale = 1.0f; // Escala del efecto

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Cargar los prefabs de efectos desde Resources si no están asignados
        LoadEffectPrefabs();
    }

    private void LoadEffectPrefabs()
    {
        if (basicMeleeEffectPrefab == null)
            basicMeleeEffectPrefab = Resources.Load<GameObject>("HitEffects/BasicMelee");
        
        if (basicRangedEffectPrefab == null)
            basicRangedEffectPrefab = Resources.Load<GameObject>("HitEffects/BasicRanged");
        
        if (biteEffectPrefab == null)
            biteEffectPrefab = Resources.Load<GameObject>("HitEffects/Bite");
        
        if (clawEffectPrefab == null)
            clawEffectPrefab = Resources.Load<GameObject>("HitEffects/Claw");
        
        // Cargar nuevos prefabs de VFX
        if (arrowEffectPrefab == null)
            arrowEffectPrefab = Resources.Load<GameObject>("HitEffects/Arrow");
        
        if (electricBoltEffectPrefab == null)
            electricBoltEffectPrefab = Resources.Load<GameObject>("HitEffects/Electric Bolt");
        
        if (fireBoltEffectPrefab == null)
            fireBoltEffectPrefab = Resources.Load<GameObject>("HitEffects/Fire Bolt");
        
        if (iceBoltEffectPrefab == null)
            iceBoltEffectPrefab = Resources.Load<GameObject>("HitEffects/Ice Bolt");
        
        if (healingEffectPrefab == null)
            healingEffectPrefab = Resources.Load<GameObject>("HitEffects/Healing");
        
        if (iceShardEffectPrefab == null)
            iceShardEffectPrefab = Resources.Load<GameObject>("HitEffects/Ice Shard");
        
        if (javelineEffectPrefab == null)
            javelineEffectPrefab = Resources.Load<GameObject>("HitEffects/Javeline");
        
        if (smiteEffectPrefab == null)
            smiteEffectPrefab = Resources.Load<GameObject>("HitEffects/Smite");
        
        if (thunderStrikeEffectPrefab == null)
            thunderStrikeEffectPrefab = Resources.Load<GameObject>("HitEffects/ThunderStrike");
    }

    /// <summary>
    /// Muestra un efecto de daño en la unidad objetivo
    /// </summary>
    /// <param name="target">Unidad que recibe el daño</param>
    /// <param name="attacker">Unidad que causa el daño</param>
    /// <param name="ability">Habilidad utilizada</param>
    /// <param name="damageAmount">Cantidad de daño</param>
    public void ShowDamageEffect(Unit target, Unit attacker, UnitAbility ability, int damageAmount)
    {
        if (target == null) return;

        Debug.Log($"[DamageEffectManager] ShowDamageEffect called - Target: {target.name}, Attacker: {attacker?.name}, Ability: {ability?.abilityName}, Damage: {damageAmount}, Range: {ability?.range}");

        // Determinar el tipo de efecto basándose en la habilidad y el atacante
        GameObject effectPrefab = GetEffectPrefab(ability, attacker);
        
        if (effectPrefab == null)
        {
            Debug.LogWarning($"[DamageEffectManager] No effect prefab found for ability: {ability?.abilityName}");
            return;
        }

        Debug.Log($"[DamageEffectManager] Using effect prefab: {effectPrefab.name}");

        // Calcular la posición del efecto
        Vector3 effectPosition = CalculateEffectPosition(target, attacker);

        Debug.Log($"[DamageEffectManager] Effect position: {effectPosition}");

        // Crear el efecto
        CreateDamageEffect(effectPrefab, effectPosition, target.transform, attacker);
    }

    private GameObject GetEffectPrefab(UnitAbility ability, Unit attacker)
    {
        if (ability == null) return basicMeleeEffectPrefab;

        string abilityName = ability.abilityName.ToLower();

        // Mapear habilidades específicas por nombre
        switch (abilityName)
        {
            // Goblin abilities
            case "goblin slash":
                return basicMeleeEffectPrefab;
            case "goblin flurry":
                return clawEffectPrefab;
            case "goblin staff":
                return basicMeleeEffectPrefab;

            // Hobgoblin abilities
            case "spear throw":
                return javelineEffectPrefab ?? basicRangedEffectPrefab;
            case "replenish spears":
                // No effect for replenish spears
                return null;

            // Paladin abilities
            case "smite":
                return smiteEffectPrefab ?? basicMeleeEffectPrefab;
            case "justice strike":
                return basicMeleeEffectPrefab;
            case "shield bash":
                return basicMeleeEffectPrefab;
            case "divine smite":
                return smiteEffectPrefab ?? basicMeleeEffectPrefab;
            case "healing prayer":
                return healingEffectPrefab;

            // Rogue abilities
            case "focus shot":
            case "piercing shot":
            case "shadow arrows":
                return arrowEffectPrefab ?? basicRangedEffectPrefab;
            case "swift dagger":
            case "shadow blade":
            case "blade barrage":
                return basicMeleeEffectPrefab;

            // Shaman abilities
            case "fire bolt":
                return fireBoltEffectPrefab ?? basicRangedEffectPrefab;
            case "lightning bolt":
                return electricBoltEffectPrefab ?? basicRangedEffectPrefab;

            // Sorcerer abilities
            case "fire bolt+":
                return fireBoltEffectPrefab ?? basicRangedEffectPrefab;
            case "lightning bolt+":
                return electricBoltEffectPrefab ?? basicRangedEffectPrefab;
            case "ice shard +":
                return iceShardEffectPrefab ?? basicRangedEffectPrefab;
            case "thunder strike":
                return thunderStrikeEffectPrefab ?? basicRangedEffectPrefab;
            case "fireball":
                return basicRangedEffectPrefab;
            case "harness power":
                // No effect for harness power
                return null;
            case "restoration bonfire":
                // No effect for restoration bonfire
                return null;

            // Champion abilities
            case "champion slash":
                return clawEffectPrefab;
            case "apprehend":
                return basicMeleeEffectPrefab;
            case "decimate":
                return clawEffectPrefab;
            case "bite":
                return biteEffectPrefab;
            case "champion rage":
            case "face me!":
                // No effect for these
                return null;

            // Elementalist abilities
            case "elementalist staff":
                return basicMeleeEffectPrefab;
            case "ice shard":
                return iceShardEffectPrefab ?? basicRangedEffectPrefab;
            case "restoration":
                return healingEffectPrefab;
            case "storm crossing":
                return electricBoltEffectPrefab ?? basicRangedEffectPrefab;

            default:
                // Default fallback based on range
                if (ability.range <= 2)
                    return basicMeleeEffectPrefab;
                else
                    return basicRangedEffectPrefab;
        }
    }

    private Vector3 CalculateEffectPosition(Unit target, Unit attacker)
    {
        Vector3 targetPosition = target.transform.position;
        
        if (attacker == null)
        {
            // Si no hay atacante, mostrar el efecto arriba de la unidad
            return targetPosition + Vector3.up * effectOffset;
        }

        // Calcular la dirección del ataque
        Vector3 attackDirection = (targetPosition - attacker.transform.position).normalized;
        
        // Determinar el offset basándose en la dirección del ataque
        Vector3 effectOffsetVector;
        
        if (Mathf.Abs(attackDirection.x) > Mathf.Abs(attackDirection.y))
        {
            // Ataque horizontal (izquierda o derecha)
            if (attackDirection.x > 0)
            {
                // Atacante a la izquierda, efecto a la derecha del objetivo
                effectOffsetVector = Vector3.right * effectOffset;
            }
            else
            {
                // Atacante a la derecha, efecto a la izquierda del objetivo
                effectOffsetVector = Vector3.left * effectOffset;
            }
        }
        else
        {
            // Ataque vertical, mostrar arriba
            effectOffsetVector = Vector3.up * effectOffset;
        }

        return targetPosition + effectOffsetVector;
    }

    private void CreateDamageEffect(GameObject effectPrefab, Vector3 position, Transform targetTransform, Unit attacker)
    {
        Debug.Log($"[DamageEffectManager] Creating damage effect at position: {position}");
        
        // Crear el efecto
        GameObject effectInstance = Instantiate(effectPrefab, position, Quaternion.identity);
        
        Debug.Log($"[DamageEffectManager] Effect instance created: {effectInstance.name}");
        
        // Configurar la escala
        effectInstance.transform.localScale = Vector3.one * effectScale;
        
        // Orientar el efecto hacia el atacante si existe
        if (attacker != null)
        {
            Vector3 directionToAttacker = (attacker.transform.position - position).normalized;
            
            // Solo voltear horizontalmente según el lado del atacante
            if (directionToAttacker.x > 0)
            {
                // Atacante a la derecha, voltear el efecto hacia la derecha
                effectInstance.transform.localScale = new Vector3(effectScale, effectScale, effectScale);
            }
            else
            {
                // Atacante a la izquierda, voltear el efecto hacia la izquierda
                effectInstance.transform.localScale = new Vector3(-effectScale, effectScale, effectScale);
            }
        }
        else
        {
            // Sin atacante, usar escala normal
            effectInstance.transform.localScale = Vector3.one * effectScale;
        }

        // Configurar el componente de animación si existe
        Animator effectAnimator = effectInstance.GetComponent<Animator>();
        if (effectAnimator != null)
        {
            Debug.Log($"[DamageEffectManager] Animator found, triggering animation");
            // Reproducir la animación
            effectAnimator.SetTrigger("Play");
        }
        else
        {
            Debug.LogWarning($"[DamageEffectManager] No Animator found on effect prefab");
        }

        // Destruir el efecto después de la duración
        Destroy(effectInstance, effectDuration);
        
        Debug.Log($"[DamageEffectManager] Effect will be destroyed in {effectDuration} seconds");
    }

    /// <summary>
    /// Método estático para mostrar efectos de daño desde otros scripts
    /// </summary>
    public static void ShowDamageEffectStatic(Unit target, Unit attacker, UnitAbility ability, int damageAmount)
    {
        if (Instance != null)
        {
            Instance.ShowDamageEffect(target, attacker, ability, damageAmount);
        }
    }
}
