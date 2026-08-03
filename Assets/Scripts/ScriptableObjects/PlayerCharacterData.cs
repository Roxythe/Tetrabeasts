using TMPro;
using UnityEngine;

public enum SpecialAbility
{
    ClearBottomRows,    // Clear bottom x rows of tiles
    RestoreAllToFull,   // Revive (hp>0) and heal to max for all inactive tiles
    GlobalImmunity,     // Tiles take no damage for a duration, with gold border + pulse
    ReducedGravity,     // Temporarily reduce gravity by 1/3 for a duration
    DoubleStats,        // Temporarily double all tile stats for a duration
    ShakeNQuake,        // Settle every column like an earthquake special block
    BombsAway           // Drop multiple bomb special blocks at once
}

[CreateAssetMenu(menuName = "Run/Player Character", fileName = "NewPlayerCharacter")]
public class PlayerCharacterData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Alyx";
    public Sprite portrait;
    public Sprite defaultBorder;
    public RuntimeAnimatorController animatedBorderController;
    public string specialAbilityName = "Ability Name";
    public string specialDescription;

    [Header("Unlock")]
    public bool startsLocked = false;
    public int unlockCost = 10;

    [Header("Special")]
    public SpecialAbility ability = SpecialAbility.ClearBottomRows;
    [Range(1, 6)] public int clearRows = 3;     // for ClearBottomRows
    public float cooldownSeconds = 0f;    
    public float specialGaugeMax = 100f;

    [Header("Special Ability Popup")]
    public GameObject specialAbilityAnimationPrefab;
    public AudioClip specialAbilityAnimationSFX;
    [Range(0f, 1f)] public float specialAbilityAnimationSFXVolume = 0.65f;

    [Header("Restore All To Full (Revive + Heal)")]
    public Sprite reviveAllVFXSprite;
    public AudioClip sfxRestoreAll;  

    [Header("Global Immunity")]
    [Min(0.25f)] public float immunityDuration = 5f;
    public AudioClip sfxImmunityOn;  
    public AudioClip sfxImmunityWarn;  
    public AudioClip sfxImmunityOff;

    [Header("Reduced Gravity")]
    [Min(0.25f)] public float reducedGravityDuration = 10f;
    [Range(0.1f, 2f)] public float reducedGravityMultiplier = 0.333334f; // 1/3 = reduce by 2/3 of toal gravity

    [Header("Double Stats (HP + Attack)")]
    [Min(0.25f)] public float doubleStatsDuration = 10f;
    public AudioClip sfxDoubleStatsOn;
    public AudioClip sfxDoubleStatsOff;
}
