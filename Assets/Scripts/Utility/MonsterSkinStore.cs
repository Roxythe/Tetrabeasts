using UnityEngine;

public static class MonsterSkinStore
{
    static string SelKey(MonsterData md) => $"skin_sel_{md.name}";
    static string LastValidKey(MonsterData md) => $"skin_last_{md.name}";
    static string UnlockKey(MonsterData md, int idx) => $"skin_unl_{md.name}_{idx}";

    public static bool IsUnlocked(MonsterData md, int idx)
    {
        if (!md) return false;
        if (idx <= 0) return true; // default always unlocked
        return PlayerPrefs.GetInt(UnlockKey(md, idx), 0) == 1;
    }

    public static void Unlock(MonsterData md, int idx)
    {
        if (!md || idx <= 0) return;
        PlayerPrefs.SetInt(UnlockKey(md, idx), 1);
        PlayerPrefs.Save();
    }

    // This is the "committed" selection (what the monster actually uses)
    public static int GetSelected(MonsterData md)
    {
        if (!md) return 0;
        return Mathf.Clamp(PlayerPrefs.GetInt(SelKey(md), 0), 0, 4);
    }

    public static int GetLastValid(MonsterData md)
    {
        if (!md) return 0;
        return Mathf.Clamp(PlayerPrefs.GetInt(LastValidKey(md), 0), 0, 4);
    }

    public static void SetSelectedAndLastValid(MonsterData md, int idx)
    {
        if (!md) return;
        idx = Mathf.Clamp(idx, 0, 4);
        PlayerPrefs.SetInt(SelKey(md), idx);
        PlayerPrefs.SetInt(LastValidKey(md), idx);
        PlayerPrefs.Save();
    }

    // Safety: if selected is locked, fallback to last valid, then default (0)
    public static int GetValidSelected(MonsterData md)
    {
        if (!md) return 0;

        int sel = GetSelected(md);
        if (IsUnlocked(md, sel)) return sel;

        int last = GetLastValid(md);
        if (IsUnlocked(md, last)) return last;

        return 0;
    }

    public static int GetCost(MonsterData md, int idx)
    {
        if (!md || idx <= 0) return 0;
        if (md.skinCosts != null && idx < md.skinCosts.Length) return Mathf.Max(0, md.skinCosts[idx]);
        return 0;
    }

    public static Sprite GetPortrait(MonsterData md, int idx)
    {
        if (!md) return null;
        idx = Mathf.Clamp(idx, 0, 4);

        if (idx == 0)
            return (md.skinPortraits != null && md.skinPortraits.Length > 0 && md.skinPortraits[0])
                ? md.skinPortraits[0]
                : md.portrait;

        if (md.skinPortraits != null && idx < md.skinPortraits.Length && md.skinPortraits[idx])
            return md.skinPortraits[idx];

        return md.portrait;
    }

    public static Sprite GetAttackSprite(MonsterData md)
    {
        if (!md) return null;

        int idx = GetValidSelected(md);

        if (md.skinAttackSprites != null && idx < md.skinAttackSprites.Length && md.skinAttackSprites[idx])
            return md.skinAttackSprites[idx];

        return md.attackSprite ? md.attackSprite : md.portrait;
    }

    public static Sprite GetAttackAltSprite(MonsterData md)
    {
        if (!md) return null;

        int idx = GetValidSelected(md);

        if (md.skinAttackSpritesAlt != null && idx < md.skinAttackSpritesAlt.Length && md.skinAttackSpritesAlt[idx])
            return md.skinAttackSpritesAlt[idx];

        return md.attackSpriteAlt;
    }

}
