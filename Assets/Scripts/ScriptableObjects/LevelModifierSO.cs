using UnityEngine;
using UnityEngine.Video;

public enum LevelModifierKind
{
    None = 0,
    DoubleDamage,
    Overgrowth,
    StormyWeather,
    RearAmbush,
    LowRations,
    Contagion,
    SpecialLock,
    DeathExplosion,
    Swamp,
    AutoRotate,
    AutoShift,
    ComboGated,
    ComboShield,
    NoSpecialPieces,
    HalfHealthStart,
    SoulLink,
    PiercingShot,
    Disarray,
    OutFlanked,
    VolcanicEruption,
    Roulette
}

[System.Serializable]
public struct LevelModifierOption
{
    public LevelModifierSO modifier;
    [Min(1)] public int weight;
}

[CreateAssetMenu(menuName = "Tetrabeasts/Level Modifier")]
public class LevelModifierSO : ScriptableObject
{
    [Header("Identity")]
    public LevelModifierKind kind = LevelModifierKind.DoubleDamage;
    public string displayName = "Level Modifier";
    [TextArea(2, 5)] public string description;
    public Sprite icon;

    [Header("Presentation")]
    [Tooltip("Optional map/background sprite override shown while this modifier is active.")]
    public Sprite backgroundOverrideSprite;
    [Tooltip("Optional animated background video override shown while this modifier is active. Static sprites remain the fallback.")]
    public VideoClip backgroundOverrideVideoClip;
    [Tooltip("Playback speed for this modifier's animated background video override. Set to 0 to use AnimatedBG_VideoPlayer's scene speed.")]
    [Min(0f)] public float backgroundOverrideVideoPlaybackSpeed = 0f;
    [Tooltip("If true, the animated background video override plays forward, then backward, in a continuous ping-pong loop.")]
    public bool backgroundOverrideVideoPingPongLoop = false;
    [Tooltip("Optional Board_Background sprite override while this modifier is active.")]
    public Sprite boardBackgroundOverrideSprite;
    public bool enableRainOverlay = false;
    [Range(8, 128)] public int rainDrops = 44;
    [Range(40f, 280f)] public float rainSpeed = 140f;
    [Range(6f, 40f)] public float rainDropLength = 16f;
    [Range(1f, 8f)] public float rainDropWidth = 2f;
    [Range(-40f, 40f)] public float rainAngle = -18f;
    public Color rainTint = new Color(0.85f, 0.93f, 1f, 0.55f);

    [Header("Double Damage")]
    [Min(0f)] public float dealtDamageMultiplier = 2f;
    [Min(0f)] public float receivedDamageMultiplier = 2f;

    [Header("Overgrowth")]
    [Min(0.1f)] public float overgrowthTargetInterval = 2.5f;
    [Min(1)] public int overgrowthInitialTargetRows = 3;
    [Min(1)] public int overgrowthCellsPerExtraRow = 6;
    [Min(0.1f)] public float overgrowthPartialSeconds = 5f;
    [Min(0.1f)] public float overgrowthFullSeconds = 10f;
    public Sprite overgrowthInitialSprite;
    public Sprite overgrowthPartialSprite;
    public Sprite overgrowthFullSprite;
    public Color overgrowthTint = Color.white;

    [Header("Stormy Weather")]
    [Min(0.1f)] public float stormStrikeIntervalMin = 5f;
    [Min(0.1f)] public float stormStrikeIntervalMax = 8f;
    [Min(1)] public int stormTargetsMin = 1;
    [Min(1)] public int stormTargetsMax = 2;
    [Range(0f, 1f)] public float stormCrossChance = 0.6f;
    [Min(1)] public int stormLowerRowsOnlyCount = 8;
    [Min(0f)] public float stormInitialStrikeDamage = 2f;
    [Min(0f)] public float stormFloorTickDamage = 1f;
    [Min(0.05f)] public float stormFloorTickInterval = 0.5f;
    [Min(0.1f)] public float stormFloorDuration = 4f;

    [Header("Rear Ambush")]
    [Min(0.1f)] public float rearAmbushInterval = 10f;
    public Sprite rearAmbushSoldierSprite;

