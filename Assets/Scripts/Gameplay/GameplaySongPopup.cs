using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene-owned, non-interactive gameplay music announcement.</summary>
public sealed class GameplaySongPopup : MonoBehaviour
{
    const float FadeInSeconds = .3f, FadeOutSeconds = .5f, ReadingSeconds = 3f;
    const float ScrollDelay = 1f, ScrollSpeed = 35f, EndReadingSeconds = .75f;
    CanvasGroup group;
    RectTransform panel, viewport;
    TMP_Text title;
    AudioManager owner;
    AudioClip song;
    float elapsed, overflow, holdSeconds;
    float layoutWidth = -1;
    bool showing;

    public static GameplaySongPopup CreateForGameplay()
    {
        var game = FindFirstObjectByType<GameController>();
        var canvas = game && game.gameBoard ? game.gameBoard.GetComponentInParent<Canvas>()?.rootCanvas : null;
        if (!canvas) return null;
        var settings = FindFirstObjectByType<VolumePanelUI>(FindObjectsInactive.Include);
        var style = settings && settings.closeButton ? settings.closeButton.GetComponentInChildren<TMP_Text>(true)
            : canvas.GetComponentInChildren<TMP_Text>(true);
        return Create(canvas, style);
    }

    public static GameplaySongPopup Create(Canvas canvas, TMP_Text style)
    {
        var root = new GameObject("Now Playing", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(canvas.transform, false);
        var popup = root.AddComponent<GameplaySongPopup>();
        popup.Build(style);
        return popup;
    }

    void Build(TMP_Text style)
    {
        panel = (RectTransform)transform;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1, 0);
        panel.anchoredPosition = new Vector2(-24, 24);
        panel.sizeDelta = new Vector2(420, 86);
        group = GetComponent<CanvasGroup>();
        group.interactable = group.blocksRaycasts = false;
        group.alpha = 0;
        var frame = Box(transform, "Gold frame", new Color(.68f, .43f, .12f, 1));
        Fill(frame.rectTransform, Vector2.zero, Vector2.zero);
        var background = Box(transform, "Background", new Color(.055f, .045f, .035f, 1));
        Fill(background.rectTransform, new Vector2(3, 3), new Vector2(-3, -3));
        var note = CreateNote(transform);
        note.anchorMin = note.anchorMax = new Vector2(0, .5f);
        note.anchoredPosition = new Vector2(28, 0);
        var caption = Label(transform, "Caption", "NOW PLAYING", 18, style);
        caption.color = new Color(1f, .72f, .25f);
        caption.rectTransform.anchorMin = new Vector2(0, 1);
        caption.rectTransform.anchorMax = Vector2.one;
        caption.rectTransform.pivot = new Vector2(0, 1);
        caption.rectTransform.offsetMin = new Vector2(54, -32);
        caption.rectTransform.offsetMax = new Vector2(-16, -8);
        viewport = new GameObject("Song viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        viewport.SetParent(transform, false);
        Fill(viewport, new Vector2(54, 12), new Vector2(-16, -34));
        title = Label(viewport, "Song title", "", 26, style);
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0, .5f);
        title.rectTransform.pivot = new Vector2(0, .5f);
    }

    public void ShowSong(AudioClip clip, AudioManager audio)
    {
        if (!clip || !SettingsStore.LoadSongPopupEnabled()) { Hide(); return; }
        owner = audio;
        song = clip;
        elapsed = 0;
        showing = true;
        title.text = GameplayPlaylist.Title(clip);
        layoutWidth = -1;
        transform.SetAsLastSibling();
        LayoutTitle();
        Advance(0);
    }

    void LayoutTitle()
    {
        float availableWidth = ((RectTransform)panel.parent).rect.width - 48;
        float width = Mathf.Clamp(availableWidth, 160, 420);
        if (Mathf.Approximately(layoutWidth, width)) return;
        layoutWidth = width;
        panel.sizeDelta = new Vector2(width, 86);
        float viewportWidth = panel.sizeDelta.x - 70;
        float textWidth = title.GetPreferredValues(title.text, Mathf.Infinity, 40).x + 4;
        overflow = Mathf.Max(0, textWidth - viewportWidth);
        title.rectTransform.sizeDelta = new Vector2(Mathf.Max(textWidth, viewportWidth), 40);
        holdSeconds = Mathf.Max(ReadingSeconds, ScrollDelay + overflow / ScrollSpeed + EndReadingSeconds);
    }

    void Update()
    {
        if (!showing) return;
        if (!SettingsStore.LoadSongPopupEnabled() || (owner && owner.CurrentGameplaySong != song)) { Hide(); return; }
        // Keep the announcement available after pausing, without covering settings.
        if (AudioListener.pause || Time.timeScale <= 0f) { group.alpha = 0; return; }
        LayoutTitle();
        Advance(Time.unscaledDeltaTime);
    }

    void Advance(float dt)
    {
        elapsed += Mathf.Max(0, dt);
        float readingTime = elapsed - FadeInSeconds;
        group.alpha = elapsed < FadeInSeconds ? Mathf.Clamp01(elapsed / FadeInSeconds)
            : readingTime <= holdSeconds ? 1f : 1f - Mathf.Clamp01((readingTime - holdSeconds) / FadeOutSeconds);
        // Read the title from its left end to its right end; the text moves left
        // through a mask and holds the ending before the popup fades.
        title.rectTransform.anchoredPosition = new Vector2(-Mathf.Min(overflow,
            Mathf.Max(0, readingTime - ScrollDelay) * ScrollSpeed), 0);
        if (readingTime >= holdSeconds + FadeOutSeconds) Hide();
    }

    public void Hide()
    {
        showing = false;
        if (group) group.alpha = 0;
    }

    public static RectTransform CreateNote(Transform parent)
    {
        var note = new GameObject("Current song note", typeof(RectTransform)).GetComponent<RectTransform>();
        note.SetParent(parent, false);
        note.sizeDelta = new Vector2(24, 28);
        // A tiny stepped music note built on whole pixels, independent of font glyphs.
        NotePart(note, new Vector2(-5, -8), new Vector2(10, 6));
        NotePart(note, new Vector2(7, -5), new Vector2(10, 6));
        NotePart(note, new Vector2(-2, 2), new Vector2(4, 20));
        NotePart(note, new Vector2(10, 5), new Vector2(4, 20));
        NotePart(note, new Vector2(4, 12), new Vector2(16, 4));
        return note;
    }

    static void NotePart(Transform parent, Vector2 position, Vector2 size)
    {
        var part = Box(parent, "Pixel", new Color(1, .72f, .25f));
        part.rectTransform.anchoredPosition = position;
        part.rectTransform.sizeDelta = size;
    }

    static Image Box(Transform parent, string name, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static TMP_Text Label(Transform parent, string name, string text, float size, TMP_Text style)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        label.transform.SetParent(parent, false);
        if (style) { label.font = style.font; label.fontSharedMaterial = style.fontSharedMaterial; }
        label.text = text;
        label.fontSize = size;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.richText = false;
        label.raycastTarget = false;
        return label;
    }

    static void Fill(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
    }
}
