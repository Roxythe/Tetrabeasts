using System;
using System.Collections.Generic;
using UnityEngine;

public static class AchievementSystem
{
    enum Scope { LifetimeInt, LifetimeFloat, RunInt, RunFloat }
    enum Compare { Gte, Lte } // Min time achievements etc

    class Def
    {
        public string id;
        public Scope scope;
        public string statKey;
        public double target;
        public Compare compare;
    }

    static bool _init;
    static readonly List<Def> _defs = new();

    // ---------- Stat keys ----------
    public static class Stat
    {
        // Lifetime
        public const string TotalRowClears = "lt_total_row_clears";
        public const string GoldEarned = "lt_gold_earned";
        public const string SpecialsUsedTotal = "lt_specials_used_total";
        public const string LevelsWon = "lt_levels_won";
        public const string Losses = "lt_losses";

        public const string PoisonDamage = "lt_poison_damage";
        public const string FireDamage = "lt_fire_damage";
        public const string SpikeDamage = "lt_spike_damage";

        public const string DeathblockUnitsRemoved = "lt_deathblock_units_removed";
        public const string EarthquakeRowClears = "lt_earthquake_row_clears";

        public const string StoneDestroyed = "lt_stone_destroyed";
        public const string MagicExplosiveKills = "lt_magic_explosive_kills";

        public const string CharactersUnlocked = "lt_characters_unlocked";
        public const string MonstersUnlocked = "lt_monsters_unlocked";
        public const string SkinsUnlocked = "lt_skins_unlocked";

        // First time flags
        public const string FirstBuffChosen = "lt_first_buff_chosen";
        public const string FirstDebuffChosen = "lt_first_debuff_chosen";

        // Run
        public const string RunLevelsWon = "run_levels_won";
        public const string RunMaxCombo = "run_max_combo";
        public const string RunMaxSingleAttackDmg = "run_max_single_attack_dmg";
        public const string RunBestLevelWinSeconds = "run_best_level_win_seconds";
        public const string RunBestLevelSeconds = "run_best_level_seconds";
        public const string RunGravityCapSeconds = "run_gravity_cap_seconds";

        public const string RunUnlockedAnyMonster = "run_unlocked_any_monster";
        public const string RunUnlockedAnySkin = "run_unlocked_any_skin";
        public const string RunUnlockedAnyCharacter = "run_unlocked_any_character";
        public const string RunPurchasedAnyShopUpgrade = "run_purchased_any_shop_upgrade";
        public const string RunBeatFinalLevel = "run_beat_final_level";

        // Challenge
        public const string PerfectLevelWins = "lt_perfect_level_wins";

        // Run counts / flags
        public const string RunBuffModsChosen = "run_buff_mods_chosen";
        public const string RunLevelTime180 = "run_level_time_180";
        public const string RunLevelTime240 = "run_level_time_240";
        public const string RunLevelTime300 = "run_level_time_300";
        public const string RunGravityCap30 = "run_gravity_cap_30";
        public const string RunGravityCap60 = "run_gravity_cap_60";

        // Lifetime flags for composite achievements
        public const string AnyShopUpgradeReached5 = "lt_any_shop_upgrade_reached_5";
        public const string SpecialsEachChar_20 = "lt_specials_each_char_20";
        public const string SpecialsEachChar_100 = "lt_specials_each_char_100";
        public const string FinalWinAllChars = "lt_final_win_all_chars";
    }

    // ---------- Achievement IDs  ----------
    public static class Ach
    {
        // -------- Lifetime (Accumulate) --------
        public const string Rows_100 = "ACH_ROWS_100";
        public const string Rows_1000 = "ACH_ROWS_1000";
        public const string Rows_10000 = "ACH_ROWS_10000";

        public const string AllCharsUnlocked = "ACH_ALL_CHARS_UNLOCKED";
        public const string AllMonstersUnlocked = "ACH_ALL_MONSTERS_UNLOCKED";

        public const string Skins_5 = "ACH_SKINS_5";
        public const string Skins_10 = "ACH_SKINS_10";

