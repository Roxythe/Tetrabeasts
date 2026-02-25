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

    void Start()
    {
        if (pressAnyKeyText)
        {
            Color c = pressAnyKeyText.color;
            c.a = 0f;
            pressAnyKeyText.color = c;
        }

        if (!videoPlayer)
        {
            Debug.LogError("IntroController: videoPlayer is not assigned.");
            return;
        }

        // Reduce first-second hitching
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;

        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.loopPointReached += OnVideoFinished;

        videoPlayer.Prepare(); // Prepare video before playing
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
        if (_pressTextFadeCR != null) StopCoroutine(_pressTextFadeCR);

        if (videoPlayer)
            videoPlayer.Stop();

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
        vp.Play();
    }
}