using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class HighScoreEntry { public string name; public int score; public string date; }
[Serializable] class HighScoreList { public List<HighScoreEntry> entries = new(); }

public static class HighScoreManager
{
    public const string LegacyMigrationKey = "HighScores_Standard_LegacySnapshot";
    public static string KeyForMode(RunGameMode mode) => "HighScores_v2_" + mode;

    public static List<HighScoreEntry> Load(RunGameMode mode = RunGameMode.Standard)
    {
        if (mode == RunGameMode.Standard) MigrateLegacyScores();
        var json = PlayerPrefs.GetString(KeyForMode(mode), "");
        return Parse(json);
    }

    static List<HighScoreEntry> Parse(string json)
    {
        if (string.IsNullOrEmpty(json)) return new List<HighScoreEntry>();
        try { return JsonUtility.FromJson<HighScoreList>(json).entries ?? new List<HighScoreEntry>(); }
        catch { return new List<HighScoreEntry>(); }
    }

    static void MigrateLegacyScores()
    {
        string legacy = PlayerPrefs.GetString("HighScores_v1", "");
        if (string.IsNullOrEmpty(legacy) || PlayerPrefs.GetString(LegacyMigrationKey, "") == legacy) return;
        var oldScores = Parse(legacy);
        if (oldScores.Count == 0) return; // Retry if a valid older cloud save arrives later.
        var merged = Parse(PlayerPrefs.GetString(KeyForMode(RunGameMode.Standard), ""));
        foreach (var entry in oldScores)
            if (entry != null && entry.score > 0 && !merged.Exists(e => e != null && e.name == entry.name && e.score == entry.score && e.date == entry.date))
                merged.Add(entry);
        merged.RemoveAll(e => e == null || e.score <= 0);
        merged.Sort((a, b) => b.score.CompareTo(a.score));
        if (merged.Count > 10) merged.RemoveRange(10, merged.Count - 10);
        PlayerPrefs.SetString(LegacyMigrationKey, legacy);
        Save(merged, RunGameMode.Standard);
        // Keep the original key intact as a recovery copy.
    }

    public static void Save(List<HighScoreEntry> list, RunGameMode mode = RunGameMode.Standard)
    {
        var wrap = new HighScoreList { entries = list };
        PlayerPrefs.SetString(KeyForMode(mode), JsonUtility.ToJson(wrap));
        PlayerPrefsSaveScheduler.QueueSave();
    }

    public static bool IsHighScore(int score, int max = 10, RunGameMode mode = RunGameMode.Standard)
    {
        var list = Load(mode);
        if (list.Count < max) return score > 0;
        return score > list[list.Count - 1].score;
    }

    public static int Add(string name, int score, int max = 10, RunGameMode mode = RunGameMode.Standard)
    {
        var list = Load(mode);

        var entry = new HighScoreEntry
        {
            name = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim(),
            score = score,
            date = DateTime.Now.ToString("yyyy-MM-dd")
        };

        list.Add(entry);
        list.Sort((a, b) => b.score.CompareTo(a.score)); // Descending order

        int rankIndex = list.IndexOf(entry);

        if (list.Count > max)
            list.RemoveRange(max, list.Count - max);

        if (rankIndex >= max) rankIndex = -1; // If it got trimmed out don't highlight anything

        Save(list, mode);
        return rankIndex;
    }

    public static void EnsureInitialized(int max = 10, RunGameMode mode = RunGameMode.Standard)
    {
        var list = Load(mode);
        if (list.Count == 0)
        {
            for (int i = 0; i < max; i++)
                list.Add(new HighScoreEntry { name = "......", score = 0, date = "" });
            Save(list, mode);
        }
    }

    public static List<HighScoreEntry> LoadTop(int max = 10, bool pad = true, RunGameMode mode = RunGameMode.Standard)
    {
        var list = Load(mode);
        list.Sort((a, b) => b.score.CompareTo(a.score));
        if (list.Count > max) list = list.GetRange(0, max);
        if (pad && list.Count < max)
            while (list.Count < max)
                list.Add(new HighScoreEntry { name = "......", score = 0, date = "" });
        return list;
    }
}
