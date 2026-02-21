using System;
using System.Collections.Generic;
using UnityEngine;

public static class AchievementSystem
{
    enum Scope { LifetimeInt, LifetimeFloat, RunInt, RunFloat }
    enum Compare { Gte, Lte } // for min-time achievements etc.

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

    // ---------- Stat keys (keep these stable forever) ----------
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

        public const string StoneDestroyed = "lt_stone_destroyed";
        public const string MagicExplosiveKills = "lt_magic_explosive_kills";

        public const string CharactersUnlocked = "lt_characters_unlocked";
        public const string MonstersUnlocked = "lt_monsters_unlocked";
        public const string SkinsUnlocked = "lt_skins_unlocked";

        // Lifetime: first-time flags
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
    }

    // ---------- Achievement IDs  ----------
    public static class Ach
    {
        // Examples (you’ll add more thresholds easily)
        public const string ClearRows_100 = "ACH_CLEAR_ROWS_100";
        public const string GoldEarned_1000 = "ACH_GOLD_1000";
        public const string SpecialsUsed_50 = "ACH_SPECIALS_50";
        public const string LevelsWon_10 = "ACH_WINS_10";

        public const string Combo_10 = "ACH_COMBO_10";
        public const string SingleAttack_100 = "ACH_SINGLE_ATK_100";

        public const string FirstBuff = "ACH_FIRST_BUFF";
        public const string FirstDebuff = "ACH_FIRST_DEBUFF";
        public const string FirstShopUpgrade = "ACH_SHOP_BUY_ANY";

        public const string BeatFinal = "ACH_BEAT_FINAL";

        public const string PerfectWins_5 = "ACH_PERFECT_WINS_5";
        public const string GravityCap_10s = "ACH_GRAVITY_CAP_10S";
    }

    public static void EnsureInitialized()
    {
        if (_init) return;
        _init = true;

        // Lifetime thresholds
        Add(Ach.ClearRows_100, Scope.LifetimeInt, Stat.TotalRowClears, 100);
        Add(Ach.GoldEarned_1000, Scope.LifetimeInt, Stat.GoldEarned, 1000);
        Add(Ach.SpecialsUsed_50, Scope.LifetimeInt, Stat.SpecialsUsedTotal, 50);
        Add(Ach.LevelsWon_10, Scope.LifetimeInt, Stat.LevelsWon, 10);

        // Run thresholds
        Add(Ach.Combo_10, Scope.RunInt, Stat.RunMaxCombo, 10);
        Add(Ach.SingleAttack_100, Scope.RunInt, Stat.RunMaxSingleAttackDmg, 100);

        // First-time flags (stored as lifetime int 0/1)
        Add(Ach.FirstBuff, Scope.LifetimeInt, Stat.FirstBuffChosen, 1);
        Add(Ach.FirstDebuff, Scope.LifetimeInt, Stat.FirstDebuffChosen, 1);
        Add(Ach.FirstShopUpgrade, Scope.RunInt, Stat.RunPurchasedAnyShopUpgrade, 1);

        // Beat final level (run flag)
        Add(Ach.BeatFinal, Scope.RunInt, Stat.RunBeatFinalLevel, 1);

        // Challenges
        Add(Ach.PerfectWins_5, Scope.LifetimeInt, Stat.PerfectLevelWins, 5);
        Add(Ach.GravityCap_10s, Scope.RunFloat, Stat.RunGravityCapSeconds, 10.0);
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