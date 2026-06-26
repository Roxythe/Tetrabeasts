using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameController : MonoBehaviour
{
    enum TimedSlowGravitySource
    {
        None,
        SpecialBlock,
        PlayerAbility
    }

    public Board gameBoard;
    public Piece piece;
    public Button restartButton;
    public Button volumeButton;
    public NextPreviewUI nextPreview;
    public TetrominoData[] allTetrominoes;

    [Header("Demo Build Guard Rails")]
    [SerializeField] bool demoBuildGuardRailsEnabled = false;
    [SerializeField, Min(1)] int demoMaxCompletedLevel = DemoBuildGuardRails.DefaultMaxCompletedLevel;
    [SerializeField, TextArea(3, 6)] string demoLevelLimitMessage =
        "Thank you for playing the Tetrabeasts demo!\n\nYou have cleared the final demo level. If you enjoyed your time with the game, please consider buying the full version.";

    [Header("XP Tuning")]
    [SerializeField] int baseXpPerLevel = 30;
    [Tooltip("Level 1 clear time that gives neither bonus nor penalty.")]
    [SerializeField, Min(0.01f)] float clearTimeXpParSeconds = 60f;
    [Tooltip("Level 1 clears at or below this time receive the full fast-clear XP bonus.")]
    [SerializeField, Min(0f)] float clearTimeXpFullBonusSeconds = 25f;
    [Tooltip("Level 1 clears at or above this time receive the full slow-clear XP penalty.")]
    [SerializeField, Min(0.01f)] float clearTimeXpFullPenaltySeconds = 180f;
    [Tooltip("Seconds added to each clear-time XP threshold for every level after level 1.")]
    [SerializeField, Min(0f)] float clearTimeXpSecondsAddedPerLevel = 15f;
    [Tooltip("Maximum fast-clear bonus as a fraction of base level XP.")]
    [SerializeField, Range(0f, 1f)] float clearTimeXpMaxBonusBaseFraction = 0.25f;
    [Tooltip("Maximum slow-clear penalty as a fraction of base level XP.")]
    [SerializeField, Range(0f, 1f)] float clearTimeXpMaxPenaltyBaseFraction = 0.15f;
    [Tooltip("Higher values make timing rewards stay flatter near par and ramp harder near the caps.")]
    [SerializeField, Min(0.1f)] float clearTimeXpCurveExponent = 1.2f;
    [SerializeField, Range(0f, 1f)] float overleveledXpMultiplierAtOneLevelGap = 0.90f;
    [SerializeField, Min(1f)] float overleveledXpGapExponent = 1.35f;
    [SerializeField, Range(0f, 1f)] float overleveledXpMinimumMultiplier = 0.05f;
    [SerializeField, Min(0)] int overleveledXpGraceLevelsPerStar = 5;

    [Header("Round Reward Rerolls")]
    [SerializeField, Min(0)] int rewardRerollsGrantedPerCompletedLevel = 1;

    [Header("Line Clear Visual Timing")]
    public float cascadeSettlePauseSeconds = 0.18f;

    [Header("Piece Action Visuals")]
    [SerializeField] bool smoothPieceActionVisuals = true;
    [SerializeField, Min(0f)] float pieceRotationVisualDuration = 0.08f;
    [SerializeField, Min(0f)] float pieceHardDropVisualDuration = 0.12f;

    [Header("Cursor (Gameplay)")]
    public UICursorController pauseCursor;

    [Header("Battle Log UI")]
    [SerializeField] private BattleLogUI battleLog;

    [Header("Floating Damage Text")]
    [SerializeField] private FloatingDamageText floatingDamageText;

    [Header("Level Progression")]
    public EnemyCastleUI enemyCastleUI;
    public CastleData[] castlesByLevel;
    int currentLevel = 0;

    [Header("Run Grid Growth")]
    public bool enableRunGridGrowth = true;
    public int growVerticalEveryNRounds = 2;   // +1 height every 2nd round
    public int growHorizontalEveryNRounds = 3; // +1 width every 3rd round

    int _baseBoardWidth = -1;
    int _baseBoardHeight = -1;

    [Header("Round Win Audio")]
    public AudioClip roundWinClip;

    [Header("Victory Flow")]
    [SerializeField] float victorySequenceDelaySeconds = 0.25f;

    [Header("Post-Final Survival")]
    [Tooltip("Optional CastleData used for the endless boss level after the final standard level is cleared.")]
    [SerializeField] CastleData postFinalSurvivalCastle;
    [SerializeField] bool forcePostFinalSurvivalInfiniteHealth = true;
    [SerializeField] bool forcePostFinalSurvivalBossLevel = true;
    [SerializeField, Min(0f)] float postFinalSurvivalDamageTakenIncreasePer60Seconds = 0.10f;

    [Header("Gravity UI")]
    [SerializeField] TMP_Text levelTimerText;
    [SerializeField] TMP_Text gravityText;
    [SerializeField] string gravityTextFormat = "{0:0.00}";

    [SerializeField] float startFallInterval = 1.0f;
    [SerializeField] float minFallInterval = 0.08f;
    [SerializeField] float finalSurvivalMinFallInterval = 0.07f;

    [Header("Gravity Progression")]
    [SerializeField] float gravityIncreasePerSecond = 0.01f;
    [SerializeField] float levelBaseGravityIncrease = 0.20f;

    [Header("Slow Gravity Special Block")]
    [SerializeField, Min(0.1f)] float slowGravitySpecialDurationSeconds = 20f;
    [SerializeField, Range(0.01f, 1f)] float minSlowGravitySpecialMultiplier = 0.10f;
    [SerializeField] Image slowGravityImage;
    [SerializeField] TMP_Text gravityTimerText;
    [SerializeField] Color gravityTextSlowColor = new Color(0.55f, 0.85f, 1f, 1f);

    // Runtime cache
    float _level1FallInterval;
    float _lastShownFallInterval = -1f;
    int _lastShownLevelTimerSeconds = -1;
    float _levelTimer = 0f;
    float _thisLevelBaseFallInterval;

    [Header("Boss Gravity Visuals")]
    [SerializeField] Image bossGravityIncreasedImage; // Toggle/flash when boss gravity is active
    [SerializeField] Color gravityTextDefaultColor = Color.white;
    [SerializeField] Color gravityTextBossColor = new Color(0.80f, 0.25f, 1.0f, 1f); // Bright purple
    [SerializeField] float bossGravityBlinkLeadSeconds = 3f;     // Start blinking this long before the effect ends
    [SerializeField] float bossGravityBlinkIntervalSeconds = 0.25f;

    Coroutine _bossGravityBlinkCR;
    bool _bossGravityVisualActive;

    [Header("Run Mods")]
    public RunModifierSO[] buffPool;
    public RunModifierSO[] debuffPool;

    public RoundRewardUI roundRewardUI;

    readonly List<RunModifierSO> _runBuffs = new();
    readonly List<RunModifierSO> _runDebuffs = new();

    [Header("Run RNG")]
    public float luck = 0f;        // Helps buffs skew higher rarity
    public float misfortune = 0f;  // Helps debuffs skew higher rarity

    [Header("Run Mods UI Panel")]
    [SerializeField] RunModsPanelUI runModsPanelUI;
    [SerializeField] Button openRunModsButton;
    [SerializeField] GameObject runModsPanelRoot;
    [SerializeField] Button closeRunModsButton;

    [Header("Gameplay Stats UI Panel")]
    [SerializeField] GameplayStatsPanelUI gameplayStatsPanelUI;
    [SerializeField] Button openGameplayStatsButton;
    [SerializeField] GameObject gameplayStatsPanelPrefab;
    [SerializeField] GameObject gameplayStatsMonsterPrefab;

    [SerializeField] GameObject helpPanelRoot;

    [Header("Player")]
    public UnityEngine.UI.Image playerPortrait;
    public TMPro.TMP_Text playerName;
    public TMPro.TMP_Text playerSpecialName;
    [SerializeField] Color specialTextDefaultColor = Color.white;
    [SerializeField] Color specialTextChargedColor = new Color(0.80f, 0.1f, 0.1f, 1f); // Red
    [SerializeField] private UnityEngine.UI.Image playerBorder;

    [Header("Characters")]
    public PlayerCharacterData selectedCharacter;
    public PlayerCharacterData[] roster;           // Populate for a character select screen

    [Header("Monsters")]
    public MonsterData[] fallbackMonsters; // 2 defaults, set in Inspector
    readonly Queue<MonsterData[]> monstersBag = new(); // Parallel to 'bag'

    [Header("Currency")]
    public CurrencyUI currencyUI;          // Drag your Currency prefab instance in GameplayScene
    public int currencyPerRoundWin = 5;    // How much for winning a round
    [Range(0f, 1f)] public float currencyChancePerClearedRow = 0.10f; // 10% per cleared row
    public Sprite currencyPopupSprite;     // Same coin sprite (for +1 popup)
    public float currencyPopupDuration = 0.6f; // Seconds
    public AudioClip sfxCurrencyLineGain;   // Play when +1 drops from a line clear

    [Header("Piece Lock SFX")]
    [SerializeField] AudioClip[] pieceLockSfxClips;
    [SerializeField, Range(0f, 2f)] float pieceLockSfxVolume = 1f;
    [SerializeField, Range(0f, 0.25f)] float pieceLockSfxPitchJitter = 0.06f;
    [SerializeField, Range(0f, 0.25f)] float pieceLockSfxVolumeJitter = 0.04f;

    [Header("Special Gauge UI")]
    public UnityEngine.UI.Slider specialSlider;
    public TMP_Text specialText;
    [SerializeField] TMP_Text gameplayControlsText;

    [Header("Special Ability Popup")]
    [SerializeField] GameObject specialAbilityPopupPrefab;
    [SerializeField] RectTransform specialAbilityPopupRoot;
    [SerializeField] AudioClip specialAbilityPopupLoopSFX;
    [SerializeField, Range(0f, 1f)] float specialAbilityPopupLoopSFXVolume = 0.55f;
    [SerializeField] AudioClip specialAbilityPopupLockInSFX;
    [SerializeField, Range(0f, 1f)] float specialAbilityPopupLockInSFXVolume = 0.65f;

    [Header("Special Gauge")]
    public float specialGauge = 0f;
    public float specialGaugeMax = 100f;
    public TMP_Text activateSpecialGaugeText;
    [SerializeField] AudioClip specialGaugeFullSFX;
    [SerializeField, Range(0f, 1f)] float specialGaugeFullSFXVolume = 1f;

    [SerializeField] bool specialUseFieryGradient = true;
    [SerializeField] float specialPulseScale = 1.15f;
    [SerializeField] float specialPulseSpeed = 2.25f;          // Pulses per second-ish
    [SerializeField] float specialGradientShiftSpeed = 1.25f;  // How fast the gradient shifts

    Coroutine _specialChargedCR;
    TetrabeastsControlProfile _lastControlsTextSavedProfile;
    TetrabeastsControlProfile _lastControlsTextEffectiveProfile;
    TetrabeastsControlProfile _lastControlsTextActiveProfile;
    string _lastControlsTextSpecialBinding;
    bool _hasControlsTextSnapshot;

    class SpecialTextDefaults
    {
        public Vector3 scale;
        public bool hadVertexGradient;
        public VertexGradient gradient;
        public Color color;
    }

    readonly System.Collections.Generic.Dictionary<TMP_Text, SpecialTextDefaults> _specialTextDefaults =
        new System.Collections.Generic.Dictionary<TMP_Text, SpecialTextDefaults>();

    [Header("Special Gauge Fiery Fill")]
    [SerializeField] Slider specialGaugeSlider;          // Gauge slider
    [SerializeField] Image specialGaugeFillImage;        // Fill image component

    [SerializeField] bool specialGaugeUseFieryFill = true;
    [SerializeField] Color specialGaugeFillingColor = new Color(0.80f, 0.10f, 0.10f, 1f);
    [SerializeField] float specialGaugeFillMinSpeed = 0.25f;  // at ~0%
    [SerializeField] float specialGaugeFillMaxSpeed = 1.25f;  // at 100%
    [SerializeField] float specialGaugeFillColorBoost = 1.0f; // 1 = normal, >1 brighter

    float _specialFillPhase = 0f;
    Coroutine _specialFillCR;

    [Header("Special Blocks")]
    public TetrominoData[] specialBlocks;
    [Range(0f, 1f)]
    public float specialChancePerEnqueue = 0.08f;
    [Range(0f, 1f)]
    public float minSpecialChance = 0.01f;
    [Range(0f, 1f)]
    public float maxSpecialChance = 0.33f; // Hard cap (33%)

    [Header("Projectiles")]
    public RectTransform projectileRoot;
    public float projectileSpeed = 700f;
    public GameObject[] attackExplosionPrefabs;
    public float attackExplosionSizeMultiplier = 2f;
    public Vector2 attackExplosionOffsetCells = new Vector2(0f, 0.5f);

    [Header("Bag Settings")]
    public int minBagPieces = 2;
    public bool forceOneSpecialPerRefill = false; // Optional "ensure a special" switch

    [Header("Castle Attacks")]
    public float castleAttackInterval = 3.0f;  // Seconds between shots
    public Sprite castleProjectileSprite;
    float _castleAttackTimer = 0f;
    int castleProjectileDamage = 1;
    float castleProjectileVisualScale = 1f;
    readonly List<int> _castleAliveTargetColumns = new();
    readonly List<int> _castleDeadOnlyTargetColumns = new();
    readonly List<int> _castleProjectileColumns = new();

    [Header("Enemy Attack Interval Ramp")]
    [SerializeField] bool enableEnemyAttackIntervalRamp = true;
    [SerializeField, Min(0f)] float enemyAttackIntervalRampDelaySeconds = 12f;
    [SerializeField, Min(1f)] float enemyAttackIntervalRampDurationSeconds = 120f;
    [SerializeField, Range(0.25f, 1f)] float enemyAttackIntervalRampEndMultiplier = 0.70f;
    [SerializeField] AnimationCurve enemyAttackIntervalRampCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Global Immunity Visuals")]
    public bool immunityActive = false;
    public Color immunityOutline = new Color(1f, 0.85f, 0f, 1f); // Gold
    public Color normalOutline = Color.black;

    [Header("Pause")]
    public GameObject pausePanel;
    public UnityEngine.UI.Button resumeButton;
    public UnityEngine.UI.Button mainMenuButton;
    public UnityEngine.UI.Button quitButton;
    public VolumePanelUI volumePanelInPause;
    public string titleSceneName = "TitleScene";
    bool isPaused = false;
    ScopedMenuNavigator pauseMenuNavigator;
    public bool IsPaused => isPaused;
    bool tutorialSuspended = false;
    bool tutorialFreezePieceGravity = false;

    public bool SmoothPieceActionVisuals => smoothPieceActionVisuals;
    public float PieceRotationVisualDuration => Mathf.Max(0f, pieceRotationVisualDuration);
    public float PieceHardDropVisualDuration => Mathf.Max(0f, pieceHardDropVisualDuration);
    public bool IsTutorialPieceGravityFrozen => tutorialFreezePieceGravity;
    public event System.Action<TutorialGameplayEvent> TutorialGameplayEventRaised;

    [Header("Unit Lives")]
    public int maxUnitLives = 20; // Base (starting unit lives)
    [SerializeField] int unitLives = 20; // Current lives (Based on max + buffs)
    [SerializeField] int reinforcementsPerWin = 5;

    [Header("Unit Lives Glass Overlay")]
    [SerializeField] Image unitLivesGlassOverlayImage;
    [SerializeField] Sprite[] unitLivesGlassOverlaySprites = new Sprite[3];
    [SerializeField] float unitLivesGlassCrackSfxVolume = 1f;

    enum UnitLivesGlassState
    {
        High = 0,
        Mid = 1,
        Low = 2
    }

    UnitLivesGlassState? _lastUnitLivesGlassState;

    public TMP_Text unitLivesText;
    public Slider unitLivesSlider;

    [Header("Unit Lives UI Flash")]
    public Color unitLivesFlashColor = new Color(1f, 0.15f, 0.15f, 1f);
    public float unitLivesFlashSeconds = 0.12f;

    Image _unitLivesFillImg;
    Color _unitLivesFillDefaultColor;
    Coroutine _unitLivesFlashCR;

    [Header("Unit Lives UI Shake")]
    public float unitLivesShakeSeconds = 0.15f;
    public float unitLivesShakeAmplitude = 6f; 
    public float unitLivesShakeHz = 26f;

    RectTransform _unitLivesBarRect;
    Vector2 _unitLivesBarDefaultPos;
    Coroutine _unitLivesShakeCR;

    [Header("Obstacles & Traps")]
    public ObstacleManager obstacleManager;

    // ================= Boss Ability Loop =================
    CastleData _castleData;
    float _bossAbilityTimer = 0f;
    float _bossNextAbilityAt = 0f;
    float _bossGravityBonusActive = 0f;
    Coroutine _bossGravityCR;

    [Header("Boss: Pylon Shield")]
    public bool bossEnablePylonShield = true;
    public int bossPylonCount = 2;
    [Range(0.05f, 1f)]
    public float bossPylonDamageMult = 0.5f; // Damage taken while pylons exist (0.5 = 50%)

    bool _bossPylonShieldActive = false;
    readonly List<int> _bossObstacleCandidateColumns = new();

    [Header("Boss: Magic Explosive")]
    public bool bossEnableMagicExplosive = true;
    public float bossExplosiveFuseSeconds = 15f;
    public int bossExplosiveRowClearBonusDamage = 50;
    public bool bossExplosivePreferUpperHalf = true;

    public bool bossPreferLowerHalf = true;
    public bool bossPreferAsLowAsPossible = true;
    public bool bossAvoidCompletingRow = true; // Obstacles wont spawn in a cell that would complete a full row

    // ================= Boss Ability Pool runtime =================
    CastleData.BossAbilityKind _bossLastAbility = (CastleData.BossAbilityKind)(-1);
    readonly Dictionary<CastleData.BossAbilityKind, float> _bossNextReadyTime = new();

    // =========== Run Modifiers (reset each run) ===========
    [Header("Run Modifiers (runtime)")]
    public float enemyAttackIntervalMult = 1f;     // < 1 = attacks faster, > 1 = slower
    public float enemyProjectileDamageMult = 1f;   // > 1 = more damage
    public float enemyProjectileSpeedMult = 1f;    // Scales enemy projectile speed
    public float enemyCastleHpMult = 1f;

    [Header("Enemy Damage Scaling")]
    [SerializeField, Min(0f)] float floorEffectDamageIncreasePerLevel = 0.05f;

    public float specialGainMult = 1f;             // Gauge gain multiplier
    public float specialDrainMult = 1f;

    public float specialBlockChanceAdd = 0f;       // Additive to specialChancePerEnqueue
    public float pieceGravityMult = 1f;            // < 1 = slower falling, > 1 = faster
    public float fallRampRateMult = 1f;            // < 1 = ramp slower, >1 = ramp faster

    public float monsterDamageMult = 1f;           // Scales monster attackPower contribution
    public float monsterSpecialGainMult = 1f;      // Scales monster specialGaugeGain contribution
    public float monsterMaxHpMult = 1f;            // Used when spawning monster tiles
    public float healPowerMult = 1f;
    public int healRangeAdd = 0;

    public bool disableNextPreview = false;
    public bool disableLandingHint = false;
    bool _pendingMainMenuAfterXp = false;
    bool _finalWinStateApplied = false;
    bool _postFinalSurvivalActive = false;
    bool _pendingPostFinalSurvivalIntro = false;
    bool _demoLimitRunEnding = false;

    public int currencyPerRoundWinAdd = 0;
    public float currencyPerRoundWinMult = 1f;
    public float lineClearCurrencyChanceAdd = 0f;
    public float lineClearCurrencyAmountMult = 1f;
    public float stoneBuffDropChanceAdd = 0f;
    public bool stoneObstacleDropsDebuffsOnly = false;
    public int reserveUnitsRestoredOnWinAdd = 0;
    public int maxReserveUnitsAdd = 0;
    public bool disableRoundWinReserveRestore = false;

    // =========== Player Special Timers ===========
    float _playerGravityMultActive = 1f;     // 1 = normal
    Coroutine _playerGravityCR;
    bool _playerGravityBaseOverrideActive;
    float _slowGravitySpecialMultActive = 1f;
    float _slowGravitySpecialRampRateMultActive = 1f;
    bool _slowGravitySpecialVisualActive;
    Coroutine _slowGravitySpecialCR;
    TimedSlowGravitySource _activeTimedSlowGravitySource = TimedSlowGravitySource.None;
    float _activeTimedSlowGravityRemainingSeconds;
    int _lastShownTimedSlowGravitySeconds = -1;

    float _playerDoubleStatsAttackMult = 1f; // Multiplied into monster damage output
    Coroutine _playerDoubleStatsCR;
    public float PlayerMonsterAttackMult => _playerDoubleStatsAttackMult; // Exposed for monster damage calculations

    // =========== Shop Buff Effective Values ===========
    int EffectiveMaxUnitLives => Mathf.Max(1, maxUnitLives + ShopBuffEffects.UnitLivesBonus + _partyPassiveBonuses.startingReserveUnits + maxReserveUnitsAdd);
    float CurrentComboWindowSeconds => comboWindowSeconds + _partyPassiveBonuses.comboDurationSeconds;
    float CurrentStoneBuffDropChance => Mathf.Clamp01(stoneBuffDropChance + _partyPassiveBonuses.stoneBuffDropChance + stoneBuffDropChanceAdd);
    int CurrentReinforcementsPerWin => disableRoundWinReserveRestore ? 0 : Mathf.Max(0, reinforcementsPerWin + _partyPassiveBonuses.reserveUnitsRestoredOnWin + reserveUnitsRestoredOnWinAdd);
    float AllyMonsterOutgoingDamageMultiplier => Mathf.Max(0f, 1f - _partyPassiveBonuses.allyMonsterDamageDoneReduction);
    public float AllyMonsterDamageTakenMultiplier =>
        Mathf.Max(0f, 1f - _partyPassiveBonuses.allyMonsterDamageTakenReduction) *
        PostFinalSurvivalDamageTakenMultiplier;
    float CurrentCurrencyGainMultiplier => Mathf.Max(0f, 1f + _partyPassiveBonuses.currencyGainMultiplierAdd);
    float CurrentPartyExperienceGainMultiplier => Mathf.Max(0f, 1f + _partyPassiveBonuses.partyExperienceGainMultiplierAdd);

    float EffectivePieceGravityMult =>
    Mathf.Max(0.01f, pieceGravityMult) *
    ShopBuffEffects.GravityMultiplier *
    _playerGravityMultActive *
    _slowGravitySpecialMultActive *
    (1f + _bossGravityBonusActive); // Slows falling (mult < 1 => slower because interval /= mult)

    float EffectiveFallRampRateMult => Mathf.Max(0f, fallRampRateMult) * ShopBuffEffects.VelocityMultiplier * _slowGravitySpecialRampRateMultActive; // Velocity Down: slows ramping (mult < 1 => slower ramp)
    float ActiveLevelModifierGravityMult => levelModifierController ? levelModifierController.ActiveGravityMultiplier : 1f;
    float EffectiveCurrencyChancePerClearedRow => // Gold Up: +2% chance per level
        Mathf.Clamp01(currencyChancePerClearedRow + lineClearCurrencyChanceAdd + ShopBuffEffects.GoldChanceBonus);
    float EffectiveLuck => luck + ShopBuffEffects.LuckBonus; // Luck Up: +10 per level 
    float CurrentGravityMinFallInterval
    {
        get
        {
            float normalCap = Mathf.Max(0.01f, minFallInterval);
            if (!_postFinalSurvivalActive)
                return normalCap;

            return Mathf.Min(normalCap, Mathf.Max(0.01f, finalSurvivalMinFallInterval));
        }
    }

    float PostFinalSurvivalDamageTakenMultiplier
    {
        get
        {
            if (!_postFinalSurvivalActive)
                return 1f;

            int stacks = Mathf.Max(0, Mathf.FloorToInt(_levelTimer / 60f));
            return 1f + (stacks * Mathf.Max(0f, postFinalSurvivalDamageTakenIncreasePer60Seconds));
        }
    }

    int _baseCurrencyPerRoundWin;
    float _baseCurrencyChancePerClearedRow;
    int _baseMaxUnitLives;
    int _baseReinforcementsPerWin;
    float _baseComboWindowSeconds;
    float _baseStoneBuffDropChance;
    float _baseSpecialChancePerEnqueue;
    bool _baseGameplayStatsCached;

    // ======== Achievements helpers ========
    [SerializeField] string[] achievementCharacterIds = new string[5]; // Set 5 character asset names
    float _gravityCapAccumSeconds = 0f;

    readonly Queue<TetrominoData> bag = new();
    public int score { get; private set; }
    bool gameOver = false;
    bool levelWon = false;
    bool _environmentRowClearResolving = false;
    private bool winQueued = false;

    [Header("Combo Scoring")]
    public float comboWindowSeconds = 10f;
    int _comboCount = 0;
    float _comboTimer = 0f;
    float comboDamMult = 0.05f; // 5% per combo count (e.g. 20 combo = +100% damage)
    bool _rowClearComboResolutionActive;

    [Header("Level Performance Tracking - Bonus XP")]
    int _maxComboThisLevel = 0;
    int _obstaclesDestroyedThisLevel = 0;
    int _levelStartMaxLives = 0;
    int _levelStartReserveUnits = 0;
    MonsterPassiveBonuses _partyPassiveBonuses;

    [Header("Run-End XP Conversion")]
    [SerializeField, Range(0f, 1f)] float baseRunEndXpConversion = 0.15f;
    [SerializeField, Range(0f, 1f)] float starDifficultyRunEndXpBonusPerStar = 0.05f;
    [SerializeField, Range(0f, 1f)] float finalLevelWinXpConversionBonus = 0.10f;

    [Header("Debug")]
    public bool logRowDamageBreakdown = true;

    public ScoreUI scoreUI;
    public HighScoreUI highScoreUI;
    [SerializeField] XpAwardUI xpAwardUI;

    [Header("Victory Panel")]
    [SerializeField] VictoryPanelUI victoryPanelUI;
    [SerializeField] GameObject victoryModifierRowPrefab;

    [Header("Round Transition")]
    [SerializeField] RoundTransitionUI roundTransitionUI;
    [SerializeField] TMP_FontAsset roundTransitionFont;
    [SerializeField] Button roundTransitionContinueButtonPrefab;
    [SerializeField, Min(0f)] float levelStartRoundTransitionDelaySeconds = 1f;

    public TMP_Text levelText;
    [HideInInspector] public LevelModifierController levelModifierController;
    bool _levelStartBlocked = false;
    int _starDifficulty = 0;
    StarDifficultyModifiers _starDifficultyModifiers = new StarDifficultyModifiers(0);
    bool _newDifficultyUnlockedThisRun;
    bool _roundTransitionActive;
    string _claimedRoundWinOneLiner = string.Empty;
    string _claimedRoundLossOneLiner = string.Empty;

    const string RoundWinTransitionText = "The Castle Has Fallen";
    const string RoundLossTransitionText = "Conquest Failed";
    const string PostFinalSurvivalIntroPrefsKey = "Tetrabeasts_PostFinalSurvivalIntroHidden";
    const string PostFinalSurvivalIntroText =
        "Endless Survival\n\n" +
        "This final battle cannot be won. The enemy has endless health, and the run continues until a loss condition is met.\n\n" +
        "Survive as long as you can.";
    const string PostFinalSurvivalIntroOptOutText = "Do not show this message again";

    public int CurrentLevel => currentLevel;
    public int CurrentReserveUnits => unitLives;
    public int MaxReserveUnits => EffectiveMaxUnitLives;
    public int CurrentStarDifficulty => _starDifficulty;
    public float CurrentMisfortune => misfortune + _starDifficultyModifiers.misfortuneAdd;
    public bool IsGameplaySuspended => gameOver || levelWon || isPaused || LoadingScreen.IsVisible || ConfirmationPopupUI.IsAnyShowing || tutorialSuspended || _roundTransitionActive || _specialAbilityCinematicActive || _levelStartBlocked || _environmentRowClearResolving || (levelModifierController && levelModifierController.IsSelectionRunning);
    public bool IsRoundActive => !IsGameplaySuspended && !gameOver && !levelWon;
    public bool IsTutorialPieceInputBlocked => tutorialPieceInputBlocked;
    public bool IsTutorialHardDropInputGraceActive => Time.unscaledTime < _tutorialHardDropInputBlockedUntilRealtime;
    public int EffectiveMaxUnitLivesForStats => EffectiveMaxUnitLives;
    public int BaseMaxUnitLivesForStats => _baseGameplayStatsCached ? _baseMaxUnitLives : maxUnitLives;
    public int CurrentReinforcementsPerWinForStats => CurrentReinforcementsPerWin;
    public int BaseReinforcementsPerWinForStats => _baseGameplayStatsCached ? _baseReinforcementsPerWin : reinforcementsPerWin;
    public int CurrentRoundWinCurrencyForStats => GetRoundWinCurrency();
    public int BaseCurrencyPerRoundWinForStats => _baseGameplayStatsCached ? _baseCurrencyPerRoundWin : currencyPerRoundWin;
    public float EffectiveCurrencyChancePerClearedRowForStats => EffectiveCurrencyChancePerClearedRow;
    public float BaseCurrencyChancePerClearedRowForStats => _baseGameplayStatsCached ? _baseCurrencyChancePerClearedRow : currencyChancePerClearedRow;
    public float CurrentComboWindowSecondsForStats => CurrentComboWindowSeconds;
    public float BaseComboWindowSecondsForStats => _baseGameplayStatsCached ? _baseComboWindowSeconds : comboWindowSeconds;
    public float CurrentStoneBuffDropChanceForStats => CurrentStoneBuffDropChance;
    public float BaseStoneBuffDropChanceForStats => _baseGameplayStatsCached ? _baseStoneBuffDropChance : stoneBuffDropChance;
    public float CurrentCurrencyGainMultiplierForStats => CurrentCurrencyGainMultiplier;
    public float CurrentPartyExperienceGainMultiplierForStats => CurrentPartyExperienceGainMultiplier;
    public float LineClearCurrencyAmountMultiplierForStats => lineClearCurrencyAmountMult * CurrentCurrencyGainMultiplier;
    public float EffectivePieceGravityMultForStats => EffectivePieceGravityMult * ActiveLevelModifierGravityMult;
    public float EffectiveFallRampRateMultForStats => EffectiveFallRampRateMult * ActiveLevelModifierGravityMult;
    public bool LevelModifierGravitySlowActiveForStats => levelModifierController && levelModifierController.AppliesAutoMovementGravitySlow;
    public bool BossGravityActiveForStats => _bossGravityBonusActive > 0.0001f;
    public float EffectiveLuckForStats => EffectiveLuck;
    public MonsterPassiveBonuses PartyPassiveBonusesForStats => _partyPassiveBonuses;
    public CastleData CurrentCastleDataForStats => currentCastleData;
    public int CastleProjectileDamageForStats => castleProjectileDamage;
    public bool SpecialUsageLockedForStats => levelModifierController && levelModifierController.BlocksSpecialUsage;
    public bool SpecialBlocksLockedForStats => levelModifierController && levelModifierController.BlocksSpecialPieceSpawns;
    public float BaseSpecialChancePerEnqueueForStats => _baseGameplayStatsCached ? _baseSpecialChancePerEnqueue : specialChancePerEnqueue;
    public float EffectiveSpecialGaugeGainMultiplierForStats =>
        SpecialUsageLockedForStats ? 0f : GetEffectiveSpecialGaugeGainMultiplier();
    public float AllyMonsterOutgoingDamageMultiplierForStats => AllyMonsterOutgoingDamageMultiplier;
    public float CurrentSpecialBlockChanceForStats
    {
        get
        {
            if (SpecialBlocksLockedForStats || specialBlocks == null || specialBlocks.Length == 0)
                return 0f;

            float chanceCap = Mathf.Max(0f, maxSpecialChance);
            float chanceFloor = Mathf.Min(Mathf.Clamp01(minSpecialChance), chanceCap);
            return Mathf.Clamp(specialChancePerEnqueue + specialBlockChanceAdd, chanceFloor, chanceCap);
        }
    }

    public float MonsterDamageOutputMultiplierForStats
    {
        get
        {
            float multiplier = Mathf.Max(0f, monsterDamageMult) * AllyMonsterOutgoingDamageMultiplier * PlayerMonsterAttackMult;
            LevelModifierSO activeModifier = levelModifierController ? levelModifierController.ActiveModifier : null;
            if (activeModifier && activeModifier.kind == LevelModifierKind.DoubleDamage)
                multiplier *= Mathf.Max(0f, activeModifier.dealtDamageMultiplier);

            return multiplier;
        }
    }

    public float MonsterSpecialGaugeGainMultiplierForStats =>
        Mathf.Max(0f, monsterSpecialGainMult) * EffectiveSpecialGaugeGainMultiplierForStats;

    private CastleData currentCastleData;

    // Used by reward UI and other systems that need to know what type of level just ended
    public bool LastLevelWasBoss => currentCastleData != null && IsCastleBossForCurrentMode(currentCastleData);
    public int CompletedBossLevelsThisRun => CountCompletedStandardBossLevelsBeforeCurrentLevel();
    public bool BossLegendaryDebuffRewardsUnlocked => CompletedBossLevelsThisRun >= 3;

    public void AddMaxReserveUnitsModifier(int amount)
    {
        maxReserveUnitsAdd += amount;
        ClampUnitLivesToEffectiveMax();
    }

    public void MultiplyMaxReserveUnitsModifier(float multiplier)
    {
        int baseMax = maxUnitLives + ShopBuffEffects.UnitLivesBonus + _partyPassiveBonuses.startingReserveUnits;
        int targetMax = Mathf.RoundToInt(EffectiveMaxUnitLives * multiplier);
        maxReserveUnitsAdd = targetMax - baseMax;
        ClampUnitLivesToEffectiveMax();
    }

    void ClampUnitLivesToEffectiveMax()
    {
        unitLives = Mathf.Clamp(unitLives, 0, EffectiveMaxUnitLives);
        UpdateUnitLivesUI();
    }

    // ========== Stone Block Buffs ==========

    [Header("Stone Buff Popup")]
    [Range(0f, 1f)] public float stoneBuffDropChance = 0.10f; // Chance to drop a run buff when stone breaks
    public bool showStoneBuffPopup = true;
    public BuffPopupStyleSO stoneBuffPopupStyle;
    public AudioClip sfxStoneBuffGranted;

    [Header("Stone Buff Pools By Rarity (Optional)")]
    public RunModifierSO[] stoneBuffPoolCommon;
    public RunModifierSO[] stoneBuffPoolUncommon;
    public RunModifierSO[] stoneBuffPoolRare;
    public RunModifierSO[] stoneBuffPoolEpic;
    public RunModifierSO[] stoneBuffPoolLegendary;

    [Header("Stone Debuff Pools By Rarity (Optional)")]
    public RunModifierSO[] stoneDebuffPoolCommon;
    public RunModifierSO[] stoneDebuffPoolUncommon;
    public RunModifierSO[] stoneDebuffPoolRare;
    public RunModifierSO[] stoneDebuffPoolEpic;
    public RunModifierSO[] stoneDebuffPoolLegendary;

    [Header("Stone Buff Rarity Weights By Level")]
    [Tooltip("Levels 1-3")]
    public Vector4 stoneWeights_L1_3 = new Vector4(0.90f, 0.10f, 0.00f, 0.00f); // C,U,R,E (no legendary here)
    [Tooltip("Levels 4-6")]
    public Vector4 stoneWeights_L4_6 = new Vector4(0.70f, 0.25f, 0.05f, 0.00f);
    [Tooltip("Levels 7-9")]
    public Vector4 stoneWeights_L7_9 = new Vector4(0.55f, 0.30f, 0.12f, 0.03f);
    [Tooltip("Levels 10+")]
    public Vector4 stoneWeights_L10P = new Vector4(0.40f, 0.30f, 0.20f, 0.10f);

    [Range(0f, 0.20f)] public float stoneLegendaryChance_L10P = 0.02f; // taken out of common at level 10+

    [Header("Tutorial Events")]
    [SerializeField] TriggeredTutorialPopupController triggeredTutorialPopups;
    [SerializeField] SpecialBlockTutorialController specialBlockTutorials;
    [SerializeField] TutorialPopupView tutorialPopupView;

    bool _wasSpecialGaugeFullLastFrame;
    bool _specialAbilityCinematicActive;
    Coroutine _specialAbilityCinematicCR;
    GameObject _prewarmedSpecialAbilityPopup;
    PlayerCharacterData _prewarmedSpecialAbilityCharacter;
    SpecialAbilityPopup _prewarmedSpecialAbilityPopupView;

    const string TutorialIdFirstFullRow = "tutorial_first_full_row";
    const string TutorialIdFirstSpecialGaugeFull = "tutorial_first_special_gauge_full";
    bool _firstFullRowTutorialShownThisRun;
    bool tutorialAllowSoftDrop = false;
    bool tutorialAllowHardDrop = false;
    bool tutorialPieceInputBlocked = false;
    float _tutorialHardDropInputBlockedUntilRealtime;
    TempRunSaveStore.SaveData _cachedTempRunCheckpoint;
    int _roundRewardRerollsAvailable;

    public bool IsTutorialSoftDropAllowed => tutorialAllowSoftDrop;
    public bool IsTutorialHardDropAllowed => tutorialAllowHardDrop;

    public bool IsTutorialPromptActive
    {
        get
        {
            if (triggeredTutorialPopups && triggeredTutorialPopups.IsPopupShowing)
                return true;

            return IsAnyTutorialSequenceRunning();
        }
    }

    const string QuitWithoutSavingWarningMessage =
    "Quit without saving? Your current run will be lost and will not be available to continue later.";

    void Awake()
    {
        CacheBaseGameplayStatsForPanel();
        ApplyDemoBuildGuardRailsSetting();
    }

    void CacheBaseGameplayStatsForPanel()
    {
        _baseCurrencyPerRoundWin = currencyPerRoundWin;
        _baseCurrencyChancePerClearedRow = currencyChancePerClearedRow;
        _baseMaxUnitLives = maxUnitLives;
        _baseReinforcementsPerWin = reinforcementsPerWin;
        _baseComboWindowSeconds = comboWindowSeconds;
        _baseStoneBuffDropChance = stoneBuffDropChance;
        _baseSpecialChancePerEnqueue = specialChancePerEnqueue;
        _baseGameplayStatsCached = true;
    }

    void ApplyDemoBuildGuardRailsSetting()
    {
        DemoBuildGuardRails.Configure(demoBuildGuardRailsEnabled, demoMaxCompletedLevel);
    }

    void Start()
    {
        ApplyDemoBuildGuardRailsSetting();
        SetGravityTimerVisible(false);
        SteamInputService.Ensure();
        SteamPlatformService.Ensure();
        SteamInputService.ControllerDisconnected -= HandleControllerDisconnected;
        SteamInputService.ControllerDisconnected += HandleControllerDisconnected;
        SteamPlatformService.OverlayActiveChanged -= HandleSteamOverlayActiveChanged;
        SteamPlatformService.OverlayActiveChanged += HandleSteamOverlayActiveChanged;
        TetrabeastsControls.ProfileChanged -= HandleControlsDisplayChanged;
        TetrabeastsControls.ProfileChanged += HandleControlsDisplayChanged;
        TetrabeastsControls.BindingsChanged -= HandleControlsDisplayChanged;
        TetrabeastsControls.BindingsChanged += HandleControlsDisplayChanged;
        TetrabeastsControls.PlatformDefaultProfileChanged -= HandleControlsDisplayChanged;
        TetrabeastsControls.PlatformDefaultProfileChanged += HandleControlsDisplayChanged;

#if ENABLE_INPUT_SYSTEM
        InputSystem.onDeviceChange -= HandleInputDeviceChange;
        InputSystem.onDeviceChange += HandleInputDeviceChange;
#endif

        // Ensure PlayerProgress exists (stats + achievements)
        if (PlayerProgress.I == null)
        {
            var go = new GameObject("PlayerProgress");
            go.AddComponent<PlayerProgress>();
        }

        TempRunSaveStore.SaveData pendingTempRunSave;
        bool resumeFromTempRun = PreparePendingTempRunResume(out pendingTempRunSave);

        // New run starts when gameplay scene starts
        if (!resumeFromTempRun)
            PlayerProgress.I.BeginRun();

        HighScoreManager.EnsureInitialized(10);
        if (!highScoreUI)
            highScoreUI = FindFirstObjectByType<HighScoreUI>(FindObjectsInactive.Include);

        if (!highScoreUI) Debug.LogError("HighScoreUI not found in scene.");
        else Debug.Log("HighScoreUI bound to: " + highScoreUI.gameObject.name);

        ResolveVictoryPanelUi();
        if (victoryPanelUI) victoryPanelUI.Hide();

        if (!roundTransitionUI)
            roundTransitionUI = FindFirstObjectByType<RoundTransitionUI>(FindObjectsInactive.Include);
        HideRoundTransitionImmediate();

        if (allTetrominoes == null || allTetrominoes.Length == 0)
        {
            Debug.LogError("GameController: allTetrominoes is empty.");
            return;
        }

        if (!gameBoard) gameBoard = FindFirstObjectByType<Board>();
        if (!piece) piece = GetComponent<Piece>();
        if (!levelModifierController) levelModifierController = GetComponent<LevelModifierController>();
        if (!levelModifierController) levelModifierController = gameObject.AddComponent<LevelModifierController>();
        EnsureFloatingDamageText();
        EnsureTriggeredTutorialPopups();
        EnsureSpecialBlockTutorials();

        if (!obstacleManager)
            obstacleManager = FindFirstObjectByType<ObstacleManager>(FindObjectsInactive.Include);

        if (obstacleManager)
            obstacleManager.Initialize(this, gameBoard);

        if (gameBoard != null)
        {
            gameBoard.ObstacleDestroyed -= OnBoardObstacleDestroyed;
            gameBoard.ObstacleDestroyed += OnBoardObstacleDestroyed;
        }

        _level1FallInterval = startFallInterval; // Cache initial value

        if (gameBoard != null)
        {
            gameBoard.TileDied -= OnBoardTileDied;
            gameBoard.TileDied += OnBoardTileDied;
        }

        if (!battleLog)
            battleLog = FindFirstObjectByType<BattleLogUI>(FindObjectsInactive.Include);

        if (battleLog)
            battleLog.SetVisible(SettingsStore.LoadCombatLogEnabled());

        if (gameBoard != null)
        {
            gameBoard.TileDamaged -= OnBoardTileDamaged;
            gameBoard.TileDamaged += OnBoardTileDamaged;

            gameBoard.TileHealed -= OnBoardTileHealed;
            gameBoard.TileHealed += OnBoardTileHealed;
        }

        // Resolve saved character selection if any
        if (selectedCharacter == null && roster != null && roster.Length > 0)
        {
            var saved = SelectedCharacterStore.ResolveFromRoster(roster);
            if (saved != null) SelectedCharacterStore.Current = saved;
        }

        // Prefer the character picked on the title screen
        if (SelectedCharacterStore.Current != null)
            selectedCharacter = SelectedCharacterStore.Current;
        else if (!selectedCharacter && roster != null && roster.Length > 0)
            selectedCharacter = roster[0];

        ApplySelectedCharacterUI();

        // Wire Run Mods panel buttons
        if (!runModsPanelRoot && runModsPanelUI)
            runModsPanelRoot = runModsPanelUI.gameObject;

        if (openRunModsButton)
            openRunModsButton.onClick.AddListener(OpenRunModsPanel);

        if (closeRunModsButton)
            closeRunModsButton.onClick.AddListener(CloseRunModsPanel);

        SetupGameplayStatsPanel();

        // Ensure pause menu sub panels start closed
        if (runModsPanelRoot) UIPanelTransition.Hide(runModsPanelRoot, true);
        if (gameplayStatsPanelUI) gameplayStatsPanelUI.Close(true);
        if (helpPanelRoot) UIPanelTransition.Hide(helpPanelRoot, true);

        // Apply character special gauge max
        if (selectedCharacter && selectedCharacter.specialGaugeMax > 0f)
            specialGaugeMax = selectedCharacter.specialGaugeMax;

        ResetSpecialGauge();
        RefreshGameplayControlTexts();
        ResetSpecialChargedVisuals();
        PrewarmSpecialAbilityPopup();

        if (_specialFillCR != null) StopCoroutine(_specialFillCR);
        _specialFillCR = StartCoroutine(SpecialGaugeFillFieryCo());

        levelModifierController?.ResetRunState();
        ResetRunMods();
        RefreshStarDifficultyState();
        RefreshActiveMonsterPassives(applyStartingReserveDelta: false);

        CacheBaseBoardSizeIfNeeded();

        if (resumeFromTempRun && pendingTempRunSave != null)
            RestoreTempRunCheckpoint(pendingTempRunSave);
        else
            StartFreshRun();

        // Pause menu defaults
        if (pausePanel) UIPanelTransition.Hide(pausePanel, true);
        Time.timeScale = 1f;
        isPaused = false;

        EnterGameplayCursorMode(); // Lock cursor at start

        // Wire pause buttons
        if (resumeButton)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (mainMenuButton)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (quitButton)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(RequestSaveAndQuit);
        }

        if (volumePanelInPause) volumePanelInPause.pauseWhenOpen = false;

        if (currencyUI) currencyUI.Refresh();

        SettingsStore.ApplySavedVolumesToAudio();

        if (pauseCursor)
            pauseCursor.SetScale(SettingsStore.LoadCursorScale());

        SettingsStore.CursorScaleChanged += OnCursorScaleChanged;
    }

    bool PreparePendingTempRunResume(out TempRunSaveStore.SaveData saveData)
    {
        saveData = null;

        if (!TempRunSaveStore.TryConsumePendingResume(out saveData) || saveData == null)
            return false;

        ApplyTempRunPersistentState(saveData);
        return true;
    }

    void ApplyTempRunPersistentState(TempRunSaveStore.SaveData saveData)
    {
        if (saveData == null)
            return;

        if (!string.IsNullOrWhiteSpace(saveData.selectedCharacterName) && roster != null)
        {
            for (int i = 0; i < roster.Length; i++)
            {
                var candidate = roster[i];
                if (!candidate || candidate.displayName != saveData.selectedCharacterName)
                    continue;

                SelectedCharacterStore.Current = candidate;
                SelectedCharacterStore.Save(candidate);
                break;
            }
        }

        if (PlayerProgress.I != null)
            PlayerProgress.I.SetSelectedStarDifficulty(saveData.starDifficulty);

        CurrencyStore.Total = Mathf.Max(0, saveData.currencyTotal);
        ApplySavedShopBuffLevels(saveData.shopBuffLevels);
        ApplyRunModsStoreSnapshot(saveData.runMods);
    }

    void ApplySavedShopBuffLevels(List<TempRunSaveStore.ShopBuffLevelEntry> levels)
    {
        var allTypes = ShopBuffStore.AllTypes;
        for (int i = 0; i < allTypes.Length; i++)
            ShopBuffStore.SetLevel(allTypes[i], 0);

        if (levels == null)
            return;

        for (int i = 0; i < levels.Count; i++)
        {
            var entry = levels[i];
            if (entry == null)
                continue;

            if (!System.Enum.IsDefined(typeof(ShopBuffType), entry.type))
                continue;

            ShopBuffStore.SetLevel((ShopBuffType)entry.type, entry.level);
        }
    }

    void ApplyRunModsStoreSnapshot(TempRunSaveStore.RunModsSnapshot snapshot)
    {
        RunModsStore.ResetAll();
        if (snapshot == null)
            return;

        RunModsStore.EnemyAttackIntervalMult = snapshot.enemyAttackIntervalMult;
        RunModsStore.EnemyProjectileDamageMult = snapshot.enemyProjectileDamageMult;
        RunModsStore.EnemyProjectileSpeedMult = snapshot.enemyProjectileSpeedMult;
        RunModsStore.SpecialGainMult = snapshot.specialGainMult;
        RunModsStore.SpecialDrainMult = snapshot.specialDrainMult;
        RunModsStore.SpecialBlockChanceAdd = snapshot.specialBlockChanceAdd;
        RunModsStore.PieceGravityMult = snapshot.pieceGravityMult;
        RunModsStore.FallRampRateMult = snapshot.fallRampRateMult;
        RunModsStore.MonsterDamageMult = snapshot.monsterDamageMult;
        RunModsStore.MonsterSpecialGainMult = snapshot.monsterSpecialGainMult;
        RunModsStore.MonsterMaxHpMult = snapshot.monsterMaxHpMult;
        RunModsStore.HealPowerMult = snapshot.healPowerMult;
        RunModsStore.HealRangeAdd = snapshot.healRangeAdd;
        RunModsStore.DisableNextPreview = snapshot.disableNextPreview;
        RunModsStore.DisableLandingHint = snapshot.disableLandingHint;
        RunModsStore.LineClearCurrencyChanceAdd = snapshot.lineClearCurrencyChanceAdd;
        RunModsStore.LineClearCurrencyAmountMult = snapshot.lineClearCurrencyAmountMult;
        RunModsStore.EnemyCastleHpMult = snapshot.enemyCastleHpMult;
        RunModsStore.Luck = snapshot.luck;
        RunModsStore.Misfortune = snapshot.misfortune;
        RunModsStore.StoneBuffDropChanceAdd = snapshot.stoneBuffDropChanceAdd;
        RunModsStore.StoneObstacleDropsDebuffsOnly = snapshot.stoneObstacleDropsDebuffsOnly;
        RunModsStore.ReserveUnitsRestoredOnWinAdd = snapshot.reserveUnitsRestoredOnWinAdd;
        RunModsStore.MaxReserveUnitsAdd = snapshot.maxReserveUnitsAdd;
        RunModsStore.DisableRoundWinReserveRestore = snapshot.disableRoundWinReserveRestore;
    }

    void ApplySelectedCharacterUI()
    {
        if (!selectedCharacter)
            return;

        if (playerPortrait && selectedCharacter.portrait)
            playerPortrait.sprite = selectedCharacter.portrait;

        if (playerBorder && selectedCharacter.defaultBorder)
            playerBorder.sprite = selectedCharacter.defaultBorder;

        if (playerName)
            playerName.text = TetrabeastsLocalization.LocalizeText(selectedCharacter.displayName);

        if (playerSpecialName)
            playerSpecialName.text = TetrabeastsLocalization.LocalizeText(selectedCharacter.specialAbilityName);
    }

    void StartFreshRun()
    {
        TutorialTestingScope.Reset();
        EnsureSpecialBlockTutorials();
        specialBlockTutorials?.ResetRunState();
        tutorialPieceInputBlocked = false;
        _tutorialHardDropInputBlockedUntilRealtime = 0f;
        HideRoundTransitionImmediate();

        currentLevel = 0;
        score = 0;
        _cachedTempRunCheckpoint = null;
        _roundRewardRerollsAvailable = 0;

        unitLives = EffectiveMaxUnitLives;
        SetupUnitLivesUI();

        ResetRunGridToBase();
        ApplyRunGridSize(currentLevel);
        InitLevel(currentLevel);

        RunMonsterProgress.BeginRun(GetActiveMonsterRoster());
        RunSummaryStats.BeginRun();

        _finalWinStateApplied = false;
        _postFinalSurvivalActive = false;
        _pendingPostFinalSurvivalIntro = false;
        _newDifficultyUnlockedThisRun = false;
        _firstFullRowTutorialShownThisRun = false;
        gameOver = false;
        levelWon = false;
        winQueued = false;
        _pendingMainMenuAfterXp = false;
        _demoLimitRunEnding = false;

        if (scoreUI) scoreUI.Set(score);

        ResetCombo();
        ResetBossGravityVisuals();

        StartCoroutine(BeginCurrentLevelSequence());
    }

    void RestoreTempRunCheckpoint(TempRunSaveStore.SaveData saveData)
    {
        HideRoundTransitionImmediate();

        if (saveData == null)
        {
            StartFreshRun();
            return;
        }

        _cachedTempRunCheckpoint = saveData;
        _postFinalSurvivalActive = saveData.postFinalSurvivalActive && postFinalSurvivalCastle != null;
        _pendingPostFinalSurvivalIntro = false;
        if (saveData.postFinalSurvivalActive && postFinalSurvivalCastle == null)
            Debug.LogWarning("GameController: temp run was saved in post-final survival, but no postFinalSurvivalCastle is assigned.");

        currentLevel = _postFinalSurvivalActive
            ? GetPostFinalSurvivalLevelIndex()
            : GetClampedSavedLevelIndex(saveData.currentLevel);
        score = Mathf.Max(0, saveData.score);
        luck = (saveData.runMods != null) ? saveData.runMods.luck : 0f;
        misfortune = (saveData.runMods != null) ? saveData.runMods.misfortune : 0f;
        _roundRewardRerollsAvailable = Mathf.Max(0, saveData.roundRewardRerollsAvailable);

        if (piece) piece.ResetPiece();
        if (gameBoard) gameBoard.ClearAll();
        if (highScoreUI) highScoreUI.Hide();
        if (victoryPanelUI) victoryPanelUI.Hide();

        gameOver = false;
        levelWon = false;
        winQueued = false;
        _pendingMainMenuAfterXp = false;
        _demoLimitRunEnding = false;
        _finalWinStateApplied = saveData.standardFinalWinApplied;
        _newDifficultyUnlockedThisRun = false;
        _firstFullRowTutorialShownThisRun = false;

        ResetRunGridToBase();
        ApplyRunGridSize(currentLevel);
        InitLevel(currentLevel);

        unitLives = Mathf.Clamp(saveData.unitLives, 0, EffectiveMaxUnitLives);
        SetupUnitLivesUI();

        RestoreActiveRunModifierLists(saveData);
        RunMonsterProgress.RestoreSnapshot(BuildRunMonsterSnapshot(saveData.runMonsterStates));
        RunSummaryStats.RestoreSerializableSnapshot(saveData.runSummary);
        PlayerProgress.I?.RestoreRunState(saveData.playerProgressRun);

        RunModsStore.Luck = EffectiveLuck;
        RunModsStore.Misfortune = CurrentMisfortune;

        if (scoreUI) scoreUI.Set(score);
        RunSummaryStats.SetFinalScore(score);
        SetSpecialGaugeImmediate(saveData.specialGauge);
        ResetCombo();
        ResetBossGravityVisuals();

        var restoredLevelModifier = ResolveSavedLevelModifier(saveData);
        int restoredRerolls = (saveData.levelModifier != null) ? saveData.levelModifier.availableRerolls : 0;
        StartCoroutine(BeginCurrentLevelSequence(useSavedLevelModifier: true,
            restoredLevelModifier: restoredLevelModifier,
            restoredRerolls: restoredRerolls));
    }

    int GetClampedSavedLevelIndex(int levelIndex)
    {
        if (castlesByLevel == null || castlesByLevel.Length == 0)
            return Mathf.Max(0, levelIndex);

        return Mathf.Clamp(levelIndex, 0, castlesByLevel.Length - 1);
    }

    void RestoreActiveRunModifierLists(TempRunSaveStore.SaveData saveData)
    {
        _runBuffs.Clear();
        _runDebuffs.Clear();
        RunModsStore.Buffs.Clear();
        RunModsStore.Debuffs.Clear();

        if (saveData == null)
            return;

        RestoreRunModifierList(saveData.buffModifierNames, _runBuffs, RunModsStore.Buffs);
        RestoreRunModifierList(saveData.debuffModifierNames, _runDebuffs, RunModsStore.Debuffs);
    }

    void RestoreRunModifierList(List<string> modifierNames, List<RunModifierSO> runtimeList, List<RunModifierSO> storeList)
    {
        if (modifierNames == null)
            return;

        for (int i = 0; i < modifierNames.Count; i++)
        {
            var resolved = ResolveRunModifierByName(modifierNames[i]);
            if (!resolved)
                continue;

            runtimeList.Add(resolved);
            storeList.Add(resolved);
            CodexProgressStore.Unlock(resolved);
        }
    }

    RunModifierSO ResolveRunModifierByName(string modifierName)
    {
        if (string.IsNullOrWhiteSpace(modifierName))
            return null;

        RunModifierSO resolved = FindRunModifierInPool(buffPool, modifierName);
        if (resolved) return resolved;

        resolved = FindRunModifierInPool(debuffPool, modifierName);
        if (resolved) return resolved;

        resolved = FindRunModifierInPool(stoneBuffPoolCommon, modifierName);
        if (resolved) return resolved;

        resolved = FindRunModifierInPool(stoneBuffPoolUncommon, modifierName);
        if (resolved) return resolved;

        resolved = FindRunModifierInPool(stoneBuffPoolRare, modifierName);
        if (resolved) return resolved;

        resolved = FindRunModifierInPool(stoneBuffPoolEpic, modifierName);
        if (resolved) return resolved;

        resolved = FindRunModifierInPool(stoneBuffPoolLegendary, modifierName);
        if (resolved) return resolved;

        resolved = FindRunModifierInPool(stoneDebuffPoolCommon, modifierName);
        if (resolved) return resolved;

        resolved = FindRunModifierInPool(stoneDebuffPoolUncommon, modifierName);
        if (resolved) return resolved;

        resolved = FindRunModifierInPool(stoneDebuffPoolRare, modifierName);
        if (resolved) return resolved;

        resolved = FindRunModifierInPool(stoneDebuffPoolEpic, modifierName);
        if (resolved) return resolved;

        return FindRunModifierInPool(stoneDebuffPoolLegendary, modifierName);
    }

    RunModifierSO FindRunModifierInPool(RunModifierSO[] pool, string modifierName)
    {
        if (pool == null)
            return null;

        for (int i = 0; i < pool.Length; i++)
        {
            var modifier = pool[i];
            if (!modifier)
                continue;

            if (modifier.name == modifierName || modifier.displayName == modifierName)
                return modifier;
        }

        return null;
    }

    Dictionary<string, RunMonsterProgress.RunState> BuildRunMonsterSnapshot(List<TempRunSaveStore.RunMonsterStateEntry> entries)
    {
        var snapshot = new Dictionary<string, RunMonsterProgress.RunState>();
        if (entries == null)
            return snapshot;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.monsterName))
                continue;

            snapshot[entry.monsterName] = new RunMonsterProgress.RunState
            {
                level = Mathf.Max(1, entry.level),
                xpInto = Mathf.Max(0f, entry.xpInto)
            };
        }

        return snapshot;
    }

    LevelModifierSO ResolveSavedLevelModifier(TempRunSaveStore.SaveData saveData)
    {
        if (saveData == null || saveData.levelModifier == null || levelModifierController == null)
            return null;

        return levelModifierController.ResolveModifierFromCurrentCastle(
            saveData.levelModifier.modifierName,
            saveData.levelModifier.modifierDisplayName);
    }

    void Update()
    {
        TetrabeastsControls.RefreshActiveInputProfile();
        RefreshGameplayControlTextsIfNeeded();
        UpdateTimedSlowGravityTimerUI();

        if (LoadingScreen.IsVisible)
            return;

        if (tutorialSuspended)
            return;

        if (_roundTransitionActive)
            return;

        if (_specialAbilityCinematicActive)
            return;

        if (TetrabeastsControls.WasPressed(TetrabeastsControlAction.Pause))
        {
            if (IsPauseMenuInputLocked())
                return;

            if (ConfirmationPopupUI.TryCancelShowingPopup())
                return;

            if (ShouldTutorialConsumeEscape())
                return;

            if (!isPaused)
            {
                PauseGame();
                EnterUICursorMode();
            }
            else
            {
                bool closedSomething = false; // Close any sub-panels opened from pause

                // Close Help panel first 
                if (helpPanelRoot && UIPanelTransition.IsVisible(helpPanelRoot))
                {
                    UIPanelTransition.Hide(helpPanelRoot);
                    closedSomething = true;
                }

                // Close Run Mods
                if (!closedSomething && runModsPanelRoot && UIPanelTransition.IsVisible(runModsPanelRoot))
                {
                    UIPanelTransition.Hide(runModsPanelRoot);
                    closedSomething = true;
                }

                // Close Gameplay Stats
                if (!closedSomething && gameplayStatsPanelUI && gameplayStatsPanelUI.IsVisible)
                {
                    gameplayStatsPanelUI.Close();
                    closedSomething = true;
                }

                // Close Volume settings panel
                if (!closedSomething && volumePanelInPause && UIPanelTransition.IsVisible(volumePanelInPause.gameObject))
                {
                    volumePanelInPause.Close();
                    closedSomething = true;
                }

                // If nothing else to close, resume the game
                if (!closedSomething)
                {
                    ResumeGame();
                    EnterGameplayCursorMode();
                }
            }

            RefreshPauseMenuNavigation();
            return;
        }

        if (isPaused)
        {
            if (IsPauseMenuInputLocked())
            {
                DisablePauseMenuNavigation();
                return;
            }

            RefreshPauseMenuNavigation();

            if (HandlePauseMenuCancelInput())
                return;

            return;
        }

        if (ConfirmationPopupUI.IsAnyShowing)
            return;

        DisablePauseMenuNavigation();

        if (TetrabeastsControls.WasPressed(TetrabeastsControlAction.Special))
            ActivateSpecial();

        if (IsRoundActive && !IsTutorialPromptActive)
            RunSummaryStats.AddActiveTime(Time.deltaTime);

        // Periodic castle projectile
        if (IsRoundActive && gameBoard)
        {
            if (!CanLaunchCastleProjectile())
            {
                _castleAttackTimer = 0f;
            }
            else
            {
                _castleAttackTimer += Time.deltaTime;

                float attackInterval = GetCurrentCastleAttackInterval();

                if (_castleAttackTimer >= Mathf.Max(0.1f, attackInterval))
                {
                    _castleAttackTimer = 0f;
                    TrySpawnCastleDownshot();
                }
            }
        }

        // Boss abilities loop
        if (IsRoundActive && gameBoard && _castleData != null && IsCastleBossForCurrentMode(_castleData))
        {
            _bossAbilityTimer += Time.deltaTime;
            if (_bossAbilityTimer >= _bossNextAbilityAt)
            {
                _bossAbilityTimer = 0f;
                _bossNextAbilityAt = Random.Range(_castleData.bossAbilityIntervalMin, _castleData.bossAbilityIntervalMax);
                TryCastRandomBossAbility();
            }
        }

        // Level timer and fall speed ramp 
        if (IsRoundActive)
        {
            bool blockGravityTimer = IsTutorialPromptActive;

            if (!blockGravityTimer)
                _levelTimer += Time.deltaTime;

            UpdateLevelTimerUI();

            // Keep the currently falling piece synced to the current interval
            float intervalNow = GetCurrentFallInterval();
            if (piece && piece.enabled)
            {
                piece.SetFallInterval(intervalNow, resetAccumulator: false);
                UpdateGravityText(intervalNow);
            }

            // Gravity cap accumulator only while active and not blocked by tutorial prompt
            if (!blockGravityTimer && intervalNow <= (CurrentGravityMinFallInterval + 0.0001f))
                _gravityCapAccumSeconds += Time.deltaTime;

            var playerProgress = PlayerProgress.I;
            if (playerProgress != null)
            {
                if (_gravityCapAccumSeconds >= 30f && playerProgress.GetRunInt(AchievementSystem.Stat.RunGravityCap30) == 0)
                    playerProgress.AddRunInt(AchievementSystem.Stat.RunGravityCap30, 1);

                if (_gravityCapAccumSeconds >= 60f && playerProgress.GetRunInt(AchievementSystem.Stat.RunGravityCap60) == 0)
                    playerProgress.AddRunInt(AchievementSystem.Stat.RunGravityCap60, 1);
            }

            // Time-based achievements (survive for X seconds in a level)
            if (playerProgress != null)
            {
                if (_levelTimer >= 180f && playerProgress.GetRunInt(AchievementSystem.Stat.RunLevelTime180) == 0)
                    playerProgress.AddRunInt(AchievementSystem.Stat.RunLevelTime180, 1);

                if (_levelTimer >= 240f && playerProgress.GetRunInt(AchievementSystem.Stat.RunLevelTime240) == 0)
                    playerProgress.AddRunInt(AchievementSystem.Stat.RunLevelTime240, 1);

                if (_levelTimer >= 300f && playerProgress.GetRunInt(AchievementSystem.Stat.RunLevelTime300) == 0)
                    playerProgress.AddRunInt(AchievementSystem.Stat.RunLevelTime300, 1);
            }
        }

        // Combo timer (clearing another row resets this timer)
        if (IsRoundActive && _comboCount > 0 && !_rowClearComboResolutionActive)
        {
            _comboTimer -= Time.deltaTime;
            if (_comboTimer <= 0f)
                ResetCombo();
        }

        // Boss shield state
        if (IsRoundActive && gameBoard)
            RefreshPylonShieldState();
    }

    void OnDestroy()
    {
        ClearPrewarmedSpecialAbilityPopup();

        SteamInputService.ControllerDisconnected -= HandleControllerDisconnected;
        SteamPlatformService.OverlayActiveChanged -= HandleSteamOverlayActiveChanged;
        TetrabeastsControls.ProfileChanged -= HandleControlsDisplayChanged;
        TetrabeastsControls.BindingsChanged -= HandleControlsDisplayChanged;
        TetrabeastsControls.PlatformDefaultProfileChanged -= HandleControlsDisplayChanged;

#if ENABLE_INPUT_SYSTEM
        InputSystem.onDeviceChange -= HandleInputDeviceChange;
#endif

        SettingsStore.CursorScaleChanged -= OnCursorScaleChanged;
    }

    void HandleControllerDisconnected()
    {
        PauseForControllerDisconnect();
    }

    void HandleSteamOverlayActiveChanged(bool active)
    {
        if (!active || !IsRoundActive)
            return;

        PauseGame();
        EnterUICursorMode();
    }

