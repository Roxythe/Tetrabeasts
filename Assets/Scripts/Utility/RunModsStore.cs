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
    public static float SpecialDrainMult = 1f;
    public static float SpecialBlockChanceAdd = 0f;

    public static float PieceGravityMult = 1f;

    public static float MonsterDamageMult = 1f;
    public static float MonsterSpecialGainMult = 1f;
    public static float MonsterMaxHpMult = 1f;

    public static void ResetAll()
    {
        Buffs.Clear();
        Debuffs.Clear();

        EnemyAttackIntervalMult = 1f;
        EnemyProjectileDamageMult = 1f;
        EnemyProjectileSpeedMult = 1f;

        SpecialGainMult = 1f;
        SpecialDrainMult = 1f;
        SpecialBlockChanceAdd = 0f;

        PieceGravityMult = 1f;

        MonsterDamageMult = 1f;
        MonsterSpecialGainMult = 1f;
        MonsterMaxHpMult = 1f;
    }
}