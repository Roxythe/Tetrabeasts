using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
using Steamworks;
#endif

[DisallowMultipleComponent]
public sealed class SteamCloudSaveService : MonoBehaviour
{
    const int SnapshotVersion = 1;
    const string CloudFileName = "tetrabeasts_cloud_save_v1.json";
    const string ProgressFileName = "tetrabeasts_progress.json";
    const string TempRunFileName = "tetrabeasts_temp_run.json";
    const string KeyLastAppliedTicks = "SteamCloud_LastAppliedUtcTicks";
    const string KeyLastUploadedTicks = "SteamCloud_LastUploadedUtcTicks";

    static SteamCloudSaveService _instance;
    public static SteamCloudSaveService Instance => _instance;

    [SerializeField] bool syncOnStart = true;
    [SerializeField] bool uploadOnChanges = true;
    [SerializeField, Min(0.25f)] float uploadDebounceSeconds = 2f;
    [SerializeField] bool deferQueuedUploadsDuringGameplay = true;
    [SerializeField] bool logCloudStatus = true;

    Coroutine uploadCoroutine;
    Coroutine prewarmAssetNameCacheCoroutine;
    bool uploadQueued;
    bool restoreAttempted;
    GameController cachedGameController;
    static readonly List<(string assetName, string displayName)> MonsterAssetNameCache = new();
    static readonly List<string> CharacterAssetNameCache = new();
    static readonly List<string> RunModifierAssetNameCache = new();
    static readonly List<string> LevelModifierAssetNameCache = new();
    static bool assetNameCachesReady;

