using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(menuName = "Tetrabeasts/Castle Data")]
public class CastleData : ScriptableObject
{
    [Header("Display / Identity")]
    public string castleName = "Castle";

    [Tooltip("0 = healthy, 1 = chipped, 2 = cracked, 3 = crumbling")]
    public Sprite[] damageStages = new Sprite[4];

    [Tooltip("Optional background sprite shown by the enemy castle UI for this level.")]
    public Sprite levelBackgroundSprite;
    [Tooltip("Optional sprite assigned to BorderFrame_Image for this level's enemy HP gauge. If empty, the scene default is kept.")]
    public Sprite hpGaugeFrameSprite;
    [Tooltip("Optional animated background video shown on the gameplay board for this level. Static sprites remain the fallback.")]
    public VideoClip levelBackgroundVideoClip;
    [Tooltip("Playback speed for this level's animated background video. Set to 0 to use AnimatedBG_VideoPlayer's scene speed.")]
    [Min(0f)] public float levelBackgroundVideoPlaybackSpeed = 0f;
    [Tooltip("If true, the animated background video plays forward, then backward, in a continuous ping-pong loop.")]
    public bool levelBackgroundVideoPingPongLoop = false;

    [Header("Board Presentation")]
    [Tooltip("Optional sprite shown on Board_Background for this level. If empty, the scene default is kept.")]
    public Sprite boardBackgroundSprite;

    [Header("Stats")]
    public int maxHP = 100;

    [Tooltip("If true, this castle can take damage for scoring/stat tracking but its HP never decreases.")]
    public bool infiniteHealth = false;

    [Header("Projectile Attack")]
    public Sprite projectileSprite;
    [Min(0.1f)] public float projectileInterval = 3f;
    [Tooltip("Base speed at the first level's base gravity. Runtime speed scales upward as gravity rises.")]
    [Min(1f)] public float projectileSpeed = 700f;
    [Tooltip("How strongly projectile speed tracks gravity. 0 = no scaling, 0.5 = square-root scaling, 1 = one-to-one scaling.")]
    [Range(0f, 1f)] public float projectileGravitySpeedExponent = 0.5f;
    [Tooltip("Scales the visual size of enemy projectiles fired by this castle.")]
    [Range(0.25f, 3f)] public float projectileVisualScale = 1f;
    [Min(1)] public int projectileDamage = 1;
    [Min(1)] public int baseEnemyProjectilesPerAttack = 1;

    [Header("SFX")]
    public AudioClip sfxProjectileHitTile;
    public AudioClip[] sfxProjectileHitTileClips;
    public AudioClip[] sfxDamageStageClips;
    public AudioClip sfxProjectileFired;
    public AudioClip[] sfxProjectileFiredClips;

    [Header("Boss Level")]
    public bool isBossLevel;
    public Sprite bossOverlaySprite;
    public Sprite bossAttackSprite;
    public Sprite bossDamageTakenSprite;
    public Sprite bossCriticalIdleSprite;
    public Sprite bossCriticalDamageTakenSprite;
    public Sprite bossCriticalAttackSprite;

    [Header("Boss Sprite Animation")]
    [Range(0.05f, 0.9f)] public float bossCriticalHpThreshold = 0.30f;

    [Tooltip("If true, attack sprite shifts right. If false, shifts left.")]
    public bool bossAttackShiftRight = true;
    [Min(0f)] public float bossAttackShiftDistance = 20f;
    [Min(0.01f)] public float bossAttackSpriteSeconds = 0.25f;
    [Min(0.01f)] public float bossDamageSpriteSeconds = 0.18f;

    [Header("Obstacle / Trap Overrides")]
    [Tooltip("If enabled, this CastleData can override the level's initial obstacle/trap counts.")]
    public bool overrideInitialObstacles = false;
    public bool overridesOnlyForLevel1 = true;

    public int overrideStoneCount = -1;
    public int overridePoisonCount = -1;
    public int overrideFireCount = -1;
    public int overrideSpikeCount = -1;
    public int overrideTeleportCount = -1;
    public int overrideZipPadCount = -1;

    [Header("Level Modifiers")]
    [Tooltip("If enabled on a normal level, the game will randomly select a single level modifier before gameplay starts.")]
    public bool enableLevelModifierSelection = false;

    [Tooltip("Shared database containing all level modifiers.")]
    public LevelModifierDatabaseSO levelModifierDatabase;
    public LevelModifierKind[] allowedLevelModifierKinds;

    [Header("Boss Abilities - Toggles")]
    public bool bossEnableRowBlast = true;
    public bool bossEnableFullBoardBlast = false;
    public bool bossEnableLightningStrike = true;
    public bool bossEnableSpawnTraps = true;
    public bool bossEnableInvulnerability = true;
    public bool bossEnableGravityBoost = true;
    public bool bossEnablePylonShield = true;
    public bool bossEnableMagicExplosive = true;
    public bool bossEnableForcedDuplication = true;
    public bool bossEnableSpecialSiphon = true;
    public bool bossEnableTeleport = true;
    public bool bossEnableZipPad = true;

    [Header("Boss Abilities - Global Timing")]
    [Min(0.25f)] public float bossAbilityIntervalMin = 4f;
    [Min(0.25f)] public float bossAbilityIntervalMax = 7f;

    [Header("Boss - Row Blast (Top 3 rows from highest monster)")]
    [Min(0f)] public float bossRowBlastDamage = 2f;

