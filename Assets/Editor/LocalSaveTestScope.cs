#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Shared by the file-backed playlist and Marathon tests, including domain reloads.
[InitializeOnLoad]
public static class LocalSaveTestScope
{
    const string Key = "Tetrabeasts.LocalSaveTests.Directory";
    static LocalSaveTestScope() { Apply(); }
    public static void Begin()
    {
        SessionState.SetString(Key, Path.Combine(Path.GetTempPath(), "TetrabeastsSaveTests", Guid.NewGuid().ToString("N")));
        Apply();
    }
    public static void Apply()
    {
        string path = SessionState.GetString(Key, "");
        var field = typeof(LocalSaveFiles).GetField("testDirectory", BindingFlags.Static | BindingFlags.NonPublic);
        if (field == null && !string.IsNullOrEmpty(path)) throw new InvalidOperationException("Local save test isolation is unavailable.");
        field?.SetValue(null, path);
        typeof(GameplayPlaylist).GetField("loadedJson", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
    }
    public static void End()
    {
        string path = SessionState.GetString(Key, "");
        SessionState.EraseString(Key);
        Apply();
        string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "TetrabeastsSaveTests")) + Path.DirectorySeparatorChar;
        if (!string.IsNullOrEmpty(path) && Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase) && Directory.Exists(path))
            Directory.Delete(path, true);
    }

    // Use the game's existing, complete preference key catalog so scene-level
    // tests can restore both existing keys and keys created during the test.
    static object CapturePreferences()
    {
        var type = typeof(SteamCloudSaveService).GetNestedType("CloudSaveSnapshot", BindingFlags.NonPublic);
        var snapshot = Activator.CreateInstance(type);
        typeof(SteamCloudSaveService).GetMethod("CapturePlayerPrefs", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new[] { snapshot });
        return snapshot;
    }
    public static void BackupScenePreferences() => SessionState.SetString(Key + ".Prefs", JsonUtility.ToJson(CapturePreferences()));
    public static void RestoreScenePreferences()
    {
        string json = SessionState.GetString(Key + ".Prefs", "");
        if (string.IsNullOrEmpty(json)) return;
        var current = CapturePreferences();
        var previous = JsonUtility.FromJson(json, current.GetType());
        foreach (string field in new[] { "stringPrefs", "intPrefs", "floatPrefs" })
        {
            var listField = current.GetType().GetField(field);
            foreach (var entry in (IEnumerable)listField.GetValue(current))
                PlayerPrefs.DeleteKey((string)entry.GetType().GetField("key").GetValue(entry));
            foreach (var entry in (IEnumerable)listField.GetValue(previous))
            {
                string key = (string)entry.GetType().GetField("key").GetValue(entry);
                object value = entry.GetType().GetField("value").GetValue(entry);
                if (field == "stringPrefs") PlayerPrefs.SetString(key, (string)value);
                else if (field == "intPrefs") PlayerPrefs.SetInt(key, (int)value);
                else PlayerPrefs.SetFloat(key, (float)value);
            }
        }
        PlayerPrefs.Save();
        SessionState.EraseString(Key + ".Prefs");
    }
}
#endif
