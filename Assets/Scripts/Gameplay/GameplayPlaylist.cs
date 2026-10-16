using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Only exclusions are saved, so new tracks and first launches default to enabled.</summary>
public static class GameplayPlaylist
{
    public const string PreferenceKey = "gameplay_playlist_disabled_v1";
    public const string FileName = "tetrabeasts_playlist.json";
    const string PendingWriteKey = "gameplay_playlist_pending_file_write";
    [Serializable] sealed class SavedSongs { public int version = 1; public List<string> songs = new(); }
    static string loadedJson;
    static string loadedPath;
    static string SavePath => Path.Combine(LocalSaveFiles.DirectoryPath, FileName);
    static readonly HashSet<string> disabled = new(StringComparer.Ordinal);

    public static bool IsEnabled(AudioClip clip)
    {
        ReloadIfChanged();
        return clip && !disabled.Contains(clip.name);
    }

    public static void SetEnabled(AudioClip clip, bool enabled)
    {
        if (!clip) return;
        ReloadIfChanged();
        bool changed = enabled ? disabled.Remove(clip.name) : disabled.Add(clip.name);
        if (!changed) return;
        var saved = new SavedSongs { songs = new List<string>(disabled) };
        saved.songs.Sort(StringComparer.Ordinal);
        loadedJson = JsonUtility.ToJson(saved);
        bool written = LocalSaveFiles.Write(SavePath, loadedJson);
        PlayerPrefs.SetInt(PendingWriteKey, written ? 0 : 1);
        if (!written)
            Debug.LogWarning("Playlist settings remain in local preferences; the settings file will be retried next launch.");
        PlayerPrefs.SetString(PreferenceKey, loadedJson);
        PlayerPrefsSaveScheduler.QueueSave();
    }

