using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public struct MonsterPassiveBonuses
{
    public float comboDurationSeconds;
    public float bonusComboChance;
    public float stoneBuffDropChance;
    public float currencyGainMultiplierAdd;
    public float partyExperienceGainMultiplierAdd;
    public int startingReserveUnits;
    public int reserveUnitsRestoredOnWin;
    public float allyMonsterDamageDoneReduction;
    public float allyMonsterDamageTakenReduction;

    public static MonsterPassiveBonuses operator +(MonsterPassiveBonuses a, MonsterPassiveBonuses b)
    {
        return new MonsterPassiveBonuses
        {
            comboDurationSeconds = a.comboDurationSeconds + b.comboDurationSeconds,
            bonusComboChance = a.bonusComboChance + b.bonusComboChance,
            stoneBuffDropChance = a.stoneBuffDropChance + b.stoneBuffDropChance,
            currencyGainMultiplierAdd = a.currencyGainMultiplierAdd + b.currencyGainMultiplierAdd,
            partyExperienceGainMultiplierAdd = a.partyExperienceGainMultiplierAdd + b.partyExperienceGainMultiplierAdd,
            startingReserveUnits = a.startingReserveUnits + b.startingReserveUnits,
            reserveUnitsRestoredOnWin = a.reserveUnitsRestoredOnWin + b.reserveUnitsRestoredOnWin,
            allyMonsterDamageDoneReduction = a.allyMonsterDamageDoneReduction + b.allyMonsterDamageDoneReduction,
            allyMonsterDamageTakenReduction = a.allyMonsterDamageTakenReduction + b.allyMonsterDamageTakenReduction
        };
    }
}

public static class MonsterPassiveSystem
{
    public static bool IsPassiveUpgradeLevel(int level)
    {
        return level == 10 || level == 25 || level == 50;
    }

    public static bool ReplacesStatUpgrade(MonsterData data, int level)
    {
        return data && data.passiveType != MonsterPassiveType.None && IsPassiveUpgradeLevel(level);
    }

    public static MonsterPassiveBonuses GetBonuses(MonsterData data, int level)
    {
        var bonuses = new MonsterPassiveBonuses();
        if (!data || data.passiveType == MonsterPassiveType.None)
            return bonuses;

        switch (data.passiveType)
        {
            case MonsterPassiveType.ComboDuration:
                bonuses.comboDurationSeconds = GetComboDurationSeconds(level);
                break;

            case MonsterPassiveType.BonusComboChance:
                bonuses.bonusComboChance = GetBonusComboChance(level);
                break;

            case MonsterPassiveType.StoneBuffDropChance:
                bonuses.stoneBuffDropChance = GetStoneBuffDropChance(level);
                break;

            case MonsterPassiveType.StartingReserveUnits:
                bonuses.startingReserveUnits = GetStartingReserveUnits(level);
                break;

            case MonsterPassiveType.ReserveUnitsRestoredOnWin:
                bonuses.reserveUnitsRestoredOnWin = GetReserveUnitsRestoredOnWin(level);
                break;

            case MonsterPassiveType.AllyMonsterBulwark:
                float reduction = GetBulwarkReduction(level);
                bonuses.allyMonsterDamageDoneReduction = reduction;
                bonuses.allyMonsterDamageTakenReduction = reduction;
                break;

            case MonsterPassiveType.CurrencyGain:
                bonuses.currencyGainMultiplierAdd = GetCurrencyGainBonus(level);
                break;

            case MonsterPassiveType.PartyExperienceGain:
                bonuses.partyExperienceGainMultiplierAdd = GetPartyExperienceGainBonus(level);
                break;

            case MonsterPassiveType.StoneForager:
                bonuses.stoneBuffDropChance = GetStoneForagerBuffDropChance(level);
                break;
        }

        return bonuses;
    }

