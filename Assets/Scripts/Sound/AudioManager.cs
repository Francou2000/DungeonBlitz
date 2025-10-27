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
    [SerializeField] private AudioSource sfxSource;

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
        if (sfxSource && startGameSFX)
            sfxSource.PlayOneShot(startGameSFX);
            PlayCombatMusic();
            Debug.Log("Playing combat music");
        
    }

    //Rename to whatever (this manager was used on other projects)
    public void PlayButtonSound()
    {
        if (sfxSource && buttonClickSFX)
            sfxSource.PlayOneShot(buttonClickSFX);
    }

    // Volume Controls
    public void SetMasterVolume(float value)
    {
        // Set master volume on both sources
        if (musicSource != null)
            musicSource.volume = Mathf.Clamp(value, 0f, 1f);
        if (sfxSource != null)
            sfxSource.volume = Mathf.Clamp(value, 0f, 1f);
    }

    public void SetMusicVolume(float value)
    {
        if (musicSource != null)
            musicSource.volume = Mathf.Clamp(value, 0f, 1f);
    }

    public void SetSFXVolume(float value)
    {
        if (sfxSource != null)
            sfxSource.volume = Mathf.Clamp(value, 0f, 1f);
    }

    //testing this may change later
    // ATTACK SFX
    public void PlayStabSound(){
        if (sfxSource && stabSFX){
            sfxSource.PlayOneShot(stabSFX);
        }
    }

    public void PlayAttackSound(string abilityName)
    {
        Debug.LogWarning(abilityName + "FALTA AGREGAR SONIDO - wazel :)");
        switch (abilityName)
        {
            case "stab":
                PlayStabSound();
                break;
            case "":
                PlayStabSound();
                break;
            case "Goblin Slash":
                PlaySFX(SoundName.GoblinAttack);
                break;
            case "Goblin Flurry":
                PlaySFX(SoundName.GoblinAttack);
                break;
            case "Firebolt":
                PlaySFX(SoundName.Magic1);
                break;
            case "Fireball":
                PlaySFX(SoundName.Magic4);
                break;
            case "Lightning Bolt":
                PlaySFX(SoundName.Electrical);
                break;
            case "Focus Shot":
            case "Shadow Arrows":
            case "Piercing Shot":
                PlaySFX(SoundName.Arrow);
                break;
            case "Goblin Tactics":
                PlaySFX(SoundName.WarCry);
                break;
        }
    }
    
    public void PlayGoblinAttack(){
        if (sfxSource && goblinAttackSFX){
            sfxSource.PlayOneShot(goblinAttackSFX);
        }
    }


    //EVADE SFX
    public void PlayEvadeMale(){
        if (sfxSource && evadeMaleSFX){
            sfxSource.PlayOneShot(evadeMaleSFX);
        }
    }

    public void PlayEvadeRogue(){
        if (sfxSource && evadeRogueSFX){
            sfxSource.PlayOneShot(evadeRogueSFX);
        }
    }


    public void PlayEvadeGoblin(){
        if (sfxSource && evadeGoblinSFX){
            sfxSource.PlayOneShot(evadeGoblinSFX);
        }
    }

    public void PlayEvadeHobgoblin(){
        if (sfxSource && evadeHobGoblinSFX){
            sfxSource.PlayOneShot(evadeHobGoblinSFX);
        }
    }


    //HURT SFX
    public void PlayHurtGoblin(){
        if (sfxSource && goblinHurtSFX){
            sfxSource.PlayOneShot(goblinHurtSFX);
        }
    }

    public void PlayHurtHobGoblin(){
         if (sfxSource && HobGoblinHurtSFX){
            sfxSource.PlayOneShot(HobGoblinHurtSFX);
        }       
    }

    public void PlayHurtMale(){
        if (sfxSource && maleHurtSFX){
            sfxSource.PlayOneShot(maleHurtSFX);
        }
    }

    public void PlayHurtFemale(){
        if (sfxSource && FemaleHurtSFX){
            sfxSource.PlayOneShot(FemaleHurtSFX);
        }
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

    public List<DictionaryElement<SoundName, AudioClip>> soundDictElements = new List<DictionaryElement<SoundName, AudioClip>>();
    Dictionary<SoundName, AudioClip> soundDict = new Dictionary<SoundName, AudioClip>();

    public void PlaySFX(SoundName s_name)
    {
        if (sfxSource && soundDict[s_name]) sfxSource.PlayOneShot(soundDict[s_name]);
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
    Electrical
}