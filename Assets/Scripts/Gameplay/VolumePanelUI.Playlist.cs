using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class VolumePanelUI
{
    GameObject playlistPanel;
    Button playlistTabButton;
    Transform playlistRows;
    ScrollRect playlistScroll;
    readonly List<PlaylistSongRowUI> playlistSongRows = new();
    readonly List<TMP_Text> playlistHeaders = new();
    RectTransform stickyPlaylistHeader;
    TMP_Text stickyPlaylistLabel;

    void EnsurePlaylistUI()
    {
        if (playlistPanel) return;
        // The same runtime construction serves the title and in-game settings prefab instances.
        var general = CreatePlaylistButton(transform, "General_Tab", "General");
        PlacePlaylistControl(general.transform, new Vector2(-135, 260), new Vector2(230, 40));
        general.interactable = false;
        playlistTabButton = CreatePlaylistButton(transform, "Playlist_Tab", "Playlist");
        PlacePlaylistControl(playlistTabButton.transform, new Vector2(135, 260), new Vector2(230, 40));
        playlistTabButton.onClick.AddListener(OpenPlaylistTab);

        playlistPanel = new GameObject("Playlist_Panel", typeof(RectTransform), typeof(Image));
        playlistPanel.transform.SetParent(transform, false);
        StretchToFill((RectTransform)playlistPanel.transform, Vector2.zero, Vector2.zero);
        var blocker = playlistPanel.GetComponent<Image>();
        blocker.color = Color.clear;
        blocker.raycastTarget = true;

        CopyPlaylistPanelImage("Background_Panel");
        CopyPlaylistPanelImage("Border_Image");
        var title = PlaylistText(playlistPanel.transform, "Playlist_Title", "Gameplay Playlist", 38, TextAlignmentOptions.Center);
        PlacePlaylistControl(title.transform, new Vector2(0, 310), new Vector2(650, 56));
        var hint = PlaylistText(playlistPanel.transform, "Playlist_Hint", "Check songs to include them. Play to preview.", 20, TextAlignmentOptions.Center);
        PlacePlaylistControl(hint.transform, new Vector2(0, 210), new Vector2(700, 32));

        var backTab = CreatePlaylistButton(playlistPanel.transform, "General_Tab", "General");
        PlacePlaylistControl(backTab.transform, new Vector2(-135, 260), new Vector2(230, 40));
        backTab.onClick.AddListener(ClosePlaylistTab);
        var activeTab = CreatePlaylistButton(playlistPanel.transform, "Playlist_Tab", "Playlist");
        PlacePlaylistControl(activeTab.transform, new Vector2(135, 260), new Vector2(230, 40));
        activeTab.interactable = false;

        var scrollGo = new GameObject("Playlist_Scroll", typeof(RectTransform), typeof(Image), typeof(SmoothPlaylistScrollRect));
        scrollGo.transform.SetParent(playlistPanel.transform, false);
        PlacePlaylistControl(scrollGo.transform, new Vector2(0, -50), new Vector2(710, 430));
        scrollGo.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.9f);
        playlistScroll = scrollGo.GetComponent<ScrollRect>();
        playlistScroll.horizontal = false;
        playlistScroll.vertical = true;
        playlistScroll.movementType = ScrollRect.MovementType.Clamped;
        playlistScroll.scrollSensitivity = 24f;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        StretchToFill((RectTransform)viewport.transform, new Vector2(0, 6), new Vector2(-22, 0));
        playlistScroll.viewport = (RectTransform)viewport.transform;
        var content = new GameObject("Songs", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        playlistRows = content.transform;
        var contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.childControlHeight = layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        playlistScroll.content = contentRect;

        // Full-width opaque overlay sits flush with the list's top edge.
        var sticky = CreateImage(viewport.transform, "Sticky_Section", new Color(0.04f, 0.04f, 0.06f, 1f));
        stickyPlaylistHeader = sticky.rectTransform;
        stickyPlaylistHeader.anchorMin = new Vector2(0, 1);
        stickyPlaylistHeader.anchorMax = Vector2.one;
        stickyPlaylistHeader.pivot = new Vector2(0.5f, 1);
        stickyPlaylistHeader.sizeDelta = new Vector2(0, 44);
        stickyPlaylistLabel = PlaylistText(sticky.transform, "Label", "", 25, TextAlignmentOptions.Left);
        stickyPlaylistLabel.color = new Color(1, 0.72f, 0.25f);
        StretchToFill(stickyPlaylistLabel.rectTransform, new Vector2(12, 0), new Vector2(-6, 0));
        sticky.raycastTarget = stickyPlaylistLabel.raycastTarget = false;
        playlistScroll.onValueChanged.AddListener(_ => UpdateStickyPlaylistHeader());

        var track = CreateImage(scrollGo.transform, "Scroll_Track", new Color(0.12f, 0.12f, 0.15f, 1));
        track.raycastTarget = true;
        var trackRect = track.rectTransform;
        trackRect.anchorMin = new Vector2(1, 0);
        trackRect.anchorMax = Vector2.one;
        trackRect.offsetMin = new Vector2(-16, 6);
        trackRect.offsetMax = new Vector2(-4, -6);
        var handle = CreateImage(track.transform, "Handle", new Color(1, 0.65f, 0.15f));
        handle.raycastTarget = true;
        StretchToFill(handle.rectTransform, Vector2.zero, Vector2.zero);
        var scrollbar = track.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handle.rectTransform;
        scrollbar.targetGraphic = handle;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        playlistScroll.verticalScrollbar = scrollbar;
        MenuScrollRectInput.Attach(playlistScroll, playlistPanel, autoCenterSelected: true);

        var footnote = PlaylistText(playlistPanel.transform, "Playlist_EmptyHint", "No checked songs in a group = silence for those levels.", 17, TextAlignmentOptions.Center);
        PlacePlaylistControl(footnote.transform, new Vector2(0, -285), new Vector2(700, 26));
        var back = CreatePlaylistButton(playlistPanel.transform, "Playlist_Back", "Back");
        PlacePlaylistControl(back.transform, new Vector2(-135, -335), new Vector2(230, 52));
        back.onClick.AddListener(ClosePlaylistTab);
        var close = CreatePlaylistButton(playlistPanel.transform, "Playlist_Close", "Close Settings");
        PlacePlaylistControl(close.transform, new Vector2(135, -335), new Vector2(230, 52));
        close.onClick.AddListener(Close);
        playlistPanel.SetActive(false);
    }

    public void OpenPlaylistTab()
    {
        EnsurePlaylistUI();
        if (playlistPanel.activeSelf) return;
        if (AudioManager.I && playlistSongRows.Count == 0)
        {
            AddPlaylistGroup("EDM / Normal Gameplay Songs", AudioManager.I.EDMGameplayMusic);
            AddPlaylistGroup("EDM / Boss Gameplay Songs", AudioManager.I.EDMBossMusic);
            AddPlaylistGroup("Metal / Normal Gameplay Songs", AudioManager.I.MetalGameplayMusic);
            AddPlaylistGroup("Metal / Boss Gameplay Songs", AudioManager.I.MetalBossMusic);
        }
        foreach (var row in playlistSongRows) row.RefreshToggle();
        playlistPanel.transform.SetAsLastSibling();
        playlistPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        playlistScroll.verticalNormalizedPosition = 1f;
        UpdateStickyPlaylistHeader();
        AudioManager.I?.BeginPlaylistEditing();
        RefreshModalSelectableScope();
        if (UINavigationUtility.ShouldKeepNavigationSelection)
            StartCoroutine(SelectFirstVisibleControlNextFrame(playlistPanel));
    }

    public void ClosePlaylistTab()
    {
        if (!playlistPanel || !playlistPanel.activeSelf) return;
        playlistPanel.SetActive(false);
        AudioManager.I?.EndPlaylistEditing();
        RefreshModalSelectableScope();
        SelectSelectable(playlistTabButton, UICursorController.IsButtonNavigationMode);
    }

    void ClosePlaylistForSettingsExit()
    {
        if (playlistPanel) playlistPanel.SetActive(false);
        AudioManager.I?.ClosePlaylistSettings();
    }

    void AddPlaylistGroup(string heading, AudioClip[] clips)
    {
        var label = PlaylistText(playlistRows, "Section", heading, 25, TextAlignmentOptions.Left);
        label.color = new Color(1, 0.72f, 0.25f);
        playlistHeaders.Add(label);
        AddLayout(label.gameObject, -1, 44, flexibleWidth: 1);
        if (clips == null) return;
        var seen = new HashSet<AudioClip>();
        foreach (var clip in clips)
        {
            if (!clip || !seen.Add(clip)) continue;
            var row = CreateHorizontalLayout(playlistRows, "Song_" + clip.name, 10, 58);
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(10, 10, 6, 6);
            var background = row.gameObject.AddComponent<Image>();
            background.color = new Color(1, 1, 1, 0.035f);

            var checkbox = CreateImage(row, "Enabled", Color.white);
            checkbox.raycastTarget = true;
            var originalBackground = combatLogToggle ? combatLogToggle.targetGraphic as Image : null;
            if (originalBackground) { checkbox.sprite = originalBackground.sprite; checkbox.type = originalBackground.type; }
            else checkbox.color = new Color(0.3f, 0.3f, 0.34f);
            AddLayout(checkbox.gameObject, 30, 30);
            var tick = CreateImage(checkbox.transform, "Checkmark", new Color(1, 0.72f, 0.25f));
            var originalTick = combatLogToggle ? combatLogToggle.graphic as Image : null;
            if (originalTick) tick.sprite = originalTick.sprite;
            StretchToFill(tick.rectTransform, new Vector2(5, 5), new Vector2(-5, -5));
            var toggle = checkbox.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = checkbox;
            toggle.graphic = tick;
            ApplyPlaylistFeedback(toggle);
            toggle.SetIsOnWithoutNotify(GameplayPlaylist.IsEnabled(clip));
            toggle.onValueChanged.AddListener(enabled => AudioManager.I?.SetPlaylistSongEnabled(clip, enabled));

            var title = PlaylistText(row, "Title", GameplayPlaylist.Title(clip), 22, TextAlignmentOptions.Left);
            AddLayout(title.gameObject, 230, 44, flexibleWidth: 1);
            var meter = new GameObject("Pixel_Equalizer", typeof(RectTransform));
            meter.transform.SetParent(row, false);
            AddLayout(meter, 30, 30);
            var bars = new RectTransform[4];
            for (int i = 0; i < bars.Length; i++)
            {
                var bar = CreateImage(meter.transform, "Bar_" + i, new Color(1, 0.72f, 0.25f));
                bars[i] = bar.rectTransform;
                bars[i].anchorMin = bars[i].anchorMax = new Vector2(0, 0);
                bars[i].pivot = new Vector2(0, 0);
                bars[i].anchoredPosition = new Vector2(i * 8, 3);
                bars[i].sizeDelta = new Vector2(4, 4);
            }
            var play = CreatePlaylistButton(row, "Play", "Play");
            AddLayout(play.gameObject, 82, 38);
            play.onClick.AddListener(() => AudioManager.I?.PreviewPlaylistSong(clip));
            var pause = CreatePlaylistButton(row, "Pause", "Pause");
            AddLayout(pause.gameObject, 82, 38);
            pause.onClick.AddListener(() => AudioManager.I?.PausePlaylistSong(clip));
            var view = row.gameObject.AddComponent<PlaylistSongRowUI>();
            view.Configure(clip, toggle, background, bars);
            playlistSongRows.Add(view);
        }
    }

    void UpdateStickyPlaylistHeader()
    {
        if (!stickyPlaylistHeader || playlistHeaders.Count == 0) return;
        var viewport = playlistScroll.viewport;
        float top = viewport.rect.yMax;
        int current = -1;
        for (int i = 0; i < playlistHeaders.Count; i++)
        {
            var rect = playlistHeaders[i].rectTransform;
            float y = viewport.InverseTransformPoint(rect.TransformPoint(new Vector3(0, rect.rect.yMax, 0))).y;
            // Switch when the next heading enters the pinned area, hiding its
            // scrolling copy so no sliced duplicate text peeks below the header.
            bool pinned = y >= top - stickyPlaylistHeader.rect.height;
            playlistHeaders[i].enabled = !pinned;
            if (pinned) current = i;
        }
        stickyPlaylistHeader.gameObject.SetActive(current >= 0);
        if (current < 0) return;
        stickyPlaylistLabel.text = playlistHeaders[current].text;
        stickyPlaylistHeader.anchoredPosition = Vector2.zero;
    }

    void CopyPlaylistPanelImage(string sourceName)
    {
        var source = FindChildComponent<Image>(transform, sourceName);
        if (!source) return;
        var copy = CreateImage(playlistPanel.transform, sourceName, source.color);
        copy.sprite = source.sprite;
        copy.type = source.type;
        copy.material = source.material;
        var rect = copy.rectTransform;
        rect.anchorMin = source.rectTransform.anchorMin;
        rect.anchorMax = source.rectTransform.anchorMax;
        rect.pivot = source.rectTransform.pivot;
        rect.sizeDelta = source.rectTransform.sizeDelta;
        rect.anchoredPosition = source.rectTransform.anchoredPosition;
    }

    TMP_Text PlaylistText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment)
    {
        var label = CreateText(parent, name, text, size, FontStyles.Normal, alignment);
        var source = closeButton ? closeButton.GetComponentInChildren<TMP_Text>(true) : null;
        if (source) { label.font = source.font; label.fontSharedMaterial = source.fontSharedMaterial; }
        return label;
    }

    Button CreatePlaylistButton(Transform parent, string name, string text)
    {
        var button = CreateSimpleButton(parent, name, text);
        var template = controlsButton ? controlsButton : closeButton;
        if (template && template.targetGraphic is Image source)
        {
            var image = button.GetComponent<Image>();
            image.sprite = source.sprite;
            image.type = source.type;
            image.color = source.color;
            var label = button.GetComponentInChildren<TMP_Text>();
            var originalLabel = template.GetComponentInChildren<TMP_Text>(true);
            if (originalLabel)
            {
                label.font = originalLabel.font;
                label.fontSharedMaterial = originalLabel.fontSharedMaterial;
                label.color = originalLabel.color;
            }
        }
        ApplyPlaylistFeedback(button);
        return button;
    }

    void ApplyPlaylistFeedback(Selectable control)
    {
        var template = controlsButton ? controlsButton : closeButton;
        if (template) control.colors = template.colors;
        control.transition = Selectable.Transition.ColorTint;
        var sfx = control.gameObject.AddComponent<UIButtonSFX>();
        var source = template ? template.GetComponent<UIButtonSFX>() : null;
        var hook = GetComponentInParent<GameplayUI_SFXHook>();
        if (!hook) hook = FindFirstObjectByType<GameplayUI_SFXHook>();
        sfx.hoverClip = source && source.hoverClip ? source.hoverClip : hook ? hook.uiHoverSFX : null;
        sfx.clickClip = source && source.clickClip ? source.clickClip : hook ? hook.uiClickSFX : null;
        if (source) { sfx.hoverVolume = source.hoverVolume; sfx.clickVolume = source.clickVolume; }
        UIButtonTargetVisual.Ensure(control.gameObject);
    }

    static void PlacePlaylistControl(Transform target, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)target;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
