using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using System.Collections.Generic;

public class Piece : MonoBehaviour
{
    public Board board;
    public Image activeTilePrefab;

    [Header("Timing")]
    public float fallInterval = 0.8f;
    public float lockDelay = 0.3f;

    [HideInInspector] public TetrominoData data;  // assigned by controller
    [HideInInspector] public Color color = Color.cyan;

    Vector2Int origin;               // rotation/translation origin
    readonly List<Vector2Int> cells = new();
    readonly List<RectTransform> visuals = new();

    float fallTimer, lockTimer;

    void OnEnable()
    {
        fallTimer = 0f; lockTimer = 0f;
        visuals.Clear(); cells.Clear();
    }

    public void SpawnAtTop()
    {
        board.RecomputeCellMetrics();

        // center top
        origin = new Vector2Int(board.width / 2, board.height);
        foreach (var c in data.cells) cells.Add(origin + c);

        // if blocked, try one row lower; else game over
        if (!board.Valid(cells.ToArray()))
        {
            Shift(Vector2Int.down);
            if (!board.Valid(cells.ToArray())) { enabled = false; return; }
        }
        BuildVisuals();
    }

    void Update()
    {
        // INPUT
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)  TryMove(Vector2Int.left);
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) TryMove(Vector2Int.right);
        if (Keyboard.current.downArrowKey.wasPressedThisFrame)  SoftDrop();
        if (Keyboard.current.upArrowKey.wasPressedThisFrame)    RotateCW();
        if (Keyboard.current.zKey.wasPressedThisFrame)          RotateCCW();
        if (Keyboard.current.spaceKey.wasPressedThisFrame)      HardDrop();
#else
        if (Input.GetKeyDown(KeyCode.LeftArrow)) TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.RightArrow)) TryMove(Vector2Int.right);
        if (Input.GetKeyDown(KeyCode.DownArrow)) SoftDrop();
        if (Input.GetKeyDown(KeyCode.UpArrow)) RotateCW();

        if (Input.GetKeyDown(KeyCode.A)) TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) TryMove(Vector2Int.right);
        if (Input.GetKeyDown(KeyCode.S)) SoftDrop();

        if (Input.GetKeyDown(KeyCode.E)) RotateCW();
        if (Input.GetKeyDown(KeyCode.Q)) RotateCCW();
        if (Input.GetKeyDown(KeyCode.Space)) HardDrop();
#endif

        if (cells.Count == 0 || visuals.Count == 0)
        {
            fallTimer = 0f;
            lockTimer = 0f;
            return;
        }

        // GRAVITY
        fallTimer += Time.deltaTime;
        if (fallTimer >= fallInterval)
        {
            fallTimer = 0f;
            if (!TryMove(Vector2Int.down)) lockTimer += fallInterval;
            else lockTimer = 0f;
        }
        if (lockTimer >= lockDelay) Lock();
    }

    void BuildVisuals()
    {
        // clear
        foreach (var v in visuals)
            if (v) Destroy(v.gameObject);
        visuals.Clear();

        if (board == null || board.gridRoot == null)
        {
            return;
        }

        foreach (var c in cells)
        {
            var img = Instantiate(activeTilePrefab, board.gridRoot);
            img.color = color;
            var rt = img.rectTransform;
            rt.sizeDelta = board.GetCellSize();
            rt.anchoredPosition = board.CellToAnchoredPos(c);
            visuals.Add(rt);
        }
    }

    void SyncVisuals()
    {
        if (visuals.Count != cells.Count)
        {
            BuildVisuals();
            return;
        }

        for (int i = 0; i < visuals.Count; i++)
        {
            // If a tile was destroyed externally, bail out and rebuild.
            if (visuals[i] == null)
            {
                BuildVisuals();
                return;
            }

            visuals[i].anchoredPosition = board.CellToAnchoredPos(cells[i]);
        }
    }

    bool TryMove(Vector2Int delta)
    {
        var next = new Vector2Int[cells.Count];
        for (int i = 0; i < cells.Count; i++) next[i] = cells[i] + delta;
        if (!board.Valid(next)) return false;
        for (int i = 0; i < cells.Count; i++) cells[i] = next[i];
        origin += delta;
        SyncVisuals();
        return true;
    }

    void Shift(Vector2Int delta)
    {
        for (int i = 0; i < cells.Count; i++) cells[i] += delta;
        origin += delta;
    }

    void SoftDrop() { if (TryMove(Vector2Int.down)) lockTimer = 0f; }
    void HardDrop() { while (TryMove(Vector2Int.down)) { } Lock(); }

    void RotateCW()
    {
        var next = new Vector2Int[cells.Count];
        for (int i = 0; i < cells.Count; i++)
        {
            var r = cells[i] - origin;             // around origin
            var rot = new Vector2Int(r.y, -r.x);
            next[i] = origin + rot;
        }
        if (board.Valid(next))
        {
            for (int i = 0; i < cells.Count; i++) cells[i] = next[i];
            SyncVisuals();
        }
    }

    void RotateCCW()
    {
        var next = new Vector2Int[cells.Count];
        for (int i = 0; i < cells.Count; i++)
        {
            var r = cells[i] - origin;
            var rot = new Vector2Int(-r.y, r.x);
            next[i] = origin + rot;
        }
        if (board.Valid(next))
        {
            for (int i = 0; i < cells.Count; i++) cells[i] = next[i];
            SyncVisuals();
        }
    }

    void Lock()
    {
        bool toppedOut = false;
        for (int i = 0; i < cells.Count; i++)
            if (cells[i].y >= board.height) { toppedOut = true; break; }

        if (toppedOut)
        {
            foreach (var v in visuals) if (v) Destroy(v.gameObject);
            visuals.Clear();

            GetComponent<GameController>().OnPieceLocked(0);
            GetComponent<GameController>().GameOver();    // we'll add this method below
            enabled = false;
            return;
        }

        // convert active visuals into placed tiles
        for (int i = 0; i < cells.Count; i++)
        {
            var placed = board.InstantiateTileUI(color);
            placed.anchoredPosition = board.CellToAnchoredPos(cells[i]);
            board.Place(cells[i], placed);
        }

        foreach (var v in visuals) if (v) Destroy(v.gameObject);
        visuals.Clear();

        board.ClearFullLines(out int squares);
        GetComponent<GameController>().OnPieceLocked(squares);
        enabled = false; // Will be re-enabled by controller
        GetComponent<GameController>().SpawnNextPiece();
    }

    public void ResetPiece()
    {
        enabled = false;

        // Remove only the active falling tile visuals we spawned
        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i] != null)
                Destroy(visuals[i].gameObject);
        }
        visuals.Clear();

        cells.Clear();
        fallTimer = 0f;
        lockTimer = 0f;
    }


}
