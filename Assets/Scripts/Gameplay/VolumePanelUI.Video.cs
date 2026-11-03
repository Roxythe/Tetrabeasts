using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class VolumePanelUI
{
    [Header("Video Tab")]
    [SerializeField] Button generalTabButton, videoTabButton;
    [SerializeField] GameObject videoPanel;
    [SerializeField] TMP_Dropdown displayModeDropdown, resolutionDropdown, vSyncDropdown, frameRateDropdown;
    [SerializeField] Button videoApplyButton, videoKeepButton, videoRevertButton;
    [SerializeField] TMP_Text videoStatus;
    readonly List<Vector2Int> videoResolutions = new();
    readonly List<GameObject> controlsHiddenTabs = new();
    bool videoPending;
    float videoConfirmRemaining;
    VideoSettingsStore.Options previousVideo, pendingVideo;

    void EnsureVideoUI()
    {
        if (!videoPanel) BuildVideoUI();
        RefreshSharedSettingsTabs();
        if (!Application.isPlaying) return;
        WireSettingsTabs();
        videoApplyButton.onClick.RemoveListener(ApplyVideoSettings);
        videoApplyButton.onClick.AddListener(ApplyVideoSettings);
        videoKeepButton.onClick.RemoveListener(KeepVideoSettings);
        videoKeepButton.onClick.AddListener(KeepVideoSettings);
        videoRevertButton.onClick.RemoveListener(RevertVideoSettings);
        videoRevertButton.onClick.AddListener(RevertVideoSettings);
        displayModeDropdown.onValueChanged.RemoveListener(UpdateVideoAvailability);
        displayModeDropdown.onValueChanged.AddListener(UpdateVideoAvailability);
        vSyncDropdown.onValueChanged.RemoveListener(UpdateVideoAvailability);
        vSyncDropdown.onValueChanged.AddListener(UpdateVideoAvailability);
    }

    void WireSettingsTabs()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (button.name == "General_Tab" || button.name == "Playlist_Back" || button.name == "Video_Back")
            { button.onClick.RemoveListener(ShowGeneralTab); button.onClick.AddListener(ShowGeneralTab); }
            else if (button.name == "Playlist_Tab")
            { button.onClick.RemoveListener(OpenPlaylistTab); button.onClick.AddListener(OpenPlaylistTab); }
            else if (button.name == "Video_Tab")
            { button.onClick.RemoveListener(OpenVideoTab); button.onClick.AddListener(OpenVideoTab); }
            else if (button.name == "Playlist_Close" || button.name == "Video_Close")
            { button.onClick.RemoveListener(Close); button.onClick.AddListener(Close); }
        }
    }

    void ShowGeneralTab() { ClosePlaylistTab(); CloseVideoTab(); }

    void RefreshSharedSettingsTabs()
    {
        if (Application.isPlaying && !gameObject.activeInHierarchy) return;
        // Reuse the three authored buttons, including their edited label transforms.
        // Parenting them into the active panel keeps modal controller navigation intact.
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (!generalTabButton && button.name == "General_Tab") generalTabButton = button;
            if (!playlistTabButton && button.name == "Playlist_Tab") playlistTabButton = button;
            if (!videoTabButton && button.name == "Video_Tab") videoTabButton = button;
        }
        bool playlistOpen = playlistPanel && playlistPanel.activeSelf;
        bool videoOpen = videoPanel && videoPanel.activeSelf;
        var parent = playlistOpen ? playlistPanel.transform : videoOpen ? videoPanel.transform : transform;
        MoveSharedSettingsTab(generalTabButton, parent, !playlistOpen && !videoOpen);
        MoveSharedSettingsTab(playlistTabButton, parent, playlistOpen);
        MoveSharedSettingsTab(videoTabButton, parent, videoOpen);
    }

    static void MoveSharedSettingsTab(Button button, Transform parent, bool selected)
    {
        if (!button) return;
        if (button.transform.parent != parent) button.transform.SetParent(parent, false);
        button.transform.SetAsLastSibling();
        button.interactable = !selected;
    }

    void HideSettingsTabsForControls()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (button.name != "General_Tab" && button.name != "Playlist_Tab" && button.name != "Video_Tab") continue;
            if (!button.gameObject.activeSelf) continue;
            controlsHiddenTabs.Add(button.gameObject);
            button.gameObject.SetActive(false);
        }
    }

    void RestoreSettingsTabsAfterControls()
    {
        foreach (var tab in controlsHiddenTabs) if (tab) tab.SetActive(true);
        controlsHiddenTabs.Clear();
    }

    public void OpenVideoTab()
    {
        EnsureVideoUI();
        if (videoPanel.activeSelf) return;
        ClosePlaylistTab();
        RefreshVideoSettings();
        videoPanel.SetActive(true);
        videoPanel.transform.SetAsLastSibling();
        RefreshSharedSettingsTabs();
        RefreshModalSelectableScope();
        if (UINavigationUtility.ShouldKeepNavigationSelection)
            StartCoroutine(SelectFirstVisibleControlNextFrame(videoPanel));
    }

    public void CloseVideoTab()
    {
        if (!videoPanel || !videoPanel.activeSelf) return;
        if (videoPending) RevertVideoSettings();
        videoPanel.SetActive(false);
        RefreshSharedSettingsTabs();
        RefreshModalSelectableScope();
        var tab = transform.Find("Video_Tab")?.GetComponent<Button>();
        if (tab) SelectSelectable(tab, UICursorController.IsButtonNavigationMode);
    }

    void RefreshVideoSettings()
    {
        var value = VideoSettingsStore.Load();
        videoResolutions.Clear();
        foreach (var resolution in Screen.resolutions) AddVideoResolution(resolution.width, resolution.height);
        AddVideoResolution(Screen.width, Screen.height);
        AddVideoResolution(value.width, value.height);
        if (videoResolutions.Count == 1)
        { AddVideoResolution(1280, 720); AddVideoResolution(1920, 1080); }
        videoResolutions.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
        var labels = new List<string>();
        foreach (var resolution in videoResolutions) labels.Add($"{resolution.x} x {resolution.y}");
        resolutionDropdown.ClearOptions(); resolutionDropdown.AddOptions(labels);
        resolutionDropdown.SetValueWithoutNotify(Mathf.Max(0, videoResolutions.IndexOf(new Vector2Int(value.width, value.height))));
        displayModeDropdown.SetValueWithoutNotify(value.displayMode);
        vSyncDropdown.SetValueWithoutNotify(value.vSync ? 1 : 0);
        frameRateDropdown.SetValueWithoutNotify(System.Array.IndexOf(VideoSettingsStore.FrameRates, value.frameRate));
        videoKeepButton.gameObject.SetActive(false);
        videoRevertButton.gameObject.SetActive(false);
        UpdateVideoAvailability(0);
    }

    void AddVideoResolution(int width, int height)
    {
        var size = new Vector2Int(width, height);
        if (width >= 640 && height >= 360 && !videoResolutions.Contains(size)) videoResolutions.Add(size);
    }

    void UpdateVideoAvailability(int _)
    {
        displayModeDropdown.interactable = vSyncDropdown.interactable = !videoPending;
        resolutionDropdown.interactable = !videoPending && displayModeDropdown.value != 2;
        frameRateDropdown.interactable = !videoPending && vSyncDropdown.value == 0;
        videoApplyButton.interactable = !videoPending;
        videoApplyButton.gameObject.SetActive(!videoPending);
        if (!videoPending) videoStatus.text = "Borderless uses your desktop resolution.\nVSync uses your display's refresh rate.\nThe game keeps its widescreen layout.";
    }

    void ApplyVideoSettings()
    {
        if (videoPending || videoResolutions.Count == 0) return;
        var size = videoResolutions[resolutionDropdown.value];
        previousVideo = VideoSettingsStore.CaptureCurrent();
        pendingVideo = new VideoSettingsStore.Options { displayMode = displayModeDropdown.value,
            width = size.x, height = size.y, vSync = vSyncDropdown.value == 1,
            frameRate = VideoSettingsStore.FrameRates[frameRateDropdown.value] };
        VideoSettingsStore.Apply(pendingVideo);
        videoPending = true;
        videoConfirmRemaining = 15f;
        videoKeepButton.gameObject.SetActive(true);
        videoRevertButton.gameObject.SetActive(true);
        UpdateVideoAvailability(0);
        RefreshModalSelectableScope();
        if (UINavigationUtility.ShouldKeepNavigationSelection) SelectSelectable(videoKeepButton, true);
    }

    void KeepVideoSettings()
    {
        if (!videoPending) return;
        VideoSettingsStore.Save(pendingVideo);
        videoPending = false;
        RefreshVideoSettings();
        RefreshModalSelectableScope();
    }

    void RevertVideoSettings()
    {
        if (!videoPending) return;
        VideoSettingsStore.Apply(previousVideo);
        videoPending = false;
        RefreshVideoSettings();
        RefreshModalSelectableScope();
    }

    void UpdateVideoConfirmation()
    {
        if (!videoPending) return;
        videoConfirmRemaining -= Time.unscaledDeltaTime;
        videoStatus.text = $"Keep these video settings?\nReverting in {Mathf.CeilToInt(videoConfirmRemaining)} seconds.";
        if (videoConfirmRemaining <= 0f) RevertVideoSettings();
    }

    void BuildVideoUI()
    {
        videoPanel = new GameObject("Video_Panel", typeof(RectTransform), typeof(Image));
        videoPanel.transform.SetParent(transform, false);
        StretchToFill((RectTransform)videoPanel.transform, Vector2.zero, Vector2.zero);
        videoPanel.GetComponent<Image>().color = Color.clear;
        foreach (string name in new[] { "Background_Panel", "Border_Image" })
            CopyPlaylistPanelImage(name, videoPanel.transform);
        var title = PlaylistText(videoPanel.transform, "Video_Title", "Video Settings", 38, TextAlignmentOptions.Center);
        PlacePlaylistControl(title.transform, new Vector2(0, 310), new Vector2(650, 56));
        displayModeDropdown = VideoDropdown("Display_Mode", "Display mode", 155, new[] { "Windowed", "Fullscreen", "Borderless" });
        resolutionDropdown = VideoDropdown("Resolution", "Resolution", 75, new[] { "1920 x 1080" });
        vSyncDropdown = VideoDropdown("VSync", "VSync", -5, new[] { "Off", "On" });
        frameRateDropdown = VideoDropdown("Frame_Rate", "Frame rate limit", -85, new[] { "60 FPS", "120 FPS", "144 FPS", "240 FPS", "Unlimited" });
        videoStatus = PlaylistText(videoPanel.transform, "Video_Status", "Borderless uses your desktop resolution.\nVSync uses your display's refresh rate.\nThe game keeps its widescreen layout.", 20, TextAlignmentOptions.Center);
        PlacePlaylistControl(videoStatus.transform, new Vector2(0, -187), new Vector2(680, 90));
        videoApplyButton = VideoButton("Video_Apply", "Apply", new Vector2(0, -270));
        videoKeepButton = VideoButton("Video_Keep", "Keep", new Vector2(-130, -270));
        videoRevertButton = VideoButton("Video_Revert", "Revert", new Vector2(130, -270));
        videoKeepButton.gameObject.SetActive(false); videoRevertButton.gameObject.SetActive(false);
        VideoButton("Video_Back", "Back", new Vector2(-135, -335));
        VideoButton("Video_Close", "Close Settings", new Vector2(135, -335));
        int index = 0;
        foreach (string name in new[] { "General", "Playlist", "Video" })
        {
            var tab = transform.Find(name + "_Tab")?.GetComponent<Button>();
            if (!tab)
            {
                tab = CreatePlaylistButton(transform, name + "_Tab", name);
                PlacePlaylistControl(tab.transform, new Vector2((index - 1) * 240, 260), new Vector2(210, 40));
            }
            index++;
        }
        videoPanel.SetActive(false);
    }

    Button VideoButton(string name, string label, Vector2 position)
    {
        var button = CreatePlaylistButton(videoPanel.transform, name, label);
        PlacePlaylistControl(button.transform, position, new Vector2(230, 48));
        return button;
    }

    TMP_Dropdown VideoDropdown(string name, string label, float y, string[] options)
    {
        var caption = PlaylistText(videoPanel.transform, name + "_Label", label, 26, TextAlignmentOptions.Left);
        PlacePlaylistControl(caption.transform, new Vector2(-165, y), new Vector2(290, 42));
        var dropdown = Instantiate(musicModeDropdown, videoPanel.transform, false);
        dropdown.name = name + "_Dropdown";
        dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
        dropdown.ClearOptions(); dropdown.AddOptions(new List<string>(options));
        dropdown.SetValueWithoutNotify(0);
        PlacePlaylistControl(dropdown.transform, new Vector2(170, y), new Vector2(270, 42));
        return dropdown;
    }

#if UNITY_EDITOR
    public void BuildEditableSettings(AudioManager library)
    {
        EnsurePlaylistUI();
        if (playlistSongRows.Count == 0)
        {
            AddPlaylistGroup("EDM / Normal Gameplay Songs", library.EDMGameplayMusic);
            AddPlaylistGroup("EDM / Boss Gameplay Songs", library.EDMBossMusic);
            AddPlaylistGroup("Metal / Normal Gameplay Songs", library.MetalGameplayMusic);
            AddPlaylistGroup("Metal / Boss Gameplay Songs", library.MetalBossMusic);
        }
        EnsureVideoUI();
    }
#endif
}
