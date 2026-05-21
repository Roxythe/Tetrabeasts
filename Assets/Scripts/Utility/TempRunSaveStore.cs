using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class TempRunSaveStore
{
    const string FileName = "tetrabeasts_temp_run.json";
    static bool _resumePending;

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int currentLevel;
        public bool postFinalSurvivalActive;
        public bool standardFinalWinApplied;
        public int score;
        public int unitLives;
        public float specialGauge;
        public int starDifficulty;
        public string selectedCharacterName;
        public List<string> selectedMonsterNames = new();
        public int currencyTotal;
        public List<ShopBuffLevelEntry> shopBuffLevels = new();
        public RunModsSnapshot runMods = new();
        public List<string> buffModifierNames = new();
        public List<string> debuffModifierNames = new();
        public List<RunMonsterStateEntry> runMonsterStates = new();
        public PlayerProgress.RunStateSnapshot playerProgressRun = new();
        public RunSummaryStats.SerializableSnapshot runSummary = new();
        public LevelModifierCheckpointData levelModifier = new();
    }

    [Serializable]
    public class ShopBuffLevelEntry
    {
        public int type;
        public int level;
    }

    [Serializable]
    public class RunModsSnapshot
    {
        public float enemyAttackIntervalMult = 1f;
        public float enemyProjectileDamageMult = 1f;
        public float enemyProjectileSpeedMult = 1f;
        public float specialGainMult = 1f;
        public float specialDrainMult = 1f;
        public float specialBlockChanceAdd = 0f;
        public float pieceGravityMult = 1f;
        public float fallRampRateMult = 1f;
        public float monsterDamageMult = 1f;
        public float monsterSpecialGainMult = 1f;
        public float monsterMaxHpMult = 1f;
        public float healPowerMult = 1f;
        public int healRangeAdd = 0;
        public bool disableNextPreview = false;
        public bool disableLandingHint = false;
        public float lineClearCurrencyChanceAdd = 0f;
        public float lineClearCurrencyAmountMult = 1f;
        public float enemyCastleHpMult = 1f;
        public float luck = 0f;
        public float misfortune = 0f;
        public float stoneBuffDropChanceAdd = 0f;
        public bool stoneObstacleDropsDebuffsOnly = false;
        public int reserveUnitsRestoredOnWinAdd = 0;
        public int maxReserveUnitsAdd = 0;
        public bool disableRoundWinReserveRestore = false;
    }

    [Serializable]
    public class RunMonsterStateEntry
    {
        public string monsterName;
        public int level;
        public float xpInto;
    }

    [Serializable]
    public class LevelModifierCheckpointData
    {
        public string modifierName;
        public string modifierDisplayName;
        public int availableRerolls;
    }

    static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool Exists()
    {
        return File.Exists(SavePath);
    }

    public static bool HasValidSave()
    {
        if (!Exists())
            return false;

        if (TryLoad(out _))
            return true;

        Delete();
        return false;
    }

    public static bool TryLoad(out SaveData data)
    {
        data = null;

        try
        {
            if (!File.Exists(SavePath))
                return false;

            string json = File.ReadAllText(SavePath);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            data = JsonUtility.FromJson<SaveData>(json);
            return data != null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"TempRunSaveStore: Failed to load temp run save. {ex.Message}");
            data = null;
            return false;
        }
    }

    public static void Save(SaveData data)
    {
        if (data == null)
        {
            Delete();
            return;
        }

        try
        {
            string dir = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            SteamCloudSaveService.QueueUpload();
        }
        catch (Exception ex)
        {
            Debug.LogError($"TempRunSaveStore: Failed to save temp run data. {ex.Message}");
        }
    }

    public static void Delete()
    {
        _resumePending = false;

        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);

            SteamCloudSaveService.QueueUpload();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"TempRunSaveStore: Failed to delete temp run save. {ex.Message}");
        }
    }

    public static void PrepareResume()
    {
        _resumePending = true;
    }

    public static void CancelPendingResume()
    {
        _resumePending = false;
    }

    public static bool TryConsumePendingResume(out SaveData data)
    {
        bool pending = _resumePending;
        _resumePending = false;

        data = null;
        if (!pending)
            return false;

        return TryLoad(out data);
    }
}
