using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Mixer (optional)")]
    public AudioMixer mixer;
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;

    [Header("Clips")]
    public AudioClip bgmLoop;
    public AudioClip sfxRestart;
    public AudioClip sfxGameOver;
    public AudioClip[] sfxLineClears;

    [Header("Defaults")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 0.25f)] public float sfxPitchJitter = 0.05f;

    private AudioSource musicSrc;
    private AudioSource sfxSrc;
    AudioSource uiSfxSrc;

    const string K_Master = "vol_master";
    const string K_Music = "vol_music";
    const string K_SFX = "vol_sfx";

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        musicSrc = gameObject.AddComponent<AudioSource>();
        sfxSrc = gameObject.AddComponent<AudioSource>();
        uiSfxSrc = gameObject.AddComponent<AudioSource>();

        musicSrc.loop = true;
        musicSrc.playOnAwake = false;
        sfxSrc.playOnAwake = false;
        uiSfxSrc.ignoreListenerPause = true;

        if (sfxGroup) uiSfxSrc.outputAudioMixerGroup = sfxGroup;

        musicSrc.spatialBlend = 0f;  // 2D
        sfxSrc.spatialBlend = 0f;

        if (musicGroup) musicSrc.outputAudioMixerGroup = musicGroup;
        if (sfxGroup) sfxSrc.outputAudioMixerGroup = sfxGroup;

        SetMasterVolume(masterVolume);
        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);
    }

    void Start()
    {
        // Load saved volumes
        if (PlayerPrefs.HasKey(K_Music)) SetMusicVolume(PlayerPrefs.GetFloat(K_Music, musicVolume));
        if (PlayerPrefs.HasKey(K_SFX)) SetSFXVolume(PlayerPrefs.GetFloat(K_SFX, sfxVolume));
        if (PlayerPrefs.HasKey(K_Master)) SetMasterVolume(PlayerPrefs.GetFloat(K_Master, masterVolume));
    }

    public void PlayMusic(AudioClip clip, bool loop = true, float vol = 0.5f)
    {
        if (!clip) return;
        musicSrc.clip = clip;
        musicSrc.loop = loop;
        musicSrc.volume = vol;
        musicSrc.Play();
    }

    public void StopMusic() => musicSrc.Stop();

    public void PlaySFX(AudioClip clip, float vol = 1f, float pitch = 1f, bool jitter = true)
    {
        if (!clip) return;
        float p = pitch;
        if (jitter) p += Random.Range(-sfxPitchJitter, sfxPitchJitter);
        sfxSrc.pitch = Mathf.Clamp(p, 0.5f, 2f);
        sfxSrc.PlayOneShot(clip, vol);
        sfxSrc.pitch = 1f; // reset
    }

    public void PlayPreviewSFX()
    {
        // Prefer your random line-clear pool, fall back to any existing one-shot
        if (sfxLineClears != null && sfxLineClears.Length > 0)
        {
            var clip = PickRandom(sfxLineClears);
            PlayUISFX(clip);
            return;
        }

        if (sfxRestart) { PlayUISFX(sfxRestart); return; }
        if (sfxGameOver) { PlayUISFX(sfxGameOver); return; }
    }

    bool MixerHasParam(string name)
    {
        if (!mixer) return false;
        float _;
        return mixer.GetFloat(name, out _); // True only if param exists
    }

    public void SetMasterVolume(float linear)
    {
        masterVolume = Mathf.Clamp01(linear);

        // If the MasterVolume exposed param exists, drive it, otherwise just scale the sources
        if (mixer && MixerHasParam("MasterVolume"))
            mixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(0.0001f, masterVolume)) * 20f);

        // Base-level scaling even when no mixer
        musicSrc.volume = masterVolume * musicVolume;
        sfxSrc.volume = masterVolume * sfxVolume;

        PlayerPrefs.SetFloat(K_Master, masterVolume); PlayerPrefs.Save();
    }

    public void SetMusicVolume(float linear)
    {
        musicVolume = Mathf.Clamp01(linear);
        if (mixer && musicGroup && MixerHasParam("MusicVolume"))
            mixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(0.0001f, musicVolume)) * 20f);
        musicSrc.volume = masterVolume * musicVolume; // Multiply by master
        PlayerPrefs.SetFloat(K_Music, musicVolume); PlayerPrefs.Save();
    }
    public void SetSFXVolume(float linear)
    {
        sfxVolume = Mathf.Clamp01(linear);
        if (mixer && sfxGroup && MixerHasParam("SFXVolume"))
            mixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(0.0001f, sfxVolume)) * 20f);
        sfxSrc.volume = masterVolume * sfxVolume; // Multiply by master
        PlayerPrefs.SetFloat(K_SFX, sfxVolume); PlayerPrefs.Save();
    }

    AudioClip PickRandom(AudioClip[] pool)
    {
        if (pool == null || pool.Length == 0) return null;
        return pool[Random.Range(0, pool.Length)];
    }

    public void PlayRandomLineClear(float vol = 1f)
    {
        var clip = PickRandom(sfxLineClears);
        PlayUISFX(clip, vol);
    }

    public void PlayUISFX(AudioClip clip, float vol = 1f, float pitch = 1f, bool jitter = true)
    {
        if (!clip) return;

        float p = pitch;
        if (jitter) p += Random.Range(-sfxPitchJitter, sfxPitchJitter);

        uiSfxSrc.pitch = Mathf.Clamp(p, 0.5f, 2f);
        uiSfxSrc.PlayOneShot(clip, vol);
        uiSfxSrc.pitch = 1f; // Reset
    }

}