    public string LastStatus { get; private set; } = "Steam Cloud is not initialized.";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Application.isPlaying)
            Ensure();
    }

    public static SteamCloudSaveService Ensure()
    {
        if (_instance)
            return _instance;

        var existing = FindFirstObjectByType<SteamCloudSaveService>(FindObjectsInactive.Include);
        if (existing)
        {
            _instance = existing;
            if (!_instance.gameObject.activeSelf)
                _instance.gameObject.SetActive(true);

            return _instance;
        }

        var go = new GameObject("SteamCloudSaveService");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<SteamCloudSaveService>();
        return _instance;
    }

    public static void QueueUpload()
    {
        if (!Application.isPlaying)
            return;

        Ensure()?.QueueUploadInternal();
    }

    public static bool TryUploadNow()
    {
        if (!Application.isPlaying)
            return false;

        var service = Ensure();
        return service && service.TryUploadSnapshot();
    }

    void Awake()
    {
        if (_instance && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (syncOnStart)
            TryRestoreFromCloud();
    }

    void Start()
    {
        if (syncOnStart)
            TryRestoreFromCloud();

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (prewarmAssetNameCacheCoroutine == null)
            prewarmAssetNameCacheCoroutine = StartCoroutine(PrewarmAssetNameCaches());
#endif
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
            TryUploadSnapshot();
    }

    void OnApplicationQuit()
    {
        TryUploadSnapshot();
    }

    void QueueUploadInternal()
    {
        if (!uploadOnChanges)
            return;

        uploadQueued = true;

        if (uploadCoroutine == null && isActiveAndEnabled)
            uploadCoroutine = StartCoroutine(UploadAfterDebounce());
    }

    IEnumerator UploadAfterDebounce()
    {
        while (uploadQueued)
        {
            uploadQueued = false;
            yield return new WaitForSecondsRealtime(uploadDebounceSeconds);
            yield return WaitForSafeQueuedUploadMoment();
            TryUploadSnapshot();
        }

        uploadCoroutine = null;
    }

    IEnumerator WaitForSafeQueuedUploadMoment()
    {
        if (!deferQueuedUploadsDuringGameplay)
            yield break;

        while (ShouldDeferQueuedUpload())
            yield return null;
    }

    bool ShouldDeferQueuedUpload()
    {
        if (LoadingScreen.IsVisible)
            return true;

        if (!cachedGameController)
            cachedGameController = FindFirstObjectByType<GameController>(FindObjectsInactive.Exclude);

        return cachedGameController && cachedGameController.IsRoundActive;
    }

    IEnumerator PrewarmAssetNameCaches()
    {
        yield return null;
        yield return null;

        while (LoadingScreen.IsVisible)
            yield return null;

        EnsureAssetNameCaches();
        prewarmAssetNameCacheCoroutine = null;
    }

    public bool TryRestoreFromCloud()
    {
        if (restoreAttempted)
            return false;

        restoreAttempted = true;

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (!IsSteamCloudAvailable())
            return false;

        try
        {
            if (!SteamRemoteStorage.FileExists(CloudFileName))
            {
                LastStatus = "No Steam Cloud save exists yet.";
                if (HasAnyLocalSaveData())
                    QueueUploadInternal();
                return false;
            }

            int size = SteamRemoteStorage.GetFileSize(CloudFileName);
            if (size <= 0)
                return false;

            var bytes = new byte[size];
            int read = SteamRemoteStorage.FileRead(CloudFileName, bytes, size);
            if (read <= 0)
                return false;

            string json = Encoding.UTF8.GetString(bytes, 0, read);
            var snapshot = JsonUtility.FromJson<CloudSaveSnapshot>(json);
            if (snapshot == null || snapshot.version <= 0)
                return false;

            long cloudTicks = Math.Max(snapshot.updatedUtcTicks, GetCloudFileTimestampTicks());
            long lastAppliedTicks = LoadTicks(KeyLastAppliedTicks);
            long localFileTicks = GetLocalFileContentTicks();

            if (lastAppliedTicks > 0 && cloudTicks <= lastAppliedTicks)
            {
                LastStatus = "Local save is already current with Steam Cloud.";
                return false;
            }

            if (localFileTicks > 0 && cloudTicks <= localFileTicks)
            {
                LastStatus = "Local save is newer than Steam Cloud; queued upload.";
                QueueUploadInternal();
                return false;
            }

            if (lastAppliedTicks == 0 && localFileTicks == 0 && HasAnyLocalPlayerPrefsData())
            {
                LastStatus = "Local PlayerPrefs data exists with no cloud sync marker; queued upload to avoid overwriting local progress.";
                QueueUploadInternal();
                return false;
            }

            ApplySnapshot(snapshot);
            SaveTicks(KeyLastAppliedTicks, cloudTicks);
            SaveTicks(KeyLastUploadedTicks, cloudTicks);
            PlayerPrefs.Save();

            PlayerProgress.I?.ReloadFromDiskAfterCloudImport();

            LastStatus = "Steam Cloud save restored.";
            if (logCloudStatus)
                Debug.Log(LastStatus);

            return true;
        }
        catch (Exception ex)
        {
            LastStatus = $"Steam Cloud restore failed: {ex.Message}";
            if (logCloudStatus)
                Debug.LogWarning(LastStatus);
            return false;
        }
#else
        LastStatus = "Steamworks.NET is not enabled for this build.";
        return false;
#endif
    }

    bool TryUploadSnapshot()
    {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (!IsSteamCloudAvailable())
            return false;

        try
        {
            var snapshot = BuildSnapshot();
            string json = JsonUtility.ToJson(snapshot, prettyPrint: true);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            bool batchStarted = SteamRemoteStorage.BeginFileWriteBatch();
            bool wrote = SteamRemoteStorage.FileWrite(CloudFileName, bytes, bytes.Length);
            SteamRemoteStorage.SetSyncPlatforms(CloudFileName, ERemoteStoragePlatform.k_ERemoteStoragePlatformAll);
            if (batchStarted)
                SteamRemoteStorage.EndFileWriteBatch();

            if (!wrote)
            {
                LastStatus = "Steam Cloud upload failed.";
                if (logCloudStatus)
                    Debug.LogWarning(LastStatus);
                return false;
            }

            SaveTicks(KeyLastAppliedTicks, snapshot.updatedUtcTicks);
            SaveTicks(KeyLastUploadedTicks, snapshot.updatedUtcTicks);
            PlayerPrefs.Save();

            LastStatus = "Steam Cloud save uploaded.";
            if (logCloudStatus)
                Debug.Log(LastStatus);

            return true;
        }
        catch (Exception ex)
        {
            LastStatus = $"Steam Cloud upload failed: {ex.Message}";
            if (logCloudStatus)
                Debug.LogWarning(LastStatus);
            return false;
        }
#else
        LastStatus = "Steamworks.NET is not enabled for this build.";
        return false;
#endif
    }

    bool IsSteamCloudAvailable()
    {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        var steam = SteamPlatformService.Ensure();
        if (!steam || !steam.IsAvailable)
        {
            LastStatus = steam ? steam.LastStatus : "Steam API did not initialize.";
            return false;
        }

        if (!SteamRemoteStorage.IsCloudEnabledForAccount())
        {
            LastStatus = "Steam Cloud is disabled for this Steam account.";
            return false;
        }

        if (!SteamRemoteStorage.IsCloudEnabledForApp())
        {
            LastStatus = "Steam Cloud is disabled for this app in Steam.";
            return false;
        }

        return true;
#else
        LastStatus = "Steamworks.NET is not enabled for this build.";
        return false;
#endif
    }

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
    static long GetCloudFileTimestampTicks()
    {
        long timestamp = SteamRemoteStorage.GetFileTimestamp(CloudFileName);
        return timestamp > 0
            ? DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime.Ticks
            : 0;
    }
#endif

    CloudSaveSnapshot BuildSnapshot()
    {
        var snapshot = new CloudSaveSnapshot
        {
            version = SnapshotVersion,
            updatedUtcTicks = DateTime.UtcNow.Ticks,
            unityVersion = Application.unityVersion,
            platform = Application.platform.ToString()
        };

        CaptureFiles(snapshot);
        CapturePlayerPrefs(snapshot);
        return snapshot;
    }

    static void CaptureFiles(CloudSaveSnapshot snapshot)
    {
        AddFile(snapshot, ProgressFileName);
        AddFile(snapshot, TempRunFileName);
    }

    static void AddFile(CloudSaveSnapshot snapshot, string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        var entry = new CloudFileEntry { name = fileName, exists = File.Exists(path) };

        if (entry.exists)
            entry.contents = File.ReadAllText(path);

        snapshot.files.Add(entry);
    }

    static void CapturePlayerPrefs(CloudSaveSnapshot snapshot)
    {
        var strings = new HashSet<string>
        {
            "SelectedCharacterName",
            "SelectedMonstersCSV",
            "HighScores_v1",
            "Settings_LanguageCode",
            "Settings_ControlBindings_KeyboardMouse",
            "Settings_ControlBindings_XboxController",
            "Settings_ControlBindings_PlayStationController",
            "Settings_ControlBindings_HandheldController"
        };

        var ints = new HashSet<string>
        {
            "CURRENCY_TOTAL",
            "Settings_CombatLogEnabled",
            "Settings_SkipIntro",
            "Settings_ControlProfile",
            "music_mode",
            "Tetrabeasts_PostFinalSurvivalIntroHidden"
        };

        var floats = new HashSet<string>
        {
            "Settings_MasterVol",
            "Settings_MusicVol",
            "Settings_SFXVol",
            "Settings_CursorScale",
            "vol_master",
            "vol_music",
            "vol_sfx"
        };

        foreach (var type in ShopBuffStore.AllTypes)
            ints.Add($"ShopBuff_{type}");

        foreach (var monsterName in GetMonsterAssetNames())
        {
            floats.Add($"MonsterXP_Total_{monsterName.displayName}");
            ints.Add($"unlock_mon_{monsterName.assetName}");
            ints.Add($"skin_sel_{monsterName.assetName}");
            ints.Add($"skin_last_{monsterName.assetName}");
            for (int i = 1; i <= MonsterSkinStore.MaxSkinIndex; i++)
                ints.Add($"skin_unl_{monsterName.assetName}_{i}");
        }

        foreach (var characterName in GetCharacterAssetNames())
        {
            ints.Add($"unlock_char_{characterName}");
            ints.Add(CommanderBorderFrameStore.GetUnlockKey(characterName));
            ints.Add(CommanderBorderFrameStore.GetActiveKey(characterName));
        }

        foreach (var modifierName in GetRunModifierAssetNames())
            ints.Add($"codex_run_modifier_{modifierName}");

        foreach (var modifierName in GetLevelModifierAssetNames())
            ints.Add($"codex_level_modifier_{modifierName}");

        AddStringPrefs(snapshot, strings);
        AddIntPrefs(snapshot, ints);
        AddFloatPrefs(snapshot, floats);
    }

    static void AddStringPrefs(CloudSaveSnapshot snapshot, IEnumerable<string> keys)
    {
        foreach (string key in keys)
        {
            if (!PlayerPrefs.HasKey(key))
                continue;

            snapshot.stringPrefs.Add(new StringPrefEntry { key = key, value = PlayerPrefs.GetString(key, string.Empty) });
        }
    }

    static void AddIntPrefs(CloudSaveSnapshot snapshot, IEnumerable<string> keys)
    {
        foreach (string key in keys)
        {
            if (!PlayerPrefs.HasKey(key))
                continue;

            snapshot.intPrefs.Add(new IntPrefEntry { key = key, value = PlayerPrefs.GetInt(key, 0) });
        }
    }

    static void AddFloatPrefs(CloudSaveSnapshot snapshot, IEnumerable<string> keys)
    {
        foreach (string key in keys)
        {
            if (!PlayerPrefs.HasKey(key))
                continue;

            snapshot.floatPrefs.Add(new FloatPrefEntry { key = key, value = PlayerPrefs.GetFloat(key, 0f) });
        }
    }

    static void ApplySnapshot(CloudSaveSnapshot snapshot)
    {
        if (snapshot.files != null)
        {
            for (int i = 0; i < snapshot.files.Count; i++)
                ApplyFile(snapshot.files[i]);
        }

        if (snapshot.stringPrefs != null)
        {
            for (int i = 0; i < snapshot.stringPrefs.Count; i++)
            {
                var entry = snapshot.stringPrefs[i];
                if (entry != null && !string.IsNullOrEmpty(entry.key))
                    PlayerPrefs.SetString(entry.key, entry.value ?? string.Empty);
            }
        }

        if (snapshot.intPrefs != null)
        {
            for (int i = 0; i < snapshot.intPrefs.Count; i++)
            {
                var entry = snapshot.intPrefs[i];
                if (entry != null && !string.IsNullOrEmpty(entry.key))
                    PlayerPrefs.SetInt(entry.key, entry.value);
            }
        }

        if (snapshot.floatPrefs != null)
        {
            for (int i = 0; i < snapshot.floatPrefs.Count; i++)
            {
                var entry = snapshot.floatPrefs[i];
                if (entry != null && !string.IsNullOrEmpty(entry.key))
                    PlayerPrefs.SetFloat(entry.key, entry.value);
            }
        }
    }

    static void ApplyFile(CloudFileEntry entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.name))
            return;

        string safeName = Path.GetFileName(entry.name);
        if (safeName != ProgressFileName && safeName != TempRunFileName)
            return;

        string path = Path.Combine(Application.persistentDataPath, safeName);
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        if (!entry.exists)
        {
            if (File.Exists(path))
                File.Delete(path);
            return;
        }

        File.WriteAllText(path, entry.contents ?? string.Empty);
    }

    static long GetLocalFileContentTicks()
    {
        long ticks = Math.Max(LoadTicks(KeyLastUploadedTicks), LoadTicks(KeyLastAppliedTicks));
        ticks = Math.Max(ticks, GetFileWriteTicks(ProgressFileName));
        ticks = Math.Max(ticks, GetFileWriteTicks(TempRunFileName));
        return ticks;
    }

    static long GetFileWriteTicks(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        return File.Exists(path) ? File.GetLastWriteTimeUtc(path).Ticks : 0;
    }

    static bool HasAnyLocalSaveData()
    {
        return File.Exists(Path.Combine(Application.persistentDataPath, ProgressFileName)) ||
            File.Exists(Path.Combine(Application.persistentDataPath, TempRunFileName)) ||
            HasAnyLocalPlayerPrefsData();
    }

    static bool HasAnyLocalPlayerPrefsData()
    {
        if (PlayerPrefs.GetInt("CURRENCY_TOTAL", 0) > 0)
            return true;

        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("SelectedCharacterName", string.Empty)))
            return true;

        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("SelectedMonstersCSV", string.Empty)))
            return true;

        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("HighScores_v1", string.Empty)))
            return true;

        foreach (var type in ShopBuffStore.AllTypes)
        {
            if (PlayerPrefs.GetInt($"ShopBuff_{type}", 0) > 0)
                return true;
        }

        return false;
    }

    static long LoadTicks(string key)
    {
        return long.TryParse(PlayerPrefs.GetString(key, "0"), out long ticks) ? ticks : 0;
    }

    static void SaveTicks(string key, long ticks)
    {
        PlayerPrefs.SetString(key, Math.Max(0L, ticks).ToString());
    }

    static IEnumerable<(string assetName, string displayName)> GetMonsterAssetNames()
    {
        EnsureAssetNameCaches();
        return MonsterAssetNameCache;
    }

    static IEnumerable<string> GetCharacterAssetNames()
    {
        EnsureAssetNameCaches();
        return CharacterAssetNameCache;
    }

    static IEnumerable<string> GetRunModifierAssetNames()
    {
        EnsureAssetNameCaches();
        return RunModifierAssetNameCache;
    }

    static IEnumerable<string> GetLevelModifierAssetNames()
    {
        EnsureAssetNameCaches();
        return LevelModifierAssetNameCache;
    }

    static void EnsureAssetNameCaches()
    {
        if (assetNameCachesReady)
            return;

        MonsterAssetNameCache.Clear();
        CharacterAssetNameCache.Clear();
        RunModifierAssetNameCache.Clear();
        LevelModifierAssetNameCache.Clear();

        var seen = new HashSet<string>();
        foreach (var monster in FindAssets<MonsterData>())
        {
            if (!monster || string.IsNullOrWhiteSpace(monster.name))
                continue;

            if (seen.Add(monster.name))
                MonsterAssetNameCache.Add((monster.name, string.IsNullOrWhiteSpace(monster.monsterName) ? monster.name : monster.monsterName));
        }

        seen.Clear();
        foreach (var character in FindAssets<PlayerCharacterData>())
        {
            if (character && !string.IsNullOrWhiteSpace(character.name) && seen.Add(character.name))
                CharacterAssetNameCache.Add(character.name);
        }

        seen.Clear();
        foreach (var modifier in FindAssets<RunModifierSO>())
        {
            if (modifier && !string.IsNullOrWhiteSpace(modifier.name) && seen.Add(modifier.name))
                RunModifierAssetNameCache.Add(modifier.name);
        }

        seen.Clear();
        foreach (var modifier in FindAssets<LevelModifierSO>())
        {
            if (modifier && !string.IsNullOrWhiteSpace(modifier.name) && seen.Add(modifier.name))
                LevelModifierAssetNameCache.Add(modifier.name);
        }

        assetNameCachesReady = true;
    }

    static IEnumerable<T> FindAssets<T>() where T : UnityEngine.Object
    {
        foreach (var asset in Resources.FindObjectsOfTypeAll<T>())
        {
            if (asset)
                yield return asset;
        }

        foreach (var asset in Resources.LoadAll<T>(string.Empty))
        {
            if (asset)
                yield return asset;
        }
    }

    [Serializable]
    class CloudSaveSnapshot
    {
        public int version = SnapshotVersion;
        public long updatedUtcTicks;
        public string unityVersion;
        public string platform;
        public List<CloudFileEntry> files = new();
        public List<StringPrefEntry> stringPrefs = new();
        public List<IntPrefEntry> intPrefs = new();
        public List<FloatPrefEntry> floatPrefs = new();
    }

    [Serializable]
    class CloudFileEntry
    {
        public string name;
        public bool exists;
        public string contents;
    }

    [Serializable]
    class StringPrefEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    class IntPrefEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    class FloatPrefEntry
    {
        public string key;
        public float value;
    }
}