    public static MonsterPassiveBonuses GetCombinedBonuses(IEnumerable<MonsterData> roster)
    {
        var total = new MonsterPassiveBonuses();
        if (roster == null) return total;

        foreach (var md in roster)
        {
            if (!md || string.IsNullOrEmpty(md.monsterName))
                continue;

            int level = RunMonsterProgress.GetCurrentLevel(md.monsterName);
            total += GetBonuses(md, level);
        }

        total.bonusComboChance = Mathf.Clamp01(total.bonusComboChance);
        total.stoneBuffDropChance = Mathf.Clamp(total.stoneBuffDropChance, 0f, 1f);
        total.currencyGainMultiplierAdd = Mathf.Max(0f, total.currencyGainMultiplierAdd);
        total.partyExperienceGainMultiplierAdd = Mathf.Max(0f, total.partyExperienceGainMultiplierAdd);
        total.allyMonsterDamageDoneReduction = Mathf.Clamp01(total.allyMonsterDamageDoneReduction);
        total.allyMonsterDamageTakenReduction = Mathf.Clamp01(total.allyMonsterDamageTakenReduction);
        return total;
    }

    public static string GetPassiveName(MonsterData data)
    {
        if (!data) return "";

        switch (data.passiveType)
        {
            case MonsterPassiveType.ComboDuration:
                return TetrabeastsLocalization.LocalizeText("Combo Extension");
            case MonsterPassiveType.BonusComboChance:
                return TetrabeastsLocalization.LocalizeText("Chain Surge");
            case MonsterPassiveType.StoneBuffDropChance:
                return TetrabeastsLocalization.LocalizeText("Stone Scrounger");
            case MonsterPassiveType.StartingReserveUnits:
                return TetrabeastsLocalization.LocalizeText("Reserve Stockpile");
            case MonsterPassiveType.ReserveUnitsRestoredOnWin:
                return TetrabeastsLocalization.LocalizeText("Reserve Recovery");
            case MonsterPassiveType.AllyMonsterBulwark:
                return TetrabeastsLocalization.LocalizeText("Bulwark Aura");
            case MonsterPassiveType.CurrencyGain:
                return TetrabeastsLocalization.LocalizeText("Treasure Aura");
            case MonsterPassiveType.PartyExperienceGain:
                return TetrabeastsLocalization.LocalizeText("Shared Wisdom");
            case MonsterPassiveType.StoneForager:
                return TetrabeastsLocalization.LocalizeText("Terra's Blessing");
            default:
                return "";
        }
    }

    public static string GetPassiveDescription(MonsterData data, int level)
    {
        if (!data || data.passiveType == MonsterPassiveType.None)
            return "";

        switch (data.passiveType)
        {
            case MonsterPassiveType.ComboDuration:
                return TetrabeastsLocalization.LocalizeFormat("Increase combo timer duration by {0}.", FormatSeconds(GetComboDurationSeconds(level)));
            case MonsterPassiveType.BonusComboChance:
                return TetrabeastsLocalization.LocalizeFormat("Each row clear has a {0} chance to increase combo count one additional time.", FormatPercent(GetBonusComboChance(level)));
            case MonsterPassiveType.StoneBuffDropChance:
                return TetrabeastsLocalization.LocalizeFormat("Increase chance of buff drop from stone obstacle destruction by {0}.", FormatPercent(GetStoneBuffDropChance(level)));
            case MonsterPassiveType.StartingReserveUnits:
                return TetrabeastsLocalization.LocalizeFormat("Increase the number of starting reserve units by {0}.", GetStartingReserveUnits(level));
            case MonsterPassiveType.ReserveUnitsRestoredOnWin:
                return TetrabeastsLocalization.LocalizeFormat("Increase the number of reserve units restored on round win by {0}.", GetReserveUnitsRestoredOnWin(level));
            case MonsterPassiveType.AllyMonsterBulwark:
                return TetrabeastsLocalization.LocalizeFormat("Decrease damage taken and damage done for all ally monster units by {0}.", FormatPercent(GetBulwarkReduction(level)));
            case MonsterPassiveType.CurrencyGain:
                return TetrabeastsLocalization.LocalizeFormat("Increase all currency gained by {0}.", FormatPercent(GetCurrencyGainBonus(level)));
            case MonsterPassiveType.PartyExperienceGain:
                return TetrabeastsLocalization.LocalizeFormat("Increase EXP gained by all party members by {0}.", FormatPercent(GetPartyExperienceGainBonus(level)));
            case MonsterPassiveType.StoneForager:
                return TetrabeastsLocalization.LocalizeFormat("Increase chance of buff drop from stone obstacle destruction by {0}.", FormatPercent(GetStoneForagerBuffDropChance(level)));
            default:
                return "";
        }
    }

