using UnityEngine;

public static class ShopBuffEffects
{
    public static int LuckBonus =>
        ShopBuffStore.GetLevel(ShopBuffType.LuckUp) * 10;

    public static float GoldChanceBonus =>
        ShopBuffStore.GetLevel(ShopBuffType.GoldUp) * 0.02f;

    public static int UnitLivesBonus =>
        ShopBuffStore.GetLevel(ShopBuffType.UnitLivesUp) * 2;

    public static float MonsterAttackBonus =>
        ShopBuffStore.GetLevel(ShopBuffType.AttackUp) * 0.5f;

    public static int MonsterHpBonus =>
        ShopBuffStore.GetLevel(ShopBuffType.HpUp) * 5;

    public static int MonsterHealBonus =>
        ShopBuffStore.GetLevel(ShopBuffType.HealPower) * 2;

    public static float GravityMultiplier =>
        Mathf.Pow(0.92f, ShopBuffStore.GetLevel(ShopBuffType.GravityDown));

    public static float VelocityMultiplier =>
        Mathf.Pow(0.90f, ShopBuffStore.GetLevel(ShopBuffType.VelocityDown));
}
