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
        if (SessionState.GetBool("PlaylistTests.HadPreference", false))
            PlayerPrefs.SetString(GameplayPlaylist.PreferenceKey, SessionState.GetString("PlaylistTests.OldPreference", ""));
        else PlayerPrefs.DeleteKey(GameplayPlaylist.PreferenceKey);
        PlayerPrefs.SetInt("gameplay_playlist_pending_file_write", SessionState.GetInt("PlaylistTests.PendingWrite", 0));
        PlayerPrefs.Save();
        LocalSaveTestScope.End();
    }

    static T Read<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
    static AudioClip[] Pool(AudioManager manager, bool boss) => (AudioClip[])typeof(AudioManager).GetMethod("BuildPool", Private).Invoke(manager, new object[] { boss });
    static void Mode(AudioManager manager, AudioManager.MusicMode mode) => typeof(AudioManager).GetProperty("musicMode").SetValue(manager, mode);
    AudioManager Prefab() => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AudioManager.prefab").GetComponent<AudioManager>();

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
        audio.PreviewPlaylistSong(audio.MetalBossMusic[0]);
        Assert.AreSame(audio.MetalBossMusic[0], preview.clip, "Replacing a sample must not wait for an outgoing fade.");
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.IsTrue(audio.IsPlaylistPreviewPlaying(audio.MetalBossMusic[0]));
        audio.ClosePlaylistSettings();
        audio.StopPauseMusic();
        AudioListener.pause = false;
        audio.ApplyPendingMusicModeAfterUnpause();
        yield return new WaitForSecondsRealtime(2.2f);
        Assert.AreSame(allowed, Read<AudioSource>(audio, "activeMusicSrc").clip);
        Assert.IsFalse(preview.isPlaying);

        // Preserve an eligible track's playback position across another preview session.
        float beforePause = Read<AudioSource>(audio, "activeMusicSrc").time;
        AudioListener.pause = true;
        audio.PlayPauseMusic();
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
        var panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Panels/Volume_Panel.prefab");
        var panelGo = Object.Instantiate(panelPrefab, canvas.transform);
        panelGo.SetActive(true);
        var panel = panelGo.GetComponent<VolumePanelUI>();
        yield return null;
        Capture(camera, "Playlist-General");
        panel.OpenPlaylistTab();
        yield return null;
        var rows = panelGo.GetComponentsInChildren<PlaylistSongRowUI>();
        Assert.AreEqual(54, rows.Length);
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
        // Part-way through group one, only its pinned copy remains at the top.
        scroll.content.anchoredPosition = new Vector2(0, 200);
        scroll.onValueChanged.Invoke(Vector2.zero);
        Assert.IsTrue(sticky.gameObject.activeSelf);
        Assert.AreEqual("EDM / Normal Gameplay Songs", sticky.GetComponentInChildren<TMPro.TMP_Text>().text);
        Assert.AreEqual(0f, sticky.anchoredPosition.y, .01f);
        float nextHeaderOffset = -headers[1].rectTransform.anchoredPosition.y - headers[1].rectTransform.rect.yMax;
        scroll.content.anchoredPosition = new Vector2(0, nextHeaderOffset - 20);
        scroll.onValueChanged.Invoke(Vector2.zero);
        Assert.AreEqual(24f, sticky.anchoredPosition.y, .1f, "Incoming section pushes out the pinned header.");
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
        panel.Close(true);
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
