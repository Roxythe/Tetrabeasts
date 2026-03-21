using System.Collections.Generic;
using UnityEngine;

public static class MonsterLeveling
{
    public const int XpPerLevel = 100;
    public const int MaxLevel = 100;

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

        int ups = level - 1;
        if (ups <= 0) return s;

        switch (data.role)
        {
            case MonsterRole.Attack:
                ApplyCycle(ref s, ups, new StatStep[]
                {
                    StatStep.Attack,
                    StatStep.Hp,
                    StatStep.Attack,
                    StatStep.SpecialGain
                });
                break;

            case MonsterRole.Defense:
                ApplyCycle(ref s, ups, new StatStep[]
                {
                    StatStep.Hp,
                    StatStep.Attack,
                    StatStep.Hp,
                    StatStep.SpecialGain,
                    StatStep.Hp,
                    StatStep.Attack
                });
                break;

            case MonsterRole.Healer:
                ApplyCycle(ref s, ups, new StatStep[]
                {
                    StatStep.HealPower,
                    StatStep.Hp,
                    StatStep.SpecialGain,
                    StatStep.HealPower,
                    StatStep.Hp,
                    StatStep.SpecialGain,
                    StatStep.HealRange
                });
                break;
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

    private static void ApplyCycle(ref LeveledStats s, int ups, StatStep[] cycle)
    {
        int n = cycle.Length;
        for (int i = 0; i < ups; i++)
        {
            var step = cycle[i % n];
            switch (step)
            {
                case StatStep.Hp:
                    s.maxHealth += 5f;
                    break;
                case StatStep.Attack:
                    s.attackPower += 1f;
                    break;
                case StatStep.SpecialGain:
                    s.specialGaugeGain += 1f;
                    break;
                case StatStep.HealPower:
                    s.healAmount += 1f;
                    break;
                case StatStep.HealRange:
                    s.healRange += 1f;
                    break;
            }
        }
    }

    public static string GetStatIncreaseSummary(MonsterData data, int fromLevel, int toLevel)
    {
        if (!data) return "";
        fromLevel = Mathf.Clamp(fromLevel, 1, MaxLevel);
        toLevel = Mathf.Clamp(toLevel, 1, MaxLevel);
        if (toLevel <= fromLevel) return "";

        int hp = 0, atk = 0, sp = 0, heal = 0, range = 0;

        var cycle = GetCycleForRole(data.role);
        int n = cycle.Length;

        for (int newLevel = fromLevel + 1; newLevel <= toLevel; newLevel++)
        {
            int stepIndex = (newLevel - 2) % n;
            switch (cycle[stepIndex])
            {
                case StatStep.Hp: hp++; break;
                case StatStep.Attack: atk++; break;
                case StatStep.SpecialGain: sp++; break;
                case StatStep.HealPower: heal++; break;
                case StatStep.HealRange: range++; break;
            }
        }

        // Build compact UI string
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        void Add(string label, int v)
        {
            if (v <= 0) return;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(label).Append(" +").Append(v);
        }

        Add("HP", hp);
        Add("ATK", atk);
        Add("Special", sp);
        Add("Heal", heal);
        Add("Range", range);

        return sb.ToString();
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
            int stepIndex = (newLevel - 2) % n;
            var step = cycle[stepIndex];

            switch (step)
            {
                case StatStep.Hp:
                    lines.Add("+ 5 HP");
                    break;
                case StatStep.Attack:
                    lines.Add("+ 1 Attack");
                    break;
                case StatStep.SpecialGain:
                    lines.Add("+ 1 Special");
                    break;
                case StatStep.HealPower:
                    lines.Add("+ 5 Heal");
                    break;
                case StatStep.HealRange:
                    lines.Add("+ 1 Range");
                    break;
            }
        }

        return lines;
    }
}