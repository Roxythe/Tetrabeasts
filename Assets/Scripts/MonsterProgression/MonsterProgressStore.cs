using UnityEngine;

public static class MonsterProgressStore
{
    private const int XpPerLevel = 100;
    private const int MaxLevel = 100;

    private static string KeyTotalXp(string monsterName) => $"MonsterXP_Total_{monsterName}";

    public static float GetPermanentTotalXp(string monsterName)
    {
        if (string.IsNullOrEmpty(monsterName)) return 0f;
        return Mathf.Max(0f, PlayerPrefs.GetFloat(KeyTotalXp(monsterName), 0f));
    }

    public static void SetPermanentTotalXp(string monsterName, float totalXp)
    {
        if (string.IsNullOrEmpty(monsterName)) return;
        PlayerPrefs.SetFloat(KeyTotalXp(monsterName), Mathf.Max(0f, totalXp));
        PlayerPrefs.Save();
    }

    public static int GetPermanentLevel(string monsterName)
    {
        float total = GetPermanentTotalXp(monsterName);
        int level = Mathf.FloorToInt(total / XpPerLevel) + 1;
        return Mathf.Clamp(level, 1, MaxLevel);
    }

    public static float GetPermanentXpIntoLevel(string monsterName)
    {
        float total = GetPermanentTotalXp(monsterName);
        int level = GetPermanentLevel(monsterName);
        float baseForLevel = (level - 1) * XpPerLevel;
        float into = total - baseForLevel;
        return Mathf.Clamp(into, 0f, XpPerLevel - 0.0001f);
    }

    public static void AddPermanentXp(string monsterName, float addXp)
    {
        if (string.IsNullOrEmpty(monsterName)) return;
        if (addXp <= 0f) return;

        float total = GetPermanentTotalXp(monsterName);
        total += addXp;

        // Clamp to max level cap
        float maxTotal = (MaxLevel - 1) * XpPerLevel + (XpPerLevel - 0.0001f);
        total = Mathf.Min(total, maxTotal);

        SetPermanentTotalXp(monsterName, total);
    }

    public static void ClearAll()
    {
        var roster = Resources.FindObjectsOfTypeAll<MonsterData>();
        if (roster != null)
        {
            foreach (var md in roster)
            {
                if (!md) continue;
                if (string.IsNullOrEmpty(md.monsterName)) continue;
                PlayerPrefs.DeleteKey(KeyTotalXp(md.monsterName));
            }
        }

        PlayerPrefs.Save();
    }
}