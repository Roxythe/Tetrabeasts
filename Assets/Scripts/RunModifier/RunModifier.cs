using UnityEngine;

public enum RunModStat
{
    // Currency
    CurrencyPerRoundWin,          // int-like (we'll round)

    // Enemy
    EnemyAttackIntervalMult,
    EnemyProjectileDamageMult,
    EnemyProjectileSpeedMult,

    // Special
    SpecialGainMult,
    SpecialDrainMult,

    // Piece / Specials
    SpecialBlockChanceAdd,
    PieceGravityMult,

    // Monsters
    MonsterDamageMult,
    MonsterSpecialGainMult,
    MonsterMaxHpMult,
}

public enum RunModOp { Add, Multiply }

public enum RunModRarity { Common, Uncommon, Rare, Epic, Legendary }

[CreateAssetMenu(menuName = "Run Mods/Generic")]
public class RunModifier : RunModifierSO
{
    [Header("Classification")]
    public bool isBuff = true;                 // helps you sort pools if you want
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
            // ---------------- Currency ----------------
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

            // ---------------- Special ----------------
            case RunModStat.SpecialGainMult:
                ApplyFloat(ref gc.specialGainMult);
                break;

            case RunModStat.SpecialDrainMult:
                ApplyFloat(ref gc.specialDrainMult);
                break;

            // ---------------- Piece / specials ----------------
            case RunModStat.SpecialBlockChanceAdd:
                // This one should almost always be Add
                if (op == RunModOp.Add) gc.specialBlockChanceAdd += value;
                else gc.specialBlockChanceAdd *= value;
                break;

            case RunModStat.PieceGravityMult:
                ApplyFloat(ref gc.pieceGravityMult);
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
        }
    }

    void ApplyFloat(ref float target)
    {
        if (op == RunModOp.Add) target += value;
        else target *= value;
    }
}
