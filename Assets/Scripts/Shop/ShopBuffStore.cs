using UnityEngine;

public static class ShopBuffStore
{
    private const string Prefix = "ShopBuff_";

    public static ShopBuffType[] AllTypes => (ShopBuffType[])System.Enum.GetValues(typeof(ShopBuffType));

    public static int GetLevel(ShopBuffType type)
        => PlayerPrefs.GetInt(Prefix + type, 0);

    public static void SetLevel(ShopBuffType type, int level)
    {
        PlayerPrefs.SetInt(Prefix + type, Mathf.Max(0, level));
        PlayerPrefsSaveScheduler.QueueSave();
    }

    public static void AddLevel(ShopBuffType type, int amount = 1)
    {
        SetLevel(type, GetLevel(type) + amount);
    }

    public static void Increment(ShopBuffType type)
    {
        PlayerPrefs.SetInt(Prefix + type, GetLevel(type) + 1);
        PlayerPrefsSaveScheduler.QueueSave();
    }

    public static int GetNextCost(ShopBuffType type)
    {
        int nextLevel = GetLevel(type) + 1;

        return GetCostForLevel(type, nextLevel);
    }

    public static int GetCostForLevel(ShopBuffType type, int level)
    {
        if (level <= 0)
            return 0;

        int cost = GetBaseCost(type);
        for (int i = 2; i <= level; i++)
            cost = ScaleCost(cost);

        return cost;
    }

    private static int ScaleCost(int currentCost)
    {
        double scaled = System.Math.Round(
            Mathf.Max(1, currentCost) * 1.5d,
            System.MidpointRounding.AwayFromZero);

        if (scaled >= int.MaxValue)
            return int.MaxValue;

        return Mathf.Max(1, (int)scaled);
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

    // --- Refund / Reset ---
    public static int GetTotalSpent(ShopBuffType type)
    {
        int level = GetLevel(type);
        return GetTotalSpentForLevel(type, level);
    }

    public static int GetTotalSpentForLevel(ShopBuffType type, int level)
    {
        level = Mathf.Max(0, level);

        long total = 0;
        int cost = GetBaseCost(type);
        for (int i = 1; i <= level; i++)
        {
            if (i > 1)
                cost = ScaleCost(cost);

            total += cost;
            if (total > int.MaxValue) return int.MaxValue;
        }

        return (int)total;
    }

    public static int GetTotalRefundAll()
    {
        long total = 0;
        foreach (var t in AllTypes)
            total += GetTotalSpent(t);
        if (total > int.MaxValue) return int.MaxValue;
        return (int)total;
    }

    public static void ResetAllToZero()
    {
        foreach (var t in AllTypes)
            SetLevel(t, 0);
    }
}
