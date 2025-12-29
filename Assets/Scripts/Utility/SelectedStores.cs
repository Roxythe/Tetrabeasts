using System.Collections.Generic;
using UnityEngine;

// ============== Store data for selected character ==============

public static class SelectedCharacterStore
{
    private const string Key = "SelectedCharacterName";
    public static PlayerCharacterData Current; // runtime ref while the app is open

    public static void Save(PlayerCharacterData data)
    {
        Current = data;
        PlayerPrefs.SetString(Key, data ? data.displayName : "");
        PlayerPrefs.Save();
    }

    public static string LoadSavedName()
    {
        return PlayerPrefs.GetString(Key, "");
    }

    // Helper: resolve saved name to an asset from a roster
    public static PlayerCharacterData ResolveFromRoster(PlayerCharacterData[] roster)
    {
        var name = LoadSavedName();
        if (string.IsNullOrEmpty(name) || roster == null) return null;
        for (int i = 0; i < roster.Length; i++)
            if (roster[i] && roster[i].displayName == name)
                return roster[i];
        return null;
    }
}

// ============== Store data for selected monster pieces ==============

public static class SelectedMonstersStore
{
    private const string Key = "SelectedMonstersCSV";

    // Runtime refs while the app is open (populated by MonsterSelectUI)
    public static List<MonsterData> Active = new List<MonsterData>();

    public static void SaveNames(List<MonsterData> list)
    {
        var names = new List<string>();
        if (list != null)
            foreach (var m in list)
                if (m) names.Add(m.monsterName);

        PlayerPrefs.SetString(Key, string.Join(",", names));
        PlayerPrefs.Save();
    }

    public static List<string> LoadSavedNames()
    {
        var csv = PlayerPrefs.GetString(Key, "");
        var result = new List<string>();
        if (string.IsNullOrEmpty(csv)) return result;
        var parts = csv.Split(',');
        foreach (var p in parts) { var t = p.Trim(); if (!string.IsNullOrEmpty(t)) result.Add(t); }
        return result;
    }

    // Helper to resolve saved names to assets using a roster
    public static List<MonsterData> ResolveFromRoster(MonsterData[] roster)
    {
        var outList = new List<MonsterData>();
        var names = LoadSavedNames();
        if (roster == null || names.Count == 0) return outList;

        foreach (var name in names)
        {
            for (int i = 0; i < roster.Length; i++)
                if (roster[i] && roster[i].monsterName == name)
                { outList.Add(roster[i]); break; }
        }
        return outList;
    }
}
