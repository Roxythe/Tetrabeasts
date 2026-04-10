using System.Text;
using UnityEngine;

public struct StarDifficultyModifiers
{
    public int stars;
    public float enemyHealthMultiplier;
    public float enemyDamageMultiplier;
    public float misfortuneAdd;
    public float specialGaugeGainMultiplier;
    public float expGainMultiplier;
    public float scoreGainMultiplier;

    public StarDifficultyModifiers(int stars)
    {
        this.stars = StarDifficultySystem.ClampStars(stars);
        enemyHealthMultiplier = 1f + this.stars;
        enemyDamageMultiplier = 1f + this.stars;
        misfortuneAdd = this.stars * 20f;
        specialGaugeGainMultiplier = Mathf.Max(0.01f, 1f - (this.stars * 0.15f));
        expGainMultiplier = 1f + (this.stars * 0.50f);
        scoreGainMultiplier = 1f + (this.stars * 0.50f);
    }

    public int EnemyHealthPercentIncrease => Mathf.RoundToInt((enemyHealthMultiplier - 1f) * 100f);
    public int EnemyDamagePercentIncrease => Mathf.RoundToInt((enemyDamageMultiplier - 1f) * 100f);
    public int SpecialGaugePercentDecrease => Mathf.RoundToInt((1f - specialGaugeGainMultiplier) * 100f);
    public int ExpPercentIncrease => Mathf.RoundToInt((expGainMultiplier - 1f) * 100f);
    public int ScorePercentIncrease => Mathf.RoundToInt((scoreGainMultiplier - 1f) * 100f);
}

public static class StarDifficultySystem
{
    public const int MaxStars = 5;

    public static int ClampStars(int stars)
    {
        return Mathf.Clamp(stars, 0, MaxStars);
    }

    public static StarDifficultyModifiers GetModifiers(int stars)
    {
        return new StarDifficultyModifiers(stars);
    }

    public static string GetSelectionLabel(int stars)
    {
        stars = ClampStars(stars);
        if (stars <= 0)
            return "0 Stars: Normal difficulty";

        return stars == 1 ? "1 Star selected" : $"{stars} Stars selected";
    }

    public static string GetUnlockInstruction(int targetStars)
    {
        targetStars = ClampStars(targetStars);
        if (targetStars <= 0)
            return "0 Stars is always available.";

        int requiredStars = Mathf.Max(0, targetStars - 1);
        return $"Beat the final level on {FormatStars(requiredStars)} to unlock {FormatStars(targetStars)}.";
    }

    public static string BuildTooltipText(int stars, int maxUnlockedStars)
    {
        stars = ClampStars(stars);
        maxUnlockedStars = ClampStars(maxUnlockedStars);

        if (stars <= 0)
            return "0-Star Difficulty\nNormal difficulty.\nNo gameplay modifiers.";

        StarDifficultyModifiers modifiers = GetModifiers(stars);
        StringBuilder sb = new StringBuilder(192);

        sb.Append(FormatStars(stars)).Append(" Difficulty");
        if (stars > maxUnlockedStars)
            sb.Append(" (Locked)");

        sb.Append('\n').Append("Enemy HP +").Append(modifiers.EnemyHealthPercentIncrease).Append('%');
        sb.Append('\n').Append("Enemy Damage +").Append(modifiers.EnemyDamagePercentIncrease).Append('%');
        sb.Append('\n').Append("Misfortune +").Append(Mathf.RoundToInt(modifiers.misfortuneAdd));
        sb.Append('\n').Append("Special Gauge Gain -").Append(modifiers.SpecialGaugePercentDecrease).Append('%');
        sb.Append('\n').Append("EXP Gain +").Append(modifiers.ExpPercentIncrease).Append('%');
        sb.Append('\n').Append("Score Gain +").Append(modifiers.ScorePercentIncrease).Append('%');

        if (stars > maxUnlockedStars)
            sb.Append('\n').Append(GetUnlockInstruction(stars));

        return sb.ToString();
    }

    public static string GetAchievementId(int stars)
    {
        stars = ClampStars(stars);

        switch (stars)
        {
            case 1: return AchievementSystem.Ach.BeatStar1;
            case 2: return AchievementSystem.Ach.BeatStar2;
            case 3: return AchievementSystem.Ach.BeatStar3;
            case 4: return AchievementSystem.Ach.BeatStar4;
            case 5: return AchievementSystem.Ach.BeatStar5;
            default: return string.Empty;
        }
    }

    public static string FormatStars(int stars)
    {
        stars = ClampStars(stars);
        return stars == 1 ? "1 Star" : $"{stars} Stars";
    }
}
