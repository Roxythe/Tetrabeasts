using System;
using UnityEngine;

public partial class GameController
{
    // Project both choices without applying either of them to the live run.
    public string RewardPreview(RunModifier reward, RunModifier pendingBuff = null, Color? buffColor = null, Color? debuffColor = null)
    {
        string Change(string label, string before, string after)
        {
            Color good = buffColor ?? new Color(.15641688f, .4393744f, .8962264f);
            Color bad = debuffColor ?? new Color(.8679245f, 0f, 0f);
            string color = before == after ? "#D8D3CB" : "#" + ColorUtility.ToHtmlStringRGB(reward.isBuff ? good : bad);
            return $"{TetrabeastsLocalization.LocalizeText(label)}\n{before}  ->  <color={color}>{after}</color>";
        }

        bool sameStat = pendingBuff && pendingBuff.stat == reward.stat;
        string Numbers(string label, float raw, string unit = "", Func<float, float> effective = null, bool integers = false)
        {
            float Apply(RunModifier mod, float n) => integers ? mod.ProjectInteger(Mathf.RoundToInt(n)) : mod.Project(n);
            float before = sameStat ? Apply(pendingBuff, raw) : raw;
            float after = Apply(reward, before);
            string Format(float n) => (effective != null ? effective(n) : n).ToString("0.##") + unit;
            return Change(label, Format(before), Format(after));
        }
        string Flag(string label, bool disabled, string enabledName = "On", string disabledName = "Off")
        {
            if (sameStat) disabled = pendingBuff.ProjectFlag(disabled);
            string Format(bool flag) => TetrabeastsLocalization.LocalizeText(flag ? disabledName : enabledName);
            return Change(label, Format(disabled), Format(reward.ProjectFlag(disabled)));
        }
        switch (reward.stat)
        {
            case RunModStat.CurrencyPerRoundWin: return Numbers("Gold per round win", currencyPerRoundWin, "", n =>
                Mathf.Max(0, Mathf.RoundToInt((n + currencyPerRoundWinAdd) * currencyPerRoundWinMult * CurrentCurrencyGainMultiplier)), true);
            case RunModStat.EnemyAttackIntervalMult: return Numbers("Enemy attack interval", enemyAttackIntervalMult, "×");
            case RunModStat.EnemyProjectileDamageMult: return Numbers("Enemy attack power", enemyProjectileDamageMult, "×");
            case RunModStat.EnemyProjectileSpeedMult: return Numbers("Enemy projectile speed", enemyProjectileSpeedMult, "×");
            case RunModStat.EnemyCastleHpMult: return Numbers("Enemy castle health", enemyCastleHpMult, "×");
            case RunModStat.SpecialGaugeGainPerSecond: return Numbers("Special gauge gain", specialGaugeGainPerSecond, "/s", n => Mathf.Max(0f,n) * EffectiveSpecialGaugeGainMultiplierForStats);
            case RunModStat.SpecialGaugeDrainPerSecond: return Numbers("Special gauge drain", specialGaugeDrainPerSecond, "/s");
            case RunModStat.SpecialBlockChanceAdd: return Numbers("Special piece chance", specialBlockChanceAdd, "%", n =>
                SpecialBlocksLockedForStats || specialBlocks == null || specialBlocks.Length == 0 ? 0f :
                100f * Mathf.Clamp(specialChancePerEnqueue + n, Mathf.Min(minSpecialChance,maxSpecialChance), maxSpecialChance));
            case RunModStat.PieceGravityMult: return Numbers("Piece fall speed", pieceGravityMult, "×");
            case RunModStat.FallRampRateMult: return Numbers("Gravity buildup", fallRampRateMult, "×");
            case RunModStat.MonsterDamageMult: return Numbers("Monster attack power", monsterDamageMult, "×");
            case RunModStat.MonsterSpecialGainMult: return Numbers("Monster special gain", monsterSpecialGainMult, "×");
            case RunModStat.MonsterMaxHpMult: return Numbers("Monster health", monsterMaxHpMult, "×");
            case RunModStat.LuckAdd: return Numbers("Luck", luck, "", n => n + ShopBuffEffects.LuckBonus);
            case RunModStat.MisfortuneAdd: return Numbers("Misfortune", misfortune, "", n => n + _starDifficultyModifiers.misfortuneAdd);
            case RunModStat.HealPowerMult: return Numbers("Healing power", healPowerMult, "×");
            case RunModStat.HealRangeAdd: return Numbers("Bonus healing range", healRangeAdd, " cells", integers: true);
            case RunModStat.DisableNextPreview: return Flag("Next piece preview", disableNextPreview);
            case RunModStat.DisableLandingHint: return Flag("Landing hint", disableLandingHint);
            case RunModStat.LineClearCurrencyChanceAdd: return Numbers("Row clear gold chance", lineClearCurrencyChanceAdd, "%", n =>
                100f * Mathf.Clamp01(currencyChancePerClearedRow + ShopBuffEffects.GoldChanceBonus + PostFinalSurvivalStarCurrencyChanceBonus + PostFinalSurvivalTimedCurrencyChanceBonus + n));
            case RunModStat.LineClearCurrencyAmountMult: return Numbers("Row clear gold", lineClearCurrencyAmountMult, "×", n => n * CurrentCurrencyGainMultiplier * PostFinalSurvivalCurrencyAmountMultiplier);
            case RunModStat.StoneBuffDropChanceAdd: return Numbers("Stone reward chance", stoneBuffDropChanceAdd, "%", n =>
                100f * Mathf.Clamp01(stoneBuffDropChance + _partyPassiveBonuses.stoneBuffDropChance + n));
            case RunModStat.StoneObstacleDropsDebuffsOnly: return Flag("Stone rewards", stoneObstacleDropsDebuffsOnly, "Buffs", "Debuffs");
            case RunModStat.ReserveUnitsRestoredOnWinAdd: return Numbers("Reinforcements per win", reserveUnitsRestoredOnWinAdd, "", n =>
                disableRoundWinReserveRestore ? 0f : Mathf.Max(0, reinforcementsPerWin + _partyPassiveBonuses.reserveUnitsRestoredOnWin + n), true);
            case RunModStat.MaxReserveUnitsAdd:
                int baseMax = maxUnitLives + ShopBuffEffects.UnitLivesBonus + _partyPassiveBonuses.startingReserveUnits;
                int ProjectMax(RunModifier mod, int raw) => mod.ProjectInteger(mod.op == RunModOp.Add ? raw : Mathf.Max(1, raw));
                int current = baseMax + maxReserveUnitsAdd;
                if (sameStat) current = ProjectMax(pendingBuff, current);
                return Change("Maximum reserves", Mathf.Max(1, current).ToString(), Mathf.Max(1, ProjectMax(reward,current)).ToString());
            case RunModStat.DisableRoundWinReserveRestore:
                int bonus = pendingBuff && pendingBuff.stat == RunModStat.ReserveUnitsRestoredOnWinAdd
                    ? pendingBuff.ProjectInteger(reserveUnitsRestoredOnWinAdd) : reserveUnitsRestoredOnWinAdd;
                int amount = Mathf.Max(0, reinforcementsPerWin + _partyPassiveBonuses.reserveUnitsRestoredOnWin + bonus);
                return Change("Reinforcements per win", (disableRoundWinReserveRestore ? 0 : amount).ToString(),
                    (reward.ProjectFlag(disableRoundWinReserveRestore) ? 0 : amount).ToString());
            default: return TetrabeastsLocalization.LocalizeText(reward.description);
        }
    }
}
