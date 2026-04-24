using System;
using UnityEngine;

public static class CodexProgressStore
{
    const string RunModifierPrefix = "codex_run_modifier_";
    const string LevelModifierPrefix = "codex_level_modifier_";

    public static event Action ProgressChanged;

    public static bool IsUnlocked(RunModifierSO modifier)
    {
        return IsUnlocked(BuildRunModifierKey(modifier));
    }

    public static bool IsUnlocked(LevelModifierSO modifier)
    {
        return IsUnlocked(BuildLevelModifierKey(modifier));
    }

    public static void Unlock(RunModifierSO modifier)
    {
        Unlock(BuildRunModifierKey(modifier));
    }

    public static void Unlock(LevelModifierSO modifier)
    {
        Unlock(BuildLevelModifierKey(modifier));
    }

    static bool IsUnlocked(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    static void Unlock(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (PlayerPrefs.GetInt(key, 0) == 1)
            return;

        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        ProgressChanged?.Invoke();
    }

    static string BuildRunModifierKey(RunModifierSO modifier)
    {
        return BuildKey(RunModifierPrefix, modifier);
    }

    static string BuildLevelModifierKey(LevelModifierSO modifier)
    {
        return BuildKey(LevelModifierPrefix, modifier);
    }

    static string BuildKey(string prefix, UnityEngine.Object asset)
    {
        if (!asset)
            return string.Empty;

        string stableName = string.IsNullOrWhiteSpace(asset.name) ? asset.GetInstanceID().ToString() : asset.name.Trim();
        return prefix + stableName;
    }
}
