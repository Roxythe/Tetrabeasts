using UnityEngine;
using UnityEngine.UI;

public sealed class PlaylistSongRowUI : MonoBehaviour
{
    AudioClip clip;
    Toggle enabledToggle;
    Image background;
    RectTransform[] bars;
    int lastFrame = -1;

    public void Configure(AudioClip song, Toggle toggle, Image rowBackground, RectTransform[] meterBars)
    {
        clip = song;
        enabledToggle = toggle;
        background = rowBackground;
        bars = meterBars;
    }

    public void RefreshToggle() => enabledToggle.SetIsOnWithoutNotify(GameplayPlaylist.IsEnabled(clip));

    void Update()
    {
        if (bars == null) return;
        bool playing = AudioManager.I && AudioManager.I.IsPlaylistPreviewPlaying(clip);
        int frame = playing ? Mathf.FloorToInt(Time.unscaledTime * 8f) : -2;
        if (frame == lastFrame) return;
        lastFrame = frame;
        background.color = playing ? new Color(1f, 0.62f, 0.08f, 0.18f) : new Color(1, 1, 1, 0.035f);
        for (int i = 0; i < bars.Length; i++)
        {
            // Quantized heights and eight frames per second keep the meter pixel styled.
            int steps = playing ? 1 + ((frame * (i + 3) + i * 2) % 6) : 1;
            bars[i].sizeDelta = new Vector2(4, steps * 4);
        }
    }
}
