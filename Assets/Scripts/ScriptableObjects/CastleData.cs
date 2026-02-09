using UnityEngine;

[CreateAssetMenu(menuName = "Tetrabeasts/Castle Data")]
public class CastleData : ScriptableObject
{
    [Header("Display / Identity")]
    public string castleName = "Castle";

    [Tooltip("0 = healthy, 1 = chipped, 2 = cracked, 3 = crumbling")]
    public Sprite[] damageStages = new Sprite[4];

    [Header("Stats")]
    public int maxHP = 100;

    [Header("Projectile Attack")]
    public Sprite projectileSprite;          // arrow sprite, etc.
    [Min(0.1f)] public float projectileInterval = 3f;
    [Min(1f)] public float projectileSpeed = 800f;
    [Min(1)] public int projectileDamage = 1;

    [Header("SFX")]
    public AudioClip sfxProjectileHitTile;
    public AudioClip[] sfxProjectileHitTileClips;
    public AudioClip[] sfxDamageStageClips;
    public AudioClip sfxProjectileFired;
    public AudioClip[] sfxProjectileFiredClips;

    [Header("Boss Level")]
    public bool isBossLevel;
    public Sprite bossOverlaySprite;
    public AudioClip bossBGM;
    [Range(0f, 1f)] public float bossBGMVolume = 0.6f;

    // (optional) non-boss per-level music
    public AudioClip levelBGM;
    [Range(0f, 1f)] public float levelBGMVolume = 0.5f;

    [Header("Obstacle / Trap Overrides (Optional)")]
    [Tooltip("If enabled, this CastleData can override the level's initial obstacle/trap counts.")]
    public bool overrideInitialObstacles = false;

    [Tooltip("Only apply overrides on Level 1 (recommended).")]
    public bool overridesOnlyForLevel1 = true;

    [Tooltip("Set to -1 to use ObstacleManager's normal formula.")]
    public int overrideStoneCount = -1;

    [Tooltip("Set to -1 to use ObstacleManager's normal formula.")]
    public int overridePoisonCount = -1;

    [Tooltip("Set to -1 to use ObstacleManager's normal formula.")]
    public int overrideFireCount = -1;

    [Tooltip("Set to -1 to use ObstacleManager's normal formula.")]
    public int overrideSpikeCount = -1;

    // ================= Boss Abilities =================
    [Header("Boss Abilities - Toggles")]
    public bool bossEnableRowBlast = true;
    public bool bossEnableFullBoardBlast = false;
    public bool bossEnableLightningStrike = true;
    public bool bossEnableSpawnTraps = true;
    public bool bossEnableInvulnerability = true;
    public bool bossEnableGravityBoost = true;

    [Header("Boss Abilities - Global Timing")]
    [Min(0.25f)] public float bossAbilityIntervalMin = 4f;
    [Min(0.25f)] public float bossAbilityIntervalMax = 7f;

    [Header("Boss - Row Blast (Top 3 rows from highest monster)")]
    [Min(0f)] public float bossRowBlastDamage = 2f;

    [Header("Boss - Full Board Blast")]
    [Min(0f)] public float bossFullBoardDamage = 1f;

    [Header("Boss - Lightning Strike")]
    public Sprite bossLightningWarningSprite;     // Indicator that flashes before strike
    [Min(0.25f)] public float bossLightningWarningMin = 3f;
    [Min(0.25f)] public float bossLightningWarningMax = 5f;
    [Min(0.01f)] public float bossLightningInitialDamage = 2f;
    [Min(0.01f)] public float bossLightningTickDamage = 1f;
    [Min(0.05f)] public float bossLightningTickInterval = 0.5f;
    [Min(0.25f)] public float bossLightningHazardDuration = 4f;

    // Boss - Spawn Traps
    public enum BossTrapKind { Spike, Stone, Poison, Fire }
    public BossTrapKind bossTrapKind = BossTrapKind.Spike;

    public enum BossTrapPattern { Single, Line4, Square2x2 }
    public BossTrapPattern bossTrapPattern = BossTrapPattern.Single;

    [Min(1)] public int bossTrapCountPerUse = 1; // How many clusters/singles per cast

    [Header("Boss - Invulnerability")]
    [Min(0.25f)] public float bossInvulnDuration = 3f;
    public Sprite bossInvulnOverlaySprite;     // Optional overlay sprite 
    public AudioClip bossInvulnHitSFX;         // Different hit SFX when invulnerable

    [Header("Boss - Gravity Boost")]
    [Min(0.1f)] public float bossGravityBonusMult = 2f; // "add +2 gravity" => multiplies fall speed by (1+2)=3x if you implement as additive
    [Min(0.25f)] public float bossGravityDuration = 10f;



    public Sprite GetSpriteForHealth(float hpPercent)
    {
        // clamp
        if (hpPercent < 0f) hpPercent = 0f;
        if (hpPercent > 1f) hpPercent = 1f;

        int index;
        if (hpPercent >= 0.76f) index = 0; // 100-76%
        else if (hpPercent >= 0.51f) index = 1; // 75-51%
        else if (hpPercent >= 0.26f) index = 2; // 50-26%
        else index = 3; // 25-0%

        if (damageStages != null && index >= 0 && index < damageStages.Length)
            return damageStages[index];

        // safety fallback
        if (damageStages != null && damageStages.Length > 0)
            return damageStages[0];

        return null;
    }

    public AudioClip PickRandom(AudioClip[] arr, AudioClip fallback = null)
    {
        if (arr != null && arr.Length > 0)
        {
            // Keep retrying until we hit a non-null element, up to a few tries
            for (int k = 0; k < 8; k++)
            {
                var c = arr[Random.Range(0, arr.Length)];
                if (c) return c;
            }
        }
        return fallback;
    }
}
