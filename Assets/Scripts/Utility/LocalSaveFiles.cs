using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>Durable local saves: write and flush a new file before replacing the last good copy.</summary>
public static class LocalSaveFiles
{
#if UNITY_INCLUDE_TESTS
    internal static string testDirectory;
#endif
    public static string DirectoryPath
    {
        get
        {
#if UNITY_INCLUDE_TESTS
            if (!string.IsNullOrEmpty(testDirectory)) return testDirectory;
#endif
            return Application.persistentDataPath;
        }
    }

    public static bool Write(string path, string json, bool preserveBackup = false)
    {
        string staging = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            using (var file = new FileStream(staging, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                file.Write(bytes, 0, bytes.Length);
                file.Flush(true);
            }
            if (File.Exists(path)) File.Replace(staging, path, preserveBackup ? null : path + ".bak");
            else File.Move(staging, path);
            return true;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Debug.LogWarning($"Local save could not be written: {ex.Message}");
            return false;
        }
    }

    public static bool Read<T>(string path, Func<T, bool> validate, out T data) where T : class
    {
        foreach (string candidate in new[] { path, path + ".bak" })
        {
            try
            {
                if (!File.Exists(candidate)) continue;
                var parsed = JsonUtility.FromJson<T>(File.ReadAllText(candidate));
                if (parsed != null && validate(parsed))
                {
                    if (candidate != path)
                        Write(path, JsonUtility.ToJson(parsed, true), preserveBackup: true);
                    data = parsed;
                    return true;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { Debug.LogWarning($"Local save could not be read: {ex.Message}"); }
        }
        data = null;
        return false;
    }

    public static void Delete(string path)
    {
        foreach (string suffix in new[] { "", ".bak", ".tmp" })
            if (File.Exists(path + suffix)) File.Delete(path + suffix);
    }
}
