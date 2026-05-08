using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Mixer (optional)")]
    public AudioMixer mixer;
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;

    [Header("UI Glass Crack")]
    public AudioClip[] sfxGlassCracks;

    [Header("Clips")]
    public AudioClip sfxAchievementUnlocked;
    public AudioClip bgmLoop;
    public AudioClip sfxRestart;
    public AudioClip sfxGameOver;
    public AudioClip[] sfxLineClears;
    public AudioClip[] sfxBossAbilityCasts;
    public AudioClip sfxBossLightningStrike;
    public AudioClip sfxBossRowBlastHit;
    public AudioClip sfxBossBoardBlastHit;
    public AudioClip sfxStoneBuffGranted;

    public AudioClip sfxXpTick;
    public AudioClip sfxXpGainOrb;
    public AudioClip sfxXpDrainOrb;
    public AudioClip sfxLevelUp;
    public AudioClip sfxMonsterDie;

    [Header("Slot Machine UI")]
    public AudioClip sfxSlotLever;
    public AudioClip sfxSlotSpinLoop;
    public AudioClip sfxSlotStop;
    public AudioClip sfxSlotReveal;

    [Header("Weather Ambience")]
    public AudioClip sfxRainLoop;
    [Range(0.01f, 1f)] public float rainFadeOutSeconds = 0.3f;

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

    [Header("Single Tracks (Intermission)")]
    public AudioClip EDMIntermissionWinMusic;
    public AudioClip MetalIntermissionWinMusic;
    public AudioClip EDMIntermissionLoseMusic;
    public AudioClip MetalIntermissionLoseMusic;

    [Header("Defaults")]
    [Range(0f, 1f)] public float masterVolume = 0.5f;
    [Range(0f, 1f)] public float musicVolume = 1.0f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
    [Range(0f, 0.25f)] public float sfxPitchJitter = 0.05f;

    private AudioSource musicSrc;
    private AudioSource sfxSrc;
    AudioSource uiSfxSrc;
    AudioSource uiLoopSfxSrc;
    AudioSource[] specialAbilityLoopSfxSources;
    AudioSource ambienceLoopSrc;
    AudioSource pauseMusicSrc;

    public MusicMode musicMode { get; private set; } = MusicMode.Both;

    Coroutine musicChainCo;
    Coroutine rainFadeCo;
    Coroutine specialAbilityLoopFadeCo;
    AudioClip[] currentPool;
    AudioClip lastGameplayClip;
    AudioClip lastBossClip;
    bool specialAbilityPausedMainMusic;
    float specialAbilityLoopSfxVolumeScale = 1f;

    enum MusicContext { None, Title, Level }
    MusicContext context = MusicContext.None;
    bool levelMusicActive = false;
    bool levelIsBoss = false;
    bool restartContextOnUnpause = false;

    const string K_Master = "vol_master";
    const string K_Music = "vol_music";
    const string K_SFX = "vol_sfx";
    const int SpecialAbilityLoopSfxLayerCount = 12;

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
        uiLoopSfxSrc = gameObject.AddComponent<AudioSource>();
        specialAbilityLoopSfxSources = new AudioSource[SpecialAbilityLoopSfxLayerCount];
        ambienceLoopSrc = gameObject.AddComponent<AudioSource>();
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
        uiSfxSrc.playOnAwake = false;
        uiSfxSrc.spatialBlend = 0f;

        uiLoopSfxSrc.ignoreListenerPause = true;
        uiLoopSfxSrc.playOnAwake = false;
        uiLoopSfxSrc.loop = true;
        uiLoopSfxSrc.spatialBlend = 0f;

        for (int i = 0; i < specialAbilityLoopSfxSources.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            ConfigureSpecialAbilityLoopSfxSource(source);
            specialAbilityLoopSfxSources[i] = source;
        }

        ambienceLoopSrc.ignoreListenerPause = false;
        ambienceLoopSrc.playOnAwake = false;
        ambienceLoopSrc.loop = true;
        ambienceLoopSrc.spatialBlend = 0f;

        if (sfxGroup) uiSfxSrc.outputAudioMixerGroup = sfxGroup;
        if (sfxGroup) uiLoopSfxSrc.outputAudioMixerGroup = sfxGroup;
        if (sfxGroup) ambienceLoopSrc.outputAudioMixerGroup = sfxGroup;

        musicSrc.spatialBlend = 0f;  // 2D
        sfxSrc.spatialBlend = 0f;

        if (musicGroup) musicSrc.outputAudioMixerGroup = musicGroup;
        if (sfxGroup) sfxSrc.outputAudioMixerGroup = sfxGroup;

        // Use SettingsStore values
        masterVolume = SettingsStore.LoadMaster();
        musicVolume = SettingsStore.LoadMusic();
        sfxVolume = SettingsStore.LoadSFX();

        // Ensure first-time defaults exist in prefs
        if (!PlayerPrefs.HasKey(K_Master)) PlayerPrefs.SetFloat(K_Master, 0.5f);
        if (!PlayerPrefs.HasKey(K_Music)) PlayerPrefs.SetFloat(K_Music, 1.0f);
        if (!PlayerPrefs.HasKey(K_SFX)) PlayerPrefs.SetFloat(K_SFX, 1.0f);
        PlayerPrefs.Save();

        // Load volumes
        masterVolume = PlayerPrefs.GetFloat(K_Master, 0.5f);
        musicVolume = PlayerPrefs.GetFloat(K_Music, 1.0f);
        sfxVolume = PlayerPrefs.GetFloat(K_SFX, 1.0f);

        // Apply without re-saving
        SetMasterVolume(masterVolume, save: false);
        SetMusicVolume(musicVolume, save: false);
        SetSFXVolume(sfxVolume, save: false);
    }

    void ConfigureSpecialAbilityLoopSfxSource(AudioSource source)
    {
        if (!source) return;

        source.ignoreListenerPause = true;
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;

        if (sfxGroup)
            source.outputAudioMixerGroup = sfxGroup;
    }

    void Start()
    {

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

    public void PauseMainMusicForSpecialAbilityPopup()
    {
        if (!musicSrc || specialAbilityPausedMainMusic)
            return;

        specialAbilityPausedMainMusic = musicSrc.isPlaying;
        if (specialAbilityPausedMainMusic)
            musicSrc.Pause();
    }

    public void ResumeMainMusicAfterSpecialAbilityPopup()
    {
        if (!musicSrc)
        {
            specialAbilityPausedMainMusic = false;
            return;
        }

        if (specialAbilityPausedMainMusic)
            musicSrc.UnPause();

        specialAbilityPausedMainMusic = false;
    }

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

    public void SetMasterVolume(float linear, bool save = true)
    {
        masterVolume = Mathf.Clamp01(linear);

        if (mixer && MixerHasParam("MasterVolume"))
            mixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(0.0001f, masterVolume)) * 20f);

        musicSrc.volume = masterVolume * musicVolume;
        sfxSrc.volume = masterVolume * sfxVolume;
        uiSfxSrc.volume = masterVolume * sfxVolume;
        uiLoopSfxSrc.volume = masterVolume * sfxVolume;
        ApplySpecialAbilityLoopSfxVolume();
        ambienceLoopSrc.volume = masterVolume * sfxVolume;
        pauseMusicSrc.volume = masterVolume * musicVolume;

        if (save) SettingsStore.SaveVolumes(masterVolume, musicVolume, sfxVolume);
    }

    public void SetMusicVolume(float linear, bool save = true)
    {
        musicVolume = Mathf.Clamp01(linear);

        if (mixer && musicGroup && MixerHasParam("MusicVolume"))
            mixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(0.0001f, musicVolume)) * 20f);

        musicSrc.volume = masterVolume * musicVolume;
        pauseMusicSrc.volume = masterVolume * musicVolume;

        if (save) SettingsStore.SaveVolumes(masterVolume, musicVolume, sfxVolume);
    }

    public void SetSFXVolume(float linear, bool save = true)
    {
        sfxVolume = Mathf.Clamp01(linear);

        if (mixer && sfxGroup && MixerHasParam("SFXVolume"))
            mixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(0.0001f, sfxVolume)) * 20f);

        sfxSrc.volume = masterVolume * sfxVolume;
        uiSfxSrc.volume = masterVolume * sfxVolume;
        uiLoopSfxSrc.volume = masterVolume * sfxVolume;
        ApplySpecialAbilityLoopSfxVolume();
        ambienceLoopSrc.volume = masterVolume * sfxVolume;

        if (save) SettingsStore.SaveVolumes(masterVolume, musicVolume, sfxVolume);
    }

    public void PlayLoopingUISFX(AudioClip clip, float vol = 1f, float pitch = 1f)
    {
        if (!clip || !uiLoopSfxSrc) return;

        if (uiLoopSfxSrc.isPlaying && uiLoopSfxSrc.clip == clip)
            return;

        uiLoopSfxSrc.Stop();
        uiLoopSfxSrc.clip = clip;
        uiLoopSfxSrc.loop = true;
        uiLoopSfxSrc.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
        uiLoopSfxSrc.volume = masterVolume * sfxVolume * Mathf.Clamp01(vol);
        uiLoopSfxSrc.Play();
    }

    public void StopLoopingUISFX()
    {
        if (!uiLoopSfxSrc) return;

        uiLoopSfxSrc.Stop();
        uiLoopSfxSrc.clip = null;
        uiLoopSfxSrc.pitch = 1f;
    }

    public void PlayRainAmbience(float vol = 1f, float pitch = 1f)
    {
        if (!sfxRainLoop || !ambienceLoopSrc)
            return;

        if (rainFadeCo != null)
        {
            StopCoroutine(rainFadeCo);
            rainFadeCo = null;
        }

        float targetVol = GetRainTargetVolume(vol);
        float clampedPitch = Mathf.Clamp(pitch, 0.5f, 2f);

        if (ambienceLoopSrc.clip == sfxRainLoop)
        {
            ambienceLoopSrc.loop = true;
            ambienceLoopSrc.pitch = clampedPitch;
            ambienceLoopSrc.volume = targetVol;

            if (!ambienceLoopSrc.isPlaying)
                ambienceLoopSrc.Play();

            return;
        }

        ambienceLoopSrc.Stop();
        ambienceLoopSrc.clip = sfxRainLoop;
        ambienceLoopSrc.loop = true;
        ambienceLoopSrc.pitch = clampedPitch;
        ambienceLoopSrc.volume = targetVol;
        ambienceLoopSrc.Play();
    }

    public void StopRainAmbience()
    {
        if (!ambienceLoopSrc) return;

        if (ambienceLoopSrc.clip != sfxRainLoop)
            return;

        if (!ambienceLoopSrc.isPlaying)
        {
            ambienceLoopSrc.clip = null;
            ambienceLoopSrc.pitch = 1f;
            ambienceLoopSrc.volume = masterVolume * sfxVolume;
            return;
        }

        if (rainFadeCo != null)
            StopCoroutine(rainFadeCo);

        rainFadeCo = StartCoroutine(CoFadeOutRainAmbience());
    }

    IEnumerator CoFadeOutRainAmbience()
    {
        if (!ambienceLoopSrc)
        {
            rainFadeCo = null;
            yield break;
        }

        float startVolume = ambienceLoopSrc.volume;
        float duration = Mathf.Max(0.01f, rainFadeOutSeconds);
        float t = 0f;

        while (t < duration && ambienceLoopSrc && ambienceLoopSrc.isPlaying && ambienceLoopSrc.clip == sfxRainLoop)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            ambienceLoopSrc.volume = Mathf.Lerp(startVolume, 0f, k);
            yield return null;
        }

        if (ambienceLoopSrc)
        {
            ambienceLoopSrc.Stop();
            ambienceLoopSrc.clip = null;
            ambienceLoopSrc.pitch = 1f;
            ambienceLoopSrc.volume = GetRainTargetVolume();
        }

        rainFadeCo = null;
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

    public void PlaySpecialAbilityAnimationSFX(AudioClip clip, float vol = 1f)
    {
        PlaySFX(clip, vol, pitch: 1f, jitter: false);
    }

    public void PlaySpecialAbilityPopupLoopSFX(AudioClip clip, float vol = 1f, float pitch = 1f)
    {
        if (!clip || specialAbilityLoopSfxSources == null) return;

        if (specialAbilityLoopFadeCo != null)
        {
            StopCoroutine(specialAbilityLoopFadeCo);
            specialAbilityLoopFadeCo = null;
        }

        specialAbilityLoopSfxVolumeScale = Mathf.Max(0f, vol);
        float clampedPitch = Mathf.Clamp(pitch, 0.5f, 2f);

        for (int i = 0; i < specialAbilityLoopSfxSources.Length; i++)
        {
            AudioSource source = specialAbilityLoopSfxSources[i];
            if (!source) continue;

            source.Stop();
            source.clip = clip;
            source.loop = true;
            source.pitch = clampedPitch;
            source.time = 0f;
        }

        ApplySpecialAbilityLoopSfxVolume();

        for (int i = 0; i < specialAbilityLoopSfxSources.Length; i++)
        {
            AudioSource source = specialAbilityLoopSfxSources[i];
            if (source && source.clip)
                source.Play();
        }
    }

    public void FadeSpecialAbilityPopupLoopSFX(float durationSeconds = 1f, float targetVolumeScale = 0.05f)
    {
        if (specialAbilityLoopSfxSources == null)
            return;

        if (specialAbilityLoopFadeCo != null)
            StopCoroutine(specialAbilityLoopFadeCo);

        specialAbilityLoopFadeCo = StartCoroutine(FadeSpecialAbilityPopupLoopSFXRoutine(durationSeconds, targetVolumeScale));
    }

    public void StopSpecialAbilityPopupLoopSFX()
    {
        if (specialAbilityLoopSfxSources == null) return;

        if (specialAbilityLoopFadeCo != null)
        {
            StopCoroutine(specialAbilityLoopFadeCo);
            specialAbilityLoopFadeCo = null;
        }

        for (int i = 0; i < specialAbilityLoopSfxSources.Length; i++)
        {
            AudioSource source = specialAbilityLoopSfxSources[i];
            if (!source) continue;

            source.Stop();
            source.clip = null;
            source.pitch = 1f;
        }

        specialAbilityLoopSfxVolumeScale = 1f;
        ApplySpecialAbilityLoopSfxVolume();
    }

    IEnumerator FadeSpecialAbilityPopupLoopSFXRoutine(float durationSeconds, float targetVolumeScale)
    {
        float duration = Mathf.Max(0.01f, durationSeconds);
        float startScale = specialAbilityLoopSfxVolumeScale;
        float endScale = Mathf.Clamp(targetVolumeScale, 0f, startScale);
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            specialAbilityLoopSfxVolumeScale = Mathf.Lerp(startScale, endScale, Mathf.Clamp01(t / duration));
            ApplySpecialAbilityLoopSfxVolume();
            yield return null;
        }

        specialAbilityLoopSfxVolumeScale = endScale;
        ApplySpecialAbilityLoopSfxVolume();
        specialAbilityLoopFadeCo = null;
    }

    void ApplySpecialAbilityLoopSfxVolume()
    {
        if (specialAbilityLoopSfxSources == null)
            return;

        float remainingVolume = masterVolume * sfxVolume * specialAbilityLoopSfxVolumeScale;
        for (int i = 0; i < specialAbilityLoopSfxSources.Length; i++)
        {
            AudioSource source = specialAbilityLoopSfxSources[i];
            if (!source) continue;

            float layerVolume = Mathf.Clamp01(remainingVolume);
            source.volume = layerVolume;
            remainingVolume -= layerVolume;
        }
    }

    public void PlaySlotLever(float vol = 4f)
    {
        if (!sfxSlotLever) return;
        PlayUISFX(sfxSlotLever, vol);
    }

    public void PlaySlotSpinLoop(float vol = 1f, float pitch = 1f)
    {
        if (!sfxSlotSpinLoop) return;
        PlayLoopingUISFX(sfxSlotSpinLoop, vol, pitch);
    }

    public void StopSlotSpinLoop()
    {
        StopLoopingUISFX();
    }

    public void PlaySlotStop(float vol = 1f)
    {
        if (!sfxSlotStop) return;
        PlayUISFX(sfxSlotStop, vol, pitch: 1f, jitter: false);
    }

    public void PlaySlotReveal(float vol = 1f)
    {
        if (!sfxSlotReveal) return;
        PlayUISFX(sfxSlotReveal, vol);
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
        StopRainAmbience();
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

    AudioClip PickIntermissionWinClipByMode()
    {
        if (musicMode == MusicMode.EDM) return EDMIntermissionWinMusic;
        if (musicMode == MusicMode.Metal) return MetalIntermissionWinMusic;

        // Both
        if (EDMIntermissionWinMusic && MetalIntermissionWinMusic)
            return (Random.value < 0.5f) ? EDMIntermissionWinMusic : MetalIntermissionWinMusic;

        return EDMIntermissionWinMusic ? EDMIntermissionWinMusic : MetalIntermissionWinMusic;
    }

    AudioClip PickIntermissionLoseClipByMode()
    {
        if (musicMode == MusicMode.EDM) return EDMIntermissionLoseMusic;
        if (musicMode == MusicMode.Metal) return MetalIntermissionLoseMusic;

        // Both
        if (EDMIntermissionLoseMusic && MetalIntermissionLoseMusic)
            return (Random.value < 0.5f) ? EDMIntermissionLoseMusic : MetalIntermissionLoseMusic;

        return EDMIntermissionLoseMusic ? EDMIntermissionLoseMusic : MetalIntermissionLoseMusic;
    }

    public void PlayIntermissionWinMusic()
    {
        var clip = PickIntermissionWinClipByMode();
        PlayIntermission(clip);
    }

    public void PlayIntermissionLoseMusic()
    {
        var clip = PickIntermissionLoseClipByMode();
        PlayIntermission(clip);
    }

    void PlayIntermission(AudioClip clip)
    {
        if (!clip) return;

        // Intermission replaces gameplay/pause music cleanly
        context = MusicContext.None;
        levelMusicActive = false;

        StopPauseMusic();
        StopLevelMusicInternal();
        StopRainAmbience();

        PlayMusic(clip, loop: true, vol: 1f);
    }

    System.Collections.IEnumerator MusicChainRoutine()
    {
        bool isBoss = levelIsBoss; // Snapshot boss flag for this chain

        // Pull last-clip state for this context to avoid repeating across levels
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

    public void PlayMonsterDieSFX(float vol = 1f)
    {
        if (!sfxMonsterDie)
        {
            Debug.LogWarning("AudioManager: sfxMonsterDie is not assigned.");
            return;
        }

        uiSfxSrc.volume = masterVolume * sfxVolume;
        PlayUISFX(sfxMonsterDie, vol: vol, pitch: 1f, jitter: true);
    }

    public void PlayRandomGlassCrack(float vol = 1f)
    {
        var clip = PickRandom(sfxGlassCracks);
        if (!clip) return;

        PlayUISFX(clip, vol, pitch: 1f, jitter: true);
    }

    public void PlayBossRowBlastHit(float vol = 1f)
    {
        if (!sfxBossRowBlastHit) return;
        PlaySFX(sfxBossRowBlastHit, vol);
    }

    public void PlayBossBoardBlastHit(float vol = 1f)
    {
        if (!sfxBossBoardBlastHit) return;
        PlaySFX(sfxBossBoardBlastHit, vol);
    }

    float GetRainTargetVolume(float vol = 1f)
    {
        return masterVolume * sfxVolume * Mathf.Clamp01(vol);
    }

    public void ConfigureExternalMusicSource(AudioSource source, float vol = 1f)
    {
        if (!source) return;

        source.spatialBlend = 0f;
        source.playOnAwake = false;
        source.ignoreListenerPause = false;

        if (musicGroup)
            source.outputAudioMixerGroup = musicGroup;

        source.volume = masterVolume * musicVolume * Mathf.Clamp01(vol);
    }
}