    public static string GetPassivePreviewBlock(MonsterData data, int level)
    {
        if (!data || data.passiveType == MonsterPassiveType.None)
            return "";

        string name = GetPassiveName(data);
        string description = GetPassiveDescription(data, level);
        string next = GetNextUpgradePreview(data, level);

        string header = $"<b><color=#FFD700>{TetrabeastsLocalization.LocalizeFormat("Passive - {0}", name)}</color></b>";

        if (string.IsNullOrEmpty(next))
            return $"{header}\n{description}";

        return $"{header}\n{description}\n{next}";
    }

    public static string GetPassiveUpgradeLine(MonsterData data, int level)
    {
        if (!ReplacesStatUpgrade(data, level))
            return "";

        string name = GetPassiveName(data);
        string description = GetPassiveDescription(data, level);
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description))
            return "";

        return $"<b><color=#FFD700>{TetrabeastsLocalization.LocalizeFormat("Passive - {0}:", name)}</color></b> {description}";
    }

    static string GetNextUpgradePreview(MonsterData data, int level)
    {
        if (!data || data.passiveType == MonsterPassiveType.None)
            return "";

        int nextLevel;
        if (level < 10) nextLevel = 10;
        else if (level < 25) nextLevel = 25;
        else if (level < 50) nextLevel = 50;
        else return TetrabeastsLocalization.LocalizeText("Passive is fully upgraded.");

        return $"\n<b><color=#FFD700>{TetrabeastsLocalization.LocalizeFormat("Next upgrade at Lv.{0}:", nextLevel)}</color></b> {GetPassiveDescription(data, nextLevel)}";
    }

    static int GetPassiveTierIndex(int level)
    {
        level = Mathf.Clamp(level, 1, MonsterLeveling.MaxLevel);
        if (level >= 50) return 3;
        if (level >= 25) return 2;
        if (level >= 10) return 1;
        return 0;
    }

    static float GetComboDurationSeconds(int level)
    {
        return 1f + GetPassiveTierIndex(level);
    }

    static float GetBonusComboChance(int level)
    {
        return 0.10f + (GetPassiveTierIndex(level) * 0.10f);
    }

    static float GetStoneBuffDropChance(int level)
    {
        return 0.05f + (GetPassiveTierIndex(level) * 0.05f);
    }

    static int GetStartingReserveUnits(int level)
    {
        return 5 + (GetPassiveTierIndex(level) * 5);
    }

    static int GetReserveUnitsRestoredOnWin(int level)
    {
        return 2 + (GetPassiveTierIndex(level) * 2);
    }

    static float GetBulwarkReduction(int level)
    {
        return 0.05f + (GetPassiveTierIndex(level) * 0.05f);
    }

    static float GetCurrencyGainBonus(int level)
    {
        return 0.05f + (GetPassiveTierIndex(level) * 0.05f);
    }

    static float GetPartyExperienceGainBonus(int level)
    {
        return 0.05f + (GetPassiveTierIndex(level) * 0.05f);
    }

    static float GetStoneForagerBuffDropChance(int level)
    {
        return 0.02f + (GetPassiveTierIndex(level) * 0.01f);
    }

    static string FormatPercent(float value)
    {
        return $"{Mathf.RoundToInt(value * 100f)}%";
    }

    static string FormatSeconds(float seconds)
    {
        int rounded = Mathf.RoundToInt(seconds);
        return rounded == 1
            ? TetrabeastsLocalization.LocalizeText("1 second")
            : TetrabeastsLocalization.LocalizeFormat("{0} seconds", rounded);
    }
}

public static class MonsterLeveling
{
    public const int XpPerLevel = 100;
    public const int MaxLevel = 100;
    const float HpPerLevelStep = 5f;
    const float AttackPowerPerLevelStep = 0.1f;
    const float HealPowerPerLevelStep = 3f;
    const float HealRangePerLevelStep = 1f;
    const float SpecialGaugeGainPerLevelStep = 0.1f;
    const int RoleMilestoneInterval = 10;
    const float AttackRoleMilestoneBonus = 1.0f;
    const float DefenseRoleMilestoneHpBonus = 25.0f;
    const float HealerRoleMilestoneHealBonus = 7.0f;

    public struct LeveledStats
    {
        public float maxHealth;
        public float attackPower;
        public float healAmount;
        public float healRange;
        public float healSpeed;
        public float specialGaugeGain;
    }

