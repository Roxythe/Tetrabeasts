using UnityEngine;

public static class MonsterSkinStore
{
    public const int MaxSkinSlots = 7;
    public const int MaxSkinIndex = MaxSkinSlots - 1;
    public const float PortraitAnimationFramesPerSecond = 4f;
    const string FinalWinMonsterElitePrefix = "lt_final_win_monster_elite_";

    static string SelKey(MonsterData md) => $"skin_sel_{md.name}";
    static string LastValidKey(MonsterData md) => $"skin_last_{md.name}";
    static string UnlockKey(MonsterData md, int idx) => $"skin_unl_{md.name}_{idx}";

    static int ClampSkinIndex(int idx) => Mathf.Clamp(idx, 0, MaxSkinIndex);
    public static string GetEliteCompletionKey(string monsterAssetName) => FinalWinMonsterElitePrefix + monsterAssetName;
    public static bool IsEliteSkin(MonsterData md, int idx) => idx > 0 && idx == GetEliteSkinIndex(md);

    public static int GetEliteSkinIndex(MonsterData md)
    {
        if (!md || md.skinPortraits == null)
            return -1;

        int lastIndex = Mathf.Min(MaxSkinIndex, md.skinPortraits.Length - 1);
        for (int i = lastIndex; i > 0; i--)
        {
            if (IsSkinAvailable(md, i))
                return i;
        }

        return -1;
    }

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
        if (IsEliteSkin(md, idx) && HasEliteCompletionUnlock(md)) return true;
        return PlayerPrefs.GetInt(UnlockKey(md, idx), 0) == 1;
    }

    public static void Unlock(MonsterData md, int idx)
    {
        if (!md) return;
        if (idx < 0 || idx > MaxSkinIndex) return;
        idx = ClampSkinIndex(idx);
        if (IsEliteSkin(md, idx)) return;
        if (idx <= 0 || !IsSkinAvailable(md, idx)) return;
        PlayerPrefs.SetInt(UnlockKey(md, idx), 1);
        PlayerPrefsSaveScheduler.QueueSave();
    }

    public static bool UnlockEliteForSuccessfulRun(MonsterData md)
    {
        int eliteIndex = GetEliteSkinIndex(md);
        if (!md || eliteIndex < 0 || !IsSkinAvailable(md, eliteIndex))
            return false;

        bool alreadyUnlocked = IsUnlocked(md, eliteIndex);

        PlayerPrefs.SetInt(UnlockKey(md, eliteIndex), 1);

        if (PlayerProgress.I != null)
        {
            string completionKey = GetEliteCompletionKey(md.name);
            if (PlayerProgress.I.GetLifetimeInt(completionKey) == 0)
                PlayerProgress.I.AddLifetimeInt(completionKey, 1);
        }

        PlayerPrefsSaveScheduler.QueueSave();
        return !alreadyUnlocked;
    }

    public static bool IsPurchasableSkin(MonsterData md, int idx)
    {
        return md && idx > 0 && idx <= MaxSkinIndex && !IsEliteSkin(md, idx) && IsSkinAvailable(md, idx);
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
        PlayerPrefsSaveScheduler.QueueSave();
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
        if (idx <= 0 || !IsPurchasableSkin(md, idx)) return 0;
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

    static bool HasEliteCompletionUnlock(MonsterData md)
    {
        return md &&
               PlayerProgress.I != null &&
               PlayerProgress.I.GetLifetimeInt(GetEliteCompletionKey(md.name)) > 0;
    }
}