    [Header("Boss - Full Board Blast")]
    [Min(0f)] public float bossFullBoardDamage = 1f;

    [Header("Boss - Lightning Strike")]
    public Sprite bossLightningWarningSprite;
    [Min(1)] public int bossLightningTargetsMin = 1;
    [Min(1)] public int bossLightningTargetsMax = 3;
    [Min(0.25f)] public float bossLightningWarningMin = 3f;
    [Min(0.25f)] public float bossLightningWarningMax = 5f;
    [Min(0.01f)] public float bossLightningInitialDamage = 2f;
    [Min(0.01f)] public float bossLightningTickDamage = 1f;
    [Min(0.05f)] public float bossLightningTickInterval = 0.5f;
    [Min(0.25f)] public float bossLightningHazardDuration = 4f;

    public enum BossTrapKind { Spike, Stone, Poison, Fire, Lightning }
    public BossTrapKind bossTrapKind = BossTrapKind.Spike;

    public enum BossTrapPattern { Single, Line4, Square2x2, Line4_V, Line4_H, Line4_Random }
    public BossTrapPattern bossTrapPattern = BossTrapPattern.Single;

    [System.Serializable]
    public struct BossTrapSpawnOption
    {
        public BossTrapKind kind;
        public BossTrapPattern pattern;
        [Min(1)] public int weight;
    }

    [Header("Boss - Spawn Traps (Option Pool)")]
    public bool useBossTrapOptionPool = false;
    public BossTrapSpawnOption[] bossTrapOptions;

    [Min(1)] public int bossTrapCountPerUse = 4;
    [Min(1)] public int bossTrapCountPerClusterUse = 1;

    public enum BossAbilityKind
    {
        RowBlast,
        FullBoardBlast,
        LightningStrike,
        SpawnTraps,
        Invulnerability,
        GravityBoost,
        PylonShield,
        MagicExplosive,
        ForcedDuplication,
        SpecialSiphon,
        Teleport,
        ZipPad
    }

    [System.Serializable]
    public struct BossAbilityOption
    {
        public BossAbilityKind kind;
        [Min(1)] public int weight;
        public bool allowRepeat;
        [Min(0f)] public float cooldown;
    }

    [Header("Boss - Ability Option Pool")]
    public bool useBossAbilityOptionPool = true;
    public BossAbilityOption[] bossAbilityOptions;

    [Header("Boss - Invulnerability")]
    [Min(0.25f)] public float bossInvulnDuration = 3f;
    public AudioClip bossInvulnHitSFX;

    [Header("Boss - Gravity Boost")]
    [Min(0.1f)] public float bossGravityBonusMult = 2f;
    [Min(0.25f)] public float bossGravityDuration = 10f;

    [Header("Boss - Forced Duplication")]
    [Min(1)] public int bossForcedDuplicationMinPieces = 5;
    [Min(1)] public int bossForcedDuplicationMaxPieces = 9;

    [Header("Boss - Special Siphon")]
    [Range(0f, 1f)] public float bossSpecialSiphonGaugeDrainPercentPerSecond = 0.03f;

    [Header("Boss - Teleport")]
    [Min(1)] public int bossTeleportTargetsMin = 1;
    [Min(1)] public int bossTeleportTargetsMax = 5;
    [Min(0.1f)] public float bossTeleportWarningSeconds = 2f;
    [Min(0)] public int bossTeleportDestinationExcludedTopRows = 4;
    [Min(0)] public int bossTeleportDestinationExcludedBottomRows = 2;

    [Header("Boss - Zip Pad")]
    [Min(0)] public int bossZipPadExcludedTopRows = 4;
    [Min(0)] public int bossZipPadExcludedBottomRows = 2;

    [Header("Boss Ability Warnings")]
    [Min(0f)] public float bossAbilityWarningSeconds = 3f;
    [Min(0.02f)] public float bossWarningToggleInterval = 0.16f;

    public Sprite bossRowBlastWarningSprite;
    public Sprite bossFullBoardWarningSprite;

    [Header("Boss - Pylon Shield")]
    [Min(1)] public int bossPylonCount = 2;
    [Range(0.05f, 1f)] public float bossPylonDamageMult = 0.5f;
    public AudioClip bossPylonReducedHitSFX;

    [Header("Boss - Magic Explosive")]
    [Min(0.1f)] public float bossExplosiveFuseSeconds = 15f;
    [Min(0)] public int bossExplosiveRowClearBonusDamage = 50;
    public Sprite bossExplosiveDetonateVFXSprite;
    public AudioClip bossExplosiveDetonateSFX;

    public Sprite GetSpriteForHealth(float hpPercent)
    {
        if (hpPercent < 0f) hpPercent = 0f;
        if (hpPercent > 1f) hpPercent = 1f;

        int index;
        if (hpPercent >= 0.76f) index = 0;
        else if (hpPercent >= 0.51f) index = 1;
        else if (hpPercent >= 0.26f) index = 2;
        else index = 3;

        if (damageStages != null && index >= 0 && index < damageStages.Length)
            return damageStages[index];

        if (damageStages != null && damageStages.Length > 0)
            return damageStages[0];

        return null;
    }

    public AudioClip PickRandom(AudioClip[] arr, AudioClip fallback = null)
    {
        if (arr != null && arr.Length > 0)
        {
            for (int k = 0; k < 8; k++)
            {
                var c = arr[Random.Range(0, arr.Length)];
                if (c) return c;
            }
        }

        return fallback;
    }
}
