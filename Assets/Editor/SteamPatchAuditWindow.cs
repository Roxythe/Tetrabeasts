#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SteamPatchAuditWindow
{
    const int ChunkSize = 1024 * 1024;
    const long LargeFileWarningBytes = 100L * 1024L * 1024L;

    [MenuItem("Tools/Tetrabeasts/Steam Patch Audit/Apply Steam-Friendly Standalone Build Settings")]
    public static void ApplySteamFriendlyStandaloneBuildSettings()
    {
        Type buildSettingsType = typeof(EditorUserBuildSettings);
        MethodInfo getCompressionType = buildSettingsType.GetMethod("GetCompressionType", BindingFlags.Public | BindingFlags.Static);
        MethodInfo setCompressionType = buildSettingsType.GetMethod("SetCompressionType", BindingFlags.Public | BindingFlags.Static);
        Type compressionType = buildSettingsType.Assembly.GetType("UnityEditor.Compression");

        if (getCompressionType == null || setCompressionType == null || compressionType == null)
        {
            Debug.LogWarning("Could not apply Steam-friendly compression automatically. In Build Profiles or Build Settings, set Standalone Compression Method to None before building for Steam.");
            return;
        }

        object noneCompression = Enum.Parse(compressionType, "None");
        object previousCompression = getCompressionType.Invoke(null, new object[] { BuildTargetGroup.Standalone });
        setCompressionType.Invoke(null, new object[] { BuildTargetGroup.Standalone, noneCompression });
        object currentCompression = getCompressionType.Invoke(null, new object[] { BuildTargetGroup.Standalone });

        Debug.Log($"Steam-friendly build setting applied. Standalone compression changed from {previousCompression} to {currentCompression}. Rebuild the player and compare it against the previous build before uploading.");
    }

    [MenuItem("Tools/Tetrabeasts/Steam Patch Audit/Compare Build Folders")]
    public static void CompareBuildFolders()
    {
        string previousRoot = EditorUtility.OpenFolderPanel("Select previous exported build folder", string.Empty, string.Empty);
        if (string.IsNullOrWhiteSpace(previousRoot))
            return;

        string currentRoot = EditorUtility.OpenFolderPanel("Select current exported build folder", previousRoot, string.Empty);
        if (string.IsNullOrWhiteSpace(currentRoot))
            return;

        string report = BuildReport(previousRoot, currentRoot);
        string reportDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "SteamPatchAudit"));
        Directory.CreateDirectory(reportDirectory);

        string reportPath = Path.Combine(reportDirectory, $"SteamPatchAudit_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllText(reportPath, report, Encoding.UTF8);

        Debug.Log($"Steam patch audit written to: {reportPath}");
        EditorUtility.RevealInFinder(reportPath);
    }

    public static string BuildReport(string previousRoot, string currentRoot)
    {
        previousRoot = Path.GetFullPath(previousRoot);
        currentRoot = Path.GetFullPath(currentRoot);

        var previousFiles = EnumerateFilesByRelativePath(previousRoot);
        var currentFiles = EnumerateFilesByRelativePath(currentRoot);
        var relativePaths = new SortedSet<string>(previousFiles.Keys, StringComparer.OrdinalIgnoreCase);
        relativePaths.UnionWith(currentFiles.Keys);

        var deltas = new List<FileDelta>();
        foreach (string relativePath in relativePaths)
        {
            previousFiles.TryGetValue(relativePath, out string previousPath);
            currentFiles.TryGetValue(relativePath, out string currentPath);
            deltas.Add(CompareFile(relativePath, previousPath, currentPath));
        }

        deltas.Sort((a, b) => b.EstimatedTransferBytes.CompareTo(a.EstimatedTransferBytes));

        long previousBytes = 0;
        long currentBytes = 0;
        long addedBytes = 0;
        long deletedBytes = 0;
        long estimatedTransferBytes = 0;
        int unchangedCount = 0;
        int modifiedCount = 0;
        int addedCount = 0;
        int deletedCount = 0;

        for (int i = 0; i < deltas.Count; i++)
        {
            var delta = deltas[i];
            previousBytes += delta.PreviousBytes;
            currentBytes += delta.CurrentBytes;
            estimatedTransferBytes += delta.EstimatedTransferBytes;

            switch (delta.Status)
            {
                case FileDeltaStatus.Unchanged:
                    unchangedCount++;
                    break;
                case FileDeltaStatus.Modified:
                    modifiedCount++;
                    break;
                case FileDeltaStatus.Added:
                    addedCount++;
                    addedBytes += delta.CurrentBytes;
                    break;
                case FileDeltaStatus.Deleted:
                    deletedCount++;
                    deletedBytes += delta.PreviousBytes;
                    break;
            }
        }

        var builder = new StringBuilder();
        builder.AppendLine("Steam Patch Audit");
        builder.AppendLine("=================");
        builder.AppendLine($"Previous: {previousRoot}");
        builder.AppendLine($"Current:  {currentRoot}");
        builder.AppendLine();
        builder.AppendLine($"Previous build size: {FormatBytes(previousBytes)}");
        builder.AppendLine($"Current build size:  {FormatBytes(currentBytes)}");
        builder.AppendLine($"Estimated changed 1 MB chunks/new files: {FormatBytes(estimatedTransferBytes)}");
        builder.AppendLine($"Added files: {addedCount} ({FormatBytes(addedBytes)})");
        builder.AppendLine($"Deleted files: {deletedCount} ({FormatBytes(deletedBytes)})");
        builder.AppendLine($"Modified files: {modifiedCount}");
        builder.AppendLine($"Unchanged files: {unchangedCount}");
        builder.AppendLine();
        builder.AppendLine("This is a local estimate, not Steam's exact manifest result. Files with many changed 1 MB chunks are the ones most likely driving large Steam uploads.");
        builder.AppendLine();
        builder.AppendLine("Largest Estimated Upload Contributors");
        builder.AppendLine("--------------------------------------");

        int written = 0;
        for (int i = 0; i < deltas.Count && written < 50; i++)
        {
            var delta = deltas[i];
            if (delta.Status == FileDeltaStatus.Unchanged)
                continue;

            builder.AppendLine(delta.ToReportLine());
            written++;
        }

        builder.AppendLine();
        builder.AppendLine("Large Modified Files To Inspect");
        builder.AppendLine("-------------------------------");

        written = 0;
        for (int i = 0; i < deltas.Count; i++)
        {
            var delta = deltas[i];
            if (delta.Status != FileDeltaStatus.Modified || delta.CurrentBytes < LargeFileWarningBytes)
                continue;

            builder.AppendLine(delta.ToReportLine());
            written++;
        }

        if (written == 0)
            builder.AppendLine("No modified files over 100 MB were found.");

        return builder.ToString();
    }

    static Dictionary<string, string> EnumerateFilesByRelativePath(string root)
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(root))
            return files;

        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            string relativePath = GetRelativePath(root, file).Replace('\\', '/');
            files[relativePath] = file;
        }

        return files;
    }

    static FileDelta CompareFile(string relativePath, string previousPath, string currentPath)
    {
        bool hasPrevious = !string.IsNullOrWhiteSpace(previousPath) && File.Exists(previousPath);
        bool hasCurrent = !string.IsNullOrWhiteSpace(currentPath) && File.Exists(currentPath);

        if (!hasPrevious && hasCurrent)
        {
            long currentBytes = new FileInfo(currentPath).Length;
            return new FileDelta(relativePath, FileDeltaStatus.Added, 0, currentBytes, currentBytes, 0, GetChunkCount(currentBytes));
        }

        if (hasPrevious && !hasCurrent)
        {
            long previousBytes = new FileInfo(previousPath).Length;
            return new FileDelta(relativePath, FileDeltaStatus.Deleted, previousBytes, 0, 0, GetChunkCount(previousBytes), 0);
        }

        if (!hasPrevious || !hasCurrent)
            return new FileDelta(relativePath, FileDeltaStatus.Unchanged, 0, 0, 0, 0, 0);

        var previousInfo = new FileInfo(previousPath);
        var currentInfo = new FileInfo(currentPath);
        int previousChunks = GetChunkCount(previousInfo.Length);
        int currentChunks = GetChunkCount(currentInfo.Length);
        int maxChunks = Math.Max(previousChunks, currentChunks);
        int changedChunks = CountChangedChunks(previousPath, currentPath, maxChunks);

        if (changedChunks == 0 && previousInfo.Length == currentInfo.Length)
            return new FileDelta(relativePath, FileDeltaStatus.Unchanged, previousInfo.Length, currentInfo.Length, 0, previousChunks, currentChunks);

        long estimatedTransferBytes = Math.Min(currentInfo.Length, (long)changedChunks * ChunkSize);
        return new FileDelta(relativePath, FileDeltaStatus.Modified, previousInfo.Length, currentInfo.Length, estimatedTransferBytes, previousChunks, currentChunks);
    }

    static int CountChangedChunks(string previousPath, string currentPath, int maxChunks)
    {
        int changedChunks = 0;
        byte[] previousBuffer = new byte[ChunkSize];
        byte[] currentBuffer = new byte[ChunkSize];

        using (var previousStream = File.OpenRead(previousPath))
        using (var currentStream = File.OpenRead(currentPath))
        {
            for (int chunk = 0; chunk < maxChunks; chunk++)
            {
                int previousRead = ReadChunk(previousStream, previousBuffer);
                int currentRead = ReadChunk(currentStream, currentBuffer);

                if (previousRead != currentRead || !BuffersEqual(previousBuffer, currentBuffer, previousRead))
                    changedChunks++;
            }
        }

        return changedChunks;
    }

    static int ReadChunk(Stream stream, byte[] buffer)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = stream.Read(buffer, totalRead, buffer.Length - totalRead);
            if (read == 0)
                break;

            totalRead += read;
        }

        return totalRead;
    }

    static bool BuffersEqual(byte[] left, byte[] right, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (left[i] != right[i])
                return false;
        }

        return true;
    }

    static int GetChunkCount(long byteCount)
    {
        if (byteCount <= 0)
            return 0;

        return (int)((byteCount + ChunkSize - 1) / ChunkSize);
    }

    static string GetRelativePath(string root, string path)
    {
        if (!root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            root += Path.DirectorySeparatorChar;

        var rootUri = new Uri(root);
        var pathUri = new Uri(path);
        return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
    }

    static string FormatBytes(long byteCount)
    {
        const double kb = 1024.0;
        const double mb = kb * 1024.0;
        const double gb = mb * 1024.0;

        if (byteCount >= gb)
            return $"{byteCount / gb:0.00} GB";

        if (byteCount >= mb)
            return $"{byteCount / mb:0.00} MB";

        if (byteCount >= kb)
            return $"{byteCount / kb:0.00} KB";

        return $"{byteCount} B";
    }

    enum FileDeltaStatus
    {
        Unchanged,
        Modified,
        Added,
        Deleted
    }

    readonly struct FileDelta
    {
        public readonly string RelativePath;
        public readonly FileDeltaStatus Status;
        public readonly long PreviousBytes;
        public readonly long CurrentBytes;
        public readonly long EstimatedTransferBytes;
        public readonly int PreviousChunks;
        public readonly int CurrentChunks;

        public FileDelta(string relativePath, FileDeltaStatus status, long previousBytes, long currentBytes,
                         long estimatedTransferBytes, int previousChunks, int currentChunks)
        {
            RelativePath = relativePath;
            Status = status;
            PreviousBytes = previousBytes;
            CurrentBytes = currentBytes;
            EstimatedTransferBytes = estimatedTransferBytes;
            PreviousChunks = previousChunks;
            CurrentChunks = currentChunks;
        }

        public string ToReportLine()
        {
            return $"{Status,-8} {FormatBytes(EstimatedTransferBytes),10} changed | old {FormatBytes(PreviousBytes),10} | new {FormatBytes(CurrentBytes),10} | chunks {PreviousChunks}->{CurrentChunks} | {RelativePath}";
        }
    }
}
#endif
