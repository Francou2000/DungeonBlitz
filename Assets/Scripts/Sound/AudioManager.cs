using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Mixer & Sources")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource; // Mantener para compatibilidad, pero usar pool para nuevos sonidos
    
    [Header("SFX Pool Settings")]
    [SerializeField] private int sfxPoolSize = 8; // Número de AudioSources en el pool
    
    private Queue<AudioSource> sfxPool = new Queue<AudioSource>();
    private List<AudioSource> activeSfxSources = new List<AudioSource>();
    private float currentSFXVolume = 1f; // Guardar el volumen actual de SFX para aplicarlo a nuevos AudioSources

    //Rename to whatever (this manager was used on other projects)
    [Header("Audio Clips")]
    [SerializeField] private AudioClip backgroundMusicMenu;
    [SerializeField] private AudioClip backgroundMusicCombat;
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private AudioClip startGameSFX;

    [Header("Attack SFX")]
    [SerializeField] private AudioClip stabSFX;
    [SerializeField] private AudioClip goblinAttackSFX;
    
    [Header("Hurt SFX")]
    [SerializeField] private AudioClip goblinHurtSFX;
    [SerializeField] private AudioClip maleHurtSFX;
    [SerializeField] private AudioClip FemaleHurtSFX;
    [SerializeField] private AudioClip HobGoblinHurtSFX;
    
    [Header("Evade SFX")]
    [SerializeField] private AudioClip evadeMaleSFX;
    [SerializeField] private AudioClip evadeRogueSFX;
    [SerializeField] private AudioClip evadeGoblinSFX;
    [SerializeField] private AudioClip evadeHobGoblinSFX;



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PlayMusic();
        foreach (var elem in soundDictElements)
        {
            soundDict[elem._key] = elem._value;
        }
        if (soundDictElements.Count == soundDict.Count) Debug.Log("Diccionario de sonidos completo");
        else Debug.Log("Diccionario de sonidos incompleto  ->  Alguna key est� repetida");

        // Inicializar pool de AudioSources para SFX simultáneos
        InitializeSFXPool();
    }

    private void InitializeSFXPool()
    {
        // Crear un objeto padre para organizar los AudioSources del pool
        GameObject poolParent = new GameObject("SFX Pool");
        poolParent.transform.SetParent(transform);
        
        // Obtener el volumen inicial del sfxSource si existe
        if (sfxSource != null)
        {
            currentSFXVolume = sfxSource.volume;
        }
        
        // Crear AudioSources en el pool
        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject sourceObj = new GameObject($"SFX Source {i}");
            sourceObj.transform.SetParent(poolParent.transform);
            AudioSource source = sourceObj.AddComponent<AudioSource>();
            
            // Copiar configuración del sfxSource original si existe
            if (sfxSource != null)
            {
                source.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
                source.volume = currentSFXVolume;
                source.pitch = sfxSource.pitch;
                source.playOnAwake = false;
            }
            
            sfxPool.Enqueue(source);
        }
    }

    // Obtener un AudioSource disponible del pool
    private AudioSource GetAvailableSFXSource()
    {
        // Limpiar AudioSources que ya terminaron de reproducir
        CleanupFinishedSFXSources();
        
        // Si hay AudioSources disponibles en el pool, usarlos
        if (sfxPool.Count > 0)
        {
            AudioSource source = sfxPool.Dequeue();
            activeSfxSources.Add(source);
            return source;
        }
        
        // Si no hay disponibles, crear uno nuevo temporalmente
        GameObject tempObj = new GameObject("Temp SFX Source");
        tempObj.transform.SetParent(transform);
        AudioSource tempSource = tempObj.AddComponent<AudioSource>();
        if (sfxSource != null)
        {
            tempSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
            tempSource.volume = currentSFXVolume; // Usar el volumen actual guardado
            tempSource.pitch = sfxSource.pitch;
            tempSource.playOnAwake = false;
        }
        activeSfxSources.Add(tempSource);
        return tempSource;
    }

    // Devolver AudioSource al pool cuando termine de reproducir
    private void ReturnSFXSourceToPool(AudioSource source)
    {
        if (source == null) return;
        
        if (activeSfxSources.Contains(source))
        {
            activeSfxSources.Remove(source);
        }
        
        // Si es un objeto temporal, destruirlo; si no, devolverlo al pool
        if (source.gameObject.name.Contains("Temp"))
        {
            Destroy(source.gameObject);
        }
        else
        {
            source.Stop();
            sfxPool.Enqueue(source);
        }
    }

    // Limpiar AudioSources que ya terminaron de reproducir
    private void CleanupFinishedSFXSources()
    {
        for (int i = activeSfxSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeSfxSources[i];
            if (source == null || !source.isPlaying)
            {
                ReturnSFXSourceToPool(source);
            }
        }
    }

    // Reproducir sonido usando el pool (reemplaza las llamadas a sfxSource.PlayOneShot)
    private void PlaySFXFromPool(AudioClip clip)
    {
        if (clip == null) return;
        
        AudioSource source = GetAvailableSFXSource();
        if (source != null)
        {
            source.PlayOneShot(clip);
            // Usar coroutine para devolver el source al pool cuando termine
            // Agregar un pequeño buffer para asegurar que el sonido termine completamente
            StartCoroutine(ReturnSourceWhenFinished(source, clip.length + 0.1f));
        }
    }

    private System.Collections.IEnumerator ReturnSourceWhenFinished(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        // Verificar que realmente terminó antes de devolverlo
        if (source != null && !source.isPlaying)
        {
            ReturnSFXSourceToPool(source);
        }
        else if (source != null)
        {
            // Si aún está reproduciendo, esperar un poco más
            yield return new WaitForSeconds(0.1f);
            ReturnSFXSourceToPool(source);
        }
    }

    public void PlayMusic()
    {
        if (musicSource && backgroundMusicMenu)
        {
            musicSource.clip = backgroundMusicMenu;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void PlayCombatMusic()
    {
        musicSource.Stop();
        if (musicSource && backgroundMusicCombat)
        {
            musicSource.clip = backgroundMusicCombat;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    //Rename to whatever (this manager was used on other projects)
    public void PlayStartGame()
    {
        if (startGameSFX) PlaySFXFromPool(startGameSFX);
        PlayCombatMusic();
        Debug.Log("Playing combat music");
    }

    //Rename to whatever (this manager was used on other projects)
    public void PlayButtonSound()
    {
        if (buttonClickSFX) PlaySFXFromPool(buttonClickSFX);
    }

    // Volume Controls
    public void SetMasterVolume(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0f, 1f);
        
        // Actualizar música
        if (musicSource != null)
            musicSource.volume = clampedValue;
        
        // Actualizar SFX original
        if (sfxSource != null)
            sfxSource.volume = clampedValue;
        
        // Actualizar volumen de todos los AudioSources del pool de SFX
        currentSFXVolume = clampedValue;
        UpdateAllSFXSourcesVolume(clampedValue);
    }

    public void SetMusicVolume(float value)
    {
        if (musicSource != null)
            musicSource.volume = Mathf.Clamp(value, 0f, 1f);
    }

    public void SetSFXVolume(float value)
    {
        float clampedVolume = Mathf.Clamp(value, 0f, 1f);
        currentSFXVolume = clampedVolume; // Guardar el volumen actual
        
        // Actualizar volumen del sfxSource original
        if (sfxSource != null)
            sfxSource.volume = clampedVolume;
        
        // Actualizar volumen de todos los AudioSources del pool
        UpdateAllSFXSourcesVolume(clampedVolume);
    }

    // Helper para actualizar el volumen de todos los AudioSources de SFX
    private void UpdateAllSFXSourcesVolume(float volume)
    {
        // Actualizar AudioSources en el pool
        foreach (AudioSource source in sfxPool)
        {
            if (source != null) source.volume = volume;
        }
        
        // Actualizar AudioSources activos
        foreach (AudioSource source in activeSfxSources)
        {
            if (source != null) source.volume = volume;
        }
    }

    //testing this may change later
    // ATTACK SFX
    public void PlayStabSound(){
        if (stabSFX) PlaySFXFromPool(stabSFX);
    }

    public void PlayAttackSound(string abilityName)
    {
        if (abilityName == null)
        {
            Debug.LogWarning("PlayAttackSound called with null abilityName");
            return;
        }

        switch (abilityName)
        {
            // Goblin attacks
            case "Goblin Slash":
                PlaySFX(SoundName.StabSFX);
                break;
            case "Goblin Flurry":
                PlaySFX(SoundName.StabSFX);
                break;
            case "Goblin Staff":
                PlaySFX(SoundName.GoblinAttack);
                break;

            // Hobgoblin attacks
            case "Spear Throw":
                PlaySFX(SoundName.Arrow);
                break;

            // Paladin attacks
            case "Justice Strike":
                PlaySFX(SoundName.StabSFX);
                break;
            case "Shield Bash":
                PlaySFX(SoundName.MetalImpact);
                break;
            case "Divine Smite":
                PlaySFX(SoundName.Electrical);
                break;
            case "Healing Prayer":
                PlaySFX(SoundName.EvadeMale);
                break;

            // Rogue attacks
            case "Focus Shot":
            case "Piercing Shot":
            case "Shadow Arrows":
                PlaySFX(SoundName.Arrow);
                break;
            case "Swift Dagger":
            case "Shadow Blade":
            case "Blade Barrage":
                PlaySFX(SoundName.StabSFX);
                break;

            // Shaman attacks
            case "Fire Bolt":
                PlaySFX(SoundName.Magic1);
                break;
            case "Lightning Bolt":
                PlaySFX(SoundName.Electrical);
                break;

            // Sorcerer attacks
            case "Fire Bolt+":
                PlaySFX(SoundName.Magic1);
                break;
            case "Lightning Bolt+":
                PlaySFX(SoundName.Electrical);
                break;
            case "Ice Shard +":
                PlaySFX(SoundName.Magic4);
                break;
            case "Thunder Strike":
                PlaySFX(SoundName.Electrical);
                break;

            // Champion attacks
            case "Champion Slash":
                PlaySFX(SoundName.GoblinAttack);
                break;
            case "Apprehend":
                PlaySFX(SoundName.GoblinAttack);
                break;
            case "Decimate":
                PlaySFX(SoundName.StabSFX);
                break;
            case "Bite":
                PlaySFX(SoundName.StabSFX);
                break;
            case "Champion Rage":
            case "Face Me!":
                PlaySFX(SoundName.WarCry);
                break;

            // Elementalist attacks
            case "Elementalist Staff":
                PlaySFX(SoundName.GoblinAttack);
                break;
            case "Ice Shard":
                PlaySFX(SoundName.Magic4);
                break;
            case "Storm Crossing":
                PlaySFX(SoundName.Electrical);
                break;

            // Hobgoblin abilities
            case "Replenish Spears":
                PlaySFX(SoundName.WarCry);
                break;

            // Paladin abilities
            case "Smite":
                PlaySFX(SoundName.StabSFX);
                break;

            // Sorcerer abilities
            case "Fireball":
                PlaySFX(SoundName.Magic5);
                break;

            // No sound abilities
            case "Harness Power":
            case "Restoration Bonfire":
            case "Restoration":
                // No sound for these
                break;

            default:
                Debug.LogWarning($"[AudioManager] No sound configured for ability: {abilityName}");
                break;
        }
    }
    
    public void PlayGoblinAttack(){
        if (goblinAttackSFX) PlaySFXFromPool(goblinAttackSFX);
    }


    //EVADE SFX
    public void PlayEvadeMale(){
        if (evadeMaleSFX) PlaySFXFromPool(evadeMaleSFX);
    }

    public void PlayEvadeRogue(){
        if (evadeRogueSFX) PlaySFXFromPool(evadeRogueSFX);
    }

    public void PlayEvadeGoblin(){
        if (evadeGoblinSFX) PlaySFXFromPool(evadeGoblinSFX);
    }

    public void PlayEvadeHobgoblin(){
        if (evadeHobGoblinSFX) PlaySFXFromPool(evadeHobGoblinSFX);
    }


    //HURT SFX
    public void PlayHurtGoblin(){
        if (goblinHurtSFX) PlaySFXFromPool(goblinHurtSFX);
    }

    public void PlayHurtHobGoblin(){
        if (HobGoblinHurtSFX) PlaySFXFromPool(HobGoblinHurtSFX);
    }

    public void PlayHurtMale(){
        if (maleHurtSFX) PlaySFXFromPool(maleHurtSFX);
    }

    public void PlayHurtFemale(){
        if (FemaleHurtSFX) PlaySFXFromPool(FemaleHurtSFX);
    }

    // Helper function to play evade sound based on unit type
    public void PlayEvadeSoundByUnitType(Unit unit)
    {
        if (unit == null) return;
        
        // Get unit name to determine type
        string unitName = unit.name.ToLower();
        
        if (unitName.Contains("goblin") && !unitName.Contains("hob"))
        {
            PlayEvadeGoblin();
        }
        else if (unitName.Contains("hobgoblin") || unitName.Contains("hob"))
        {
            PlayEvadeHobgoblin();
        }
        else if (unitName.Contains("rogue") || unitName.Contains("elementalist"))
        {
            PlayEvadeRogue();
        }
        else // Default to male for other units
        {
            PlayEvadeMale();
        }
    }

    // Helper function to play hurt sound based on unit type
    public void PlayHurtSoundByUnitType(Unit unit)
    {
        if (unit == null) return;
        
        // Get unit name to determine type
        string unitName = unit.name.ToLower();
        
        if (unitName.Contains("goblin") && !unitName.Contains("hob"))
        {
            PlayHurtGoblin();
        }
        else if (unitName.Contains("hobgoblin") || unitName.Contains("hob"))
        {
            PlayHurtHobGoblin();
        }
        else if (unitName.Contains("shaman") || unitName.Contains("elementalist") || unitName.Contains("sorcerer"))
        {
            PlayHurtFemale();
        }
        else // Default to male for other units (paladin, rogue, etc.)
        {
            PlayHurtMale();
        }
    }

    public List<DictionaryElement<SoundName, AudioClip>> soundDictElements = new List<DictionaryElement<SoundName, AudioClip>>();
    Dictionary<SoundName, AudioClip> soundDict = new Dictionary<SoundName, AudioClip>();

    public void PlaySFX(SoundName s_name)
    {
        if (soundDict.ContainsKey(s_name) && soundDict[s_name] != null)
        {
            // Usar pool para reproducir múltiples sonidos simultáneamente
            PlaySFXFromPool(soundDict[s_name]);
        }
    }

    public void PlayBGM(SoundName s_name)
    {
        if (musicSource && soundDict[s_name]) musicSource.PlayOneShot(soundDict[s_name]);
    }

}

//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------

[Serializable]
public struct DictionaryElement<T, K>
{
    public T _key;
    public K _value;
}

public enum SoundName
{
    NONE,
    BackgroundMainMenu,
    BackgroundCombat,
    ButtonClick,
    StartGame, 
    StabSFX,
    GoblinAttack,
    HurtGoblin,
    HurtHobGoblin,
    HurtMale,
    HurtFemale,
    EvadeMale,
    EvadeRogue,
    EvadeGoblin,
    EvadeHobGoblin,
    Magic1,
    Magic2,
    Magic3,
    Magic4,
    Magic5,
    WarCry,
    Arrow,
    Electrical,
    MetalImpact,
    NextTurn
}