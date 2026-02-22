using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class PlayerProgress : MonoBehaviour
{
    public static PlayerProgress I { get; private set; }

    [Serializable]
    class SaveData
    {
        public Dictionary<string, long> lifetimeInt = new();
        public Dictionary<string, double> lifetimeFloat = new();

        public Dictionary<string, long> runInt = new();
        public Dictionary<string, double> runFloat = new();

        public HashSet<string> unlockedAchievements = new();
        public HashSet<string> pendingExternalUnlocks = new();
    }

    SaveData _data = new SaveData();

    public event Action<string> AchievementUnlocked;
    public event Action<string, double> StatChanged;

    string SavePath => Path.Combine(Application.persistentDataPath, "tetrabeasts_progress.json");

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        Load();
        AchievementSystem.EnsureInitialized();
    }

    // ---------------- Run lifecycle ----------------

    public void BeginRun()
    {
        _data.runInt.Clear();
        _data.runFloat.Clear();
        Save();
    }

    public void EndRun()
    {
        Save();
    }

    // ---------------- Stats API ----------------

    public long GetLifetimeInt(string key) => _data.lifetimeInt.TryGetValue(key, out var v) ? v : 0;
    public double GetLifetimeFloat(string key) => _data.lifetimeFloat.TryGetValue(key, out var v) ? v : 0;

    public long GetRunInt(string key) => _data.runInt.TryGetValue(key, out var v) ? v : 0;
    public double GetRunFloat(string key) => _data.runFloat.TryGetValue(key, out var v) ? v : 0;

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
        _data.runInt[key] = GetRunInt(key) + delta;
        StatChanged?.Invoke(key, _data.runInt[key]);
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
        if (value <= cur) return;
        _data.runInt[key] = value;
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
            StatChanged?.Invoke(key, value);
            Save();
            AchievementSystem.OnStatChanged(key);
        }
    }

    // ---------------- Achievements ----------------

    public bool IsUnlocked(string achievementId) => _data.unlockedAchievements.Contains(achievementId);

    public void UnlockAchievement(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId)) return;
        if (_data.unlockedAchievements.Contains(achievementId)) return;

        _data.unlockedAchievements.Add(achievementId);

        // If Steam backend present, attempt unlock now, otherwise queue
        _data.pendingExternalUnlocks.Add(achievementId);

        Save();
        AchievementUnlocked?.Invoke(achievementId);
    }

    public List<string> GetUnlockedAchievementIds()
    {
        return new List<string>(_data.unlockedAchievements);
    }

    public List<string> GetPendingExternalUnlockIds()
    {
        return new List<string>(_data.pendingExternalUnlocks);
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
            var wrapper = SaveDataWrapper.FromData(_data);
            string json = JsonUtility.ToJson(wrapper, prettyPrint: true);
            File.WriteAllText(SavePath, json);
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

        public List<string> unlocked = new();
        public List<string> pending = new();

        public static SaveDataWrapper FromData(SaveData d)
        {
            var w = new SaveDataWrapper();

            foreach (var kv in d.lifetimeInt) { w.lifetimeIntKeys.Add(kv.Key); w.lifetimeIntVals.Add(kv.Value); }
            foreach (var kv in d.lifetimeFloat) { w.lifetimeFloatKeys.Add(kv.Key); w.lifetimeFloatVals.Add(kv.Value); }
            foreach (var kv in d.runInt) { w.runIntKeys.Add(kv.Key); w.runIntVals.Add(kv.Value); }
            foreach (var kv in d.runFloat) { w.runFloatKeys.Add(kv.Key); w.runFloatVals.Add(kv.Value); }

            w.unlocked.AddRange(d.unlockedAchievements);
            w.pending.AddRange(d.pendingExternalUnlocks);

            return w;
        }

        public SaveData ToData()
        {
            var d = new SaveData();

            for (int i = 0; i < lifetimeIntKeys.Count && i < lifetimeIntVals.Count; i++)
                d.lifetimeInt[lifetimeIntKeys[i]] = lifetimeIntVals[i];

            for (int i = 0; i < lifetimeFloatKeys.Count && i < lifetimeFloatVals.Count; i++)
                d.lifetimeFloat[lifetimeFloatKeys[i]] = lifetimeFloatVals[i];

            for (int i = 0; i < runIntKeys.Count && i < runIntVals.Count; i++)
                d.runInt[runIntKeys[i]] = runIntVals[i];

            for (int i = 0; i < runFloatKeys.Count && i < runFloatVals.Count; i++)
                d.runFloat[runFloatKeys[i]] = runFloatVals[i];

            d.unlockedAchievements = new HashSet<string>(unlocked);
            d.pendingExternalUnlocks = new HashSet<string>(pending);

            return d;
        }
    }
}