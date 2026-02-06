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

    [Header("Cursor (Gameplay)")]
    public UICursorController pauseCursor;

    [Header("Level Progression")]
    public EnemyCastleUI enemyCastleUI;
    public CastleData[] castlesByLevel;
    int currentLevel = 0;

    [Header("Round Win Audio")]
    public AudioClip roundWinClip;
    [Range(0f, 1f)] public float roundWinClipVolume = 1f;

    [Header("Drop Speed Progression")]
    [SerializeField] TMP_Text levelTimerText;
    [SerializeField] float startFallInterval = 1.0f; 

    [SerializeField] float minFallInterval = 0.10f;                // Clamp (fastest allowed)
    [SerializeField] float fallIntervalDecreasePerSecond = 0.008f;  // In-level ramp (shrinks interval over time)

    // Smooth non-compounding per-level start speed
    [SerializeField] float baseLevelGravity = 1.0f;                // Level 1 gravity baseline
    [SerializeField] float gravityIncreasePerLevel = 0.15f;        // Added each new level 

    // Runtime cache 
    float _level1FallInterval;        // Snapshot of Level 1 baseline interval
    float _levelTimer = 0f;
    float _thisLevelStartInterval;    // Computed at each level start
    int _lastLevelInitialized = -999; // Prevents double-applying

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
    public int maxUnitLives = 10; // Base (starting unit lives)
    [SerializeField] int unitLives = 20; // Current lives (Based on max + buffs)

    public TMP_Text unitLivesText;
    public Slider unitLivesSlider;

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

    // =========== Shop Buff Effective Values ===========
    int EffectiveMaxUnitLives => maxUnitLives + ShopBuffEffects.UnitLivesBonus; // Unit Lives Up: +2 per level
    float EffectivePieceGravityMult => Mathf.Max(0.01f, pieceGravityMult) * ShopBuffEffects.GravityMultiplier; // Gravity Down: slows falling (mult < 1 => slower because interval /= mult)
    float EffectiveFallRampRateMult => Mathf.Max(0f, fallRampRateMult) * ShopBuffEffects.VelocityMultiplier; // Velocity Down: slows ramping (mult < 1 => slower ramp)
    float EffectiveCurrencyChancePerClearedRow => // Gold Up: +2% chance per level
        Mathf.Clamp01(currencyChancePerClearedRow + lineClearCurrencyChanceAdd + ShopBuffEffects.GoldChanceBonus);
    float EffectiveLuck => luck + ShopBuffEffects.LuckBonus; // Luck Up: +10 per level 

    readonly Queue<TetrominoData> bag = new();
    public int score { get; private set; }
    bool gameOver = false;
    bool levelWon = false;
    private bool winQueued = false;

    public ScoreUI scoreUI;
    public HighScoreUI highScoreUI;
    public TMP_Text levelText;

    public int CurrentLevel => currentLevel;
    public bool IsRoundActive => !isPaused && !gameOver && !levelWon;

    private CastleData currentCastleData;

    void Start()
    {
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

        _level1FallInterval = startFallInterval; // Cache initial value

        // Initialize unit lives
        unitLives = EffectiveMaxUnitLives;
        SetupUnitLivesUI();

        if (gameBoard != null)
        {
            gameBoard.TileDied -= OnBoardTileDied; // Prevents double-subscribe if Start re-runs
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
        if (currencyUI) currencyUI.Refresh();
        if (AudioManager.I) AudioManager.I.PlayMusic(AudioManager.I.bgmLoop, true, AudioManager.I.musicVolume);

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

        // Level timer + fall speed ramp 
        if (!isPaused && !gameOver && !levelWon)
        {
            _levelTimer += Time.deltaTime;
            UpdateLevelTimerUI();

            // Keep the currently falling piece synced to the current interval
            if (piece && piece.enabled)
                piece.SetFallInterval(GetCurrentFallInterval(), resetAccumulator: false);
        }
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
        if (AudioManager.I) AudioManager.I.StopMusic();
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
        // Score rule: rowsCleared × 10
        int gained = Mathf.Max(0, rowsCleared) * 10;
        score += gained;
        if (scoreUI) scoreUI.Set(score);

        if (rowsCleared > 0 && AudioManager.I)
            AudioManager.I.PlayRandomLineClear();

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
        if (rowsCleared > 0 && gameBoard && enemyCastleUI && enemyCastleUI.castleImage)
        {
            foreach (var kv in rowDamage)
            {
                int rowY = kv.Key;
                int dmg = Mathf.Max(0, kv.Value);

                // Use dominant monster for this row pick attack sprite
                Sprite attackSprite = null;
                MonsterData attackerMD = null;

                if (rowDominantMonster != null && rowDominantMonster.TryGetValue(rowY, out var md) && md)
                {
                    attackerMD = md; // Remember for SFX on impact
                    attackSprite = md.attackSprite ? md.attackSprite : md.portrait;
                }

                // Choose a start column from the last-placed piece's columns on this row
                int col = UnityEngine.Random.Range(0, gameBoard.width);
                if (colsByRow != null && colsByRow.TryGetValue(rowY, out var cols) && cols != null && cols.Count > 0)
                    col = cols[UnityEngine.Random.Range(0, cols.Count)];

                Vector2 startBoard = gameBoard.CellToAnchoredPos(new Vector2Int(col, rowY));
                Vector2 targetCastle = enemyCastleUI.castleImage.rectTransform.anchoredPosition;

                // Convert both to the projectileRoot (UI_Canvas) space
                var root = projectileRoot ? projectileRoot : gameBoard.gridRoot;
                Vector2 start = LocalTo(root, gameBoard.gridRoot, startBoard);
                Vector2 target = LocalTo(root, enemyCastleUI.castleImage.rectTransform, targetCastle);

                SpawnAttackProjectile(attackSprite, start, target, dmg, attackerMD);
            }
        }

        // NO immediate ApplyDamage here; damage applies on impact
    }

    void InitLevel(int levelIndex)
    {
        levelWon = false;
        winQueued = false;

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
            // Priority: bossBGM > levelBGM > global bgmLoop
            if (data.isBossLevel && data.bossBGM)
                AudioManager.I.PlayMusic(data.bossBGM, loop: true, vol: data.bossBGMVolume);
            else if (data.levelBGM)
                AudioManager.I.PlayMusic(data.levelBGM, loop: true, vol: data.levelBGMVolume);
            else
                AudioManager.I.PlayMusic(AudioManager.I.bgmLoop, loop: true, vol: AudioManager.I.musicVolume);
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
            AudioManager.I.StopMusic(); // if your AudioManager uses a different method name, use that
            if (roundWinClip)
                AudioManager.I.PlaySFX(roundWinClip, roundWinClipVolume);
        }

        if (nextPreview) nextPreview.ClearPreview();

        currentLevel++;

        int roundsWon = currentLevel;

        // +5 every round, plus +5 more for each completed set of 3 rounds
        int bonusSteps = roundsWon / 3;
        float misfortuneGain = 5f + (5f * bonusSteps);

        misfortune += misfortuneGain;
        RunModsStore.Misfortune = misfortune; // Keep store in sync

        int gained = GetRoundWinCurrency();

        CurrencyStore.Add(gained);
        if (currencyUI) currencyUI.Refresh();

        // Show rewards before continuing
        if (roundRewardUI)
        {
            roundRewardUI.Show(buffPool, debuffPool, OnRoundModsChosen, gained);
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
        }

        if (debuff)
        {
            _runDebuffs.Add(debuff);
            RunModsStore.Debuffs.Add(debuff);
            debuff.Apply(this);
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

        InitLevel(currentLevel); // Sets castle to full HP and updates level text

        bag.Clear();
        monstersBag.Clear();
        RefillBag();
        EnsureMinBag(3);
        ShowNextPreview();
        SpawnNextPiece();

        EnterGameplayCursorMode();

        levelWon = false; // re-arm
    }

    void EndRunAsWin()
    {
        gameOver = true;
        if (AudioManager.I) AudioManager.I.StopMusic();
        if (highScoreUI) highScoreUI.TryShow(score);
        if (restartButton) restartButton.gameObject.SetActive(true);

        EnterUICursorMode();
    }

    void ActivateSpecial()
    {
        if (gameOver || gameBoard == null || selectedCharacter == null) return;

        // Require full gauge
        if (specialGauge < specialGaugeMax) return;

        switch (selectedCharacter.ability)
        {
            case SpecialAbility.ClearBottomRows:
                {
                    int rows = Mathf.Max(1, selectedCharacter.clearRows);
                    int squaresCleared = gameBoard.ClearBottomRows(rows);

                    // Reset gauge on use
                    specialGauge = 0f;
                    if (activateSpecialGaugeText)
                        activateSpecialGaugeText.gameObject.SetActive(false);

                    UpdateSpecialUI();

                    if (squaresCleared > 0)
                    {
                        // 1 point / damage per square
                        score += squaresCleared;
                        if (scoreUI) scoreUI.Set(score);

                        if (AudioManager.I) AudioManager.I.PlayRandomLineClear();

                        if (enemyCastleUI && !levelWon)
                        {
                            enemyCastleUI.ApplyDamage(squaresCleared);
                            if (enemyCastleUI.currentHP <= 0) OnCastleDestroyed();
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

                    // Reset gauge
                    specialGauge = 0f;
                    UpdateSpecialUI();
                    break;
                }
        }
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

    void SpawnAttackProjectile(Sprite sprite, Vector2 startAnchored, Vector2 targetAnchored, int damage, MonsterData attackerMD)
    {
        if (projectileRoot == null) projectileRoot = gameBoard.gridRoot;

        var go = new GameObject("AttackProjectile", typeof(UnityEngine.UI.Image));
        var img = go.GetComponent<UnityEngine.UI.Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.SetParent(projectileRoot, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = gameBoard.GetCellSize();
        rt.anchoredPosition = startAnchored;

        StartCoroutine(MoveProjectileAndHit(rt, targetAnchored, damage, attackerMD));
    }

    System.Collections.IEnumerator MoveProjectileAndHit(RectTransform rt, Vector2 targetAnchored, int damage, MonsterData attackerMD)
    {
        float speed = Mathf.Max(10f, projectileSpeed);
        while (rt && (rt.anchoredPosition - targetAnchored).sqrMagnitude > 9f)
        {
            rt.anchoredPosition = Vector2.MoveTowards(rt.anchoredPosition, targetAnchored, speed * Time.deltaTime);
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

        if (AudioManager.I) AudioManager.I.PlaySFX(AudioManager.I.sfxRestart);
        if (piece) piece.ResetPiece(); // Stop the current drop
        if (gameBoard) gameBoard.ClearAll(); // Clear board tiles

        // Reset run state
        RunModsStore.ResetAll();
        ResetRunMods();

        // Reset Level 1 difficulty baseline + unit lives
        unitLives = EffectiveMaxUnitLives;
        SetupUnitLivesUI();

        _lastLevelInitialized = -999;

        currentLevel = 0;
        InitLevel(currentLevel);
        gameOver = false;

        specialGaugeMax = (selectedCharacter && selectedCharacter.specialGaugeMax > 0f)
        ? selectedCharacter.specialGaugeMax
        : 100f;
        specialGauge = 0f;
        UpdateSpecialUI();

        score = 0;
        if (scoreUI) scoreUI.Set(score);
        if (highScoreUI) highScoreUI.Hide(); // Close high-score panel if it was open

        // Reset bag and preview
        bag.Clear();
        monstersBag.Clear();
        RefillBag();
        EnsureMinBag(2);
        ShowNextPreview();
        SpawnNextPiece(); // Spawn fresh piece
        if (restartButton) restartButton.gameObject.SetActive(false);
        if (AudioManager.I) AudioManager.I.PlayMusic(AudioManager.I.bgmLoop);
    }

    public void ResumeGame()
    {
        ClosePauseSubPanels();

        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (pausePanel) pausePanel.SetActive(false);
        EnterGameplayCursorMode();
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;

        // Stop music before loading main menu
        if (AudioManager.I) AudioManager.I.StopMusic();
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

        // Level 1 => levelIndex 0
        float levelGravity = baseLevelGravity + (Mathf.Max(0, levelIndex) * gravityIncreasePerLevel);

        // Convert gravity to a starting interval
        _thisLevelStartInterval = _level1FallInterval / Mathf.Max(0.01f, levelGravity);

        UpdateLevelTimerUI();
    }

    float GetCurrentFallInterval()
    {
        // During the level, the interval shrinks over time => drops faster.
        float ramp = fallIntervalDecreasePerSecond * EffectiveFallRampRateMult;
        float interval = _thisLevelStartInterval - (_levelTimer * ramp);
  
        interval /= EffectivePieceGravityMult; // Apply existing gravity modifier (>1 = faster, <1 = slower)

        return Mathf.Max(minFallInterval, interval);  
    }

    void UpdateLevelTimerUI()
    {
        if (!levelTimerText) return;

        int mins = Mathf.FloorToInt(_levelTimer / 60f);
        int secs = Mathf.FloorToInt(_levelTimer % 60f);
        levelTimerText.text = $"{mins:00}:{secs:00}";
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

    // ================== Helper/Utility ==================

    Vector2 BoardRightGutterY(int rowY, float marginCells = 0.6f)
    {
        // y from the row's cell center
        float y = gameBoard.CellToAnchoredPos(new Vector2Int(0, rowY)).y;

        // x = right edge of grid + margin
        float halfW = gameBoard.gridRoot.rect.width * 0.5f;   // Right edge in anchored coords
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