    public static LeveledStats GetLeveledStats(MonsterData data, int level)
    {
        if (!data) return default;

        level = Mathf.Clamp(level, 1, MaxLevel);

        var s = new LeveledStats
        {
            maxHealth = data.maxHealth,
            attackPower = data.attackPower,
            healAmount = data.healAmount,
            healRange = data.healRange,
            healSpeed = data.healSpeed,
            specialGaugeGain = data.specialGaugeGain
        };

        if (level <= 1) return s;

        var cycle = GetCycleForRole(data.role);
        int cycleLength = cycle.Length;

        for (int newLevel = 2; newLevel <= level; newLevel++)
        {
            if (!MonsterPassiveSystem.ReplacesStatUpgrade(data, newLevel))
            {
                int stepIndex = GetStepIndexForLevel(newLevel, cycleLength);
                ApplyStep(ref s, cycle[stepIndex]);
            }

            ApplyRoleMilestoneBonus(ref s, data.role, newLevel);
        }

        return s;
    }

    private enum StatStep
    {
        Hp,
        Attack,
        SpecialGain,
        HealPower,
        HealRange
    }

    static int GetStepIndexForLevel(int newLevel, int cycleLength)
    {
        return Mathf.Max(0, newLevel - 2) % cycleLength;
    }

    static void ApplyStep(ref LeveledStats s, StatStep step)
    {
        switch (step)
        {
            case StatStep.Hp:
                s.maxHealth += HpPerLevelStep;
                break;
            case StatStep.Attack:
                s.attackPower += AttackPowerPerLevelStep;
                break;
            case StatStep.SpecialGain:
                s.specialGaugeGain += SpecialGaugeGainPerLevelStep;
                break;
            case StatStep.HealPower:
                s.healAmount += HealPowerPerLevelStep;
                break;
            case StatStep.HealRange:
                s.healRange += HealRangePerLevelStep;
                break;
        }
    }

    static void ApplyRoleMilestoneBonus(ref LeveledStats s, MonsterRole role, int newLevel)
    {
        if (!IsRoleMilestoneLevel(newLevel))
            return;

        switch (role)
        {
            case MonsterRole.Attack:
                s.attackPower += AttackRoleMilestoneBonus;
                break;
            case MonsterRole.Defense:
                s.maxHealth += DefenseRoleMilestoneHpBonus;
                break;
            case MonsterRole.Healer:
                s.healAmount += HealerRoleMilestoneHealBonus;
                break;
        }
    }

    static bool IsRoleMilestoneLevel(int newLevel)
    {
        return newLevel > 1 && RoleMilestoneInterval > 0 && newLevel % RoleMilestoneInterval == 0;
    }

    public static string GetStatIncreaseSummary(MonsterData data, int fromLevel, int toLevel)
    {
        if (!data) return "";
        fromLevel = Mathf.Clamp(fromLevel, 1, MaxLevel);
        toLevel = Mathf.Clamp(toLevel, 1, MaxLevel);
        if (toLevel <= fromLevel) return "";

        float hp = 0f;
        float attack = 0f;
        int specialSteps = 0;
        float heal = 0f;
        float range = 0f;
        var specialLines = new List<string>();

        var cycle = GetCycleForRole(data.role);
        int n = cycle.Length;

        for (int newLevel = fromLevel + 1; newLevel <= toLevel; newLevel++)
        {
            if (MonsterPassiveSystem.ReplacesStatUpgrade(data, newLevel))
            {
                specialLines.Add("Passive+");
            }
            else
            {
                int stepIndex = GetStepIndexForLevel(newLevel, n);
                switch (cycle[stepIndex])
                {
                    case StatStep.Hp: hp += HpPerLevelStep; break;
                    case StatStep.Attack: attack += AttackPowerPerLevelStep; break;
                    case StatStep.SpecialGain: specialSteps++; break;
                    case StatStep.HealPower: heal += HealPowerPerLevelStep; break;
                    case StatStep.HealRange: range += HealRangePerLevelStep; break;
                }
            }

            AddRoleMilestoneAmounts(data.role, newLevel, ref hp, ref attack, ref heal);
        }

        // Build compact UI string
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        void AddNumber(string label, float v)
        {
            if (v <= 0) return;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(label).Append(" +").Append(FormatStatAmount(v));
        }

        void AddSymbolic(string label, int steps)
        {
            if (steps <= 0) return;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(label).Append("+");
        }

        AddNumber("HP", hp);
        AddNumber("ATK", attack);
        AddSymbolic("Special", specialSteps);
        AddNumber("Heal", heal);
        AddNumber("Range", range);

        foreach (var line in specialLines)
        {
            if (sb.Length > 0) sb.Append(" | ");
            sb.Append(line);
        }

        return sb.ToString();
    }

