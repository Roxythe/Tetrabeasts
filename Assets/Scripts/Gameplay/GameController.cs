using UnityEngine;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    public Board gameBoard;
    public Piece piece;
    public NextPreviewUI nextPreview;
    public TetrominoData[] allTetrominoes;

    readonly Queue<TetrominoData> bag = new();
    public int score { get; private set; }
    bool gameOver = false;

    public ScoreUI scoreUI;          // drag your ScoreUI here
    public HighScoreUI highScoreUI;  // drag HighScoreUI here


    void Start()
    {
        HighScoreManager.EnsureInitialized(10);

        if (allTetrominoes == null || allTetrominoes.Length == 0)
        {
            Debug.LogError("GameController: allTetrominoes is empty.");
            return;
        }
        if (!gameBoard) gameBoard = FindFirstObjectByType<Board>();
        if (!piece) piece = GetComponent<Piece>();
        if (bag.Count == 0) RefillBag();
        ShowNextPreview();
        SpawnNextPiece();

        score = 0;
        if (scoreUI) scoreUI.Set(score);
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

    public void GameOver()
    {
        gameOver = true;
        Debug.Log("Game Over");
        if (highScoreUI) highScoreUI.TryShow(score);
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
        score += Mathf.Max(0, linesCleared);    // +1 per row
        if (scoreUI) scoreUI.Set(score);
    }
}
