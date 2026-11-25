using System;
using UnityEngine;

/// <summary>Display preferences are device-local because monitors differ between PCs.</summary>
public static class VideoSettingsStore
{
    public const string PreferenceKey = "Settings_Video_v1";
    public static readonly int[] FrameRates = { 30, 60, 90, 120, -1 };
    public static string[] FrameRateLabels => Array.ConvertAll(FrameRates, fps => fps < 0 ? "Unlimited" : fps + " FPS");

    [Serializable]
    public struct Options
    {
        public int displayMode; // Windowed, exclusive fullscreen, borderless.
        public int width, height;
        public bool vSync;
        public int frameRate;
    }

    public static Options Defaults => new Options { displayMode = 2, width = 1920, height = 1080, vSync = false, frameRate = 60 };

    public static Options Load()
    {
        if (!PlayerPrefs.HasKey(PreferenceKey)) return Defaults;
        try { return Validate(JsonUtility.FromJson<Options>(PlayerPrefs.GetString(PreferenceKey))); }
        catch (ArgumentException) { return Defaults; }
    }

    public static Options Validate(Options value)
    {
        value.displayMode = Mathf.Clamp(value.displayMode, 0, 2);
        if (value.width < 640 || value.width > 16384 || value.height < 360 || value.height > 16384)
        { value.width = 1920; value.height = 1080; }
        if (Array.IndexOf(FrameRates, value.frameRate) < 0) value.frameRate = 60;
        return value;
    }

    public static FullScreenMode ScreenMode(int mode) => mode == 0 ? FullScreenMode.Windowed
        : mode == 1 ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow;

    public static bool RequiresDisplayConfirmation(Options current, Options requested) =>
        current.displayMode != requested.displayMode ||
        (requested.displayMode != 2 && (current.width != requested.width || current.height != requested.height));

    public static void ApplyPerformance(Options value)
    {
        value = Validate(value);
        if (Application.isEditor) return;
        QualitySettings.vSyncCount = value.vSync ? 1 : 0;
        Application.targetFrameRate = value.frameRate;
    }

    public static Options CaptureCurrent() => new Options
    {
        displayMode = Screen.fullScreenMode == FullScreenMode.Windowed ? 0
            : Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen ? 1 : 2,
        width = Screen.width, height = Screen.height, vSync = QualitySettings.vSyncCount > 0,
        frameRate = Application.targetFrameRate
    };

    public static void Apply(Options value)
    {
        value = Validate(value);
        // The Editor Game view must never change the developer's desktop display.
        if (Application.isEditor) return;
        var desktop = Screen.currentResolution;
        int width = value.width, height = value.height;
        if (value.displayMode == 2 && desktop.width > 0 && desktop.height > 0)
        { width = desktop.width; height = desktop.height; }
        if (value.displayMode == 1 && Screen.resolutions.Length > 0 &&
            !Array.Exists(Screen.resolutions, r => r.width == width && r.height == height))
        { width = desktop.width; height = desktop.height; }
        Screen.SetResolution(width, height, ScreenMode(value.displayMode));
        ApplyPerformance(value);
    }

    public static void Save(Options value)
    {
        PlayerPrefs.SetString(PreferenceKey, JsonUtility.ToJson(Validate(value)));
        PlayerPrefsSaveScheduler.QueueSave(queueCloudUpload: false);
    }
}
