using System.Collections;
using UnityEngine;

public partial class AudioManager
{
    AudioSource playlistPreviewSource;
    Coroutine playlistPreviewFade;
    float playlistPreviewScale;
    bool playlistSettingsSession;
    bool playlistNeedsNewSong;
    AudioClip requestedPreview;
    bool previewPaused;

    public bool IsPlaylistPreviewPlaying(AudioClip clip) =>
        clip && requestedPreview == clip && !previewPaused && playlistPreviewSource &&
        playlistPreviewSource.clip == clip && playlistPreviewSource.isPlaying;

    public void BeginPlaylistEditing()
    {
        playlistSettingsSession = true;
        PlayPauseMusic();
    }

    public void PreviewPlaylistSong(AudioClip clip)
    {
        if (!clip) return;
        playlistSettingsSession = true;
        FadeMainMusicForPause(true);
        FadePauseMusicTo(0f, stopAtEnd: true);
        if (!playlistPreviewSource)
        {
            playlistPreviewSource = gameObject.AddComponent<AudioSource>();
            ConfigureMusicSource(playlistPreviewSource);
            playlistPreviewSource.ignoreListenerPause = true;
        }

        bool resume = requestedPreview == clip && previewPaused && playlistPreviewSource.clip == clip;
        requestedPreview = clip;
        previewPaused = false;
        if (playlistPreviewFade != null) StopCoroutine(playlistPreviewFade);
        playlistPreviewFade = StartCoroutine(ChangePlaylistPreview(clip, resume));
    }

    public void PausePlaylistSong(AudioClip clip)
    {
        if (requestedPreview != clip || previewPaused) return;
        previewPaused = true;
        if (playlistPreviewFade != null) StopCoroutine(playlistPreviewFade);
        playlistPreviewFade = null;
        playlistPreviewSource.Pause();
        playlistPreviewScale = 0f;
        ApplyPlaylistPreviewVolume();
    }

    public void EndPlaylistEditing()
    {
        StopPlaylistPreview();
        RefreshGameplayPlaylist();
        PlayPauseMusic(restart: true);
    }

    public void ClosePlaylistSettings()
    {
        if (!playlistSettingsSession) return;
        playlistSettingsSession = false;
        StopPlaylistPreview();
        RefreshGameplayPlaylist();
        if (AudioListener.pause)
        {
            // Settings can close to the pause menu; ResumeGame owns the unpause.
            PlayPauseMusic(restart: true);
            return;
        }
        StopPauseMusic();
        ApplyPendingMusicModeAfterUnpause();
    }

    public void SetPlaylistSongEnabled(AudioClip clip, bool enabled)
    {
        GameplayPlaylist.SetEnabled(clip, enabled);
        RefreshGameplayPlaylist();
    }

    void RefreshGameplayPlaylist()
    {
        if (context != MusicContext.Level || !levelMusicActive) return;
        currentPool = BuildPool(levelIsBoss);
        AudioSource active = ResolveActiveMusicSource();
        playlistNeedsNewSong = !active || !active.clip || System.Array.IndexOf(currentPool, active.clip) < 0 ||
            (musicSrc.clip && !GameplayPlaylist.IsEnabled(musicSrc.clip)) ||
            (musicTransitionSrc.clip && !GameplayPlaylist.IsEnabled(musicTransitionSrc.clip));
    }

    void SilenceExcludedResumeSources()
    {
        if (context != MusicContext.Level || (!restartContextOnUnpause && !playlistNeedsNewSong)) return;
        // Both sources can hold a track when pause interrupts a crossfade.
        if (musicSrc.clip && System.Array.IndexOf(BuildPool(levelIsBoss), musicSrc.clip) < 0)
        {
            StopMainMusicSource(musicSrc);
            pauseMusicSrcResumeScale = 0f;
        }
        if (musicTransitionSrc.clip && System.Array.IndexOf(BuildPool(levelIsBoss), musicTransitionSrc.clip) < 0)
        {
            StopMainMusicSource(musicTransitionSrc);
            pauseMusicTransitionResumeScale = 0f;
        }
    }

    void StopPlaylistPreview()
    {
        requestedPreview = null;
        previewPaused = false;
        if (!playlistPreviewSource) return;
        if (playlistPreviewFade != null) StopCoroutine(playlistPreviewFade);
        playlistPreviewFade = null;
        playlistPreviewSource.Stop();
        playlistPreviewSource.clip = null;
        playlistPreviewScale = 0f;
        ApplyPlaylistPreviewVolume();
    }

    IEnumerator ChangePlaylistPreview(AudioClip clip, bool resume)
    {
        if (!resume)
        {
            playlistPreviewSource.Stop();
            playlistPreviewScale = 0f;
            ApplyPlaylistPreviewVolume();
            playlistPreviewSource.clip = clip;
            playlistPreviewSource.loop = true;
            playlistPreviewSource.Play();
        }
        else
            playlistPreviewSource.UnPause();
        yield return FadePlaylistPreview(1f);
        playlistPreviewFade = null;
    }

    IEnumerator FadePlaylistPreview(float target)
    {
        float start = playlistPreviewScale;
        if (Mathf.Approximately(start, target)) yield break;
        for (float t = 0f; t < pauseMusicFadeSeconds; t += Time.unscaledDeltaTime)
        {
            playlistPreviewScale = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / pauseMusicFadeSeconds));
            ApplyPlaylistPreviewVolume();
            yield return null;
        }
        playlistPreviewScale = target;
        ApplyPlaylistPreviewVolume();
    }

    void ApplyPlaylistPreviewVolume()
    {
        if (playlistPreviewSource)
            playlistPreviewSource.volume = MainMusicFullVolume * playlistPreviewScale;
    }
}
