using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerProgress : MonoBehaviour
{
    public static PlayerProgress I { get; private set; }

    [Serializable]
    class SaveData
    {
        public Dictionary<string, long> lifetimeInt = new();
        public Dictionary<string, double> lifetimeFloat = new();

        public Dictionary<string, long> runInt = new();
        public Dictionary<string, double> runFloat = new();
        public Dictionary<string, long> bestRunInt = new();
        public Dictionary<string, double> bestRunFloatMin = new();

        public HashSet<string> unlockedAchievements = new();
        public HashSet<string> pendingExternalUnlocks = new();
        public HashSet<string> deferredDemoAchievementUnlocks = new();
        public HashSet<string> completedTutorials = new();

        public int selectedStarDifficulty = 0;
        public int maxUnlockedStarDifficulty = 0;
    }

    [Serializable]
    public class StringLongEntry
    {
        public string key;
        public long value;
    }

    [Serializable]
    public class StringDoubleEntry
    {
        public string key;
        public double value;
    }

    [Serializable]
    public class RunStateSnapshot
    {
        public List<StringLongEntry> runInt = new();
        public List<StringDoubleEntry> runFloat = new();
    }

    SaveData _data = new SaveData();

    public event Action<string> AchievementUnlocked;
    public event Action<string, double> StatChanged;

    string SavePath => Path.Combine(Application.persistentDataPath, "tetrabeasts_progress.json");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        // Only create during actual play mode
        if (!Application.isPlaying) return;

        Ensure();
    }

    public static PlayerProgress Ensure()
    {
        if (I != null)
            return I;

        if (!Application.isPlaying)
            return null;

        var go = new GameObject("PlayerProgress");
        return go.AddComponent<PlayerProgress>();
    }

    void Awake()
    {
        if (I != null && I != this)
        {
#if UNITY_EDITOR
            DestroyImmediate(gameObject);
#else
            Destroy(gameObject);
#endif
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    void Start()
    {
        SteamCloudSaveService.Ensure();

        if (!DemoBuildGuardRails.IsDemoBuild)
            SteamAchievementService.Ensure();
    }

    // ---------------- Run lifecycle ----------------

    public void BeginRun()
    {
        TutorialTestingScope.Reset();
        _data.runInt.Clear();
        _data.runFloat.Clear();
        Save();
    }

    public void EndRun()
    {
        Save();
    }

    public RunStateSnapshot CaptureRunState()
    {
        var snapshot = new RunStateSnapshot();

        foreach (var kv in _data.runInt)
            snapshot.runInt.Add(new StringLongEntry { key = kv.Key, value = kv.Value });

        foreach (var kv in _data.runFloat)
            snapshot.runFloat.Add(new StringDoubleEntry { key = kv.Key, value = kv.Value });

        return snapshot;
    }

    public void RestoreRunState(RunStateSnapshot snapshot, bool saveAfterRestore = true)
    {
        _data.runInt.Clear();
        _data.runFloat.Clear();

        if (snapshot != null)
        {
            if (snapshot.runInt != null)
            {
                for (int i = 0; i < snapshot.runInt.Count; i++)
                {
                    var entry = snapshot.runInt[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                        continue;

                    _data.runInt[entry.key] = entry.value;
                    SetBestRunIntNoNotify(entry.key, entry.value);
                }
            }

            if (snapshot.runFloat != null)
            {
                for (int i = 0; i < snapshot.runFloat.Count; i++)
                {
                    var entry = snapshot.runFloat[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                        continue;

                    _data.runFloat[entry.key] = entry.value;
                    SetBestRunFloatMinNoNotify(entry.key, entry.value);
                }
            }
        }

        if (saveAfterRestore)
            Save();
    }

    // ---------------- Stats API ----------------

    public long GetLifetimeInt(string key) => _data.lifetimeInt.TryGetValue(key, out var v) ? v : 0;
    public double GetLifetimeFloat(string key) => _data.lifetimeFloat.TryGetValue(key, out var v) ? v : 0;

    public long GetRunInt(string key) => _data.runInt.TryGetValue(key, out var v) ? v : 0;
    public double GetRunFloat(string key) => _data.runFloat.TryGetValue(key, out var v) ? v : 0;
    public long GetBestRunInt(string key) => _data.bestRunInt.TryGetValue(key, out var v) ? v : 0;
    public double GetBestRunFloatMin(string key) => _data.bestRunFloatMin.TryGetValue(key, out var v) ? v : 0;

    public void AddLifetimeInt(string key, long delta)
    {
        if (delta == 0) return;
        _data.lifetimeInt[key] = GetLifetimeInt(key) + delta;
        StatChanged?.Invoke(key, _data.lifetimeInt[key]);
        Save();
        AchievementSystem.OnStatChanged(key);
    }

    public void AddLifetimeFloat(string key, double delta)
    {
        if (Math.Abs(delta) < 0.0000001) return;
        _data.lifetimeFloat[key] = GetLifetimeFloat(key) + delta;
        StatChanged?.Invoke(key, _data.lifetimeFloat[key]);
        Save();
        AchievementSystem.OnStatChanged(key);
    }

    public void AddRunInt(string key, long delta)
    {
        if (delta == 0) return;
        long next = GetRunInt(key) + delta;
        _data.runInt[key] = next;
        SetBestRunIntNoNotify(key, next);
        StatChanged?.Invoke(key, next);
        Save();
        AchievementSystem.OnStatChanged(key);
    }

    public void AddRunFloat(string key, double delta)
    {
        if (Math.Abs(delta) < 0.0000001) return;
        _data.runFloat[key] = GetRunFloat(key) + delta;
        StatChanged?.Invoke(key, _data.runFloat[key]);
        Save();
        AchievementSystem.OnStatChanged(key);
    }

    public void SetRunBestInt(string key, long value)
    {
        long cur = GetRunInt(key);
        if (value <= cur)
        {
            if (SetBestRunIntNoNotify(key, cur))
            {
                StatChanged?.Invoke(key, cur);
                Save();
            }

            return;
        }

        _data.runInt[key] = value;
        SetBestRunIntNoNotify(key, value);
        StatChanged?.Invoke(key, value);
        Save();
        AchievementSystem.OnStatChanged(key);
    }

    public void SetLifetimeBestInt(string key, long value)
    {
        long cur = GetLifetimeInt(key);
        if (value <= cur) return;

        _data.lifetimeInt[key] = value;
        StatChanged?.Invoke(key, value);
        Save();
        AchievementSystem.OnStatChanged(key);
    }

    public void SetRunBestFloatMin(string key, double value)
    {
        double cur = GetRunFloat(key);
        if (cur <= 0 || value < cur)
        {
            _data.runFloat[key] = value;
            SetBestRunFloatMinNoNotify(key, value);
            StatChanged?.Invoke(key, value);
            Save();
            AchievementSystem.OnStatChanged(key);
            return;
        }

        if (SetBestRunFloatMinNoNotify(key, cur))
        {
            StatChanged?.Invoke(key, cur);
            Save();
        }
    }

    bool SetBestRunIntNoNotify(string key, long value)
    {
        long cur = GetBestRunInt(key);
        if (value > cur)
        {
            _data.bestRunInt[key] = value;
            return true;
        }

        return false;
    }

    bool SetBestRunFloatMinNoNotify(string key, double value)
    {
        if (value <= 0)
            return false;

        double cur = GetBestRunFloatMin(key);
        if (cur <= 0 || value < cur)
        {
            _data.bestRunFloatMin[key] = value;
            return true;
        }

        return false;
    }

    // ---------------- Achievements ----------------

    public bool IsUnlocked(string achievementId) => _data.unlockedAchievements.Contains(achievementId);

    public void UnlockAchievement(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId)) return;
        if (_data.unlockedAchievements.Contains(achievementId)) return;

        _data.unlockedAchievements.Add(achievementId);
        _data.deferredDemoAchievementUnlocks.Remove(achievementId);

        // If Steam backend present, attempt unlock now, otherwise queue
        _data.pendingExternalUnlocks.Add(achievementId);

        Save();
        AchievementUnlocked?.Invoke(achievementId);
    }

    public void DeferDemoAchievementUnlock(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId)) return;
        if (_data.unlockedAchievements.Contains(achievementId)) return;
        if (_data.deferredDemoAchievementUnlocks.Add(achievementId))
            Save();
    }

    public int UnlockDeferredDemoAchievements()
    {
        if (_data.deferredDemoAchievementUnlocks.Count == 0)
            return 0;

        var deferred = new List<string>(_data.deferredDemoAchievementUnlocks);
        int unlocked = 0;

        for (int i = 0; i < deferred.Count; i++)
        {
            string achievementId = deferred[i];
            if (string.IsNullOrEmpty(achievementId))
                continue;

            if (!_data.unlockedAchievements.Contains(achievementId))
            {
                UnlockAchievement(achievementId);
                unlocked++;
            }

            _data.deferredDemoAchievementUnlocks.Remove(achievementId);
        }

        Save();
        return unlocked;
    }

    public List<string> GetUnlockedAchievementIds()
    {
        return new List<string>(_data.unlockedAchievements);
    }

    public List<string> GetPendingExternalUnlockIds()
    {
        return new List<string>(_data.pendingExternalUnlocks);
    }

    public bool IsTutorialCompleted(string tutorialId)
    {
        return !string.IsNullOrWhiteSpace(tutorialId) && _data.completedTutorials.Contains(tutorialId);
    }

    public void SetTutorialCompleted(string tutorialId)
    {
        if (string.IsNullOrWhiteSpace(tutorialId)) return;
        if (_data.completedTutorials.Add(tutorialId))
        {
            Save();
            SteamCloudSaveService.TryUploadNow();
        }
    }

    public void ClearTutorialCompleted(string tutorialId)
    {
        if (string.IsNullOrWhiteSpace(tutorialId)) return;
        if (_data.completedTutorials.Remove(tutorialId))
            Save();
    }

    public List<string> GetCompletedTutorialIds()
    {
        return new List<string>(_data.completedTutorials);
    }

    public int GetSelectedStarDifficulty()
    {
        return _data.selectedStarDifficulty;
    }

    public int GetMaxUnlockedStarDifficulty()
    {
        return _data.maxUnlockedStarDifficulty;
    }

    public void SetSelectedStarDifficulty(int stars)
    {
        stars = Mathf.Clamp(stars, 0, _data.maxUnlockedStarDifficulty);
        if (_data.selectedStarDifficulty == stars)
            return;

        _data.selectedStarDifficulty = stars;
        Save();
    }

    public bool TryUnlockStarDifficulty(int stars)
    {
        stars = StarDifficultySystem.ClampStars(stars);
        if (stars <= _data.maxUnlockedStarDifficulty)
            return false;

        _data.maxUnlockedStarDifficulty = stars;
        _data.selectedStarDifficulty = Mathf.Clamp(_data.selectedStarDifficulty, 0, _data.maxUnlockedStarDifficulty);
        Save();
        return true;
    }

    public void ClearPendingExternalUnlock(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId)) return;
        if (_data.pendingExternalUnlocks.Remove(achievementId))
            Save();
    }

    // ---------------- Save/Load ----------------

    void Load()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                _data = new SaveData();
                return;
            }

            string json = File.ReadAllText(SavePath);
            _data = JsonUtility.FromJson<SaveDataWrapper>(json)?.ToData() ?? new SaveData();
            SanitizeStarDifficultyState();
        }
        catch
        {
            _data = new SaveData();
        }
    }

    void Save()
    {
        try
        {
            SanitizeStarDifficultyState();
            var wrapper = SaveDataWrapper.FromData(_data);
            string json = JsonUtility.ToJson(wrapper, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            SteamCloudSaveService.QueueUpload();
        }
        catch { /* ignore */ }
    }

    // JsonUtility doesn't support dictionaries or hashsets, so convert to lists of keys/values for serialization
    [Serializable]
    class SaveDataWrapper
    {
        public List<string> lifetimeIntKeys = new();
        public List<long> lifetimeIntVals = new();

        public List<string> lifetimeFloatKeys = new();
        public List<double> lifetimeFloatVals = new();

        public List<string> runIntKeys = new();
        public List<long> runIntVals = new();

        public List<string> runFloatKeys = new();
        public List<double> runFloatVals = new();
        public List<StringLongEntry> bestRunInt = new();
        public List<StringDoubleEntry> bestRunFloatMin = new();

        public List<string> unlocked = new();
        public List<string> pending = new();
        public List<string> deferredDemo = new();
        public List<string> completedTutorials = new();

        public int selectedStarDifficulty = 0;
        public int maxUnlockedStarDifficulty = 0;

        public static SaveDataWrapper FromData(SaveData d)
        {
            var w = new SaveDataWrapper();

            foreach (var kv in d.lifetimeInt) { w.lifetimeIntKeys.Add(kv.Key); w.lifetimeIntVals.Add(kv.Value); }
            foreach (var kv in d.lifetimeFloat) { w.lifetimeFloatKeys.Add(kv.Key); w.lifetimeFloatVals.Add(kv.Value); }
            foreach (var kv in d.runInt) { w.runIntKeys.Add(kv.Key); w.runIntVals.Add(kv.Value); }
            foreach (var kv in d.runFloat) { w.runFloatKeys.Add(kv.Key); w.runFloatVals.Add(kv.Value); }
            foreach (var kv in d.bestRunInt) { w.bestRunInt.Add(new StringLongEntry { key = kv.Key, value = kv.Value }); }
            foreach (var kv in d.bestRunFloatMin) { w.bestRunFloatMin.Add(new StringDoubleEntry { key = kv.Key, value = kv.Value }); }

            w.unlocked.AddRange(d.unlockedAchievements);
            w.pending.AddRange(d.pendingExternalUnlocks);
            w.deferredDemo.AddRange(d.deferredDemoAchievementUnlocks);
            w.completedTutorials.AddRange(d.completedTutorials);
            w.selectedStarDifficulty = d.selectedStarDifficulty;
            w.maxUnlockedStarDifficulty = d.maxUnlockedStarDifficulty;

            return w;
        }

        public SaveData ToData()
        {
            var d = new SaveData();

            lifetimeIntKeys ??= new List<string>();
            lifetimeIntVals ??= new List<long>();
            lifetimeFloatKeys ??= new List<string>();
            lifetimeFloatVals ??= new List<double>();
            runIntKeys ??= new List<string>();
            runIntVals ??= new List<long>();
            runFloatKeys ??= new List<string>();
            runFloatVals ??= new List<double>();
            bestRunInt ??= new List<StringLongEntry>();
            bestRunFloatMin ??= new List<StringDoubleEntry>();
            unlocked ??= new List<string>();
            pending ??= new List<string>();
            deferredDemo ??= new List<string>();
            completedTutorials ??= new List<string>();

            for (int i = 0; i < lifetimeIntKeys.Count && i < lifetimeIntVals.Count; i++)
                d.lifetimeInt[lifetimeIntKeys[i]] = lifetimeIntVals[i];

            for (int i = 0; i < lifetimeFloatKeys.Count && i < lifetimeFloatVals.Count; i++)
                d.lifetimeFloat[lifetimeFloatKeys[i]] = lifetimeFloatVals[i];

            for (int i = 0; i < runIntKeys.Count && i < runIntVals.Count; i++)
                d.runInt[runIntKeys[i]] = runIntVals[i];

            for (int i = 0; i < runFloatKeys.Count && i < runFloatVals.Count; i++)
                d.runFloat[runFloatKeys[i]] = runFloatVals[i];

            for (int i = 0; i < bestRunInt.Count; i++)
            {
                var entry = bestRunInt[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.key))
                    d.bestRunInt[entry.key] = entry.value;
            }

            for (int i = 0; i < bestRunFloatMin.Count; i++)
            {
                var entry = bestRunFloatMin[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.key))
                    d.bestRunFloatMin[entry.key] = entry.value;
            }

            d.unlockedAchievements = new HashSet<string>(unlocked);
            d.pendingExternalUnlocks = new HashSet<string>(pending);
            d.deferredDemoAchievementUnlocks = new HashSet<string>(deferredDemo);
            d.completedTutorials = new HashSet<string>(completedTutorials);
            d.selectedStarDifficulty = selectedStarDifficulty;
            d.maxUnlockedStarDifficulty = maxUnlockedStarDifficulty;

            return d;
        }
    }

    void SanitizeStarDifficultyState()
    {
        _data.maxUnlockedStarDifficulty = StarDifficultySystem.ClampStars(_data.maxUnlockedStarDifficulty);
        _data.selectedStarDifficulty = Mathf.Clamp(_data.selectedStarDifficulty, 0, _data.maxUnlockedStarDifficulty);
    }

    public void DEV_ClearAllProgress()
    {
        _data = new SaveData();

        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch { }

        Save();
    }

    public void ReloadFromDiskAfterCloudImport()
    {
        Load();
    }
}
