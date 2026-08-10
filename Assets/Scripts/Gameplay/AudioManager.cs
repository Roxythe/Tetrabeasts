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
    [Range(0f, 1f)] public float rainAmbienceVolumeScale = 0.35f;

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

    [Header("Music Transitions")]
    [SerializeField, Range(0.05f, 8f)] float levelMusicCrossfadeSeconds = 2f;
    [SerializeField, Range(0.05f, 8f)] float musicCrossfadeSeconds = 1.25f;
    [SerializeField, Range(0.05f, 8f)] float musicFadeOutSeconds = 1f;
    [SerializeField, Range(0.05f, 4f)] float pauseMusicFadeSeconds = 0.45f;
    [SerializeField, Range(0.05f, 4f)] float specialAbilityMusicFadeSeconds = 0.35f;
    [SerializeField, Range(0f, 1f)] float specialAbilityBackgroundMusicVolumeScale = 0.18f;

    [Header("Special Ability SFX Tuning")]
    [SerializeField, Min(0f)] float specialAbilityLoopVolumeBoost = 4f;
    [SerializeField, Min(0f)] float specialAbilityLoopMaxVolumeScale = 4f;

    private AudioSource musicSrc;
    AudioSource musicTransitionSrc;
    private AudioSource sfxSrc;
    AudioSource[] sfxOneShotSources;
    int sfxOneShotSourceIndex;
    AudioSource uiSfxSrc;
    AudioSource uiLoopSfxSrc;
    AudioSource specialAbilityOneShotSrc;
    AudioSource[] specialAbilityLoopSfxSources;
    AudioSource ambienceLoopSrc;
    AudioSource pauseMusicSrc;

    public MusicMode musicMode { get; private set; } = MusicMode.EDM;

    Coroutine musicChainCo;
    Coroutine mainMusicFadeCo;
    Coroutine pauseMusicFadeCo;
    Coroutine pauseMainMusicFadeCo;
    Coroutine specialAbilityMusicFadeCo;
    Coroutine rainFadeCo;
    Coroutine specialAbilityLoopFadeCo;
    AudioClip[] currentPool;
    AudioClip lastGameplayClip;
    AudioClip lastBossClip;
    bool specialAbilityPausedMainMusic;
    float specialAbilityLoopSfxVolumeScale = 1f;
    float rainAmbienceRequestedVolume = 1f;
    AudioSource activeMusicSrc;
    float musicSrcVolumeScale = 1f;
    float musicTransitionSrcVolumeScale = 0f;
    float pauseMusicVolumeScale = 1f;
    float specialAbilityMusicSrcResumeScale = 1f;
    float specialAbilityMusicTransitionResumeScale = 0f;
    bool pauseFadedMainMusic;
    float pauseMusicSrcResumeScale = 1f;
    float pauseMusicTransitionResumeScale = 0f;
    bool applicationHasFocus = true;
    bool applicationPaused;
    bool holdMusicChainForFocusResume;
    Coroutine focusResumeHoldCo;

    enum MusicContext { None, Title, Level }
    MusicContext context = MusicContext.None;
    bool levelMusicActive = false;
    bool levelIsBoss = false;
    bool restartContextOnUnpause = false;

    const int SfxOneShotSourceCount = 12;
    const int SpecialAbilityLoopSfxLayerCount = 12;

    public enum MusicMode { EDM = 0, Metal = 1, Both = 2 }
    const string K_MusicMode = "music_mode";


    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        musicSrc = gameObject.AddComponent<AudioSource>();
        musicTransitionSrc = gameObject.AddComponent<AudioSource>();
        sfxSrc = gameObject.AddComponent<AudioSource>();
        BuildSfxOneShotSources();
        uiSfxSrc = gameObject.AddComponent<AudioSource>();
        uiLoopSfxSrc = gameObject.AddComponent<AudioSource>();
        specialAbilityOneShotSrc = gameObject.AddComponent<AudioSource>();
        specialAbilityLoopSfxSources = new AudioSource[SpecialAbilityLoopSfxLayerCount];
        ambienceLoopSrc = gameObject.AddComponent<AudioSource>();
        pauseMusicSrc = gameObject.AddComponent<AudioSource>();

        pauseMusicSrc.playOnAwake = false;
        pauseMusicSrc.loop = true;
        pauseMusicSrc.spatialBlend = 0f;
        pauseMusicSrc.ignoreListenerPause = true; // Plays while AudioListener.pause = true
        if (musicGroup) pauseMusicSrc.outputAudioMixerGroup = musicGroup;

        musicMode = (MusicMode)PlayerPrefs.GetInt(K_MusicMode, (int)MusicMode.EDM); // Load saved mode
        applicationHasFocus = Application.isFocused;
        applicationPaused = false;

        ConfigureMusicSource(musicSrc);
        ConfigureMusicSource(musicTransitionSrc);
        activeMusicSrc = musicSrc;

        uiSfxSrc.ignoreListenerPause = true;
        uiSfxSrc.playOnAwake = false;
        uiSfxSrc.spatialBlend = 0f;

        uiLoopSfxSrc.ignoreListenerPause = true;
        uiLoopSfxSrc.playOnAwake = false;
        uiLoopSfxSrc.loop = true;
        uiLoopSfxSrc.spatialBlend = 0f;

        ConfigureSpecialAbilityOneShotSource();

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

        ApplySfxOneShotSourceSettings();

        masterVolume = SettingsStore.LoadMaster();
        musicVolume = SettingsStore.LoadMusic();
        sfxVolume = SettingsStore.LoadSFX();

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

    void ConfigureSpecialAbilityOneShotSource()
    {
        if (!specialAbilityOneShotSrc) return;

        specialAbilityOneShotSrc.ignoreListenerPause = true;
        specialAbilityOneShotSrc.playOnAwake = false;
        specialAbilityOneShotSrc.loop = false;
        specialAbilityOneShotSrc.spatialBlend = 0f;
        specialAbilityOneShotSrc.volume = masterVolume * sfxVolume;

        if (sfxGroup)
            specialAbilityOneShotSrc.outputAudioMixerGroup = sfxGroup;
    }

    void BuildSfxOneShotSources()
    {
        sfxOneShotSources = new AudioSource[SfxOneShotSourceCount];
        sfxOneShotSources[0] = sfxSrc;

        ConfigureSfxOneShotSource(sfxSrc);

        for (int i = 1; i < sfxOneShotSources.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            ConfigureSfxOneShotSource(source);
            sfxOneShotSources[i] = source;
        }
    }

    void ConfigureSfxOneShotSource(AudioSource source)
    {
        if (!source) return;

        source.ignoreListenerPause = true;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = masterVolume * sfxVolume;

        if (sfxGroup)
            source.outputAudioMixerGroup = sfxGroup;
    }

    void ApplySfxOneShotSourceSettings()
    {
        if (sfxOneShotSources == null || sfxOneShotSources.Length == 0)
        {
            ConfigureSfxOneShotSource(sfxSrc);
            return;
        }

        for (int i = 0; i < sfxOneShotSources.Length; i++)
            ConfigureSfxOneShotSource(sfxOneShotSources[i]);
    }

    void ApplySfxOneShotSourceVolume()
    {
        float volume = masterVolume * sfxVolume;

        if (sfxOneShotSources == null || sfxOneShotSources.Length == 0)
        {
            if (sfxSrc) sfxSrc.volume = volume;
            return;
        }

        for (int i = 0; i < sfxOneShotSources.Length; i++)
        {
            AudioSource source = sfxOneShotSources[i];
            if (source) source.volume = volume;
        }
    }

    AudioSource GetNextSfxOneShotSource()
    {
        if (sfxOneShotSources == null || sfxOneShotSources.Length == 0)
            return sfxSrc;

        int start = sfxOneShotSourceIndex;
        for (int i = 0; i < sfxOneShotSources.Length; i++)
        {
            int index = (start + i) % sfxOneShotSources.Length;
            AudioSource source = sfxOneShotSources[index];
            if (!source) continue;

            sfxOneShotSourceIndex = (index + 1) % sfxOneShotSources.Length;
            return source;
        }

        return sfxSrc;
    }

    void Start()
    {

    }

    void OnApplicationFocus(bool hasFocus)
    {
        applicationHasFocus = hasFocus;
        if (hasFocus)
            HoldMusicChainForFocusResumeFrame();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        applicationPaused = pauseStatus;
        if (!pauseStatus)
            HoldMusicChainForFocusResumeFrame();
    }

    void ConfigureMusicSource(AudioSource source)
    {
        if (!source) return;

        source.loop = false;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;

        if (musicGroup)
            source.outputAudioMixerGroup = musicGroup;
    }

    public void PlayMusic(AudioClip clip, bool loop = true, float vol = 1f)
    {
        if (!clip) return;

        CrossfadeMainMusicTo(clip, loop, Mathf.Clamp01(vol), GetGeneralMusicCrossfadeDuration(null, clip));
    }

    public void StopMusic()
    {
        StopPauseMainMusicFadeCoroutine();
        StopSpecialAbilityMusicFadeCoroutine();
        pauseFadedMainMusic = false;
        specialAbilityPausedMainMusic = false;
        FadeOutMainMusic(musicFadeOutSeconds);
    }

    public void PauseMainMusicForSpecialAbilityPopup()
    {
        if (specialAbilityPausedMainMusic)
            return;

        specialAbilityPausedMainMusic = IsMainMusicPlaying();
        if (specialAbilityPausedMainMusic)
        {
            specialAbilityMusicSrcResumeScale = GetMainMusicSourceVolumeScale(musicSrc);
            specialAbilityMusicTransitionResumeScale = GetMainMusicSourceVolumeScale(musicTransitionSrc);
            StartSpecialAbilityMainMusicFade(pausing: true);
        }
    }

    public void ResumeMainMusicAfterSpecialAbilityPopup()
    {
        if (specialAbilityPausedMainMusic)
            StartSpecialAbilityMainMusicFade(pausing: false);
    }

    void ResetMainMusicRouting()
    {
        if (musicSrc)
        {
            musicSrc.Stop();
            musicSrc.clip = null;
            musicSrc.loop = false;
        }

        if (musicTransitionSrc)
        {
            musicTransitionSrc.Stop();
            musicTransitionSrc.clip = null;
            musicTransitionSrc.loop = false;
        }

        activeMusicSrc = musicSrc;
        SetMainMusicSourceVolumeScale(musicSrc, 1f);
        SetMainMusicSourceVolumeScale(musicTransitionSrc, 0f);
    }

    void StopMainMusicSource(AudioSource source)
    {
        if (!source) return;

        source.Stop();
        source.clip = null;
        source.loop = false;
    }

    void PauseMainMusicSource(AudioSource source)
    {
        if (source && source.isPlaying)
            source.Pause();
    }

    void UnPauseMainMusicSource(AudioSource source)
    {
        if (source && source.clip)
            source.UnPause();
    }

    bool IsMainMusicPlaying()
    {
        return (musicSrc && musicSrc.isPlaying) ||
               (musicTransitionSrc && musicTransitionSrc.isPlaying);
    }

    float MainMusicFullVolume => masterVolume * musicVolume;

    void SetMainMusicSourceVolumeScale(AudioSource source, float scale)
    {
        scale = Mathf.Clamp01(scale);

        if (source == musicSrc)
            musicSrcVolumeScale = scale;
        else if (source == musicTransitionSrc)
            musicTransitionSrcVolumeScale = scale;

        if (source)
            source.volume = MainMusicFullVolume * scale;
    }

    float GetMainMusicSourceVolumeScale(AudioSource source)
    {
        if (source == musicTransitionSrc)
            return musicTransitionSrcVolumeScale;

        return musicSrcVolumeScale;
    }

    void ApplyMainMusicSourceVolumes()
    {
        if (musicSrc)
            musicSrc.volume = MainMusicFullVolume * musicSrcVolumeScale;

        if (musicTransitionSrc)
            musicTransitionSrc.volume = MainMusicFullVolume * musicTransitionSrcVolumeScale;
    }

    void SetPauseMusicVolumeScale(float scale)
    {
        pauseMusicVolumeScale = Mathf.Clamp01(scale);
        ApplyPauseMusicVolume();
    }

    void ApplyPauseMusicVolume()
    {
        if (pauseMusicSrc)
            pauseMusicSrc.volume = masterVolume * musicVolume * pauseMusicVolumeScale;
    }

    void StopMainMusicFadeCoroutine()
    {
        if (mainMusicFadeCo == null)
            return;

        StopCoroutine(mainMusicFadeCo);
        mainMusicFadeCo = null;
    }

    void StopPauseMainMusicFadeCoroutine()
    {
        if (pauseMainMusicFadeCo == null)
            return;

        StopCoroutine(pauseMainMusicFadeCo);
        pauseMainMusicFadeCo = null;
    }

    void StopSpecialAbilityMusicFadeCoroutine()
    {
        if (specialAbilityMusicFadeCo == null)
            return;

        StopCoroutine(specialAbilityMusicFadeCo);
        specialAbilityMusicFadeCo = null;
    }

    void PrepareMainMusicTransition()
    {
        StopMainMusicFadeCoroutine();
        StopPauseMainMusicFadeCoroutine();
        StopSpecialAbilityMusicFadeCoroutine();
        pauseFadedMainMusic = false;
        specialAbilityPausedMainMusic = false;
    }

    void CrossfadeMainMusicTo(AudioClip clip, bool loop, float targetScale, float duration)
    {
        if (!clip)
            return;

        PrepareMainMusicTransition();
        mainMusicFadeCo = StartCoroutine(CoCrossfadeMainMusicTo(clip, loop, Mathf.Clamp01(targetScale), duration));
    }

    IEnumerator CoCrossfadeMainMusicTo(AudioClip clip, bool loop, float targetScale, float duration)
    {
        AudioSource from = ResolveActiveMusicSource();
        if (from && from.clip == clip)
        {
            activeMusicSrc = from;
            from.loop = loop;

            if (!from.isPlaying)
                from.Play();

            yield return CoFadeMainMusicSourceScale(from, targetScale, duration);
            mainMusicFadeCo = null;
            yield break;
        }

        AudioSource to = from == musicSrc ? musicTransitionSrc : musicSrc;
        if (!to)
            to = musicSrc ? musicSrc : musicTransitionSrc;

        if (!to)
        {
            mainMusicFadeCo = null;
            yield break;
        }

        to.Stop();
        to.clip = clip;
        to.loop = loop;
        to.time = 0f;
        SetMainMusicSourceVolumeScale(to, 0f);
        to.Play();
        activeMusicSrc = to;

        float fadeDuration = GetGeneralMusicCrossfadeDuration(from ? from.clip : null, clip, duration);
        float fromStartScale = from ? GetMainMusicSourceVolumeScale(from) : 0f;
        float t = 0f;

        while (t < fadeDuration && to && to.clip == clip)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeDuration));

            if (from)
                SetMainMusicSourceVolumeScale(from, Mathf.Lerp(fromStartScale, 0f, k));

            SetMainMusicSourceVolumeScale(to, Mathf.Lerp(0f, targetScale, k));
            yield return null;
        }

        if (to && to.clip == clip)
            SetMainMusicSourceVolumeScale(to, targetScale);

        if (from && from != to)
        {
            StopMainMusicSource(from);
            SetMainMusicSourceVolumeScale(from, 0f);
        }

        mainMusicFadeCo = null;
    }

    IEnumerator CoFadeMainMusicSourceScale(AudioSource source, float targetScale, float duration)
    {
        if (!source)
            yield break;

        float fadeDuration = Mathf.Max(0.01f, duration);
        float startScale = GetMainMusicSourceVolumeScale(source);
        float endScale = Mathf.Clamp01(targetScale);
        float t = 0f;

        while (t < fadeDuration && source)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeDuration));
            SetMainMusicSourceVolumeScale(source, Mathf.Lerp(startScale, endScale, k));
            yield return null;
        }

        if (source)
            SetMainMusicSourceVolumeScale(source, endScale);
    }

    void FadeOutMainMusic(float duration)
    {
        PrepareMainMusicTransition();
        mainMusicFadeCo = StartCoroutine(CoFadeOutMainMusic(duration));
    }

    IEnumerator CoFadeOutMainMusic(float duration)
    {
        float fadeDuration = Mathf.Max(0.01f, duration);
        float musicStart = GetMainMusicSourceVolumeScale(musicSrc);
        float transitionStart = GetMainMusicSourceVolumeScale(musicTransitionSrc);
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeDuration));
            SetMainMusicSourceVolumeScale(musicSrc, Mathf.Lerp(musicStart, 0f, k));
            SetMainMusicSourceVolumeScale(musicTransitionSrc, Mathf.Lerp(transitionStart, 0f, k));
            yield return null;
        }

        StopMainMusicSource(musicSrc);
        StopMainMusicSource(musicTransitionSrc);
        activeMusicSrc = musicSrc;
        SetMainMusicSourceVolumeScale(musicSrc, 1f);
        SetMainMusicSourceVolumeScale(musicTransitionSrc, 0f);
        mainMusicFadeCo = null;
    }

    void StartSpecialAbilityMainMusicFade(bool pausing)
    {
        StopSpecialAbilityMusicFadeCoroutine();
        StopMainMusicFadeCoroutine();
        StopPauseMainMusicFadeCoroutine();
        pauseFadedMainMusic = false;
        specialAbilityMusicFadeCo = StartCoroutine(CoSpecialAbilityMainMusicFade(pausing));
    }

    IEnumerator CoSpecialAbilityMainMusicFade(bool pausing)
    {
        if (pausing)
        {
            float fadeDuration = Mathf.Max(0.01f, specialAbilityMusicFadeSeconds);
            float musicStart = GetMainMusicSourceVolumeScale(musicSrc);
            float transitionStart = GetMainMusicSourceVolumeScale(musicTransitionSrc);
            float musicTarget = musicStart * Mathf.Clamp01(specialAbilityBackgroundMusicVolumeScale);
            float transitionTarget = transitionStart * Mathf.Clamp01(specialAbilityBackgroundMusicVolumeScale);
            float t = 0f;

            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeDuration));
                SetMainMusicSourceVolumeScale(musicSrc, Mathf.Lerp(musicStart, musicTarget, k));
                SetMainMusicSourceVolumeScale(musicTransitionSrc, Mathf.Lerp(transitionStart, transitionTarget, k));
                yield return null;
            }

            SetMainMusicSourceVolumeScale(musicSrc, musicTarget);
            SetMainMusicSourceVolumeScale(musicTransitionSrc, transitionTarget);
        }
        else
        {
            UnPauseMainMusicSource(musicSrc);
            UnPauseMainMusicSource(musicTransitionSrc);

            float fadeDuration = Mathf.Max(0.01f, specialAbilityMusicFadeSeconds);
            float musicStart = GetMainMusicSourceVolumeScale(musicSrc);
            float transitionStart = GetMainMusicSourceVolumeScale(musicTransitionSrc);
            float t = 0f;

            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeDuration));
                SetMainMusicSourceVolumeScale(musicSrc, Mathf.Lerp(musicStart, specialAbilityMusicSrcResumeScale, k));
                SetMainMusicSourceVolumeScale(musicTransitionSrc, Mathf.Lerp(transitionStart, specialAbilityMusicTransitionResumeScale, k));
                yield return null;
            }

            SetMainMusicSourceVolumeScale(musicSrc, specialAbilityMusicSrcResumeScale);
            SetMainMusicSourceVolumeScale(musicTransitionSrc, specialAbilityMusicTransitionResumeScale);
            specialAbilityPausedMainMusic = false;
        }

        specialAbilityMusicFadeCo = null;
    }

    void FadeMainMusicForPause(bool fadeOut)
    {
        if (fadeOut && pauseFadedMainMusic)
            return;

        if (!fadeOut && !pauseFadedMainMusic)
            return;

        if (pauseMainMusicFadeCo != null)
        {
            StopCoroutine(pauseMainMusicFadeCo);
            pauseMainMusicFadeCo = null;
        }

        StopMainMusicFadeCoroutine();

        if (fadeOut)
        {
            pauseFadedMainMusic = IsMainMusicPlaying();
            pauseMusicSrcResumeScale = GetMainMusicSourceVolumeScale(musicSrc);
            pauseMusicTransitionResumeScale = GetMainMusicSourceVolumeScale(musicTransitionSrc);
            pauseMainMusicFadeCo = StartCoroutine(CoFadeMainMusicForPause(0f, 0f));
            return;
        }

        pauseFadedMainMusic = false;
        pauseMainMusicFadeCo = StartCoroutine(CoFadeMainMusicForPause(pauseMusicSrcResumeScale, pauseMusicTransitionResumeScale));
    }

    IEnumerator CoFadeMainMusicForPause(float musicTargetScale, float transitionTargetScale)
    {
        bool fadingOut = musicTargetScale <= 0f && transitionTargetScale <= 0f;
        if (!fadingOut)
        {
            UnPauseMainMusicSource(musicSrc);
            UnPauseMainMusicSource(musicTransitionSrc);
        }

        float fadeDuration = Mathf.Max(0.01f, pauseMusicFadeSeconds);
        float musicStart = GetMainMusicSourceVolumeScale(musicSrc);
        float transitionStart = GetMainMusicSourceVolumeScale(musicTransitionSrc);
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeDuration));
            SetMainMusicSourceVolumeScale(musicSrc, Mathf.Lerp(musicStart, musicTargetScale, k));
            SetMainMusicSourceVolumeScale(musicTransitionSrc, Mathf.Lerp(transitionStart, transitionTargetScale, k));
            yield return null;
        }

        SetMainMusicSourceVolumeScale(musicSrc, musicTargetScale);
        SetMainMusicSourceVolumeScale(musicTransitionSrc, transitionTargetScale);

        if (fadingOut)
        {
            PauseMainMusicSource(musicSrc);
            PauseMainMusicSource(musicTransitionSrc);
        }

        pauseMainMusicFadeCo = null;
    }

    void FadePauseMusicTo(float targetScale, bool stopAtEnd)
    {
        if (!pauseMusicSrc)
            return;

        if (pauseMusicFadeCo != null)
            StopCoroutine(pauseMusicFadeCo);

        pauseMusicFadeCo = StartCoroutine(CoFadePauseMusicTo(targetScale, stopAtEnd));
    }

    IEnumerator CoFadePauseMusicTo(float targetScale, bool stopAtEnd)
    {
        float fadeDuration = Mathf.Max(0.01f, pauseMusicFadeSeconds);
        float startScale = pauseMusicVolumeScale;
        float endScale = Mathf.Clamp01(targetScale);
        float t = 0f;

        while (t < fadeDuration && pauseMusicSrc)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeDuration));
            SetPauseMusicVolumeScale(Mathf.Lerp(startScale, endScale, k));
            yield return null;
        }

        SetPauseMusicVolumeScale(endScale);

        if (stopAtEnd && pauseMusicSrc)
        {
            pauseMusicSrc.Stop();
            pauseMusicSrc.clip = null;
            SetPauseMusicVolumeScale(1f);
        }

        pauseMusicFadeCo = null;
    }

    public void PlaySFX(AudioClip clip, float vol = 1f, float pitch = 1f, bool jitter = true)
    {
        if (!clip) return;
        AudioSource source = GetNextSfxOneShotSource();
        if (!source) return;

        float p = pitch;
        if (jitter) p += Random.Range(-sfxPitchJitter, sfxPitchJitter);
        source.pitch = Mathf.Clamp(p, 0.5f, 2f);
        source.PlayOneShot(clip, vol);
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

        ApplyMainMusicSourceVolumes();
        ApplySfxOneShotSourceVolume();
        uiSfxSrc.volume = masterVolume * sfxVolume;
        uiLoopSfxSrc.volume = masterVolume * sfxVolume;
        if (specialAbilityOneShotSrc) specialAbilityOneShotSrc.volume = masterVolume * sfxVolume;
        ApplySpecialAbilityLoopSfxVolume();
        ApplyRainAmbienceVolume();
        ApplyPauseMusicVolume();

        if (save) SettingsStore.SaveVolumes(masterVolume, musicVolume, sfxVolume);
    }

    public void SetMusicVolume(float linear, bool save = true)
    {
        musicVolume = Mathf.Clamp01(linear);

        if (mixer && musicGroup && MixerHasParam("MusicVolume"))
            mixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(0.0001f, musicVolume)) * 20f);

        ApplyMainMusicSourceVolumes();
        ApplyPauseMusicVolume();

        if (save) SettingsStore.SaveVolumes(masterVolume, musicVolume, sfxVolume);
    }

    public void SetSFXVolume(float linear, bool save = true)
    {
        sfxVolume = Mathf.Clamp01(linear);

        if (mixer && sfxGroup && MixerHasParam("SFXVolume"))
            mixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(0.0001f, sfxVolume)) * 20f);

        ApplySfxOneShotSourceVolume();
        uiSfxSrc.volume = masterVolume * sfxVolume;
        uiLoopSfxSrc.volume = masterVolume * sfxVolume;
        if (specialAbilityOneShotSrc) specialAbilityOneShotSrc.volume = masterVolume * sfxVolume;
        ApplySpecialAbilityLoopSfxVolume();
        ApplyRainAmbienceVolume();

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

    public void PlayRoundTransitionAppearSFX(AudioClip clip, float vol = 1f)
    {
        PlayUISFX(clip, vol, pitch: 1f, jitter: false);
    }

    public void PlayRoundTransitionLoopSFX(AudioClip clip, float vol = 1f, float pitch = 1f)
    {
        PlayLoopingUISFX(clip, vol, pitch);
    }

    public void StopRoundTransitionLoopSFX(AudioClip clip = null)
    {
        if (!uiLoopSfxSrc) return;
        if (clip && uiLoopSfxSrc.clip != clip) return;

        StopLoopingUISFX();
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

        rainAmbienceRequestedVolume = Mathf.Clamp01(vol);
        float targetVol = GetRainTargetVolume(rainAmbienceRequestedVolume);
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
            ApplyRainAmbienceVolume();
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
            ApplyRainAmbienceVolume();
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
        if (!clip) return;

        float normalizedVolume = NormalizeSpecialAbilityVolume(vol);
        if (!specialAbilityOneShotSrc)
        {
            PlaySFX(clip, normalizedVolume, pitch: 1f, jitter: false);
            return;
        }

        specialAbilityOneShotSrc.pitch = 1f;
        specialAbilityOneShotSrc.PlayOneShot(clip, normalizedVolume);
    }

    public void PlaySpecialAbilityPopupLoopSFX(AudioClip clip, float vol = 1f, float pitch = 1f)
    {
        if (!clip || specialAbilityLoopSfxSources == null) return;

        if (specialAbilityLoopFadeCo != null)
        {
            StopCoroutine(specialAbilityLoopFadeCo);
            specialAbilityLoopFadeCo = null;
        }

        specialAbilityLoopSfxVolumeScale = Mathf.Min(
            NormalizeSpecialAbilityVolume(vol) * Mathf.Max(0f, specialAbilityLoopVolumeBoost),
            Mathf.Max(0f, specialAbilityLoopMaxVolumeScale));
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

    static float NormalizeSpecialAbilityVolume(float volume)
    {
        if (float.IsNaN(volume) || float.IsInfinity(volume))
            return 1f;

        return Mathf.Clamp01(volume);
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
        PlayerPrefsSaveScheduler.QueueSave();

        if (AudioListener.pause)
        {
            restartContextOnUnpause = true;

            if (pauseMusicSrc)
                PlayPauseMusic();

            return;
        }

        RestartContextMusic();

        if (pauseMusicSrc && pauseMusicSrc.isPlaying)
            PlayPauseMusic();
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
        StopLevelMusicInternal(fadeOutMusic: false);

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

        StopLevelMusicInternal(fadeOutMusic: false);

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
        StopLevelMusicInternal(fadeOutMusic: true);
    }

    void StopLevelMusicInternal(bool fadeOutMusic)
    {
        if (musicChainCo != null) { StopCoroutine(musicChainCo); musicChainCo = null; }
        currentPool = null;
        if (fadeOutMusic)
            StopMusic();
    }

    public void PlayPauseMusic()
    {
        // Pick EDM/Metal/both clip
        var clip = PickPauseClipByMode();
        FadeMainMusicForPause(fadeOut: true);
        if (!clip) return;

        if (pauseMusicSrc.isPlaying && pauseMusicSrc.clip == clip)
        {
            FadePauseMusicTo(1f, stopAtEnd: false);
            return;
        }

        pauseMusicSrc.clip = clip;
        pauseMusicSrc.loop = true;
        pauseMusicSrc.time = 0f;
        SetPauseMusicVolumeScale(0f);
        pauseMusicSrc.Play();
        FadePauseMusicTo(1f, stopAtEnd: false);
    }

    public void StopPauseMusic()
    {
        FadeMainMusicForPause(fadeOut: false);

        if (pauseMusicSrc && pauseMusicSrc.isPlaying)
            FadePauseMusicTo(0f, stopAtEnd: true);
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
        StopLevelMusicInternal(fadeOutMusic: false);
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

            while (levelMusicActive && ShouldHoldLevelMusicChain())
                yield return null;

            if (!levelMusicActive)
                yield break;

            // Save last clip for this context (boss vs gameplay)
            lastClip = next;
            if (isBoss) lastBossClip = lastClip;
            else lastGameplayClip = lastClip;

            if (ResolveActiveMusicSource())
                yield return CrossfadeToLevelMusicClip(next);
            else
                PlayLevelMusicClipImmediate(next);

            yield return WaitForLevelMusicTransitionPoint(activeMusicSrc, next);
        }
    }

    void PlayLevelMusicClipImmediate(AudioClip clip)
    {
        if (!clip) return;

        PrepareMainMusicTransition();

        AudioSource source = activeMusicSrc ? activeMusicSrc : musicSrc;
        AudioSource other = source == musicSrc ? musicTransitionSrc : musicSrc;

        StopMainMusicSource(other);
        SetMainMusicSourceVolumeScale(other, 0f);

        source.Stop();
        source.clip = clip;
        source.loop = false;
        source.time = 0f;
        SetMainMusicSourceVolumeScale(source, 0f);
        source.Play();

        activeMusicSrc = source;
        mainMusicFadeCo = StartCoroutine(CoFadeMainMusicSourceScale(source, 1f, GetLevelMusicCrossfadeDuration(null, clip)));
    }

    IEnumerator CrossfadeToLevelMusicClip(AudioClip next)
    {
        if (!next)
            yield break;

        while (levelMusicActive && ShouldHoldLevelMusicChain())
            yield return null;

        if (!levelMusicActive)
            yield break;

        PrepareMainMusicTransition();

        AudioSource from = ResolveActiveMusicSource();
        if (!from || !from.clip)
        {
            PlayLevelMusicClipImmediate(next);
            yield break;
        }

        AudioSource to = from == musicSrc ? musicTransitionSrc : musicSrc;
        if (!to || to == from)
        {
            PlayLevelMusicClipImmediate(next);
            yield break;
        }

        to.Stop();
        to.clip = next;
        to.loop = false;
        to.time = 0f;
        SetMainMusicSourceVolumeScale(to, 0f);
        to.Play();

        activeMusicSrc = to;

        float duration = GetLevelMusicCrossfadeDuration(from.clip, next);
        if (from.clip)
            duration = Mathf.Min(duration, Mathf.Max(0.05f, from.clip.length - from.time));

        float fromStartScale = GetMainMusicSourceVolumeScale(from);
        float t = 0f;

        while (t < duration && levelMusicActive && to && to.clip == next)
        {
            if (ShouldHoldLevelMusicChain())
            {
                yield return null;
                continue;
            }

            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            SetMainMusicSourceVolumeScale(from, Mathf.Lerp(fromStartScale, 0f, k));
            SetMainMusicSourceVolumeScale(to, k);
            yield return null;
        }

        if (to && to.clip == next)
            SetMainMusicSourceVolumeScale(to, 1f);

        if (from)
        {
            StopMainMusicSource(from);
            SetMainMusicSourceVolumeScale(from, 0f);
        }
    }

    AudioSource ResolveActiveMusicSource()
    {
        if (activeMusicSrc && activeMusicSrc.clip)
            return activeMusicSrc;

        if (musicSrc && musicSrc.clip)
            return musicSrc;

        if (musicTransitionSrc && musicTransitionSrc.clip)
            return musicTransitionSrc;

        return null;
    }

    IEnumerator WaitForLevelMusicTransitionPoint(AudioSource source, AudioClip clip)
    {
        if (!source || !clip)
            yield break;

        float transitionTime = Mathf.Max(0f, clip.length - GetLevelMusicCrossfadeDuration(clip, null));

        while (levelMusicActive && source && source.clip == clip && source.time < transitionTime)
        {
            if (ShouldHoldLevelMusicChain())
            {
                yield return null;
                continue;
            }

            if (!source.isPlaying)
                yield break;

            yield return null;
        }
    }

    bool ShouldHoldLevelMusicChain()
    {
        return specialAbilityPausedMainMusic ||
               AudioListener.pause ||
               applicationPaused ||
               !applicationHasFocus ||
               holdMusicChainForFocusResume;
    }

    void HoldMusicChainForFocusResumeFrame()
    {
        holdMusicChainForFocusResume = true;

        if (focusResumeHoldCo != null)
            StopCoroutine(focusResumeHoldCo);

        focusResumeHoldCo = StartCoroutine(CoReleaseFocusResumeHold());
    }

    IEnumerator CoReleaseFocusResumeHold()
    {
        yield return null;
        holdMusicChainForFocusResume = false;
        focusResumeHoldCo = null;
    }

    float GetLevelMusicCrossfadeDuration(AudioClip fromClip, AudioClip toClip)
    {
        float duration = Mathf.Max(0.05f, levelMusicCrossfadeSeconds);

        if (fromClip)
            duration = Mathf.Min(duration, Mathf.Max(0.05f, fromClip.length * 0.5f));

        if (toClip)
            duration = Mathf.Min(duration, Mathf.Max(0.05f, toClip.length * 0.5f));

        return duration;
    }

    float GetGeneralMusicCrossfadeDuration(AudioClip fromClip, AudioClip toClip, float requestedDuration = -1f)
    {
        float duration = requestedDuration > 0f
            ? requestedDuration
            : Mathf.Max(0.05f, musicCrossfadeSeconds);

        if (fromClip)
            duration = Mathf.Min(duration, Mathf.Max(0.05f, fromClip.length * 0.5f));

        if (toClip)
            duration = Mathf.Min(duration, Mathf.Max(0.05f, toClip.length * 0.5f));

        return duration;
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
        return masterVolume * sfxVolume * Mathf.Clamp01(rainAmbienceVolumeScale) * Mathf.Clamp01(vol);
    }

    void ApplyRainAmbienceVolume()
    {
        if (!ambienceLoopSrc) return;
        ambienceLoopSrc.volume = GetRainTargetVolume(rainAmbienceRequestedVolume);
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
