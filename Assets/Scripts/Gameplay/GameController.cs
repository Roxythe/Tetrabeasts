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

    [Header("Player")]
    public UnityEngine.UI.Image playerPortrait;
    public TMPro.TMP_Text playerName;

    [Header("Characters")]
    public PlayerCharacterData selectedCharacter;   // set this in the inspector, or via a select UI
    public PlayerCharacterData[] roster;           // optional: populate for a character select screen

    [Header("Monsters")]
    public MonsterData[] fallbackMonsters; // 2 defaults, set in Inspector
    readonly Queue<MonsterData[]> monstersBag = new(); // parallel to 'bag'

    [Header("Special Gauge UI")]
    public UnityEngine.UI.Slider specialSlider;  // assign in inspector (min=0, max=1)
    public TMP_Text specialText;                 // optional 0% - 100%

    [Header("Special Gauge")]
    public float specialGauge = 0f;
    public float specialGaugeMax = 100f; // overridden by selectedCharacter if set

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


    readonly Queue<TetrominoData> bag = new();
    public int score { get; private set; }
    bool gameOver = false;
    bool levelWon = false;

    public ScoreUI scoreUI;        
    public HighScoreUI highScoreUI;
    public TMP_Text levelText;

    public int CurrentLevel => currentLevel;

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

        // Apply character special gauge max
        if (selectedCharacter && selectedCharacter.specialGaugeMax > 0f)
            specialGaugeMax = selectedCharacter.specialGaugeMax;

        specialGauge = 0f;
        UpdateSpecialUI();

        InitLevel(currentLevel);

        if (bag.Count == 0) RefillBag();
        EnsureMinBag(3);
        ShowNextPreview();
        SpawnNextPiece();

        score = 0;
        if (scoreUI) scoreUI.Set(score);
        if (restartButton) restartButton.gameObject.SetActive(false);
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

    void ShowNextPreview()
    {
        if (!nextPreview) return;

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

    public void RestartGame()
    {
        if (AudioManager.I) AudioManager.I.PlaySFX(AudioManager.I.sfxRestart);
        if (piece) piece.ResetPiece(); // Stop the current drop
        if (gameBoard) gameBoard.ClearAll(); // Clear board tiles

        // Reset run state
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

        foreach (var d in normals)
        {
            TetrominoData use = d;

            // Gate: maybe replace with special
            if (specialsAvailable && Random.value < specialChancePerEnqueue)
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

        // Spawn one projectile per cleared row
        if (rowsCleared > 0 && gameBoard && enemyCastleUI && enemyCastleUI.castleImage)
        {
            foreach (var kv in rowDamage)
            {
                int rowY = kv.Key;
                int dmg = Mathf.Max(0, kv.Value);

                // Use dominant monster for this row pick attack sprite
                Sprite attackSprite = null;
                if (rowDominantMonster != null && rowDominantMonster.TryGetValue(rowY, out var md) && md)
                    attackSprite = md.attackSprite ? md.attackSprite : md.portrait;

                // Choose a start column from the last-placed piece's columns on this row
                int col = UnityEngine.Random.Range(0, gameBoard.width);
                if (colsByRow != null && colsByRow.TryGetValue(rowY, out var cols) && cols != null && cols.Count > 0)
                    col = cols[UnityEngine.Random.Range(0, cols.Count)];

                // pick start in board space
                Vector2 startBoard = gameBoard.CellToAnchoredPos(new Vector2Int(col, rowY));
                // pick target in castle-image space (its anchored center)
                Vector2 targetCastle = enemyCastleUI.castleImage.rectTransform.anchoredPosition;

                // convert both to the projectileRoot (UI_Canvas) space
                var root = projectileRoot ? projectileRoot : gameBoard.gridRoot;  // you’ll set this to UI_Canvas
                Vector2 start = LocalTo(root, gameBoard.gridRoot, startBoard);
                Vector2 target = LocalTo(root, enemyCastleUI.castleImage.rectTransform, targetCastle);

                // spawn
                SpawnAttackProjectile(attackSprite, start, target, dmg);
            }
        }

        // NO immediate ApplyDamage here; damage applies on impact
    }

    void InitLevel(int levelIndex)
    {
        levelWon = false;

        CastleData data = null;
        if (castlesByLevel != null && levelIndex >= 0 && levelIndex < castlesByLevel.Length)
            data = castlesByLevel[levelIndex];

        if (!enemyCastleUI)
            enemyCastleUI = FindFirstObjectByType<EnemyCastleUI>(FindObjectsInactive.Include);

        if (enemyCastleUI && data)
        {
            int levelNumber = levelIndex + 1;
            enemyCastleUI.InitCastle(data, levelNumber);
            castleAttackInterval = data.projectileInterval;
            projectileSpeed = data.projectileSpeed;
            castleProjectileSprite = data.projectileSprite;
            castleProjectileDamage = data.projectileDamage;
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

        if (nextPreview)
            nextPreview.ClearPreview();

        currentLevel++;

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
    }

    void SpawnAttackProjectile(Sprite sprite, Vector2 startAnchored, Vector2 targetAnchored, int damage)
    {
        if (projectileRoot == null) projectileRoot = gameBoard.gridRoot; // fallback

        var go = new GameObject("AttackProjectile", typeof(UnityEngine.UI.Image));
        var img = go.GetComponent<UnityEngine.UI.Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.SetParent(projectileRoot, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = gameBoard.GetCellSize(); // roughly tile-sized
        rt.anchoredPosition = startAnchored;

        StartCoroutine(MoveProjectileAndHit(rt, targetAnchored, damage));
    }

    System.Collections.IEnumerator MoveProjectileAndHit(RectTransform rt, Vector2 targetAnchored, int damage)
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
            enemyCastleUI.ApplyDamage(damage);
            if (enemyCastleUI.currentHP <= 0) OnCastleDestroyed();
        }
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

        // On impact: deal damage if we have a valid, living target
        if (targetCell.HasValue && !levelWon && !gameOver)
        {
            bool aliveAfter = gameBoard.DamageTile(targetCell.Value, castleProjectileDamage);
            if (gameBoard.TryGetMonster(targetCell.Value, out var inst) && inst.data)
                Debug.Log($"Hit {inst.data.name} at {targetCell.Value}: {inst.hp}/{inst.data.maxHealth}"
                          + (aliveAfter ? "" : " (DEAD)"));
        }

    }

}