        public const string Gold_100 = "ACH_GOLD_100";
        public const string Gold_1000 = "ACH_GOLD_1000";
        public const string Gold_10000 = "ACH_GOLD_10000";

        public const string Specials_1 = "ACH_SPECIALS_1";
        public const string Specials_100 = "ACH_SPECIALS_100";
        public const string Specials_1000 = "ACH_SPECIALS_1000";

        public const string SpecialsEachChar_20 = "ACH_SPECIALS_EACH_CHAR_20";
        public const string SpecialsEachChar_100 = "ACH_SPECIALS_EACH_CHAR_100";

        public const string Wins_1 = "ACH_WINS_1";
        public const string Wins_50 = "ACH_WINS_50";
        public const string Wins_100 = "ACH_WINS_100";

        public const string PoisonDmg_1000 = "ACH_ENVPOISON_DMG_1000";
        public const string FireDmg_1000 = "ACH_ENVFIRE_DMG_1000";
        public const string SpikeDmg_1000 = "ACH_ENVSPIKE_DMG_1000";

        public const string Stone_100 = "ACH_STONE_100";
        public const string MagicExplosiveKills_100 = "ACH_MAGICEXP_KILLS_100";

        public const string AnyShopUpgradeLv5 = "ACH_ANY_SHOP_UPGRADE_LV5";

        public const string Loss_1 = "ACH_LOSS_1";
        public const string Loss_50 = "ACH_LOSS_50";
        public const string Loss_100 = "ACH_LOSS_100";

        public const string DeathBlockRemove_1000 = "ACH_DEATHBLOCK_REMOVE_1000";
        public const string EarthquakeRows_25 = "ACH_EQ_ROWS_25";
        public const string EarthquakeRows_250 = "ACH_EQ_ROWS_250";
        public const string EarthquakeRows_1000 = "ACH_EQ_ROWS_1000";

        public const string FinalWinAllChars = "ACH_FINAL_WIN_ALL_CHARS";

        // -------- Single Run / Instance --------
        public const string SingleAttack_100 = "ACH_RUN_SINGLE_ATK_100";
        public const string Combo_10 = "ACH_RUN_COMBO_10";

        public const string FirstBuff = "ACH_FIRST_BUFF";
        public const string Buffs_15 = "ACH_RUN_BUFFS_15";

        public const string FirstDebuff = "ACH_FIRST_DEBUFF";

        public const string RunWins_10 = "ACH_RUN_WINS_10";

        public const string UnlockAnySkin = "ACH_RUN_UNLOCK_ANY_SKIN";

        public const string UnlockAnyMonster = "ACH_RUN_UNLOCK_ANY_MONSTER";
        public const string UnlockAnyChar = "ACH_RUN_UNLOCK_ANY_CHAR";

        public const string BuyAnyShopUpgrade = "ACH_RUN_SHOP_BUY_ANY";

        public const string LevelTime_180 = "ACH_LEVEL_TIME_180";
        public const string LevelTime_240 = "ACH_LEVEL_TIME_240";
        public const string LevelTime_300 = "ACH_LEVEL_TIME_300";

        public const string BeatFinal = "ACH_BEAT_FINAL";

        // -------- Challenge / Conditional --------
        public const string PerfectWins_1 = "ACH_PERFECT_WINS_1";
        public const string PerfectWins_25 = "ACH_PERFECT_WINS_25";
        public const string PerfectWins_100 = "ACH_PERFECT_WINS_100";

        public const string WinUnder_60 = "ACH_WIN_UNDER_60";
        public const string WinUnder_45 = "ACH_WIN_UNDER_45";
        public const string WinUnder_30 = "ACH_WIN_UNDER_30";

        public const string GravityCap_30s = "ACH_GRAVITY_CAP_30S";
        public const string GravityCap_60s = "ACH_GRAVITY_CAP_60S";
    }

