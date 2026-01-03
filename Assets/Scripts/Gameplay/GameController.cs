using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    [Header("Progression")]
    public EnemyCastleUI enemyCastleUI;
    public CastleData[] castlesByLevel;
    int currentLevel = 0;

    [Header("Run Mods")]
    public RunModifierSO[] buffPool;
    public RunModifierSO[] debuffPool;

    public RoundRewardUI roundRewardUI; // drag the panel controller here

    readonly List<RunModifierSO> _runBuffs = new();
    readonly List<RunModifierSO> _runDebuffs = new();

    [Header("Run Mods UI Panel")]
    [SerializeField] RunModsPanelUI runModsPanelUI;
    [SerializeField] Button openRunModsButton;      // the button on pause menu
    [SerializeField] GameObject runModsPanelRoot;   // the panel GameObject to show/hide
    [SerializeField] Button closeRunModsButton;     // the close button on the panel

    [Header("Player")]
    public UnityEngine.UI.Image playerPortrait;
    public TMPro.TMP_Text playerName;

    [Header("Characters")]
    public PlayerCharacterData selectedCharacter;   // set this in the inspector, or via a select UI
    public PlayerCharacterData[] roster;           // optional: populate for a character select screen

    [Header("Monsters")]
    public MonsterData[] fallbackMonsters; // 2 defaults, set in Inspector
    readonly Queue<MonsterData[]> monstersBag = new(); // parallel to 'bag'

    [Header("Currency")]
    public CurrencyUI currencyUI;          // drag your Currency prefab instance in GameplayScene
    public int currencyPerRoundWin = 5;    // how much for winning a round
    [Range(0f, 1f)] public float currencyChancePerClearedRow = 0.10f; // 10% per cleared row
    public Sprite currencyPopupSprite;     // same coin sprite (for +1 popup)
    public float currencyPopupDuration = 0.6f; // seconds
    public AudioClip sfxCurrencyLineGain;   // play when +1 drops from a line clear

    [Header("Special Gauge UI")]
    public UnityEngine.UI.Slider specialSlider;  // assign in inspector (min=0, max=1)
    public TMP_Text specialText;                 // optional 0% - 100%

    [Header("Special Gauge")]
    public float specialGauge = 0f;
    public float specialGaugeMax = 100f; // overridden by selectedCharacter if set
    public TMP_Text activateSpecialGaugeText;

    [Header("Special Blocks")]
    public TetrominoData[] specialBlocks;      // Assign special pieces here
    public float specialChancePerEnqueue = 0.08f;

    [Header("Projectiles")]
    public RectTransform projectileRoot;  // assign to your UI canvas layer (same space as board.gridRoot)
    public float projectileSpeed = 800f;

    [Header("Bag Settings")]
    public int minBagPieces = 2;                 // keep at least this many ready
    public bool forceOneSpecialPerRefill = false; // optional "ensure a special" switch

    [Header("Castle Attacks")]
    public float castleAttackInterval = 3.0f;  // seconds between shots
    public Sprite castleProjectileSprite;      // optional; else reuse an attack sprite
    float _castleAttackTimer = 0f;
    int castleProjectileDamage = 1;

    [Header("Global Immunity Visuals")]
    public bool immunityActive = false;
    public Color immunityOutline = new Color(1f, 0.85f, 0f, 1f); // gold
    public Color normalOutline = Color.black;

    [Header("Pause")]
    public GameObject pausePanel;           // panel that holds Resume / Restart / Volume / Main Menu / Quit
    public UnityEngine.UI.Button resumeButton;
    public UnityEngine.UI.Button mainMenuButton;
    public UnityEngine.UI.Button quitButton;
    public VolumePanelUI volumePanelInPause; // your existing VolumePanelUI moved under pause menu
    public string titleSceneName = "TitleScene";
    bool isPaused = false;
    public bool IsPaused => isPaused;

    // -------- RUN MODIFIERS (reset each run) --------
    [Header("Run Modifiers (runtime)")]
    public float enemyAttackIntervalMult = 1f;     // < 1 = attacks faster, > 1 = slower
    public float enemyProjectileDamageMult = 1f;   // > 1 = more damage
    public float enemyProjectileSpeedMult = 1f;    // < 1 = Faster enemy projectiles (Currently unused)
    public float enemyCastleHpMult = 1f;

    public float specialGainMult = 1f;             // gauge gain multiplier
    public float specialDrainMult = 1f;            // if you have drain/decay anywhere (Currently unused)

    public float specialBlockChanceAdd = 0f;       // additive to specialChancePerEnqueue
    public float pieceGravityMult = 1f;            // < 1 = slower falling, > 1 = faster

    public float monsterDamageMult = 1f;           // scales monster attackPower contribution
    public float monsterSpecialGainMult = 1f;      // scales monster specialGaugeGain contribution
    public float monsterMaxHpMult = 1f;            // used when spawning monster tiles
    public float healPowerMult = 1f;
    public int healRangeAdd = 0;

    public bool disableNextPreview = false;
    public bool disableLandingHint = false;

    public int currencyPerRoundWinAdd = 0;
    public float currencyPerRoundWinMult = 1f;
    public float lineClearCurrencyChanceAdd = 0f;
    public float lineClearCurrencyAmountMult = 1f; 

    readonly Queue<TetrominoData> bag = new();
    public int score { get; private set; }
    bool gameOver = false;
    bool levelWon = false;
    private bool winQueued = false;

    public ScoreUI scoreUI;
    public HighScoreUI highScoreUI;
    public TMP_Text levelText;

    public int CurrentLevel => currentLevel;

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

        // Ensure it starts closed
        if (runModsPanelRoot) runModsPanelRoot.SetActive(false);

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

        // Wire pause buttons once
        if (resumeButton) resumeButton.onClick.AddListener(ResumeGame);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (quitButton) quitButton.onClick.AddListener(QuitGame);

        // If you moved your Volume panel under pause menu, let it NOT pause by itself
        if (volumePanelInPause) volumePanelInPause.pauseWhenOpen = false;

        if (bag.Count == 0) RefillBag();
        EnsureMinBag(3);
        ShowNextPreview();
        SpawnNextPiece();

        score = 0;
        if (scoreUI) scoreUI.Set(score);
        if (currencyUI) currencyUI.Refresh();
        //if (restartButton) restartButton.gameObject.SetActive(false); // Set to false if you want to remove restart until game over
        if (AudioManager.I) AudioManager.I.PlayMusic(AudioManager.I.bgmLoop, true, AudioManager.I.musicVolume);
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
                    ResumeGame();
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
    }

    void ResetRunMods()
    {
        _runBuffs.Clear();
        _runDebuffs.Clear();

        // Pull runtime values from the store (fresh defaults after TitleMenu reset)
        enemyAttackIntervalMult = RunModsStore.EnemyAttackIntervalMult;
        enemyProjectileDamageMult = RunModsStore.EnemyProjectileDamageMult;
        enemyProjectileSpeedMult = RunModsStore.EnemyProjectileSpeedMult;

        specialGainMult = RunModsStore.SpecialGainMult;
        specialDrainMult = RunModsStore.SpecialDrainMult;
        specialBlockChanceAdd = RunModsStore.SpecialBlockChanceAdd;

        pieceGravityMult = RunModsStore.PieceGravityMult;

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
            nextPreview.ClearPreview();   // optional, but helps hide old preview
            return;
        }

        EnsureMinBag(3);
        var next = PeekSafeHead();
        if (next == null) return;

        // aligned partner for preview
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

        // Make sure there is a valid, non-null head before we dequeue
        var head = PeekSafeHead();
        if (head == null) { return; } // nothing valid to spawn; caller will try again later

        // Dequeue the aligned pair (PeekSafeHead already trimmed nulls & topped up)
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
        // TODO: show UI, restart, etc.
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

        float chance = Mathf.Clamp01(specialChancePerEnqueue + specialBlockChanceAdd);

        foreach (var d in normals)
        {
            TetrominoData use = d;

            // Gate: maybe replace with special
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

            // Enqueue selected piece
            bag.Enqueue(use);

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

        // Optional: guarantee at least ONE special per refill batch
        if (forceOneSpecialPerRefill && specialsAvailable && specialsAddedThisRefill == 0)
        {
            // append one extra special at end of queues
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

        // Chance-based currency drops: +1 per cleared row with probability
        if (rowsCleared > 0)
        {
            for (int i = 0; i < rowsCleared; i++)
            {
                if (Random.value <= currencyChancePerClearedRow)
                {
                    CurrencyStore.Add(1);
                    if (currencyUI) currencyUI.Refresh();

                    // Pick a cleared row (prefer rows you actually cleared if provided)
                    int rowY = i;
                    if (colsByRow != null && colsByRow.Count > 0)
                    {
                        foreach (var kv in colsByRow) { rowY = kv.Key; break; }
                    }

                    // Compute start just OUTSIDE the grid's right edge, same row (in board/grid space)
                    Vector2 startBoardRight = BoardRightGutterY(rowY, -2.5f); // bump margin up if you want it further in/out

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

                // pick start in board space
                Vector2 startBoard = gameBoard.CellToAnchoredPos(new Vector2Int(col, rowY));
                // pick target in castle-image space (its anchored center)
                Vector2 targetCastle = enemyCastleUI.castleImage.rectTransform.anchoredPosition;

                // convert both to the projectileRoot (UI_Canvas) space
                var root = projectileRoot ? projectileRoot : gameBoard.gridRoot;  // Set this to UI_Canvas
                Vector2 start = LocalTo(root, gameBoard.gridRoot, startBoard);
                Vector2 target = LocalTo(root, enemyCastleUI.castleImage.rectTransform, targetCastle);

                SpawnAttackProjectile(attackSprite, start, target, dmg, attackerMD); // Spawn projectile
            }
        }

        // NO immediate ApplyDamage here; damage applies on impact
    }

    void InitLevel(int levelIndex)
    {
        levelWon = false;
        winQueued = false;

        CastleData data = null;
        if (castlesByLevel != null && levelIndex >= 0 && levelIndex < castlesByLevel.Length)
            data = castlesByLevel[levelIndex];

        if (!enemyCastleUI)
            enemyCastleUI = FindFirstObjectByType<EnemyCastleUI>(FindObjectsInactive.Include);

        currentCastleData = data; // for external reference if needed

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

        if (nextPreview) nextPreview.ClearPreview();

        currentLevel++;

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

        InitLevel(currentLevel);   // sets castle to full HP and updates level text

        bag.Clear();
        monstersBag.Clear();
        RefillBag();
        EnsureMinBag(3);
        ShowNextPreview();
        SpawnNextPiece();

        levelWon = false; // re-arm
    }

    void EndRunAsWin()
    {
        gameOver = true;
        if (AudioManager.I) AudioManager.I.StopMusic();
        if (highScoreUI) highScoreUI.TryShow(score);
        if (restartButton) restartButton.gameObject.SetActive(true);
    }

    void ActivateSpecial()
    {
        if (gameOver || gameBoard == null || selectedCharacter == null) return;

        // Gate: require full gauge
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
                        // 1 point / damage per square (your existing rule for this special)
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

                    // reset gauge
                    specialGauge = 0f;
                    UpdateSpecialUI();
                    break;
                }

            case SpecialAbility.GlobalImmunity:
                {
                    // start the timer/pulse co-routine
                    StartCoroutine(GlobalImmunityCo(Mathf.Max(0.25f, selectedCharacter.immunityDuration)));

                    // reset gauge
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
            if (playerName)
                playerName.text = selectedCharacter.displayName;
        }
    }

    List<MonsterData> GetActiveMonsterRoster()
    {
        var active = (SelectedMonstersStore.Active != null && SelectedMonstersStore.Active.Count >= 2)
                     ? SelectedMonstersStore.Active
                     : new System.Collections.Generic.List<MonsterData>(fallbackMonsters);

        // enforce 2..4 just in case
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

        bool full = specialGauge >= (specialGaugeMax - 0.001f); // small tolerance
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
                StartCoroutine(CoWinAfterDelay(0.25f)); // or your death clip length
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
        // If monsters queue got out of sync somehow, realign by dropping extras.
        while (monstersBag.Count > bag.Count && monstersBag.Count > 0)
            monstersBag.Dequeue();

        int safety = 12; // avoids infinite loops if arrays are misconfigured
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
        // Build two column pools: cols with any tile alive, cols with any tile (all dead)
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
            return; // no targets on board

        // Choose a column: prefer aliveCols, else deadOnlyCols
        var pool = (aliveCols.Count > 0) ? aliveCols : deadOnlyCols;
        int col = pool[UnityEngine.Random.Range(0, pool.Count)];

        // Find the first *living* target in that column (if none, we’ll drop to floor)
        Vector2Int? hitCell = null;
        for (int y = gameBoard.height - 1; y >= 0; y--)
        {
            var c = new Vector2Int(col, y);
            if (gameBoard.IsFree(c)) continue;

            if (gameBoard.TryGetMonster(c, out var inst) && inst.data && inst.hp > 0f)
            { hitCell = c; break; }
            // else: dead tile — projectile should pass through
        }

        // Start above top; end at hitCell (if any) or bottom
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

        SpawnCastleDownProjectile(castleProjectileSprite, start, end, hitCell); // Spawn projectile
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

        // On impact: deal damage if we have a valid, living target
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

        immunityActive = true;                      // set flag
        gameBoard.SetTilesDamageImmunity(true);

        // Start immunity: GOLD outlines everywhere
        gameBoard.SetAllTileBorderColor(gameBoard.immuneBorderColor);
        if (piece && piece.enabled) piece.SetInlineBorderColor(gameBoard.immuneBorderColor);

        if (nextPreview)
            nextPreview.SyncBorderToImmunity(true, gameBoard.immuneBorderColor, gameBoard.normalBorderColor);

        if (selectedCharacter && selectedCharacter.sfxImmunityOn && AudioManager.I)
            AudioManager.I.PlaySFX(selectedCharacter.sfxImmunityOn);

        // steady gold (no pulse)
        float warnWindow = Mathf.Min(3f, totalSeconds);
        float steady = Mathf.Max(0f, totalSeconds - warnWindow);
        for (float t = 0f; t < steady; t += Time.deltaTime) yield return null;

        if (selectedCharacter && selectedCharacter.sfxImmunityWarn && AudioManager.I)
            AudioManager.I.PlaySFX(selectedCharacter.sfxImmunityWarn);

        // Pulse outlines ONLY: gold <-> black
        int sets = 3;
        float[] setDur = { 0.9f, 0.6f, 0.45f };
        for (int s = 0; s < sets; s++)
        {
            float setTime = setDur[Mathf.Clamp(s, 0, setDur.Length - 1)];
            float pulseDur = setTime / 3f;

            for (int p = 0; p < 3; p++)
            {
                // GOLD -> BLACK
                gameBoard.SetAllTileBorderColor(Color.black);
                if (piece && piece.enabled) piece.SetInlineBorderColor(Color.black);

                if (nextPreview)
                    nextPreview.SyncBorderToImmunity(true, gameBoard.immuneBorderColor, gameBoard.normalBorderColor);

                yield return new WaitForSeconds(pulseDur * 0.5f);

                // BLACK -> GOLD
                gameBoard.SetAllTileBorderColor(gameBoard.immuneBorderColor);
                if (piece && piece.enabled) piece.SetInlineBorderColor(gameBoard.immuneBorderColor);

                if (nextPreview)
                    nextPreview.SyncBorderToImmunity(true, gameBoard.immuneBorderColor, gameBoard.normalBorderColor);

                yield return new WaitForSeconds(pulseDur * 0.5f);
            }
        }

        // End immunity: back to BLACK
        gameBoard.SetTilesDamageImmunity(false);
        immunityActive = false;                     // clear flag
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
            ? gameBoard.immuneBorderColor  // gold during immunity
            : gameBoard.normalBorderColor; // black otherwise
    }


    // ================ Pause Menu Button Functions ===================

    public void PauseGame()
    {
        if (gameOver || levelWon) return;
        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;  // pause SFX/music globally

        if (pausePanel) pausePanel.SetActive(true);

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
    }

    // Called by the Pause menu "Main Menu" button
    public void ReturnToMainMenu()
    {
        // Always resume time/audio before scene swap
        Time.timeScale = 1f;
        AudioListener.pause = false;
        isPaused = false;

        if (!string.IsNullOrEmpty(titleSceneName))
            UnityEngine.SceneManagement.SceneManager.LoadScene(titleSceneName);
        else
            Debug.LogError("GameController.titleSceneName is empty or not set.");
    }

    // Called by the Pause menu "Quit" button
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
        if (!gameBoard) return; // hard guard

        var parent = projectileRoot ? projectileRoot : gameBoard.gridRoot;
        if (!parent) return; // nothing to parent to

        // container
        var go = new GameObject("Currency+1", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredStart;

        // Icon component
        var iconGO = new GameObject("Icon", typeof(UnityEngine.UI.Image));
        var icon = iconGO.GetComponent<UnityEngine.UI.Image>();
        if (currencyPopupSprite) icon.sprite = currencyPopupSprite; // sprite optional
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
            // rise
            rt.anchoredPosition = Vector2.LerpUnclamped(start, end, u);
            // fade
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

    // ================== Helper/Utility ==================
    Vector2 BoardRightGutterY(int rowY, float marginCells = 0.6f)
    {
        // y from the row's cell center:
        float y = gameBoard.CellToAnchoredPos(new Vector2Int(0, rowY)).y;

        // x = right edge of grid + margin
        float halfW = gameBoard.gridRoot.rect.width * 0.5f;   // right edge in anchored coords (pivot-centered)
        float x = halfW + gameBoard.GetCellSize().x * marginCells;

        return new Vector2(x, y);
    }
}
