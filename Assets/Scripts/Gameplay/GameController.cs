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

    readonly Queue<TetrominoData> bag = new();
    public int score { get; private set; }
    bool gameOver = false;
    bool levelWon = false;

    public ScoreUI scoreUI;        
    public HighScoreUI highScoreUI;
    public TMP_Text levelText;

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
        if (nextPreview == null || bag.Count == 0) return;
        var next = bag.Peek();
        nextPreview.Show(next, next.color);
    }

    public void SpawnNextPiece()
    {
        if (gameOver) return;
        if (bag.Count == 0) RefillBag();
        var data = bag.Dequeue();

        piece.data = data;
        piece.color = data.color;
        piece.enabled = true;
        piece.SpawnAtTop();

        ShowNextPreview();   // update preview to the new peek
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
        foreach (var d in list) bag.Enqueue(d);
    }

    public void OnPieceLocked(int squaresCleared)
    {
        // score update: 1 point per square
        score += Mathf.Max(0, squaresCleared);
        if (scoreUI) scoreUI.Set(score);

        // SFX if we cleared anything
        if (squaresCleared > 0 && AudioManager.I)
            AudioManager.I.PlayRandomLineClear();

        // Deal castle damage: 1 dmg per square
        if (squaresCleared > 0 && enemyCastleUI && !gameOver)
        {
            int damage = squaresCleared;
            enemyCastleUI.ApplyDamage(damage);
            if (enemyCastleUI.currentHP <= 0) OnCastleDestroyed();
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
            StartNextLevel();  // fresh board + new castle, keep score
        }
        else
        {
            EndRunAsWin();     // only when last castle is beaten
        }
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

        switch (selectedCharacter.ability)
        {
            case SpecialAbility.ClearBottomRows:
                {
                    int rows = Mathf.Max(1, selectedCharacter.clearRows);
                    int squaresCleared = gameBoard.ClearBottomRows(rows);

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
                // Add more abilities here later
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

}