    static void ReloadIfChanged()
    {
        if (loadedPath != SavePath || loadedJson == null)
        {
            loadedPath = SavePath;
            if (PlayerPrefs.GetInt(PendingWriteKey, 0) != 0)
            {
                if (LocalSaveFiles.Write(SavePath, PlayerPrefs.GetString(PreferenceKey, "{\"version\":1,\"songs\":[]}")))
                {
                    PlayerPrefs.SetInt(PendingWriteKey, 0);
                    PlayerPrefsSaveScheduler.QueueSave();
                }
            }
            else if (LocalSaveFiles.Read<SavedSongs>(SavePath, s => s.version == 1 && s.songs != null, out var saved))
                PlayerPrefs.SetString(PreferenceKey, JsonUtility.ToJson(saved));
            else if (!File.Exists(SavePath))
            {
                // Migrate the earlier preferences-only playlist without losing exclusions.
                string previous = NormalizePreferenceJson(PlayerPrefs.GetString(PreferenceKey, ""));
                LocalSaveFiles.Write(SavePath, previous);
                PlayerPrefs.SetString(PreferenceKey, previous);
            }
        }
        string json = PlayerPrefs.GetString(PreferenceKey, string.Empty);
        if (json == loadedJson) return;
        loadedJson = json;
        disabled.Clear();
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            var saved = JsonUtility.FromJson<SavedSongs>(json);
            if (saved?.songs != null) disabled.UnionWith(saved.songs);
        }
        catch (ArgumentException) { /* An invalid preference falls back to all songs enabled. */ }
    }

    public static void ApplyCloudPreferenceToLocalFile()
    {
        string json = NormalizePreferenceJson(PlayerPrefs.GetString(PreferenceKey, ""));
        PlayerPrefs.SetString(PreferenceKey, json);
        PlayerPrefs.SetInt(PendingWriteKey, LocalSaveFiles.Write(SavePath, json) ? 0 : 1);
        loadedJson = null;
    }

    static string NormalizePreferenceJson(string json)
    {
        SavedSongs saved = null;
        try { if (!string.IsNullOrEmpty(json)) saved = JsonUtility.FromJson<SavedSongs>(json); }
        catch (ArgumentException) { }
        saved ??= new SavedSongs();
        saved.version = 1;
        saved.songs ??= new List<string>();
        return JsonUtility.ToJson(saved);
    }

    // Asset identities and display titles are independent of pool ordering and user language.
    static readonly Dictionary<string, string> Titles = new(StringComparer.Ordinal)
    {
        { "EDMJazz_Gameplay_v1", "Neon Battlements" },
        { "EDMJazz_Gameplay_v2", "Midnight Circuit" },
        { "EDMJazz_Gameplay_v3", "Chromatic Castle" },
        { "EDMViolin_Gameplay_v1", "Crystal Current" },
        { "EDMViolin_Gameplay_v2", "Starlight Strings" },
        { "EDMViolin_Gameplay_v3", "Prismatic Rush" },
        { "EDMSwingJazz_GP", "Clockwork Shuffle" },
        { "EDM_GPMusic(1)", "Pixel Skyline" },
        { "EDM_GPMusic(2)", "Voltage Garden" },
        { "EDM_GPMusic(3)", "Moonlit Machine" },
        { "EDM_GPMusic(4)", "Arcade Aurora" },
        { "EDM_GPMusic(5)", "Gravity Groove" },
        { "EDM_GPMusic(6)", "Luminous Labyrinth" },
        { "EDM_GPMusic(7)", "Turbo Twilight" },
        { "EDM_GPMusic(8)", "Spectrum Sprint" },
        { "EDM_GPMusic(9)", "Digital Daybreak" },
        { "EDM_GPMusic(10)", "Afterglow Ascent" },
        { "ViolinEDM_GPMusic(1)", "Silver Arpeggio" },
        { "ViolinEDM_GPMusic(2)", "Bowstring Binary" },
        { "ViolinEDM_GPMusic(3)", "Electric Serenade" },
        { "ViolinEDM_GPMusic(4)", "Skyward Symphony" },
        { "EDMViolin_BossFight_v1", "Crown of Sparks" },
        { "EDMJazz_BossFight_v4", "Royal Overdrive" },
        { "EDMJazz_BossFight_v3", "Thunder in the Ballroom" },
        { "EDMJazz_BossFight_v1", "Checkmate at Midnight" },
        { "EDMSwingJazz_BF", "The Last Swing" },
        { "EDM_BFMusic(1)", "Neon Nemesis" },
        { "EDM_BFMusic(2)", "Overclocked Throne" },
        { "EDM_BFMusic(3)", "Cathedral of Bass" },
        { "EDM_BFMusic(4)", "Final Frequency" },
        { "EDM_BFMusic(5)", "Prism Breaker" },
        { "ViolinEDMSwingJazz_BossMusic(1)", "Waltz of the Warlord" },
        { "ViolinEDMSwingJazz_BossMusic(2)", "Fiddle of Fury" },
        { "ViolinEDMSwingJazz_BossMusic(3)", "Sovereign Surge" },
        { "ViolinEDMSwingJazz_BossMusic(4)", "Stormstring Duel" },
        { "ViolinEDMSwingJazz_BossMusic(5)", "Encore of the Eclipse" },
        { "Metal_Gameplay_v1", "Iron Horizon" },
        { "Metal_Gameplay_v2", "Ember March" },
        { "Metal_Gameplay_v3", "Steel and Stone" },
        { "PowerBallad_Gameplay_v1", "Ashes into Dawn" },
        { "Medieval_GameplayFadeIn_Music", "Banner of the Fallen" },
        { "Metal_Gameplay_v4", "Forgefire Run" },
        { "Metal_Gameplay_v5", "Blackstone Road" },
        { "Metal_GPMusic(1)", "Rivets and Ruin" },
        { "Metal_GPMusic(2)", "Crimson Anvil" },
        { "Metal_GPMusic(3)", "Hammerheart" },
        { "Metal_BossFight_v1", "Siegebreaker" },
        { "Metal_BossFight_v2", "Tyrant of Cinders" },
        { "PowerBallad_BossFight_v1", "Until the Ramparts Fall" },
        { "MetalBossFightBG_Music", "Obsidian Crown" },
        { "Metal_BossFight(1)", "Dragon of the Deep" },
        { "Metal_BossFight(2)", "War Drum Eclipse" },
        { "Metal_BossFight(3)", "The Iron Reckoning" },
        { "Metal_BossFight(4)", "Last Stand at Blackspire" }
    };

    public static string Title(AudioClip clip) => clip && Titles.TryGetValue(clip.name, out var title)
        ? title : clip ? clip.name : string.Empty;
}
