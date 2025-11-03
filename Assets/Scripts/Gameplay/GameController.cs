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
    public CastleData[] castlesByLevel;  // fill in inspector: level 0,1,2...
    int currentLevel = 0;

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

        InitLevel(currentLevel);

        if (bag.Count == 0) RefillBag();
        ShowNextPreview();
        SpawnNextPiece();

        score = 0;
        if (scoreUI) scoreUI.Set(score);
        if (restartButton) restartButton.gameObject.SetActive(false);
        if (AudioManager.I) AudioManager.I.PlayMusic(AudioManager.I.bgmLoop, true, AudioManager.I.musicVolume);
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

    public void OnPieceLocked(int linesCleared)
    {
        // score update
        score += Mathf.Max(0, linesCleared);
        if (scoreUI) scoreUI.Set(score);

        // play SFX if we cleared anything
        if (linesCleared > 0 && AudioManager.I)
            AudioManager.I.PlayRandomLineClear();

        // Deal castle damage if we cleared anything
        if (linesCleared > 0 && enemyCastleUI && !gameOver)
        {
            int damage = linesCleared * 10; // simple 1 dmg per row cleared
            enemyCastleUI.ApplyDamage(damage);

            // Did the castle die here?
            if (enemyCastleUI.currentHP <= 0)
            {
                // Let GameController handle victory
                OnCastleDestroyed();
            }
        }
    }

    void InitLevel(int levelIndex)
    {
        levelWon = false;

        // Pick castle data for this level
        CastleData data = null;
        if (castlesByLevel != null && levelIndex >= 0 && levelIndex < castlesByLevel.Length)
        {
            data = castlesByLevel[levelIndex];
        }

        if (!enemyCastleUI)
        {
            enemyCastleUI = FindFirstObjectByType<EnemyCastleUI>(FindObjectsInactive.Include);
        }

        if (enemyCastleUI && data)
        {
            enemyCastleUI.InitCastle(data);
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

        Debug.Log("Level WON! Castle destroyed.");

        // Give reward, increment level, etc.
        currentLevel++;

        // For now let's just end the run as a 'win' and go to high score:
        EndRunAsWin();
    }

    void EndRunAsWin()
    {
        gameOver = true;

        // Stop music, maybe play victory SFX
        if (AudioManager.I) AudioManager.I.StopMusic();
        // You could add AudioManager.I.PlaySFX(victoryClip);

        // Show high score entry
        if (highScoreUI) highScoreUI.TryShow(score);
        if (restartButton) restartButton.gameObject.SetActive(true);
    }

}