    static void AddRoleMilestoneAmounts(MonsterRole role, int newLevel, ref float hp, ref float attack, ref float heal)
    {
        if (!IsRoleMilestoneLevel(newLevel))
            return;

        switch (role)
        {
            case MonsterRole.Attack:
                attack += AttackRoleMilestoneBonus;
                break;
            case MonsterRole.Defense:
                hp += DefenseRoleMilestoneHpBonus;
                break;
            case MonsterRole.Healer:
                heal += HealerRoleMilestoneHealBonus;
                break;
        }
    }

    static string FormatStatAmount(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value))
            ? Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.#", CultureInfo.InvariantCulture);
    }

    static StatStep[] GetCycleForRole(MonsterRole role)
    {
        switch (role)
        {
            case MonsterRole.Attack:
                return new[]
                {
                StatStep.Attack,
                StatStep.Hp,
                StatStep.Attack,
                StatStep.SpecialGain
            };

            case MonsterRole.Defense:
                return new[]
                {
                StatStep.Hp,
                StatStep.Attack,
                StatStep.Hp,
                StatStep.SpecialGain,
                StatStep.Hp,
                StatStep.Attack
            };

            case MonsterRole.Healer:
                return new[]
                {
                StatStep.HealPower,
                StatStep.Hp,
                StatStep.SpecialGain,
                StatStep.HealPower,
                StatStep.Hp,
                StatStep.SpecialGain,
                StatStep.HealRange
            };

            default:
                return new[] { StatStep.Hp };
        }
    }

    public static List<string> GetOrderedStatStepLines(MonsterData data, int fromLevel, int toLevel)
    {
        var lines = new List<string>();
        if (!data) return lines;

        fromLevel = Mathf.Clamp(fromLevel, 1, MaxLevel);
        toLevel = Mathf.Clamp(toLevel, 1, MaxLevel);
        if (toLevel <= fromLevel) return lines;

        var cycle = GetCycleForRole(data.role);
        int n = cycle.Length;

        for (int newLevel = fromLevel + 1; newLevel <= toLevel; newLevel++)
        {
            string line;
            if (MonsterPassiveSystem.ReplacesStatUpgrade(data, newLevel))
            {
                line = "Passive+";
            }
            else
            {
                int stepIndex = GetStepIndexForLevel(newLevel, n);
                var step = cycle[stepIndex];
                line = GetStatStepLine(step);
            }

            string milestoneLine = GetRoleMilestoneLine(data.role, newLevel);
            line = CombineStatStepLines(line, milestoneLine);

            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line);
        }

        return lines;
    }

    static string GetStatStepLine(StatStep step)
    {
        switch (step)
        {
            case StatStep.Hp:
                return "+ 5 HP";
            case StatStep.Attack:
                return $"+ {FormatStatAmount(AttackPowerPerLevelStep)} ATK";
            case StatStep.SpecialGain:
                return $"+ {FormatStatAmount(SpecialGaugeGainPerLevelStep)} Special";
            case StatStep.HealPower:
                return "+ 1 Heal";
            case StatStep.HealRange:
                return "+ 1 Range";
            default:
                return "";
        }
    }

    static string GetRoleMilestoneLine(MonsterRole role, int newLevel)
    {
        if (!IsRoleMilestoneLevel(newLevel))
            return "";

        switch (role)
        {
            case MonsterRole.Attack:
                return "+ 0.5 ATK";
            case MonsterRole.Defense:
                return "+ 25 HP";
            case MonsterRole.Healer:
                return "+ 5 Heal";
            default:
                return "";
        }
    }

    static string CombineStatStepLines(string first, string second)
    {
        bool hasFirst = !string.IsNullOrWhiteSpace(first);
        bool hasSecond = !string.IsNullOrWhiteSpace(second);

        if (hasFirst && hasSecond)
            return first + "\n" + second;

        if (hasFirst)
            return first;

        return hasSecond ? second : "";
    }
}
