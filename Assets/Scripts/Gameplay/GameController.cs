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
    public TMP_Text specialText;                 // optional “0% … 100%” readout

    [Header("Special Gauge")]
    public float specialGauge = 0f;
    public float specialGaugeMax = 100f; // overridden by selectedCharacter if set

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
    }

    void ShowNextPreview()
    {
        if (nextPreview == null || bag.Count == 0 || monstersBag.Count == 0) return;
        var next = bag.Peek();
        var m = monstersBag.Peek();
        nextPreview.Show(next, next.color, m); // new overload that accepts monsters[]
    }

    public bool CanSpawnNewPiece() => !gameOver && !levelWon;

    public void SpawnNextPiece()
    {
        if (gameOver || levelWon) return;
        if (bag.Count == 0) RefillBag();

        var data = bag.Dequeue();
        var mons = monstersBag.Dequeue();

        piece.data = data;
        piece.color = data.color;
        piece.SetMonsters(mons);
        piece.enabled = true;
        piece.SpawnAtTop();

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
        score = 0;
        if (scoreUI) scoreUI.Set(score);   
        if (highScoreUI) highScoreUI.Hide(); // Close high-score panel if it was open

        // Reset bag and preview
        bag.Clear();
        RefillBag();
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
        var list = new List<TetrominoData>(allTetrominoes);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        var roster = GetActiveMonsterRoster();
        foreach (var d in list)
        {
            bag.Enqueue(d);

            // Create a monster array (one entry per tile in the piece)
            var cellsCount = Mathf.Max(1, d.cells.Length);
            var arr = new MonsterData[cellsCount];
            for (int k = 0; k < cellsCount; k++)
                arr[k] = WeightedPick(roster);

            monstersBag.Enqueue(arr);
        }
    }

    public void OnPieceLocked(int rowsCleared,
                          List<Vector2Int> removedCells,
                          int damageFromMonsters,
                          float specialChargeFromMonsters)
    {
        // Score rule: rowsCleared × 10
        int gained = Mathf.Max(0, rowsCleared) * 10;
        score += gained;
        if (scoreUI) scoreUI.Set(score);

        // Play SFX for the clear itself (even if damage is 0)
        if (rowsCleared > 0 && AudioManager.I)
            AudioManager.I.PlayRandomLineClear();

        // Apply monster damage (precomputed by Board)
        int damage = Mathf.Max(0, damageFromMonsters);
        if (damage > 0 && enemyCastleUI && !gameOver)
        {
            enemyCastleUI.ApplyDamage(damage);
            if (enemyCastleUI.currentHP <= 0) OnCastleDestroyed();
        }

        // Apply special charge
        if (specialChargeFromMonsters > 0f)
        {
            specialGauge = Mathf.Min(specialGaugeMax, specialGauge + specialChargeFromMonsters);
            UpdateSpecialUI();
        }
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
            if (levelText) levelText.text = $"Level: {levelNumber}";
        }
        else
        {
            Debug.LogWarning("InitLevel: castle data or enemyCastleUI missing");
        }
    }

    // Call this when castle HP has been reduced to 0
    public void OnCastleDestroyed()
    {
        if (gameOver || levelWon) return;
        levelWon = true;

        currentLevel++;

        if (castlesByLevel != null && currentLevel < castlesByLevel.Length)
        {
            // Defer the level transition to next frame
            StartCoroutine(CoStartNextLevel());
        }
        else
        {
            EndRunAsWin();
        }
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
        RefillBag();
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
}
