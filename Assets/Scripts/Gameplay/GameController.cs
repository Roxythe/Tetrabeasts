using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public Board gameBoard;
    public Piece piece;
    public Button restartButton;
    public Button volumeButton;
    public NextPreviewUI nextPreview;
    public TetrominoData[] allTetrominoes;

    [Header("Line Clear Visual Timing")]
    public float cascadeSettlePauseSeconds = 0.18f;

    [Header("Cursor (Gameplay)")]
    public UICursorController pauseCursor;

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

    [Header("Gravity UI")]
    [SerializeField] TMP_Text levelTimerText;
    [SerializeField] TMP_Text gravityText; // Displays current fall interval / speed
    [SerializeField] string gravityTextFormat = "{0:0.0}";

    [SerializeField] float fallRampStepSeconds = 2f; // 1 = every second, 2 = every other second, 3 = every 3rd second
    [SerializeField] float startFallInterval = 1.0f;

    [SerializeField] float minFallInterval = 0.10f;                // Clamp (fastest allowed)
    [SerializeField] float fallIntervalDecreasePerSecond = 0.008f;  // In-level ramp (shrinks interval over time)

    // Smooth non-compounding per-level start speed
    [SerializeField] float baseLevelGravity = 1.0f;                // Level 1 gravity baseline
    [SerializeField] float gravityIncreasePerLevel = 0.15f;        // Added each new level 

    // Runtime cache 
    float _level1FallInterval;          // Snapshot of Level 1 baseline interval
    float _lastShownFallInterval = -1f; // Cache last shown interval to avoid redundant UI updates
    float _levelTimer = 0f;
    float _thisLevelStartInterval;      // Computed at each level start
    int _lastLevelInitialized = -999;   // Prevents double-applying

    [Header("Boss Gravity Visuals")]
    [SerializeField] Image bossGravityIncreasedImage; // Toggle/flash when boss gravity is active
    [SerializeField] Color gravityTextDefaultColor = Color.white;
    [SerializeField] Color gravityTextBossColor = new Color(0.80f, 0.25f, 1.0f, 1f); // Bright purple
    [SerializeField] float bossGravityBlinkLeadSeconds = 3f;     // Start blinking this long before the effect ends
    [SerializeField] float bossGravityBlinkIntervalSeconds = 0.25f;

    Coroutine _bossGravityBlinkCR;

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

    [Header("Player")]
    public UnityEngine.UI.Image playerPortrait;
    public TMPro.TMP_Text playerName;
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

    [Header("Special Gauge UI")]
    public UnityEngine.UI.Slider specialSlider;
    public TMP_Text specialText;

    [Header("Special Gauge")]
    public float specialGauge = 0f;
    public float specialGaugeMax = 100f;
    public TMP_Text activateSpecialGaugeText;

    [Header("Special Blocks")]
    public TetrominoData[] specialBlocks;
    [Range(0f, 1f)]
    public float specialChancePerEnqueue = 0.08f;
    [Range(0f, 1f)]
    public float maxSpecialChance = 0.33f; // Hard cap (33%)

    [Header("Projectiles")]
    public RectTransform projectileRoot;
    public float projectileSpeed = 800f;

    [Header("Bag Settings")]
    public int minBagPieces = 2;
    public bool forceOneSpecialPerRefill = false; // Optional "ensure a special" switch

    [Header("Castle Attacks")]
    public float castleAttackInterval = 3.0f;  // Seconds between shots
    public Sprite castleProjectileSprite;
    float _castleAttackTimer = 0f;
    int castleProjectileDamage = 1;

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
    public bool IsPaused => isPaused;

    [Header("Unit Lives")]
    public int maxUnitLives = 20; // Base (starting unit lives)
    [SerializeField] int unitLives = 20; // Current lives (Based on max + buffs)
    [SerializeField] int reinforcementsPerWin = 5;

    public TMP_Text unitLivesText;
    public Slider unitLivesSlider;

    [Header("Obstacles & Traps")]
    public ObstacleManager obstacleManager;

    [Range(0f, 1f)]
    public float stoneBuffDropChance = 0.25f; // Chance to drop a run buff when stone breaks

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
    public float enemyProjectileSpeedMult = 1f;    // < 1 = Faster enemy projectiles (Currently unused)
    public float enemyCastleHpMult = 1f;

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

    public int currencyPerRoundWinAdd = 0;
    public float currencyPerRoundWinMult = 1f;
    public float lineClearCurrencyChanceAdd = 0f;
    public float lineClearCurrencyAmountMult = 1f;

    // ================= Player Special Timers =================
    float _playerGravityMultActive = 1f;     // 1 = normal
    Coroutine _playerGravityCR;

    float _playerDoubleStatsAttackMult = 1f; // Multiplied into monster damage output
    Coroutine _playerDoubleStatsCR;
    public float PlayerMonsterAttackMult => _playerDoubleStatsAttackMult; // Exposed for monster damage calculations

    // =========== Shop Buff Effective Values ===========
    int EffectiveMaxUnitLives => maxUnitLives + ShopBuffEffects.UnitLivesBonus; // Unit Lives Up: +2 per level

    float EffectivePieceGravityMult =>
    Mathf.Max(0.01f, pieceGravityMult) *
    ShopBuffEffects.GravityMultiplier *
    _playerGravityMultActive *
    (1f + _bossGravityBonusActive); // Slows falling (mult < 1 => slower because interval /= mult)

    float EffectiveFallRampRateMult => Mathf.Max(0f, fallRampRateMult) * ShopBuffEffects.VelocityMultiplier; // Velocity Down: slows ramping (mult < 1 => slower ramp)
    float EffectiveCurrencyChancePerClearedRow => // Gold Up: +2% chance per level
        Mathf.Clamp01(currencyChancePerClearedRow + lineClearCurrencyChanceAdd + ShopBuffEffects.GoldChanceBonus);
    float EffectiveLuck => luck + ShopBuffEffects.LuckBonus; // Luck Up: +10 per level 

    // ======== Achievements helpers ========
    [SerializeField] string[] achievementCharacterIds = new string[5]; // Set 5 character asset names
    float _gravityCapAccumSeconds = 0f;

    readonly Queue<TetrominoData> bag = new();
    public int score { get; private set; }
    bool gameOver = false;
    bool levelWon = false;
    private bool winQueued = false;

    [Header("Combo Scoring")]
    public float comboWindowSeconds = 5f;
    int _comboCount = 0;
    float _comboTimer = 0f;

    public ScoreUI scoreUI;
    public HighScoreUI highScoreUI;
    public TMP_Text levelText;

    public int CurrentLevel => currentLevel;
    public bool IsRoundActive => !isPaused && !gameOver && !levelWon;

    private CastleData currentCastleData;

    // Used by reward UI and other systems that need to know what type of level just ended
    public bool LastLevelWasBoss => currentCastleData != null && currentCastleData.isBossLevel;

    void Start()
    {
        // Ensure PlayerProgress exists (stats + achievements)
        if (PlayerProgress.I == null)
        {
            var go = new GameObject("PlayerProgress");
            go.AddComponent<PlayerProgress>();
        }

        // New run starts when gameplay scene starts
        PlayerProgress.I.BeginRun();

        HighScoreManager.EnsureInitialized(10);
        if (!highScoreUI)
            highScoreUI = FindFirstObjectByType<HighScoreUI>(FindObjectsInactive.Include);

        if (!highScoreUI) Debug.LogError("HighScoreUI not found in scene.");
        else Debug.Log("HighScoreUI bound to: " + highScoreUI.gameObject.name);

        if (allTetrominoes == null || allTetrominoes.Length == 0)
        {
            Debug.LogError("GameController: allTetrominoes is empty.");
            return;
        }

        if (!gameBoard) gameBoard = FindFirstObjectByType<Board>();
        if (!piece) piece = GetComponent<Piece>();

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

        // Initialize unit lives
        unitLives = EffectiveMaxUnitLives;
        SetupUnitLivesUI();

        if (gameBoard != null)
        {
            gameBoard.TileDied -= OnBoardTileDied;
            gameBoard.TileDied += OnBoardTileDied;
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

        // Apply character UI
        if (selectedCharacter)
        {
            if (playerPortrait && selectedCharacter.portrait)
                playerPortrait.sprite = selectedCharacter.portrait;
            if (playerBorder && selectedCharacter.defaultBorder)
                playerBorder.sprite = selectedCharacter.defaultBorder;
            if (playerName)
                playerName.text = selectedCharacter.displayName;
        }

        // Wire Run Mods panel buttons
        if (!runModsPanelRoot && runModsPanelUI)
            runModsPanelRoot = runModsPanelUI.gameObject;

        if (openRunModsButton)
            openRunModsButton.onClick.AddListener(OpenRunModsPanel);

        if (closeRunModsButton)
            closeRunModsButton.onClick.AddListener(CloseRunModsPanel);

        if (runModsPanelRoot) runModsPanelRoot.SetActive(false); // Ensure it starts closed

        // Apply character special gauge max
        if (selectedCharacter && selectedCharacter.specialGaugeMax > 0f)
            specialGaugeMax = selectedCharacter.specialGaugeMax;

        specialGauge = 0f; // Initialize Special gauge. Start full for testing
        UpdateSpecialUI();

        ResetRunMods();

        CacheBaseBoardSizeIfNeeded();
        ApplyRunGridSize(currentLevel);

        InitLevel(currentLevel);

        // Pause menu defaults
        if (pausePanel) pausePanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        EnterGameplayCursorMode(); // Lock cursor at start

        // Wire pause buttons once
        if (resumeButton) resumeButton.onClick.AddListener(ResumeGame);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (quitButton) quitButton.onClick.AddListener(QuitGame);

        if (volumePanelInPause) volumePanelInPause.pauseWhenOpen = false;

        if (bag.Count == 0) RefillBag();
        EnsureMinBag(3);
        ShowNextPreview();
        SpawnNextPiece();

        score = 0;
        if (scoreUI) scoreUI.Set(score);

        ResetCombo();
        ResetBossGravityVisuals();

        if (currencyUI) currencyUI.Refresh();

        SettingsStore.ApplySavedVolumesToAudio();

        if (pauseCursor)
            pauseCursor.SetScale(SettingsStore.LoadCursorScale());

        SettingsStore.CursorScaleChanged += OnCursorScaleChanged;
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
        ActivateSpecial();
#else
        if (Input.GetKeyDown(KeyCode.R))
            ActivateSpecial();
#endif

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused)
            {
                PauseGame();
                EnterUICursorMode();
            }
            else
            {
                // Close any sub-panels opened from pause
                bool closedSomething = false;

                if (runModsPanelRoot && runModsPanelRoot.activeSelf)
                {
                    runModsPanelRoot.SetActive(false);
                    closedSomething = true;
                }

                if (!closedSomething && volumePanelInPause && volumePanelInPause.gameObject.activeSelf)
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
        }

        // Periodic castle projectile
        if (!gameOver && !levelWon && gameBoard)
        {
            _castleAttackTimer += Time.deltaTime;
            if (_castleAttackTimer >= castleAttackInterval)
            {
                _castleAttackTimer = 0f;
                TrySpawnCastleDownshot();
            }
        }

        // Boss abilities loop
        if (!isPaused && !gameOver && !levelWon && gameBoard && _castleData != null && _castleData.isBossLevel)
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
        if (!isPaused && !gameOver && !levelWon)
        {
            _levelTimer += Time.deltaTime;
            UpdateLevelTimerUI();

            // Keep the currently falling piece synced to the current interval
            if (piece && piece.enabled)
            {
                float interval = GetCurrentFallInterval();
                piece.SetFallInterval(interval, resetAccumulator: false);
                UpdateGravityText(interval);
            }

            // Gravity cap accumulator (only counts while gameplay is active and unpaused)
            float intervalNow = GetCurrentFallInterval();
            if (intervalNow <= (minFallInterval + 0.0001f))
                _gravityCapAccumSeconds += Time.deltaTime;

            if (PlayerProgress.I != null)
            {
                if (_gravityCapAccumSeconds >= 30f && PlayerProgress.I.GetRunInt(AchievementSystem.Stat.RunGravityCap30) == 0)
                    PlayerProgress.I.AddRunInt(AchievementSystem.Stat.RunGravityCap30, 1);

                if (_gravityCapAccumSeconds >= 60f && PlayerProgress.I.GetRunInt(AchievementSystem.Stat.RunGravityCap60) == 0)
                    PlayerProgress.I.AddRunInt(AchievementSystem.Stat.RunGravityCap60, 1);
            }

            // Time-based achievements (survive for X seconds in a level)
            if (PlayerProgress.I != null)
            {
                if (_levelTimer >= 180f && PlayerProgress.I.GetRunInt(AchievementSystem.Stat.RunLevelTime180) == 0)
                    PlayerProgress.I.AddRunInt(AchievementSystem.Stat.RunLevelTime180, 1);

                if (_levelTimer >= 240f && PlayerProgress.I.GetRunInt(AchievementSystem.Stat.RunLevelTime240) == 0)
                    PlayerProgress.I.AddRunInt(AchievementSystem.Stat.RunLevelTime240, 1);

                if (_levelTimer >= 300f && PlayerProgress.I.GetRunInt(AchievementSystem.Stat.RunLevelTime300) == 0)
                    PlayerProgress.I.AddRunInt(AchievementSystem.Stat.RunLevelTime300, 1);
            }
        }

        // Combo timer (clearing another row resets this timer)
        if (!isPaused && !gameOver && !levelWon && _comboCount > 0)
        {
            _comboTimer -= Time.deltaTime;
            if (_comboTimer <= 0f)
                ResetCombo();
        }

        // Boss shield state
        if (!isPaused && IsRoundActive && gameBoard)
            RefreshPylonShieldState();
    }

    void OnDestroy()
    {
        SettingsStore.CursorScaleChanged -= OnCursorScaleChanged;
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

    public bool CanSpawnNewPiece() => !gameOver && !levelWon;

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

        // Keep bags topped and refresh preview after consuming one
        EnsureMinBag(3);
        ShowNextPreview();
    }

    public void GameOver()
    {
        gameOver = true;
        Debug.Log("Game Over");

        // Update lifetime stats and end run
        if (PlayerProgress.I)
        {
            PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.Losses, 1);
            PlayerProgress.I.EndRun();
        }

        if (AudioManager.I)
        {
            AudioManager.I.StopPauseMusic();
            AudioManager.I.StopLevelMusic();
        }
        AudioListener.pause = false;

        if (AudioManager.I) AudioManager.I.PlaySFX(AudioManager.I.sfxGameOver);

        if (highScoreUI) highScoreUI.TryShow(score);
        if (restartButton) restartButton.gameObject.SetActive(true);

        EnterUICursorMode();
    }

    void RefillBag()
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

        // Fisher–Yates shuffle
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

        float chance = Mathf.Clamp(specialChancePerEnqueue + specialBlockChanceAdd, 0f, maxSpecialChance);

        foreach (var d in normals)
        {
            TetrominoData use = d;

            // Chance to swap in a special block
            if (specialsAvailable && Random.value < chance)
            {
                float r = Random.Range(0f, specialTotal);
                for (int k = 0; k < specialBlocks.Length; k++)
                {
                    var sp = specialBlocks[k];
                    if (!sp) continue;
                    float w = Mathf.Max(0f, sp.spawnWeight);
                    if ((r -= w) <= 0f) { use = sp; specialsAddedThisRefill++; break; }
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
        if (forceOneSpecialPerRefill && specialsAvailable && specialsAddedThisRefill == 0)
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

        // Special gauge
        if (specialChargeFromMonsters > 0f)
        {
            specialGauge = Mathf.Min(specialGaugeMax, specialGauge + specialChargeFromMonsters);
            UpdateSpecialUI();
        }

        // Chance-based currency drops
        if (rowsCleared > 0)
        {
            for (int i = 0; i < rowsCleared; i++)
            {
                float chance = EffectiveCurrencyChancePerClearedRow;

                if (Random.value <= chance)
                {
                    int amount = Mathf.Max(1, Mathf.RoundToInt(1f * lineClearCurrencyAmountMult));
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

                    ShowCurrencyPopup(start, +1);
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

                SpawnAttackProjectile(attackSprite, attackSpriteAlt, animType, start, target, dmg, attackerMD);
            }
        }

        // No immediate damage, damage applies on impact
    }

    // ================= Combo scoring and damage bonus =================

    public void ApplyComboForRowClear(int monstersClearedInRow, ref int rowDamage)
    {
        int combo = IncrementCombo();

        // Score: 1 point per monster, multiplied by current combo count
        int gained = Mathf.Max(0, monstersClearedInRow) * combo;
        if (gained > 0)
            AddScoreInternal(gained);

        // Damage: +10% per combo step after the first, capped at 200%
        float dmgMult = GetComboDamageMultiplier(combo);
        rowDamage = Mathf.RoundToInt(rowDamage * dmgMult);
    }

    int IncrementCombo()
    {
        _comboCount = Mathf.Max(0, _comboCount) + 1;
        _comboTimer = Mathf.Max(0.1f, comboWindowSeconds);

        if (scoreUI)
            scoreUI.SetCombo(_comboCount);

        if (PlayerProgress.I)
            PlayerProgress.I.SetRunBestInt(AchievementSystem.Stat.RunMaxCombo, _comboCount);

        return _comboCount;
    }

    float GetComboDamageMultiplier(int combo)
    {
        float mult = 1f + (Mathf.Max(0, combo - 1) * 0.10f);
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
        score += Mathf.Max(0, points);
        if (scoreUI) scoreUI.Set(score);
    }


    void InitLevel(int levelIndex)
    {
        levelWon = false;
        winQueued = false;
        ResetBossGravityVisuals();

        // Only apply level-start logic once per level
        if (levelIndex != _lastLevelInitialized)
        {
            _lastLevelInitialized = levelIndex;
            ResetLevelTimerAndDrop(levelIndex);
        }

        CastleData data = null;
        if (castlesByLevel != null && levelIndex >= 0 && levelIndex < castlesByLevel.Length)
            data = castlesByLevel[levelIndex];

        if (!enemyCastleUI)
            enemyCastleUI = FindFirstObjectByType<EnemyCastleUI>(FindObjectsInactive.Include);

        currentCastleData = data; // For external reference if needed

        if (enemyCastleUI && data)
        {
            int levelNumber = levelIndex + 1;
            enemyCastleUI.InitCastle(data, levelNumber, enemyCastleHpMult);

            _castleData = data;

            if (_castleData != null && _castleData.isBossLevel)
            {
                _bossAbilityTimer = 0f;
                _bossNextAbilityAt = Random.Range(_castleData.bossAbilityIntervalMin, _castleData.bossAbilityIntervalMax);
            }

            enemyCastleUI.SetMagicShieldActive(false);
            castleProjectileSprite = data.projectileSprite;

            castleAttackInterval = data.projectileInterval * enemyAttackIntervalMult;
            projectileSpeed = data.projectileSpeed * enemyProjectileSpeedMult;
            castleProjectileDamage = Mathf.RoundToInt(data.projectileDamage * enemyProjectileDamageMult);

            if (levelText) levelText.text = $"Level: {levelNumber}";
        }
        else
        {
            Debug.LogWarning("InitLevel: castle data or enemyCastleUI missing");
        }

        if (AudioManager.I && data != null)
        {
            AudioManager.I.PlayLevelMusic(data.isBossLevel);
        }

        if (obstacleManager)
        {
            obstacleManager.OnLevelStart(levelIndex + 1, currentCastleData);
        }
    }

    // Call this when castle HP has been reduced to 0
    public void OnCastleDestroyed()
    {
        if (gameOver || levelWon) return;
        levelWon = true;

        // Stop ongoing music/loop as soon as the round ends
        if (AudioManager.I)
        {
            AudioManager.I.StopLevelMusic();

            if (roundWinClip)
                AudioManager.I.PlaySFX(roundWinClip);
        }

        if (nextPreview) nextPreview.ClearPreview();

        currentLevel++;

        // Track level wins for achievements
        if (PlayerProgress.I)
        {
            PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.LevelsWon, 1);
            PlayerProgress.I.AddRunInt(AchievementSystem.Stat.RunLevelsWon, 1);

            // Level time stats (for "win in X seconds" / "spend X seconds in a level")
            PlayerProgress.I.SetRunBestFloatMin(AchievementSystem.Stat.RunBestLevelWinSeconds, _levelTimer);
            PlayerProgress.I.SetRunBestFloatMin(AchievementSystem.Stat.RunBestLevelSeconds, _levelTimer);

            // Perfect win: no units dead at end of level
            int maxLivesNow = EffectiveMaxUnitLives;
            if (unitLives >= maxLivesNow)
                PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.PerfectLevelWins, 1);
        }

        int roundsWon = currentLevel;

        // +5 every round, plus +5 more for each completed set of 3 rounds
        int bonusSteps = roundsWon / 3;
        float misfortuneGain = 5f + (5f * bonusSteps);

        misfortune += misfortuneGain;
        RunModsStore.Misfortune = misfortune; // Keep store in sync

        int gained = GetRoundWinCurrency();
        CurrencyStore.Add(gained);

        if (PlayerProgress.I && gained > 0) // Track gold earned from round wins for achievements
            PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.GoldEarned, gained);

        if (currencyUI) currencyUI.Refresh();

        // Grant reinforcements (extra unit lives) on round win
        int beforeLives = unitLives;
        int maxLives = EffectiveMaxUnitLives;

        int reinforcementsGranted = Mathf.Max(0, reinforcementsPerWin);
        unitLives = Mathf.Clamp(unitLives + reinforcementsGranted, 0, maxLives);

        int actualReinforcements = unitLives - beforeLives;
        UpdateUnitLivesUI();

        // Show rewards before continuing
        if (roundRewardUI)
        {
            roundRewardUI.Show(buffPool, debuffPool, OnRoundModsChosen, gained, actualReinforcements);
        }
        else
        {
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
        RunModsStore.Misfortune = misfortune;
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

        bag.Clear();
        monstersBag.Clear();
        RefillBag();
        EnsureMinBag(3);
        ShowNextPreview();
        SpawnNextPiece();

        EnterGameplayCursorMode();

        levelWon = false; // re-arm
        if (roundRewardUI) roundRewardUI.Hide();
    }

    void EndRunAsWin()
    {
        gameOver = true;

        // Track final level wins for achievements
        if (PlayerProgress.I)
        {
            PlayerProgress.I.AddRunInt(AchievementSystem.Stat.RunBeatFinalLevel, 1);
            PlayerProgress.I.EndRun();
        }

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

        if (AudioManager.I) AudioManager.I.StopMusic();
        if (highScoreUI) highScoreUI.TryShow(score);
        if (restartButton) restartButton.gameObject.SetActive(true);

        EnterUICursorMode();
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

        if (specialGauge < specialGaugeMax) return; // Require full gauge

        if (PlayerProgress.I) // Track total special uses for achievements
        {
            PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.SpecialsUsedTotal, 1);

            // Per-character special count (stable key based on asset name)
            string charKey = selectedCharacter ? selectedCharacter.name : "Unknown";
            PlayerProgress.I.AddLifetimeInt($"lt_specials_used_char_{charKey}", 1);

            // Check "each character specials" thresholds (20 / 100)
            TryMarkSpecialsEachCharacter(20, AchievementSystem.Stat.SpecialsEachChar_20);
            TryMarkSpecialsEachCharacter(100, AchievementSystem.Stat.SpecialsEachChar_100);
        }

        switch (selectedCharacter.ability)
        {
            case SpecialAbility.ClearBottomRows:
                {
                    int rows = Mathf.Max(1, selectedCharacter.clearRows);

                    int squaresCleared = gameBoard.ClearBottomRowsWithCombat(rows, out int totalMonsterDamage,
                                         out float _specialChargeIgnored, out Dictionary<int, int> rowDamage,
                                         out Dictionary<int, MonsterData> rowDominantMonster,
                                         out Dictionary<int, List<int>> colsByRow);

                    // Reset gauge on use
                    specialGauge = 0f;
                    if (activateSpecialGaugeText)
                        activateSpecialGaugeText.gameObject.SetActive(false);

                    UpdateSpecialUI();

                    if (squaresCleared > 0 && AudioManager.I)
                        AudioManager.I.PlayRandomLineClear();

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
                    // Count how many were dead before revive
                    int deadBefore = gameBoard.CountDeadTiles();

                    var changed = new List<Vector2Int>();
                    int count = gameBoard.ReviveAllTilesToFull(changed);

                    if (selectedCharacter.sfxRestoreAll && AudioManager.I)
                        AudioManager.I.PlaySFX(selectedCharacter.sfxRestoreAll);

                    Sprite vfx = (selectedCharacter && selectedCharacter.reviveAllVFXSprite)
                                 ? selectedCharacter.reviveAllVFXSprite
                                 : null;

                    if (vfx && count > 0)
                        foreach (var cell in changed)
                            gameBoard.SpawnHealBurst(cell, vfx, 0.5f);

                    // Refund lives by number of dead units at cast time (cap at max)
                    if (deadBefore > 0)
                    {
                        unitLives = Mathf.Clamp(unitLives + deadBefore, 0, EffectiveMaxUnitLives);
                        UpdateUnitLivesUI();
                    }

                    // Reset gauge
                    specialGauge = 0f;
                    UpdateSpecialUI();
                    break;
                }

            case SpecialAbility.GlobalImmunity:
                {
                    // Start the timer/pulse co-routine
                    StartCoroutine(GlobalImmunityCo(Mathf.Max(0.25f, selectedCharacter.immunityDuration)));

                    specialGauge = 0f; // Reset gauge
                    UpdateSpecialUI();
                    break;
                }

            case SpecialAbility.ReducedGravity:
                {
                    float dur = (selectedCharacter != null) ? Mathf.Max(0.25f, selectedCharacter.reducedGravityDuration) : 10f;
                    float mult = (selectedCharacter != null) ? Mathf.Clamp(selectedCharacter.reducedGravityMultiplier, 0.1f, 2f) : 0.6666667f;

                    if (_playerGravityCR != null)
                    {
                        StopCoroutine(_playerGravityCR);
                        _playerGravityCR = null;
                        _playerGravityMultActive = 1f;
                    }

                    _playerGravityCR = StartCoroutine(PlayerReducedGravityCo(mult, dur));

                    specialGauge = 0f; // Reset gauge
                    UpdateSpecialUI();
                    break;
                }

            case SpecialAbility.DoubleStats:
                {
                    float dur = (selectedCharacter != null) ? Mathf.Max(0.25f, selectedCharacter.doubleStatsDuration) : 10f;

                    if (_playerDoubleStatsCR != null)
                    {
                        StopCoroutine(_playerDoubleStatsCR);
                        _playerDoubleStatsCR = null;

                        // Force revert before re-applying
                        _playerDoubleStatsAttackMult = 1f;
                        if (gameBoard) gameBoard.MultiplyAllMonsterHpAndMax(0.5f);
                    }

                    _playerDoubleStatsCR = StartCoroutine(PlayerDoubleStatsCo(dur));

                    // Reset gauge
                    specialGauge = 0f;
                    UpdateSpecialUI();
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
        if (selectedCharacter)
        {
            if (playerPortrait && selectedCharacter.portrait)
                playerPortrait.sprite = selectedCharacter.portrait;

            if (playerBorder)
                playerBorder.sprite = selectedCharacter.defaultBorder;

            if (playerName)
                playerName.text = selectedCharacter.displayName;
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

    void UpdateSpecialUI()
    {
        if (specialSlider)
            specialSlider.value = Mathf.Clamp01(specialGauge / Mathf.Max(1f, specialGaugeMax));

        if (specialText)
            specialText.text = $"{Mathf.RoundToInt(100f * Mathf.Clamp01(specialGauge / Mathf.Max(1f, specialGaugeMax)))}%";

        bool full = specialGauge >= (specialGaugeMax - 0.001f); // Small tolerance
        if (activateSpecialGaugeText) activateSpecialGaugeText.gameObject.SetActive(full);
    }

    void SpawnAttackProjectile(Sprite sprite, Sprite altSprite, AttackAnimType animType,
                           Vector2 startAnchored, Vector2 targetAnchored, int damage, MonsterData attackerMD)
    {
        if (!sprite) return;
        if (projectileRoot == null) projectileRoot = gameBoard.gridRoot;

        Vector2 cellSize = gameBoard.GetCellSize();

        // Parent container
        var go = new GameObject("AttackProjectile", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(projectileRoot, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = cellSize;
        rt.anchoredPosition = startAnchored;

        // Build visuals based on anim type
        UnityEngine.UI.Image topImg = null;
        UnityEngine.UI.Image botImg = null;

        if (animType == AttackAnimType.MirrorToggle)
        {
            // Bottom mirrored image (always visible)
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

            // Top image (toggles on/off)
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
            // Simple single image
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

            topImg = img; // Use top Img var as the single image reference
        }

        StartCoroutine(MoveProjectileAndHit(rt, topImg, botImg, animType, targetAnchored, damage, attackerMD));
    }

    System.Collections.IEnumerator MoveProjectileAndHit(RectTransform rt,
    UnityEngine.UI.Image topImg, UnityEngine.UI.Image botImg,
    AttackAnimType animType, Vector2 targetAnchored, int damage, MonsterData attackerMD)
    {
        float speed = Mathf.Max(10f, projectileSpeed);

        // Animation timers
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

            // Animate
            if (animType == AttackAnimType.MirrorToggle && topImg)
            {
                toggleT += Time.deltaTime;
                if (toggleT >= toggleInterval)
                {
                    toggleT = 0f;
                    topOn = !topOn;

                    // Toggle by alpha so layout doesn't jump
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

        if (rt) Destroy(rt.gameObject);

        // Apply damage on impact
        if (damage > 0 && enemyCastleUI && !levelWon && !gameOver)
        {
            // Play the attacking monster's impact SFX 
            if (AudioManager.I && attackerMD)
            {
                var clip = attackerMD.PickRandomAttackSFX();
                if (clip)
                    AudioManager.I.PlaySFX(clip);
            }

            // Pylon shield: boss takes reduced damage while any pylons remain
            bool pylonsAlive = gameBoard && gameBoard.CountObstaclesOfType(Board.ObstacleType.MagicPylon) > 0;

            if (pylonsAlive)
            {
                damage = Mathf.Max(1, Mathf.CeilToInt(damage * Mathf.Clamp(bossPylonDamageMult, 0.05f, 1f)));
            }

            if (enemyCastleUI)
                enemyCastleUI.SetMagicShieldActive(pylonsAlive);

            _bossPylonShieldActive = pylonsAlive;
            enemyCastleUI.ApplyDamage(damage);

            if (enemyCastleUI.currentHP <= 0 && !winQueued)
            {
                winQueued = true;
                StartCoroutine(CoWinAfterDelay(0.25f)); // Slight delay to allow multiple projectiles to land
                yield break;
            }
        }
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
        // Find columns with targets: alive and dead-only
        var aliveCols = new List<int>();
        var deadOnlyCols = new List<int>();

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

            if (hasAlive) aliveCols.Add(x);
            else if (hasAny) deadOnlyCols.Add(x);
        }

        if (aliveCols.Count == 0 && deadOnlyCols.Count == 0)
            return; // No targets on board

        // Choose a column: prefer aliveCols, else deadOnlyCols
        var pool = (aliveCols.Count > 0) ? aliveCols : deadOnlyCols;
        int col = pool[UnityEngine.Random.Range(0, pool.Count)];

        // Find the first living target in that column
        Vector2Int? hitCell = null;
        for (int y = gameBoard.height - 1; y >= 0; y--)
        {
            var c = new Vector2Int(col, y);
            if (gameBoard.IsFree(c)) continue;

            if (gameBoard.TryGetMonster(c, out var inst) && inst.data && inst.hp > 0f)
            { hitCell = c; break; }
            // else: dead tile — projectile should pass through
        }

        // Compute start/end positions in board/grid space
        Vector2 start = gameBoard.CellToAnchoredPos(new Vector2Int(col, gameBoard.height - 1))
                      + new Vector2(0f, gameBoard.GetCellSize().y * 0.75f);

        Vector2 end = hitCell.HasValue
            ? gameBoard.CellToAnchoredPos(hitCell.Value)
            : gameBoard.CellToAnchoredPos(new Vector2Int(col, 0));

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

        SpawnCastleDownProjectile(castleProjectileSprite, start, end, hitCell);
    }

    void SpawnCastleDownProjectile(Sprite sprite, Vector2 start, Vector2 end, Vector2Int? targetCell)
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
        rt.sizeDelta = gameBoard.GetCellSize();
        rt.anchoredPosition = start;

        StartCoroutine(CastleDownshotCo(rt, end, targetCell));
    }

    System.Collections.IEnumerator CastleDownshotCo(RectTransform rt, Vector2 end, Vector2Int? targetCell)
    {
        float speed = Mathf.Max(10f, projectileSpeed);
        while (rt && (rt.anchoredPosition - end).sqrMagnitude > 4f)
        {
            rt.anchoredPosition = Vector2.MoveTowards(rt.anchoredPosition, end, speed * Time.deltaTime);
            yield return null;
        }

        if (rt) Destroy(rt.gameObject);

        AudioClip hitClip = null;
        if (currentCastleData)
            hitClip = currentCastleData.PickRandom(currentCastleData.sfxProjectileHitTileClips,
                                                   currentCastleData.sfxProjectileHitTile);

        // Apply damage on impact
        if (targetCell.HasValue && !levelWon && !gameOver)
        {
            bool aliveAfter = gameBoard.DamageTile(targetCell.Value, castleProjectileDamage,
                                                   Board.DamageSource.CastleProjectile, hitClip);

            if (gameBoard.TryGetMonster(targetCell.Value, out var inst) && inst.data)
                Debug.Log($"Hit {inst.data.name} at {targetCell.Value}: {inst.hp}/{inst.data.maxHealth}"
                          + (aliveAfter ? "" : " (DEAD)"));
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
                    nextPreview.SyncBorderToImmunity(true, gameBoard.immuneBorderColor, gameBoard.normalBorderColor);

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
            nextPreview.SyncBorderToImmunity(true, gameBoard.immuneBorderColor, gameBoard.normalBorderColor);

        if (selectedCharacter && selectedCharacter.sfxImmunityOff && AudioManager.I)
            AudioManager.I.PlaySFX(selectedCharacter.sfxImmunityOff);
    }

    public void StyleInlineBorder(RectTransform rt)
    {
        var borderImg = rt ? rt.GetComponent<UnityEngine.UI.Image>() : null;
        if (!borderImg) return;

        borderImg.color = immunityActive
            ? gameBoard.immuneBorderColor  // Gold during immunity
            : gameBoard.normalBorderColor; // Black otherwise
    }

    // ================ Pause Menu Button Functions ===================

    public void PauseGame()
    {
        if (gameOver || levelWon) return;
        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;  // Pause SFX/music globally

        if (AudioManager.I) AudioManager.I.PlayPauseMusic(); // Play pause menu music if assigned
        if (pausePanel) pausePanel.SetActive(true);

        EnterUICursorMode();
        StartCoroutine(ReapplyUICursorNextFrame());
    }

    public void RestartGame()
    {
        // Unpause if needed
        Time.timeScale = 1f;
        AudioListener.pause = false;
        isPaused = false;
        if (pausePanel) pausePanel.SetActive(false);

        ClosePauseSubPanels();

        if (AudioManager.I) AudioManager.I.StopPauseMusic();
        if (AudioManager.I) AudioManager.I.PlaySFX(AudioManager.I.sfxRestart);

        if (piece) piece.ResetPiece(); // Stop the current drop
        if (gameBoard) gameBoard.ClearAll(); // Clear board tiles

        if (_bossGravityCR != null) { StopCoroutine(_bossGravityCR); _bossGravityCR = null; }
        _bossGravityBonusActive = 0f;
        ResetBossGravityVisuals();

        // Reset run state
        RunModsStore.ResetAll();
        ResetRunMods();

        // Reset Level 1 difficulty baseline + unit lives
        unitLives = EffectiveMaxUnitLives;
        SetupUnitLivesUI();

        _lastLevelInitialized = -999;

        currentLevel = 0;
        ResetRunGridToBase();
        ApplyRunGridSize(currentLevel);
        InitLevel(currentLevel);
        gameOver = false;

        specialGaugeMax = (selectedCharacter && selectedCharacter.specialGaugeMax > 0f)
        ? selectedCharacter.specialGaugeMax
        : 100f;
        specialGauge = 0f;
        UpdateSpecialUI();

        score = 0;
        if (scoreUI) scoreUI.Set(score);

        ResetCombo();
        if (highScoreUI) highScoreUI.Hide(); // Close high-score panel if it was open

        // Reset bag and preview
        bag.Clear();
        monstersBag.Clear();
        RefillBag();
        EnsureMinBag(2);
        ShowNextPreview();
        SpawnNextPiece(); // Spawn fresh piece
        if (restartButton) restartButton.gameObject.SetActive(true);
        if (currentCastleData && AudioManager.I)
            AudioManager.I.PlayLevelMusic(currentCastleData.isBossLevel);
    }

    public void ResumeGame()
    {
        ClosePauseSubPanels();

        if (AudioManager.I) AudioManager.I.StopPauseMusic();

        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (AudioManager.I) // Handle music mode changes that were made while paused
            AudioManager.I.ApplyPendingMusicModeAfterUnpause(); 

        if (pausePanel) pausePanel.SetActive(false);
        EnterGameplayCursorMode();
    }


    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;

        // Stop music before loading main menu
        if (AudioManager.I)
        {
            AudioManager.I.StopPauseMusic();
            AudioManager.I.StopLevelMusic();
        }

        AudioListener.pause = false;

        if (!string.IsNullOrEmpty(titleSceneName))
            UnityEngine.SceneManagement.SceneManager.LoadScene(titleSceneName);
        else
            Debug.LogError("GameController.titleSceneName is empty or not set.");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    public void OpenRunModsPanel()
    {
        if (!runModsPanelRoot) return;

        if (runModsPanelUI) runModsPanelUI.Refresh(); // Make sure the list is up to date before showing

        runModsPanelRoot.SetActive(true);
    }

    public void CloseRunModsPanel()
    {
        if (runModsPanelRoot) runModsPanelRoot.SetActive(false);
    }

    void ClosePauseSubPanels()
    {
        // Close Run Mods
        if (runModsPanelRoot && runModsPanelRoot.activeSelf)
            runModsPanelRoot.SetActive(false);

        // Close Volume settings panel
        if (volumePanelInPause && volumePanelInPause.gameObject.activeSelf)
            volumePanelInPause.Close();
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
        float raw = (baseAmt + currencyPerRoundWinAdd) * currencyPerRoundWinMult; // Apply run mods

        return Mathf.Max(0, Mathf.RoundToInt(raw)); // Round and clamp
    }

    // ================ Level Timer and Fall Interval System ===================

    void ResetLevelTimerAndDrop(int levelIndex)
    {
        _levelTimer = 0f;
        _gravityCapAccumSeconds = 0f;

        // Level 1 => levelIndex 0
        float levelGravity = baseLevelGravity + (Mathf.Max(0, levelIndex) * gravityIncreasePerLevel);

        // Convert gravity to a starting interval
        _thisLevelStartInterval = _level1FallInterval / Mathf.Max(0.01f, levelGravity);

        UpdateLevelTimerUI();
        UpdateGravityText(GetCurrentFallInterval());
    }

    float GetCurrentFallInterval()
    {
        // During the level, the interval shrinks over time 
        float ramp = fallIntervalDecreasePerSecond * EffectiveFallRampRateMult;

        float step = Mathf.Max(0.01f, fallRampStepSeconds);
        float steppedTime = Mathf.Floor(_levelTimer / step) * step; // Only advances every 'step' seconds

        float interval = _thisLevelStartInterval - (steppedTime * ramp);

        interval /= EffectivePieceGravityMult; // >1 = faster, <1 = slower
        return Mathf.Max(minFallInterval, interval);
    }

    void UpdateLevelTimerUI()
    {
        if (!levelTimerText) return;

        int mins = Mathf.FloorToInt(_levelTimer / 60f);
        int secs = Mathf.FloorToInt(_levelTimer % 60f);
        levelTimerText.text = $"{mins:00}:{secs:00}";
    }

    void UpdateGravityText(float currentInterval)
    {
        if (!gravityText) return;

        // Avoid spamming string rebuilds if interval hasn't meaningfully changed
        if (_lastShownFallInterval >= 0f && Mathf.Abs(_lastShownFallInterval - currentInterval) < 0.001f)
            return;

        _lastShownFallInterval = currentInterval;

        float cellsPerSecond = (currentInterval > 0.0001f) ? (1f / currentInterval) : 0f;
        gravityText.text = $"Gravity: {cellsPerSecond:0.0}";
    }

    // ================ Unit Lives System ===================
    void SetupUnitLivesUI()
    {
        if (unitLivesSlider)
        {
            unitLivesSlider.minValue = 0;
            unitLivesSlider.maxValue = EffectiveMaxUnitLives;
        }

        UpdateUnitLivesUI();
    }

    void UpdateUnitLivesUI()
    {
        int max = EffectiveMaxUnitLives;

        if (unitLivesText)
            unitLivesText.text = $"{unitLives} / {max}";

        if (unitLivesSlider)
            unitLivesSlider.value = unitLives;
    }

    void OnBoardTileDied(Vector2Int cell, MonsterData data)
    {
        if (gameOver) return;

        unitLives = Mathf.Max(0, unitLives - 1);
        UpdateUnitLivesUI();

        if (unitLives <= 0)
            GameOver();
    }

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

    // ================ Obstacle Destruction Buff Drop System ===================

    void OnBoardObstacleDestroyed(Vector2Int cell, Board.ObstacleType type)
    {
        if (!IsRoundActive) return;

        // Stone existing run buff drops
        if (type == Board.ObstacleType.Stone)
        {
            if (buffPool == null || buffPool.Length == 0) return;
            if (Random.value > stoneBuffDropChance) return;

            var buff = buffPool[Random.Range(0, buffPool.Length)];
            if (!buff) return;

            RunModsStore.Buffs.Add(buff);
            buff.Apply(this);

            if (runModsPanelUI) runModsPanelUI.Refresh(); // Refresh UI if open
            return;
        }

        // Pylon shield turns off once all pylons are gone
        if (type == Board.ObstacleType.MagicPylon)
        {
            RefreshPylonShieldState();
            return;
        }
    }

    // ================ Boss Abilities ===================

    bool TryGetRandomEmptyCellAnyRow(out Vector2Int cell, int minYInclusive, int maxYInclusive, int maxAttempts = 300)
    {
        cell = default;
        if (!gameBoard) return false;

        int minY = Mathf.Clamp(minYInclusive, 0, gameBoard.height - 1);
        int maxY = Mathf.Clamp(maxYInclusive, 0, gameBoard.height - 1);
        if (maxY < minY) (minY, maxY) = (maxY, minY);

        for (int a = 0; a < maxAttempts; a++)
        {
            int x = Random.Range(0, gameBoard.width);
            int y = Random.Range(minY, maxY + 1);
            var c = new Vector2Int(x, y);
            if (gameBoard.IsFree(c))
            {
                cell = c;
                return true;
            }
        }

        return false;
    }

    bool TryPickBossObstacleCell(out Vector2Int cell)
    {
        cell = default;
        if (!gameBoard) return false;

        int minY = 0;
        int maxY = gameBoard.height - 1;

        if (bossPreferLowerHalf)
            maxY = Mathf.Max(0, (gameBoard.height / 2) - 1); // lower half = smaller y values

        // Scan y from bottom up and take the first row that has candidates.
        if (bossPreferAsLowAsPossible)
        {
            for (int y = minY; y <= maxY; y++)
            {
                // Collect valid candidates for this y
                var candidates = new List<int>();
                for (int x = 0; x < gameBoard.width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (!gameBoard.IsFree(c)) continue;

                    if (bossAvoidCompletingRow && gameBoard.WouldCompleteRowIfFilled(c))
                        continue;

                    candidates.Add(x);
                }

                if (candidates.Count > 0)
                {
                    int xPick = candidates[Random.Range(0, candidates.Count)];
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

    bool TryPickBossObstacleCellExcluding(HashSet<Vector2Int> used, out Vector2Int cell, int maxTries = 250)
    {
        cell = default;

        for (int i = 0; i < maxTries; i++)
        {
            if (!TryPickBossObstacleCell(out cell))
                return false;

            if (used != null && used.Contains(cell))
                continue;

            return true;
        }

        cell = default;
        return false;
    }

    IEnumerator Boss_PylonShieldRoutine()
    {
        float warn = BossWarnSeconds();
        int want = Mathf.Max(1, _castleData.bossPylonCount);

        var used = new HashSet<Vector2Int>();
        int remaining = want;

        // Safety cap to avoid infinite loops if board is too full
        int batches = 0;
        int maxBatches = Mathf.Max(5, want * 5);

        while (remaining > 0 && batches < maxBatches)
        {
            batches++;

            // Pick 'remaining' cells for this batch
            var batchCells = new List<Vector2Int>(remaining);

            for (int i = 0; i < remaining; i++)
            {
                if (!TryPickBossObstacleCellExcluding(used, out var c))
                    break;

                used.Add(c);
                batchCells.Add(c);
            }

            if (batchCells.Count == 0)
                break;

            // Warn all at once
            PlayBossAbilityWarningSFX();
            for (int i = 0; i < batchCells.Count; i++)
                FlashBossWarning(batchCells[i], gameBoard.magicPylonSprite, warn);

            // Wait once
            if (warn > 0f)
                yield return new WaitForSeconds(warn);

            // Spawn all at once 
            int spawnedThisBatch = 0;
            for (int i = 0; i < batchCells.Count; i++)
            {
                if (gameBoard.TrySpawnMagicPylonObstacle(batchCells[i]))
                    spawnedThisBatch++;
            }

            RefreshPylonShieldState();

            // If none spawned in this batch, don't loop forever
            if (spawnedThisBatch == 0)
                break;

            remaining -= spawnedThisBatch; // Retry only for those that failed
        }

        RefreshPylonShieldState(); // Final state refresh in case some spawned late in the process
    }

    IEnumerator Boss_MagicExplosiveRoutine()
    {
        float warn = BossWarnSeconds();
        var used = new HashSet<Vector2Int>(); // Used cells so retries don't keep flashing the same tile

        // Safety cap so it won't loop forever
        for (int attempts = 0; attempts < 10; attempts++)
        {
            if (!TryPickBossObstacleCellExcluding(used, out var cell))
                yield break;

            used.Add(cell);

            PlayBossAbilityWarningSFX();
            FlashBossWarning(cell, gameBoard.magicExplosiveSprite, warn);

            if (warn > 0f)
                yield return new WaitForSeconds(warn);

            // If spawn fails (occupied), pick a new cell, flash again, and retry
            if (gameBoard.TrySpawnMagicExplosiveObstacle(
                    cell,
                    _castleData.bossExplosiveFuseSeconds,
                    _castleData.bossExplosiveRowClearBonusDamage,
                    _castleData.bossExplosiveDetonateVFXSprite,
                    _castleData.bossExplosiveDetonateSFX))
            {
                yield break;
            }
        }
    }

    void TryCastRandomBossAbility()
    {
        if (_castleData == null || gameBoard == null) return;

        // --- Weighted option pool path ---
        if (_castleData.useBossAbilityOptionPool &&
            _castleData.bossAbilityOptions != null &&
            _castleData.bossAbilityOptions.Length > 0)
        {
            var pickedKind = PickBossAbilityKindFromPool(_castleData);

            // Execute the picked ability
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

            return;
        }

        // --- Prototype behavior fallback ---
        var picks = new List<System.Action>();

        if (_castleData.bossEnableRowBlast) picks.Add(Boss_RowBlastTop3);
        if (_castleData.bossEnableFullBoardBlast) picks.Add(Boss_FullBoardBlast);
        if (_castleData.bossEnableLightningStrike) picks.Add(Boss_LightningStrike);
        if (_castleData.bossEnableSpawnTraps) picks.Add(Boss_SpawnTraps);
        if (_castleData.bossEnableInvulnerability) picks.Add(Boss_Invulnerability);
        if (_castleData.bossEnableGravityBoost) picks.Add(Boss_GravityBoost);
        if (_castleData.bossEnablePylonShield) picks.Add(Boss_PylonShield);
        if (_castleData.bossEnableMagicExplosive) picks.Add(Boss_MagicExplosive);

        if (picks.Count == 0) return;

        var cast = picks[Random.Range(0, picks.Count)];
        cast?.Invoke();
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

        float dmg = Mathf.Max(0f, _castleData.bossRowBlastDamage);

        // Build target cells
        var targets = new List<Vector2Int>();
        for (int x = 0; x < gameBoard.width; x++)
        {
            targets.Add(new Vector2Int(x, y0));
            targets.Add(new Vector2Int(x, y1));
            targets.Add(new Vector2Int(x, y2));
        }

        StartCoroutine(BossDelayedDamageRoutine(
            targets,
            dmg,
            PickWarningSprite(_castleData.bossRowBlastWarningSprite)
        ));
    }

    void Boss_FullBoardBlast()
    {
        if (_castleData == null || gameBoard == null) return;

        float dmg = Mathf.Max(0f, _castleData.bossFullBoardDamage);
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

    IEnumerator BossFullBoardBlastWarnThenDamage(List<Vector2Int> targets, float dmg, float warn)
    {
        PlayBossAbilityWarningSFX(); // SFX at warning start

        // Warning only where monsters exist
        Sprite sprite = _castleData.bossFullBoardWarningSprite;
        foreach (var c in targets)
            FlashBossWarning(c, sprite, warn);

        yield return new WaitForSeconds(warn);

        // Damage only where monsters exist
        foreach (var c in targets)
            gameBoard.DamageTile(c, dmg);
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
                if (gameBoard.IsFree(c)) candidates.Add(c);
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
                gameBoard.DamageTile(c, damage);
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
        float initial = Mathf.Max(0f, _castleData.bossLightningInitialDamage);

        if (initial > 0f) gameBoard.DamageTile(cell, initial);

        // Spawn temporary hazard (ticks) without destroying monsters
        float tickDmg = Mathf.Max(0f, _castleData.bossLightningTickDamage);
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

        StartCoroutine(BossSpawnTrapsSplitWithWarningRoutine());
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

    IEnumerator BossSpawnTrapsSplitWithWarningRoutine()
    {
        float warn = BossWarnSeconds();

        var reserved = new HashSet<Vector2Int>();
        var batches = new List<TrapSpawnBatch>();

        // Roll ONE option for this entire cast (kind + pattern)
        var castOpt = GetBossTrapOptionForThisSpawn();

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
                        obstacleManager.spikeOneShotDamage, 1f, 1);
                    break;

                case CastleData.BossTrapKind.Poison:
                    gameBoard.SetFloorEffect(c, Board.FloorEffectType.Poison,
                        obstacleManager.poisonTickDamage, obstacleManager.poisonTickInterval, obstacleManager.poisonTicks);
                    break;

                case CastleData.BossTrapKind.Fire:
                    gameBoard.SetFloorEffect(c, Board.FloorEffectType.Burn,
                        obstacleManager.fireTickDamage, obstacleManager.fireTickInterval, obstacleManager.fireTicks);
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
        if (_bossGravityBlinkCR != null)
        {
            StopCoroutine(_bossGravityBlinkCR);
            _bossGravityBlinkCR = null;
        }

        if (bossGravityIncreasedImage)
            bossGravityIncreasedImage.enabled = false;

        if (gravityText)
            gravityText.color = gravityTextDefaultColor;
    }

    void SetBossGravityVisualsActive(bool active)
    {
        if (active)
        {
            if (bossGravityIncreasedImage)
                bossGravityIncreasedImage.enabled = true;

            if (gravityText)
                gravityText.color = gravityTextBossColor;
        }
        else
        {
            ResetBossGravityVisuals();
        }
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
            bossGravityIncreasedImage.enabled = true;
    }

    // ================== Player Special Co-Routines ==================

    System.Collections.IEnumerator PlayerReducedGravityCo(float mult, float seconds)
    {
        _playerGravityMultActive = mult;

        yield return new WaitForSeconds(seconds);

        _playerGravityMultActive = 1f;
        _playerGravityCR = null;
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
    }

    void ResetRunGridToBase()
    {
        if (!gameBoard) return;

        CacheBaseBoardSizeIfNeeded();
        if (_baseBoardWidth <= 0 || _baseBoardHeight <= 0) return;

        gameBoard.SetGridSize(_baseBoardWidth, _baseBoardHeight);
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

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;

        if (isPaused || (runModsPanelRoot && runModsPanelRoot.activeSelf))
            EnterUICursorMode();
        else
            EnterGameplayCursorMode();
    }

    IEnumerator ReapplyUICursorNextFrame()
    {
        yield return null;
        EnterUICursorMode();
    }

}
