using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using TMPro;

public class IntroController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public TMP_Text pressAnyKeyText;
    public string titleSceneName = "TitleScene";

    bool firstInputShown = false;
    bool skippingEnabled = false;

    Coroutine _pressTextFadeCR;

    [Header("Press Any Key Fade")]
    public float fadeDuration = 0.6f;
    public float holdDuration = 0.15f;

    public GameObject blackOverlay; 
    public AudioSource videoAudioSource; 

    bool _firstFrameShown = false;
    float _savedVideoVolume = 1f;

    [Header("Fail Safe")]
    public float prepareTimeoutSeconds = 3f;

    bool _loadingTitle = false;
    Coroutine _prepareTimeoutCR;

    void Start()
    {
        bool hasSelections = SelectedCharacterStore.HasSavedSelection() || SelectedMonstersStore.HasSavedSelection();

        if (SettingsStore.LoadSkipIntroEnabled() && hasSelections)
        {
            LoadTitle();
            return;
        }

        if (pressAnyKeyText)
        {
            Color c = pressAnyKeyText.color;
            c.a = 0f;
            pressAnyKeyText.color = c;
        }

        if (!videoPlayer)
        {
            Debug.LogError("IntroController: videoPlayer is not assigned. Loading title scene.");
            LoadTitle();
            return;
        }

        // Reduce first-second hitching
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;

        // If there's no assigned clip and no URL, skip intro
        if (videoPlayer.clip == null && string.IsNullOrWhiteSpace(videoPlayer.url))
        {
            Debug.LogWarning("IntroController: No intro video clip or URL found. Loading title scene.");
            LoadTitle();
            return;
        }

        // Show black until video is ready
        _firstFrameShown = false;
        if (blackOverlay) blackOverlay.SetActive(true);

        if (videoAudioSource)
        {
            _savedVideoVolume = videoAudioSource.volume;
            videoAudioSource.volume = 0f;

            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, videoAudioSource);
        }

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;

        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.loopPointReached += OnVideoFinished;

        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.frameReady -= OnFrameReady;
        videoPlayer.frameReady += OnFrameReady;

        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.errorReceived += OnVideoError;

        videoPlayer.Prepare(); // Prepare video before playing

        // Skip intro
        if (_prepareTimeoutCR != null) StopCoroutine(_prepareTimeoutCR);
        _prepareTimeoutCR = StartCoroutine(PrepareTimeout());
    }

    void Update()
    {
        if (!Input.anyKeyDown)
            return;

        // First key pressto show text
        if (!firstInputShown)
        {
            firstInputShown = true;
            skippingEnabled = true;

            // Start pulsing fade loop
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

    void OnVideoFinished(VideoPlayer vp)
    {
        LoadTitle();
    }

    void LoadTitle()
    {
        if (_loadingTitle) return;
        _loadingTitle = true;

        if (_pressTextFadeCR != null) StopCoroutine(_pressTextFadeCR);

        if (videoPlayer)
        {
            videoPlayer.frameReady -= OnFrameReady;
            videoPlayer.sendFrameReadyEvents = false;
            videoPlayer.Stop();
        }

        if (blackOverlay) blackOverlay.SetActive(false);

        SceneManager.LoadScene(titleSceneName);
    }

    System.Collections.IEnumerator FadePressAnyKeyLoop()
    {
        while (true)
        {
            // Fade in
            yield return FadeTextAlpha(0f, 1f, fadeDuration);
            yield return new WaitForSeconds(holdDuration);

            // Fade out
            yield return FadeTextAlpha(1f, 0f, fadeDuration);
            yield return new WaitForSeconds(holdDuration);
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
        if (_prepareTimeoutCR != null)
        {
            StopCoroutine(_prepareTimeoutCR);
            _prepareTimeoutCR = null;
        }

        vp.Play();
    }

    void OnFrameReady(VideoPlayer vp, long frameIdx)
    {
        if (_firstFrameShown) return;
        if (frameIdx < 1) return;

        _firstFrameShown = true;

        // Hide black overlay
        if (blackOverlay) blackOverlay.SetActive(false);

        // Unmute now that playback is stable
        if (videoAudioSource)
            videoAudioSource.volume = _savedVideoVolume;

        vp.frameReady -= OnFrameReady;
        vp.sendFrameReadyEvents = false;
    }

    System.Collections.IEnumerator PrepareTimeout()
    {
        float t = 0f;

        while (t < prepareTimeoutSeconds)
        {
            if (_loadingTitle) yield break;
            if (videoPlayer == null) yield break;

            // If it becomes prepared, stop timing out
            if (videoPlayer.isPrepared) yield break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning("IntroController: Video prepare timed out. Loading title scene.");
        LoadTitle();
    }

    void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogWarning($"IntroController: VideoPlayer error: {message}. Loading title scene.");
        LoadTitle();
    }
}