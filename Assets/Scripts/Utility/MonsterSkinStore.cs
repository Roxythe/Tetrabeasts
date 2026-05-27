using UnityEngine;

public static class MonsterSkinStore
{
    public const int MaxSkinSlots = 6;
    public const int MaxSkinIndex = MaxSkinSlots - 1;
    public const float PortraitAnimationFramesPerSecond = 4f;

    static string SelKey(MonsterData md) => $"skin_sel_{md.name}";
    static string LastValidKey(MonsterData md) => $"skin_last_{md.name}";
    static string UnlockKey(MonsterData md, int idx) => $"skin_unl_{md.name}_{idx}";

    static int ClampSkinIndex(int idx) => Mathf.Clamp(idx, 0, MaxSkinIndex);

    public static bool IsSkinAvailable(MonsterData md, int idx)
    {
        if (!md) return false;

        if (idx < 0 || idx > MaxSkinIndex)
            return false;

        if (idx == 0) return true;

        return md.skinPortraits != null
            && idx < md.skinPortraits.Length
            && md.skinPortraits[idx];
    }

    public static bool IsUnlocked(MonsterData md, int idx)
    {
        if (!md) return false;
        if (idx < 0 || idx > MaxSkinIndex) return false;
        idx = ClampSkinIndex(idx);
        if (!IsSkinAvailable(md, idx)) return false;
        if (idx <= 0) return true; // default always unlocked
        return PlayerPrefs.GetInt(UnlockKey(md, idx), 0) == 1;
    }

    public static void Unlock(MonsterData md, int idx)
    {
        if (!md) return;
        if (idx < 0 || idx > MaxSkinIndex) return;
        idx = ClampSkinIndex(idx);
        if (idx <= 0 || !IsSkinAvailable(md, idx)) return;
        PlayerPrefs.SetInt(UnlockKey(md, idx), 1);
        PlayerPrefs.Save();
        SteamCloudSaveService.QueueUpload();
    }

    // This is the "committed" selection (what the monster actually uses)
    public static int GetSelected(MonsterData md)
    {
        if (!md) return 0;
        return ClampSkinIndex(PlayerPrefs.GetInt(SelKey(md), 0));
    }

    public static int GetLastValid(MonsterData md)
    {
        if (!md) return 0;
        return ClampSkinIndex(PlayerPrefs.GetInt(LastValidKey(md), 0));
    }

    public static void SetSelectedAndLastValid(MonsterData md, int idx)
    {
        if (!md) return;
        idx = ClampSkinIndex(idx);
        if (!IsSkinAvailable(md, idx))
            idx = 0;

        PlayerPrefs.SetInt(SelKey(md), idx);
        PlayerPrefs.SetInt(LastValidKey(md), idx);
        PlayerPrefs.Save();
        SteamCloudSaveService.QueueUpload();
    }

    // Safety: if selected is locked, fallback to last valid, then default (0)
    public static int GetValidSelected(MonsterData md)
    {
        if (!md) return 0;

        int sel = GetSelected(md);
        if (IsSkinAvailable(md, sel) && IsUnlocked(md, sel)) return sel;

        int last = GetLastValid(md);
        if (IsSkinAvailable(md, last) && IsUnlocked(md, last)) return last;

        return 0;
    }

    public static int GetCost(MonsterData md, int idx)
    {
        if (!md) return 0;
        if (idx < 0 || idx > MaxSkinIndex) return 0;
        idx = ClampSkinIndex(idx);
        if (idx <= 0 || !IsSkinAvailable(md, idx)) return 0;
        if (md.skinCosts != null && idx < md.skinCosts.Length) return Mathf.Max(0, md.skinCosts[idx]);
        return 0;
    }

    public static Sprite GetPortrait(MonsterData md, int idx)
    {
        if (!md) return null;
        idx = ClampSkinIndex(idx);

        if (idx == 0)
            return (md.skinPortraits != null && md.skinPortraits.Length > 0 && md.skinPortraits[0])
                ? md.skinPortraits[0]
                : md.portrait;

        if (md.skinPortraits != null && idx < md.skinPortraits.Length && md.skinPortraits[idx])
            return md.skinPortraits[idx];

        return md.portrait;
    }

    public static Sprite GetAltPortrait(MonsterData md, int idx)
    {
        if (!md) return null;
        idx = ClampSkinIndex(idx);

        if (md.skinAltPortraits == null || idx >= md.skinAltPortraits.Length)
            return null;

        if (!IsSkinAvailable(md, idx))
            return null;

        return md.skinAltPortraits[idx];
    }

    public static Sprite GetPreviewPortrait(MonsterData md, int idx)
    {
        var alt = GetAltPortrait(md, idx);
        return alt ? alt : GetPortrait(md, idx);
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
