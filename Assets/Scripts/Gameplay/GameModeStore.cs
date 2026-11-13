using UnityEngine;

public enum RunGameMode { Standard, Chaos, Marathon }

public static class GameModeStore
{
    public static RunGameMode Current { get; set; }
    const string UnlockStat = "lt_standard_final_level_reached";
    const string CommanderCompletionPrefix = "lt_standard_final_level_char_";

    public static bool HasStandardCompletion(PlayerCharacterData commander)
    {
        var progress = PlayerProgress.I;
        if (!commander || !progress) return false;
        // Preserve commander-specific victories recorded by older versions.
        return progress.GetLifetimeInt(CommanderCompletionPrefix + commander.name) > 0 ||
               progress.GetLifetimeInt("lt_final_win_char_" + commander.name) > 0;
    }
    public static bool ExtraModesUnlocked(int standardLevelCount)
    {
        var progress = PlayerProgress.I;
        if (!progress) return false;
        if (progress.GetLifetimeInt(UnlockStat) > 0) return true;
        // Older versions recorded best completed levels and final-win achievements.
        bool legacy = progress.GetBestRunInt(AchievementSystem.Stat.RunLevelsWon) >= Mathf.Max(1, standardLevelCount - 1) ||
            progress.GetRunInt(AchievementSystem.Stat.RunLevelsWon) >= Mathf.Max(1, standardLevelCount - 1) ||
            progress.IsUnlocked(AchievementSystem.Ach.BeatFinal) || progress.GetMaxUnlockedStarDifficulty() > 0;
        if (!legacy && TempRunSaveStore.TryLoad(out var save))
            legacy = save.gameMode == RunGameMode.Standard && save.currentLevel >= standardLevelCount - 1;
        if (legacy) MarkFinalLevelReached();
        return legacy;
    }
    public static void MarkFinalLevelReached(PlayerCharacterData commander = null)
    {
        if (PlayerProgress.I && PlayerProgress.I.GetLifetimeInt(UnlockStat) == 0)
            PlayerProgress.I.AddLifetimeInt(UnlockStat, 1);
        if (Current == RunGameMode.Standard && commander && PlayerProgress.I)
        {
            string key = CommanderCompletionPrefix + commander.name;
            if (PlayerProgress.I.GetLifetimeInt(key) == 0)
                PlayerProgress.I.AddLifetimeInt(key, 1);
        }
    }
}