    [Header("Low Rations")]
    [Min(0.1f)] public float lowRationsTickInterval = 3f;
    [Min(0)] public int lowRationsNegligibleReserveThreshold = 5;
    [Min(0f)] public float lowRationsNegligibleDamage = 0.1f;
    [Min(0f)] public float lowRationsDamageAtMaxReserve = 1.35f;

    [Header("Contagion")]
    [Range(0f, 1f)] public float contagionInfectionChanceOnPieceSet = 0.30f;
    [Min(0.1f)] public float contagionTickInterval = 3f;
    [Min(0f)] public float contagionPercentDamagePerTick = 0.04f;
    [Min(0f)] public float contagionPercentIncreasePerTick = 0.02f;
    [Min(0.1f)] public float contagionSpreadInterval = 4f;
    [Range(0f, 1f)] public float contagionSpreadChance = 0.15f;
    public Sprite contagionUnitOverlaySprite;
    public Sprite contagionFloorSprite;
    public Color contagionUnitTint = new Color(0.6f, 1f, 0.45f, 0.85f);
    public Color contagionFloorTint = new Color(0.45f, 0.9f, 0.35f, 0.50f);

    [Header("Death Explosion")]
    [Range(0f, 1f)] public float deathExplosionMaxHpPercent = 0.3f;

    [Header("Swamp")]
    [Min(0f)] public float swampPoisonDamage = 1f;
    [Min(0.05f)] public float swampPoisonInterval = 1f;
    public int swampPoisonTicks = -1;

    [Header("Auto Rotate")]
    [Min(0.05f)] public float autoRotateInterval = 0.75f;

    [Header("Auto Shift")]
    [Min(0.05f)] public float autoShiftInterval = 0.4f;
    [Min(0f)] public float autoShiftEdgePauseSeconds = 0.6f;

    [Header("Combo Gated")]
    [Min(1)] public int comboThreshold = 3;
    [Range(0f, 1f)] public float comboBelowThresholdDamageMultiplier = 0.20f;
    [Min(0f)] public float comboAtOrAboveThresholdDamageMultiplier = 1.50f;

    [Header("Combo Shield")]
    [Min(1)] public int comboShieldThreshold = 4;
    [Min(1)] public int comboShieldCount = 3;
    [Range(0f, 1f)] public float comboShieldBlockedDamageMultiplier = 0.10f;

    [Header("Half Health Start")]
    [Range(0.01f, 1f)] public float startingHealthFraction = 0.5f;

    [Header("Piercing Shot")]
    public Sprite piercingProjectileSprite;

    [Header("Disarray")]
    [Range(0f, 1f)] public float disarrayTriminoChance = 0.15f;
    [Range(0f, 1f)] public float disarrayPentaminoChance = 0.15f;

    [Header("Out Flanked")]
    public Sprite outFlankedProjectileSprite;
    [Min(0.1f)] public float outFlankedIntervalMin = 6f;
    [Min(0.1f)] public float outFlankedIntervalMax = 10f;
    [Min(1)] public int outFlankedRowsPerWave = 1;
    [Min(0f)] public float outFlankedWarningSeconds = 2f;
    [Min(0.5f)] public float outFlankedProjectileSideOffsetCells = 3.25f;
    [Min(0.05f)] public float outFlankedProjectileSpeedMultiplier = 0.75f;
    [Min(0.1f)] public float outFlankedProjectileSizeMultiplier = 1f;

    [Header("Volcanic Eruption")]
    [Min(0.1f)] public float volcanicEruptionIntervalMin = 12f;
    [Min(0.1f)] public float volcanicEruptionIntervalMax = 20f;
    [Min(1)] public int volcanicEruptionTargetCount = 4;
    [Min(0)] public int volcanicEruptionExcludedTopRows = 4;
    [Min(0.1f)] public float volcanicEruptionWarningSeconds = 2f;
    [Range(0f, 1f)] public float volcanicEruptionMonsterTargetChance = 0.5f;
    public Sprite volcanicEruptionWarningSprite;
    public Color volcanicEruptionWarningTint = new Color(1f, 0.08f, 0f, 0.45f);
    public Sprite hardenedLavaObstacleSprite;

    [Header("Roulette")]
    [Min(0.1f)] public float roulettePreviewInterval = 1f;
}
