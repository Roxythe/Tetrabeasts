using UnityEngine;
using UnityEngine.UI;

// Samples the sprite clip and its explosion cues on one unscaled timeline.
// Each portrait owns its own timeline and audio voice.
public sealed class SquadLevelUpFireworks : MonoBehaviour
{
    GameModeAssets assets;
    GameObject artwork;
    AudioSource sound;
    RectTransform portrait;
    RectTransform artworkRect;
    float elapsed;
    int nextCue;
    bool playing;

    public static SquadLevelUpFireworks Create(Image portraitImage)
    {
        var config = GameModeAssets.Load();
        if (!portraitImage || !config || !config.squadFireworkPrefab || !config.squadFireworkAnimation) return null;
        var root = new GameObject("Squad Level Up Fireworks", typeof(RectTransform), typeof(LayoutElement), typeof(RectMask2D));
        root.transform.SetParent(portraitImage.transform.parent, false);
        root.GetComponent<LayoutElement>().ignoreLayout = true;
        root.transform.SetSiblingIndex(portraitImage.transform.GetSiblingIndex());
        var effect = root.AddComponent<SquadLevelUpFireworks>();
        effect.assets = config;
        effect.portrait = portraitImage.rectTransform;
        // Leave layout and scroll bounds alone; extend only the viewport's stencil.
        var scroll = portraitImage.GetComponentInParent<ScrollRect>();
        if (scroll && scroll.viewport)
        {
            float headroom = portraitImage.rectTransform.rect.height * .5f *
                Mathf.Abs(portraitImage.transform.lossyScale.y) / Mathf.Max(.0001f, Mathf.Abs(scroll.viewport.lossyScale.y));
            var mask = scroll.viewport.GetComponent<Mask>();
            if (mask && mask.graphic)
            {
                var padding = mask.GetComponent<SquadFireworkMaskHeadroom>();
                if (!padding) padding = mask.gameObject.AddComponent<SquadFireworkMaskHeadroom>();
                padding.ExpandTop(headroom);
            }
            else if (scroll.viewport.TryGetComponent<RectMask2D>(out var rectangularMask))
            {
                var padding = rectangularMask.padding;
                padding.w = Mathf.Min(padding.w, -headroom);
                rectangularMask.padding = padding;
            }
        }
        effect.artwork = Instantiate(config.squadFireworkPrefab, root.transform, false);
        foreach (var animator in effect.artwork.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        foreach (var graphic in effect.artwork.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        effect.artworkRect = (RectTransform)effect.artwork.transform;
        effect.artworkRect.anchorMin = effect.artworkRect.anchorMax = new Vector2(.5f, .5f);
        effect.artworkRect.pivot = new Vector2(.5f, .5f);
        root.GetComponent<RectMask2D>().softness = new Vector2Int(0, 5);
        effect.sound = root.AddComponent<AudioSource>();
        effect.sound.playOnAwake = false; effect.sound.ignoreListenerPause = true; effect.sound.spatialBlend = 0;
        if (AudioManager.I) effect.sound.outputAudioMixerGroup = AudioManager.I.sfxGroup;
        effect.Align();
        return effect;
    }

    public void Begin()
    {
        // Multiple level gains in one XP distribution share this celebration.
        if (playing && isActiveAndEnabled) return;
        playing = true;
        elapsed = 0; nextCue = 0;
        sound.Stop();
        gameObject.SetActive(true);
        RefreshVolume();
        assets.squadFireworkAnimation.SampleAnimation(artwork, 0);
    }
    public void End() { gameObject.SetActive(false); }
    void OnDisable() { playing = false; if (sound) sound.Stop(); }
    void LateUpdate()
    {
        if (!playing) return;
        if (!portrait || !portrait.gameObject.activeInHierarchy) { End(); return; }
        Align();
        RefreshVolume();
        Advance(Time.unscaledDeltaTime);
    }
    void Align()
    {
        var root = (RectTransform)transform;
        Vector2 size = portrait.rect.size * 1.9f;
        // Keep the original artwork size/position. Raise only its visible window:
        // the lower 40% contains the launch trails, above it are the explosions.
        float crop = size.y * .4f;
        root.anchorMin = portrait.anchorMin; root.anchorMax = portrait.anchorMax;
        root.pivot = portrait.pivot;
        root.sizeDelta = portrait.sizeDelta + portrait.rect.size * .9f - Vector2.up * crop;
        root.anchoredPosition = portrait.anchoredPosition + Vector2.up * crop * (1f - root.pivot.y);
        root.localScale = portrait.localScale;
        artworkRect.sizeDelta = size;
        artworkRect.anchoredPosition = Vector2.down * crop * .5f;
        root.SetSiblingIndex(portrait.GetSiblingIndex() - (root.GetSiblingIndex() < portrait.GetSiblingIndex() ? 1 : 0));
    }
    void RefreshVolume()
    {
        // AudioSource.volume clamps at 1. Apply the 120% boost on each voice instead.
        sound.volume = AudioManager.I ? AudioManager.I.masterVolume * AudioManager.I.sfxVolume : .5f;
    }
    void Advance(float delta)
    {
        float duration = Mathf.Max(.01f, assets.squadFireworkAnimation.length);
        elapsed += delta;
        while (elapsed >= duration) { elapsed -= duration; nextCue = 0; }
        assets.squadFireworkAnimation.SampleAnimation(artwork, Mathf.Min(elapsed, duration - .001f));
        var cues = assets.squadFireworkExplosionTimes;
        while (cues != null && nextCue < cues.Length && cues[nextCue] <= elapsed)
        {
            if (sound && assets.squadFireworkSfx)
            {
                sound.PlayOneShot(assets.squadFireworkSfx, assets.squadFireworkVolume);
            }
            nextCue++;
        }
    }
}
