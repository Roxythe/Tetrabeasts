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
    SpecialGainMult,
    SpecialDrainMult,

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
                if (op == RunModOp.Add) gc.lineClearCurrencyChanceAdd += value;
                else gc.lineClearCurrencyChanceAdd *= value;
                break;

            case RunModStat.LineClearCurrencyAmountMult:
                ApplyFloat(ref gc.lineClearCurrencyAmountMult);
                break;

            // ---------------- Currency Per Round ----------------
            case RunModStat.CurrencyPerRoundWin:
                if (op == RunModOp.Add) gc.currencyPerRoundWin += Mathf.RoundToInt(value);
                else gc.currencyPerRoundWin = Mathf.RoundToInt(gc.currencyPerRoundWin * value);
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
            case RunModStat.SpecialGainMult:
                ApplyFloat(ref gc.specialGainMult);
                break;

            case RunModStat.SpecialDrainMult:
                ApplyFloat(ref gc.specialDrainMult);
                break;

            // ---------------- Piece / specials ----------------
            case RunModStat.SpecialBlockChanceAdd:
                if (op == RunModOp.Add) gc.specialBlockChanceAdd += value;
                else gc.specialBlockChanceAdd *= value;
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

            // ---------------- Rarity Chance ----------------
            case RunModStat.LuckAdd:
                // Typically Add only. Multiply supported for scaling mods later
                if (op == RunModOp.Add) gc.luck += value;
                else gc.luck *= value;
                break;

            case RunModStat.MisfortuneAdd:
                if (op == RunModOp.Add) gc.misfortune += value;
                else gc.misfortune *= value;
                break;

            // ---------------- Healing ----------------
            case RunModStat.HealPowerMult:
                ApplyFloat(ref gc.healPowerMult);
                break;

            case RunModStat.HealRangeAdd:
                if (op == RunModOp.Add) gc.healRangeAdd += Mathf.RoundToInt(value);
                else gc.healRangeAdd = Mathf.RoundToInt(gc.healRangeAdd * value);
                break;

            // ---------------- UI / Hints ----------------
            case RunModStat.DisableNextPreview:
                if (op == RunModOp.Add) gc.disableNextPreview = (value >= 0.5f) || gc.disableNextPreview;
                else gc.disableNextPreview = (value >= 0.5f);
                break;

            case RunModStat.DisableLandingHint:
                if (op == RunModOp.Add) gc.disableLandingHint = (value >= 0.5f) || gc.disableLandingHint;
                else gc.disableLandingHint = (value >= 0.5f);
                break;

        }
    }

    void ApplyFloat(ref float target)
    {
        if (op == RunModOp.Add) target += value;
        else target *= value;
    }
}
