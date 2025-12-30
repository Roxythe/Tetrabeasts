using UnityEngine;
using UnityEngine.UI;
using System.Linq;

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
    readonly List<RectTransform> hintOverlays = new();
    static readonly Color hintColor = new Color(1f, 0f, 0f, 0.25f); // light red

    float fallTimer, lockTimer;


    // Single-pixel white sprite for UI fills (so Image actually renders)
    static Sprite _onePx;
    static Sprite OnePx()
    {
        if (_onePx) return _onePx;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);
        _onePx = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _onePx;
    }

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

        if (data.special != SpecialType.None)
            UpdateSpecialHints();
        else
            UpdateNormalHints();
    }

    void BuildVisuals()
    {
        bool isSpecial = data.special != SpecialType.None;

        foreach (var v in visuals) if (v) Destroy(v.gameObject);
        visuals.Clear();

        if (board == null || board.gridRoot == null) return;

        var activeSet = new HashSet<Vector2Int>(cells);

        for (int i = 0; i < cells.Count; i++)
        {
            var c = cells[i];

            // Root = transparent container (border drawn by Edge_* children)
            var img = Instantiate(activeTilePrefab, board.gridRoot);
            var rt = img.rectTransform;

            img.sprite = null;
            img.raycastTarget = false;
            img.color = new Color(0f, 0f, 0f, 0f);

            var anyOutline = img.GetComponent<UnityEngine.UI.Outline>();
            if (anyOutline) Destroy(anyOutline);

            // size/position
            rt.sizeDelta = board.GetCellSize();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = board.CellToAnchoredPos(c);

            // pick outline color (gold while immune, otherwise black)
            var gc = GetComponent<GameController>();
            Color borderColor = (gc && gc.immunityActive) ? board.immuneBorderColor : board.normalBorderColor;
            board.SetInlineBorderColor(rt, borderColor);

            // Build inner fill FIRST so ApplySharedEdges can resize it correctly on shared edges
            var fillGO = new GameObject("ActiveFill", typeof(UnityEngine.UI.Image));
            var fill = fillGO.GetComponent<UnityEngine.UI.Image>();
            fill.raycastTarget = false;
            fill.sprite = OnePx();
            fill.type = UnityEngine.UI.Image.Type.Simple;
            fill.color = color;

            var frt = fill.rectTransform;
            frt.SetParent(rt, false);
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = rt.sizeDelta;
            frt.anchoredPosition = Vector2.zero;
            frt.SetAsFirstSibling(); // icons/portraits sit on top

            // halve thickness on shared edges (active-active OR active-placed)
            bool L = activeSet.Contains(c + Vector2Int.left) || (board.InBounds(c + Vector2Int.left) && !board.IsFree(c + Vector2Int.left));
            bool R = activeSet.Contains(c + Vector2Int.right) || (board.InBounds(c + Vector2Int.right) && !board.IsFree(c + Vector2Int.right));
            bool U = activeSet.Contains(c + Vector2Int.up) || (board.InBounds(c + Vector2Int.up) && !board.IsFree(c + Vector2Int.up));
            bool D = activeSet.Contains(c + Vector2Int.down) || (board.InBounds(c + Vector2Int.down) && !board.IsFree(c + Vector2Int.down));

            board.ApplySharedEdges(rt, L, R, U, D);

            // Portrait/special icon (parented to inner fill so it always matches varying border thickness)
            var innerRT = frt;
            if (isSpecial && data.specialSprite != null)
            {
                var go = new GameObject("SpecialIcon", typeof(UnityEngine.UI.Image));
                var p = go.GetComponent<UnityEngine.UI.Image>();
                p.sprite = data.specialSprite; p.preserveAspect = true;
                p.raycastTarget = false;

                var prt = p.rectTransform; prt.SetParent(innerRT, false);
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.sizeDelta = innerRT.sizeDelta - new Vector2(2f, 2f);
                prt.anchoredPosition = Vector2.zero;
            }
            else if (!isSpecial && i < monstersForCells.Count && monstersForCells[i] && monstersForCells[i].portrait)
            {
                var go = new GameObject("MonsterPortrait", typeof(UnityEngine.UI.Image));
                var p = go.GetComponent<UnityEngine.UI.Image>();
                p.sprite = monstersForCells[i].portrait; p.preserveAspect = true;
                p.raycastTarget = false;

                var prt = p.rectTransform; prt.SetParent(innerRT, false);
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.sizeDelta = innerRT.sizeDelta - new Vector2(2f, 2f);
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
        bool isSpecial = data.special != SpecialType.None;

        for (int i = 0; i < cells.Count; i++)
            if (cells[i].y >= board.height) { toppedOut = true; break; }

        if (toppedOut)
        {
            foreach (var v in visuals) if (v) Destroy(v.gameObject);
            visuals.Clear();

            GetComponent<GameController>().OnPieceLocked(0, new System.Collections.Generic.List<Vector2Int>(),
                                                         0, 0f, new System.Collections.Generic.Dictionary<int, int>(),
                                                         new System.Collections.Generic.Dictionary<int, MonsterData>(),
                                                         new System.Collections.Generic.Dictionary<int,
                                                         System.Collections.Generic.List<int>>());

            GetComponent<GameController>().GameOver();
            enabled = false;
            return;
        }

        var gc = GetComponent<GameController>();

        // ---------- SPECIAL PIECES ----------
        if (isSpecial)
        {
            // Clean up active visuals + any hint overlays
            ClearHints();
            foreach (var v in visuals) if (v) Destroy(v.gameObject);
            visuals.Clear();

            // Landing/center cell (clamped)
            var center = cells[0];
            center.x = Mathf.Clamp(center.x, 0, board.width - 1);
            center.y = Mathf.Clamp(center.y, 0, board.height - 1);

            // This list is used by Death/Bomb/Bolt only
            var toRemove = new List<Vector2Int>();

            switch (data.special)
            {
                case SpecialType.Death:
                    {
                        int dropY = center.y;
                        while (dropY > 0 && board.IsFree(new Vector2Int(center.x, dropY - 1)))
                            dropY--;

                        var landing = new Vector2Int(center.x, dropY);

                        // Find the first monster below the landing in that column
                        MonsterData chosen = null;
                        for (int y = landing.y - 1; y >= 0; y--)
                        {
                            var c = new Vector2Int(landing.x, y);
                            if (!board.InBounds(c)) break;
                            if (!board.IsFree(c))
                            {
                                if (board.TryGetMonster(c, out var inst) && inst.data)
                                    chosen = inst.data;
                                break;
                            }
                        }

                        // If we found a monster type, remove all of that type (board-wide)
                        if (chosen)
                        {
                            for (int y = 0; y < board.height; y++)
                                for (int x = 0; x < board.width; x++)
                                {
                                    var c = new Vector2Int(x, y);
                                    if (board.TryGetMonster(c, out var inst) && inst.data == chosen)
                                        toRemove.Add(c);
                                }
                        }
                        break;
                    }

                case SpecialType.Bomb:
                    {
                        // 3x3 around landing
                        for (int dy = -1; dy <= 1; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                var c = new Vector2Int(center.x + dx, center.y + dy);
                                if (board.InBounds(c) && !board.IsFree(c)) toRemove.Add(c);
                            }
                        break;
                    }

                case SpecialType.Bolt:
                    {
                        // All blocks strictly below in same column
                        for (int y = center.y - 1; y >= 0; y--)
                        {
                            var c = new Vector2Int(center.x, y);
                            if (!board.IsFree(c)) toRemove.Add(c);
                        }
                        break;
                    }

                case SpecialType.Earthquake:
                    {
                        // SFX (once)
                        if (AudioManager.I && data.specialSFX) AudioManager.I.PlaySFX(data.specialSFX);

                        // Flash: whole board (or only occupied, based on SO flag)
                        var affectedEQ = new List<Vector2Int>();
                        for (int y = 0; y < board.height; y++)
                            for (int x = 0; x < board.width; x++)
                                affectedEQ.Add(new Vector2Int(x, y));
                        board.FlashCells(affectedEQ, data.specialFlashSprite, data.flashOnlyOccupied);

                        board.SettleAllColumns();

                        board.ClearFullLines(out int rowsEQ, out var removedEQ, out int dmgEQ, out float chargeEQ,
                                             out var rowDamageEQ, out var rowDomEQ);

                        var emptyCols = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<int>>();

                        gc.OnPieceLocked(rowsEQ, removedEQ, dmgEQ, chargeEQ, rowDamageEQ, rowDomEQ, emptyCols);

                        enabled = false;
                        if (gc != null && gc.CanSpawnNewPiece()) gc.SpawnNextPiece();
                        return; // <<< IMPORTANT: stop here so we don't run the common path below
                    }
            }

            // SFX 
            if (AudioManager.I && data.specialSFX) AudioManager.I.PlaySFX(data.specialSFX);

            // Flash on affected cells using data flags
            board.FlashCells(toRemove, data.specialFlashSprite, data.flashOnlyOccupied);

            // Remove targets and make only the directly-above tiles fall sparsely
            board.RemoveCellsAndFall(toRemove, out var removedA, out int dmgA, out float chargeA);

            board.ClearFullLines(out int rowsAfter, out var removedB, out int dmgB, out float chargeB,
                                 out var rowDamageB, out var rowDomB);

            var colsByRowSpecial = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<int>>();

            gc.OnPieceLocked(rowsAfter, removedB, dmgA + dmgB, chargeA + chargeB, rowDamageB, rowDomB, colsByRowSpecial);

            enabled = false;
            if (gc != null && gc.CanSpawnNewPiece()) gc.SpawnNextPiece();
            return;
        }

        // ---------- NORMAL PIECES ----------

        ClearHints();
        for (int i = 0; i < cells.Count; i++)
        {
            var sprite = (i < monstersForCells.Count && monstersForCells[i]) ? monstersForCells[i].portrait : null;
            var placed = board.InstantiateTileUI(color, sprite);
            placed.anchoredPosition = board.CellToAnchoredPos(cells[i]);
            board.Place(cells[i], placed);

            MonsterData md = (i < monstersForCells.Count) ? monstersForCells[i] : null;
            board.SetMonsterAt(cells[i], new Board.MonsterInstance(md));
        }

        foreach (var v in visuals)

            // Recompute per-edge border thickness for the new blocks + neighbors
            for (int i = 0; i < cells.Count; i++)
                board.RefreshTileBordersAround(cells[i]);

        foreach (var v in visuals) if (v) Destroy(v.gameObject);
        visuals.Clear();

        var colsByRow = new Dictionary<int, List<int>>();
        for (int i = 0; i < cells.Count; i++)
        {
            var c = cells[i];
            if (c.y < 0 || c.y >= board.height) continue;
            if (!colsByRow.TryGetValue(c.y, out var list)) { list = new List<int>(); colsByRow[c.y] = list; }
            if (!list.Contains(c.x)) list.Add(c.x);
        }

        // Call the extended Board method
        board.ClearFullLines(out int rowsCleared, out var removedCells, out int damageFromMonsters,
                             out float specialChargeFromMonsters, out var rowDamage, out var rowDominantMonster);

        // Pass colsByRow forward 
        int levelBefore = gc.CurrentLevel;
        gc.OnPieceLocked(rowsCleared, removedCells, damageFromMonsters, specialChargeFromMonsters, rowDamage, rowDominantMonster, colsByRow);

        enabled = false;
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

    void ClearHints()
    {
        for (int i = 0; i < hintOverlays.Count; i++)
            if (hintOverlays[i]) Destroy(hintOverlays[i].gameObject);
        hintOverlays.Clear();
    }

    void UpdateSpecialHints()
    {
        ClearHints();
        if (data.special == SpecialType.None) return;

        // Compute the landing cell for the special center
        var center = cells[0];
        // drop until blocked (or bottom)
        var dropY = center.y;
        while (dropY > 0 && board.IsFree(new Vector2Int(center.x, dropY - 1))) dropY--;
        var landing = new Vector2Int(Mathf.Clamp(center.x, 0, board.width - 1),
                                     Mathf.Clamp(dropY, 0, board.height - 1));

        // Build affected set based on the special behavior
        var affected = new HashSet<Vector2Int>();

        switch (data.special)
        {
            case SpecialType.Bomb:
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        var c = new Vector2Int(landing.x + dx, landing.y + dy);
                        if (board.InBounds(c)) affected.Add(c);
                    }
                break;

            case SpecialType.Bolt:
                for (int y = landing.y - 1; y >= 0; y--)
                {
                    var c = new Vector2Int(landing.x, y);
                    if (board.InBounds(c)) affected.Add(c);
                }
                break;

            case SpecialType.Death:
                MonsterData chosen = null;
                for (int y = landing.y - 1; y >= 0; y--)
                {
                    var c = new Vector2Int(landing.x, y);
                    if (!board.IsFree(c))
                    {
                        if (board.TryGetMonster(c, out var inst) && inst.data) chosen = inst.data;
                        break;
                    }
                }
                if (chosen)
                {
                    for (int y = 0; y < board.height; y++)
                        for (int x = 0; x < board.width; x++)
                        {
                            var c = new Vector2Int(x, y);
                            if (board.TryGetMonster(c, out var inst) && inst.data == chosen)
                                affected.Add(c);
                        }
                }
                break;

            case SpecialType.Earthquake:
                for (int y = 0; y < board.height; y++)
                    for (int x = 0; x < board.width; x++)
                    {
                        var c = new Vector2Int(x, y);
                        if (!board.IsFree(c))
                        {
                            var below = new Vector2Int(x, y - 1);
                            if (y > 0 && board.IsFree(below)) affected.Add(c);
                        }
                    }
                break;
        }

        // Draw the tinted overlays
        foreach (var c in affected)
        {
            var img = new GameObject("SpecialHint", typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            img.color = hintColor;
            var rt = img.rectTransform;
            rt.SetParent(board.gridRoot, false);
            rt.sizeDelta = board.GetCellSize();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = board.CellToAnchoredPos(c);
            hintOverlays.Add(rt);
        }
    }

    List<Vector2Int> ComputeLandingCells()
    {
        // Make a working copy of the current cells
        var landing = new List<Vector2Int>(cells);

        // Try dropping the copy straight down until it would collide
        bool canDrop = true;
        while (canDrop)
        {
            // build next test
            var next = new Vector2Int[landing.Count];
            for (int i = 0; i < landing.Count; i++)
                next[i] = landing[i] + Vector2Int.down;

            if (board.Valid(next))
            {
                for (int i = 0; i < landing.Count; i++) landing[i] = next[i];
            }
            else
            {
                canDrop = false;
            }
        }

        // Clamp to visible board only (ignore any cells above the top)
        for (int i = landing.Count - 1; i >= 0; i--)
            if (landing[i].y < 0 || landing[i].y >= board.height)
                landing.RemoveAt(i);

        return landing;
    }

    void UpdateNormalHints()
    {
        ClearHints();

        // Where would a Spacebar (HardDrop) put us right now?
        var landing = ComputeLandingCells();
        if (landing.Count == 0) return;

        foreach (var c in landing)
        {
            var img = new GameObject("GhostHint", typeof(UnityEngine.UI.Image))
                      .GetComponent<UnityEngine.UI.Image>();
            img.color = hintColor;                       // the same light red you already use
            var rt = img.rectTransform;
            rt.SetParent(board.gridRoot, false);
            rt.sizeDelta = board.GetCellSize();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = board.CellToAnchoredPos(c);
            hintOverlays.Add(rt);
        }
    }

    // ========== Inline Border Color Change ==========

    public void SetInlineBorderColor(Color c)
    {
        for (int i = 0; i < visuals.Count; i++)
        {
            var img = visuals[i] ? visuals[i].GetComponent<UnityEngine.UI.Image>() : null;
            if (img) img.color = c;   // only the root (border)
        }
    }

    public void ResetInlineBorderColor(Color c)
    {
        SetInlineBorderColor(c);
    }
}
