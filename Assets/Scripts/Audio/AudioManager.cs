using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Audio Sources")]
    [Tooltip("For sound effects (digging, menu sounds, etc.)")]
    public AudioSource sfxSource;
    
    [Tooltip("For music/ambient sounds")]
    public AudioSource musicSource;
    
    [Header("Sound Effects")]
    public AudioClip victorySound;
    public AudioClip menuOpenSound;
    public AudioClip digSound;
    public AudioClip levelUpSound;
    public AudioClip sellSound;
    public AudioClip upgradeSound;
    
    [Header("Settings")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    
    void Awake()
    {
        // Singleton pattern - only one AudioManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Create AudioSources if not assigned
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
        
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }
        
        UpdateVolumes();
    }
    
    void UpdateVolumes()
    {
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }
    
    public void PlaySound(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
    
    public void PlaySound(AudioClip clip, float volumeScale)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
        }
    }
    
    // Convenience methods for specific sounds
    public void PlayVictory(float volumeScale = 0.65f) => PlaySound(victorySound, volumeScale);
    public void PlayMenuOpen(float volumeScale = 3f) => PlaySound(menuOpenSound, volumeScale);
    public void PlayDig(float volumeScale = 0.625f) => PlaySound(digSound, volumeScale);
    public void PlayLevelUp(float volumeScale = 0.2f) => PlaySound(levelUpSound, volumeScale);
    public void PlaySell(float volumeScale = 1f) => PlaySound(sellSound, volumeScale);
    public void PlayUpgrade(float volumeScale = 1f) => PlaySound(upgradeSound, volumeScale);

    public void PlayMusic(AudioClip clip)
    {
        if (clip != null && musicSource != null)
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
    }
    
    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }
}
