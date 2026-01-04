using UnityEngine;

public static class UnlockStore
{
    static string MonsterKey(MonsterData md) => $"unlock_mon_{md.name}";
    static string CharacterKey(PlayerCharacterData cd) => $"unlock_char_{cd.name}";

    public static bool IsUnlocked(MonsterData md)
    {
        if (md == null) return false;
        if (!md.startsLocked) return true; // always available if not configured as locked
        return PlayerPrefs.GetInt(MonsterKey(md), 0) == 1;
    }

    public static bool IsUnlocked(PlayerCharacterData cd)
    {
        if (cd == null) return false;
        if (!cd.startsLocked) return true;
        return PlayerPrefs.GetInt(CharacterKey(cd), 0) == 1;
    }

    public static void Unlock(MonsterData md)
    {
        if (md == null) return;
        PlayerPrefs.SetInt(MonsterKey(md), 1);
        PlayerPrefs.Save();
    }

    public static void Unlock(PlayerCharacterData cd)
    {
        if (cd == null) return;
        PlayerPrefs.SetInt(CharacterKey(cd), 1);
        PlayerPrefs.Save();
    }
}
