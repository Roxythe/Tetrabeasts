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
    public AudioClip[] sfxBossAbilityCasts;
    public AudioClip sfxBossLightningStrike;

    [Header("Music Pools (Gameplay)")]
    public AudioClip[] EDMGameplayMusic;
    public AudioClip[] EDMBossMusic;
    public AudioClip[] MetalGameplayMusic;
    public AudioClip[] MetalBossMusic;

    [Header("Single Tracks (Menus)")]
    public AudioClip EDMTitleMusic;
    public AudioClip MetalTitleMusic;
    public AudioClip EDMPauseMusic;
    public AudioClip MetalPauseMusic;

    [Header("Defaults")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 0.25f)] public float sfxPitchJitter = 0.05f;

    private AudioSource musicSrc;
    private AudioSource sfxSrc;
    AudioSource uiSfxSrc;
    AudioSource pauseMusicSrc;

    public MusicMode musicMode { get; private set; } = MusicMode.Both;

    Coroutine musicChainCo;
    AudioClip[] currentPool;
    AudioClip lastGameplayClip;
    AudioClip lastBossClip;

    enum MusicContext { None, Title, Level }
    MusicContext context = MusicContext.None;
    bool levelMusicActive = false;
    bool levelIsBoss = false;
    bool restartContextOnUnpause = false;

    const string K_Master = "vol_master";
    const string K_Music = "vol_music";
    const string K_SFX = "vol_sfx";

    public enum MusicMode { EDM = 0, Metal = 1, Both = 2 }
    const string K_MusicMode = "music_mode";


    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        musicSrc = gameObject.AddComponent<AudioSource>();
        sfxSrc = gameObject.AddComponent<AudioSource>();
        uiSfxSrc = gameObject.AddComponent<AudioSource>();
        pauseMusicSrc = gameObject.AddComponent<AudioSource>();

        pauseMusicSrc.playOnAwake = false;
        pauseMusicSrc.loop = true;
        pauseMusicSrc.spatialBlend = 0f;
        pauseMusicSrc.ignoreListenerPause = true; // Plays while AudioListener.pause = true
        if (musicGroup) pauseMusicSrc.outputAudioMixerGroup = musicGroup;

        musicMode = (MusicMode)PlayerPrefs.GetInt(K_MusicMode, (int)MusicMode.Both); // Load saved mode

        musicSrc.loop = false; // Change manually
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

    public void PlayMusic(AudioClip clip, bool loop = true, float vol = 1f)
    {
        if (!clip) return;
        musicSrc.clip = clip;
        musicSrc.loop = loop;

        musicSrc.volume = masterVolume * musicVolume * Mathf.Clamp01(vol); // Apply volumes
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
        sfxSrc.pitch = 1f; // Reset
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
        uiSfxSrc.volume = masterVolume * sfxVolume;
        pauseMusicSrc.volume = masterVolume * musicVolume;

        PlayerPrefs.SetFloat(K_Master, masterVolume); PlayerPrefs.Save();
    }

    public void SetMusicVolume(float linear)
    {
        musicVolume = Mathf.Clamp01(linear);
        if (mixer && musicGroup && MixerHasParam("MusicVolume"))
            mixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(0.0001f, musicVolume)) * 20f);
        musicSrc.volume = masterVolume * musicVolume; // Multiply by master
        pauseMusicSrc.volume = masterVolume * musicVolume;
        PlayerPrefs.SetFloat(K_Music, musicVolume); PlayerPrefs.Save();
    }
    public void SetSFXVolume(float linear)
    {
        sfxVolume = Mathf.Clamp01(linear);
        if (mixer && sfxGroup && MixerHasParam("SFXVolume"))
            mixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(0.0001f, sfxVolume)) * 20f);
        sfxSrc.volume = masterVolume * sfxVolume; // Multiply by master
        uiSfxSrc.volume = masterVolume * sfxVolume;
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

    public void PlayBossAbilityCast(float vol = 1f)
    {
        if (sfxBossAbilityCasts == null || sfxBossAbilityCasts.Length == 0) return;

        // Pick a non-null clip if possible
        for (int tries = 0; tries < sfxBossAbilityCasts.Length; tries++)
        {
            var clip = sfxBossAbilityCasts[UnityEngine.Random.Range(0, sfxBossAbilityCasts.Length)];
            if (!clip) continue;

            PlaySFX(clip, vol);
            return;
        }
    }

    public void PlayBossLightningStrike(float vol = 1f)
    {
        if (!sfxBossLightningStrike) return;
        PlaySFX(sfxBossLightningStrike, vol);
    }

    public MusicMode GetMusicMode() => musicMode;

    public void SetMusicMode(MusicMode mode)
    {
        musicMode = mode;
        PlayerPrefs.SetInt(K_MusicMode, (int)musicMode);
        PlayerPrefs.Save();

        if (AudioListener.pause)
        {
            restartContextOnUnpause = true;

            if (pauseMusicSrc)
            {
                pauseMusicSrc.Stop();
                PlayPauseMusic();
            }

            return;
        }

        RestartContextMusic();

        if (pauseMusicSrc && pauseMusicSrc.isPlaying)
        {
            pauseMusicSrc.Stop();
            PlayPauseMusic();
        }
    }

    public void ApplyPendingMusicModeAfterUnpause()
    {
        if (!restartContextOnUnpause) return;

        restartContextOnUnpause = false;
        RestartContextMusic();
    }

    public void PlayTitleMusic()
    {
        context = MusicContext.Title;
        levelMusicActive = false;
        StopLevelMusicInternal();

        var clip = PickTitleClipByMode();
        if (!clip && bgmLoop) clip = bgmLoop;

        PlayMusic(clip, loop: true, vol: 1f);
    }

    public void PlayLevelMusic(bool isBoss)
    {
        StopPauseMusic();

        context = MusicContext.Level;
        levelIsBoss = isBoss;
        levelMusicActive = true;

        StopLevelMusicInternal();

        currentPool = BuildPool(isBoss);
        if (currentPool == null || currentPool.Length == 0)
        {
            if (bgmLoop) PlayMusic(bgmLoop, loop: true, vol: 1f);
            return;
        }

        musicChainCo = StartCoroutine(MusicChainRoutine());
    }

    public void StopLevelMusic()
    {
        levelMusicActive = false;
        StopLevelMusicInternal();
    }

    void StopLevelMusicInternal()
    {
        if (musicChainCo != null) { StopCoroutine(musicChainCo); musicChainCo = null; }
        currentPool = null;
        StopMusic();
    }

    public void PlayPauseMusic()
    {
        // Pick EDM/Metal/both clip
        var clip = PickPauseClipByMode();
        if (!clip) return;

        pauseMusicSrc.clip = clip;
        pauseMusicSrc.volume = masterVolume * musicVolume;
        pauseMusicSrc.loop = true;
        pauseMusicSrc.Play();
    }

    public void StopPauseMusic()
    {
        if (pauseMusicSrc) pauseMusicSrc.Stop();
    }

    void RestartContextMusic()
    {
        if (AudioListener.pause) return;  // If paused, leave pause music alone

        switch (context)
        {
            case MusicContext.Title:
                PlayTitleMusic();
                break;
            case MusicContext.Level:
                PlayLevelMusic(levelIsBoss);
                break;
        }
    }

    AudioClip[] BuildPool(bool isBoss)
    {
        // Gather allowed pools based on mode
        var pools = new System.Collections.Generic.List<AudioClip[]>();

        if (musicMode == MusicMode.EDM || musicMode == MusicMode.Both)
            pools.Add(isBoss ? EDMBossMusic : EDMGameplayMusic);

        if (musicMode == MusicMode.Metal || musicMode == MusicMode.Both)
            pools.Add(isBoss ? MetalBossMusic : MetalGameplayMusic);

        // Flatten pools into one list
        var merged = new System.Collections.Generic.List<AudioClip>();
        foreach (var p in pools)
        {
            if (p == null) continue;
            for (int i = 0; i < p.Length; i++)
                if (p[i]) merged.Add(p[i]);
        }

        return merged.ToArray();
    }

    AudioClip PickTitleClipByMode()
    {
        if (musicMode == MusicMode.EDM) return EDMTitleMusic;
        if (musicMode == MusicMode.Metal) return MetalTitleMusic;

        // Both
        if (EDMTitleMusic && MetalTitleMusic) return (Random.value < 0.5f) ? EDMTitleMusic : MetalTitleMusic;
        return EDMTitleMusic ? EDMTitleMusic : MetalTitleMusic;
    }

    AudioClip PickPauseClipByMode()
    {
        if (musicMode == MusicMode.EDM) return EDMPauseMusic;
        if (musicMode == MusicMode.Metal) return MetalPauseMusic;

        // Both
        if (EDMPauseMusic && MetalPauseMusic) return (Random.value < 0.5f) ? EDMPauseMusic : MetalPauseMusic;
        return EDMPauseMusic ? EDMPauseMusic : MetalPauseMusic;
    }

    System.Collections.IEnumerator MusicChainRoutine()
    {
        // Snapshot boss flag for this chain (in case another level starts mid-routine)
        bool isBoss = levelIsBoss;

        // Pull last-clip state for this context so we avoid repeating across levels
        AudioClip lastClip = isBoss ? lastBossClip : lastGameplayClip;

        while (levelMusicActive && currentPool != null && currentPool.Length > 0)
        {
            AudioClip next = null;

            // Count valid clips once
            int validCount = 0;
            for (int i = 0; i < currentPool.Length; i++)
                if (currentPool[i]) validCount++;

            for (int tries = 0; tries < 16; tries++)
            {
                var c = currentPool[Random.Range(0, currentPool.Length)];
                if (!c) continue;

                if (validCount > 1 && c == lastClip) continue;

                next = c;
                break;
            }

            if (!next)
            {
                for (int i = 0; i < currentPool.Length; i++)
                    if (currentPool[i]) { next = currentPool[i]; break; }
            }

            if (!next) yield break;

            // Save last clip for this context (boss vs gameplay)
            lastClip = next;
            if (isBoss) lastBossClip = lastClip;
            else lastGameplayClip = lastClip;

            musicSrc.clip = next;
            musicSrc.loop = false;
            musicSrc.volume = masterVolume * musicVolume;
            musicSrc.Play();

            while (levelMusicActive && musicSrc.clip == next && musicSrc.time < next.length - 0.05f)
                yield return null;
        }
    }

}
