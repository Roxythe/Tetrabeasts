using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Explicitly samples the authored opening clip so reverse playback, holding the
// closed frame and one-shot opening also work while Time.timeScale is zero.
public sealed class MarathonCurtainTransition : MonoBehaviour
{
    GameObject artwork;
    AnimationClip opening;
    AudioSource pulley;
    bool pulleyStarted;
    public bool AnimationPaused { get; set; }
    public bool IsMoving { get; private set; }
    public bool IsClosed { get; private set; }
    public bool VisualsReady { get; private set; }

    public static MarathonCurtainTransition Create(Canvas parent, GameObject prefab, AnimationClip clip)
    {
        var root = new GameObject("Marathon level curtain", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        root.transform.SetParent(parent.transform, false);
        var rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        var canvas = root.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32760;
        var curtain = root.AddComponent<MarathonCurtainTransition>();
        curtain.artwork = Instantiate(prefab, root.transform, false);
        foreach (var animator in curtain.artwork.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        curtain.opening = clip;
        var assets = GameModeAssets.Load();
        if (assets && assets.curtainPulleySfx)
        {
            curtain.pulley = root.AddComponent<AudioSource>();
            curtain.pulley.clip = assets.curtainPulleySfx;
            curtain.pulley.loop = true;
            curtain.pulley.playOnAwake = false;
            curtain.pulley.ignoreListenerPause = true;
            curtain.pulley.spatialBlend = 0;
            if (AudioManager.I) curtain.pulley.outputAudioMixerGroup = AudioManager.I.sfxGroup;
        }
        root.SetActive(false);
        return curtain;
    }

    public IEnumerator Close()
    {
        if (pulley) pulley.Stop();
        pulleyStarted = false;
        AnimationPaused = false;
        VisualsReady = false; IsClosed = false;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        yield return Play(reverse: true);
        IsClosed = true;
    }

    public void SignalVisualsReady() => VisualsReady = true;

    public IEnumerator Open()
    {
        while (!VisualsReady) yield return null;
        yield return Play(reverse: false);
        IsClosed = false;
        gameObject.SetActive(false);
    }

    IEnumerator Play(bool reverse)
    {
        // Avoid the loop boundary even if the artist's source clip is set to loop.
        float end = Mathf.Max(0f, opening.length - 0.001f);
        opening.SampleAnimation(artwork, reverse ? end : 0f);
        IsMoving = true;
        try
        {
            for (float elapsed = 0f; elapsed < opening.length;)
            {
                if (AnimationPaused)
                {
                    if (pulley) pulley.Pause();
                    yield return null;
                    continue;
                }
                if (pulley)
                {
                    pulley.volume = AudioManager.I ? AudioManager.I.masterVolume * AudioManager.I.sfxVolume : .5f;
                    if (!pulleyStarted) { pulley.Play(); pulleyStarted = true; }
                    else if (!pulley.isPlaying) pulley.UnPause();
                }
                float t = Mathf.Clamp01(elapsed / opening.length);
                opening.SampleAnimation(artwork, Mathf.Lerp(reverse ? end : 0f, reverse ? 0f : end, t));
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            opening.SampleAnimation(artwork, reverse ? 0f : end);
        }
        finally { IsMoving = false; if (pulley) pulley.Pause(); }
    }

    void OnDisable() { IsMoving = false; if (pulley) pulley.Stop(); pulleyStarted = false; }
}
