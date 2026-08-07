using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ============== Store data for selected character ==============

public static class SelectedCharacterStore
{
    private const string Key = "SelectedCharacterName";
    public static PlayerCharacterData Current; // runtime ref while the app is open

    public static void Save(PlayerCharacterData data)
    {
        Current = data;
        PlayerPrefs.SetString(Key, data ? data.displayName : "");
        PlayerPrefsSaveScheduler.QueueSave();
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

    public static bool HasSavedSelection()
    {
        return !string.IsNullOrEmpty(PlayerPrefs.GetString(Key, ""));
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
        PlayerPrefsSaveScheduler.QueueSave();
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

    public static bool HasSavedSelection()
    {
        return !string.IsNullOrEmpty(PlayerPrefs.GetString(Key, ""));
    }
}

// ============== Store data for settings (volume + cursor) ==============

public static class SettingsStore
{
    // Keys
    private const string KeyMaster = "Settings_MasterVol";
    private const string KeyMusic = "Settings_MusicVol";
    private const string KeySFX = "Settings_SFXVol";
    private const string KeyCursor = "Settings_CursorScale";
    private const string KeyCombatLog = "Settings_CombatLogEnabled";
    private const string KeySkipIntro = "Settings_SkipIntro";

    // Defaults used when no saved data exists
    public const float DefaultMaster = 0.5f;
    public const float DefaultMusic = 1f;
    public const float DefaultSFX = 1f;
    public const float DefaultCursorScale = 1f;
    public const bool DefaultCombatLogEnabled = true;

    // Events
    public static event Action<float> CursorScaleChanged;
    public static event Action<float, float, float> VolumesChanged;

    // ----- Cursor -----
    public static float LoadCursorScale() =>
        PlayerPrefs.GetFloat(KeyCursor, DefaultCursorScale);

    public static void SaveCursorScale(float scale)
    {
        PlayerPrefs.SetFloat(KeyCursor, scale);
        PlayerPrefsSaveScheduler.QueueSave();
        CursorScaleChanged?.Invoke(scale);
    }

    // ----- Volume -----
    public static float LoadMaster() => PlayerPrefs.GetFloat(KeyMaster, DefaultMaster);
    public static float LoadMusic() => PlayerPrefs.GetFloat(KeyMusic, DefaultMusic);
    public static float LoadSFX() => PlayerPrefs.GetFloat(KeySFX, DefaultSFX);

    public static void SaveVolumes(float master, float music, float sfx)
    {
        PlayerPrefs.SetFloat(KeyMaster, master);
        PlayerPrefs.SetFloat(KeyMusic, music);
        PlayerPrefs.SetFloat(KeySFX, sfx);
        PlayerPrefsSaveScheduler.QueueSave();
        VolumesChanged?.Invoke(master, music, sfx);
    }

    // Apply saved values to AudioManager
    public static void ApplySavedVolumesToAudio()
    {
        if (!AudioManager.I) return;

        AudioManager.I.SetMasterVolume(LoadMaster());
        AudioManager.I.SetMusicVolume(LoadMusic());
        AudioManager.I.SetSFXVolume(LoadSFX());
    }

    public static bool LoadCombatLogEnabled() =>
    PlayerPrefs.GetInt(KeyCombatLog, DefaultCombatLogEnabled ? 1 : 0) == 1;

    public static void SaveCombatLogEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(KeyCombatLog, enabled ? 1 : 0);
        PlayerPrefsSaveScheduler.QueueSave();
    }

    public const bool DefaultSkipIntro = false;

    public static bool LoadSkipIntroEnabled() =>
        PlayerPrefs.GetInt(KeySkipIntro, DefaultSkipIntro ? 1 : 0) == 1;

    public static void SaveSkipIntroEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(KeySkipIntro, enabled ? 1 : 0);
        PlayerPrefsSaveScheduler.QueueSave();
    }
}

public static class CommanderBorderFrameStore
{
    const string UnlockPrefix = "unlock_commander_border_anim_";
    const string ActivePrefix = "active_commander_border_anim_";
    const string FinalWinCharacterPrefix = "lt_final_win_char_";

    public static bool HasAnimatedBorder(PlayerCharacterData character)
    {
        return character && character.animatedBorderController;
    }

    public static Sprite GetStaticBorderSprite(PlayerCharacterData character)
    {
        if (!character)
            return null;

        return character.animatedBorderStaticFrame
            ? character.animatedBorderStaticFrame
            : character.defaultBorder;
    }

    public static string GetUnlockKey(string characterAssetName) => UnlockPrefix + characterAssetName;
    public static string GetActiveKey(string characterAssetName) => ActivePrefix + characterAssetName;

    public static bool IsUnlocked(PlayerCharacterData character)
    {
        if (!HasAnimatedBorder(character))
            return false;

        return HasExplicitUnlock(character) || HasCompletionUnlock(character);
    }

    public static bool IsActive(PlayerCharacterData character)
    {
        if (!IsUnlocked(character))
            return false;

        return PlayerPrefs.GetInt(GetActiveKey(character.name), 1) == 1;
    }

    public static void SetActive(PlayerCharacterData character, bool active)
    {
        if (!HasAnimatedBorder(character))
            return;

        if (active && !IsUnlocked(character))
            return;

        PlayerPrefs.SetInt(GetActiveKey(character.name), active ? 1 : 0);
        PlayerPrefsSaveScheduler.QueueSave();
    }

    public static bool ToggleActive(PlayerCharacterData character)
    {
        bool next = !IsActive(character);
        SetActive(character, next);
        return next;
    }

    public static bool UnlockForSuccessfulRun(PlayerCharacterData character)
    {
        if (!HasAnimatedBorder(character) || HasExplicitUnlock(character))
            return false;

        PlayerPrefs.SetInt(GetUnlockKey(character.name), 1);
        PlayerPrefs.SetInt(GetActiveKey(character.name), 1);
        PlayerPrefsSaveScheduler.QueueSave();
        return true;
    }

    public static bool EnsureUnlockedFromExistingProgress(PlayerCharacterData character)
    {
        if (!HasAnimatedBorder(character) || HasExplicitUnlock(character) || !HasCompletionUnlock(character))
            return false;

        return UnlockForSuccessfulRun(character);
    }

    public static void ApplyToImage(Image borderImage, PlayerCharacterData character)
    {
        if (!borderImage)
            return;

        bool useAnimated = IsActive(character);
        Animator animator = borderImage.GetComponent<Animator>();
        Sprite staticBorder = GetStaticBorderSprite(character);

        if (useAnimated)
        {
            if (!animator)
                animator = borderImage.gameObject.AddComponent<Animator>();

            if (staticBorder)
                borderImage.sprite = staticBorder;

            animator.runtimeAnimatorController = character.animatedBorderController;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.enabled = true;
            animator.Rebind();
            return;
        }

        if (animator)
        {
            animator.enabled = false;
            animator.runtimeAnimatorController = null;
        }

        if (staticBorder)
            borderImage.sprite = staticBorder;
    }

    static bool HasExplicitUnlock(PlayerCharacterData character)
    {
        return character && PlayerPrefs.GetInt(GetUnlockKey(character.name), 0) == 1;
    }

    static bool HasCompletionUnlock(PlayerCharacterData character)
    {
        return character &&
               PlayerProgress.I != null &&
               PlayerProgress.I.GetLifetimeInt(FinalWinCharacterPrefix + character.name) > 0;
    }
}