    public static void EnsureInitialized()
    {
        if (_init) return;
        _init = true;

        // -------- Lifetime thresholds --------
        Add(Ach.Rows_100, Scope.LifetimeInt, Stat.TotalRowClears, 100);
        Add(Ach.Rows_1000, Scope.LifetimeInt, Stat.TotalRowClears, 1000);
        Add(Ach.Rows_10000, Scope.LifetimeInt, Stat.TotalRowClears, 10000);

        Add(Ach.AllCharsUnlocked, Scope.LifetimeInt, Stat.CharactersUnlocked, 4);

        // Monster total varies by roster count
        Add(Ach.AllMonstersUnlocked, Scope.LifetimeInt, Stat.MonstersUnlocked, 4); // Number of unlockable monsters

        Add(Ach.Skins_5, Scope.LifetimeInt, Stat.SkinsUnlocked, 5);
        Add(Ach.Skins_10, Scope.LifetimeInt, Stat.SkinsUnlocked, 10);

        Add(Ach.Gold_100, Scope.LifetimeInt, Stat.GoldEarned, 100);
        Add(Ach.Gold_1000, Scope.LifetimeInt, Stat.GoldEarned, 1000);
        Add(Ach.Gold_10000, Scope.LifetimeInt, Stat.GoldEarned, 10000);

        Add(Ach.Specials_1, Scope.LifetimeInt, Stat.SpecialsUsedTotal, 1);
        Add(Ach.Specials_100, Scope.LifetimeInt, Stat.SpecialsUsedTotal, 100);
        Add(Ach.Specials_1000, Scope.LifetimeInt, Stat.SpecialsUsedTotal, 1000);

        Add(Ach.SpecialsEachChar_20, Scope.LifetimeInt, Stat.SpecialsEachChar_20, 1);
        Add(Ach.SpecialsEachChar_100, Scope.LifetimeInt, Stat.SpecialsEachChar_100, 1);

        Add(Ach.Wins_1, Scope.LifetimeInt, Stat.LevelsWon, 1);
        Add(Ach.Wins_50, Scope.LifetimeInt, Stat.LevelsWon, 50);
        Add(Ach.Wins_100, Scope.LifetimeInt, Stat.LevelsWon, 100);

        Add(Ach.PoisonDmg_1000, Scope.LifetimeFloat, Stat.PoisonDamage, 1000.0);
        Add(Ach.FireDmg_1000, Scope.LifetimeFloat, Stat.FireDamage, 1000.0);
        Add(Ach.SpikeDmg_1000, Scope.LifetimeFloat, Stat.SpikeDamage, 1000.0);

        Add(Ach.Stone_100, Scope.LifetimeInt, Stat.StoneDestroyed, 100);
        Add(Ach.MagicExplosiveKills_100, Scope.LifetimeInt, Stat.MagicExplosiveKills, 100);

        Add(Ach.AnyShopUpgradeLv5, Scope.LifetimeInt, Stat.AnyShopUpgradeReached5, 1);

        Add(Ach.Loss_1, Scope.LifetimeInt, Stat.Losses, 1);
        Add(Ach.Loss_50, Scope.LifetimeInt, Stat.Losses, 50);
        Add(Ach.Loss_100, Scope.LifetimeInt, Stat.Losses, 100);

        // Death block unit removal
        Add(Ach.DeathBlockRemove_1000, Scope.LifetimeInt, Stat.DeathblockUnitsRemoved, 1000);

        Add(Ach.EarthquakeRows_25, Scope.LifetimeInt, Stat.EarthquakeRowClears, 25);
        Add(Ach.EarthquakeRows_250, Scope.LifetimeInt, Stat.EarthquakeRowClears, 250);
        Add(Ach.EarthquakeRows_1000, Scope.LifetimeInt, Stat.EarthquakeRowClears, 1000);

        Add(Ach.FinalWinAllChars, Scope.LifetimeInt, Stat.FinalWinAllChars, 1);

        // -------- Run thresholds / flags --------
        Add(Ach.SingleAttack_100, Scope.RunInt, Stat.RunMaxSingleAttackDmg, 100);
        Add(Ach.Combo_10, Scope.RunInt, Stat.RunMaxCombo, 10);

        Add(Ach.FirstBuff, Scope.LifetimeInt, Stat.FirstBuffChosen, 1);
        Add(Ach.Buffs_15, Scope.RunInt, Stat.RunBuffModsChosen, 15);

        Add(Ach.FirstDebuff, Scope.LifetimeInt, Stat.FirstDebuffChosen, 1);

        Add(Ach.RunWins_10, Scope.RunInt, Stat.RunLevelsWon, 10);

        Add(Ach.UnlockAnySkin, Scope.RunInt, Stat.RunUnlockedAnySkin, 1);
        Add(Ach.UnlockAnyMonster, Scope.RunInt, Stat.RunUnlockedAnyMonster, 1);
        Add(Ach.UnlockAnyChar, Scope.RunInt, Stat.RunUnlockedAnyCharacter, 1);

        Add(Ach.BuyAnyShopUpgrade, Scope.RunInt, Stat.RunPurchasedAnyShopUpgrade, 1);

        Add(Ach.LevelTime_180, Scope.RunInt, Stat.RunLevelTime180, 1);
        Add(Ach.LevelTime_240, Scope.RunInt, Stat.RunLevelTime240, 1);
        Add(Ach.LevelTime_300, Scope.RunInt, Stat.RunLevelTime300, 1);

        Add(Ach.BeatFinal, Scope.RunInt, Stat.RunBeatFinalLevel, 1);

        // -------- Challenges --------
        // Perfect wins
        Add(Ach.PerfectWins_1, Scope.LifetimeInt, Stat.PerfectLevelWins, 1);
        Add(Ach.PerfectWins_25, Scope.LifetimeInt, Stat.PerfectLevelWins, 25);
        Add(Ach.PerfectWins_100, Scope.LifetimeInt, Stat.PerfectLevelWins, 100);

        // Win time checks
        Add(Ach.WinUnder_60, Scope.RunFloat, Stat.RunBestLevelWinSeconds, 60.0, Compare.Lte);
        Add(Ach.WinUnder_45, Scope.RunFloat, Stat.RunBestLevelWinSeconds, 45.0, Compare.Lte);
        Add(Ach.WinUnder_30, Scope.RunFloat, Stat.RunBestLevelWinSeconds, 30.0, Compare.Lte);

        // Gravity cap time flags
        Add(Ach.GravityCap_30s, Scope.RunInt, Stat.RunGravityCap30, 1);
        Add(Ach.GravityCap_60s, Scope.RunInt, Stat.RunGravityCap60, 1);
    }

