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
    readonly List<MonsterData> monstersForCells = new();

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
        foreach (var v in visuals) if (v) Destroy(v.gameObject);
        visuals.Clear();

        if (board == null || board.gridRoot == null) return;

        for (int i = 0; i < cells.Count; i++)
        {
            var c = cells[i];
            var img = Instantiate(activeTilePrefab, board.gridRoot);
            img.color = color;

            var rt = img.rectTransform;
            rt.sizeDelta = board.GetCellSize();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = board.CellToAnchoredPos(c);

            // child portrait (centered)
            if (i < monstersForCells.Count && monstersForCells[i] && monstersForCells[i].portrait)
            {
                var portraitGO = new GameObject("MonsterPortrait", typeof(UnityEngine.UI.Image));
                var pimg = portraitGO.GetComponent<UnityEngine.UI.Image>();
                pimg.sprite = monstersForCells[i].portrait;
                pimg.preserveAspect = true;

                var prt = pimg.rectTransform;
                prt.SetParent(rt, false);
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                // inset a bit so the portrait sits inside the tile nicely
                float inset = 4f;
                prt.sizeDelta = rt.sizeDelta - new Vector2(inset, inset);
                prt.anchoredPosition = Vector2.zero;
            }

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

            GetComponent<GameController>().OnPieceLocked(0, new System.Collections.Generic.List<Vector2Int>(), 0, 0f);
            GetComponent<GameController>().GameOver();
            enabled = false;
            return;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            var sprite = (i < monstersForCells.Count && monstersForCells[i]) ? monstersForCells[i].portrait : null;
            var placed = board.InstantiateTileUI(color, sprite);
            placed.anchoredPosition = board.CellToAnchoredPos(cells[i]);
            board.Place(cells[i], placed);

            MonsterData md = (i < monstersForCells.Count) ? monstersForCells[i] : null;
            board.SetMonsterAt(cells[i], new Board.MonsterInstance(md));
        }

        foreach (var v in visuals) if (v) Destroy(v.gameObject);
        visuals.Clear();

        board.ClearFullLines(out int rows,
                     out var removedCells,
                     out int damageFromMonsters,
                     out float specialChargeFromMonsters);

        var gc = GetComponent<GameController>();
        int levelBefore = gc.CurrentLevel;
        gc.OnPieceLocked(rows, removedCells, damageFromMonsters, specialChargeFromMonsters);

        enabled = false;

        // Only spawn if the level didn't end
        if (gc != null && gc.CurrentLevel == levelBefore && gc.CanSpawnNewPiece())
            gc.SpawnNextPiece();
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

    public void SetMonsters(MonsterData[] arr)
    {
        monstersForCells.Clear();
        if (arr != null) monstersForCells.AddRange(arr);
    }
}
