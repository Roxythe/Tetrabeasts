#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameplayPlaylistTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    string oldPlaylist;
    bool hadPlaylist;
    readonly List<Object> temporary = new();

    [SetUp] public void SetUp()
    {
        LocalSaveTestScope.Begin();
        SessionState.SetBool("PlaylistTests.HadVideo", PlayerPrefs.HasKey(VideoSettingsStore.PreferenceKey));
        SessionState.SetString("PlaylistTests.Video", PlayerPrefs.GetString(VideoSettingsStore.PreferenceKey));
        PlayerPrefs.DeleteKey(VideoSettingsStore.PreferenceKey);
        SessionState.SetInt("PlaylistTests.SongPopup", PlayerPrefs.GetInt(SettingsStore.KeySongPopup, -1));
        PlayerPrefs.DeleteKey(SettingsStore.KeySongPopup);
        SessionState.SetInt("PlaylistTests.PendingWrite", PlayerPrefs.GetInt("gameplay_playlist_pending_file_write", 0));
        PlayerPrefs.DeleteKey("gameplay_playlist_pending_file_write");
        hadPlaylist = PlayerPrefs.HasKey(GameplayPlaylist.PreferenceKey);
        oldPlaylist = PlayerPrefs.GetString(GameplayPlaylist.PreferenceKey);
        SessionState.SetBool("PlaylistTests.HadPreference", hadPlaylist);
        SessionState.SetString("PlaylistTests.OldPreference", oldPlaylist);
        PlayerPrefs.DeleteKey(GameplayPlaylist.PreferenceKey);
    }

    [TearDown] public void TearDown()
    {
        AudioListener.pause = false;
        Time.timeScale = 1;
        foreach (var obj in temporary) if (obj) Object.DestroyImmediate(obj);
        temporary.Clear();
        if (SessionState.GetBool("PlaylistTests.HadVideo", false))
            PlayerPrefs.SetString(VideoSettingsStore.PreferenceKey, SessionState.GetString("PlaylistTests.Video", ""));
        else PlayerPrefs.DeleteKey(VideoSettingsStore.PreferenceKey);
        if (SessionState.GetBool("PlaylistTests.HadPreference", false))
            PlayerPrefs.SetString(GameplayPlaylist.PreferenceKey, SessionState.GetString("PlaylistTests.OldPreference", ""));
        else PlayerPrefs.DeleteKey(GameplayPlaylist.PreferenceKey);
        PlayerPrefs.SetInt("gameplay_playlist_pending_file_write", SessionState.GetInt("PlaylistTests.PendingWrite", 0));
        int popupPreference = SessionState.GetInt("PlaylistTests.SongPopup", -1);
        if (popupPreference < 0) PlayerPrefs.DeleteKey(SettingsStore.KeySongPopup);
        else PlayerPrefs.SetInt(SettingsStore.KeySongPopup, popupPreference);
        PlayerPrefs.Save();
        LocalSaveTestScope.End();
    }

    static T Read<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
    static AudioClip[] Pool(AudioManager manager, bool boss) => (AudioClip[])typeof(AudioManager).GetMethod("BuildPool", Private).Invoke(manager, new object[] { boss });
    static void Mode(AudioManager manager, AudioManager.MusicMode mode) => typeof(AudioManager).GetProperty("musicMode").SetValue(manager, mode);
    AudioManager Prefab() => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AudioManager.prefab").GetComponent<AudioManager>();

    [Test] public void SettingsTabsAndSongsAreSerializedInTheEditablePrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Panels/Volume_Panel.prefab");
        var panel = prefab.GetComponent<VolumePanelUI>();
        foreach (string tab in new[] { "General_Tab", "Playlist_Tab", "Video_Tab" })
        {
            Assert.IsNotNull(prefab.transform.Find(tab), tab);
            Assert.AreEqual(1, prefab.GetComponentsInChildren<Button>(true).Count(b => b.name == tab), "One authored tab is shared across all panels.");
        }
        Assert.AreEqual(54, prefab.GetComponentsInChildren<PlaylistSongRowUI>(true).Length);
        Assert.AreEqual(54, Read<List<PlaylistSongRowUI>>(panel, "playlistSongRows").Count);
        Assert.IsNotNull(Read<GameObject>(panel, "videoPanel"));
        Assert.IsNotNull(Read<ScrollRect>(panel, "playlistScroll"));
    }

    [Test] public void SongPopupUsesTheActualGameplayCanvasAboveExistingUI()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            var popup = GameplaySongPopup.CreateForGameplay();
            Assert.IsNotNull(popup);
            Assert.IsTrue(popup.gameObject.activeInHierarchy);
            var canvas = popup.GetComponent<Canvas>();
            Assert.IsTrue(canvas.overrideSorting);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var other in root.GetComponentsInChildren<Canvas>(true))
                    if (other != canvas) Assert.Greater(canvas.sortingOrder, other.sortingOrder, other.name);
        }
        finally { UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
    }

    [Test] public void VideoSettingsPersistAndRejectInvalidValues()
    {
        Assert.AreEqual(2, VideoSettingsStore.Load().displayMode);
        foreach (int mode in new[] { 0, 1, 2 })
        {
            var value = new VideoSettingsStore.Options { displayMode = mode, width = 1280, height = 720, vSync = false, frameRate = 120 };
            VideoSettingsStore.Save(value);
            Assert.AreEqual(JsonUtility.ToJson(value), JsonUtility.ToJson(VideoSettingsStore.Load()));
        }
        PlayerPrefs.SetString(VideoSettingsStore.PreferenceKey, "broken json");
        Assert.AreEqual(2, VideoSettingsStore.Load().displayMode);
        var invalid = VideoSettingsStore.Validate(new VideoSettingsStore.Options { displayMode = 9, width = -1, height = 0, frameRate = 0 });
        Assert.AreEqual(2, invalid.displayMode);
        Assert.AreEqual(1920, invalid.width);
        Assert.AreEqual(60, invalid.frameRate);
        Assert.AreEqual(FullScreenMode.Windowed, VideoSettingsStore.ScreenMode(0));
        Assert.AreEqual(FullScreenMode.ExclusiveFullScreen, VideoSettingsStore.ScreenMode(1));
        Assert.AreEqual(FullScreenMode.FullScreenWindow, VideoSettingsStore.ScreenMode(2));
    }

    [Test] public void EveryConfiguredSongHasAUniqueFixedTitleAndDefaultsToEnabled()
    {
        var audio = Prefab();
        var songs = audio.EDMGameplayMusic.Concat(audio.EDMBossMusic).Concat(audio.MetalGameplayMusic).Concat(audio.MetalBossMusic).ToArray();
        Assert.AreEqual(54, songs.Length);
        Assert.AreEqual(songs.Length, songs.Select(GameplayPlaylist.Title).Distinct().Count());
        foreach (var song in songs)
        {
            Assert.IsTrue(GameplayPlaylist.IsEnabled(song));
            Assert.AreNotEqual(song.name, GameplayPlaylist.Title(song), song.name);
        }
    }

    [Test] public void ExclusionsPersistAcrossReloadAndNewSongsDefaultToEnabled()
    {
        var song = Prefab().EDMGameplayMusic[0];
        GameplayPlaylist.SetEnabled(song, false);
        Assert.IsTrue(PlayerPrefs.GetString(GameplayPlaylist.PreferenceKey).Contains(song.name));
        typeof(GameplayPlaylist).GetField("loadedJson", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
        Assert.IsFalse(GameplayPlaylist.IsEnabled(song));
        Assert.IsTrue(GameplayPlaylist.IsEnabled(Prefab().EDMGameplayMusic[1]));
        GameplayPlaylist.SetEnabled(song, true);
        Assert.IsTrue(GameplayPlaylist.IsEnabled(song));
    }

    [TestCase(AudioManager.MusicMode.EDM)]
    [TestCase(AudioManager.MusicMode.Metal)]
    [TestCase(AudioManager.MusicMode.Both)]
    public void FilteringPreservesGenreAndBossPoolsAndAllowsEmptyPools(AudioManager.MusicMode mode)
    {
        var go = new GameObject("Playlist pool test"); temporary.Add(go); go.SetActive(false);
        var audio = go.AddComponent<AudioManager>();
        var prefab = Prefab();
        audio.EDMGameplayMusic = prefab.EDMGameplayMusic;
        audio.EDMBossMusic = prefab.EDMBossMusic;
        audio.MetalGameplayMusic = prefab.MetalGameplayMusic;
        audio.MetalBossMusic = prefab.MetalBossMusic;
        Mode(audio, mode);
        foreach (bool boss in new[] { false, true })
        {
            var expected = new List<AudioClip>();
            if (mode != AudioManager.MusicMode.Metal) expected.AddRange(boss ? audio.EDMBossMusic : audio.EDMGameplayMusic);
            if (mode != AudioManager.MusicMode.EDM) expected.AddRange(boss ? audio.MetalBossMusic : audio.MetalGameplayMusic);
            CollectionAssert.AreEquivalent(expected, Pool(audio, boss));
            foreach (var clip in expected) GameplayPlaylist.SetEnabled(clip, false);
            Assert.IsEmpty(Pool(audio, boss));
            GameplayPlaylist.SetEnabled(expected[0], true);
            CollectionAssert.AreEqual(new[] { expected[0] }, Pool(audio, boss));
        }
    }

    [UnityTest] public IEnumerator PreviewPauseResumeAndSettingsExitPreserveContext()
    {
        yield return new EnterPlayMode();
        LocalSaveTestScope.Apply();
        // These fields are initialized after the domain reload on entering play mode.
        var audio = Object.Instantiate(Prefab()); temporary.Add(audio.gameObject);
        Mode(audio, AudioManager.MusicMode.EDM);
        typeof(AudioManager).GetField("applicationHasFocus", Private).SetValue(audio, true);
        audio.PlayTitleMusic();
        yield return new WaitForSecondsRealtime(1.4f);
        var title = Read<AudioSource>(audio, "activeMusicSrc");
        var titleClip = title.clip;
        audio.BeginPlaylistEditing();
        var song = audio.EDMGameplayMusic[0];
        audio.PreviewPlaylistSong(song);
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.IsTrue(audio.IsPlaylistPreviewPlaying(song));
        Assert.IsFalse(title.isPlaying);
        audio.PausePlaylistSong(song);
        Assert.IsFalse(Read<AudioSource>(audio, "playlistPreviewSource").isPlaying, "Pause must cut audio in the same frame.");
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.IsFalse(audio.IsPlaylistPreviewPlaying(song));
        var preview = Read<AudioSource>(audio, "playlistPreviewSource");
        float pausedAt = preview.time;
        audio.PreviewPlaylistSong(song);
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.Greater(preview.time, pausedAt);
        audio.EndPlaylistEditing();
        Assert.IsFalse(preview.isPlaying, "Leaving the playlist must cut the sample before pause music returns.");
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.IsFalse(preview.isPlaying);
        Assert.IsTrue(Read<AudioSource>(audio, "pauseMusicSrc").isPlaying);
        audio.ClosePlaylistSettings();
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.AreSame(titleClip, Read<AudioSource>(audio, "activeMusicSrc").clip);
        Assert.IsTrue(title.isPlaying);

        audio.PlayLevelMusic(false);
        Assert.IsNotNull(Read<AudioSource>(audio, "activeMusicSrc"), "Source immediately after starting gameplay");
        yield return new WaitForSecondsRealtime(2.2f);
        Assert.IsNotNull(Read<AudioSource>(audio, "activeMusicSrc"), "Source after gameplay fade");
        var currentSong = audio.CurrentGameplaySong;
        Assert.AreSame(Read<AudioSource>(audio, "activeMusicSrc").clip, currentSong);
        AudioListener.pause = true;
        Time.timeScale = 0;
        audio.PlayPauseMusic();
        audio.BeginPlaylistEditing();
        Assert.IsNotNull(Read<AudioSource>(audio, "activeMusicSrc"), "Source immediately after pausing gameplay");
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.IsNotNull(Read<AudioSource>(audio, "activeMusicSrc"), "Source after pause fade");
        var excluded = Read<AudioSource>(audio, "activeMusicSrc").clip;
        foreach (var clip in audio.EDMGameplayMusic) audio.SetPlaylistSongEnabled(clip, false);
        // Avoid a captured lambda in this iterator: EnterPlayMode reloads its closure.
        var allowed = audio.EDMGameplayMusic[0] != excluded ? audio.EDMGameplayMusic[0] : audio.EDMGameplayMusic[1];
        audio.SetPlaylistSongEnabled(allowed, true);
        var pauseTrack = Read<AudioSource>(audio, "pauseMusicSrc");
        pauseTrack.time = 8f;
        var pauseClip = pauseTrack.clip;
        audio.PreviewPlaylistSong(audio.MetalBossMusic[0]);
        Assert.AreSame(currentSong, audio.CurrentGameplaySong, "Pause and previews retain the gameplay song identity.");
        Assert.AreSame(audio.MetalBossMusic[0], preview.clip, "Replacing a sample must not wait for an outgoing fade.");
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.IsTrue(audio.IsPlaylistPreviewPlaying(audio.MetalBossMusic[0]));
        Assert.AreEqual(8f, pauseTrack.time, .2f, "Previewing freezes the pause track's playback position.");
        audio.EndPlaylistEditing();
        yield return new WaitForSecondsRealtime(.6f);
        Assert.AreSame(pauseClip, pauseTrack.clip);
        Assert.Greater(pauseTrack.time, 8f, "Leaving the playlist resumes instead of restarting pause music.");
        Mode(audio, AudioManager.MusicMode.Both);
        for (int i = 0; i < 6; i++) audio.PlayPauseMusic();
        Assert.AreSame(pauseClip, pauseTrack.clip, "Panel navigation keeps the chosen Random-mode song.");
        Assert.Greater(pauseTrack.time, 8f);
        Mode(audio, AudioManager.MusicMode.EDM);
        audio.ClosePlaylistSettings();
        audio.StopPauseMusic();
        AudioListener.pause = false;
        audio.ApplyPendingMusicModeAfterUnpause();
        yield return new WaitForSecondsRealtime(2.2f);
        Assert.AreSame(allowed, Read<AudioSource>(audio, "activeMusicSrc").clip);
        Assert.AreSame(allowed, audio.CurrentGameplaySong);
        Assert.IsFalse(preview.isPlaying);

        // Preserve an eligible track's playback position across another preview session.
        float beforePause = Read<AudioSource>(audio, "activeMusicSrc").time;
        AudioListener.pause = true;
        audio.PlayPauseMusic();
        Assert.Less(pauseTrack.time, 1f, "A new pause session starts the track over.");
        pauseTrack.time = 8f;
        audio.StopPauseMusic();
        audio.PlayPauseMusic();
        Assert.Less(pauseTrack.time, 1f, "An immediate re-pause also starts fresh during the outgoing fade.");
        audio.BeginPlaylistEditing();
        audio.PreviewPlaylistSong(song);
        yield return new WaitForSecondsRealtime(0.6f);
        audio.ClosePlaylistSettings();
        audio.StopPauseMusic();
        AudioListener.pause = false;
        audio.ApplyPendingMusicModeAfterUnpause();
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.AreSame(allowed, Read<AudioSource>(audio, "activeMusicSrc").clip);
        Assert.Greater(Read<AudioSource>(audio, "activeMusicSrc").time, beforePause);

        // Empty normal pools never fall back to menu music, and boss pools remain independent.
        audio.BeginPlaylistEditing();
        audio.SetPlaylistSongEnabled(allowed, false);
        audio.ClosePlaylistSettings();
        yield return new WaitForSecondsRealtime(1.2f);
        Assert.IsFalse(Read<AudioSource>(audio, "musicSrc").isPlaying);
        Assert.IsFalse(Read<AudioSource>(audio, "musicTransitionSrc").isPlaying);
        Assert.IsNull(audio.CurrentGameplaySong, "An empty playlist has no current song marker.");
        audio.PlayLevelMusic(true);
        yield return new WaitForSecondsRealtime(2.2f);
        CollectionAssert.Contains(audio.EDMBossMusic, Read<AudioSource>(audio, "activeMusicSrc").clip);

        // Rapid preview switches and closing mid-fade must not restart a stale preview.
        audio.BeginPlaylistEditing();
        audio.PreviewPlaylistSong(song);
        audio.PreviewPlaylistSong(audio.MetalBossMusic[0]);
        audio.PausePlaylistSong(audio.MetalBossMusic[0]);
        audio.PreviewPlaylistSong(audio.MetalBossMusic[0]);
        audio.ClosePlaylistSettings();
        yield return new WaitForSecondsRealtime(0.8f);
        Assert.IsFalse(preview.isPlaying);
        Object.DestroyImmediate(audio.gameObject);
        yield return new ExitPlayMode();
    }

    [UnityTest] public IEnumerator PlaylistUIContainsOnlyPoolSongsAndScrollsToLastRow()
    {
        yield return new EnterPlayMode();
        LocalSaveTestScope.Apply();
        GameModeStore.Current = RunGameMode.Chaos;
        var audio = Object.Instantiate(Prefab()); temporary.Add(audio.gameObject);
        var canvasGo = new GameObject("Playlist test canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        temporary.Add(canvasGo);
        var cameraGo = new GameObject("Playlist test camera", typeof(Camera)); temporary.Add(cameraGo);
        var camera = cameraGo.GetComponent<Camera>();
        var renderTarget = new RenderTexture(1920, 1080, 24); temporary.Add(renderTarget);
        camera.targetTexture = renderTarget;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var hook = canvasGo.AddComponent<GameplayUI_SFXHook>();
        hook.uiHoverSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ButtonHoverUI_SFX.wav");
        var events = new GameObject("Playlist test events", typeof(EventSystem)); temporary.Add(events);
        // Gameplay (including Chaos) uses the settings prefab nested inside PauseMenu.
        var pausePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Panels/PauseMenu_Panel.prefab");
        var pauseGo = Object.Instantiate(pausePrefab, canvas.transform);
        pauseGo.SetActive(true);
        var panelGo = pauseGo.GetComponentInChildren<VolumePanelUI>(true).gameObject;
        panelGo.SetActive(true);
        var panel = panelGo.GetComponent<VolumePanelUI>();
        var sharedGeneral = panelGo.transform.Find("General_Tab").GetComponent<Button>();
        var sharedPlaylist = panelGo.transform.Find("Playlist_Tab").GetComponent<Button>();
        var sharedVideo = panelGo.transform.Find("Video_Tab").GetComponent<Button>();
        var authoredOffset = sharedVideo.GetComponentInChildren<TMPro.TMP_Text>(true).rectTransform.anchoredPosition;
        yield return null;
        Capture(camera, "Playlist-General");
        panel.OpenControlsPanel();
        Assert.IsFalse(sharedGeneral.gameObject.activeSelf);
        Assert.IsFalse(sharedPlaylist.gameObject.activeSelf);
        Assert.IsFalse(sharedVideo.gameObject.activeSelf);
        panel.CloseControlsPanel(true);
        Assert.IsTrue(sharedGeneral.gameObject.activeSelf);
        Assert.IsTrue(sharedPlaylist.gameObject.activeSelf);
        Assert.IsTrue(sharedVideo.gameObject.activeSelf);
        panel.OpenPlaylistTab();
        Assert.AreSame(sharedVideo, Read<GameObject>(panel, "playlistPanel").transform.Find("Video_Tab").GetComponent<Button>());
        Assert.AreEqual(authoredOffset, sharedVideo.GetComponentInChildren<TMPro.TMP_Text>(true).rectTransform.anchoredPosition);
        var popupToggle = Read<Toggle>(panel, "songPopupToggle");
        Assert.IsTrue(popupToggle.isOn, "New accounts default to showing song popups.");
        popupToggle.isOn = false;
        Assert.IsFalse(SettingsStore.LoadSongPopupEnabled());
        panel.ClosePlaylistTab();
        panel.OpenPlaylistTab();
        Assert.IsFalse(popupToggle.isOn, "Reopening settings retains the choice.");
        popupToggle.isOn = true;
        typeof(AudioManager).GetMethod("NotifyGameplaySongStarted", Private).Invoke(audio, new object[] { audio.EDMGameplayMusic[0] });
        yield return null;
        var rows = panelGo.GetComponentsInChildren<PlaylistSongRowUI>();
        Assert.AreEqual(54, rows.Length);
        Assert.IsTrue(rows[0].transform.Find("Current song/Current song note").gameObject.activeSelf);
        Assert.IsFalse(rows[1].transform.Find("Current song/Current song note").gameObject.activeSelf);
        foreach (var row in rows)
        {
            Assert.IsTrue(row.GetComponentInChildren<Toggle>().isOn);
            foreach (var button in row.GetComponentsInChildren<Button>())
            {
                Assert.IsNotNull(button.GetComponent<UIButtonSFX>().hoverClip);
                Assert.AreNotEqual(button.colors.normalColor, button.colors.highlightedColor);
            }
        }
        var playlistPanel = Read<GameObject>(panel, "playlistPanel");
        Assert.AreSame(panelGo.transform.Find("Background_Panel").GetComponent<Image>().sprite,
            playlistPanel.transform.Find("Background_Panel").GetComponent<Image>().sprite);
        var scroll = Read<ScrollRect>(panel, "playlistScroll");
        Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height);
        var headers = Read<List<TMPro.TMP_Text>>(panel, "playlistHeaders");
        var sticky = Read<RectTransform>(panel, "stickyPlaylistHeader");
        Assert.IsInstanceOf<SmoothPlaylistScrollRect>(scroll);
        scroll.verticalNormalizedPosition = 1f;
        float previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        scroll.OnScroll(new PointerEventData(EventSystem.current) { scrollDelta = Vector2.down });
        Assert.AreEqual(0f, scroll.content.anchoredPosition.y, .01f, "Wheel input queues movement instead of jumping.");
        yield return new WaitForSecondsRealtime(.08f);
        Assert.Greater(scroll.content.anchoredPosition.y, 0f, "Wheel easing works while gameplay is paused.");
        Assert.LessOrEqual(scroll.content.anchoredPosition.y, scroll.scrollSensitivity);
        yield return new WaitForSecondsRealtime(.5f);
        Assert.AreEqual(scroll.scrollSensitivity, scroll.content.anchoredPosition.y, .2f);
        scroll.OnScroll(new PointerEventData(EventSystem.current) { scrollDelta = Vector2.down });
        scroll.content.anchoredPosition = new Vector2(0f, 100f);
        yield return null;
        yield return null;
        Assert.AreEqual(100f, scroll.content.anchoredPosition.y, .1f, "Scrollbar/controller changes cancel wheel easing.");
        Time.timeScale = previousTimeScale;
        // Part-way through group one, only its pinned copy remains at the top.
        scroll.content.anchoredPosition = new Vector2(0, 200);
        scroll.onValueChanged.Invoke(Vector2.zero);
        Assert.IsTrue(sticky.gameObject.activeSelf);
        Assert.AreEqual("EDM / Normal Gameplay Songs", sticky.GetComponentInChildren<TMPro.TMP_Text>().text);
        Assert.AreEqual(0f, sticky.anchoredPosition.y, .01f);
        float nextHeaderOffset = -headers[1].rectTransform.anchoredPosition.y - headers[1].rectTransform.rect.yMax;
        scroll.content.anchoredPosition = new Vector2(0, nextHeaderOffset - 20);
        scroll.onValueChanged.Invoke(Vector2.zero);
        Assert.AreEqual(0f, sticky.anchoredPosition.y, .1f, "Header stays flush and masks scrolling content.");
        Assert.AreEqual(1f, sticky.GetComponent<Image>().color.a);
        Assert.AreEqual(scroll.viewport.rect.width, sticky.rect.width, .1f);
        Capture(camera, "Playlist-Sticky-Transition");
        scroll.content.anchoredPosition = new Vector2(0, nextHeaderOffset + 10);
        scroll.onValueChanged.Invoke(Vector2.zero);
        Assert.AreEqual(headers[1].text, sticky.GetComponentInChildren<TMPro.TMP_Text>().text);
        Assert.AreEqual(0f, sticky.anchoredPosition.y, .01f);
        scroll.verticalNormalizedPosition = 1f;
        rows[0].transform.Find("Play").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.IsTrue(audio.IsPlaylistPreviewPlaying(audio.EDMGameplayMusic[0]));
        var meter = rows[0].transform.Find("Pixel_Equalizer");
        // A valid meter animation has a flat beat; observe a full cycle instead
        // of sampling a single phase that can legitimately have minimum bars.
        float animationDeadline = Time.realtimeSinceStartup + 1.5f;
        while (!HasRaisedBars(meter) && Time.realtimeSinceStartup < animationDeadline) yield return null;
        Assert.IsTrue(HasRaisedBars(meter));
        Capture(camera, "Playlist-EDM");
        rows[0].transform.Find("Pause").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.IsFalse(audio.IsPlaylistPreviewPlaying(audio.EDMGameplayMusic[0]));
        Assert.IsTrue(meter.GetComponentsInChildren<RectTransform>().Skip(1).All(bar => bar.sizeDelta.y == 4));
        var firstToggle = rows[0].GetComponentInChildren<Toggle>();
        firstToggle.isOn = false;
        Assert.IsFalse(GameplayPlaylist.IsEnabled(audio.EDMGameplayMusic[0]));
        firstToggle.isOn = true;
        EventSystem.current.SetSelectedGameObject(rows.Last().GetComponentInChildren<Toggle>().gameObject);
        yield return null;
        yield return null;
        Assert.Less(scroll.verticalNormalizedPosition, 0.05f);
        Capture(camera, "Playlist-Metal");
        panel.ClosePlaylistTab();
        Assert.IsFalse(playlistPanel.activeSelf);
        panelGo.transform.Find("Video_Tab").GetComponent<Button>().onClick.Invoke();
        var video = Read<GameObject>(panel, "videoPanel");
        Assert.IsTrue(video.activeSelf);
        Assert.AreSame(sharedVideo, video.transform.Find("Video_Tab").GetComponent<Button>());
        Assert.AreEqual(authoredOffset, sharedVideo.GetComponentInChildren<TMPro.TMP_Text>(true).rectTransform.anchoredPosition);
        foreach (string tab in new[] { "General_Tab", "Playlist_Tab", "Video_Tab" })
            Assert.AreEqual(1, panelGo.GetComponentsInChildren<Button>(true).Count(b => b.name == tab));
        Assert.IsEmpty(video.GetComponentsInChildren<Slider>(true), "Artwork must not duplicate General-tab controls.");
        Assert.AreSame(playlistPanel.transform.Find("Background_Panel").GetComponent<Image>().sprite,
            video.transform.Find("Background_Panel").GetComponent<Image>().sprite);
        var display = Read<TMPro.TMP_Dropdown>(panel, "displayModeDropdown");
        var resolution = Read<TMPro.TMP_Dropdown>(panel, "resolutionDropdown");
        var vsync = Read<TMPro.TMP_Dropdown>(panel, "vSyncDropdown");
        var fps = Read<TMPro.TMP_Dropdown>(panel, "frameRateDropdown");
        Assert.IsFalse(resolution.interactable, "Borderless uses desktop resolution.");
        Assert.IsFalse(fps.interactable, "VSync controls timing while enabled.");
        Capture(camera, "Settings-Video");
        display.value = 0; vsync.value = 0; fps.value = 1;
        Assert.IsTrue(resolution.interactable);
        Assert.IsTrue(fps.interactable);
        Read<Button>(panel, "videoApplyButton").onClick.Invoke();
        Assert.IsFalse(PlayerPrefs.HasKey(VideoSettingsStore.PreferenceKey), "A pending change is not saved yet.");
        yield return null;
        Capture(camera, "Settings-Video-Confirm");
        Read<Button>(panel, "videoKeepButton").onClick.Invoke();
        Assert.AreEqual(0, VideoSettingsStore.Load().displayMode);
        Assert.AreEqual(120, VideoSettingsStore.Load().frameRate);
        display.value = 1;
        Read<Button>(panel, "videoApplyButton").onClick.Invoke();
        typeof(VolumePanelUI).GetField("videoConfirmRemaining", Private).SetValue(panel, 0f);
        yield return null;
        Assert.IsFalse(Read<bool>(panel, "videoPending"), "Confirmation expires even while gameplay is paused.");
        Assert.AreEqual(0, display.value);
        display.value = 2;
        Read<Button>(panel, "videoApplyButton").onClick.Invoke();
        video.transform.Find("Playlist_Tab").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(Read<bool>(panel, "videoPending"), "Leaving Video reverts unconfirmed settings.");
        Assert.IsTrue(playlistPanel.activeSelf);
        Assert.AreEqual(54, panelGo.GetComponentsInChildren<PlaylistSongRowUI>(true).Length, "Reopening tabs retains the authored rows.");
        panel.ClosePlaylistTab();
        panel.Close(true);
        var popup = GameplaySongPopup.Create(canvas, panel.closeButton.GetComponentInChildren<TMPro.TMP_Text>(true));
        popup.ShowSong(audio.EDMGameplayMusic[0], null);
        var group = popup.GetComponent<CanvasGroup>();
        Assert.IsTrue(group.ignoreParentGroups);
        Assert.IsTrue(popup.GetComponent<Canvas>().overrideSorting);
        Assert.AreEqual(32766, popup.GetComponent<Canvas>().sortingOrder);
        Assert.AreEqual(0f, group.alpha);
        typeof(GameplaySongPopup).GetMethod("Advance", Private).Invoke(popup, new object[] { .3f });
        Assert.AreEqual(1f, group.alpha);
        Assert.IsFalse(group.blocksRaycasts);
        Capture(camera, "Song-Popup");
        typeof(GameplaySongPopup).GetMethod("Advance", Private).Invoke(popup, new object[] { 2.99f });
        Assert.AreEqual(1f, group.alpha, "Keep a short title fully opaque for three seconds.");
        typeof(GameplaySongPopup).GetMethod("Advance", Private).Invoke(popup, new object[] { .26f });
        Assert.That(group.alpha, Is.InRange(.49f, .51f));
        typeof(GameplaySongPopup).GetMethod("Advance", Private).Invoke(popup, new object[] { .25f });
        Assert.AreEqual(0f, group.alpha);
        var longSong = AudioClip.Create("A Very Long Song Name That Scrolls All The Way To Its Final Words", 44100, 1, 44100, false);
        temporary.Add(longSong);
        popup.ShowSong(longSong, null);
        Assert.Greater(Read<float>(popup, "overflow"), 0f);
        float hold = Read<float>(popup, "holdSeconds");
        typeof(GameplaySongPopup).GetMethod("Advance", Private).Invoke(popup, new object[] { .3f + hold });
        var songTitle = Read<TMPro.TMP_Text>(popup, "title");
        Assert.AreEqual(-Read<float>(popup, "overflow"), songTitle.rectTransform.anchoredPosition.x, .01f);
        Assert.AreEqual(1f, group.alpha);
        Capture(camera, "Song-Popup-Long-End");
        SettingsStore.SaveSongPopupEnabled(false);
        yield return null;
        Assert.AreEqual(0f, group.alpha, "Turning the setting off dismisses an existing popup.");
        foreach (var obj in temporary) if (obj) Object.DestroyImmediate(obj);
        temporary.Clear();
        yield return new ExitPlayMode();
    }

    public static void Capture(Camera camera, string name)
    {
        Canvas.ForceUpdateCanvases();
        var texture = camera.targetTexture;
        var previous = RenderTexture.active;
        camera.Render();
        RenderTexture.active = texture;
        var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        image.Apply();
        System.IO.Directory.CreateDirectory("Logs");
        System.IO.File.WriteAllBytes("Logs/" + name + ".png", image.EncodeToPNG());
        RenderTexture.active = previous;
        Object.DestroyImmediate(image);
    }

    static bool HasRaisedBars(Transform meter) => meter.GetComponentsInChildren<RectTransform>().Skip(1).Any(bar => bar.sizeDelta.y > 4);
}
#endif