    static void Add(string id, Scope scope, string statKey, double target, Compare compare = Compare.Gte)
    {
        _defs.Add(new Def { id = id, scope = scope, statKey = statKey, target = target, compare = compare });
    }

    public static void OnStatChanged(string statKey)
    {
        if (!_init) EnsureInitialized();
        if (PlayerProgress.I == null) return;

        // Evaluate only achievements that reference this key
        for (int i = 0; i < _defs.Count; i++)
        {
            var d = _defs[i];
            if (d.statKey != statKey) continue;

            if (PlayerProgress.I.IsUnlocked(d.id)) continue;

            double v = ReadStat(d.scope, d.statKey);

            bool ok = d.compare == Compare.Gte ? (v >= d.target) : (v <= d.target);
            if (ok) PlayerProgress.I.UnlockAchievement(d.id);
        }
    }

    static double ReadStat(Scope scope, string key)
    {
        switch (scope)
        {
            case Scope.LifetimeInt: return PlayerProgress.I.GetLifetimeInt(key);
            case Scope.LifetimeFloat: return PlayerProgress.I.GetLifetimeFloat(key);
            case Scope.RunInt: return PlayerProgress.I.GetRunInt(key);
            case Scope.RunFloat: return PlayerProgress.I.GetRunFloat(key);
        }
        return 0;
    }
}