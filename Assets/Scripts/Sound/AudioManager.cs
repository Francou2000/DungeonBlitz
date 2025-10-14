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
        SetVolume("MasterVolume", value);
    }

    public void SetMusicVolume(float value)
    {
        SetVolume("MusicVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        SetVolume("SFXVolume", value);
    }

    private void SetVolume(string exposedParam, float value)
    {
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1)) * 20f;
        audioMixer.SetFloat(exposedParam, dB);
    }
}
