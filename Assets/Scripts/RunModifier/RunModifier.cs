using UnityEngine;

public enum RunModStat
{
    // Currency
    CurrencyPerRoundWin,

    // Enemy
    EnemyAttackIntervalMult,
    EnemyProjectileDamageMult,
    EnemyProjectileSpeedMult,
    EnemyCastleHpMult,

    // Special
    SpecialGaugeGainPerSecond,
    SpecialGaugeDrainPerSecond,

    // Piece / Specials
    SpecialBlockChanceAdd,
    PieceGravityMult, // Base gravity
    FallRampRateMult, // How much faster gravity gets over time

    // Monsters
    MonsterDamageMult,
    MonsterSpecialGainMult,
    MonsterMaxHpMult,

    // Mod Rarity chance increase
    LuckAdd,
    MisfortuneAdd,

    // Healing
    HealPowerMult,
    HealRangeAdd,

    // UI / Hints
    DisableNextPreview,
    DisableLandingHint,

    // Currency drops from cleared lines
    LineClearCurrencyChanceAdd,  // +0.05 = +5%
    LineClearCurrencyAmountMult, // 1.25 = +25%

    // Stone obstacle run modifier drops
    StoneBuffDropChanceAdd,
    StoneObstacleDropsDebuffsOnly,

    // Unit reserves
    ReserveUnitsRestoredOnWinAdd,
    MaxReserveUnitsAdd,
    DisableRoundWinReserveRestore,
}

public enum RunModOp { Add, Multiply }

public enum RunModRarity { Common, Uncommon, Rare, Epic, Legendary }

[CreateAssetMenu(menuName = "Run Mods/Generic")]
public class RunModifier : RunModifierSO
{
    [Header("Classification")]
    public bool isBuff = true;
    public RunModRarity rarity = RunModRarity.Common;

    [Header("Effect")]
    public RunModStat stat;
    public RunModOp op = RunModOp.Multiply;

    [Tooltip("For Multiply: 1.10 = +10%. For Add: 0.10 = +10% chance if the stat is a chance add.")]
    public float value = 1f;

    public override void Apply(GameController gc)
    {
        switch (stat)
        {
            // ---------------- Currency Drops ----------------
            case RunModStat.LineClearCurrencyChanceAdd:
                ApplyFloat(ref gc.lineClearCurrencyChanceAdd);
                break;

            case RunModStat.LineClearCurrencyAmountMult:
                ApplyFloat(ref gc.lineClearCurrencyAmountMult);
                break;

            // ---------------- Stone obstacle drops ----------------
            case RunModStat.StoneBuffDropChanceAdd:
                ApplyFloat(ref gc.stoneBuffDropChanceAdd);
                break;

            case RunModStat.StoneObstacleDropsDebuffsOnly:
                gc.stoneObstacleDropsDebuffsOnly = ProjectFlag(gc.stoneObstacleDropsDebuffsOnly);
                break;

            // ---------------- Currency Per Round ----------------
            case RunModStat.CurrencyPerRoundWin:
                gc.currencyPerRoundWin = ProjectInteger(gc.currencyPerRoundWin);
                break;

            // ---------------- Enemy ----------------
            case RunModStat.EnemyAttackIntervalMult:
                ApplyFloat(ref gc.enemyAttackIntervalMult);
                break;

            case RunModStat.EnemyProjectileDamageMult:
                ApplyFloat(ref gc.enemyProjectileDamageMult);
                break;

            case RunModStat.EnemyProjectileSpeedMult:
                ApplyFloat(ref gc.enemyProjectileSpeedMult);
                break;

            case RunModStat.EnemyCastleHpMult:
                ApplyFloat(ref gc.enemyCastleHpMult);
                break;

            // ---------------- Special ----------------
            case RunModStat.SpecialGaugeGainPerSecond:
                ApplyFloat(ref gc.specialGaugeGainPerSecond);
                break;

            case RunModStat.SpecialGaugeDrainPerSecond:
                ApplyFloat(ref gc.specialGaugeDrainPerSecond);
                break;

            // ---------------- Piece / specials ----------------
            case RunModStat.SpecialBlockChanceAdd:
                ApplyFloat(ref gc.specialBlockChanceAdd);
                break;

            case RunModStat.PieceGravityMult:
                ApplyFloat(ref gc.pieceGravityMult);
                break;

            case RunModStat.FallRampRateMult:
                ApplyFloat(ref gc.fallRampRateMult);
                break;

            // ---------------- Monsters ----------------
            case RunModStat.MonsterDamageMult:
                ApplyFloat(ref gc.monsterDamageMult);
                break;

            case RunModStat.MonsterSpecialGainMult:
                ApplyFloat(ref gc.monsterSpecialGainMult);
                break;

            case RunModStat.MonsterMaxHpMult:
                ApplyFloat(ref gc.monsterMaxHpMult);
                break;

            // ---------------- Unit reserves ----------------
            case RunModStat.ReserveUnitsRestoredOnWinAdd:
                gc.reserveUnitsRestoredOnWinAdd = ProjectInteger(gc.reserveUnitsRestoredOnWinAdd);
                break;

            case RunModStat.MaxReserveUnitsAdd:
                if (op == RunModOp.Add) gc.AddMaxReserveUnitsModifier(Mathf.RoundToInt(value));
                else gc.MultiplyMaxReserveUnitsModifier(value);
                break;

            case RunModStat.DisableRoundWinReserveRestore:
                gc.disableRoundWinReserveRestore = ProjectFlag(gc.disableRoundWinReserveRestore);
                break;

            // ---------------- Rarity Chance ----------------
            case RunModStat.LuckAdd:
                // Typically Add only. Multiply supported for scaling mods later
                ApplyFloat(ref gc.luck);
                break;

            case RunModStat.MisfortuneAdd:
                ApplyFloat(ref gc.misfortune);
                break;

            // ---------------- Healing ----------------
            case RunModStat.HealPowerMult:
                ApplyFloat(ref gc.healPowerMult);
                break;

            case RunModStat.HealRangeAdd:
                gc.healRangeAdd = ProjectInteger(gc.healRangeAdd);
                break;

            // ---------------- UI / Hints ----------------
            case RunModStat.DisableNextPreview:
                gc.disableNextPreview = ProjectFlag(gc.disableNextPreview);
                break;

            case RunModStat.DisableLandingHint:
                gc.disableLandingHint = ProjectFlag(gc.disableLandingHint);
                break;

        }
    }

    public float Project(float current) => op == RunModOp.Add ? current + value : current * value;
    public int ProjectInteger(int current) => op == RunModOp.Add
        ? current + Mathf.RoundToInt(value) : Mathf.RoundToInt(current * value);
    public bool ProjectFlag(bool current) => value >= 0.5f || (op == RunModOp.Add && current);

    void ApplyFloat(ref float target) => target = Project(target);
}
