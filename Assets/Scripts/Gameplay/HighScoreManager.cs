using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class HighScoreEntry { public string name; public int score; public string date; }
[Serializable] class HighScoreList { public List<HighScoreEntry> entries = new(); }

public static class HighScoreManager
{
    const string Key = "HighScores_v1";

    public static List<HighScoreEntry> Load()
    {
        var json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return new List<HighScoreEntry>();
        try { return JsonUtility.FromJson<HighScoreList>(json).entries ?? new List<HighScoreEntry>(); }
        catch { return new List<HighScoreEntry>(); }
    }

    public static void Save(List<HighScoreEntry> list)
    {
        var wrap = new HighScoreList { entries = list };
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(wrap));
        PlayerPrefs.Save();
    }

    public static bool IsHighScore(int score, int max = 10)
    {
        var list = Load();
        if (list.Count < max) return score > 0;
        return score > list[list.Count - 1].score;
    }

    public static void Add(string name, int score, int max = 10)
    {
        var list = Load();
        list.Add(new HighScoreEntry { name = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim(), score = score, date = DateTime.Now.ToString("yyyy-MM-dd") });
        list.Sort((a, b) => b.score.CompareTo(a.score)); // desc
        if (list.Count > max) list.RemoveRange(max, list.Count - max);
        Save(list);
    }

    public static void EnsureInitialized(int max = 10)
    {
        var list = Load();
        if (list.Count == 0)
        {
            for (int i = 0; i < max; i++)
                list.Add(new HighScoreEntry { name = "No Name", score = 0, date = "" });
            Save(list);
        }
    }

    public static List<HighScoreEntry> LoadTop(int max = 10, bool pad = true)
    {
        var list = Load();
        list.Sort((a, b) => b.score.CompareTo(a.score));
        if (list.Count > max) list = list.GetRange(0, max);
        if (pad && list.Count < max)
            while (list.Count < max)
                list.Add(new HighScoreEntry { name = "No Name", score = 0, date = "" });
        return list;
    }
}
