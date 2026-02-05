using UnityEngine;

public static class ShopBuffStore
{
    private const string Prefix = "ShopBuff_";

    public static int GetLevel(ShopBuffType type)
        => PlayerPrefs.GetInt(Prefix + type, 0);

    public static void SetLevel(ShopBuffType type, int level)
    {
        PlayerPrefs.SetInt(Prefix + type, Mathf.Max(0, level));
        PlayerPrefs.Save();
    }

    public static void AddLevel(ShopBuffType type, int amount = 1)
    {
        SetLevel(type, GetLevel(type) + amount);
    }

    public static void Increment(ShopBuffType type)
    {
        PlayerPrefs.SetInt(Prefix + type, GetLevel(type) + 1);
        PlayerPrefs.Save();
    }

    public static int GetNextCost(ShopBuffType type)
    {
        int nextLevel = GetLevel(type) + 1;
        int baseCost = GetBaseCost(type);

        return baseCost * nextLevel * nextLevel; // Quadratic scaling
    }

    private static int GetBaseCost(ShopBuffType type)
    {
        switch (type)
        {
            case ShopBuffType.AttackUp: return 80;
            case ShopBuffType.HpUp: return 70;
            case ShopBuffType.HealPower: return 60;
            case ShopBuffType.UnitLivesUp: return 120;

            case ShopBuffType.GravityDown: return 50;
            case ShopBuffType.VelocityDown: return 50;

            case ShopBuffType.LuckUp: return 40;
            case ShopBuffType.GoldUp: return 60;
            default: return 50;
        }
    }
}