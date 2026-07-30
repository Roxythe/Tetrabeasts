using System.Collections.Generic;

public static class RunModsStore
{
    public static readonly List<RunModifierSO> Buffs = new();
    public static readonly List<RunModifierSO> Debuffs = new();

    // runtime multipliers/additives (defaults)
    public static float EnemyAttackIntervalMult = 1f;
    public static float EnemyProjectileDamageMult = 1f;
    public static float EnemyProjectileSpeedMult = 1f;

    public static float SpecialGainMult = 1f;
    public static float SpecialGaugeGainPerSecond = 0f;
    public static float SpecialGaugeDrainPerSecond = 0f;
    public static float SpecialBlockChanceAdd = 0f;

    public static float PieceGravityMult = 1f;
    public static float FallRampRateMult = 1f;

    public static float MonsterDamageMult = 1f;
    public static float MonsterSpecialGainMult = 1f;
    public static float MonsterMaxHpMult = 1f;

    public static float HealPowerMult = 1f;
    public static int HealRangeAdd = 0;

    public static bool DisableNextPreview = false;
    public static bool DisableLandingHint = false;

    public static float LineClearCurrencyChanceAdd = 0f;
    public static float LineClearCurrencyAmountMult = 1f;

    public static float EnemyCastleHpMult = 1f;

    public static float Luck = 0f;
    public static float Misfortune = 0f;

    public static float StoneBuffDropChanceAdd = 0f;
    public static bool StoneObstacleDropsDebuffsOnly = false;

    public static int ReserveUnitsRestoredOnWinAdd = 0;
    public static int MaxReserveUnitsAdd = 0;
    public static bool DisableRoundWinReserveRestore = false;


    public static void ResetAll()
    {
        Buffs.Clear();
        Debuffs.Clear();

        EnemyAttackIntervalMult = 1f;
        EnemyProjectileDamageMult = 1f;
        EnemyProjectileSpeedMult = 1f;

        SpecialGainMult = 1f;
        SpecialGaugeGainPerSecond = 0f;
        SpecialGaugeDrainPerSecond = 0f;
        SpecialBlockChanceAdd = 0f;

        PieceGravityMult = 1f;
        FallRampRateMult = 1f;

        MonsterDamageMult = 1f;
        MonsterSpecialGainMult = 1f;
        MonsterMaxHpMult = 1f;

        HealPowerMult = 1f;
        HealRangeAdd = 0;

        DisableNextPreview = false;
        DisableLandingHint = false;

        LineClearCurrencyChanceAdd = 0f;
        LineClearCurrencyAmountMult = 1f;

        EnemyCastleHpMult = 1f;

        Luck = 0f;
        Misfortune = 0f;

        StoneBuffDropChanceAdd = 0f;
        StoneObstacleDropsDebuffsOnly = false;

        ReserveUnitsRestoredOnWinAdd = 0;
        MaxReserveUnitsAdd = 0;
        DisableRoundWinReserveRestore = false;
    }
}
