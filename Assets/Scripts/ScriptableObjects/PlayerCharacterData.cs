using TMPro;
using UnityEngine;

public enum SpecialAbility
{
    ClearBottomRows,    // Clear bottom x rows of tiles
    RestoreAllToFull,   // Revive (hp>0) + heal to max for all inactive tiles
    GlobalImmunity      // Tiles take no damage for a duration, with gold border + pulse
}

[CreateAssetMenu(menuName = "Run/Player Character", fileName = "NewPlayerCharacter")]
public class PlayerCharacterData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Alyx";
    public Sprite portrait;
    public Sprite defaultBorder;
    public string specialDescription;

    [Header("Unlock")]
    public bool startsLocked = false;
    public int unlockCost = 10;

    [Header("Special")]
    public SpecialAbility ability = SpecialAbility.ClearBottomRows;
    [Range(1, 6)] public int clearRows = 3;     // for ClearBottomRows
    public float cooldownSeconds = 0f;    
    public float specialGaugeMax = 100f;

    [Header("Restore All To Full (Revive + Heal)")]
    public Sprite reviveAllVFXSprite;
    public AudioClip sfxRestoreAll;  

    [Header("Global Immunity")]
    [Min(0.25f)] public float immunityDuration = 5f;
    public AudioClip sfxImmunityOn;  
    public AudioClip sfxImmunityWarn;  
    public AudioClip sfxImmunityOff;    
}