#if ENABLE_INPUT_SYSTEM
    void HandleInputDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (!(device is Gamepad))
            return;

        if (change != InputDeviceChange.Disconnected && change != InputDeviceChange.Removed &&
            TetrabeastsControls.TrySetActiveInputProfileFromDevice(device))
            RefreshGameplayControlTexts();

        if (change == InputDeviceChange.Disconnected || change == InputDeviceChange.Removed)
            PauseForControllerDisconnect();
    }
#endif

    void PauseForControllerDisconnect()
    {
        if (!IsRoundActive)
            return;

        PauseGame();
        EnterUICursorMode();
    }

    public float CurrentFallInterval => GetCurrentFallInterval();

    public float CurrentGravityRamp
    {
        get
        {
            float minInterval = CurrentGravityMinFallInterval;
            float baseInterval = Mathf.Max(minInterval, _thisLevelBaseFallInterval);
            float current = GetCurrentFallInterval();

            if (baseInterval <= minInterval + 0.0001f)
                return 1f;

            return Mathf.Clamp01(1f - Mathf.InverseLerp(baseInterval, minInterval, current));
        }
    }

    float GetRunEndXpConversionFraction(bool finalLevelWin)
    {
        float fraction = Mathf.Clamp01(baseRunEndXpConversion)
            + (Mathf.Max(0, _starDifficulty) * Mathf.Clamp01(starDifficultyRunEndXpBonusPerStar));

        if (finalLevelWin)
            fraction += Mathf.Clamp01(finalLevelWinXpConversionBonus);

        return Mathf.Clamp01(fraction);
    }

    bool HasReachedSuccessfulRunEnd()
    {
        return _finalWinStateApplied || _postFinalSurvivalActive;
    }

    void ResetRunMods()
    {
        _runBuffs.Clear();
        _runDebuffs.Clear();

        // Pull runtime values from the store
        enemyAttackIntervalMult = RunModsStore.EnemyAttackIntervalMult;
        enemyProjectileDamageMult = RunModsStore.EnemyProjectileDamageMult;
        enemyProjectileSpeedMult = RunModsStore.EnemyProjectileSpeedMult;

        specialGainMult = RunModsStore.SpecialGainMult;
        specialDrainMult = RunModsStore.SpecialDrainMult;
        specialBlockChanceAdd = RunModsStore.SpecialBlockChanceAdd;

        pieceGravityMult = RunModsStore.PieceGravityMult;
        fallRampRateMult = RunModsStore.FallRampRateMult;

        monsterDamageMult = RunModsStore.MonsterDamageMult;
        monsterSpecialGainMult = RunModsStore.MonsterSpecialGainMult;
        monsterMaxHpMult = RunModsStore.MonsterMaxHpMult;

        healPowerMult = RunModsStore.HealPowerMult;
        healRangeAdd = RunModsStore.HealRangeAdd;

        disableNextPreview = RunModsStore.DisableNextPreview;
        disableLandingHint = RunModsStore.DisableLandingHint;

        lineClearCurrencyChanceAdd = RunModsStore.LineClearCurrencyChanceAdd;
        lineClearCurrencyAmountMult = RunModsStore.LineClearCurrencyAmountMult;

        enemyCastleHpMult = RunModsStore.EnemyCastleHpMult;
        stoneBuffDropChanceAdd = RunModsStore.StoneBuffDropChanceAdd;
        stoneObstacleDropsDebuffsOnly = RunModsStore.StoneObstacleDropsDebuffsOnly;
        reserveUnitsRestoredOnWinAdd = RunModsStore.ReserveUnitsRestoredOnWinAdd;
        maxReserveUnitsAdd = RunModsStore.MaxReserveUnitsAdd;
        disableRoundWinReserveRestore = RunModsStore.DisableRoundWinReserveRestore;
    }

    void RefreshStarDifficultyState()
    {
        _starDifficulty = (PlayerProgress.I != null) ? PlayerProgress.I.GetSelectedStarDifficulty() : 0;
        _starDifficultyModifiers = StarDifficultySystem.GetModifiers(_starDifficulty);
    }

    float GetEffectiveSpecialGaugeGainMultiplier()
    {
        return Mathf.Max(0f, specialGainMult * _starDifficultyModifiers.specialGaugeGainMultiplier);
    }

    int GetScaledScorePoints(int points)
    {
        int clampedPoints = Mathf.Max(0, points);
        if (clampedPoints <= 0)
            return 0;

        return Mathf.Max(0, Mathf.RoundToInt(clampedPoints * _starDifficultyModifiers.scoreGainMultiplier));
    }

    public float GetScaledEnemyDamage(float amount)
    {
        return Mathf.Max(0f, amount) * _starDifficultyModifiers.enemyDamageMultiplier;
    }

    public float GetScaledFloorEffectDamage(float amount)
    {
        float levelMultiplier = 1f + (Mathf.Max(0, currentLevel) * Mathf.Max(0f, floorEffectDamageIncreasePerLevel));
        return GetScaledEnemyDamage(amount) * levelMultiplier;
    }

    float GetCurrentCastleAttackInterval()
    {
        float baseInterval = Mathf.Max(0.1f, castleAttackInterval);

        if (!enableEnemyAttackIntervalRamp)
            return baseInterval;

        float elapsed = Mathf.Max(0f, _levelTimer - enemyAttackIntervalRampDelaySeconds);
        if (elapsed <= 0f)
            return baseInterval;

        float t = Mathf.Clamp01(elapsed / Mathf.Max(1f, enemyAttackIntervalRampDurationSeconds));
        float curveT = enemyAttackIntervalRampCurve != null
            ? Mathf.Clamp01(enemyAttackIntervalRampCurve.Evaluate(t))
            : t * t * (3f - 2f * t);
        float multiplier = Mathf.Lerp(1f, Mathf.Clamp(enemyAttackIntervalRampEndMultiplier, 0.25f, 1f), curveT);
        return Mathf.Max(0.1f, baseInterval * multiplier);
    }

    bool HandleStarDifficultyFinalWin()
    {
        if (PlayerProgress.I == null)
            return false;

        if (_starDifficulty > 0)
        {
            string achievementId = StarDifficultySystem.GetAchievementId(_starDifficulty);
            if (!string.IsNullOrEmpty(achievementId))
                PlayerProgress.I.UnlockAchievement(achievementId);
        }

        if (_starDifficulty >= StarDifficultySystem.MaxStars)
            return false;

        if (PlayerProgress.I.GetMaxUnlockedStarDifficulty() == _starDifficulty)
        {
            PlayerProgress.I.TryUnlockStarDifficulty(_starDifficulty + 1);
            return true;
        }

        return false;
    }

    void RefreshActiveMonsterPassives(bool applyStartingReserveDelta)
    {
        int previousStartingReserveBonus = _partyPassiveBonuses.startingReserveUnits;
        _partyPassiveBonuses = MonsterPassiveSystem.GetCombinedBonuses(GetActiveMonsterRoster());

        if (!applyStartingReserveDelta)
            return;

        int delta = _partyPassiveBonuses.startingReserveUnits - previousStartingReserveBonus;
        if (delta > 0)
            unitLives += delta;

        unitLives = Mathf.Clamp(unitLives, 0, EffectiveMaxUnitLives);
        UpdateUnitLivesUI();
    }

    void ShowNextPreview()
    {
        if (!nextPreview) return;

        if (disableNextPreview)
        {
            nextPreview.ClearPreview();
            return;
        }

        EnsureMinBag(3);
        var next = PeekSafeHead();
        if (next == null) return;

        // Aligned partner for preview
        var m = (monstersBag.Count > 0) ? monstersBag.Peek() : null;
        if (next.special == SpecialType.None)
        {
            if (m == null || m.Length != next.cells.Length)
            {
                var roster = GetActiveMonsterRoster();
                var chosen = WeightedPick(roster);
                var rebuilt = new MonsterData[next.cells.Length];
                for (int i = 0; i < rebuilt.Length; i++) rebuilt[i] = chosen;
                m = rebuilt;
            }
        }
        else
        {
            m = System.Array.Empty<MonsterData>();
        }

        nextPreview.Show(next, next.color, m);

        if (nextPreview)
            nextPreview.SyncBorderToImmunity(immunityActive, gameBoard.immuneBorderColor, gameBoard.normalBorderColor);
    }

    public bool CanSpawnNewPiece() => !gameOver && !levelWon && !_levelStartBlocked && !LoadingScreen.IsVisible;

    public void SpawnNextPiece()
    {
        if (gameOver || levelWon) return;

        // Make sure there is a valid, non-null head before dequeue
        var head = PeekSafeHead();
        if (head == null) { return; } // Nothing valid to spawn; caller will try again later

        // Dequeue the aligned pair
        var data = bag.Dequeue();
        var mons = monstersBag.Count > 0 ? monstersBag.Dequeue() : null;

        // Heal monster array for normal pieces
        if (data.special == SpecialType.None)
        {
            if (mons == null || mons.Length != data.cells.Length)
            {
                var roster = GetActiveMonsterRoster();
                var chosen = WeightedPick(roster);
                mons = new MonsterData[data.cells.Length];
                for (int i = 0; i < mons.Length; i++) mons[i] = chosen;
            }
        }
        else
        {
            mons = System.Array.Empty<MonsterData>();
        }

        // Spawn piece
        piece.data = data;
        piece.color = data.color;
        piece.SetMonsters(mons);
        piece.enabled = true;
        piece.SpawnAtTop();
        piece.SetFallInterval(GetCurrentFallInterval(), resetAccumulator: true);
        QueueSpecialBlockTutorialIfNeeded(data);

        // Keep bags topped and refresh preview after consuming one
        EnsureMinBag(3);
        ShowNextPreview();
    }

    IEnumerator BeginCurrentLevelSequence(bool useSavedLevelModifier = false,
        LevelModifierSO restoredLevelModifier = null, int restoredRerolls = 0,
        bool keepRoundRewardVisibleUntilLevelModifierPanelShown = false)
    {
        _levelStartBlocked = true;

        bag.Clear();
        monstersBag.Clear();

        bool roundRewardHiddenByLevelModifierPanel = false;
        void HideRoundRewardPanelAfterLevelModifierPanelShown()
        {
            if (roundRewardHiddenByLevelModifierPanel)
                return;

            roundRewardHiddenByLevelModifierPanel = true;
            if (roundRewardUI)
                roundRewardUI.Hide();
        }

        if (levelModifierController)
        {
            if (useSavedLevelModifier)
            {
                levelModifierController.RestoreCheckpointState(restoredLevelModifier, restoredRerolls);
            }
            else
            {
                yield return levelModifierController.BeginLevel(
                    currentCastleData,
                    keepRoundRewardVisibleUntilLevelModifierPanelShown
                        ? HideRoundRewardPanelAfterLevelModifierPanelShown
                        : null);
            }
        }

        if (keepRoundRewardVisibleUntilLevelModifierPanelShown && !roundRewardHiddenByLevelModifierPanel && roundRewardUI)
            roundRewardUI.Hide();

        yield return CoShowLevelStartTransition();

        RefillBag(forceFirstEntryNormal: true);
        EnsureMinBag(3);
        ShowNextPreview();
        CacheAndWriteTempRunCheckpoint();

        _levelStartBlocked = false;

        bool spawnedPiece = false;
        if (CanSpawnNewPiece())
        {
            SpawnNextPiece();
            spawnedPiece = piece && piece.HasActiveCells;
        }

        if (_pendingPostFinalSurvivalIntro && _postFinalSurvivalActive && spawnedPiece)
            StartCoroutine(CoShowPostFinalSurvivalIntroAfterFirstPiece());
    }

    IEnumerator CoShowLevelStartTransition()
    {
        yield return WaitForLoadingScreenToClose();

        PauseGameplayForRoundTransition(showCursor: false);

        if (levelStartRoundTransitionDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(levelStartRoundTransitionDelaySeconds);

        int levelNumber = currentLevel + 1;
        string castleName = currentCastleData && !string.IsNullOrWhiteSpace(currentCastleData.castleName)
            ? TetrabeastsLocalization.LocalizeText(currentCastleData.castleName)
            : TetrabeastsLocalization.LocalizeText("Castle");

        yield return CoShowTimedRoundTransition($"{TetrabeastsLocalization.LocalizeFormat("Level {0}", levelNumber)}\n{castleName}");

        ResumeGameplayAfterRoundTransition(showCursor: false);
        EnterGameplayCursorMode();
    }

    void CacheAndWriteTempRunCheckpoint()
    {
        _cachedTempRunCheckpoint = BuildTempRunCheckpointData();
        TempRunSaveStore.Save(_cachedTempRunCheckpoint);
        PlayerPrefs.Save();
    }

    TempRunSaveStore.SaveData BuildTempRunCheckpointData()
    {
        var data = new TempRunSaveStore.SaveData
        {
            currentLevel = currentLevel,
            postFinalSurvivalActive = _postFinalSurvivalActive,
            standardFinalWinApplied = _finalWinStateApplied,
            score = Mathf.Max(0, score),
            unitLives = Mathf.Max(0, unitLives),
            specialGauge = Mathf.Max(0f, specialGauge),
            starDifficulty = _starDifficulty,
            selectedCharacterName = selectedCharacter ? selectedCharacter.displayName : string.Empty,
            currencyTotal = Mathf.Max(0, CurrencyStore.Total),
            roundRewardRerollsAvailable = Mathf.Max(0, _roundRewardRerollsAvailable),
            runMods = new TempRunSaveStore.RunModsSnapshot
            {
                enemyAttackIntervalMult = enemyAttackIntervalMult,
                enemyProjectileDamageMult = enemyProjectileDamageMult,
                enemyProjectileSpeedMult = enemyProjectileSpeedMult,
                specialGainMult = specialGainMult,
                specialDrainMult = specialDrainMult,
                specialBlockChanceAdd = specialBlockChanceAdd,
                pieceGravityMult = pieceGravityMult,
                fallRampRateMult = fallRampRateMult,
                monsterDamageMult = monsterDamageMult,
                monsterSpecialGainMult = monsterSpecialGainMult,
                monsterMaxHpMult = monsterMaxHpMult,
                healPowerMult = healPowerMult,
                healRangeAdd = healRangeAdd,
                disableNextPreview = disableNextPreview,
                disableLandingHint = disableLandingHint,
                lineClearCurrencyChanceAdd = lineClearCurrencyChanceAdd,
                lineClearCurrencyAmountMult = lineClearCurrencyAmountMult,
                enemyCastleHpMult = enemyCastleHpMult,
                luck = luck,
                misfortune = misfortune,
                stoneBuffDropChanceAdd = stoneBuffDropChanceAdd,
                stoneObstacleDropsDebuffsOnly = stoneObstacleDropsDebuffsOnly,
                reserveUnitsRestoredOnWinAdd = reserveUnitsRestoredOnWinAdd,
                maxReserveUnitsAdd = maxReserveUnitsAdd,
                disableRoundWinReserveRestore = disableRoundWinReserveRestore
            },
            playerProgressRun = PlayerProgress.I != null ? PlayerProgress.I.CaptureRunState() : new PlayerProgress.RunStateSnapshot(),
            runSummary = RunSummaryStats.CaptureSerializableSnapshot(),
            levelModifier = new TempRunSaveStore.LevelModifierCheckpointData
            {
                modifierName = (levelModifierController && levelModifierController.ActiveModifier)
                    ? levelModifierController.ActiveModifier.name
                    : string.Empty,
                modifierDisplayName = (levelModifierController && levelModifierController.ActiveModifier)
                    ? levelModifierController.ActiveModifier.displayName
                    : string.Empty,
                availableRerolls = levelModifierController ? levelModifierController.AvailableRerolls : 0
            }
        };

        var activeRoster = GetActiveMonsterRoster();
        if (activeRoster != null)
        {
            for (int i = 0; i < activeRoster.Count; i++)
            {
                var monster = activeRoster[i];
                if (monster && !string.IsNullOrWhiteSpace(monster.monsterName))
                    data.selectedMonsterNames.Add(monster.monsterName);
            }
        }

        var allTypes = ShopBuffStore.AllTypes;
        for (int i = 0; i < allTypes.Length; i++)
        {
            data.shopBuffLevels.Add(new TempRunSaveStore.ShopBuffLevelEntry
            {
                type = (int)allTypes[i],
                level = ShopBuffStore.GetLevel(allTypes[i])
            });
        }

        for (int i = 0; i < RunModsStore.Buffs.Count; i++)
        {
            var buff = RunModsStore.Buffs[i];
            if (buff)
                data.buffModifierNames.Add(buff.name);
        }

        for (int i = 0; i < RunModsStore.Debuffs.Count; i++)
        {
            var debuff = RunModsStore.Debuffs[i];
            if (debuff)
                data.debuffModifierNames.Add(debuff.name);
        }

        var runMonsterSnapshot = RunMonsterProgress.GetSnapshot();
        foreach (var kv in runMonsterSnapshot)
        {
            data.runMonsterStates.Add(new TempRunSaveStore.RunMonsterStateEntry
            {
                monsterName = kv.Key,
                level = kv.Value.level,
                xpInto = kv.Value.xpInto
            });
        }

        return data;
    }

    bool SaveTempRunForQuit()
    {
        if (_cachedTempRunCheckpoint == null)
            _cachedTempRunCheckpoint = BuildTempRunCheckpointData();

        if (_cachedTempRunCheckpoint == null)
            return false;

        TempRunSaveStore.Save(_cachedTempRunCheckpoint);
        PlayerPrefs.Save();
        PlayerProgress.I?.EndRun();
        return TempRunSaveStore.HasValidSave();
    }

    void ClearTempRunCheckpoint()
    {
        _cachedTempRunCheckpoint = null;
        TempRunSaveStore.Delete();
    }

    void ResolveRoundTransitionUI()
    {
        if (roundTransitionUI)
            return;

        roundTransitionUI = FindFirstObjectByType<RoundTransitionUI>(FindObjectsInactive.Include);
        if (!roundTransitionUI)
            roundTransitionUI = RoundTransitionUI.CreateRuntimeInstance(roundTransitionFont, roundTransitionContinueButtonPrefab);

        roundTransitionUI.Configure(roundTransitionFont, roundTransitionContinueButtonPrefab);
    }

    void PauseGameplayForRoundTransition(bool showCursor = true)
    {
        ResolveRoundTransitionUI();

        _roundTransitionActive = true;
        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = false;

        if (pausePanel)
            UIPanelTransition.Hide(pausePanel, true);

        if (showCursor)
        {
            EnterUICursorMode();
            StartCoroutine(ReapplyUICursorNextFrame());
        }
        else
        {
            EnterGameplayCursorMode();
            StartCoroutine(ReapplyGameplayCursorNextFrame());
        }
    }

    void PauseGameplayForBlockingPopup()
    {
        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = false;

        if (pausePanel)
            UIPanelTransition.Hide(pausePanel, true);

        EnterUICursorMode();
        StartCoroutine(ReapplyUICursorNextFrame());
    }

    void ResumeGameplayAfterRoundTransition(bool showCursor = true)
    {
        _roundTransitionActive = false;
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (showCursor)
        {
            EnterUICursorMode();
            StartCoroutine(ReapplyUICursorNextFrame());
        }
        else
        {
            EnterGameplayCursorMode();
            StartCoroutine(ReapplyGameplayCursorNextFrame());
        }
    }

    void FinishRoundTransitionAndKeepGameplayPaused(bool showCursor = true)
    {
        _roundTransitionActive = false;
        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = false;

        if (showCursor)
        {
            EnterUICursorMode();
            StartCoroutine(ReapplyUICursorNextFrame());
        }
        else
        {
            EnterGameplayCursorMode();
            StartCoroutine(ReapplyGameplayCursorNextFrame());
        }
    }

    IEnumerator CoShowRoundTransition(string message)
    {
        yield return CoShowRoundTransition(message, RoundTransitionVariant.Default);
    }

    IEnumerator CoShowRoundTransition(string message, RoundTransitionVariant variant)
    {
        yield return CoShowRoundTransition(message, variant, string.Empty, false, null);
    }

    IEnumerator CoShowRoundTransition(string message, RoundTransitionVariant variant, string claimedOneLiner)
    {
        yield return CoShowRoundTransition(message, variant, string.Empty, false, null, claimedOneLiner);
    }

    IEnumerator CoShowRoundTransition(string message, string optOutLabel, bool optOutInitialValue,
                                      System.Action<bool> onOptOutContinue)
    {
        yield return CoShowRoundTransition(message, RoundTransitionVariant.Default, optOutLabel, optOutInitialValue, onOptOutContinue);
    }

    IEnumerator CoShowRoundTransition(string message, RoundTransitionVariant variant, string optOutLabel, bool optOutInitialValue,
                                      System.Action<bool> onOptOutContinue, string claimedOneLiner = "")
    {
        yield return WaitForLoadingScreenToClose();

        ResolveRoundTransitionUI();

        bool continued = false;
        if (roundTransitionUI)
            roundTransitionUI.Show(
                TetrabeastsLocalization.LocalizeText(message),
                () => continued = true,
                TetrabeastsLocalization.LocalizeText(optOutLabel),
                optOutInitialValue,
                onOptOutContinue,
                variant,
                claimedOneLiner);
        else
            continued = true;

        yield return new WaitUntil(() => continued);
    }

    string ClaimRoundTransitionOneLiner(RoundTransitionVariant variant)
    {
        ResolveRoundTransitionUI();
        return roundTransitionUI ? roundTransitionUI.ClaimOneLiner(variant) : string.Empty;
    }

    IEnumerator CoShowTimedRoundTransition(string message)
    {
        yield return WaitForLoadingScreenToClose();

        ResolveRoundTransitionUI();

        bool completed = false;
        if (roundTransitionUI)
            roundTransitionUI.ShowTimed(TetrabeastsLocalization.LocalizeText(message), () => completed = true);
        else
            completed = true;

        yield return new WaitUntil(() => completed);
    }

    IEnumerator WaitForLoadingScreenToClose()
    {
        while (LoadingScreen.IsVisible)
            yield return null;
    }

    void HideRoundTransitionImmediate()
    {
        _roundTransitionActive = false;

        if (roundTransitionUI)
            roundTransitionUI.HideImmediate();
    }

    public void GameOver()
    {
        if (gameOver)
            return;

        gameOver = true;
        _claimedRoundLossOneLiner = ClaimRoundTransitionOneLiner(RoundTransitionVariant.Loss);
        ClearTempRunCheckpoint();
        Debug.Log("Game Over");

        if (AudioManager.I)
        {
            AudioManager.I.StopPauseMusic();
            AudioManager.I.StopLevelMusic();
        }
        AudioListener.pause = false;

        if (AudioManager.I) AudioManager.I.PlaySFX(AudioManager.I.sfxGameOver);

        if (PlayerProgress.I)
        {
            PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.Losses, 1);
            PlayerProgress.I.EndRun();
        }

        StartCoroutine(CoShowGameOverTransitionThenHighScore());
    }

    IEnumerator CoShowGameOverTransitionThenHighScore()
    {
        PauseGameplayForRoundTransition();

        string claimedOneLiner = _claimedRoundLossOneLiner;
        _claimedRoundLossOneLiner = string.Empty;

        yield return CoShowRoundTransition(RoundLossTransitionText, RoundTransitionVariant.Loss, claimedOneLiner);
        ResumeGameplayAfterRoundTransition();

        ShowRunEndCommitAfterLoss();
    }

    void ShowRunEndCommitAfterLoss()
    {
        bool skipVisibleRunXp = currentLevel <= 0 || !RunMonsterProgress.RunActive;
        bool successfulRunReached = HasReachedSuccessfulRunEnd();

        if (!skipVisibleRunXp && xpAwardUI)
        {
            OpenXpUiMode();

            if (AudioManager.I)
                AudioManager.I.PlayIntermissionLoseMusic();

            xpAwardUI.ShowRunEndCommit(
                GetActiveMonsterRoster(),
                GetRunEndXpConversionFraction(successfulRunReached),
                () =>
                {
                    ShowFinalStatsBeforeHighScore(ShowGameOverLocalHighScore, CloseAndHideXpUiMode);
                },
                hideOnFinalContinue: false);

            return;
        }

        try
        {
            CommitRunEndXpSilently(successfulRunReached);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }

        ShowGameOverHighScore();
    }

    void ShowGameOverHighScore()
    {
        if (AudioManager.I)
            AudioManager.I.PlayIntermissionLoseMusic();

        EnsureHighScoreUI();
        ShowFinalStatsBeforeHighScore(ShowGameOverLocalHighScore);
    }

    void ShowGameOverLocalHighScore()
    {
        SubmitSteamLeaderboardScore();

        if (EnsureHighScoreUI())
        {
            highScoreUI.SetRestartButtonSuppressed(false);
            highScoreUI.TryShow(score);
        }

        if (restartButton)
            restartButton.gameObject.SetActive(true);

        EnterUICursorMode();
    }

    bool EnsureHighScoreUI()
    {
        if (!highScoreUI)
            highScoreUI = FindFirstObjectByType<HighScoreUI>(FindObjectsInactive.Include);

        if (!highScoreUI)
        {
            Debug.LogWarning("GameController: HighScoreUI was not found in the scene. Assign the local high score panel in the inspector.");
            return false;
        }

        return true;
    }

    void CommitRunEndXpSilently(bool finalLevelWin)
    {
        var kept = RunMonsterProgress.EndRunAndComputeKeptXp(GetRunEndXpConversionFraction(finalLevelWin));
        foreach (var kv in kept)
            MonsterProgressStore.AddPermanentXp(kv.Key, kv.Value);
    }

    void SubmitSteamLeaderboardScore()
    {
        if (score <= 0)
            return;

        SteamLeaderboardService.Ensure().SubmitScore(score, selectedCharacter, roster);
    }

    public void PlayPieceLockSFX()
    {
        if (!AudioManager.I || pieceLockSfxClips == null || pieceLockSfxClips.Length == 0)
            return;

        AudioClip clip = null;
        for (int tries = 0; tries < pieceLockSfxClips.Length; tries++)
        {
            clip = pieceLockSfxClips[UnityEngine.Random.Range(0, pieceLockSfxClips.Length)];
            if (clip)
                break;
        }

        if (clip)
        {
            float pitch = 1f + UnityEngine.Random.Range(-pieceLockSfxPitchJitter, pieceLockSfxPitchJitter);
            float volumeScale = 1f + UnityEngine.Random.Range(-pieceLockSfxVolumeJitter, pieceLockSfxVolumeJitter);
            AudioManager.I.PlaySFX(clip, pieceLockSfxVolume * volumeScale, pitch, jitter: false);
        }
    }

    void RefillBag(bool forceFirstEntryNormal = false)
    {
        // Validate source arrays to avoid null floods
        var normals = new List<TetrominoData>();
        for (int i = 0; i < allTetrominoes.Length; i++)
            if (allTetrominoes[i] != null) normals.Add(allTetrominoes[i]);

        if (normals.Count == 0)
        {
            Debug.LogError("RefillBag: allTetrominoes has no valid entries.");
            return;
        }

        // Fisher Yates shuffle
        for (int i = normals.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (normals[i], normals[j]) = (normals[j], normals[i]);
        }

        // Build weighted index for specials once
        float specialTotal = 0f;
        if (specialBlocks != null)
            for (int i = 0; i < specialBlocks.Length; i++)
                if (specialBlocks[i]) specialTotal += Mathf.Max(0f, specialBlocks[i].spawnWeight);

        bool specialsAvailable = specialBlocks != null && specialBlocks.Length > 0 && specialTotal > 0f;

        var roster = GetActiveMonsterRoster();
        int specialsAddedThisRefill = 0;

        float chanceCap = Mathf.Max(0f, maxSpecialChance);
        float chanceFloor = Mathf.Min(Mathf.Clamp01(minSpecialChance), chanceCap);
        float chance = Mathf.Clamp(specialChancePerEnqueue + specialBlockChanceAdd, chanceFloor, chanceCap);

        foreach (var d in normals)
        {
            TetrominoData use = d;
            bool isFirstBagEntry = forceFirstEntryNormal && bag.Count == 0;

            if (!isFirstBagEntry &&
                specialsAvailable &&
                !(levelModifierController && levelModifierController.BlocksSpecialPieceSpawns) &&
                Random.value < chance)
            {
                float r = Random.Range(0f, specialTotal);
                for (int k = 0; k < specialBlocks.Length; k++)
                {
                    var sp = specialBlocks[k];
                    if (!sp) continue;
                    float w = Mathf.Max(0f, sp.spawnWeight);
                    if ((r -= w) <= 0f)
                    {
                        use = sp;
                        specialsAddedThisRefill++;
                        break;
                    }
                }
            }

            bag.Enqueue(use); // Enqueue selected piece

            // Enqueue monsters array in parallel
            int cellsCount = Mathf.Max(1, use.cells != null ? use.cells.Length : 1);
            MonsterData[] arr;
            if (use.special == SpecialType.None)
            {
                var chosen = WeightedPick(roster);
                arr = new MonsterData[cellsCount];
                for (int k = 0; k < cellsCount; k++) arr[k] = chosen;
            }
            else
            {
                arr = System.Array.Empty<MonsterData>();
            }

            monstersBag.Enqueue(arr);
        }

        // Guarantee at least one special per refill batch
        if (forceOneSpecialPerRefill &&
            specialsAvailable &&
            specialsAddedThisRefill == 0 &&
            !(levelModifierController && levelModifierController.BlocksSpecialPieceSpawns))
        {
            // Append one extra special at end of queues
            float r = Random.Range(0f, specialTotal);
            TetrominoData spPick = null;
            for (int k = 0; k < specialBlocks.Length; k++)
            {
                var sp = specialBlocks[k];
                if (!sp) continue;
                float w = Mathf.Max(0f, sp.spawnWeight);
                if ((r -= w) <= 0f) { spPick = sp; break; }
            }
            if (spPick != null)
            {
                bag.Enqueue(spPick);
                monstersBag.Enqueue(System.Array.Empty<MonsterData>());
            }
        }
    }

    public void OnPieceLocked(int rowsCleared, List<Vector2Int> removedCells, int damageFromMonsters,
                          float specialChargeFromMonsters, Dictionary<int, int> rowDamage,
                          Dictionary<int, MonsterData> rowDominantMonster, Dictionary<int,
                              List<int>> colsByRow)
    {
        if (rowsCleared > 0 && AudioManager.I) // Only play if at least one row was cleared
            AudioManager.I.PlayRandomLineClear();

        if (rowsCleared > 0 && PlayerProgress.I) // Track total row clears for achievements
            PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.TotalRowClears, rowsCleared);

        if (rowsCleared > 0)
            RunSummaryStats.AddLinesCleared(rowsCleared);

        // Special gauge
        if (levelModifierController)
            specialChargeFromMonsters = levelModifierController.ModifySpecialGaugeGain(specialChargeFromMonsters);

        specialChargeFromMonsters *= GetEffectiveSpecialGaugeGainMultiplier();

        if (specialChargeFromMonsters > 0f)
        {
            specialGauge = Mathf.Min(specialGaugeMax, specialGauge + specialChargeFromMonsters);
            UpdateSpecialUI();
        }

        levelModifierController?.OnRowsResolved(rowDamage);

        // Chance-based currency drops
        if (rowsCleared > 0)
        {
            for (int i = 0; i < rowsCleared; i++)
            {
                float chance = EffectiveCurrencyChancePerClearedRow;

                if (Random.value <= chance)
                {
                    int amount = Mathf.Max(1, Mathf.RoundToInt(1f * lineClearCurrencyAmountMult * CurrentCurrencyGainMultiplier));
                    CurrencyStore.Add(amount);

                    if (PlayerProgress.I && amount > 0) // Track total gold earned for achievements
                        PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.GoldEarned, amount);

                    if (currencyUI) currencyUI.Refresh();

                    // Pick a cleared row
                    int rowY = i;
                    if (colsByRow != null && colsByRow.Count > 0)
                    {
                        foreach (var kv in colsByRow) { rowY = kv.Key; break; }
                    }

                    // Compute start just outside the grid's right edge, same row (in board/grid space)
                    Vector2 startBoardRight = BoardRightGutterY(rowY, -2.5f);

                    // Convert to projectile root space if different
                    var root = projectileRoot ? projectileRoot : gameBoard.gridRoot;
                    Vector2 start = (root == gameBoard.gridRoot)
                        ? startBoardRight
                        : LocalTo(root, gameBoard.gridRoot, startBoardRight);

                    if (AudioManager.I && sfxCurrencyLineGain)
                        AudioManager.I.PlaySFX(sfxCurrencyLineGain);

                    ShowCurrencyPopup(start, amount);
                }
            }
        }

        // Spawn one projectile per cleared row
        if (rowsCleared > 0 && gameBoard && enemyCastleUI && enemyCastleUI.castleImage && rowDamage != null)
        {
            // Sort by clearIndex so projectiles fire in the same order rows were cleared
            var ordered = new List<KeyValuePair<int, int>>(rowDamage);
            ordered.Sort((a, b) => (a.Key / 1000).CompareTo(b.Key / 1000));

            var usedCols = new HashSet<int>();

            foreach (var kv in ordered)
            {
                int rowKey = kv.Key;
                int rowY = rowKey % 1000;
                rowY = Mathf.Clamp(rowY, 0, gameBoard.height - 1);
               
                int dmg = Mathf.Max(0, kv.Value); // Damage carried by this projectile only (per cleared row)

                if (PlayerProgress.I) // Track best single attack damage for achievements
                    PlayerProgress.I.SetRunBestInt(AchievementSystem.Stat.RunMaxSingleAttackDmg, dmg);

                // Use dominant monster for this row pick attack sprite
                Sprite attackSprite = null;
                Sprite attackSpriteAlt = null;
                AttackAnimType animType = AttackAnimType.None;
                MonsterData attackerMD = null;

                if (rowDominantMonster != null && rowDominantMonster.TryGetValue(rowKey, out var md) && md)
                {
                    attackerMD = md;
                    animType = md.attackAnim;

                    // Choose sprite based on selected skin
                    attackSprite = MonsterSkinStore.GetAttackSprite(md);
                    attackSpriteAlt = MonsterSkinStore.GetAttackAltSprite(md);
                }

                // Preferred start column: last-placed piece's columns on this visual row (if known)
                int preferredCol = UnityEngine.Random.Range(0, gameBoard.width);
                if (colsByRow != null && colsByRow.TryGetValue(rowY, out var cols) && cols != null && cols.Count > 0)
                    preferredCol = cols[UnityEngine.Random.Range(0, cols.Count)];

                // If multiple projectiles would spawn in the same column, shift the later one to the nearest free column
                int col = preferredCol;
                if (usedCols.Contains(col))
                {
                    for (int step = 1; step < gameBoard.width; step++)
                    {
                        int r = preferredCol + step;
                        if (r < gameBoard.width && !usedCols.Contains(r)) { col = r; break; }

                        int l = preferredCol - step;
                        if (l >= 0 && !usedCols.Contains(l)) { col = l; break; }
                    }
                }

                usedCols.Add(col);

                Vector2 startBoard = gameBoard.CellToAnchoredPos(new Vector2Int(col, rowY));

                // Convert start to projectileRoot space
                var root = projectileRoot ? projectileRoot : gameBoard.gridRoot;
                Vector2 start = LocalTo(root, gameBoard.gridRoot, startBoard);

                Vector3 castleBottomWorld = enemyCastleUI.castleImage.rectTransform.TransformPoint(
                    new Vector3(0f, -enemyCastleUI.castleImage.rectTransform.rect.height * 0.5f, 0f));

                Vector2 castleBottomInRoot = root.InverseTransformPoint(castleBottomWorld);
                Vector2 target = new Vector2(start.x, castleBottomInRoot.y);

                // Debug output for attack damage and source monster
                if (logRowDamageBreakdown)
                {
                    int clearIndex = rowKey / 1000;
                    Debug.Log(
                        $"[CastleAttack Spawn] clearIndex={clearIndex} rowY={rowY} " +
                        $"dmg={dmg} attacker={(attackerMD ? attackerMD.name : "None")}"
                    );
                }

                SpawnAttackProjectile(attackSprite, attackSpriteAlt, animType, start, target, dmg, attackerMD);
            }
        }

        // No immediate damage, damage applies on impact
    }

    bool ResolveFullRowsAfterBossObstacleSpawn(Dictionary<int, List<int>> clearOriginColumnsByRow)
    {
        if (!gameBoard || clearOriginColumnsByRow == null || clearOriginColumnsByRow.Count == 0)
            return false;

        if (_environmentRowClearResolving || gameOver || levelWon)
            return false;

        bool hasClearableFullRow = false;
        foreach (var kv in clearOriginColumnsByRow)
        {
            int row = kv.Key;
            if (row < 0 || row >= gameBoard.height)
                continue;

            if (gameBoard.IsClearableOccupiedRow(row))
            {
                hasClearableFullRow = true;
                break;
            }
        }

        if (!hasClearableFullRow)
            return false;

        _environmentRowClearResolving = true;
        gameBoard.ClearFullLinesAnimated((rowsCleared, removedCells, damageFromMonsters, specialChargeFromMonsters, rowDamage, rowDominantMonster) =>
        {
            try
            {
                OnPieceLocked(
                    rowsCleared,
                    removedCells,
                    damageFromMonsters,
                    specialChargeFromMonsters,
                    rowDamage,
                    rowDominantMonster,
                    clearOriginColumnsByRow);
            }
            finally
            {
                _environmentRowClearResolving = false;
            }
        }, clearOriginColumnsByRow: clearOriginColumnsByRow);

        return true;
    }

    static void AddClearOriginColumn(Dictionary<int, List<int>> colsByRow, Vector2Int cell)
    {
        if (colsByRow == null)
            return;

        if (!colsByRow.TryGetValue(cell.y, out var cols))
        {
            cols = new List<int>();
            colsByRow[cell.y] = cols;
        }

        if (!cols.Contains(cell.x))
            cols.Add(cell.x);
    }

    // ================= Combo scoring and damage bonus =================

    public const int MinimumFinalMonsterDamage = 5;

    public int ApplyComboForRowClear(int monstersClearedInRow, float rowDamage)
    {
        float baseRow = Mathf.Max(0f, rowDamage); // Sum of per-monster attacks (+bonus)

        int combo = IncrementCombo();

        // Apply shop/run buffs once here
        float buffMult = monsterDamageMult * AllyMonsterOutgoingDamageMultiplier * PlayerMonsterAttackMult;
        int afterBuffs = Mathf.RoundToInt(baseRow * buffMult);

        // Apply combo (+5% per step after first)
        float comboMult = GetComboDamageMultiplier(combo);
        int finalDamage = Mathf.RoundToInt(afterBuffs * comboMult);

        if (levelModifierController)
            finalDamage = levelModifierController.ModifyOutgoingDamage(finalDamage, combo);

        if (baseRow > 0f)
            finalDamage = Mathf.Max(MinimumFinalMonsterDamage, finalDamage);

        if (logRowDamageBreakdown)
        {
            float buffBonus = afterBuffs - baseRow;
            int comboBonus = finalDamage - afterBuffs;

            Debug.Log(
                $"[RowClearDamage] Combo={combo} Monsters={monstersClearedInRow} " +
                $"Base={baseRow:0.###} BuffMult={buffMult:0.###} (+{buffBonus:0.###}) " +
                $"ComboMult={comboMult:0.###} (+{comboBonus}) Final={finalDamage}"
            );
        }

        // Score 1 point per monster, multiplied by current combo count
        int gained = Mathf.Max(0, monstersClearedInRow) * combo;
        if (gained > 0)
            AddScoreInternal(gained);

        return finalDamage;
    }

    public void SetRowClearComboResolutionActive(bool active)
    {
        _rowClearComboResolutionActive = active;
    }

    int IncrementCombo()
    {
        _comboCount = Mathf.Max(0, _comboCount) + 1;

        if (_partyPassiveBonuses.bonusComboChance > 0f && Random.value <= _partyPassiveBonuses.bonusComboChance)
            _comboCount += 1;

        if (_comboCount > _maxComboThisLevel)
            _maxComboThisLevel = _comboCount; // Update max combo for bonus XP tracking

        RunSummaryStats.RecordCombo(_comboCount);

        _comboTimer = Mathf.Max(0.1f, CurrentComboWindowSeconds);

        if (scoreUI)
            scoreUI.SetCombo(_comboCount);

        if (PlayerProgress.I)
            PlayerProgress.I.SetRunBestInt(AchievementSystem.Stat.RunMaxCombo, _comboCount);

        return _comboCount;
    }

    float GetComboDamageMultiplier(int combo)
    {
        float mult = 1f + (Mathf.Max(0, combo - 1) * comboDamMult); // Increase damage by 5% per combo step after the first
        return Mathf.Min(2f, mult);
    }

    void ResetCombo()
    {
        _comboCount = 0;
        _comboTimer = 0f;
        if (scoreUI)
            scoreUI.ClearCombo();
    }

    void AddScoreInternal(int points)
    {
        score += GetScaledScorePoints(points);
        RunSummaryStats.SetFinalScore(score);
        if (scoreUI) scoreUI.Set(score);
    }

    CastleData ResolveCastleDataForLevel(int levelIndex)
    {
        if (_postFinalSurvivalActive && postFinalSurvivalCastle)
            return postFinalSurvivalCastle;

        if (castlesByLevel != null && levelIndex >= 0 && levelIndex < castlesByLevel.Length)
            return castlesByLevel[levelIndex];

        return null;
    }

    bool IsCastleBossForCurrentMode(CastleData data)
    {
        if (!data)
            return false;

        return data.isBossLevel || (_postFinalSurvivalActive && forcePostFinalSurvivalBossLevel);
    }

    int CountCompletedStandardBossLevelsBeforeCurrentLevel()
    {
        if (castlesByLevel == null || castlesByLevel.Length == 0)
            return 0;

        int completedLevels = Mathf.Clamp(currentLevel, 0, castlesByLevel.Length);
        int completedBosses = 0;

        for (int i = 0; i < completedLevels; i++)
        {
            CastleData data = castlesByLevel[i];
            if (data && data.isBossLevel)
                completedBosses++;
        }

        return completedBosses;
    }

    bool CanStartPostFinalSurvival()
    {
        return postFinalSurvivalCastle != null;
    }

    int GetPostFinalSurvivalLevelIndex()
    {
        return Mathf.Max(0, castlesByLevel != null ? castlesByLevel.Length : currentLevel + 1);
    }


    void InitLevel(int levelIndex)
    {
        levelWon = false;
        winQueued = false;
        ResetBossGravityVisuals();

        ResetCombo();

        // Reset level performance tracking
        _maxComboThisLevel = 0;
        _obstaclesDestroyedThisLevel = 0;
        _levelStartMaxLives = EffectiveMaxUnitLives;
        _levelStartReserveUnits = unitLives;

        ResetLevelTimerAndDrop(levelIndex);

        CastleData data = ResolveCastleDataForLevel(levelIndex);
        castleProjectileVisualScale = 1f;

        if (!enemyCastleUI)
            enemyCastleUI = FindFirstObjectByType<EnemyCastleUI>(FindObjectsInactive.Include);

        currentCastleData = data; // For external reference if needed

        if (enemyCastleUI && data)
        {
            int levelNumber = levelIndex + 1;
            bool forceInfiniteHealth = _postFinalSurvivalActive && forcePostFinalSurvivalInfiniteHealth;
            bool forceBossLevel = _postFinalSurvivalActive && forcePostFinalSurvivalBossLevel;
            enemyCastleUI.InitCastle(
                data,
                levelNumber,
                enemyCastleHpMult * _starDifficultyModifiers.enemyHealthMultiplier,
                forceInfiniteHealth,
                forceBossLevel);

            _castleData = data;

            if (_castleData != null && IsCastleBossForCurrentMode(_castleData))
            {
                _bossAbilityTimer = 0f;
                _bossNextAbilityAt = Random.Range(_castleData.bossAbilityIntervalMin, _castleData.bossAbilityIntervalMax);
            }

            enemyCastleUI.SetMagicShieldActive(false);
            castleProjectileSprite = data.projectileSprite;

            castleAttackInterval = data.projectileInterval * enemyAttackIntervalMult;
            _castleAttackTimer = 0f;
            projectileSpeed = Mathf.Max(1f, data.projectileSpeed) * Mathf.Max(0.01f, enemyProjectileSpeedMult);
            castleProjectileVisualScale = Mathf.Max(0.1f, data.projectileVisualScale);
            castleProjectileDamage = Mathf.Max(1, Mathf.RoundToInt(GetScaledEnemyDamage(data.projectileDamage * enemyProjectileDamageMult)));

            if (levelText) levelText.text = TetrabeastsLocalization.LocalizeFormat("Level: {0}", levelNumber);
        }
        else
        {
            Debug.LogWarning("InitLevel: castle data or enemyCastleUI missing");
        }

        if (AudioManager.I && data != null)
        {
            AudioManager.I.PlayLevelMusic(IsCastleBossForCurrentMode(data));
        }

        if (obstacleManager)
        {
            obstacleManager.OnLevelStart(levelIndex + 1, currentCastleData);
        }
    }

    // Call this when castle HP has been reduced to 0
    void OnCastleDestroyed()
    {
        if (gameOver || levelWon) return;
        levelWon = true;
        _claimedRoundWinOneLiner = ClaimRoundTransitionOneLiner(RoundTransitionVariant.Win);

        StartCoroutine(CoHandleCastleDestroyedVictory());
    }

    IEnumerator CoHandleCastleDestroyedVictory()
    {
        PauseGameplayForRoundTransition();

        if (AudioManager.I)
        {
            AudioManager.I.StopLevelMusic();

            if (roundWinClip)
                AudioManager.I.PlaySFX(roundWinClip);
        }

        if (nextPreview)
            nextPreview.ClearPreview();

        float delay = Mathf.Max(0f, victorySequenceDelaySeconds);
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        string claimedOneLiner = _claimedRoundWinOneLiner;
        _claimedRoundWinOneLiner = string.Empty;

        yield return CoShowRoundTransition(RoundWinTransitionText, RoundTransitionVariant.Win, claimedOneLiner);
        FinishRoundTransitionAndKeepGameplayPaused();

        int completedLevelNumber = currentLevel + 1;
        bool finalLevel = IsFinalLevelIndex(currentLevel);

        var roster = GetActiveMonsterRoster();
        var computed = ComputeRoundWinXp(completedLevelNumber);

        if (DemoBuildGuardRails.HasReachedLevelLimit(completedLevelNumber))
        {
            ShowDemoLevelLimitPopupThenEndRun(roster, computed);
            yield break;
        }

        void ContinueAfterRoundXp()
        {
            if (finalLevel)
            {
                ShowFinalVictoryPanelAfterRoundWin(roster);
                return;
            }

            ContinueAfterRoundWinRewards(closeXpAfterRewardOpen: true);
        }

        if (xpAwardUI)
        {
            OpenXpUiMode();

            if (AudioManager.I)
                AudioManager.I.PlayIntermissionWinMusic();

            xpAwardUI.ShowRoundWin(
                computed.breakdown,
                roster,
                computed.perMonsterAwardXp,
                ContinueAfterRoundXp,
                hideOnFinalContinue: false,
                perMonsterReductionPercent: computed.perMonsterXpReductionPercent);
            yield break;
        }

        foreach (var kv in computed.perMonsterAwardXp)
            RunMonsterProgress.AddRunXp(kv.Key, kv.Value);

        if (finalLevel)
        {
            ShowFinalVictoryPanelAfterRoundWin(roster);
            yield break;
        }

        ContinueAfterRoundWinRewards();
    }

    void ShowDemoLevelLimitPopupThenEndRun(List<MonsterData> roster, ComputedRoundXp computed)
    {
        if (_demoLimitRunEnding)
            return;

        _demoLimitRunEnding = true;

        if (xpAwardUI)
            xpAwardUI.HideAll();

        PauseGameplayForBlockingPopup();

        string message = string.IsNullOrWhiteSpace(demoLevelLimitMessage)
            ? "Thank you for playing the demo. If you enjoyed yourself, please consider buying the full game."
            : demoLevelLimitMessage;

        ShowAlertPopup(message, () => StartCoroutine(CoEndDemoLimitRunAfterPopup(roster, computed)), "Continue");
    }

    IEnumerator CoEndDemoLimitRunAfterPopup(List<MonsterData> roster, ComputedRoundXp computed)
    {
        yield return CoAwardDemoLimitRoundXp(roster, computed);

        ApplyFinalLevelRoundWinBookkeeping();

        gameOver = true;
        ClearTempRunCheckpoint();
        PlayerProgress.I?.EndRun();

        ShowDemoLimitRunEndXpTransfer(roster);
    }

    IEnumerator CoAwardDemoLimitRoundXp(List<MonsterData> roster, ComputedRoundXp computed)
    {
        if (xpAwardUI)
        {
            bool done = false;

            OpenXpUiMode();

            if (AudioManager.I)
                AudioManager.I.PlayIntermissionWinMusic();

            xpAwardUI.ShowRoundWin(
                computed.breakdown,
                roster,
                computed.perMonsterAwardXp,
                () => done = true,
                perMonsterReductionPercent: computed.perMonsterXpReductionPercent);
            yield return new WaitUntil(() => done);
            yield break;
        }

        foreach (var kv in computed.perMonsterAwardXp)
            RunMonsterProgress.AddRunXp(kv.Key, kv.Value);
    }

    void ShowDemoLimitRunEndXpTransfer(List<MonsterData> roster)
    {
        if (xpAwardUI && RunMonsterProgress.RunActive)
        {
            OpenXpUiMode();

            if (AudioManager.I)
                AudioManager.I.PlayIntermissionLoseMusic();

            xpAwardUI.ShowRunEndCommit(
                roster,
                GetRunEndXpConversionFraction(finalLevelWin: false),
                () =>
                {
                    ShowFinalStatsBeforeHighScore(ShowDemoLimitLocalHighScore, CloseAndHideXpUiMode);
                },
                hideOnFinalContinue: false,
                showRunEndXpTutorials: false);

            return;
        }

        CommitRunEndXpSilently(finalLevelWin: false);
        ShowDemoLimitHighScore();
    }

    void ShowDemoLimitHighScore()
    {
        if (xpAwardUI)
            xpAwardUI.HideAll();

        ShowFinalStatsBeforeHighScore(ShowDemoLimitLocalHighScore);
    }

    void ShowDemoLimitLocalHighScore()
    {
        if (highScoreUI)
        {
            if (highScoreUI.mainMenuButton)
            {
                highScoreUI.mainMenuButton.onClick.RemoveListener(DoReturnToMainMenuNow);
                highScoreUI.mainMenuButton.onClick.AddListener(DoReturnToMainMenuNow);
            }

            highScoreUI.SetRestartButtonSuppressed(true);
            highScoreUI.TryShow(score);
        }
        else
        {
            DoReturnToMainMenuNow();
            return;
        }

        if (restartButton)
            restartButton.gameObject.SetActive(false);

        EnterUICursorMode();
    }

    void ContinueAfterRoundWinRewards(bool closeXpAfterRewardOpen = false)
    {
        currentLevel++;
        levelModifierController?.GrantRerollForCompletedLevel(currentLevel);
        _roundRewardRerollsAvailable += Mathf.Max(0, rewardRerollsGrantedPerCompletedLevel);

        // Track level wins for achievements
        if (PlayerProgress.I)
        {
            PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.LevelsWon, 1);
            PlayerProgress.I.AddRunInt(AchievementSystem.Stat.RunLevelsWon, 1);

            PlayerProgress.I.SetRunBestFloatMin(AchievementSystem.Stat.RunBestLevelWinSeconds, _levelTimer);
            PlayerProgress.I.SetRunBestFloatMin(AchievementSystem.Stat.RunBestLevelSeconds, _levelTimer);

            int maxLivesNow = EffectiveMaxUnitLives;
            if (unitLives >= maxLivesNow)
                PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.PerfectLevelWins, 1);
        }

        RefreshActiveMonsterPassives(applyStartingReserveDelta: true);

        int roundsWon = currentLevel;

        int bonusSteps = roundsWon / 3;
        float misfortuneGain = 5f + (5f * bonusSteps);

        misfortune += misfortuneGain;
        RunModsStore.Misfortune = CurrentMisfortune;

        int gained = GetRoundWinCurrency();
        CurrencyStore.Add(gained);

        if (PlayerProgress.I && gained > 0)
            PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.GoldEarned, gained);

        if (currencyUI) currencyUI.Refresh();

        int beforeLives = unitLives;
        int maxLives = EffectiveMaxUnitLives;

        int reinforcementsGranted = CurrentReinforcementsPerWin;
        unitLives = Mathf.Clamp(unitLives + reinforcementsGranted, 0, maxLives);

        int actualReinforcements = unitLives - beforeLives;
        UpdateUnitLivesUI();

        if (roundRewardUI)
        {
            roundRewardUI.Show(
                buffPool,
                debuffPool,
                OnRoundModsChosen,
                gained,
                actualReinforcements,
                () => _roundRewardRerollsAvailable,
                TrySpendRoundRewardReroll);

            if (closeXpAfterRewardOpen)
                StartCoroutine(CoCloseXpAfterPanelFullyShown(roundRewardUI.rootPanel, resumeGameplay: false));
        }
        else
        {
            if (closeXpAfterRewardOpen)
                CloseAndHideXpUiMode(resumeGameplay: false);

            ContinueAfterRoundRewards();
        }

        EnterUICursorMode();
    }

    void OnRoundModsChosen(RunModifierSO buff, RunModifierSO debuff)
    {
        if (buff)
        {
            _runBuffs.Add(buff);
            RunModsStore.Buffs.Add(buff);
            buff.Apply(this);
            CodexProgressStore.Unlock(buff);

            // Track buff round mod achievements
            if (PlayerProgress.I && PlayerProgress.I.GetLifetimeInt(AchievementSystem.Stat.FirstBuffChosen) == 0)
                PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.FirstBuffChosen, 1);

            if (PlayerProgress.I)
                PlayerProgress.I.AddRunInt(AchievementSystem.Stat.RunBuffModsChosen, 1);
        }

        if (debuff)
        {
            _runDebuffs.Add(debuff);
            RunModsStore.Debuffs.Add(debuff);
            debuff.Apply(this);
            CodexProgressStore.Unlock(debuff);

            // Track first debuff chosen for achievements
            if (PlayerProgress.I && PlayerProgress.I.GetLifetimeInt(AchievementSystem.Stat.FirstDebuffChosen) == 0)
                PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.FirstDebuffChosen, 1);
        }

        SyncRunModsToStore();
        if (runModsPanelUI) runModsPanelUI.Refresh();
        ContinueAfterRoundRewards();
    }

    void SyncRunModsToStore()
    {
        RunModsStore.EnemyAttackIntervalMult = enemyAttackIntervalMult;
        RunModsStore.EnemyProjectileDamageMult = enemyProjectileDamageMult;
        RunModsStore.EnemyProjectileSpeedMult = enemyProjectileSpeedMult;

        RunModsStore.SpecialGainMult = specialGainMult;
        RunModsStore.SpecialDrainMult = specialDrainMult;
        RunModsStore.SpecialBlockChanceAdd = specialBlockChanceAdd;

        RunModsStore.PieceGravityMult = pieceGravityMult;
        RunModsStore.FallRampRateMult = fallRampRateMult;

        RunModsStore.MonsterDamageMult = monsterDamageMult;
        RunModsStore.MonsterSpecialGainMult = monsterSpecialGainMult;
        RunModsStore.MonsterMaxHpMult = monsterMaxHpMult;

        RunModsStore.HealPowerMult = healPowerMult;
        RunModsStore.HealRangeAdd = healRangeAdd;

        RunModsStore.DisableNextPreview = disableNextPreview;
        RunModsStore.DisableLandingHint = disableLandingHint;

        RunModsStore.LineClearCurrencyChanceAdd = lineClearCurrencyChanceAdd;
        RunModsStore.LineClearCurrencyAmountMult = lineClearCurrencyAmountMult;

        RunModsStore.EnemyCastleHpMult = enemyCastleHpMult;

        RunModsStore.Luck = EffectiveLuck;
        RunModsStore.Misfortune = CurrentMisfortune;

        RunModsStore.StoneBuffDropChanceAdd = stoneBuffDropChanceAdd;
        RunModsStore.StoneObstacleDropsDebuffsOnly = stoneObstacleDropsDebuffsOnly;
        RunModsStore.ReserveUnitsRestoredOnWinAdd = reserveUnitsRestoredOnWinAdd;
        RunModsStore.MaxReserveUnitsAdd = maxReserveUnitsAdd;
        RunModsStore.DisableRoundWinReserveRestore = disableRoundWinReserveRestore;
    }

    void ContinueAfterRoundRewards()
    {
        if (castlesByLevel != null && currentLevel < castlesByLevel.Length)
            StartCoroutine(CoStartNextLevel());
        else
            EndRunAsWin();
    }

    private System.Collections.IEnumerator CoStartNextLevel()
    {
        yield return null;

        StartNextLevel();
    }

    void StartNextLevel()
    {
        if (piece) piece.ResetPiece();
        if (gameBoard) gameBoard.ClearAll();

        ApplyRunGridSize(currentLevel); // Adjust grid size if needed for this level
        InitLevel(currentLevel); // Sets castle to full HP and updates level text

        ResetCombo();

        if (battleLog) battleLog.Clear();

        EnterGameplayCursorMode();

        levelWon = false; // re-arm
        StartCoroutine(BeginCurrentLevelSequence(keepRoundRewardVisibleUntilLevelModifierPanelShown: roundRewardUI != null));
    }

    void EndRunAsWin()
    {
        FinalizeRunAsWinState();
        ShowRunEndCommitAfterFinalWin(GetActiveMonsterRoster(), playIntermissionMusic: true);
    }

    void ShowFinalVictoryPanelAfterRoundWin(List<MonsterData> roster)
    {
        ApplyFinalLevelRoundWinBookkeeping();
        bool canContinueToSurvival = CanStartPostFinalSurvival();
        if (canContinueToSurvival)
        {
            ApplyStandardFinalWinState();
            if (xpAwardUI)
                xpAwardUI.HideAll();

            StartPostFinalSurvivalLevel();
        }
        else
        {
            FinalizeRunAsWinState();
            ShowRunEndCommitAfterFinalWin(roster, playIntermissionMusic: false);
        }
    }

    void StartPostFinalSurvivalLevel()
    {
        if (!CanStartPostFinalSurvival())
        {
            ShowRunEndCommitAfterFinalWin(GetActiveMonsterRoster(), playIntermissionMusic: false);
            return;
        }

        if (victoryPanelUI)
            victoryPanelUI.Hide();

        if (xpAwardUI)
            xpAwardUI.HideAll();

        CloseXpUiMode();
        HideRoundTransitionImmediate();

        if (roundRewardUI)
            roundRewardUI.Hide();

        if (piece)
            piece.ResetPiece();

        if (gameBoard)
            gameBoard.ClearAll();

        _postFinalSurvivalActive = true;
        currentLevel = GetPostFinalSurvivalLevelIndex();

        gameOver = false;
        levelWon = false;
        winQueued = false;
        _pendingMainMenuAfterXp = false;

        ResetCombo();
        ResetBossGravityVisuals();

        if (battleLog)
            battleLog.Clear();

        ApplyRunGridSize(currentLevel);
        InitLevel(currentLevel);

        _pendingPostFinalSurvivalIntro = ShouldShowPostFinalSurvivalIntro();
        BeginPostFinalSurvivalGameplay();
    }

    bool ShouldShowPostFinalSurvivalIntro()
    {
        return PlayerPrefs.GetInt(PostFinalSurvivalIntroPrefsKey, 0) == 0;
    }

    IEnumerator CoShowPostFinalSurvivalIntroAfterFirstPiece()
    {
        if (!_pendingPostFinalSurvivalIntro)
            yield break;

        _pendingPostFinalSurvivalIntro = false;
        _levelStartBlocked = true;

        yield return null;
        Canvas.ForceUpdateCanvases();

        if (gameBoard)
            gameBoard.RecomputeCellMetrics();

        if (piece && piece.HasActiveCells)
        {
            piece.RefreshVisualsExternal();
            piece.RefreshLandingHintsExternal();
        }

        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();

        PauseGameplayForRoundTransition();

        bool doNotShowAgain = false;
        yield return CoShowRoundTransition(
            PostFinalSurvivalIntroText,
            PostFinalSurvivalIntroOptOutText,
            false,
            value => doNotShowAgain = value);

        if (doNotShowAgain)
        {
            PlayerPrefs.SetInt(PostFinalSurvivalIntroPrefsKey, 1);
            PlayerPrefs.Save();
            SteamCloudSaveService.QueueUpload();
        }

        _levelStartBlocked = false;
        ResumeGameplayAfterRoundTransition();
        EnterGameplayCursorMode();
    }

    void BeginPostFinalSurvivalGameplay()
    {
        EnterGameplayCursorMode();
        StartCoroutine(BeginCurrentLevelSequence());
    }

    void ApplyStandardFinalWinState()
    {
        if (_finalWinStateApplied)
            return;

        _finalWinStateApplied = true;
        if (PlayerProgress.I)
            PlayerProgress.I.AddRunInt(AchievementSystem.Stat.RunBeatFinalLevel, 1);

        _newDifficultyUnlockedThisRun = HandleStarDifficultyFinalWin();

        if (PlayerProgress.I != null)
        {
            string charId = selectedCharacter ? selectedCharacter.name : "";
            if (!string.IsNullOrEmpty(charId))
            {
                string key = "lt_final_win_char_" + charId;
                if (PlayerProgress.I.GetLifetimeInt(key) == 0)
                    PlayerProgress.I.AddLifetimeInt(key, 1);
            }

            TryMarkFinalWinAllCharacters();
        }
    }

    void FinalizeRunAsWinState()
    {
        ApplyStandardFinalWinState();

        if (gameOver)
            return;

        gameOver = true;
        ClearTempRunCheckpoint();
        PlayerProgress.I?.EndRun();
    }

    void ShowRunEndCommitAfterFinalWin(List<MonsterData> roster, bool playIntermissionMusic)
    {
        void ShowHighScore(System.Action onFinalStatsFullyShown = null)
        {
            if (AudioManager.I) AudioManager.I.StopMusic();
            ShowFinalStatsBeforeHighScore(ShowFinalWinLocalHighScore, onFinalStatsFullyShown);
        }

        void ShowFinalWinLocalHighScore()
        {
            SubmitSteamLeaderboardScore();
            if (highScoreUI)
            {
                highScoreUI.SetRestartButtonSuppressed(false);
                highScoreUI.TryShow(score);
            }
            if (restartButton) restartButton.gameObject.SetActive(true);
            EnterUICursorMode();
        }

        if (xpAwardUI)
        {
            OpenXpUiMode();

            if (playIntermissionMusic && AudioManager.I)
                AudioManager.I.PlayIntermissionWinMusic();

            xpAwardUI.ShowRunEndCommit(
                roster,
                GetRunEndXpConversionFraction(finalLevelWin: true),
                () =>
                {
                    ShowHighScore(CloseAndHideXpUiMode);
                },
                hideOnFinalContinue: false,
                showRunEndXpTutorials: false);

            return;
        }

        var kept = RunMonsterProgress.EndRunAndComputeKeptXp(GetRunEndXpConversionFraction(finalLevelWin: true));
        foreach (var kv in kept)
            MonsterProgressStore.AddPermanentXp(kv.Key, kv.Value);

        CloseXpUiMode();
        ShowHighScore();
    }

    void ShowFinalStatsBeforeHighScore(System.Action showHighScore, System.Action onFinalStatsFullyShown = null)
    {
        ResolveVictoryPanelUi(logWarning: false);

        if (!victoryPanelUI)
        {
            onFinalStatsFullyShown?.Invoke();
            showHighScore?.Invoke();
            return;
        }

        if (roundRewardUI)
            roundRewardUI.Hide();

        victoryPanelUI.SetUnlockedDifficultyText(_newDifficultyUnlockedThisRun ? "New Difficulty Unlocked!" : string.Empty);
        victoryPanelUI.Show(RunSummaryStats.GetSnapshot(), RunModsStore.Buffs, RunModsStore.Debuffs, () =>
        {
            StartCoroutine(CoOpenHighScoreBeforeClosingFinalStats(showHighScore));
        });

        if (onFinalStatsFullyShown != null)
            StartCoroutine(CoInvokeAfterPanelFullyShown(victoryPanelUI.RootPanel, onFinalStatsFullyShown));

        EnterUICursorMode();
    }

    bool TrySpendRoundRewardReroll()
    {
        if (_roundRewardRerollsAvailable <= 0)
            return false;

        _roundRewardRerollsAvailable--;
        return true;
    }

    IEnumerator CoOpenHighScoreBeforeClosingFinalStats(System.Action showHighScore)
    {
        showHighScore?.Invoke();

        GameObject highScorePanel = highScoreUI ? highScoreUI.PanelRoot : null;
        yield return CoWaitForPanelFullyShown(highScorePanel);

        if (victoryPanelUI)
            victoryPanelUI.Hide();
    }

    IEnumerator CoInvokeAfterPanelFullyShown(GameObject panel, System.Action action)
    {
        yield return CoWaitForPanelFullyShown(panel);
        action?.Invoke();
    }

    IEnumerator CoCloseXpAfterPanelFullyShown(GameObject panel, bool resumeGameplay = true)
    {
        yield return CoWaitForPanelFullyShown(panel);
        CloseAndHideXpUiMode(resumeGameplay);
    }

    IEnumerator CoWaitForPanelFullyShown(GameObject panel)
    {
        if (!panel)
            yield break;

        yield return null;

        while (panel && UIPanelTransition.IsVisible(panel) && !UIPanelTransition.IsFullyShown(panel))
            yield return null;
    }

    void ApplyFinalLevelRoundWinBookkeeping()
    {
        currentLevel++;
        levelModifierController?.GrantRerollForCompletedLevel(currentLevel);

        if (PlayerProgress.I)
        {
            PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.LevelsWon, 1);
            PlayerProgress.I.AddRunInt(AchievementSystem.Stat.RunLevelsWon, 1);

            PlayerProgress.I.SetRunBestFloatMin(AchievementSystem.Stat.RunBestLevelWinSeconds, _levelTimer);
            PlayerProgress.I.SetRunBestFloatMin(AchievementSystem.Stat.RunBestLevelSeconds, _levelTimer);

            int maxLivesNow = EffectiveMaxUnitLives;
            if (unitLives >= maxLivesNow)
                PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.PerfectLevelWins, 1);
        }

        RefreshActiveMonsterPassives(applyStartingReserveDelta: true);

        int roundsWon = currentLevel;
        int bonusSteps = roundsWon / 3;
        float misfortuneGain = 5f + (5f * bonusSteps);

        misfortune += misfortuneGain;
        RunModsStore.Misfortune = CurrentMisfortune;

        int gained = GetRoundWinCurrency();
        CurrencyStore.Add(gained);

        if (PlayerProgress.I && gained > 0)
            PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.GoldEarned, gained);

        if (currencyUI) currencyUI.Refresh();

        int maxLives = EffectiveMaxUnitLives;
        int reinforcementsGranted = CurrentReinforcementsPerWin;

        unitLives = Mathf.Clamp(unitLives + reinforcementsGranted, 0, maxLives);
        UpdateUnitLivesUI();
    }

    void TryMarkFinalWinAllCharacters()
    {
        if (PlayerProgress.I == null) return;
        if (PlayerProgress.I.GetLifetimeInt(AchievementSystem.Stat.FinalWinAllChars) != 0) return;

        if (achievementCharacterIds == null || achievementCharacterIds.Length < 5) return;

        for (int i = 0; i < achievementCharacterIds.Length; i++)
        {
            string id = achievementCharacterIds[i];
            if (string.IsNullOrEmpty(id)) return;

            if (PlayerProgress.I.GetLifetimeInt("lt_final_win_char_" + id) == 0)
                return;
        }

        PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.FinalWinAllChars, 1);
    }

    void ActivateSpecial()
    {
        if (gameOver || gameBoard == null || selectedCharacter == null) return;
        if (!IsRoundActive) return;
        if (_specialAbilityCinematicCR != null) return;

        if (levelModifierController && levelModifierController.BlocksSpecialUsage)
        {
            SetSpecialGaugeImmediate(0f);
            return;
        }

        if (specialGaugeMax <= 0f || specialGauge < specialGaugeMax) return; // Require full gauge

        PlayerCharacterData character = selectedCharacter;
        TrackSpecialActivation(character);
        PlaySpecialAbilityActivationSFX(character);

        _specialAbilityCinematicActive = true;
        _specialAbilityCinematicCR = StartCoroutine(ActivateSpecialRoutine(character));
    }

    void PlaySpecialAbilityActivationSFX(PlayerCharacterData character)
    {
        if (AudioManager.I && character && character.specialAbilityAnimationSFX)
            AudioManager.I.PlaySpecialAbilityAnimationSFX(character.specialAbilityAnimationSFX, character.specialAbilityAnimationSFXVolume);
    }

    void TrackSpecialActivation(PlayerCharacterData character)
    {
        NotifyTutorialGameplayEvent(TutorialGameplayEvent.SpecialActivated);

        if (battleLog && character)
            battleLog.LogAbilityUse(
                TetrabeastsLocalization.LocalizeText(character.displayName),
                TetrabeastsLocalization.LocalizeText(character.specialAbilityName));

        if (PlayerProgress.I) // Track total special uses for achievements
        {
            PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.SpecialsUsedTotal, 1);

            // Per-character special count (stable key based on asset name)
            string charKey = character ? character.name : "Unknown";
            PlayerProgress.I.AddLifetimeInt($"lt_specials_used_char_{charKey}", 1);

            // Check "each character specials" thresholds (20 / 100)
            TryMarkSpecialsEachCharacter(20, AchievementSystem.Stat.SpecialsEachChar_20);
            TryMarkSpecialsEachCharacter(100, AchievementSystem.Stat.SpecialsEachChar_100);
        }
    }

    IEnumerator ActivateSpecialRoutine(PlayerCharacterData character)
    {
        if (specialAbilityPopupPrefab)
        {
            PauseGameplayForSpecialAbilityPopup();
            yield return PlaySpecialAbilityPopup(character);
            ResumeGameplayAfterSpecialAbilityPopup();
        }

        _specialAbilityCinematicActive = false;
        _specialAbilityCinematicCR = null;

        if (!gameOver && !levelWon && character)
            ApplySpecialGameplayEffect(character);

        StartCoroutine(CoPrewarmSpecialAbilityPopupDeferred());
    }

    IEnumerator CoPrewarmSpecialAbilityPopupDeferred()
    {
        yield return null;

        if (!_specialAbilityCinematicActive)
            PrewarmSpecialAbilityPopup();
    }

    IEnumerator PlaySpecialAbilityPopup(PlayerCharacterData character)
    {
        Transform parent = ResolveSpecialAbilityPopupParent();
        SpecialAbilityPopup popupView;
        GameObject popup = TakePrewarmedSpecialAbilityPopup(character, parent, out popupView);
        if (!popup)
        {
            popup = Instantiate(specialAbilityPopupPrefab, parent, false);
            popup.SetActive(true);
            PrepareSpecialAbilityPopupRect(popup);

            popupView = popup.GetComponent<SpecialAbilityPopup>();
            if (!popupView)
                popupView = popup.AddComponent<SpecialAbilityPopup>();

            popupView.Prepare(character);
        }

        if (AudioManager.I)
        {
            AudioManager.I.PauseMainMusicForSpecialAbilityPopup();

            if (specialAbilityPopupLoopSFX)
                AudioManager.I.PlaySpecialAbilityPopupLoopSFX(specialAbilityPopupLoopSFX, specialAbilityPopupLoopSFXVolume);
        }

        yield return popupView.PlayPrepared(
            onSlideInComplete: () =>
            {
                if (AudioManager.I && specialAbilityPopupLockInSFX)
                    AudioManager.I.PlaySpecialAbilityAnimationSFX(specialAbilityPopupLockInSFX, specialAbilityPopupLockInSFXVolume);
            },
            onClosingStarted: fadeDuration =>
            {
                if (AudioManager.I)
                    AudioManager.I.FadeSpecialAbilityPopupLoopSFX(fadeDuration);
            });

        if (AudioManager.I)
        {
            AudioManager.I.StopSpecialAbilityPopupLoopSFX();
            AudioManager.I.ResumeMainMusicAfterSpecialAbilityPopup();
        }

        if (popup)
            Destroy(popup);
    }

    void PrewarmSpecialAbilityPopup()
    {
        if (!specialAbilityPopupPrefab || !selectedCharacter)
            return;

        if (_prewarmedSpecialAbilityPopup && _prewarmedSpecialAbilityCharacter == selectedCharacter)
            return;

        ClearPrewarmedSpecialAbilityPopup();

        Transform parent = ResolveSpecialAbilityPopupParent();
        GameObject popup = Instantiate(specialAbilityPopupPrefab, parent, false);
        popup.SetActive(true);
        PrepareSpecialAbilityPopupRect(popup);

        SpecialAbilityPopup popupView = popup.GetComponent<SpecialAbilityPopup>();
        if (!popupView)
            popupView = popup.AddComponent<SpecialAbilityPopup>();

        popupView.Prepare(selectedCharacter);
        popup.SetActive(false);

        _prewarmedSpecialAbilityPopup = popup;
        _prewarmedSpecialAbilityCharacter = selectedCharacter;
        _prewarmedSpecialAbilityPopupView = popupView;
    }

    GameObject TakePrewarmedSpecialAbilityPopup(PlayerCharacterData character, Transform parent, out SpecialAbilityPopup popupView)
    {
        popupView = null;

        if (!_prewarmedSpecialAbilityPopup || _prewarmedSpecialAbilityCharacter != character)
            return null;

        GameObject popup = _prewarmedSpecialAbilityPopup;
        popupView = _prewarmedSpecialAbilityPopupView;

        _prewarmedSpecialAbilityPopup = null;
        _prewarmedSpecialAbilityCharacter = null;
        _prewarmedSpecialAbilityPopupView = null;

        if (!popupView && popup)
            popupView = popup.GetComponent<SpecialAbilityPopup>();

        if (!popup || !popupView)
        {
            if (popup)
                Destroy(popup);
            return null;
        }

        if (parent && popup.transform.parent != parent)
            popup.transform.SetParent(parent, false);

        popup.SetActive(true);
        PrepareSpecialAbilityPopupRect(popup);
        popupView.ResetIntroState();
        return popup;
    }

    void ClearPrewarmedSpecialAbilityPopup()
    {
        if (_prewarmedSpecialAbilityPopup)
            Destroy(_prewarmedSpecialAbilityPopup);

        _prewarmedSpecialAbilityPopup = null;
        _prewarmedSpecialAbilityCharacter = null;
        _prewarmedSpecialAbilityPopupView = null;
    }

    Transform ResolveSpecialAbilityPopupParent()
    {
        if (specialAbilityPopupRoot)
            return specialAbilityPopupRoot;

        Canvas canvas = null;
        if (pausePanel)
            canvas = pausePanel.GetComponentInParent<Canvas>(true);

        if (!canvas && specialSlider)
            canvas = specialSlider.GetComponentInParent<Canvas>(true);

        if (!canvas)
            canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        return canvas ? canvas.transform : transform;
    }

    void PrepareSpecialAbilityPopupRect(GameObject popup)
    {
        if (!popup)
            return;

        if (popup.TryGetComponent(out RectTransform rt))
        {
            rt.SetAsLastSibling();
            rt.localScale = Vector3.one;

            if (rt.parent is RectTransform)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }
            else
            {
                rt.localPosition = Vector3.zero;
            }
        }
        else
        {
            popup.transform.SetAsLastSibling();
            popup.transform.localPosition = Vector3.zero;
            popup.transform.localScale = Vector3.one;
        }
    }

    void PauseGameplayForSpecialAbilityPopup()
    {
        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = false;

        if (pausePanel)
            UIPanelTransition.Hide(pausePanel, true);

        EnterGameplayCursorMode();
    }

    void ResumeGameplayAfterSpecialAbilityPopup()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        isPaused = false;

        EnterGameplayCursorMode();
    }

    void ApplySpecialGameplayEffect(PlayerCharacterData character)
    {
        switch (character.ability)
        {
            case SpecialAbility.ClearBottomRows:
                {
                    RunSummaryStats.AddSpecialUsed();

                    int rows = Mathf.Max(1, character.clearRows);

                    int squaresCleared = gameBoard.ClearBottomRowsWithCombat(rows, out int totalMonsterDamage,
                                         out float _specialChargeIgnored, out Dictionary<int, int> rowDamage,
                                         out Dictionary<int, MonsterData> rowDominantMonster,
                                         out Dictionary<int, List<int>> colsByRow);

                    ResetSpecialGauge();

                    if (squaresCleared > 0 && AudioManager.I)
                        AudioManager.I.PlayRandomLineClear();

                    if (rowDamage != null && rowDamage.Count > 0)
                        RunSummaryStats.AddLinesCleared(rowDamage.Count);

                    // Spawn one projectile per cleared row, using the dominant monster for visuals
                    if (rowDamage != null && rowDamage.Count > 0 && gameBoard && enemyCastleUI && enemyCastleUI.castleImage && !levelWon)
                    {
                        // Stable ordering
                        var ordered = new List<KeyValuePair<int, int>>(rowDamage);
                        ordered.Sort((a, b) => (a.Key / 1000).CompareTo(b.Key / 1000));

                        var usedCols = new HashSet<int>();

                        foreach (var kv in ordered)
                        {
                            int rowKey = kv.Key;
                            int rowY = rowKey % 1000;
                            rowY = Mathf.Clamp(rowY, 0, gameBoard.height - 1);

                            int dmg = Mathf.Max(0, kv.Value);
                            if (dmg <= 0) continue;

                            if (PlayerProgress.I) // Track best single attack damage for achievements
                                PlayerProgress.I.SetRunBestInt(AchievementSystem.Stat.RunMaxSingleAttackDmg, dmg);

                            // Per-row dominant visuals
                            Sprite attackSprite = null;
                            Sprite attackSpriteAlt = null;
                            AttackAnimType animType = AttackAnimType.None;
                            MonsterData attackerMD = null;

                            if (rowDominantMonster != null && rowDominantMonster.TryGetValue(rowKey, out var md) && md)
                            {
                                attackerMD = md;
                                animType = md.attackAnim;
                                attackSprite = MonsterSkinStore.GetAttackSprite(md);
                                attackSpriteAlt = MonsterSkinStore.GetAttackAltSprite(md);
                            }

                            // Choose a preferred start column from candidates on this row
                            int preferredCol = UnityEngine.Random.Range(0, gameBoard.width);
                            if (colsByRow != null && colsByRow.TryGetValue(rowY, out var cols) && cols != null && cols.Count > 0)
                                preferredCol = cols[UnityEngine.Random.Range(0, cols.Count)];

                            // Shift if another projectile already uses this column
                            int col = preferredCol;
                            if (usedCols.Contains(col))
                            {
                                for (int step = 1; step < gameBoard.width; step++)
                                {
                                    int r = preferredCol + step;
                                    if (r < gameBoard.width && !usedCols.Contains(r)) { col = r; break; }

                                    int l = preferredCol - step;
                                    if (l >= 0 && !usedCols.Contains(l)) { col = l; break; }
                                }
                            }
                            usedCols.Add(col);

                            Vector2 startBoard = gameBoard.CellToAnchoredPos(new Vector2Int(col, rowY));

                            var root = projectileRoot ? projectileRoot : gameBoard.gridRoot;
                            Vector2 start = LocalTo(root, gameBoard.gridRoot, startBoard);

                            Vector3 castleBottomWorld = enemyCastleUI.castleImage.rectTransform.TransformPoint(
                                new Vector3(0f, -enemyCastleUI.castleImage.rectTransform.rect.height * 0.5f, 0f));

                            Vector2 castleBottomInRoot = root.InverseTransformPoint(castleBottomWorld);
                            Vector2 target = new Vector2(start.x, castleBottomInRoot.y);

                            SpawnAttackProjectile(attackSprite, attackSpriteAlt, animType, start, target, dmg, attackerMD);
                        }
                    }

                    break;
                }

            case SpecialAbility.RestoreAllToFull:
                {
                    RunSummaryStats.AddSpecialUsed();

                    // Count how many were dead before revive
                    int deadBefore = gameBoard.CountDeadTiles();

                    var changed = new List<Vector2Int>();
                    int count = gameBoard.ReviveAllTilesToFull(changed, out float totalRestored);

                    if (totalRestored > 0f)
                        RunSummaryStats.AddHealingDone(totalRestored);

                    if (character.sfxRestoreAll && AudioManager.I)
                        AudioManager.I.PlaySFX(character.sfxRestoreAll);

                    Sprite vfx = character.reviveAllVFXSprite ? character.reviveAllVFXSprite : null;

                    if (vfx && count > 0)
                        foreach (var cell in changed)
                            gameBoard.SpawnHealBurst(cell, vfx, 0.5f);

                    // Refund lives by number of dead units at cast time (cap at max)
                    if (deadBefore > 0)
                    {
                        unitLives = Mathf.Clamp(unitLives + deadBefore, 0, EffectiveMaxUnitLives);
                        UpdateUnitLivesUI();
                    }

                    ResetSpecialGauge();
                    break;
                }

            case SpecialAbility.GlobalImmunity:
                {
                    RunSummaryStats.AddSpecialUsed();

                    // Start the timer/pulse co-routine
                    StartCoroutine(GlobalImmunityCo(Mathf.Max(0.25f, character.immunityDuration)));

                    ResetSpecialGauge();
                    break;
                }

            case SpecialAbility.ReducedGravity:
                {
                    float dur = Mathf.Max(0.25f, character.reducedGravityDuration);
                    float projectedInterval = CalculateFallInterval(
                        playerGravityMult: 1f,
                        playerBaseOverrideActive: true,
                        slowGravitySpecialMult: 1f,
                        slowGravitySpecialRampRateMult: 1f);

                    if (!ShouldReplaceTimedSlowGravityEffect(
                            TimedSlowGravitySource.PlayerAbility,
                            projectedInterval,
                            dur))
                    {
                        break;
                    }

                    RunSummaryStats.AddSpecialUsed();

                    if (_playerGravityCR != null)
                    {
                        ResetPlayerGravitySpecialEffect();
                    }

                    ResetSlowGravitySpecialEffect();

                    _playerGravityCR = StartCoroutine(PlayerReducedGravityCo(dur));

                    ResetSpecialGauge();
                    break;
                }

            case SpecialAbility.DoubleStats:
                {
                    RunSummaryStats.AddSpecialUsed();

                    float dur = Mathf.Max(0.25f, character.doubleStatsDuration);

                    if (_playerDoubleStatsCR != null)
                    {
                        StopCoroutine(_playerDoubleStatsCR);
                        _playerDoubleStatsCR = null;

                        // Force revert before re-applying
                        _playerDoubleStatsAttackMult = 1f;
                        if (gameBoard) gameBoard.MultiplyAllMonsterHpAndMax(0.5f);
                    }

                    _playerDoubleStatsCR = StartCoroutine(PlayerDoubleStatsCo(dur));

                    ResetSpecialGauge();
                    break;
                }
        }
    }

    void TryMarkSpecialsEachCharacter(int needed, string flagKey)
    {
        if (PlayerProgress.I == null) return;
        if (PlayerProgress.I.GetLifetimeInt(flagKey) != 0) return;

        // Must have all 5 ids set
        if (achievementCharacterIds == null || achievementCharacterIds.Length < 5) return;

        for (int i = 0; i < achievementCharacterIds.Length; i++)
        {
            string id = achievementCharacterIds[i];
            if (string.IsNullOrEmpty(id)) return;

            long v = PlayerProgress.I.GetLifetimeInt("lt_specials_used_char_" + id);
            if (v < needed) return;
        }

        PlayerProgress.I.AddLifetimeInt(flagKey, 1); // All met
    }

    public void SetCharacter(PlayerCharacterData data)
    {
        selectedCharacter = data;
        ClearPrewarmedSpecialAbilityPopup();

        if (selectedCharacter)
        {
            if (playerPortrait && selectedCharacter.portrait)
                playerPortrait.sprite = selectedCharacter.portrait;

            if (playerBorder)
                playerBorder.sprite = selectedCharacter.defaultBorder;

            if (playerName)
                playerName.text = TetrabeastsLocalization.LocalizeText(selectedCharacter.displayName);

            PrewarmSpecialAbilityPopup();
        }
    }

    List<MonsterData> GetActiveMonsterRoster()
    {
        var active = (SelectedMonstersStore.Active != null && SelectedMonstersStore.Active.Count >= 2)
                     ? SelectedMonstersStore.Active
                     : new System.Collections.Generic.List<MonsterData>(fallbackMonsters);

        // Ensure at least 2 monsters
        if (active.Count < 2 && fallbackMonsters != null && fallbackMonsters.Length >= 2)
            active = new System.Collections.Generic.List<MonsterData>(fallbackMonsters);
        if (active.Count > 4) active = active.GetRange(0, 4);
        return active;
    }

    public List<MonsterData> GetActiveMonsterRosterSnapshot()
    {
        return new List<MonsterData>(GetActiveMonsterRoster());
    }

    MonsterData WeightedPick(List<MonsterData> roster)
    {
        float total = 0f;
        for (int i = 0; i < roster.Count; i++) total += Mathf.Max(0f, roster[i].weightedSpawnRate);
        if (total <= 0f) return roster[Random.Range(0, roster.Count)];

        float r = Random.Range(0f, total);
        for (int i = 0; i < roster.Count; i++)
        {
            float w = Mathf.Max(0f, roster[i].weightedSpawnRate);
            if ((r -= w) <= 0f) return roster[i];
        }
        return roster[roster.Count - 1];
    }

    void UpdateSpecialUI(bool playGaugeFullSFX = true)
    {
        if (specialSlider)
            specialSlider.value = Mathf.Clamp01(specialGauge / Mathf.Max(1f, specialGaugeMax));

        if (specialText)
            specialText.text = $"{Mathf.RoundToInt(100f * Mathf.Clamp01(specialGauge / Mathf.Max(1f, specialGaugeMax)))}%";

        UpdateSpecialGaugeFieryFill();

        bool full = specialGauge >= (specialGaugeMax - 0.001f);

        if (full && !_wasSpecialGaugeFullLastFrame)
        {
            if (playGaugeFullSFX)
                PlaySpecialGaugeFullSFX();

            QueueFirstSpecialGaugeTutorialIfNeeded();
        }

        _wasSpecialGaugeFullLastFrame = full;

        SetSpecialChargedVisuals(full); // Apply visuals first

        // Always show the special name text
        if (playerSpecialName) playerSpecialName.gameObject.SetActive(true);

        // Only show the "activate special" prompt when fully charged
        if (activateSpecialGaugeText) activateSpecialGaugeText.gameObject.SetActive(full);
    }

    public void SetSpecialGaugeImmediate(float value, bool playGaugeFullSFX = false)
    {
        specialGauge = Mathf.Clamp(value, 0f, specialGaugeMax);
        UpdateSpecialUI(playGaugeFullSFX);
    }

    void ResetSpecialGauge()
    {
        SetSpecialGaugeImmediate(0f);
    }

    void PlaySpecialGaugeFullSFX()
    {
        if (AudioManager.I && specialGaugeFullSFX)
            AudioManager.I.PlaySFX(specialGaugeFullSFX, specialGaugeFullSFXVolume);
    }

    void SpawnAttackProjectile(Sprite sprite, Sprite altSprite, AttackAnimType animType,
                               Vector2 startAnchored, Vector2 targetAnchored, int damage, MonsterData attackerMD)
    {
        if (!sprite)
        {
            RectTransform impactRoot = projectileRoot ? projectileRoot : (gameBoard ? gameBoard.gridRoot : null);
            AudioClip fallbackImpactClip = attackerMD ? attackerMD.PickRandomAttackSFX() : null;
            ApplyCastleAttackDamage(damage, attackerMD, fallbackImpactClip, impactRoot, targetAnchored);
            SpawnAttackExplosion(impactRoot, targetAnchored);
            return;
        }

        if (projectileRoot == null) projectileRoot = gameBoard.gridRoot;

        Vector2 cellSize = gameBoard.GetCellSize();

        var go = new GameObject("AttackProjectile", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(projectileRoot, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = cellSize;
        rt.anchoredPosition = startAnchored;

        UnityEngine.UI.Image topImg = null;
        UnityEngine.UI.Image botImg = null;

        if (animType == AttackAnimType.MirrorToggle)
        {
            var bot = new GameObject("Bot", typeof(UnityEngine.UI.Image));
            botImg = bot.GetComponent<UnityEngine.UI.Image>();
            botImg.sprite = sprite;
            botImg.preserveAspect = true;
            botImg.raycastTarget = false;

            var brt = botImg.rectTransform;
            brt.SetParent(rt, false);
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = cellSize;
            brt.anchoredPosition = Vector2.zero;
            brt.localScale = Vector3.one;

            var top = new GameObject("Top", typeof(UnityEngine.UI.Image));
            topImg = top.GetComponent<UnityEngine.UI.Image>();
            topImg.sprite = altSprite ? altSprite : sprite;
            topImg.preserveAspect = true;
            topImg.raycastTarget = false;

            var trt = topImg.rectTransform;
            trt.SetParent(rt, false);
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = cellSize;
            trt.anchoredPosition = Vector2.zero;
        }
        else
        {
            var imgGO = new GameObject("Img", typeof(UnityEngine.UI.Image));
            var img = imgGO.GetComponent<UnityEngine.UI.Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var irt = img.rectTransform;
            irt.SetParent(rt, false);
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = cellSize;
            irt.anchoredPosition = Vector2.zero;

            topImg = img;
        }

        // Lock-in the clip once, at spawn time
        AudioClip impactClip = attackerMD ? attackerMD.PickRandomAttackSFX() : null;

        StartCoroutine(MoveProjectileAndHit(rt, topImg, botImg, animType, targetAnchored, damage, attackerMD, impactClip));
    }

    System.Collections.IEnumerator MoveProjectileAndHit(RectTransform rt, UnityEngine.UI.Image topImg,
        UnityEngine.UI.Image botImg, AttackAnimType animType, Vector2 targetAnchored, int damage,
        MonsterData attackerMD, AudioClip impactClip)
    {
        float speed = Mathf.Max(10f, projectileSpeed);

        float toggleT = 0f;
        float toggleInterval = (attackerMD ? Mathf.Max(0.03f, attackerMD.attackToggleInterval) : 0.08f);
        float spinDPS = (attackerMD ? attackerMD.spinDegreesPerSecond : 720f);

        bool topOn = true;

        if (animType == AttackAnimType.MirrorToggle && topImg)
        {
            var c = topImg.color;
            c.a = 1f;
            topImg.color = c;
        }

        while (rt && (rt.anchoredPosition - targetAnchored).sqrMagnitude > 9f)
        {
            rt.anchoredPosition = Vector2.MoveTowards(rt.anchoredPosition, targetAnchored, speed * Time.deltaTime);

            if (animType == AttackAnimType.MirrorToggle && topImg)
            {
                toggleT += Time.deltaTime;
                if (toggleT >= toggleInterval)
                {
                    toggleT = 0f;
                    topOn = !topOn;
                    var c = topImg.color;
                    c.a = topOn ? 1f : 0f;
                    topImg.color = c;
                }
            }
            else if (animType == AttackAnimType.SpinClockwise)
            {
                if (topImg)
                    topImg.rectTransform.Rotate(0f, 0f, -spinDPS * Time.deltaTime);
            }

            yield return null;
        }

        var impactRoot = projectileRoot ? projectileRoot : (gameBoard ? gameBoard.gridRoot : null);
        Vector2 impactPosition = targetAnchored;

        if (rt)
        {
            rt.anchoredPosition = targetAnchored;
            impactPosition = rt.anchoredPosition;
        }

        bool impactEndedLevel = ApplyCastleAttackDamage(damage, attackerMD, impactClip, impactRoot, impactPosition);

        if (rt)
            Destroy(rt.gameObject);

        SpawnAttackExplosion(impactRoot, impactPosition);

        if (impactEndedLevel)
            yield break;
    }

    void SpawnAttackExplosion(RectTransform parent, Vector2 anchoredPosition)
    {
        if (!parent || attackExplosionPrefabs == null || attackExplosionPrefabs.Length == 0)
            return;

        GameObject prefab = PickRandomAttackExplosionPrefab();
        if (!prefab)
            return;

        GameObject go = Instantiate(prefab, parent, false);

        foreach (var graphic in go.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        Vector2 visualPosition = anchoredPosition + GetAttackExplosionOffset();

        if (go.transform is RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = visualPosition;

            if (gameBoard)
                rt.sizeDelta = gameBoard.GetCellSize() * Mathf.Max(0.01f, attackExplosionSizeMultiplier);
        }
        else
        {
            go.transform.localPosition = new Vector3(visualPosition.x, visualPosition.y, 0f);
        }

        Animator animator = go.GetComponent<Animator>();
        if (animator)
        {
            animator.Rebind();
            animator.Update(0f);
            StartCoroutine(DestroyAttackExplosionAfterAnimation(go, animator));
        }
        else
        {
            StartCoroutine(DestroyAttackExplosionAfterDelay(go, 0.5f));
        }
    }

    Vector2 GetAttackExplosionOffset()
    {
        if (!gameBoard)
            return Vector2.zero;

        Vector2 cellSize = gameBoard.GetCellSize();
        return new Vector2(cellSize.x * attackExplosionOffsetCells.x, cellSize.y * attackExplosionOffsetCells.y);
    }

    GameObject PickRandomAttackExplosionPrefab()
    {
        for (int tries = 0; tries < 12; tries++)
        {
            GameObject prefab = attackExplosionPrefabs[Random.Range(0, attackExplosionPrefabs.Length)];
            if (prefab) return prefab;
        }

        for (int i = 0; i < attackExplosionPrefabs.Length; i++)
        {
            if (attackExplosionPrefabs[i]) return attackExplosionPrefabs[i];
        }

        return null;
    }

    IEnumerator DestroyAttackExplosionAfterAnimation(GameObject instance, Animator animator)
    {
        yield return null;

        float elapsed = 0f;
        float timeout = GetAttackExplosionCleanupTimeout(animator);

        while (instance && animator && animator.isActiveAndEnabled && elapsed < timeout)
        {
            if (animator.layerCount <= 0)
                break;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!animator.IsInTransition(0) && state.normalizedTime >= 1f)
                break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (instance)
            Destroy(instance);
    }

    float GetAttackExplosionCleanupTimeout(Animator animator)
    {
        if (animator && animator.layerCount > 0)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.length > 0.01f)
                return state.length + 0.25f;
        }

        float duration = 0.5f;
        if (animator && animator.runtimeAnimatorController)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i])
                    duration = Mathf.Max(duration, clips[i].length);
            }
        }

        return duration + 0.25f;
    }

    IEnumerator DestroyAttackExplosionAfterDelay(GameObject instance, float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delaySeconds));

        if (instance)
            Destroy(instance);
    }

    bool ApplyCastleAttackDamage(int damage, MonsterData attackerMD, AudioClip impactClip,
                                 RectTransform impactRoot = null, Vector2 impactAnchoredPosition = default)
    {
        if (damage <= 0 || !enemyCastleUI || levelWon || gameOver)
            return false;

        int originalDamage = damage;

        AudioClip resolvedImpactClip = impactClip ? impactClip : (attackerMD ? attackerMD.PickRandomAttackSFX() : null);
        if (AudioManager.I && resolvedImpactClip)
            AudioManager.I.PlaySFX(resolvedImpactClip);

        bool pylonsAlive = gameBoard && gameBoard.CountObstaclesOfType(Board.ObstacleType.MagicPylon) > 0;
        if (pylonsAlive)
            damage = Mathf.Max(1, Mathf.CeilToInt(damage * Mathf.Clamp(bossPylonDamageMult, 0.05f, 1f)));

        enemyCastleUI.SetMagicShieldActive(pylonsAlive);
        _bossPylonShieldActive = pylonsAlive;

        if (battleLog)
        {
            string attackerName = attackerMD ? attackerMD.name : "Unknown";
            bool pylonsReduced = pylonsAlive && damage < originalDamage;
            battleLog.LogCastleHit(attackerName, damage, pylonsReduced);
        }

        levelModifierController?.OnCastleProjectileImpact();

        int appliedDamage = enemyCastleUI.ApplyDamage(damage);
        if (appliedDamage > 0)
        {
            RunSummaryStats.RecordDamageDealt(appliedDamage);
            ShowCastleFloatingDamageText(appliedDamage,
                !enemyCastleUI.InfiniteHealth && enemyCastleUI.currentHP <= 0,
                impactRoot,
                impactAnchoredPosition);
        }

        if (enemyCastleUI.currentHP <= 0 && !winQueued)
        {
            winQueued = true;
            StartCoroutine(CoWinAfterDelay(0.25f));
            return true;
        }

        return false;
    }

    IEnumerator CoWinAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        OnCastleDestroyed();
    }


    Vector2 LocalTo(RectTransform dst, RectTransform src, Vector2 anchored)
    {
        var world = src.TransformPoint(new Vector3(anchored.x, anchored.y, 0f));
        var local = dst.InverseTransformPoint(world);
        return new Vector2(local.x, local.y);
    }

    void EnsureMinBag(int min = -1)
    {
        if (min <= 0) min = Mathf.Max(1, minBagPieces);

        // Trim any leading nulls & keep queues aligned
        while (bag.Count > 0 && bag.Peek() == null)
        {
            bag.Dequeue();
            if (monstersBag.Count > 0) monstersBag.Dequeue();
        }

        // Trim monstersBag if it got out of sync
        while (monstersBag.Count > bag.Count && monstersBag.Count > 0)
            monstersBag.Dequeue();

        int safety = 12; // Avoids infinite loops if arrays are misconfigured
        while ((bag.Count < min || monstersBag.Count < min) && safety-- > 0)
            RefillBag();

        // Final trim pass in case RefillBag skipped null entries
        while (bag.Count > 0 && bag.Peek() == null)
        {
            bag.Dequeue();
            if (monstersBag.Count > 0) monstersBag.Dequeue();
        }
    }

    TetrominoData PeekSafeHead()
    {
        EnsureMinBag(3);

        while (bag.Count > 0 && bag.Peek() == null)
        {
            bag.Dequeue();
            if (monstersBag.Count > 0) monstersBag.Dequeue();
        }

        if (bag.Count == 0)
        {
            // Fabricate one piece if the source arrays are misconfigured
            var fallback = System.Array.Find(allTetrominoes, t => t != null);
            if (fallback != null)
            {
                bag.Enqueue(fallback);
                var cells = Mathf.Max(1, fallback.cells != null ? fallback.cells.Length : 1);
                monstersBag.Enqueue(new MonsterData[cells]);
            }
        }

        return bag.Count > 0 ? bag.Peek() : null;
    }

    // ================ Castle Attack System ===================

    void TrySpawnCastleDownshot()
    {
        if (!CanLaunchCastleProjectile())
            return;

        _castleAliveTargetColumns.Clear();
        _castleDeadOnlyTargetColumns.Clear();

        for (int x = 0; x < gameBoard.width; x++)
        {
            bool hasAny = false, hasAlive = false;

            for (int y = gameBoard.height - 1; y >= 0; y--)
            {
                var c = new Vector2Int(x, y);
                if (gameBoard.IsFree(c)) continue;

                hasAny = true;
                if (gameBoard.TryGetMonster(c, out var mi) && mi.data && mi.hp > 0f)
                { hasAlive = true; break; }
            }

            if (hasAlive) _castleAliveTargetColumns.Add(x);
            else if (hasAny) _castleDeadOnlyTargetColumns.Add(x);
        }

        if (_castleAliveTargetColumns.Count == 0 && _castleDeadOnlyTargetColumns.Count == 0)
            return; // No targets on board

        int projectileCount = GetCurrentCastleProjectilesPerAttack();
        var columns = PickCastleProjectileColumns(_castleAliveTargetColumns, _castleDeadOnlyTargetColumns, projectileCount);
        for (int i = 0; i < columns.Count; i++)
            SpawnCastleDownshotInColumn(columns[i]);
    }

    int GetCurrentCastleProjectilesPerAttack()
    {
        if (!gameBoard || gameBoard.width <= 0)
            return 0;

        int baseCount = currentCastleData
            ? Mathf.Max(1, currentCastleData.baseEnemyProjectilesPerAttack)
            : 1;
        int timedBonus = Mathf.Max(0, Mathf.FloorToInt(_levelTimer / 60f));
        return Mathf.Clamp(baseCount + timedBonus, 1, gameBoard.width);
    }

    List<int> PickCastleProjectileColumns(List<int> aliveCols, List<int> deadOnlyCols, int maxCount)
    {
        _castleProjectileColumns.Clear();
        int limit = Mathf.Max(0, maxCount);

        AddRandomCastleProjectileColumns(aliveCols, _castleProjectileColumns, limit);
        AddRandomCastleProjectileColumns(deadOnlyCols, _castleProjectileColumns, limit);

        return _castleProjectileColumns;
    }

    void AddRandomCastleProjectileColumns(List<int> source, List<int> columns, int limit)
    {
        while (source != null && source.Count > 0 && columns.Count < limit)
        {
            int pick = UnityEngine.Random.Range(0, source.Count);
            columns.Add(source[pick]);

            int lastIndex = source.Count - 1;
            source[pick] = source[lastIndex];
            source.RemoveAt(lastIndex);
        }
    }

    void SpawnCastleDownshotInColumn(int col)
    {
        // Compute start/end positions in board/grid space
        Vector2 start = gameBoard.CellToAnchoredPos(new Vector2Int(col, gameBoard.height - 1))
                      + new Vector2(0f, gameBoard.GetCellSize().y * 0.75f);

        Vector2 end = gameBoard.CellToAnchoredPos(new Vector2Int(col, 0))
                    - new Vector2(0f, gameBoard.GetCellSize().y * 0.5f);

        // Convert to projectile root space if needed
        var root = projectileRoot ? projectileRoot : gameBoard.gridRoot;
        if (root != gameBoard.gridRoot)
        {
            start = LocalTo(root, gameBoard.gridRoot, start);
            end = LocalTo(root, gameBoard.gridRoot, end);
        }

        if (AudioManager.I && currentCastleData)
        {
            var fireClip = currentCastleData.PickRandom(
                currentCastleData.sfxProjectileFiredClips,
                currentCastleData.sfxProjectileFired // fallback
            );

            if (fireClip)
                AudioManager.I.PlaySFX(fireClip);
        }

        SpawnCastleDownProjectile(castleProjectileSprite, start, end, col);
    }

    void SpawnCastleDownProjectile(Sprite sprite, Vector2 start, Vector2 end, int column)
    {
        var root = projectileRoot ? projectileRoot : gameBoard.gridRoot;
        var go = new GameObject("CastleDownshot", typeof(UnityEngine.UI.Image));
        var img = go.GetComponent<UnityEngine.UI.Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.SetParent(root, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = gameBoard.GetCellSize() * Mathf.Max(0.1f, castleProjectileVisualScale);
        rt.anchoredPosition = start;

        StartCoroutine(CastleDownshotCo(rt, end, column));
    }

    System.Collections.IEnumerator CastleDownshotCo(RectTransform rt, Vector2 end, int column)
    {
        float speed = GetCurrentCastleProjectileSpeed();
        while (rt && (rt.anchoredPosition - end).sqrMagnitude > 4f)
        {
            Vector2 previous = rt.anchoredPosition;
            rt.anchoredPosition = Vector2.MoveTowards(rt.anchoredPosition, end, speed * Time.deltaTime);

            if (TryGetCastleDownshotImpactCell(column, previous, rt.anchoredPosition, out var hitCell, out bool damageMonster))
            {
                rt.anchoredPosition = BoardLocalToProjectileRoot(gameBoard.CellToAnchoredPos(hitCell));
                Destroy(rt.gameObject);

                ResolveCastleDownshotImpact(hitCell, damageMonster);
                yield break;
            }

            yield return null;
        }

        if (rt) Destroy(rt.gameObject);
    }

    bool TryGetCastleDownshotImpactCell(int column, Vector2 previousRootPos, Vector2 currentRootPos,
                                        out Vector2Int hitCell, out bool damageMonster)
    {
        hitCell = default;
        damageMonster = false;

        if (!gameBoard || column < 0 || column >= gameBoard.width)
            return false;

        Vector2 previousBoard = ProjectileRootToBoardLocal(previousRootPos);
        Vector2 currentBoard = ProjectileRootToBoardLocal(currentRootPos);
        float sweepTop = Mathf.Max(previousBoard.y, currentBoard.y);
        float sweepBottom = Mathf.Min(previousBoard.y, currentBoard.y);
        float halfCell = gameBoard.GetCellSize().y * 0.5f;

        for (int y = gameBoard.height - 1; y >= 0; y--)
        {
            var c = new Vector2Int(column, y);
            float centerY = gameBoard.CellToAnchoredPos(c).y;
            float cellTop = centerY + halfCell;
            float cellBottom = centerY - halfCell;

            if (sweepTop < cellBottom || sweepBottom > cellTop)
                continue;

            if (gameBoard.HasObstacle(c))
            {
                hitCell = c;
                return true;
            }

            if (gameBoard.TryGetMonster(c, out var inst) && inst.data && inst.hp > 0f)
            {
                hitCell = c;
                damageMonster = true;
                return true;
            }
        }

        return false;
    }

    Vector2 ProjectileRootToBoardLocal(Vector2 anchored)
    {
        if (!gameBoard) return anchored;

        var root = projectileRoot ? projectileRoot : gameBoard.gridRoot;
        return root == gameBoard.gridRoot
            ? anchored
            : LocalTo(gameBoard.gridRoot, root, anchored);
    }

    Vector2 BoardLocalToProjectileRoot(Vector2 anchored)
    {
        if (!gameBoard) return anchored;

        var root = projectileRoot ? projectileRoot : gameBoard.gridRoot;
        return root == gameBoard.gridRoot
            ? anchored
            : LocalTo(root, gameBoard.gridRoot, anchored);
    }

    void ResolveCastleDownshotImpact(Vector2Int hitCell, bool damageMonster)
    {
        if (!gameBoard || levelWon || gameOver)
            return;

        AudioClip hitClip = null;
        if (currentCastleData)
            hitClip = currentCastleData.PickRandom(currentCastleData.sfxProjectileHitTileClips,
                                                   currentCastleData.sfxProjectileHitTile);

        if (damageMonster)
        {
            bool aliveAfter = gameBoard.DamageTile(hitCell, castleProjectileDamage,
                                                   Board.DamageSource.CastleProjectile, hitClip);

            if (gameBoard.TryGetMonster(hitCell, out var inst) && inst.data)
                Debug.Log($"Hit {inst.data.name} at {hitCell}: {inst.hp}/{inst.data.maxHealth}"
                          + (aliveAfter ? "" : " (DEAD)"));
        }
        else if (AudioManager.I && hitClip)
        {
            AudioManager.I.PlaySFX(hitClip);
        }
    }

    // ================ Global Immunity System ===================

    System.Collections.IEnumerator GlobalImmunityCo(float totalSeconds)
    {
        if (totalSeconds <= 0f) yield break;

        immunityActive = true;
        gameBoard.SetTilesDamageImmunity(true);

        // Start immunity: gold outlines everywhere
        gameBoard.SetAllTileBorderColor(gameBoard.immuneBorderColor);
        if (piece && piece.enabled) piece.SetInlineBorderColor(gameBoard.immuneBorderColor);

        if (nextPreview)
            nextPreview.SyncBorderToImmunity(true, gameBoard.immuneBorderColor, gameBoard.normalBorderColor);

        if (selectedCharacter && selectedCharacter.sfxImmunityOn && AudioManager.I)
            AudioManager.I.PlaySFX(selectedCharacter.sfxImmunityOn);

        // Steady gold
        float warnWindow = Mathf.Min(3f, totalSeconds);
        float steady = Mathf.Max(0f, totalSeconds - warnWindow);
        for (float t = 0f; t < steady; t += Time.deltaTime) yield return null;

        if (selectedCharacter && selectedCharacter.sfxImmunityWarn && AudioManager.I)
            AudioManager.I.PlaySFX(selectedCharacter.sfxImmunityWarn);

        // Pulse warning
        int sets = 3;
        float[] setDur = { 0.9f, 0.6f, 0.45f };
        for (int s = 0; s < sets; s++)
        {
            float setTime = setDur[Mathf.Clamp(s, 0, setDur.Length - 1)];
            float pulseDur = setTime / 3f;

            for (int p = 0; p < 3; p++)
            {
                // Gold -> Black
                gameBoard.SetAllTileBorderColor(Color.black);
                if (piece && piece.enabled) piece.SetInlineBorderColor(Color.black);

                if (nextPreview)
                    nextPreview.SyncBorderToImmunity(false, gameBoard.immuneBorderColor, gameBoard.normalBorderColor);

                yield return new WaitForSeconds(pulseDur * 0.5f);

                // Black -> Gold
                gameBoard.SetAllTileBorderColor(gameBoard.immuneBorderColor);
                if (piece && piece.enabled) piece.SetInlineBorderColor(gameBoard.immuneBorderColor);

                if (nextPreview)
                    nextPreview.SyncBorderToImmunity(true, gameBoard.immuneBorderColor, gameBoard.normalBorderColor);

                yield return new WaitForSeconds(pulseDur * 0.5f);
            }
        }

        // End immunity: back to black
        gameBoard.SetTilesDamageImmunity(false);
        immunityActive = false;
        gameBoard.SetAllTileBorderColor(gameBoard.normalBorderColor);
        if (piece && piece.enabled) piece.ResetInlineBorderColor(gameBoard.normalBorderColor);

        if (nextPreview)
            nextPreview.SyncBorderToImmunity(false, gameBoard.immuneBorderColor, gameBoard.normalBorderColor);

        if (selectedCharacter && selectedCharacter.sfxImmunityOff && AudioManager.I)
            AudioManager.I.PlaySFX(selectedCharacter.sfxImmunityOff);
    }

    public void StyleInlineBorder(RectTransform rt)
    {
        if (!gameBoard || !rt) return;

        gameBoard.SetInlineBorderColor(rt, immunityActive
            ? gameBoard.immuneBorderColor
            : gameBoard.normalBorderColor);
    }

    // ================ Pause Menu Button Functions ===================

    void RefreshPauseMenuNavigation()
    {
        if (IsPauseMenuInputLocked())
        {
            DisablePauseMenuNavigation();
            return;
        }

        GameObject root = GetCurrentPauseNavigationRoot();
        if (!root)
        {
            DisablePauseMenuNavigation();
            return;
        }

        if (!pauseMenuNavigator)
            pauseMenuNavigator = ScopedMenuNavigator.Attach(pausePanel, root);

        if (!pauseMenuNavigator)
            return;

        pauseMenuNavigator.enabled = true;
        pauseMenuNavigator.SetNavigationRoot(root);
    }

    void DisablePauseMenuNavigation()
    {
        if (pauseMenuNavigator)
            pauseMenuNavigator.enabled = false;
    }

    GameObject GetCurrentPauseNavigationRoot()
    {
        if (!isPaused || !pausePanel || !UIPanelTransition.IsVisible(pausePanel))
            return null;

        if (IsPauseMenuInputLocked())
            return null;

        if (ConfirmationPopupUI.TryGetShowingRoot(out var popupRoot))
            return popupRoot;

        if (volumePanelInPause && UIPanelTransition.IsVisible(volumePanelInPause.gameObject))
            return null;

        if (gameplayStatsPanelUI && gameplayStatsPanelUI.IsVisible)
            return gameplayStatsPanelUI.gameObject;

        if (helpPanelRoot && UIPanelTransition.IsVisible(helpPanelRoot))
            return helpPanelRoot;

        if (runModsPanelRoot && UIPanelTransition.IsVisible(runModsPanelRoot))
            return runModsPanelRoot;

        return pausePanel;
    }

    bool IsPauseMenuInputLocked()
    {
        if (IsPanelTransitioning(pausePanel))
            return true;

        if (IsPanelTransitioning(helpPanelRoot))
            return true;

        if (IsPanelTransitioning(runModsPanelRoot))
            return true;

        if (volumePanelInPause && IsPanelTransitioning(volumePanelInPause.gameObject))
            return true;

        if (gameplayStatsPanelUI && IsPanelTransitioning(gameplayStatsPanelUI.gameObject))
            return true;

        return false;
    }

    static bool IsPanelTransitioning(GameObject panel)
    {
        return panel && UIPanelTransition.IsPanelTransitioning(panel);
    }

    bool HandlePauseMenuCancelInput()
    {
        if (IsPauseMenuInputLocked())
            return false;

        if (!TetrabeastsControls.WasPressed(TetrabeastsControlAction.MenuCancel))
            return false;

        if (ConfirmationPopupUI.TryCancelShowingPopup())
            return true;

        if (volumePanelInPause && UIPanelTransition.IsVisible(volumePanelInPause.gameObject))
            return false;

        if (gameplayStatsPanelUI && gameplayStatsPanelUI.IsVisible)
        {
            gameplayStatsPanelUI.Close();
            RefreshPauseMenuNavigation();
            return true;
        }

        if (helpPanelRoot && UIPanelTransition.IsVisible(helpPanelRoot))
        {
            UIPanelTransition.Hide(helpPanelRoot);
            RefreshPauseMenuNavigation();
            return true;
        }

        if (runModsPanelRoot && UIPanelTransition.IsVisible(runModsPanelRoot))
        {
            UIPanelTransition.Hide(runModsPanelRoot);
            RefreshPauseMenuNavigation();
            return true;
        }

        ResumeGame();
        EnterGameplayCursorMode();
        return true;
    }

    public void PauseGame()
    {
        if (gameOver || levelWon || _levelStartBlocked) return;
        if (isPaused || IsPauseMenuInputLocked()) return;

        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;  // Pause SFX/music globally

        if (AudioManager.I) AudioManager.I.PlayPauseMusic(); // Play pause menu music if assigned
        if (pausePanel) UIPanelTransition.Show(pausePanel);
        RefreshPauseMenuNavigation();

        NotifyTutorialGameplayEvent(TutorialGameplayEvent.PauseOpened);

        EnterUICursorMode();
        StartCoroutine(ReapplyUICursorNextFrame());
    }

    public void RestartGame()
    {
        if (IsPauseMenuInputLocked()) return;

        if (ShouldWarnBeforeDiscardingCurrentRun())
        {
            ShowConfirmationPopup(
                "Restarting will treat this run as a loss. The current temp save will be erased and this run will not be saved. Continue?",
                RestartGameAfterDiscardingTempRun);
            return;
        }

        RestartGameAfterDiscardingTempRun();
    }

    void RestartGameAfterDiscardingTempRun()
    {
        ClearTempRunCheckpoint();

        // If still on level 1 (no run XP earned yet), restart immediately
        if (currentLevel == 0)
        {
            DoRestartGameNow();
            RunMonsterProgress.BeginRun(GetActiveMonsterRoster());
            return;
        }

        // Treat restart like a loss
        if (xpAwardUI && RunMonsterProgress.RunActive)
        {
            OpenXpUiMode();

            if (AudioManager.I) AudioManager.I.PlayIntermissionLoseMusic();

            xpAwardUI.ShowRunEndCommit(
                GetActiveMonsterRoster(),
                GetRunEndXpConversionFraction(HasReachedSuccessfulRunEnd()),
                () =>
                {
                    CloseXpUiMode();
                    DoRestartGameNow();

                    // New run starts from updated permanent progression
                    RunMonsterProgress.BeginRun(GetActiveMonsterRoster());
                });

            return;
        }

        DoRestartGameNow();
        RunMonsterProgress.BeginRun(GetActiveMonsterRoster());
    }

    void DoRestartGameNow()
    {
        TutorialTestingScope.Reset();
        EnsureSpecialBlockTutorials();
        specialBlockTutorials?.ResetRunState();
        tutorialPieceInputBlocked = false;
        _tutorialHardDropInputBlockedUntilRealtime = 0f;
        ClearTempRunCheckpoint();
        HideRoundTransitionImmediate();

        // Unpause if needed
        Time.timeScale = 1f;
        AudioListener.pause = false;
        isPaused = false;
        if (pausePanel) UIPanelTransition.Hide(pausePanel, true);
        DisablePauseMenuNavigation();

        ClosePauseSubPanels(true);

        if (AudioManager.I) AudioManager.I.StopPauseMusic();
        if (AudioManager.I) AudioManager.I.PlaySFX(AudioManager.I.sfxRestart);

        if (piece) piece.ResetPiece(); // Stop the current drop
        if (gameBoard) gameBoard.ClearAll(); // Clear board tiles

        if (_bossGravityCR != null) { StopCoroutine(_bossGravityCR); _bossGravityCR = null; }
        _bossGravityBonusActive = 0f;
        ResetBossGravityVisuals();

        // Reset run state
        levelModifierController?.ResetRunState();
        RunModsStore.ResetAll();
        ResetRunMods();
        RefreshStarDifficultyState();
        RefreshActiveMonsterPassives(applyStartingReserveDelta: false);
        _roundRewardRerollsAvailable = 0;
        _postFinalSurvivalActive = false;
        _pendingPostFinalSurvivalIntro = false;

        // Reset Level 1 difficulty baseline + unit lives
        unitLives = EffectiveMaxUnitLives;
        SetupUnitLivesUI();

        currentLevel = 0;
        ResetRunGridToBase();
        ApplyRunGridSize(currentLevel);
        InitLevel(currentLevel);
        gameOver = false;

        specialGaugeMax = (selectedCharacter && selectedCharacter.specialGaugeMax > 0f)
        ? selectedCharacter.specialGaugeMax
        : 100f;
        ResetSpecialGauge();

        score = 0;
        if (scoreUI) scoreUI.Set(score);

        ResetCombo();
        if (highScoreUI) highScoreUI.Hide(); // Close high-score panel if it was open
        if (victoryPanelUI) victoryPanelUI.Hide();

        RunSummaryStats.BeginRun();
        _finalWinStateApplied = false;
        _demoLimitRunEnding = false;

        // Reset bag and preview
        StartCoroutine(BeginCurrentLevelSequence());
        if (restartButton) restartButton.gameObject.SetActive(true);
    }

    public void ResumeGame()
    {
        if (IsPauseMenuInputLocked())
            return;

        ClosePauseSubPanels(true);

        if (AudioManager.I) AudioManager.I.StopPauseMusic();

        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (AudioManager.I) // Handle music mode changes that were made while paused
            AudioManager.I.ApplyPendingMusicModeAfterUnpause(); 

        if (pausePanel) UIPanelTransition.Hide(pausePanel);
        DisablePauseMenuNavigation();
        NotifyTutorialGameplayEvent(TutorialGameplayEvent.PauseClosed);
        EnterGameplayCursorMode();
    }

    public void ReturnToMainMenu()
    {
        if (IsPauseMenuInputLocked()) return;

        if (ShouldWarnBeforeDiscardingCurrentRun())
        {
            ShowConfirmationPopup(
                "Returning to the main menu will treat this run as a loss. The current temp save will be erased and this run will not be saved. Continue?",
                ReturnToMainMenuAfterDiscardingTempRun);
            return;
        }

        ReturnToMainMenuAfterDiscardingTempRun();
    }

    void ReturnToMainMenuAfterDiscardingTempRun()
    {
        ClearTempRunCheckpoint();

        if (currentLevel == 0)
        {
            _pendingMainMenuAfterXp = false;
            DoReturnToMainMenuNow();
            return;
        }

        if (xpAwardUI && RunMonsterProgress.RunActive)
        {
            _pendingMainMenuAfterXp = true;

            OpenXpUiMode();
            if (AudioManager.I) AudioManager.I.PlayIntermissionLoseMusic();

            xpAwardUI.ShowRunEndCommit(
                GetActiveMonsterRoster(),
                GetRunEndXpConversionFraction(HasReachedSuccessfulRunEnd()),
                () =>
                {
                    _pendingMainMenuAfterXp = false;

                    DoReturnToMainMenuNow(); // Keep XP UI visible to avoid flashing the gameplay scene
                }, 
                hideOnFinalContinue: false);

            return;
        }

        _pendingMainMenuAfterXp = false;
        DoReturnToMainMenuNow();
    }

    void DoReturnToMainMenuNow()
    {
        ClearTempRunCheckpoint();

        if (_pendingMainMenuAfterXp && xpAwardUI && xpAwardUI.gameObject.activeInHierarchy)
            return;

        Time.timeScale = 1f;
        isPaused = false;

        if (AudioManager.I)
        {
            AudioManager.I.StopPauseMusic();
            AudioManager.I.StopLevelMusic();
        }

        AudioListener.pause = false;

        levelModifierController?.ResetRunState();

        if (!string.IsNullOrEmpty(titleSceneName))
        {
            TetrabeastsControls.SuppressMenuSubmit(0.35f);
            if (!LoadingScreen.LoadSceneAsync(titleSceneName))
                Debug.LogError("GameController: failed to start loading title scene.");
        }
        else
            Debug.LogError("GameController.titleSceneName is empty or not set.");
    }

    public void RequestSaveAndQuit()
    {
        if (IsPauseMenuInputLocked()) return;

        ShowConfirmationPopup(
            "Save this run and quit the game? Continuing later will resume from the start of the current level checkpoint. While a run is saved, you will not be able to change your commander, squad, or shop buffs from the title menu.",
            ConfirmSaveAndQuit);
    }

    void ConfirmSaveAndQuit()
    {
        if (!SaveTempRunForQuit())
        {
            ShowAlertPopup("The run could not be temp-saved, so the game will stay open.");
            return;
        }

        QuitGameNow();
    }

    public void QuitGame()
    {
        if (IsPauseMenuInputLocked()) return;

        ShowConfirmationPopup(
            QuitWithoutSavingWarningMessage,
            ConfirmQuitWithoutSaving);
    }

    void ConfirmQuitWithoutSaving()
    {
        QuitGameNow();
    }

    void QuitGameNow()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    bool ShouldWarnBeforeDiscardingCurrentRun() => !gameOver && !levelWon;

    void ShowConfirmationPopup(string body, System.Action onConfirm, System.Action onCancel = null)
    {
        var popup = ConfirmationPopupUI.FindOrCreate();
        if (popup)
        {
            popup.ShowConfirmation(body, onConfirm, onCancel ?? (() => { }), showWarningVisual: true);
            return;
        }

        onConfirm?.Invoke();
    }

    void ShowAlertPopup(string body, System.Action onClosed = null, string continueText = "OK")
    {
        var popup = ConfirmationPopupUI.FindOrCreate();
        if (popup)
        {
            popup.ShowAlert(body, onClosed, continueText, showWarningVisual: true);
            return;
        }

        Debug.LogWarning(body);
        onClosed?.Invoke();
    }

    public void OpenRunModsPanel()
    {
        if (!runModsPanelRoot) return;
        if (IsPauseMenuInputLocked()) return;

        if (runModsPanelUI) runModsPanelUI.Refresh(); // Make sure the list is up to date before showing

        if (gameplayStatsPanelUI && gameplayStatsPanelUI.IsVisible)
            gameplayStatsPanelUI.Close();

        UIPanelTransition.Show(runModsPanelRoot);
        RefreshPauseMenuNavigation();
    }

    public void CloseRunModsPanel()
    {
        if (IsPauseMenuInputLocked()) return;

        if (runModsPanelRoot) UIPanelTransition.Hide(runModsPanelRoot);
        RefreshPauseMenuNavigation();
    }

    public void OpenGameplayStatsPanel()
    {
        if (IsPauseMenuInputLocked()) return;

        SetupGameplayStatsPanel();
        if (!gameplayStatsPanelUI)
            return;

        if (helpPanelRoot && UIPanelTransition.IsVisible(helpPanelRoot))
            UIPanelTransition.Hide(helpPanelRoot);

        if (runModsPanelRoot && UIPanelTransition.IsVisible(runModsPanelRoot))
            UIPanelTransition.Hide(runModsPanelRoot);

        if (volumePanelInPause && UIPanelTransition.IsVisible(volumePanelInPause.gameObject))
            volumePanelInPause.Close();

        gameplayStatsPanelUI.Open(this);
        RefreshPauseMenuNavigation();
    }

    public void CloseGameplayStatsPanel(bool instant = false)
    {
        if (!instant && IsPauseMenuInputLocked()) return;

        if (gameplayStatsPanelUI)
            gameplayStatsPanelUI.Close(instant);

        RefreshPauseMenuNavigation();
    }

    void SetupGameplayStatsPanel()
    {
        if (!gameplayStatsPanelUI)
            gameplayStatsPanelUI = FindFirstObjectByType<GameplayStatsPanelUI>(FindObjectsInactive.Include);

        if (!gameplayStatsPanelUI && gameplayStatsPanelPrefab && pausePanel)
        {
            GameObject instance = Instantiate(gameplayStatsPanelPrefab, pausePanel.transform);
            instance.name = gameplayStatsPanelPrefab.name;
            gameplayStatsPanelUI = instance.GetComponent<GameplayStatsPanelUI>();
            if (!gameplayStatsPanelUI)
                gameplayStatsPanelUI = instance.AddComponent<GameplayStatsPanelUI>();
        }

        if (!gameplayStatsPanelUI && pausePanel)
            gameplayStatsPanelUI = GameplayStatsPanelUI.Create(this, pausePanel.transform, gameplayStatsMonsterPrefab);

        if (gameplayStatsPanelUI)
            gameplayStatsPanelUI.Initialize(this, gameplayStatsMonsterPrefab);

        if (!openGameplayStatsButton && pausePanel)
            openGameplayStatsButton = FindPauseMenuButton("Stats_Button");

        if (openGameplayStatsButton)
        {
            openGameplayStatsButton.onClick.RemoveListener(OpenGameplayStatsPanel);
            openGameplayStatsButton.onClick.AddListener(OpenGameplayStatsPanel);
        }
    }

    Button FindPauseMenuButton(string buttonName)
    {
        if (!pausePanel || string.IsNullOrWhiteSpace(buttonName))
            return null;

        Button[] buttons = pausePanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button && button.name == buttonName)
                return button;
        }

        return null;
    }

    void ClosePauseSubPanels(bool instant = false)
    {
        // Close Help panel
        if (helpPanelRoot && UIPanelTransition.IsVisible(helpPanelRoot))
            UIPanelTransition.Hide(helpPanelRoot, instant);

        // Close Run Mods
        if (runModsPanelRoot && UIPanelTransition.IsVisible(runModsPanelRoot))
            UIPanelTransition.Hide(runModsPanelRoot, instant);

        // Close Gameplay Stats
        if (gameplayStatsPanelUI && gameplayStatsPanelUI.IsVisible)
            gameplayStatsPanelUI.Close(instant);

        // Close Volume settings panel
        if (volumePanelInPause && UIPanelTransition.IsVisible(volumePanelInPause.gameObject))
            volumePanelInPause.Close(instant);

        RefreshPauseMenuNavigation();
    }

    // ================ Currency Popup System ===================

    void ShowCurrencyPopup(Vector2 anchoredStart, int amount)
    {
        if (!gameBoard) return; // Hard guard

        var parent = projectileRoot ? projectileRoot : gameBoard.gridRoot;
        if (!parent) return; // Nothing to parent to

        // container
        var go = new GameObject("Currency+1", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredStart;

        // Icon component
        var iconGO = new GameObject("Icon", typeof(UnityEngine.UI.Image));
        var icon = iconGO.GetComponent<UnityEngine.UI.Image>();
        if (currencyPopupSprite) icon.sprite = currencyPopupSprite; // Sprite optional
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var cell = gameBoard.GetCellSize();
        var irt = icon.rectTransform;
        irt.SetParent(rt, false);
        irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
        irt.sizeDelta = cell * 0.5f;
        irt.anchoredPosition = Vector2.left * (irt.sizeDelta.x * 0.6f);

        // Text component
        var textGO = new GameObject("Text", typeof(TMPro.TextMeshProUGUI));
        var tmp = textGO.GetComponent<TMPro.TextMeshProUGUI>();
        tmp.text = $"+{amount}";
        tmp.fontSize = 24f;
        tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        tmp.color = new Color(1f, 1f, 1f, 1f);

        var trt = tmp.rectTransform;
        trt.SetParent(rt, false);
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = Vector2.right * (irt.sizeDelta.x * 0.6f);

        StartCoroutine(CurrencyPopupCo(rt, tmp, icon, currencyPopupDuration));
    }

    System.Collections.IEnumerator CurrencyPopupCo(RectTransform rt, TMPro.TMP_Text tmp, UnityEngine.UI.Image icon, float dur)
    {
        float t = 0f;
        var start = rt.anchoredPosition;
        var end = start + new Vector2(0f, gameBoard.GetCellSize().y * 0.6f);

        while (t < dur && rt)
        {
            t += Time.deltaTime;
            float u = t / dur;

            rt.anchoredPosition = Vector2.LerpUnclamped(start, end, u);

            float a = Mathf.Lerp(1f, 0f, u);
            if (tmp) tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, a);
            if (icon) icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, a);
            yield return null;
        }
        if (rt) Destroy(rt.gameObject);
    }

    int GetRoundWinCurrency()
    {
        int baseAmt = currencyPerRoundWin; // Base is inspector value
        float raw = (baseAmt + currencyPerRoundWinAdd) * currencyPerRoundWinMult * CurrentCurrencyGainMultiplier; // Apply run mods and passives

        return Mathf.Max(0, Mathf.RoundToInt(raw)); // Round and clamp
    }

    // ================ Level Timer and Fall Interval System ===================

    void ResetLevelTimerAndDrop(int levelIndex)
    {
        _levelTimer = 0f;
        _lastShownLevelTimerSeconds = -1;
        _gravityCapAccumSeconds = 0f;
        ResetPlayerGravitySpecialEffect();
        ResetSlowGravitySpecialEffect();

        int displayedLevel = Mathf.Max(1, levelIndex + 1);
        float levelBonus = Mathf.Max(0, displayedLevel - 1) * Mathf.Max(0f, levelBaseGravityIncrease);
        float baseGravity = 1f / Mathf.Max(0.0001f, _level1FallInterval);
        float levelStartGravity = Mathf.Max(0.0001f, baseGravity + levelBonus);

        _thisLevelBaseFallInterval = Mathf.Max(
            CurrentGravityMinFallInterval,
            1f / levelStartGravity
        );

        float interval = GetCurrentFallInterval();

        if (piece)
            piece.SetFallInterval(interval, resetAccumulator: true);

        UpdateGravityText(interval);
        UpdateLevelTimerUI();
    }

    void ResetSlowGravitySpecialEffect()
    {
        if (_slowGravitySpecialCR != null)
        {
            StopCoroutine(_slowGravitySpecialCR);
            _slowGravitySpecialCR = null;
        }

        _slowGravitySpecialMultActive = 1f;
        _slowGravitySpecialRampRateMultActive = 1f;
        _slowGravitySpecialVisualActive = false;
        ClearTimedSlowGravityEffect(TimedSlowGravitySource.SpecialBlock);
        gameBoard?.StopSlowGravityBoardVFX();
        EnsureSlowGravityImage();
        if (slowGravityImage)
            slowGravityImage.gameObject.SetActive(false);

        RefreshGravityTextColor();
    }

    public void ActivateSlowGravitySpecial(float gravityMultiplier, float rampRateMultiplier)
    {
        float minMultiplier = Mathf.Clamp(minSlowGravitySpecialMultiplier, 0.01f, 1f);
        float gravityMult = Mathf.Clamp(gravityMultiplier, minMultiplier, 1f);
        float durationSeconds = Mathf.Max(0.1f, slowGravitySpecialDurationSeconds);
        float projectedInterval = CalculateFallInterval(
            playerGravityMult: 1f,
            playerBaseOverrideActive: false,
            slowGravitySpecialMult: gravityMult,
            slowGravitySpecialRampRateMult: 1f);

        if (!ShouldReplaceTimedSlowGravityEffect(
                TimedSlowGravitySource.SpecialBlock,
                projectedInterval,
                durationSeconds))
        {
            return;
        }

        ResetPlayerGravitySpecialEffect();

        if (_slowGravitySpecialCR != null)
            StopCoroutine(_slowGravitySpecialCR);

        _slowGravitySpecialMultActive = gravityMult;
        _slowGravitySpecialRampRateMultActive = 1f;
        _slowGravitySpecialVisualActive = true;
        EnsureSlowGravityImage();
        if (slowGravityImage)
            slowGravityImage.gameObject.SetActive(true);

        float interval = GetCurrentFallInterval();
        if (piece && piece.enabled)
            piece.SetFallInterval(interval, resetAccumulator: true);

        _lastShownFallInterval = -1f;
        UpdateGravityText(interval);
        RefreshGravityTextColor();
        SetTimedSlowGravityEffect(TimedSlowGravitySource.SpecialBlock, durationSeconds);
        gameBoard?.PlaySlowGravityBoardVFX(
            durationSeconds,
            () => _activeTimedSlowGravitySource == TimedSlowGravitySource.SpecialBlock
                ? _activeTimedSlowGravityRemainingSeconds
                : 0f);
        _slowGravitySpecialCR = StartCoroutine(SlowGravitySpecialCo(durationSeconds));
    }

    IEnumerator SlowGravitySpecialCo(float seconds)
    {
        yield return TickTimedSlowGravityEffect(TimedSlowGravitySource.SpecialBlock, seconds);

        _slowGravitySpecialCR = null;
        ResetSlowGravitySpecialEffect();
        RefreshCurrentFallInterval(resetAccumulator: false);
    }

    float GetCurrentFallInterval()
    {
        return CalculateFallInterval(
            _playerGravityMultActive,
            _playerGravityBaseOverrideActive,
            _slowGravitySpecialMultActive,
            _slowGravitySpecialRampRateMultActive);
    }

    float GetCurrentGravityCellsPerSecond()
    {
        float interval = GetCurrentFallInterval();
        return interval > 0.0001f ? 1f / interval : 0f;
    }

    float GetLevelOneBaseGravityCellsPerSecond()
    {
        float levelOneInterval = _level1FallInterval > 0f ? _level1FallInterval : startFallInterval;
        return 1f / Mathf.Max(0.0001f, levelOneInterval);
    }

    float GetCurrentCastleProjectileSpeed()
    {
        float baseSpeed = currentCastleData
            ? Mathf.Max(1f, currentCastleData.projectileSpeed) * Mathf.Max(0.01f, enemyProjectileSpeedMult)
            : Mathf.Max(1f, projectileSpeed);
        float baseGravity = GetLevelOneBaseGravityCellsPerSecond();
        float currentGravity = GetCurrentGravityCellsPerSecond();
        float gravityRatio = baseGravity > 0.0001f
            ? Mathf.Max(1f, currentGravity / baseGravity)
            : 1f;
        float exponent = currentCastleData ? Mathf.Max(0f, currentCastleData.projectileGravitySpeedExponent) : 0.5f;

        return Mathf.Max(10f, baseSpeed * Mathf.Pow(gravityRatio, exponent));
    }

    float CalculateFallInterval(
        float playerGravityMult,
        bool playerBaseOverrideActive,
        float slowGravitySpecialMult,
        float slowGravitySpecialRampRateMult)
    {
        float baseGravity = 1f / Mathf.Max(0.0001f, _thisLevelBaseFallInterval);
        float gravityRamp = playerBaseOverrideActive
            ? 0f
            : Mathf.Max(0f, gravityIncreasePerSecond) *
              Mathf.Max(0f, CalculateFallRampRateMultiplier(slowGravitySpecialRampRateMult)) *
              _levelTimer;
        float interval = 1f / Mathf.Max(0.0001f, baseGravity + gravityRamp);

        interval /= Mathf.Max(
            0.01f,
            CalculatePieceGravityMultiplier(playerGravityMult, slowGravitySpecialMult) *
            ActiveLevelModifierGravityMult);

        return Mathf.Max(CurrentGravityMinFallInterval, interval);
    }

    float CalculatePieceGravityMultiplier(float playerGravityMult, float slowGravitySpecialMult)
    {
        return Mathf.Max(0.01f, pieceGravityMult) *
            ShopBuffEffects.GravityMultiplier *
            Mathf.Max(0.01f, playerGravityMult) *
            Mathf.Max(0.01f, slowGravitySpecialMult) *
            (1f + _bossGravityBonusActive);
    }

    float CalculateFallRampRateMultiplier(float slowGravitySpecialRampRateMult)
    {
        return Mathf.Max(0f, fallRampRateMult) *
            ShopBuffEffects.VelocityMultiplier *
            Mathf.Max(0f, slowGravitySpecialRampRateMult);
    }

    bool ShouldReplaceTimedSlowGravityEffect(
        TimedSlowGravitySource source,
        float projectedInterval,
        float durationSeconds)
    {
        if (_activeTimedSlowGravitySource == TimedSlowGravitySource.None)
            return true;

        if (_activeTimedSlowGravitySource == source)
            return true;

        if (_activeTimedSlowGravityRemainingSeconds <= 0f)
            return true;

        float currentInterval = GetCurrentFallInterval();
        if (projectedInterval > currentInterval + 0.001f)
            return true;

        return durationSeconds > _activeTimedSlowGravityRemainingSeconds + 0.1f;
    }

    void SetTimedSlowGravityEffect(TimedSlowGravitySource source, float durationSeconds)
    {
        _activeTimedSlowGravitySource = source;
        _activeTimedSlowGravityRemainingSeconds = Mathf.Max(0.1f, durationSeconds);
        _lastShownTimedSlowGravitySeconds = -1;
        UpdateTimedSlowGravityTimerUI();
    }

    void ClearTimedSlowGravityEffect(TimedSlowGravitySource source)
    {
        if (source != TimedSlowGravitySource.None && _activeTimedSlowGravitySource != source)
            return;

        _activeTimedSlowGravitySource = TimedSlowGravitySource.None;
        _activeTimedSlowGravityRemainingSeconds = 0f;
        _lastShownTimedSlowGravitySeconds = -1;
        SetGravityTimerVisible(false);
    }

    IEnumerator TickTimedSlowGravityEffect(TimedSlowGravitySource source, float seconds)
    {
        if (_activeTimedSlowGravitySource != source)
            SetTimedSlowGravityEffect(source, seconds);

        while (_activeTimedSlowGravitySource == source && _activeTimedSlowGravityRemainingSeconds > 0f)
        {
            if (ShouldTickTimedSlowGravityEffect())
                _activeTimedSlowGravityRemainingSeconds -= Time.deltaTime;

            UpdateTimedSlowGravityTimerUI();
            yield return null;
        }
    }

    bool ShouldTickTimedSlowGravityEffect()
    {
        return IsRoundActive &&
               !tutorialSuspended &&
               !isPaused &&
               !_roundTransitionActive &&
               !_specialAbilityCinematicActive &&
               !ConfirmationPopupUI.IsAnyShowing &&
               !IsTutorialPromptActive;
    }

    void UpdateTimedSlowGravityTimerUI()
    {
        EnsureGravityTimerText();
        if (!gravityTimerText)
            return;

        if (_activeTimedSlowGravitySource == TimedSlowGravitySource.None ||
            _activeTimedSlowGravityRemainingSeconds <= 0f)
        {
            SetGravityTimerVisible(false);
            return;
        }

        int seconds = Mathf.Max(1, Mathf.CeilToInt(_activeTimedSlowGravityRemainingSeconds));
        SetGravityTimerVisible(true);

        if (seconds == _lastShownTimedSlowGravitySeconds)
            return;

        _lastShownTimedSlowGravitySeconds = seconds;
        gravityTimerText.text = seconds.ToString();
    }

    void SetGravityTimerVisible(bool visible)
    {
        EnsureGravityTimerText();
        if (gravityTimerText && gravityTimerText.gameObject.activeSelf != visible)
            gravityTimerText.gameObject.SetActive(visible);
    }

    void EnsureGravityTimerText()
    {
        if (gravityTimerText)
            return;

        Transform timer = null;
        if (gravityText)
            timer = gravityText.transform.Find("GravityTimer_Text");

        if (!timer && gravityText && gravityText.transform.parent)
            timer = gravityText.transform.parent.Find("GravityTimer_Text");

        if (timer)
            gravityTimerText = timer.GetComponent<TMP_Text>();
    }

    void UpdateLevelTimerUI()
    {
        if (!levelTimerText) return;

        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(_levelTimer));
        if (totalSeconds == _lastShownLevelTimerSeconds)
            return;

        _lastShownLevelTimerSeconds = totalSeconds;
        int mins = totalSeconds / 60;
        int secs = totalSeconds % 60;
        levelTimerText.SetText("{0:00}:{1:00}", mins, secs);
    }

    void UpdateGravityText(float currentInterval)
    {
        if (!gravityText) return;

        // Avoid spamming string rebuilds if interval hasn't meaningfully changed
        if (_lastShownFallInterval >= 0f && Mathf.Abs(_lastShownFallInterval - currentInterval) < 0.001f)
            return;

        _lastShownFallInterval = currentInterval;

        float cellsPerSecond = (currentInterval > 0.0001f) ? (1f / currentInterval) : 0f;
        gravityText.text = TetrabeastsLocalization.LocalizeFormat("Gravity: {0:0.0}", cellsPerSecond);
    }

    void ResetPlayerGravitySpecialEffect()
    {
        if (_playerGravityCR != null)
        {
            StopCoroutine(_playerGravityCR);
            _playerGravityCR = null;
        }

        _playerGravityMultActive = 1f;
        _playerGravityBaseOverrideActive = false;
        ClearTimedSlowGravityEffect(TimedSlowGravitySource.PlayerAbility);
        RefreshGravityTextColor();
    }

    void RefreshCurrentFallInterval(bool resetAccumulator)
    {
        float interval = GetCurrentFallInterval();
        if (piece && piece.enabled)
            piece.SetFallInterval(interval, resetAccumulator);

        _lastShownFallInterval = -1f;
        UpdateGravityText(interval);
    }

    void EnsureSlowGravityImage()
    {
        if (slowGravityImage || !gravityText)
            return;

        Transform child = gravityText.transform.Find("SlowGravity_Image");
        if (!child)
            child = gravityText.transform.Find("SlowGravitu_Image");

        if (child)
            slowGravityImage = child.GetComponent<Image>();
    }

    void RefreshGravityTextColor()
    {
        if (!gravityText)
            return;

        if (_bossGravityVisualActive)
            gravityText.color = gravityTextBossColor;
        else if (_playerGravityBaseOverrideActive || _slowGravitySpecialVisualActive)
            gravityText.color = gravityTextSlowColor;
        else
            gravityText.color = gravityTextDefaultColor;
    }

    // ================ Unit Lives System ===================

    void SetupUnitLivesUI()
    {
        if (unitLivesSlider)
        {
            unitLivesSlider.minValue = 0;
            unitLivesSlider.maxValue = EffectiveMaxUnitLives;

            _unitLivesFillImg = unitLivesSlider.fillRect ? unitLivesSlider.fillRect.GetComponent<Image>() : null;
            if (_unitLivesFillImg)
                _unitLivesFillDefaultColor = _unitLivesFillImg.color;

            _unitLivesBarRect = unitLivesSlider.GetComponent<RectTransform>();
            if (_unitLivesBarRect)
                _unitLivesBarDefaultPos = _unitLivesBarRect.anchoredPosition;
        }

        _lastUnitLivesGlassState = null;
        UpdateUnitLivesUI();
    }

    void FlashUnitLivesFill()
    {
        if (!_unitLivesFillImg) return;

        if (_unitLivesFlashCR != null)
            StopCoroutine(_unitLivesFlashCR);

        _unitLivesFlashCR = StartCoroutine(CoFlashUnitLivesFill());
    }

    IEnumerator CoFlashUnitLivesFill()
    {
        _unitLivesFillImg.color = unitLivesFlashColor;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, unitLivesFlashSeconds));
        _unitLivesFillImg.color = _unitLivesFillDefaultColor;
        _unitLivesFlashCR = null;
    }

    void ShakeUnitLivesBar()
    {
        if (!_unitLivesBarRect) return;

        if (_unitLivesShakeCR != null)
            StopCoroutine(_unitLivesShakeCR);

        _unitLivesShakeCR = StartCoroutine(CoShakeUnitLivesBar());
    }

    IEnumerator CoShakeUnitLivesBar()
    {
        float dur = Mathf.Max(0.01f, unitLivesShakeSeconds);
        float amp = Mathf.Max(0f, unitLivesShakeAmplitude);
        float hz = Mathf.Max(1f, unitLivesShakeHz);

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float phase = t * (Mathf.PI * 2f) * hz;

            float x = Mathf.Sin(phase) * amp;
            float y = Mathf.Cos(phase * 0.97f) * amp * 0.6f;

            _unitLivesBarRect.anchoredPosition = _unitLivesBarDefaultPos + new Vector2(x, y);
            yield return null;
        }

        _unitLivesBarRect.anchoredPosition = _unitLivesBarDefaultPos;
        _unitLivesShakeCR = null;
    }

    void UpdateUnitLivesUI()
    {
        int max = EffectiveMaxUnitLives;

        if (unitLivesText)
            unitLivesText.text = $"{unitLives} / {max}";

        if (unitLivesSlider)
        {
            unitLivesSlider.maxValue = max;
            unitLivesSlider.value = unitLives;
        }

        RefreshUnitLivesGlassOverlay(unitLives, max);
    }

    UnitLivesGlassState GetUnitLivesGlassState(int current, int max)
    {
        if (max <= 0)
            return UnitLivesGlassState.Low;

        float pct = Mathf.Clamp01((float)current / max);

        if (pct >= 0.66f)
            return UnitLivesGlassState.High;

        if (pct >= 0.33f)
            return UnitLivesGlassState.Mid;

        return UnitLivesGlassState.Low;
    }

    void RefreshUnitLivesGlassOverlay(int current, int max)
    {
        if (!unitLivesGlassOverlayImage)
            return;

        UnitLivesGlassState nextState = GetUnitLivesGlassState(current, max);
        int spriteIndex = (int)nextState;

        if (unitLivesGlassOverlaySprites != null &&
            spriteIndex >= 0 &&
            spriteIndex < unitLivesGlassOverlaySprites.Length)
        {
            unitLivesGlassOverlayImage.sprite = unitLivesGlassOverlaySprites[spriteIndex];
        }

        bool shouldPlayCrack =
            _lastUnitLivesGlassState.HasValue &&
            (int)nextState > (int)_lastUnitLivesGlassState.Value;

        if (shouldPlayCrack && AudioManager.I)
            AudioManager.I.PlayRandomGlassCrack(unitLivesGlassCrackSfxVolume);

        _lastUnitLivesGlassState = nextState;
    }

    void OnBoardTileDied(Vector2Int cell, MonsterData data)
    {
        if (battleLog && data)
            battleLog.LogDeath(data.name);

        levelModifierController?.OnTileDied(cell, data);

        if (gameOver) return;

        // Death SFX
        if (AudioManager.I)
            AudioManager.I.PlayMonsterDieSFX(vol: 1.8f);

        unitLives = Mathf.Max(0, unitLives - 1);
        RunSummaryStats.AddUnitsDied();
        UpdateUnitLivesUI();

        FlashUnitLivesFill(); // Flash reserve slider fill red briefly
        ShakeUnitLivesBar();  // Shake whole reserve bar briefly

        if (unitLives <= 0)
            GameOver();
    }

    void OnBoardTileDamaged(Vector2Int cell, MonsterData data, float amount, Board.DamageSource src)
    {
        levelModifierController?.OnTileDamaged(cell, data, amount, src);

        if (!data) return;

        int v = Mathf.RoundToInt(Mathf.Max(0f, amount));
        if (v <= 0) return;

        bool killingBlow = gameBoard && gameBoard.TryGetMonster(cell, out var inst) && inst.hp <= 0f;
        ShowUnitFloatingDamageText(cell, v, src, killingBlow);

        if (!battleLog) return;

        GetDamageTextParts(src, out string damageTypeWord, out Color32? damageTypeColor, out string fromLabel);

        battleLog.LogDamageDetailed(data.name, v, damageTypeWord, damageTypeColor, fromLabel);
    }

    void EnsureFloatingDamageText()
    {
        if (!floatingDamageText)
            floatingDamageText = GetComponent<FloatingDamageText>();

        if (!floatingDamageText)
            floatingDamageText = FindFirstObjectByType<FloatingDamageText>(FindObjectsInactive.Include);

        if (!floatingDamageText)
            floatingDamageText = gameObject.AddComponent<FloatingDamageText>();

        RectTransform fallback = projectileRoot ? projectileRoot : (gameBoard ? gameBoard.gridRoot : null);
        if (floatingDamageText)
            floatingDamageText.SetFallbackRoot(fallback);
    }

    void ShowUnitFloatingDamageText(Vector2Int cell, int amount, Board.DamageSource src, bool killingBlow)
    {
        if (amount <= 0 || !gameBoard)
            return;

        EnsureFloatingDamageText();
        if (!floatingDamageText)
            return;

        FloatingDamageText.DamageKind kind = FloatingDamageKindForSource(src);

        if (gameBoard.TryGetTileRect(cell, out var tileRect) && tileRect)
        {
            floatingDamageText.Show(tileRect, amount, kind, killingBlow);
            return;
        }

        if (gameBoard.gridRoot)
            floatingDamageText.ShowAtLocalPosition(gameBoard.gridRoot, gameBoard.CellToAnchoredPos(cell),
                gameBoard.GetCellSize(), amount, kind, killingBlow);
    }

    void ShowCastleFloatingDamageText(int amount, bool killingBlow, RectTransform impactRoot = null,
                                      Vector2 impactAnchoredPosition = default)
    {
        if (amount <= 0 || !enemyCastleUI)
            return;

        EnsureFloatingDamageText();
        if (!floatingDamageText)
            return;

        if (impactRoot)
        {
            floatingDamageText.ShowAtLocalPosition(
                impactRoot,
                impactAnchoredPosition,
                Vector2.zero,
                amount,
                FloatingDamageText.DamageKind.Normal,
                killingBlow);
            return;
        }

        RectTransform target = null;
        if (enemyCastleUI.bossOverlayImage && enemyCastleUI.bossOverlayImage.enabled)
            target = enemyCastleUI.bossOverlayImage.rectTransform;
        else if (enemyCastleUI.castleImage)
            target = enemyCastleUI.castleImage.rectTransform;

        if (target)
            floatingDamageText.Show(target, amount, FloatingDamageText.DamageKind.Normal, killingBlow);
    }

    FloatingDamageText.DamageKind FloatingDamageKindForSource(Board.DamageSource src)
    {
        return src switch
        {
            Board.DamageSource.FloorBurn => FloatingDamageText.DamageKind.Fire,
            Board.DamageSource.FloorPoison => FloatingDamageText.DamageKind.Poison,
            Board.DamageSource.FloorLightning => FloatingDamageText.DamageKind.Lightning,
            Board.DamageSource.FloorSpike => FloatingDamageText.DamageKind.Spike,
            Board.DamageSource.Contagion => FloatingDamageText.DamageKind.Contagion,
            Board.DamageSource.Rations => FloatingDamageText.DamageKind.Rations,
            Board.DamageSource.DeathExplosion => FloatingDamageText.DamageKind.DeathExplosion,
            Board.DamageSource.BossAbility => FloatingDamageText.DamageKind.BossAbility,
            Board.DamageSource.MagicExplosive => FloatingDamageText.DamageKind.MagicExplosive,
            Board.DamageSource.Overgrowth => FloatingDamageText.DamageKind.Overgrowth,
            Board.DamageSource.RearAmbush => FloatingDamageText.DamageKind.RearAmbush,
            _ => FloatingDamageText.DamageKind.Normal
        };
    }

    string SourceLabel(Board.DamageSource src)
    {
        return src switch
        {
            Board.DamageSource.CastleProjectile => "Castle Projectile",
            Board.DamageSource.Generic => "Damage",
            _ => src.ToString()
        };
    }

    void OnBoardTileHealed(Vector2Int cell, MonsterData target, MonsterData source, float amount)
    {
        levelModifierController?.OnTileHealed(cell, target, source, amount);

        if (amount > 0f)
            RunSummaryStats.AddHealingDone(amount);

        if (!battleLog || !target || !source) return;

        int v = Mathf.RoundToInt(Mathf.Max(0f, amount));
        if (v <= 0) return;

        battleLog.LogHealDetailed(source.name, v, target.name);
    }

    void GetDamageTextParts(Board.DamageSource src, out string damageTypeWord, out Color32? damageTypeColor, out string fromLabel)
    {
        damageTypeWord = null;
        damageTypeColor = null;
        fromLabel = null;

        switch (src)
        {
            case Board.DamageSource.FloorPoison:
                damageTypeWord = "poison";
                damageTypeColor = GetBattleLogPoisonColor();
                fromLabel = "floor effect";
                break;

            case Board.DamageSource.FloorBurn:
                damageTypeWord = "fire";
                damageTypeColor = GetBattleLogFireColor();
                fromLabel = "floor effect";
                break;

            case Board.DamageSource.FloorLightning:
                damageTypeWord = "lightning";
                damageTypeColor = GetBattleLogLightningColor();
                fromLabel = "storm";
                break;

            case Board.DamageSource.Contagion:
                damageTypeWord = "contagion";
                damageTypeColor = GetBattleLogContagionColor();
                fromLabel = "infection";
                break;

            case Board.DamageSource.Rations:
                damageTypeWord = "starvation";
                damageTypeColor = GetBattleLogLowRationsColor();
                fromLabel = "low rations";
                break;

            case Board.DamageSource.DeathExplosion:
                damageTypeWord = "burst";
                damageTypeColor = GetBattleLogDeathBurstColor();
                fromLabel = "death burst";
                break;

            case Board.DamageSource.FloorSpike:
                fromLabel = "spikes";
                break;

            case Board.DamageSource.CastleProjectile:
                fromLabel = "Enemy Archer";
                break;

            case Board.DamageSource.BossAbility:
                fromLabel = "Boss";
                break;

            case Board.DamageSource.MagicExplosive:
                fromLabel = "Magic Explosive";
                break;

            case Board.DamageSource.Overgrowth:
                fromLabel = "Overgrowth";
                break;

            case Board.DamageSource.RearAmbush:
                fromLabel = "rear ambush";
                break;

            default:
                fromLabel = null;
                break;
        }
    }

    Color32 GetBattleLogLowRationsColor()
    {
        return battleLog ? GetPrivateColorOrFallback(new Color32(214, 184, 96, 255)) : new Color32(214, 184, 96, 255);
    }

    Color32 GetBattleLogDeathBurstColor()
    {
        return battleLog ? GetPrivateColorOrFallback(new Color32(255, 120, 120, 255)) : new Color32(255, 120, 120, 255);
    }

    Color32 GetBattleLogLightningColor()
    {
        return battleLog ? GetPrivateColorOrFallback(new Color32(80, 230, 255, 255)) : new Color32(80, 230, 255, 255);
    }

    Color32 GetBattleLogContagionColor()
    {
        return battleLog ? GetPrivateColorOrFallback(new Color32(190, 90, 255, 255)) : new Color32(190, 90, 255, 255);
    }

    Color32 GetBattleLogPoisonColor()
    {
        return battleLog ? GetPrivateColorOrFallback(new Color32(190, 90, 255, 255)) : new Color32(190, 90, 255, 255);
    }

    Color32 GetBattleLogFireColor()
    {
        return battleLog ? GetPrivateColorOrFallback(new Color32(255, 150, 40, 255)) : new Color32(255, 150, 40, 255);
    }

    Color32 GetPrivateColorOrFallback(Color32 fallback) => fallback;

    // ================ Cursor Scaling System ===================

    void OnCursorScaleChanged(float _)
    {
        if (pauseCursor)
            pauseCursor.SetScale(SettingsStore.LoadCursorScale());
    }

    void EnterGameplayCursorMode()
    {
        ClearHardwareCursor();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (pauseCursor)
            pauseCursor.SetVisible(false);
    }

    void EnterUICursorMode()
    {
        ClearHardwareCursor();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        if (pauseCursor)
        {
            pauseCursor.SetVisible(true);
            pauseCursor.SetScale(SettingsStore.LoadCursorScale());
        }
    }

    bool ShouldUseUICursorModeForCurrentState()
    {
        if (isPaused || gameOver || levelWon || ConfirmationPopupUI.IsAnyShowing)
            return true;

        if (levelModifierController && levelModifierController.IsSelectionRunning)
            return true;

        if (runModsPanelRoot && UIPanelTransition.IsVisible(runModsPanelRoot))
            return true;

        if (gameplayStatsPanelUI && gameplayStatsPanelUI.IsVisible)
            return true;

        if (helpPanelRoot && UIPanelTransition.IsVisible(helpPanelRoot))
            return true;

        if (roundRewardUI && roundRewardUI.rootPanel && UIPanelTransition.IsVisible(roundRewardUI.rootPanel))
            return true;

        if (highScoreUI && highScoreUI.PanelRoot && UIPanelTransition.IsVisible(highScoreUI.PanelRoot))
            return true;

        return victoryPanelUI && victoryPanelUI.RootPanel && UIPanelTransition.IsVisible(victoryPanelUI.RootPanel);
    }

    void ApplyCursorModeForCurrentState()
    {
        if (ShouldUseUICursorModeForCurrentState())
            EnterUICursorMode();
        else
            EnterGameplayCursorMode();
    }

    // ================ Obstacle Destruction Buff Drop System ===================

    void OnBoardObstacleDestroyed(Vector2Int cell, Board.ObstacleType type)
    {
        if (!IsRoundActive) return;

        _obstaclesDestroyedThisLevel += 1;
        RunSummaryStats.AddObstaclesDestroyed();

        // Stone run modifier drops
        if (type == Board.ObstacleType.Stone)
        {
            if (Random.value > CurrentStoneBuffDropChance) return;

            int levelNumber = Mathf.Max(1, currentLevel + 1);

            if (stoneObstacleDropsDebuffsOnly)
            {
                if (!TryPickStoneDebuffForLevel(levelNumber, out var debuff, out var debuffRarity)) return;

                GrantStoneRunModifier(cell, debuff, debuffRarity, isBuff: false);
                return;
            }

            if (!TryPickStoneBuffForLevel(levelNumber, out var buff, out var rarity)) return;

            GrantStoneRunModifier(cell, buff, rarity, isBuff: true);
            return;
        }

        // Pylon shield turns off once all pylons are gone
        if (type == Board.ObstacleType.MagicPylon)
        {
            RefreshPylonShieldState();
            return;
        }
    }

    void GrantStoneRunModifier(Vector2Int cell, RunModifierSO modifier, RunModRarity rarity, bool isBuff)
    {
        if (!modifier) return;

        if (isBuff)
            RunModsStore.Buffs.Add(modifier);
        else
            RunModsStore.Debuffs.Add(modifier);

        modifier.Apply(this);
        CodexProgressStore.Unlock(modifier);
        SyncRunModsToStore();

        if (AudioManager.I)
        {
            var clip = sfxStoneBuffGranted ? sfxStoneBuffGranted : AudioManager.I.sfxStoneBuffGranted;
            if (clip) AudioManager.I.PlayUISFX(clip);
        }

        if (runModsPanelUI) runModsPanelUI.Refresh();

        ShowStoneRunModifierGrantedPopup(cell, modifier, rarity);
    }

    Vector4 GetStoneWeightsForLevel(int levelNumber)
    {
        if (levelNumber <= 3) return stoneWeights_L1_3;
        if (levelNumber <= 6) return stoneWeights_L4_6;
        if (levelNumber <= 9) return stoneWeights_L7_9;
        return stoneWeights_L10P;
    }

    static RunModifierSO PickRandomNonNull(RunModifierSO[] pool)
    {
        if (pool == null || pool.Length == 0) return null;
        for (int i = 0; i < 12; i++)
        {
            var b = pool[Random.Range(0, pool.Length)];
            if (b) return b;
        }
        return null;
    }

    bool TryPickStoneBuffForLevel(int levelNumber, out RunModifierSO buff, out RunModRarity rarity)
    {
        return TryPickStoneModifierForLevel(
            levelNumber,
            buffPool,
            stoneBuffPoolCommon,
            stoneBuffPoolUncommon,
            stoneBuffPoolRare,
            stoneBuffPoolEpic,
            stoneBuffPoolLegendary,
            useFallbackPoolRarityWeights: false,
            out buff,
            out rarity);
    }

    bool TryPickStoneDebuffForLevel(int levelNumber, out RunModifierSO debuff, out RunModRarity rarity)
    {
        return TryPickStoneModifierForLevel(
            levelNumber,
            debuffPool,
            stoneDebuffPoolCommon,
            stoneDebuffPoolUncommon,
            stoneDebuffPoolRare,
            stoneDebuffPoolEpic,
            stoneDebuffPoolLegendary,
            useFallbackPoolRarityWeights: true,
            out debuff,
            out rarity);
    }

    bool TryPickStoneModifierForLevel(int levelNumber,
        RunModifierSO[] fallbackPool,
        RunModifierSO[] commonPool,
        RunModifierSO[] uncommonPool,
        RunModifierSO[] rarePool,
        RunModifierSO[] epicPool,
        RunModifierSO[] legendaryPool,
        bool useFallbackPoolRarityWeights,
        out RunModifierSO modifier,
        out RunModRarity rarity)
    {
        modifier = null;
        rarity = RunModRarity.Common;

        bool hasAnyRarityPools =
            (commonPool != null && commonPool.Length > 0) ||
            (uncommonPool != null && uncommonPool.Length > 0) ||
            (rarePool != null && rarePool.Length > 0) ||
            (epicPool != null && epicPool.Length > 0) ||
            (legendaryPool != null && legendaryPool.Length > 0);

        if (!hasAnyRarityPools)
        {
            if (useFallbackPoolRarityWeights)
                return TryPickStoneModifierFromFallbackPoolByRarity(levelNumber, fallbackPool, out modifier, out rarity);

            modifier = PickRandomNonNull(fallbackPool);
            rarity = GetRaritySafe(modifier, RunModRarity.Common);
            return modifier != null;
        }

        if (levelNumber >= 10 && legendaryPool != null && legendaryPool.Length > 0)
        {
            float pLeg = Mathf.Clamp01(stoneLegendaryChance_L10P);
            if (Random.value < pLeg)
            {
                modifier = PickRandomNonNull(legendaryPool);
                rarity = RunModRarity.Legendary;
                return modifier != null;
            }
        }

        Vector4 w = GetStoneWeightsForLevel(levelNumber);
        float wc = Mathf.Max(0f, w.x);
        float wu = Mathf.Max(0f, w.y);
        float wr = Mathf.Max(0f, w.z);
        float we = Mathf.Max(0f, w.w);

        if (commonPool == null || commonPool.Length == 0) wc = 0f;
        if (uncommonPool == null || uncommonPool.Length == 0) wu = 0f;
        if (rarePool == null || rarePool.Length == 0) wr = 0f;
        if (epicPool == null || epicPool.Length == 0) we = 0f;

        float sum = wc + wu + wr + we;
        if (sum <= 0.0001f)
        {
            modifier = PickRandomNonNull(fallbackPool);
            rarity = GetRaritySafe(modifier, RunModRarity.Common);
            return modifier != null;
        }

        float roll = Random.value * sum;

        if (roll < wc)
        {
            modifier = PickRandomNonNull(commonPool);
            rarity = RunModRarity.Common;
            return modifier != null;
        }
        roll -= wc;

        if (roll < wu)
        {
            modifier = PickRandomNonNull(uncommonPool);
            rarity = RunModRarity.Uncommon;
            return modifier != null;
        }
        roll -= wu;

        if (roll < wr)
        {
            modifier = PickRandomNonNull(rarePool);
            rarity = RunModRarity.Rare;
            return modifier != null;
        }

        modifier = PickRandomNonNull(epicPool);
        rarity = RunModRarity.Epic;
        return modifier != null;
    }

    bool TryPickStoneModifierFromFallbackPoolByRarity(int levelNumber, RunModifierSO[] pool, out RunModifierSO modifier, out RunModRarity rarity)
    {
        modifier = null;
        rarity = RunModRarity.Common;

        if (pool == null || pool.Length == 0)
            return false;

        if (levelNumber >= 10 && HasModifierWithRarity(pool, RunModRarity.Legendary))
        {
            float pLeg = Mathf.Clamp01(stoneLegendaryChance_L10P);
            if (Random.value < pLeg)
            {
                modifier = PickRandomNonNullByRarity(pool, RunModRarity.Legendary);
                rarity = RunModRarity.Legendary;
                return modifier != null;
            }
        }

        Vector4 w = GetStoneWeightsForLevel(levelNumber);
        float wc = HasModifierWithRarity(pool, RunModRarity.Common) ? Mathf.Max(0f, w.x) : 0f;
        float wu = HasModifierWithRarity(pool, RunModRarity.Uncommon) ? Mathf.Max(0f, w.y) : 0f;
        float wr = HasModifierWithRarity(pool, RunModRarity.Rare) ? Mathf.Max(0f, w.z) : 0f;
        float we = HasModifierWithRarity(pool, RunModRarity.Epic) ? Mathf.Max(0f, w.w) : 0f;

        float sum = wc + wu + wr + we;
        if (sum <= 0.0001f)
        {
            modifier = PickRandomNonNull(pool);
            rarity = GetRaritySafe(modifier, RunModRarity.Common);
            return modifier != null;
        }

        float roll = Random.value * sum;

        if (roll < wc)
        {
            modifier = PickRandomNonNullByRarity(pool, RunModRarity.Common);
            rarity = RunModRarity.Common;
            return modifier != null;
        }
        roll -= wc;

        if (roll < wu)
        {
            modifier = PickRandomNonNullByRarity(pool, RunModRarity.Uncommon);
            rarity = RunModRarity.Uncommon;
            return modifier != null;
        }
        roll -= wu;

        if (roll < wr)
        {
            modifier = PickRandomNonNullByRarity(pool, RunModRarity.Rare);
            rarity = RunModRarity.Rare;
            return modifier != null;
        }

        modifier = PickRandomNonNullByRarity(pool, RunModRarity.Epic);
        rarity = RunModRarity.Epic;
        return modifier != null;
    }

    static bool HasModifierWithRarity(RunModifierSO[] pool, RunModRarity rarity)
    {
        if (pool == null) return false;

        for (int i = 0; i < pool.Length; i++)
        {
            var modifier = pool[i];
            if (modifier && GetRaritySafe(modifier, RunModRarity.Common) == rarity)
                return true;
        }

        return false;
    }

    static RunModifierSO PickRandomNonNullByRarity(RunModifierSO[] pool, RunModRarity rarity)
    {
        if (pool == null || pool.Length == 0) return null;

        int count = 0;
        for (int i = 0; i < pool.Length; i++)
        {
            var modifier = pool[i];
            if (modifier && GetRaritySafe(modifier, RunModRarity.Common) == rarity)
                count++;
        }

        if (count == 0) return null;

        int pickIndex = Random.Range(0, count);
        for (int i = 0; i < pool.Length; i++)
        {
            var modifier = pool[i];
            if (!modifier || GetRaritySafe(modifier, RunModRarity.Common) != rarity)
                continue;

            if (pickIndex == 0)
                return modifier;

            pickIndex--;
        }

        return null;
    }

    void ShowStoneRunModifierGrantedPopup(Vector2Int cell, RunModifierSO buff, RunModRarity rarity)
    {
        if (!showStoneBuffPopup) return;
        if (!gameBoard || !buff) return;
        if (!stoneBuffPopupStyle || !stoneBuffPopupStyle.popupPrefab) return;

        string shownName = !string.IsNullOrWhiteSpace(buff.displayName)
            ? TetrabeastsLocalization.LocalizeText(buff.displayName)
            : buff.name;
        string msg = $"{shownName}";

        gameBoard.ShowBuffPopupAtCell(cell, msg, stoneBuffPopupStyle, rarity);
    }

    static RunModRarity GetRaritySafe(RunModifierSO buff, RunModRarity fallback)
    {
        if (buff is RunModifier rm) return rm.rarity;
        return fallback;
    }

    // ================ Boss Abilities ===================

    bool TryPickBossObstacleCell(out Vector2Int cell, int avoidLastNEmptyInRow = 0)
    {
        cell = default;
        if (!gameBoard) return false;

        int minY = 0;
        int maxY = gameBoard.height - 1;

        if (bossPreferLowerHalf)
            maxY = Mathf.Max(0, (gameBoard.height / 2) - 1); // Lower half = smaller y values

        int minFreeInRowExclusive = Mathf.Max(0, avoidLastNEmptyInRow);

        // Scan y from bottom up and take the first row that has candidates
        if (bossPreferAsLowAsPossible)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (minFreeInRowExclusive > 0 && gameBoard.CountFreeCellsInRow(y) <= minFreeInRowExclusive)
                    continue;

                _bossObstacleCandidateColumns.Clear();
                for (int x = 0; x < gameBoard.width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (!gameBoard.IsFree(c)) continue;

                    if (bossAvoidCompletingRow && gameBoard.WouldCompleteRowIfFilled(c))
                        continue;

                    _bossObstacleCandidateColumns.Add(x);
                }

                if (_bossObstacleCandidateColumns.Count > 0)
                {
                    int xPick = _bossObstacleCandidateColumns[Random.Range(0, _bossObstacleCandidateColumns.Count)];
                    cell = new Vector2Int(xPick, y);
                    return true;
                }
            }

            return false;
        }

        // Random attempts in the allowed range
        for (int a = 0; a < 300; a++)
        {
            int x = Random.Range(0, gameBoard.width);
            int y = Random.Range(minY, maxY + 1);

            if (minFreeInRowExclusive > 0 && gameBoard.CountFreeCellsInRow(y) <= minFreeInRowExclusive)
                continue;

            var c = new Vector2Int(x, y);

            if (!gameBoard.IsFree(c)) continue;
            if (bossAvoidCompletingRow && gameBoard.WouldCompleteRowIfFilled(c)) continue;

            cell = c;
            return true;
        }

        return false;
    }

    void Boss_PylonShield()
    {
        if (!bossEnablePylonShield) return;
        if (!gameBoard || _castleData == null) return;

        StartCoroutine(Boss_PylonShieldRoutine());
    }

    void RefreshPylonShieldState()
    {
        bool pylonsAlive = gameBoard && gameBoard.CountObstaclesOfType(Board.ObstacleType.MagicPylon) > 0;

        _bossPylonShieldActive = pylonsAlive;

        if (enemyCastleUI)
            enemyCastleUI.SetMagicShieldActive(pylonsAlive);
    }

    void Boss_MagicExplosive()
    {
        if (!bossEnableMagicExplosive) return;
        if (!gameBoard || _castleData == null) return;

        StartCoroutine(Boss_MagicExplosiveRoutine());
    }

    bool TryPickBossObstacleCellExcluding(HashSet<Vector2Int> used, HashSet<int> usedRows,
                                       out Vector2Int cell, int avoidLastNEmptyInRow = 0, int maxTries = 250)
    {
        cell = default;

        for (int i = 0; i < maxTries; i++)
        {
            if (!TryPickBossObstacleCell(out cell, avoidLastNEmptyInRow))
                return false;

            if (used != null && used.Contains(cell))
                continue;

            if (usedRows != null && usedRows.Contains(cell.y))
                continue;

            return true;
        }

        cell = default;
        return false;
    }

    IEnumerator Boss_PylonShieldRoutine()
    {
        float baseWarn = BossWarnSeconds();
        int blockedPlacements = 0;
        int want = Mathf.Max(1, _castleData.bossPylonCount);

        var used = new HashSet<Vector2Int>();
        int remaining = want;

        // Safety cap to avoid infinite loops if board is too full
        int batches = 0;
        int maxBatches = Mathf.Max(5, want * 5);

        while (remaining > 0 && batches < maxBatches)
        {
            batches++;

            // Pick remaining cells for this batch
            var batchCells = new List<Vector2Int>(remaining);
            var usedRowsThisBatch = new HashSet<int>();

            for (int i = 0; i < remaining; i++)
            {
                // Prefer different rows
                bool picked = TryPickBossObstacleCellExcluding(used, usedRowsThisBatch, out var c, want)
                              || TryPickBossObstacleCellExcluding(used, null, out c, want);

                if (!picked)
                    break;

                used.Add(c);
                usedRowsThisBatch.Add(c.y);
                batchCells.Add(c);
            }

            if (batchCells.Count == 0)
                break;

            // Warn all at once
            float warn = BossWarnSecondsForBlockedRetry(baseWarn, blockedPlacements);
            PlayBossAbilityWarningSFX();
            for (int i = 0; i < batchCells.Count; i++)
                FlashBossWarning(batchCells[i], gameBoard.magicPylonSprite, warn);

            // Wait once
            if (warn > 0f)
                yield return new WaitForSeconds(warn);

            // Spawn all at once
            int spawnedThisBatch = 0;
            var spawnedColsByRow = new Dictionary<int, List<int>>();
            for (int i = 0; i < batchCells.Count; i++)
            {
                if (gameBoard.TrySpawnMagicPylonObstacle(batchCells[i]))
                {
                    spawnedThisBatch++;
                    AddClearOriginColumn(spawnedColsByRow, batchCells[i]);
                }
            }

            blockedPlacements += Mathf.Max(0, batchCells.Count - spawnedThisBatch);

            RefreshPylonShieldState();
            bool rowClearStarted = ResolveFullRowsAfterBossObstacleSpawn(spawnedColsByRow);

            // If none spawned in this batch, don't loop forever
            if (spawnedThisBatch == 0)
                continue;

            if (rowClearStarted)
                yield return new WaitUntil(() => !_environmentRowClearResolving);

            remaining -= spawnedThisBatch; // Retry only for those that failed
        }

        RefreshPylonShieldState(); // Final state refresh in case some spawned late in the process
    }

    IEnumerator Boss_MagicExplosiveRoutine()
    {
        float baseWarn = BossWarnSeconds();
        int blockedPlacements = 0;
        var used = new HashSet<Vector2Int>(); // Used cells so retries don't keep flashing the same tile

        // Safety cap so it won't loop forever
        for (int attempts = 0; attempts < 10; attempts++)
        {
            if (!TryPickBossObstacleCellExcluding(used, null, out var cell, 1))
                yield break;

            used.Add(cell);

            float warn = BossWarnSecondsForBlockedRetry(baseWarn, blockedPlacements);
            PlayBossAbilityWarningSFX();
            FlashBossWarning(cell, gameBoard.magicExplosiveSprite, warn);

            if (warn > 0f)
                yield return new WaitForSeconds(warn);

            // If spawn fails, pick a new cell, flash again, and retry
            if (gameBoard.TrySpawnMagicExplosiveObstacle(
                    cell,
                    _castleData.bossExplosiveFuseSeconds,
                    _castleData.bossExplosiveRowClearBonusDamage,
                    _castleData.bossExplosiveDetonateVFXSprite,
                    _castleData.bossExplosiveDetonateSFX))
            {
                var spawnedColsByRow = new Dictionary<int, List<int>>();
                AddClearOriginColumn(spawnedColsByRow, cell);
                ResolveFullRowsAfterBossObstacleSpawn(spawnedColsByRow);
                yield break;
            }

            blockedPlacements++;
        }
    }

    void TryCastRandomBossAbility()
    {
        if (_castleData == null || !IsCastleBossForCurrentMode(_castleData)) return;

        // --- Weighted option pool path ---
        if (_castleData.useBossAbilityOptionPool &&
            _castleData.bossAbilityOptions != null &&
            _castleData.bossAbilityOptions.Length > 0)
        {
            var pickedKind = PickBossAbilityKindFromPool(_castleData);

            LogAndExecuteBossAbility(pickedKind);

            return;
        }

        // --- Prototype behavior fallback ---
        var picks = new List<CastleData.BossAbilityKind>();

        if (_castleData.bossEnableRowBlast) picks.Add(CastleData.BossAbilityKind.RowBlast);
        if (_castleData.bossEnableFullBoardBlast) picks.Add(CastleData.BossAbilityKind.FullBoardBlast);
        if (_castleData.bossEnableLightningStrike) picks.Add(CastleData.BossAbilityKind.LightningStrike);
        if (_castleData.bossEnableSpawnTraps) picks.Add(CastleData.BossAbilityKind.SpawnTraps);
        if (_castleData.bossEnableInvulnerability) picks.Add(CastleData.BossAbilityKind.Invulnerability);
        if (_castleData.bossEnableGravityBoost) picks.Add(CastleData.BossAbilityKind.GravityBoost);
        if (_castleData.bossEnablePylonShield) picks.Add(CastleData.BossAbilityKind.PylonShield);
        if (_castleData.bossEnableMagicExplosive) picks.Add(CastleData.BossAbilityKind.MagicExplosive);

        if (picks.Count == 0) return;

        var pickedFallbackKind = picks[Random.Range(0, picks.Count)];

        LogAndExecuteBossAbility(pickedFallbackKind);
    }

    void LogAndExecuteBossAbility(CastleData.BossAbilityKind pickedKind)
    {
        if (battleLog && pickedKind != CastleData.BossAbilityKind.SpawnTraps)
            battleLog.LogBossAbility(pickedKind.ToString());

        // Play attack for any boss special cast
        if (enemyCastleUI) enemyCastleUI.PlayBossAttackSprite();

        switch (pickedKind)
        {
            case CastleData.BossAbilityKind.RowBlast: Boss_RowBlastTop3(); break;
            case CastleData.BossAbilityKind.FullBoardBlast: Boss_FullBoardBlast(); break;
            case CastleData.BossAbilityKind.LightningStrike: Boss_LightningStrike(); break;
            case CastleData.BossAbilityKind.SpawnTraps: Boss_SpawnTraps(); break;
            case CastleData.BossAbilityKind.Invulnerability: Boss_Invulnerability(); break;
            case CastleData.BossAbilityKind.GravityBoost: Boss_GravityBoost(); break;
            case CastleData.BossAbilityKind.PylonShield: Boss_PylonShield(); break;
            case CastleData.BossAbilityKind.MagicExplosive: Boss_MagicExplosive(); break;
        }
    }

    CastleData.BossAbilityKind PickBossAbilityKindFromPool(CastleData cd)
    {
        // Build filtered active options
        var active = new List<CastleData.BossAbilityOption>(cd.bossAbilityOptions.Length);

        for (int i = 0; i < cd.bossAbilityOptions.Length; i++)
        {
            var opt = cd.bossAbilityOptions[i];

            // Respect existing toggles
            if (!IsBossAbilityEnabled(cd, opt.kind))
                continue;

            // Cooldown gate
            if (_bossNextReadyTime.TryGetValue(opt.kind, out float readyAt) && Time.time < readyAt)
                continue;

            // No-repeat gate
            if (!opt.allowRepeat && (int)_bossLastAbility != -1 && opt.kind == _bossLastAbility)
                continue;

            active.Add(opt);
        }

        // If everything got filtered out, repeat/cooldown but still respect toggles
        if (active.Count == 0)
        {
            for (int i = 0; i < cd.bossAbilityOptions.Length; i++)
            {
                var opt = cd.bossAbilityOptions[i];
                if (IsBossAbilityEnabled(cd, opt.kind))
                    active.Add(opt);
            }
        }

        if (active.Count == 0)
            return CastleData.BossAbilityKind.RowBlast;

        var picked = PickWeightedBossAbilityOption(active);

        // Apply cooldown
        if (picked.cooldown > 0f)
            _bossNextReadyTime[picked.kind] = Time.time + picked.cooldown;

        _bossLastAbility = picked.kind;
        return picked.kind;
    }

    bool IsBossAbilityEnabled(CastleData cd, CastleData.BossAbilityKind kind)
    {
        switch (kind)
        {
            case CastleData.BossAbilityKind.RowBlast: return cd.bossEnableRowBlast;
            case CastleData.BossAbilityKind.FullBoardBlast: return cd.bossEnableFullBoardBlast;
            case CastleData.BossAbilityKind.LightningStrike: return cd.bossEnableLightningStrike;
            case CastleData.BossAbilityKind.SpawnTraps: return cd.bossEnableSpawnTraps;
            case CastleData.BossAbilityKind.Invulnerability: return cd.bossEnableInvulnerability;
            case CastleData.BossAbilityKind.GravityBoost: return cd.bossEnableGravityBoost;
            case CastleData.BossAbilityKind.PylonShield: return cd.bossEnablePylonShield;
            case CastleData.BossAbilityKind.MagicExplosive: return cd.bossEnableMagicExplosive;
            default: return false;
        }
    }

    CastleData.BossAbilityOption PickWeightedBossAbilityOption(List<CastleData.BossAbilityOption> options)
    {
        int total = 0;
        for (int i = 0; i < options.Count; i++)
            total += Mathf.Max(1, options[i].weight);

        int roll = Random.Range(0, total);

        for (int i = 0; i < options.Count; i++)
        {
            roll -= Mathf.Max(1, options[i].weight);
            if (roll < 0) return options[i];
        }

        return options[0];
    }

    float BossWarnSeconds()
    {
        return (_castleData != null) ? Mathf.Max(0f, _castleData.bossAbilityWarningSeconds) : 3f;
    }

    float BossWarnSecondsForBlockedRetry(float baseWarnSeconds, int blockedPlacements)
    {
        if (baseWarnSeconds <= 0f)
            return 0f;

        float multiplier = Mathf.Pow(0.67f, Mathf.Max(0, blockedPlacements));
        return Mathf.Max(0.1f, baseWarnSeconds * multiplier);
    }

    Sprite PickWarningSprite(Sprite preferred)
    {
        if (preferred) return preferred;
        if (_castleData != null && _castleData.bossLightningWarningSprite) return _castleData.bossLightningWarningSprite;
        return null;
    }

    void PlayBossAbilityWarningSFX()
    {
        if (AudioManager.I) AudioManager.I.PlayBossAbilityCast();
    }

    int GetHighestAliveMonsterRow()
    {
        int highest = -1;
        for (int x = 0; x < gameBoard.width; x++)
            for (int y = gameBoard.height - 1; y >= 0; y--)
            {
                var c = new Vector2Int(x, y);
                if (gameBoard.TryGetMonster(c, out var mi) && mi.data && mi.hp > 0f)
                {
                    if (y > highest) highest = y;
                    break; // Higher rows checked first per column
                }
            }
        return highest;
    }

    void Boss_RowBlastTop3()
    {
        if (_castleData == null || gameBoard == null) return;

        int highest = GetHighestAliveMonsterRow();
        int yTop = (highest <= 1) ? 2 : highest;
        int y0 = Mathf.Clamp(yTop, 0, gameBoard.height - 1);
        int y1 = Mathf.Clamp(yTop - 1, 0, gameBoard.height - 1);
        int y2 = Mathf.Clamp(yTop - 2, 0, gameBoard.height - 1);

        float dmg = GetScaledEnemyDamage(Mathf.Max(0f, _castleData.bossRowBlastDamage));

        var targets = new List<Vector2Int>();
        for (int x = 0; x < gameBoard.width; x++)
        {
            targets.Add(new Vector2Int(x, y0));
            targets.Add(new Vector2Int(x, y1));
            targets.Add(new Vector2Int(x, y2));
        }

        StartCoroutine(BossRowBlastWarnThenDamage(
            targets,
            dmg,
            PickWarningSprite(_castleData.bossRowBlastWarningSprite)
        ));
    }

    void Boss_FullBoardBlast()
    {
        if (_castleData == null || gameBoard == null) return;

        float dmg = GetScaledEnemyDamage(Mathf.Max(0f, _castleData.bossFullBoardDamage));
        float warn = BossWarnSeconds();

        // Collect only occupied monster cells (hp > 0)
        var targets = new List<Vector2Int>();
        for (int x = 0; x < gameBoard.width; x++)
        {
            for (int y = 0; y < gameBoard.height; y++)
            {
                var c = new Vector2Int(x, y);

                if (gameBoard.TryGetMonster(c, out var inst) && inst.hp > 0f)
                    targets.Add(c);
            }
        }

        if (targets.Count == 0) return;

        StartCoroutine(BossFullBoardBlastWarnThenDamage(targets, dmg, warn));
    }

    IEnumerator BossRowBlastWarnThenDamage(List<Vector2Int> targets, float damage, Sprite warningSprite)
    {
        float warn = BossWarnSeconds();

        if (warningSprite != null)
        {
            PlayBossAbilityWarningSFX();
            foreach (var c in targets)
                if (gameBoard.InBounds(c))
                    FlashBossWarning(c, warningSprite, warn);
        }

        yield return new WaitForSeconds(warn);

        if (AudioManager.I) AudioManager.I.PlayBossRowBlastHit();

        foreach (var c in targets)
            if (gameBoard.InBounds(c))
                gameBoard.DamageTile(c, damage, Board.DamageSource.BossAbility);
    }

    IEnumerator BossFullBoardBlastWarnThenDamage(List<Vector2Int> targets, float dmg, float warn)
    {
        PlayBossAbilityWarningSFX(); // SFX at warning start

        Sprite sprite = _castleData.bossFullBoardWarningSprite;
        foreach (var c in targets)
            FlashBossWarning(c, sprite, warn);

        yield return new WaitForSeconds(warn);

        if (AudioManager.I) AudioManager.I.PlayBossBoardBlastHit();

        foreach (var c in targets)
            gameBoard.DamageTile(c, dmg, Board.DamageSource.BossAbility);
    }

    void Boss_LightningStrike()
    {
        int highest = GetHighestAliveMonsterRow();
        if (highest < 0) return;

        int minY = Mathf.Max(0, highest - 2);
        int maxY = Mathf.Min(gameBoard.height - 1, highest);

        // Pick an empty tile in that band
        var candidates = new List<Vector2Int>();
        for (int x = 0; x < gameBoard.width; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var c = new Vector2Int(x, y);
                if (!gameBoard.IsFree(c) && gameBoard.TryGetMonster(c, out var inst) && inst.data != null && inst.hp > 0f)
                    candidates.Add(c);
            }
        }

        if (candidates.Count == 0) return;

        int minTargets = Mathf.Max(1, _castleData.bossLightningTargetsMin);
        int maxTargets = Mathf.Max(minTargets, _castleData.bossLightningTargetsMax);

        // How many unique cells to strike this cast
        int strikes = Random.Range(minTargets, maxTargets + 1);
        strikes = Mathf.Min(strikes, candidates.Count);

        for (int i = 0; i < strikes; i++)
        {
            int pickIndex = Random.Range(0, candidates.Count);
            var target = candidates[pickIndex];
            candidates.RemoveAt(pickIndex); // ensures uniqueness

            float warn = BossWarnSeconds();
            StartCoroutine(BossLightningRoutine(target, warn));
        }

    }

    void FlashBossWarning(Vector2Int cell, Sprite sprite, float seconds)
    {
        if (!sprite) return;

        float toggle = (_castleData != null) ? Mathf.Max(0.02f, _castleData.bossWarningToggleInterval) : 0.16f;
        gameBoard.FlashWarningAtCell(cell, sprite, seconds, toggleInterval: toggle);
    }

    IEnumerator BossDelayedDamageRoutine(List<Vector2Int> targets, float damage, Sprite warningSprite)
    {
        float warn = BossWarnSeconds();

        if (warningSprite != null)
        {
            PlayBossAbilityWarningSFX();
            foreach (var c in targets)
                if (gameBoard.InBounds(c))
                    FlashBossWarning(c, warningSprite, warn);
        }

        yield return new WaitForSeconds(warn);

        foreach (var c in targets)
            if (gameBoard.InBounds(c))
                gameBoard.DamageTile(c, damage, Board.DamageSource.BossAbility);
    }

    IEnumerator BossLightningRoutine(Vector2Int cell, float warningSeconds)
    {
        if (_castleData == null || gameBoard == null) yield break;

        // Warning flash
        if (_castleData.bossLightningWarningSprite)
        {
            PlayBossAbilityWarningSFX();
            FlashBossWarning(cell, _castleData.bossLightningWarningSprite, warningSeconds);
        }

        yield return new WaitForSeconds(warningSeconds);

        if (AudioManager.I) AudioManager.I.PlayBossLightningStrike();

        // Initial impact damage if a monster is there now (even if it was empty when chosen)
        float initial = GetScaledEnemyDamage(Mathf.Max(0f, _castleData.bossLightningInitialDamage));

        if (initial > 0f) gameBoard.DamageTile(cell, initial, Board.DamageSource.FloorLightning);

        // Spawn temporary hazard (ticks) without destroying monsters
        float tickDmg = GetScaledFloorEffectDamage(Mathf.Max(0f, _castleData.bossLightningTickDamage));
        float interval = Mathf.Max(0.05f, _castleData.bossLightningTickInterval);
        float duration = Mathf.Max(0.05f, _castleData.bossLightningHazardDuration);

        int ticks = Mathf.CeilToInt(duration / interval);
        gameBoard.SetFloorEffect(cell, (Board.FloorEffectType)System.Enum.Parse(typeof(Board.FloorEffectType), "Lightning"),
                                 tickDmg, interval, ticks);

        yield return new WaitForSeconds(duration);

        gameBoard.ClearFloorEffect(cell);
    }

    struct TrapSpawnBatch
    {
        public CastleData.BossTrapKind kind;
        public List<Vector2Int> cells;
    }

    void Boss_SpawnTraps()
    {
        if (obstacleManager == null || gameBoard == null || _castleData == null) return;

        if (bossEnableMagicExplosive)
            Boss_MagicExplosive();

        var castOpt = GetBossTrapOptionForThisSpawn();
        StartCoroutine(BossSpawnTrapsSplitWithWarningRoutine(castOpt));
    }

    void FlashBossTrapWarning(Vector2Int cell, Sprite sprite, float seconds)
    {
        if (!sprite) return;

        float toggle = (_castleData != null) ? Mathf.Max(0.02f, _castleData.bossWarningToggleInterval) : 0.16f;
        gameBoard.FlashWarningAtCell(cell, sprite, seconds, toggleInterval: toggle, alpha: 0.65f);
    }

    CastleData.BossTrapSpawnOption GetBossTrapOptionForThisSpawn()
    {
        if (_castleData.useBossTrapOptionPool && _castleData.bossTrapOptions != null && _castleData.bossTrapOptions.Length > 0)
            return PickWeightedBossTrapOption(_castleData.bossTrapOptions);

        // Build a fallback option using the legacy single-choice fields
        CastleData.BossTrapSpawnOption opt;
        opt.kind = _castleData.bossTrapKind;
        opt.pattern = _castleData.bossTrapPattern;
        opt.weight = 1;
        return opt;
    }

    IEnumerator BossSpawnTrapsSplitWithWarningRoutine(CastleData.BossTrapSpawnOption castOpt)
    {
        float warn = BossWarnSeconds();

        var reserved = new HashSet<Vector2Int>();
        var batches = new List<TrapSpawnBatch>();

        // Decide how many placements based on the selected pattern
        int count =
            (castOpt.pattern == CastleData.BossTrapPattern.Single)
            ? Mathf.Max(0, _castleData.bossTrapCountPerUse)
            : Mathf.Max(0, _castleData.bossTrapCountPerClusterUse);

        for (int i = 0; i < count; i++)
        {
            if (TryPickBossTrapClusterCells(castOpt.kind, castOpt.pattern, reserved, out var cells))
            {
                foreach (var c in cells) reserved.Add(c);
                batches.Add(new TrapSpawnBatch { kind = castOpt.kind, cells = cells });
            }
        }

        if (batches.Count == 0)
            yield break;

        if (battleLog)
            battleLog.LogBossTrapAbility(castOpt.kind);

        // Warning flash
        PlayBossAbilityWarningSFX();

        foreach (var b in batches)
        {
            var warnSprite = GetTrapWarningSprite(b.kind);
            if (!warnSprite) continue;

            foreach (var c in b.cells)
                if (gameBoard.InBounds(c))
                    FlashBossTrapWarning(c, warnSprite, warn);
        }

        yield return new WaitForSeconds(warn);

        // Place after warning ends
        foreach (var b in batches)
            PlaceBossTrapCells(b.kind, b.cells);
    }

    bool TryPickBossTrapClusterCells(
    CastleData.BossTrapKind kind,
    CastleData.BossTrapPattern pattern,
    HashSet<Vector2Int> reserved,
    out List<Vector2Int> cellsOut)
    {
        cellsOut = null;
        int attempts = 250;
        int stoneTopBuffer = 7; // Number of rows from the top to avoid when spawning stone traps
        int maxStoneY = Mathf.Max(0, gameBoard.height - 1 - stoneTopBuffer);

        // Require free cells in the first pass to maximize fairness
        for (int k = 0; k < attempts; k++)
        {
            var cells = MakePatternCells(pattern, kind, maxStoneY);
            if (cells == null || cells.Count == 0) continue;

            if (AllCellsValid(cells, kind, reserved, requireFreeCells: true))
            {
                cellsOut = cells;
                return true;
            }
        }

        // Allowed to spawn on occupied cells in fallback pass, except stone traps (to avoid unfair insta-deaths)
        bool allowOccupied = (kind != CastleData.BossTrapKind.Stone);

        if (allowOccupied)
        {
            for (int k = 0; k < attempts; k++)
            {
                var cells = MakePatternCells(pattern, kind, maxStoneY);
                if (cells == null || cells.Count == 0) continue;

                if (AllCellsValid(cells, kind, reserved, requireFreeCells: false))
                {
                    cellsOut = cells;
                    return true;
                }
            }
        }

        return false;

        bool AllCellsValid(List<Vector2Int> cells, CastleData.BossTrapKind knd, HashSet<Vector2Int> res, bool requireFreeCells)
        {
            foreach (var c in cells)
            {
                if (!gameBoard.InBounds(c)) return false;
                if (res != null && res.Contains(c)) return false;
                if (gameBoard.HasFloorEffect(c)) return false; // Never stack floor effects

                // Stone obstacles must spawn into free cells 
                if (knd == CastleData.BossTrapKind.Stone)
                {
                    if (!gameBoard.IsFree(c)) return false;
                    if (c.y > maxStoneY) return false;
                    continue;
                }

                // Prefer empty first pass, allow occupied only in fallback pass
                if (requireFreeCells && !gameBoard.IsFree(c))
                    return false;
            }

            return true;
        }

        List<Vector2Int> MakePatternCells(CastleData.BossTrapPattern pat, CastleData.BossTrapKind knd, int maxStoneRowY)
        {
            int maxY = (knd == CastleData.BossTrapKind.Stone) ? maxStoneRowY : (gameBoard.height - 1);
            maxY = Mathf.Clamp(maxY, 0, gameBoard.height - 1);

            // Bottom-biased random cell
            Vector2Int PickOne()
            {
                int x = Random.Range(0, gameBoard.width);

                int y = Mathf.FloorToInt(Mathf.Pow(Random.value, 2.0f) * (maxY + 1));
                y = Mathf.Clamp(y, 0, maxY);

                return new Vector2Int(x, y);
            }

            var a = PickOne(); // Anchor point

            switch (pat)
            {
                case CastleData.BossTrapPattern.Single:
                    return new List<Vector2Int> { a };

                case CastleData.BossTrapPattern.Square2x2:
                    return new List<Vector2Int>
            {
                a,
                new Vector2Int(a.x + 1, a.y),
                new Vector2Int(a.x, a.y + 1),
                new Vector2Int(a.x + 1, a.y + 1),
            };

                case CastleData.BossTrapPattern.Line4_H:
                    return new List<Vector2Int>
            {
                a,
                new Vector2Int(a.x + 1, a.y),
                new Vector2Int(a.x + 2, a.y),
                new Vector2Int(a.x + 3, a.y),
            };

                case CastleData.BossTrapPattern.Line4_V:
                    return new List<Vector2Int>
            {
                a,
                new Vector2Int(a.x, a.y + 1),
                new Vector2Int(a.x, a.y + 2),
                new Vector2Int(a.x, a.y + 3),
            };

                case CastleData.BossTrapPattern.Line4:
                    return (Random.value < 0.5f)
                        ? new List<Vector2Int>
                        {
                    a,
                    new Vector2Int(a.x + 1, a.y),
                    new Vector2Int(a.x + 2, a.y),
                    new Vector2Int(a.x + 3, a.y),
                        }
                        : new List<Vector2Int>
                        {
                    a,
                    new Vector2Int(a.x, a.y + 1),
                    new Vector2Int(a.x, a.y + 2),
                    new Vector2Int(a.x, a.y + 3),
                        };

                case CastleData.BossTrapPattern.Line4_Random:
                    {
                        var cells = new List<Vector2Int>(4);
                        var used = new HashSet<Vector2Int>();

                        int tries = 0;
                        while (cells.Count < 4 && tries++ < 50)
                        {
                            var c = PickOne();
                            if (used.Add(c))
                                cells.Add(c);
                        }

                        return (cells.Count == 4) ? cells : null;
                    }
            }

            return new List<Vector2Int> { a };
        }
    }

    void PlaceBossTrapCells(CastleData.BossTrapKind kind, List<Vector2Int> cells)
    {
        foreach (var c in cells)
        {
            switch (kind)
            {
                case CastleData.BossTrapKind.Stone:
                    gameBoard.TrySpawnStoneObstacle(c, obstacleManager.stoneHitsToBreak);
                    break;

                case CastleData.BossTrapKind.Spike:
                    gameBoard.SetFloorEffect(c, Board.FloorEffectType.Spike,
                        GetScaledFloorEffectDamage(obstacleManager.spikeOneShotDamage), 1f, 1);
                    break;

                case CastleData.BossTrapKind.Poison:
                    gameBoard.SetFloorEffect(c, Board.FloorEffectType.Poison,
                        GetScaledFloorEffectDamage(obstacleManager.poisonTickDamage), obstacleManager.poisonTickInterval, obstacleManager.poisonTicks);
                    break;

                case CastleData.BossTrapKind.Fire:
                    gameBoard.SetFloorEffect(c, Board.FloorEffectType.Burn,
                        GetScaledFloorEffectDamage(obstacleManager.fireTickDamage), obstacleManager.fireTickInterval, obstacleManager.fireTicks);
                    break;
            }
        }
    }

    CastleData.BossTrapSpawnOption PickWeightedBossTrapOption(CastleData.BossTrapSpawnOption[] options)
    {
        int total = 0;
        for (int i = 0; i < options.Length; i++)
            total += Mathf.Max(1, options[i].weight);

        int roll = Random.Range(0, total);

        for (int i = 0; i < options.Length; i++)
        {
            roll -= Mathf.Max(1, options[i].weight);
            if (roll < 0) return options[i];
        }

        return options[0];
    }

    Sprite GetTrapWarningSprite(CastleData.BossTrapKind kind)
    {
        if (!gameBoard) return null;

        switch (kind)
        {
            case CastleData.BossTrapKind.Spike: return gameBoard.spikeSpriteHigh;
            case CastleData.BossTrapKind.Poison: return gameBoard.poisonBorderSprite;
            case CastleData.BossTrapKind.Fire: return gameBoard.fireBorderSprite;
            case CastleData.BossTrapKind.Lightning: return gameBoard.lightningBorderSprite;
            case CastleData.BossTrapKind.Stone: return gameBoard.stoneUndamagedSprite;
            default: return null;
        }
    }

    void Boss_Invulnerability()
    {
        if (_castleData == null) return;

        if (bossEnablePylonShield)
        {
            Boss_PylonShield();
            return;
        }

        if (enemyCastleUI == null) return;

        PlayBossAbilityWarningSFX();
        enemyCastleUI.StartInvulnerability(_castleData.bossInvulnDuration);
    }

    void Boss_GravityBoost()
    {
        if (_castleData == null) return;

        PlayBossAbilityWarningSFX();

        if (_bossGravityCR != null) StopCoroutine(_bossGravityCR);
        _bossGravityCR = StartCoroutine(BossGravityRoutine(_castleData.bossGravityBonusMult, _castleData.bossGravityDuration));
    }

    IEnumerator BossGravityRoutine(float bonusMult, float seconds)
    {
        float delta = Mathf.Max(0f, bonusMult);
        float dur = Mathf.Max(0.05f, seconds);

        _bossGravityBonusActive += delta;
        SetBossGravityVisualsActive(true); // Turn on UI cues

        // Start blinking shortly before the effect ends
        if (_bossGravityBlinkCR != null) StopCoroutine(_bossGravityBlinkCR);
        _bossGravityBlinkCR = StartCoroutine(BossGravityBlinkCo(dur));

        yield return new WaitForSeconds(dur);

        _bossGravityBonusActive -= delta;

        if (_bossGravityBonusActive < 0f) _bossGravityBonusActive = 0f;

        SetBossGravityVisualsActive(false); // Turn off UI cues

        _bossGravityCR = null;
    }

    void ResetBossGravityVisuals()
    {
        _bossGravityVisualActive = false;

        if (_bossGravityBlinkCR != null)
        {
            StopCoroutine(_bossGravityBlinkCR);
            _bossGravityBlinkCR = null;
        }

        if (bossGravityIncreasedImage)
        {
            bossGravityIncreasedImage.enabled = true;
            bossGravityIncreasedImage.gameObject.SetActive(false);
        }

        RefreshGravityTextColor();
    }

    void SetBossGravityVisualsActive(bool active)
    {
        if (active)
        {
            _bossGravityVisualActive = true;

            if (bossGravityIncreasedImage)
            {
                bossGravityIncreasedImage.enabled = true;
                bossGravityIncreasedImage.gameObject.SetActive(true);
            }
        }
        else
        {
            ResetBossGravityVisuals();
            return;
        }

        RefreshGravityTextColor();
    }

    IEnumerator BossGravityBlinkCo(float totalSeconds)
    {
        float lead = Mathf.Max(0f, bossGravityBlinkLeadSeconds);
        float wait = Mathf.Max(0f, totalSeconds - lead);

        // Wait until lead time begins
        if (wait > 0f)
            yield return new WaitForSeconds(wait);

        // Blink for the final lead seconds
        float t = 0f;
        while (t < lead)
        {
            if (bossGravityIncreasedImage)
                bossGravityIncreasedImage.enabled = !bossGravityIncreasedImage.enabled;

            yield return new WaitForSeconds(Mathf.Max(0.05f, bossGravityBlinkIntervalSeconds));
            t += bossGravityBlinkIntervalSeconds;
        }

        // Ensure it's on right up until the routine ends
        if (bossGravityIncreasedImage)
        {
            bossGravityIncreasedImage.enabled = true;
            bossGravityIncreasedImage.gameObject.SetActive(true);
        }
    }

    // ================== Player Special Co-Routines ==================

    System.Collections.IEnumerator PlayerReducedGravityCo(float seconds)
    {
        _playerGravityMultActive = 1f;
        _playerGravityBaseOverrideActive = true;
        SetTimedSlowGravityEffect(TimedSlowGravitySource.PlayerAbility, seconds);
        RefreshCurrentFallInterval(resetAccumulator: true);
        RefreshGravityTextColor();

        yield return TickTimedSlowGravityEffect(TimedSlowGravitySource.PlayerAbility, seconds);

        _playerGravityBaseOverrideActive = false;
        _playerGravityCR = null;
        ClearTimedSlowGravityEffect(TimedSlowGravitySource.PlayerAbility);
        RefreshCurrentFallInterval(resetAccumulator: false);
        RefreshGravityTextColor();
    }

    System.Collections.IEnumerator PlayerDoubleStatsCo(float seconds)
    {
        if (selectedCharacter && selectedCharacter.sfxDoubleStatsOn && AudioManager.I)
            AudioManager.I.PlaySFX(selectedCharacter.sfxDoubleStatsOn);
        
        if (gameBoard)
            gameBoard.MultiplyAllMonsterHpAndMax(2f); // Double HP/MaxHP for all monsters currently on the board

        _playerDoubleStatsAttackMult = 2f; // Double attack output

        yield return new WaitForSeconds(seconds);

        // Revert the HP changes by halving all current HP/MaxHP values
        if (gameBoard) gameBoard.MultiplyAllMonsterHpAndMax(0.5f);
        _playerDoubleStatsAttackMult = 1f;

        if (selectedCharacter && selectedCharacter.sfxDoubleStatsOff && AudioManager.I)
            AudioManager.I.PlaySFX(selectedCharacter.sfxDoubleStatsOff);

        _playerDoubleStatsCR = null;
    }

    // ================== Grid Growth System ==================

    void CacheBaseBoardSizeIfNeeded()
    {
        if (_baseBoardWidth > 0 && _baseBoardHeight > 0) return;
        if (!gameBoard) return;

        _baseBoardWidth = Mathf.Max(1, gameBoard.width);
        _baseBoardHeight = Mathf.Max(1, gameBoard.height);
    }

    void ApplyRunGridSize(int levelIndex)
    {
        if (!enableRunGridGrowth) return;
        if (!gameBoard) return;

        CacheBaseBoardSizeIfNeeded();
        if (_baseBoardWidth <= 0 || _baseBoardHeight <= 0) return;

        int roundNumber = levelIndex + 1; // levelIndex 0 => Round 1

        int addH = (growVerticalEveryNRounds > 0) ? (roundNumber / growVerticalEveryNRounds) : 0;
        int addW = (growHorizontalEveryNRounds > 0) ? (roundNumber / growHorizontalEveryNRounds) : 0;

        int newW = _baseBoardWidth + addW;
        int newH = _baseBoardHeight + addH;

        gameBoard.SetGridSize(newW, newH);
        if (obstacleManager)
            obstacleManager.SetStoneSpawnRowGrowthBonus(addH);
    }

    void ResetRunGridToBase()
    {
        if (!gameBoard) return;

        CacheBaseBoardSizeIfNeeded();
        if (_baseBoardWidth <= 0 || _baseBoardHeight <= 0) return;

        gameBoard.SetGridSize(_baseBoardWidth, _baseBoardHeight);
        if (obstacleManager)
            obstacleManager.SetStoneSpawnRowGrowthBonus(0);
    }

    // ================== Special Gauge & Text Visuals ==================

    void HandleControlsDisplayChanged(TetrabeastsControlProfile _)
    {
        RefreshGameplayControlTexts();
    }

    void RefreshGameplayControlTextsIfNeeded()
    {
        TetrabeastsControlProfile savedProfile = TetrabeastsControls.SavedProfile;
        TetrabeastsControlProfile effectiveProfile = TetrabeastsControls.EffectiveProfile;
        TetrabeastsControlProfile activeProfile = TetrabeastsControls.ActiveInputProfile;
        string specialBinding = GetGameplayBindingLabel(TetrabeastsControlAction.Special);

        if (_hasControlsTextSnapshot &&
            _lastControlsTextSavedProfile == savedProfile &&
            _lastControlsTextEffectiveProfile == effectiveProfile &&
            _lastControlsTextActiveProfile == activeProfile &&
            string.Equals(_lastControlsTextSpecialBinding, specialBinding, System.StringComparison.Ordinal))
            return;

        RefreshGameplayControlTexts();
    }

    void RefreshGameplayControlTexts()
    {
        ResolveGameplayControlsText();

        TetrabeastsControlProfile savedProfile = TetrabeastsControls.SavedProfile;
        TetrabeastsControlProfile effectiveProfile = TetrabeastsControls.EffectiveProfile;
        TetrabeastsControlProfile activeProfile = TetrabeastsControls.ActiveInputProfile;
        string specialBinding = GetGameplayBindingLabel(TetrabeastsControlAction.Special);

        if (activateSpecialGaugeText)
        {
            activateSpecialGaugeText.richText = true;
            activateSpecialGaugeText.text = FormatSpecialReadyPrompt(specialBinding);
        }

        if (gameplayControlsText)
        {
            gameplayControlsText.richText = true;
            string profileLabel = GetGameplayControlsProfileHeader(GetGameplayControlsDisplayProfile());
            gameplayControlsText.text = string.Join("\n", new[]
            {
                profileLabel,
                FormatGameplayControlLine(TetrabeastsControlAction.Pause),
                FormatGameplayControlLine(TetrabeastsControlAction.RotateCounterClockwise),
                FormatGameplayControlLine(TetrabeastsControlAction.RotateClockwise),
                FormatGameplayControlLine(TetrabeastsControlAction.MoveLeft),
                FormatGameplayControlLine(TetrabeastsControlAction.MoveRight),
                FormatGameplayControlLine(TetrabeastsControlAction.SoftDrop),
                FormatGameplayControlLine(TetrabeastsControlAction.HardDrop),
                $"{specialBinding} = {TetrabeastsLocalization.LocalizeText("Character Special")}"
            });
        }

        _lastControlsTextSavedProfile = savedProfile;
        _lastControlsTextEffectiveProfile = effectiveProfile;
        _lastControlsTextActiveProfile = activeProfile;
        _lastControlsTextSpecialBinding = specialBinding;
        _hasControlsTextSnapshot = true;
    }

    void ResolveGameplayControlsText()
    {
        if (gameplayControlsText)
            return;

        var labels = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            var label = labels[i];
            if (label && label.name == "Control_Text")
            {
                gameplayControlsText = label;
                return;
            }
        }
    }

    string FormatGameplayControlLine(TetrabeastsControlAction action)
    {
        return $"{GetGameplayBindingLabel(action)} = {TetrabeastsLocalization.LocalizeText(GetGameplayActionText(action))}";
    }

    string GetGameplayControlsProfileHeader(TetrabeastsControlProfile profile)
    {
        string label = TetrabeastsControls.GetProfileLabel(profile);
        return string.Equals(label, "Keyboard / Mouse", System.StringComparison.Ordinal)
            ? "Mouse/Keyboard"
            : label;
    }

    string GetGameplayBindingLabel(TetrabeastsControlAction action)
    {
        string label = TetrabeastsControls.GetCompactBindingLabel(action, GetGameplayControlsDisplayProfile());
        return string.IsNullOrWhiteSpace(label) ? TetrabeastsControls.GetActionLabel(action) : label;
    }

    string FormatSpecialReadyPrompt(string specialBinding)
    {
        TetrabeastsControlProfile profile = GetGameplayControlsDisplayProfile();
        string binding = string.IsNullOrWhiteSpace(specialBinding)
            ? TetrabeastsControls.GetActionLabel(TetrabeastsControlAction.Special)
            : specialBinding;

        if (profile == TetrabeastsControlProfile.KeyboardMouse)
            binding = $"[{binding}]";

        return $"{TetrabeastsLocalization.LocalizeText("Special Ready Press")} {binding}";
    }

    TetrabeastsControlProfile GetGameplayControlsDisplayProfile()
    {
        TetrabeastsControlProfile activeProfile = TetrabeastsControls.ActiveInputProfile;
        return activeProfile == TetrabeastsControlProfile.PlatformDefault
            ? TetrabeastsControls.EffectiveProfile
            : activeProfile;
    }

    static string GetGameplayActionText(TetrabeastsControlAction action)
    {
        return action switch
        {
            TetrabeastsControlAction.MoveLeft => "Move Left",
            TetrabeastsControlAction.MoveRight => "Move Right",
            TetrabeastsControlAction.SoftDrop => "Soft Drop",
            TetrabeastsControlAction.RotateClockwise => "Rotate Clockwise",
            TetrabeastsControlAction.RotateCounterClockwise => "Rotate Counterclockwise",
            TetrabeastsControlAction.HardDrop => "Hard Drop",
            TetrabeastsControlAction.Pause => "Pause",
            _ => TetrabeastsControls.GetActionLabel(action)
        };
    }

    void CacheSpecialDefaultsIfNeeded(TMP_Text t)
    {
        if (!t) return;
        if (_specialTextDefaults.ContainsKey(t)) return;

        var d = new SpecialTextDefaults
        {
            scale = t.rectTransform.localScale,
            hadVertexGradient = t.enableVertexGradient,
            gradient = t.colorGradient,
            color = t.color
        };

        _specialTextDefaults.Add(t, d);
    }

    void ResetSpecialChargedVisuals()
    {
        if (_specialChargedCR != null)
        {
            StopCoroutine(_specialChargedCR);
            _specialChargedCR = null;
        }

        ResetSpecialText(activateSpecialGaugeText);
        ResetSpecialText(playerSpecialName);
    }

    void ResetSpecialText(TMP_Text t)
    {
        if (!t) return;

        CacheSpecialDefaultsIfNeeded(t);
        var d = _specialTextDefaults[t];

        t.color = specialTextDefaultColor; // Force back to default
        t.enableVertexGradient = d.hadVertexGradient;
        t.colorGradient = d.gradient;
        t.rectTransform.localScale = d.scale;

        SafeUpdateTMPColors(t); // Ensure TMP refreshes visuals
    }

    void SetSpecialChargedVisuals(bool charged)
    {
        if (charged)
        {
            // Start coroutine once for both texts
            if (_specialChargedCR == null)
                _specialChargedCR = StartCoroutine(SpecialChargedPulseCo());
        }
        else
        {
            ResetSpecialChargedVisuals();
        }
    }

    Color FireColor(float t)
    {
        // Loop red, orange, yellow, orange, red
        t = Mathf.Repeat(t, 1f);
        if (t < 0.25f) return Color.Lerp(new Color(0.80f, 0.10f, 0.10f, 1f), new Color(1.00f, 0.35f, 0.00f, 1f), t / 0.25f);
        if (t < 0.50f) return Color.Lerp(new Color(1.00f, 0.35f, 0.00f, 1f), new Color(1.00f, 0.90f, 0.10f, 1f), (t - 0.25f) / 0.25f);
        if (t < 0.75f) return Color.Lerp(new Color(1.00f, 0.90f, 0.10f, 1f), new Color(1.00f, 0.35f, 0.00f, 1f), (t - 0.50f) / 0.25f);
        return Color.Lerp(new Color(1.00f, 0.35f, 0.00f, 1f), new Color(0.80f, 0.10f, 0.10f, 1f), (t - 0.75f) / 0.25f);
    }

    IEnumerator SpecialChargedPulseCo()
    {
        // Cache defaults
        CacheSpecialDefaultsIfNeeded(activateSpecialGaugeText);
        CacheSpecialDefaultsIfNeeded(playerSpecialName);

        while (true)
        {
            bool full = specialGauge >= (specialGaugeMax - 0.001f);
            if (!full) yield break;

            float time = Time.unscaledTime;

            // Pulse scale
            float s = 1f;
            if (specialPulseScale > 1.001f)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(time * (specialPulseSpeed * 6.2831853f));
                s = Mathf.Lerp(1f, specialPulseScale, wave);
            }

            ApplySpecialChargedVisualsToText(activateSpecialGaugeText, time, s);
            ApplySpecialChargedVisualsToText(playerSpecialName, time, s);

            yield return null;
        }
    }

    void ApplySpecialChargedVisualsToText(TMP_Text t, float time, float scaleMul)
    {
        if (!t) return;

        CacheSpecialDefaultsIfNeeded(t);
        var d = _specialTextDefaults[t];

        t.rectTransform.localScale = d.scale * scaleMul;

        if (specialUseFieryGradient)
        {
            t.enableVertexGradient = true;

            float tt = time * specialGradientShiftSpeed;
            Color topL = FireColor(tt);
            Color topR = FireColor(tt + 0.17f);
            Color botL = Color.Lerp(FireColor(tt + 0.45f), new Color(0.55f, 0.05f, 0.05f, 1f), 0.45f);
            Color botR = Color.Lerp(FireColor(tt + 0.62f), new Color(0.55f, 0.05f, 0.05f, 1f), 0.45f);

            t.colorGradient = new VertexGradient(topL, topR, botL, botR);
            t.color = Color.white;
        }
        else
        {
            t.enableVertexGradient = d.hadVertexGradient;
            t.color = specialTextChargedColor;
            t.colorGradient = d.gradient;
        }

        SafeUpdateTMPColors(t); // Force the vertex color refresh
    }

    void SafeUpdateTMPColors(TMP_Text t)
    {
        if (!t) return;

        if (!t.isActiveAndEnabled) return;
        if (!t.gameObject.activeInHierarchy) return;

        t.ForceMeshUpdate(); // Ensure mesh data exists before pushing color updates

        try
        {
            t.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
        catch
        {
            // Wait a frame and try again
        }
    }

    void UpdateSpecialGaugeFieryFill()
    {
        if (!specialGaugeUseFieryFill) return;
        if (!specialGaugeFillImage) return;
        if (specialGaugeMax <= 0f) return;

        float pct = Mathf.Clamp01(specialGauge / specialGaugeMax);
        bool full = specialGauge >= (specialGaugeMax - 0.001f);

        if (!full)
        {
            Color fillingColor = specialGaugeFillingColor;
            fillingColor.a = 1f;
            specialGaugeFillImage.color = fillingColor;
            return;
        }

        // Once full, keep the existing fiery red/orange/yellow cycle.
        float speed = Mathf.Lerp(specialGaugeFillMinSpeed, specialGaugeFillMaxSpeed, pct);

        _specialFillPhase += Time.unscaledDeltaTime * speed;
        Color c = FireColor(_specialFillPhase);

        // Slightly brighten near full
        float boost = Mathf.Lerp(1f, specialGaugeFillColorBoost, pct);
        c *= boost;
        c.a = 1f;

        specialGaugeFillImage.color = c;
    }

    IEnumerator SpecialGaugeFillFieryCo()
    {
        while (true)
        {
            UpdateSpecialGaugeFieryFill();
            yield return null;
        }
    }

    // ================== Level Win XP Granting ==================

    struct ComputedRoundXp
    {
        public XpAwardUI.RoundXpBreakdown breakdown;
        public Dictionary<string, float> perMonsterAwardXp;
        public Dictionary<string, float> perMonsterXpReductionPercent;
    }

    ComputedRoundXp ComputeRoundWinXp(int gameLevelNumber)
    {
        int xpLevelMultiplier = Mathf.Max(1, gameLevelNumber) * 2;
        int baseXp = Mathf.Max(0, baseXpPerLevel) * xpLevelMultiplier;

        int clearTimeBonus = CalculateClearTimeXpBonus(_levelTimer, baseXp, gameLevelNumber);

        int startReserve = _levelStartReserveUnits;
        int endReserve = unitLives;
        int reserveBonusRaw = 5 + (endReserve - startReserve);
        int unitsLostBonus = Mathf.Max(-5, reserveBonusRaw) * xpLevelMultiplier;
        int maxReserve = _levelStartMaxLives > 0 ? _levelStartMaxLives : EffectiveMaxUnitLives;

        int comboBonus = Mathf.Max(0, _maxComboThisLevel) * xpLevelMultiplier;
        int obstacleBonus = Mathf.Max(0, _obstaclesDestroyedThisLevel) * xpLevelMultiplier;

        int totalBeforeDifficulty = Mathf.Max(0, baseXp + clearTimeBonus + unitsLostBonus + comboBonus + obstacleBonus);
        int difficultyBonus = Mathf.RoundToInt(totalBeforeDifficulty * (_starDifficultyModifiers.expGainMultiplier - 1f));
        int totalBeforeReduction = Mathf.Max(0, totalBeforeDifficulty + difficultyBonus);
        int partyPassiveBonus = Mathf.RoundToInt(totalBeforeReduction * (CurrentPartyExperienceGainMultiplier - 1f));
        totalBeforeReduction = Mathf.Max(0, totalBeforeReduction + partyPassiveBonus);

        var roster = GetActiveMonsterRoster();
        var perMonster = new Dictionary<string, float>();
        var perMonsterReductionPercent = new Dictionary<string, float>();

        if (roster != null)
        {
            foreach (var md in roster)
            {
                if (!md) continue;

                int monsterLevel = RunMonsterProgress.GetCurrentLevel(md.monsterName);

                float levelMultiplier = GetOverleveledRoundXpMultiplier(monsterLevel, gameLevelNumber);
                float finalXp = totalBeforeReduction * levelMultiplier;
                perMonster[md.monsterName] = Mathf.Max(0f, finalXp);

                float reductionPercent = Mathf.Clamp01(1f - levelMultiplier) * 100f;
                if (reductionPercent > 0.01f)
                    perMonsterReductionPercent[md.monsterName] = reductionPercent;
            }
        }

        return new ComputedRoundXp
        {
            breakdown = new XpAwardUI.RoundXpBreakdown
            {
                gameLevelNumber = gameLevelNumber,
                baseXp = baseXp,
                levelClearTime = _levelTimer,
                clearTimeBonus = clearTimeBonus,
                startReserve = _levelStartReserveUnits,
                endReserve = unitLives,
                reserveBonus = unitsLostBonus,
                comboBonus = comboBonus,
                obstacleBonus = obstacleBonus,
                difficultyStars = _starDifficulty,
                difficultyBonus = difficultyBonus,
                partyPassiveBonus = partyPassiveBonus,
                totalBeforeDifficulty = totalBeforeDifficulty,
                totalBeforeReduction = totalBeforeReduction
            },
            perMonsterAwardXp = perMonster,
            perMonsterXpReductionPercent = perMonsterReductionPercent
        };
    }

    int CalculateClearTimeXpBonus(float clearSeconds, int baseXp, int gameLevelNumber)
    {
        if (baseXp <= 0)
            return 0;

        float levelTimeOffset = Mathf.Max(0, gameLevelNumber - 1) * Mathf.Max(0f, clearTimeXpSecondsAddedPerLevel);
        float parSeconds = Mathf.Max(0.01f, clearTimeXpParSeconds + levelTimeOffset);
        float exponent = Mathf.Max(0.1f, clearTimeXpCurveExponent);
        clearSeconds = Mathf.Max(0f, clearSeconds);

        if (clearSeconds <= parSeconds)
        {
            float fullBonusSeconds = Mathf.Clamp(clearTimeXpFullBonusSeconds + levelTimeOffset, 0f, parSeconds);
            float bonusWindow = Mathf.Max(0.01f, parSeconds - fullBonusSeconds);
            float t = Mathf.Clamp01((parSeconds - clearSeconds) / bonusWindow);
            int maxBonus = Mathf.RoundToInt(baseXp * Mathf.Clamp01(clearTimeXpMaxBonusBaseFraction));

            return Mathf.RoundToInt(maxBonus * Mathf.Pow(t, exponent));
        }

        float fullPenaltySeconds = Mathf.Max(parSeconds + 0.01f, clearTimeXpFullPenaltySeconds + levelTimeOffset);
        float penaltyWindow = Mathf.Max(0.01f, fullPenaltySeconds - parSeconds);
        float penaltyT = Mathf.Clamp01((clearSeconds - parSeconds) / penaltyWindow);
        int maxPenalty = Mathf.RoundToInt(baseXp * Mathf.Clamp01(clearTimeXpMaxPenaltyBaseFraction));

        return -Mathf.RoundToInt(maxPenalty * Mathf.Pow(penaltyT, exponent));
    }

    float GetOverleveledRoundXpMultiplier(int monsterLevel, int gameLevelNumber)
    {
        int stars = StarDifficultySystem.ClampStars(_starDifficulty);
        if (stars >= StarDifficultySystem.MaxStars)
            return 1f;

        int curveStartLevel = Mathf.Max(1, gameLevelNumber) + (stars * Mathf.Max(0, overleveledXpGraceLevelsPerStar));
        int levelGap = Mathf.Max(0, monsterLevel - curveStartLevel);
        if (levelGap <= 0)
            return 1f;

        float oneLevelGapMultiplier = Mathf.Clamp01(overleveledXpMultiplierAtOneLevelGap);
        float curveExponent = Mathf.Max(1f, overleveledXpGapExponent);
        float minimumMultiplier = Mathf.Clamp01(overleveledXpMinimumMultiplier);
        float curvedGap = Mathf.Pow(levelGap, curveExponent);
        float multiplier = Mathf.Pow(oneLevelGapMultiplier, curvedGap);

        return Mathf.Clamp(multiplier, minimumMultiplier, 1f);
    }

    void OpenXpUiMode()
    {
        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = false;

        // Ensure pause menu itself is not shown
        if (pausePanel) UIPanelTransition.Hide(pausePanel, true);

        if (xpAwardUI && !xpAwardUI.gameObject.activeInHierarchy)
            UIPanelTransition.Show(xpAwardUI.gameObject);

        EnterUICursorMode();
        StartCoroutine(ReapplyUICursorNextFrame());
    }

    void CloseXpUiMode()
    {
        CloseXpUiMode(resumeGameplay: true);
    }

    void CloseXpUiMode(bool resumeGameplay)
    {
        if (resumeGameplay)
        {
            isPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        EnterUICursorMode();
        StartCoroutine(ReapplyUICursorNextFrame());
    }

    void CloseAndHideXpUiMode()
    {
        CloseAndHideXpUiMode(resumeGameplay: true);
    }

    void CloseAndHideXpUiMode(bool resumeGameplay)
    {
        if (xpAwardUI)
            xpAwardUI.HideAll();

        CloseXpUiMode(resumeGameplay);
    }

    void ResolveVictoryPanelUi(bool logWarning = true)
    {
        if (!victoryPanelUI)
            victoryPanelUI = FindFirstObjectByType<VictoryPanelUI>(FindObjectsInactive.Include);

        if (!victoryPanelUI)
        {
            if (logWarning)
                Debug.LogWarning("GameController: FinalStats_Panel/VictoryPanelUI was not found in the scene. Assign the existing inactive FinalStats panel in the inspector.");
            return;
        }

        if (victoryModifierRowPrefab)
            victoryPanelUI.SetModifierRowPrefab(victoryModifierRowPrefab);
    }

    bool IsFinalLevelIndex(int levelIndex)
    {
        if (_postFinalSurvivalActive)
            return false;

        return castlesByLevel == null || castlesByLevel.Length == 0 || levelIndex >= castlesByLevel.Length - 1;
    }

    // ================== Helper/Utility ==================

    Vector2 BoardRightGutterY(int rowY, float marginCells = 0.6f)
    {
        // Y from the row's cell center
        float y = gameBoard.CellToAnchoredPos(new Vector2Int(0, rowY)).y;

        // X = right edge of grid + margin
        float halfW = gameBoard.gridRoot.rect.width * 0.5f; // Right edge in anchored coords
        float x = halfW + gameBoard.GetCellSize().x * marginCells;

        return new Vector2(x, y);
    }

    void ClearHardwareCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    void EnsureTriggeredTutorialPopups()
    {
        if (!triggeredTutorialPopups)
            triggeredTutorialPopups = GetComponent<TriggeredTutorialPopupController>();

        if (!triggeredTutorialPopups)
            triggeredTutorialPopups = gameObject.AddComponent<TriggeredTutorialPopupController>();
    }

    void EnsureSpecialBlockTutorials()
    {
        if (!specialBlockTutorials)
            specialBlockTutorials = GetComponent<SpecialBlockTutorialController>();

        if (!specialBlockTutorials)
            specialBlockTutorials = FindFirstObjectByType<SpecialBlockTutorialController>(FindObjectsInactive.Include);
    }

    RectTransform GetSpecialGaugeTutorialTarget()
    {
        if (specialGaugeSlider)
            return specialGaugeSlider.transform as RectTransform;

        return specialGaugeFillImage ? specialGaugeFillImage.rectTransform : null;
    }

    void QueueSpecialBlockTutorialIfNeeded(TetrominoData data)
    {
        if (!data || data.special == SpecialType.None || !piece || !piece.enabled)
            return;

        EnsureSpecialBlockTutorials();
        if (!specialBlockTutorials)
            return;

        specialBlockTutorials.TryShowForSpecialBlock(data.special, piece.GetTutorialHighlightTargets());
    }

    public IEnumerator ShowFirstFullRowTutorialIfNeeded(IReadOnlyList<RectTransform> highlightTargets)
    {
        if (_firstFullRowTutorialShownThisRun)
            yield break;

        _firstFullRowTutorialShownThisRun = true;

        EnsureTriggeredTutorialPopups();
        if (!triggeredTutorialPopups)
            yield break;

        yield return triggeredTutorialPopups.ShowOnceAndWait(
            TutorialIdFirstFullRow,
            new List<string>
            {
            "You completed a full row. When a row is filled all units are cleared, an attack is launched at the " +
            "enemy castle, and your Special Gauge is partially charged. (Press [F] to Continue)",
            "Completing multiple rows within a set time period will build up your combo. Higher combos will deal " +
            "increased damage and increase your score quickly. Each time a full row is cleared your combo timer " +
            "will reset giving you a chance to build larger combos and deal massive damage. (Press [F] to Continue)"
            },
            TutorialPopupView.PopupAnchorPreset.Top,
            defaultPopupAnchoredPosition: default,
            popupAlpha: 1f,
            pauseGameplay: true,
            freezePieceGravity: false,
            allowSkip: true,
            highlightTargets: highlightTargets,
            highlightPadding: new Vector2(12f, 12f));
    }

    void QueueFirstSpecialGaugeTutorialIfNeeded()
    {
        EnsureTriggeredTutorialPopups();
        if (!triggeredTutorialPopups)
            return;

        if (!enemyCastleUI || enemyCastleUI.CurrentHP <= 0)
            return;

        if (gameOver || levelWon)
            return;

        triggeredTutorialPopups.QueueShowOnce(
            TutorialIdFirstSpecialGaugeFull,
            "Your Special Gauge is full. Press [R] to activate your commander's special ability. " +
            "(Press [F] to Continue)",
            TutorialPopupView.PopupAnchorPreset.Top,
            popupAlpha: 1f,
            pauseGameplay: true,
            freezePieceGravity: false,
            allowSkip: true,
            highlightTarget: GetSpecialGaugeTutorialTarget(),
            highlightPadding: new Vector2(12f, 12f));
    }

    public void SetTutorialSuspended(bool suspended)
    {
        tutorialSuspended = suspended;
    }

    public void SetTutorialFreezePieceGravity(bool frozen)
    {
        tutorialFreezePieceGravity = frozen;
    }

    public System.Collections.Generic.IReadOnlyList<RectTransform> GetTutorialActivePieceHighlightTargets()
    {
        return piece ? piece.GetTutorialHighlightTargets() : null;
    }

    public void NotifyTutorialGameplayEvent(TutorialGameplayEvent gameplayEvent)
    {
        TutorialGameplayEventRaised?.Invoke(gameplayEvent);
    }

    public void SetTutorialDropPermissions(bool allowSoftDrop, bool allowHardDrop)
    {
        tutorialAllowSoftDrop = allowSoftDrop;
        tutorialAllowHardDrop = allowHardDrop;
    }

    public void SetTutorialPieceInputBlocked(bool blocked)
    {
        tutorialPieceInputBlocked = blocked;
    }

    public void BlockHardDropInputFor(float seconds)
    {
        if (seconds <= 0f)
            return;

        _tutorialHardDropInputBlockedUntilRealtime = Mathf.Max(
            _tutorialHardDropInputBlockedUntilRealtime,
            Time.unscaledTime + seconds);
    }

    bool IsAnyTutorialSequenceRunning()
    {
        var sequences = FindObjectsByType<TutorialSequenceController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < sequences.Length; i++)
        {
            var sequence = sequences[i];
            if (sequence && sequence.IsSequenceRunning)
                return true;
        }

        return false;
    }

    bool ShouldTutorialConsumeEscape()
    {
        if (triggeredTutorialPopups && triggeredTutorialPopups.IsPopupShowing)
            return true;

        bool hasRunningSequence = false;
        var sequences = FindObjectsByType<TutorialSequenceController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < sequences.Length; i++)
        {
            var sequence = sequences[i];
            if (!sequence || !sequence.IsSequenceRunning)
                continue;

            hasRunningSequence = true;
            if (sequence.AllowsGameplayPauseInput)
                return false;
        }

        if (hasRunningSequence)
            return true;

        EnsureTutorialPopupView();
        return tutorialPopupView && tutorialPopupView.IsShowing;
    }

    bool HasAnyTutorialPromptActive()
    {
        if (triggeredTutorialPopups && triggeredTutorialPopups.IsPopupShowing)
            return true;

        return IsAnyTutorialSequenceRunning();
    }

    bool CanLaunchCastleProjectile()
    {
        return gameBoard &&
               gameBoard.HasPlacedTiles() &&
               !HasAnyTutorialPromptActive();
    }

    void EnsureTutorialPopupView()
    {
        if (!tutorialPopupView)
            tutorialPopupView = FindTutorialPopupView();
    }

    static TutorialPopupView FindTutorialPopupView()
    {
        var views = FindObjectsByType<TutorialPopupView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < views.Length; i++)
        {
            var view = views[i];
            if (view && !view.IsReservedForWarnings)
                return view;
        }

        return null;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            if (ShouldUseUICursorModeForCurrentState())
                EnterUICursorMode();

            return;
        }

        TetrabeastsControls.RefreshActiveInputProfile();
        ApplyCursorModeForCurrentState();
        StartCoroutine(ReapplyCursorForFocusNextFrame());
    }

    IEnumerator ReapplyCursorForFocusNextFrame()
    {
        yield return null;
        ApplyCursorModeForCurrentState();
    }

    IEnumerator ReapplyUICursorNextFrame()
    {
        yield return null;
        EnterUICursorMode();
    }

    IEnumerator ReapplyGameplayCursorNextFrame()
    {
        yield return null;
        EnterGameplayCursorMode();
    }

}
