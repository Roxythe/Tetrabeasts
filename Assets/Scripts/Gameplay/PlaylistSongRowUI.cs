using UnityEngine;
using UnityEngine.UI;

public sealed class PlaylistSongRowUI : MonoBehaviour
{
    [SerializeField] AudioClip clip;
    [SerializeField] Toggle enabledToggle;
    [SerializeField] Image background;
    [SerializeField] RectTransform[] bars;
    [SerializeField] RectTransform currentSongNote;
    bool callbacksWired;
    bool lastCurrent;
    int lastFrame = -1;

    public void Configure(AudioClip song, Toggle toggle, Image rowBackground, RectTransform[] meterBars, RectTransform note = null)
    {
        clip = song;
        enabledToggle = toggle;
        background = rowBackground;
        bars = meterBars;
        currentSongNote = note;
        if (Application.isPlaying) WireCallbacks();
    }

    void Awake() => WireCallbacks();

    void WireCallbacks()
    {
        if (callbacksWired || !clip || !enabledToggle) return;
        callbacksWired = true;
        enabledToggle.onValueChanged.AddListener(SetEnabled);
        transform.Find("Play").GetComponent<Button>().onClick.AddListener(Preview);
        transform.Find("Pause").GetComponent<Button>().onClick.AddListener(Pause);
    }

    void SetEnabled(bool value) => AudioManager.I?.SetPlaylistSongEnabled(clip, value);
    void Preview() => AudioManager.I?.PreviewPlaylistSong(clip);
    void Pause() => AudioManager.I?.PausePlaylistSong(clip);

    public void RefreshToggle() => enabledToggle.SetIsOnWithoutNotify(GameplayPlaylist.IsEnabled(clip));

    void Update()
    {
        if (bars == null) return;
        bool playing = AudioManager.I && AudioManager.I.IsPlaylistPreviewPlaying(clip);
        bool current = AudioManager.I && AudioManager.I.CurrentGameplaySong == clip;
        if (currentSongNote && currentSongNote.gameObject.activeSelf != current)
            currentSongNote.gameObject.SetActive(current);
        int frame = playing ? Mathf.FloorToInt(Time.unscaledTime * 8f) : -2;
        if (frame == lastFrame && current == lastCurrent) return;
        lastFrame = frame;
        lastCurrent = current;
        background.color = playing || current ? new Color(1f, 0.62f, 0.08f, 0.18f) : new Color(1, 1, 1, 0.035f);
        for (int i = 0; i < bars.Length; i++)
        {
            // Quantized heights and eight frames per second keep the meter pixel styled.
            int steps = playing ? 1 + ((frame * (i + 3) + i * 2) % 6) : 1;
            bars[i].sizeDelta = new Vector2(4, steps * 4);
        }
    }
}
