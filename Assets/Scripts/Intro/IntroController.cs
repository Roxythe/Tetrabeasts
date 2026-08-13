using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using TMPro;

public class IntroController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public TMP_Text pressAnyKeyText;
    public string titleSceneName = "TitleScene";

    [Header("Cursor")]
    public UICursorController uiCursorController;

    bool firstInputShown = false;
    bool skippingEnabled = false;

    Coroutine _pressTextFadeCR;

    [Header("Press Any Key Fade")]
    public float fadeDuration = 0.6f;
    public float holdDuration = 0.15f;

    [Header("Skip Prompt Auto-Hide")]
    public float skipPromptIdleHideSeconds = 2.5f;
    float _lastInputTime = -999f;

    public GameObject blackOverlay;
    public AudioSource videoAudioSource;
    [Range(0f, 1f)] public float introVideoVolume = 1f;

    bool _firstFrameShown = false;
    float _savedVideoVolume = 1f;

    [Header("Fail Safe")]
    public float prepareTimeoutSeconds = 3f;

    bool _loadingTitle = false;
    Coroutine _prepareTimeoutCR;
    Coroutine _playPreparedVideoCR;
    bool _allowVideoFrameReveal = false;

    void Awake()
    {
        SuspendVideoUntilLoadingCompletes();
    }

    void Start()
    {
        SettingsStore.ApplySavedVolumesToAudio();

        if (uiCursorController)
        {
            uiCursorController.SetScale(SettingsStore.LoadCursorScale());
            uiCursorController.SetVisible(false);
        }

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        bool hasSelections = SelectedCharacterStore.HasSavedSelection() || SelectedMonstersStore.HasSavedSelection();

        if (SettingsStore.LoadSkipIntroEnabled() && hasSelections)
        {
            LoadTitle();
            return;
        }

        HideSkipPromptInstant();

        if (!videoPlayer)
        {
            Debug.LogError("IntroController: videoPlayer is not assigned. Loading title scene.");
            LoadTitle();
            return;
        }

        SuspendVideoUntilLoadingCompletes();

        if (videoPlayer.clip == null && string.IsNullOrWhiteSpace(videoPlayer.url))
        {
            Debug.LogWarning("IntroController: No intro video clip or URL found. Loading title scene.");
            LoadTitle();
            return;
        }

        _firstFrameShown = false;
        _allowVideoFrameReveal = false;
        if (blackOverlay) blackOverlay.SetActive(true);
        LoadingScreen.Show();

        ConfigureIntroVideoAudio();

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;

        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.loopPointReached += OnVideoFinished;

        videoPlayer.sendFrameReadyEvents = false;
        videoPlayer.frameReady -= OnFrameReady;
        videoPlayer.frameReady += OnFrameReady;

        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.errorReceived += OnVideoError;

        videoPlayer.Prepare();

        if (_prepareTimeoutCR != null) StopCoroutine(_prepareTimeoutCR);
        _prepareTimeoutCR = StartCoroutine(PrepareTimeout());
    }

    void Update()
    {
        // Auto-hide and reset skip prompt if idle for too long after first input
        if (firstInputShown && !_loadingTitle)
        {
            if (Time.unscaledTime - _lastInputTime >= Mathf.Max(0.1f, skipPromptIdleHideSeconds))
            {
                ResetSkipPromptState(); // Hides and resets conditions
            }
        }

        if (!Input.anyKeyDown)
            return;

        _lastInputTime = Time.unscaledTime; // Refresh idle timer on any key

        // First key press: show skip prompt
        if (!firstInputShown)
        {
            firstInputShown = true;
            skippingEnabled = true;

            if (_pressTextFadeCR != null) StopCoroutine(_pressTextFadeCR);
            _pressTextFadeCR = StartCoroutine(FadePressAnyKeyLoop());

            return;
        }

        // Second key press to skip
        if (skippingEnabled)
        {
            LoadTitle();
        }
    }

    void ResetSkipPromptState()
    {
        firstInputShown = false;
        skippingEnabled = false;
        _lastInputTime = -999f;
        HideSkipPromptInstant();
    }

    void HideSkipPromptInstant()
    {
        if (_pressTextFadeCR != null)
        {
            StopCoroutine(_pressTextFadeCR);
            _pressTextFadeCR = null;
        }

        if (pressAnyKeyText)
        {
            var c = pressAnyKeyText.color;
            c.a = 0f;
            pressAnyKeyText.color = c;
        }
    }

    void OnVideoFinished(VideoPlayer vp) => LoadTitle();

    void LoadTitle()
    {
        if (_loadingTitle) return;
        _loadingTitle = true;
        _allowVideoFrameReveal = false;

        HideSkipPromptInstant(); // Ensures prompt/coroutine fully stops

        if (videoPlayer)
        {
            videoPlayer.frameReady -= OnFrameReady;
            videoPlayer.sendFrameReadyEvents = false;
            videoPlayer.Stop();
        }

        if (videoAudioSource)
        {
            videoAudioSource.Stop();
            videoAudioSource.volume = _savedVideoVolume;
        }

        if (_playPreparedVideoCR != null)
        {
            StopCoroutine(_playPreparedVideoCR);
            _playPreparedVideoCR = null;
        }

        if (blackOverlay) blackOverlay.SetActive(false);

        if (!LoadingScreen.LoadSceneAsync(titleSceneName))
        {
            LoadingScreen.Hide();
            Debug.LogError("IntroController: titleSceneName is empty or not set.");
        }
    }

    System.Collections.IEnumerator FadePressAnyKeyLoop()
    {
        while (true)
        {
            yield return FadeTextAlpha(0f, 1f, fadeDuration);
            yield return new WaitForSecondsRealtime(holdDuration);

            yield return FadeTextAlpha(1f, 0f, fadeDuration);
            yield return new WaitForSecondsRealtime(holdDuration);
        }
    }

    System.Collections.IEnumerator FadeTextAlpha(float from, float to, float duration)
    {
        if (!pressAnyKeyText) yield break;

        float t = 0f;
        Color c = pressAnyKeyText.color;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            c.a = a;
            pressAnyKeyText.color = c;
            yield return null;
        }

        c.a = to;
        pressAnyKeyText.color = c;
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        if (_loadingTitle) return;

        if (_prepareTimeoutCR != null)
        {
            StopCoroutine(_prepareTimeoutCR);
            _prepareTimeoutCR = null;
        }

        if (_playPreparedVideoCR != null)
            StopCoroutine(_playPreparedVideoCR);

        _playPreparedVideoCR = StartCoroutine(PlayPreparedVideoAfterLoadingMinimum(vp));
    }

    void ConfigureIntroVideoAudio()
    {
        if (!videoPlayer) return;

        if (!videoAudioSource)
            videoAudioSource = videoPlayer.GetComponent<AudioSource>();

        if (!videoAudioSource)
            videoAudioSource = videoPlayer.gameObject.AddComponent<AudioSource>();

        videoAudioSource.playOnAwake = false;
        videoAudioSource.loop = false;
        videoAudioSource.spatialBlend = 0f;
        videoAudioSource.Stop();

        if (AudioManager.I)
            AudioManager.I.ConfigureExternalMusicSource(videoAudioSource, introVideoVolume);
        else
            videoAudioSource.volume = Mathf.Clamp01(introVideoVolume);

        _savedVideoVolume = videoAudioSource.volume;
        videoAudioSource.volume = 0f;

        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);
    }

    System.Collections.IEnumerator PlayPreparedVideoAfterLoadingMinimum(VideoPlayer vp)
    {
        if (vp)
        {
            vp.Pause();
            vp.skipOnDrop = false;
            ResetVideoToStart(vp);
        }

        LoadingScreen.RestartMinimumVisibleTimer();
        yield return LoadingScreen.HideAndWaitUntilInactive();

        _playPreparedVideoCR = null;

        if (_loadingTitle || !vp)
            yield break;

        ResetVideoToStart(vp);
        vp.skipOnDrop = false;
        vp.frameReady -= OnFrameReady;
        vp.frameReady += OnFrameReady;
        vp.sendFrameReadyEvents = true;
        _allowVideoFrameReveal = true;
        vp.Play();
    }

    void OnFrameReady(VideoPlayer vp, long frameIdx)
    {
        if (!_allowVideoFrameReveal) return;
        if (_firstFrameShown) return;

        _firstFrameShown = true;
        _allowVideoFrameReveal = false;

        if (blackOverlay) blackOverlay.SetActive(false);

        if (videoAudioSource)
            videoAudioSource.volume = _savedVideoVolume;

        vp.frameReady -= OnFrameReady;
        vp.sendFrameReadyEvents = false;
    }

    static void ResetVideoToStart(VideoPlayer vp)
    {
        if (vp && vp.canSetTime)
            vp.time = 0d;
    }

    System.Collections.IEnumerator PrepareTimeout()
    {
        float t = 0f;

        while (t < prepareTimeoutSeconds)
        {
            if (_loadingTitle) yield break;
            if (videoPlayer == null) yield break;
            if (videoPlayer.isPrepared) yield break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning("Video prepare timed out. Loading title scene.");
        LoadTitle();
    }

    void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogWarning($"VideoPlayer error: {message}. Loading title scene.");
        LoadTitle();
    }

    void SuspendVideoUntilLoadingCompletes()
    {
        if (!videoPlayer) return;

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        // Cold decoder startup can otherwise drop the opening frames before the overlay/audio gate opens.
        videoPlayer.skipOnDrop = false;
        videoPlayer.sendFrameReadyEvents = false;

        videoPlayer.Stop();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        if (uiCursorController)
            uiCursorController.SetVisible(false);
    }
}
