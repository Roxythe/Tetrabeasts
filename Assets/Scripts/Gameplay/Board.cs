using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Board : MonoBehaviour
{
    [Header("Grid Size")]
    public int width = 10;
    public int height = 20;
    public float verticalBufferCells = 0.25f;

    [Header("Refs")]
    public RectTransform gridRoot;   // child under GameBoard
    public Image tilePrefab;         // TileUI_Prefab (UI Image)

    // runtime
    RectTransform boardRect;         // the GameBoard panel
    Vector2 cellSize;            // size of one (square) cell in pixels
    Vector2 contentSize;         // total pixel size actually used by the grid (width*cell, height*cell)
    readonly Transform[,] grid = null; // (not used for UI placement—kept for logic)
    Dictionary<Vector2Int, RectTransform> placed = new();

    [System.Serializable]
    public struct MonsterInstance
    {
        public MonsterData data;
        public float hp;

        public MonsterInstance(MonsterData d) { data = d; hp = (d ? d.maxHealth : 0f); }
    }

    private readonly Dictionary<Vector2Int, MonsterInstance> monsters = new();

    void Awake()
    {
        boardRect = GetComponent<RectTransform>();

        // Always isolate spawned tiles/lines under a dedicated child of THIS Board
        if (!gridRoot || gridRoot.transform.parent != transform)
        {
            var go = new GameObject("GridRoot", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            gridRoot = rt;
        }
    }

    void Start()
    {
        RecomputeCellMetrics();
        DrawGridOverlay(); // nice to have; remove if you don’t want lines
    }

    public void RecomputeCellMetrics()
    {
        var r = boardRect.rect;  // Pixel rect of the GameBoard panel
        float sWidth = r.width / width;
        float sHeight = r.height / (height + 2f * verticalBufferCells);

        float s = Mathf.Floor(Mathf.Min(sWidth, sHeight));
        s = Mathf.Max(1f, s);

        cellSize = new Vector2(s, s);
        contentSize = new Vector2(width * s, height * s);
    }

    public Vector2 CellToAnchoredPos(Vector2Int cell)
    {
        var r = boardRect.rect;

        float x0 = -contentSize.x * 0.5f;
        float y0 = -contentSize.y * 0.5f;
        float x = x0 + (cell.x + 0.5f) * cellSize.x;
        float y = y0 + (cell.y + 0.5f) * cellSize.y;
        return new Vector2(x, y);
    }

    public RectTransform InstantiateTileUI(Color color)
    {
        var t = Instantiate(tilePrefab, gridRoot);
        t.color = color;
        t.name = "Tile_" + t.GetInstanceID();
        t.gameObject.AddComponent<BoardOwned>(); 

        var rt = t.rectTransform;
        rt.sizeDelta = cellSize;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        return rt;
    }

    public RectTransform InstantiateTileUI(Color color, Sprite portrait, float inset = 4f)
    {
        var rt = InstantiateTileUI(color); // existing method
        if (portrait)
        {
            var go = new GameObject("MonsterPortrait", typeof(UnityEngine.UI.Image));
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.sprite = portrait;
            img.preserveAspect = true;

            var prt = img.rectTransform;
            prt.SetParent(rt, false);
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = rt.sizeDelta - new Vector2(inset, inset);
            prt.anchoredPosition = Vector2.zero;
        }
        return rt;
    }


    public bool InBounds(Vector2Int c) =>
        c.x >= 0 && c.x < width && c.y >= 0 && c.y < height;

    public bool IsFree(Vector2Int c) => InBounds(c) && !placed.ContainsKey(c);

    public bool Valid(Vector2Int[] cells)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            var c = cells[i];

            if (c.x < 0 || c.x >= width) return false; // X must always be inside columns
            
            if (c.y >= height) continue; // Above the board is OK (spawn zone)
            
            if (c.y < 0) return false; // Below the board is not OK (floor)

            if (placed.ContainsKey(c)) return false; // Inside the board: must be empty
        }
        return true;
    }

    public void Place(Vector2Int c, RectTransform visual)
    {
        placed[c] = visual;
        visual.anchoredPosition = CellToAnchoredPos(c);
        visual.sizeDelta = cellSize;
    }

    public void ClearAll()
    {
        placed.Clear();
        monsters.Clear();

        if (gridRoot != null)
        {
            var owned = gridRoot.GetComponentsInChildren<BoardOwned>(true);
            for (int i = owned.Length - 1; i >= 0; i--)
            {
                Destroy(owned[i].gameObject);
            }
        }

        RecomputeCellMetrics();
        DrawGridOverlay();
    }

    public void ClearFullLines(out int rowsCleared,
                           out List<Vector2Int> removedCells,
                           out int damageFromMonsters,
                           out float specialChargeFromMonsters,
                           out Dictionary<int, int> rowDamage,                    
                           out Dictionary<int, MonsterData> rowDominantMonster) 
    {
        rowsCleared = 0;
        removedCells = new List<Vector2Int>();
        damageFromMonsters = 0;
        specialChargeFromMonsters = 0f;
        rowDamage = new Dictionary<int, int>();
        rowDominantMonster = new Dictionary<int, MonsterData>();

        for (int y = 0; y < height; y++)
        {
            bool full = true;
            for (int x = 0; x < width; x++)
                if (!placed.ContainsKey(new Vector2Int(x, y))) { full = false; break; }
            if (!full) continue;

            rowsCleared++;

            // Tally this row BEFORE removal
            int dmgRow = 0;
            var counts = new Dictionary<MonsterData, int>();
            for (int x = 0; x < width; x++)
            {
                var key = new Vector2Int(x, y);
                if (monsters.TryGetValue(key, out var inst) && inst.data)
                {
                    // damage rule: only if hp>0 counts toward damage, but for dominance we count presence
                    if (inst.hp > 0f) dmgRow += Mathf.RoundToInt(inst.data.attackPower);

                    if (!counts.ContainsKey(inst.data)) counts[inst.data] = 0;
                    counts[inst.data] += 1;

                    specialChargeFromMonsters += inst.data.specialGaugeGain;
                }
            }

            damageFromMonsters += dmgRow;
            rowDamage[y] = dmgRow;

            // pick dominant (ties -> random among max)
            if (counts.Count > 0)
            {
                int max = 0;
                foreach (var kv in counts) if (kv.Value > max) max = kv.Value;
                var candidates = new List<MonsterData>();
                foreach (var kv in counts) if (kv.Value == max) candidates.Add(kv.Key);
                var dominant = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                rowDominantMonster[y] = dominant;
            }

            // remove this row 
            for (int x = 0; x < width; x++)
            {
                var key = new Vector2Int(x, y);

                if (monsters.ContainsKey(key)) monsters.Remove(key);

                if (placed.TryGetValue(key, out var rt))
                {
                    UnityEngine.Object.Destroy(rt.gameObject);
                    placed.Remove(key);
                    removedCells.Add(key);
                }
            }

            // shift down tiles + monsters above 
            for (int yy = y + 1; yy < height; yy++)
            {
                for (int x = 0; x < width; x++)
                {
                    var from = new Vector2Int(x, yy);
                    var to = new Vector2Int(x, yy - 1);

                    if (placed.TryGetValue(from, out var rt))
                    {
                        placed.Remove(from);
                        placed[to] = rt;
                        rt.anchoredPosition = CellToAnchoredPos(to);
                    }
                    if (monsters.TryGetValue(from, out var inst))
                    {
                        monsters.Remove(from);
                        monsters[to] = inst;
                    }
                }
            }

            y--; // re-check same row after shift
        }

        CleanOrphanedTiles();
    }

    public int ClearBottomRows(int rows)
    {
        if (rows <= 0) return 0;
        rows = Mathf.Min(rows, height);

        int squaresCleared = 0;

        var toDelete = new List<Vector2Int>();
        foreach (var kv in placed)
            if (kv.Key.y < rows) toDelete.Add(kv.Key);

        foreach (var key in toDelete)
        {
            if (placed.TryGetValue(key, out var rt))
            {
                Destroy(rt.gameObject);
            }
            placed.Remove(key);
            squaresCleared++;
        }

        // Shift everything at y >= rows down by 'rows'
        // Move using a snapshot to avoid key-collision while iterating.
        var snapshot = new List<KeyValuePair<Vector2Int, RectTransform>>(placed);
        foreach (var kv in snapshot)
        {
            var from = kv.Key;
            if (from.y >= rows)
            {
                var rt = kv.Value;
                var to = new Vector2Int(from.x, from.y - rows);
                placed.Remove(from);
                placed[to] = rt;
                rt.anchoredPosition = CellToAnchoredPos(to);
            }
        }

        return squaresCleared;
    }

    // simple thin-line overlay
    void DrawGridOverlay()
    {
        // Remove old grid lines by marker, not by name
        if (gridRoot)
        {
            var olds = gridRoot.GetComponentsInChildren<BoardOwned>(true);
            foreach (var m in olds)
            {
                if (m.name.StartsWith("GridLine_")) Destroy(m.gameObject);
            }
        }

        var col = new Color(1, 1, 1, 0.5f);
        for (int x = 0; x <= width; x++)
        {
            var img = new GameObject($"GridLine_V_{x}", typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(gridRoot, false);
            img.color = col;
            img.gameObject.AddComponent<BoardOwned>(); 
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(2f, Mathf.Round(contentSize.y));
            rt.anchoredPosition = new Vector2(Mathf.Round(-contentSize.x * 0.5f + x * cellSize.x), 0f);
            img.name = "GridLine_" + img.name;
        }
        for (int y = 0; y <= height; y++)
        {
            var img = new GameObject($"GridLine_H_{y}", typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(gridRoot, false);
            img.color = col;
            img.gameObject.AddComponent<BoardOwned>(); 
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(Mathf.Round(contentSize.x), 2f);
            rt.anchoredPosition = new Vector2(0f, Mathf.Round(-contentSize.y * 0.5f + y * cellSize.y));
            img.name = "GridLine_" + img.name;
        }
    }

    public void RemoveCellsAndFall(IEnumerable<Vector2Int> toRemove,
                               out List<Vector2Int> removedCells,
                               out int damageFromMonsters,
                               out float specialChargeFromMonsters)
    {
        removedCells = new List<Vector2Int>();
        damageFromMonsters = 0;
        specialChargeFromMonsters = 0f;

        // Remove & tally
        var removeSet = new HashSet<Vector2Int>(toRemove);
        foreach (var key in removeSet)
        {
            if (monsters.TryGetValue(key, out var inst))
            {
                if (inst.data)
                {
                    if (inst.hp > 0f) damageFromMonsters += Mathf.RoundToInt(inst.data.attackPower);
                    specialChargeFromMonsters += inst.data.specialGaugeGain;
                }
                monsters.Remove(key);
            }

            if (placed.TryGetValue(key, out var rt))
            {
                Destroy(rt.gameObject);
                placed.Remove(key);
                removedCells.Add(key);
            }
        }

        // Sparse drop only move tiles down by the count of removed rows below them
        for (int x = 0; x < width; x++)
        {
            // Build a sorted list of removed rows in this column
            var removedRows = new List<int>();
            for (int y = 0; y < height; y++)
            {
                var key = new Vector2Int(x, y);
                if (!placed.ContainsKey(key))
                    continue;
            }

            // We already know which cells were removed; collect their rows for this x:
            foreach (var rc in removedCells)
                if (rc.x == x)
                    removedRows.Add(rc.y);
            if (removedRows.Count == 0)
                continue;
            removedRows.Sort(); // ascending

            var moves = new List<(Vector2Int from, Vector2Int to, RectTransform rt)>();
            var monsterMoves = new List<(Vector2Int from, Vector2Int to, Board.MonsterInstance inst)>();

            for (int y = 0; y < height; y++)
            {
                var from = new Vector2Int(x, y);
                if (!placed.TryGetValue(from, out var rt)) continue;

                int drop = 0;
                for (int i = 0; i < removedRows.Count; i++)
                    if (removedRows[i] < y) drop++; else break;

                if (drop > 0)
                {
                    var to = new Vector2Int(x, y - drop);
                    moves.Add((from, to, rt));
                    if (monsters.TryGetValue(from, out var inst))
                        monsterMoves.Add((from, to, inst));
                }
            }

            // Apply moves
            foreach (var m in moves)
            {
                placed.Remove(m.from);
                placed[m.to] = m.rt;
                m.rt.anchoredPosition = CellToAnchoredPos(m.to);
            }
            foreach (var mm in monsterMoves)
            {
                monsters.Remove(mm.from);
                monsters[mm.to] = mm.inst;
            }
        }

        CleanOrphanedTiles();
    }

    public void SettleAllColumns()
    {
        for (int x = 0; x < width; x++)
        {
            int writeY = 0;
            for (int y = 0; y < height; y++)
            {
                var from = new Vector2Int(x, y);
                if (placed.TryGetValue(from, out var rt))
                {
                    var to = new Vector2Int(x, writeY);
                    if (to != from)
                    {
                        placed.Remove(from);
                        placed[to] = rt;
                        rt.anchoredPosition = CellToAnchoredPos(to);
                    }

                    if (monsters.TryGetValue(from, out var inst))
                    {
                        monsters.Remove(from);
                        monsters[to] = inst;
                    }
                    writeY++;
                }
            }
        }

        CleanOrphanedTiles();
    }

    public void FlashCells(IEnumerable<Vector2Int> cells, Sprite sprite, bool onlyOccupied)
    {
        if (sprite == null) return;
        StartCoroutine(FlashCellsCo(cells, sprite, onlyOccupied));
    }

    System.Collections.IEnumerator FlashCellsCo(IEnumerable<Vector2Int> cells, Sprite sprite, bool onlyOccupied)
    {
        var made = new List<UnityEngine.UI.Image>();
        foreach (var c in cells)
        {
            if (onlyOccupied && !placed.ContainsKey(c)) continue;

            var img = new GameObject("SpecialFlash", typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = new Color(1, 1, 1, 0.9f);
            var rt = img.rectTransform;
            rt.SetParent(gridRoot, false);
            rt.sizeDelta = GetCellSize();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = CellToAnchoredPos(c);
            made.Add(img);
        }

        yield return new WaitForSeconds(0.15f);

        for (int i = 0; i < made.Count; i++)
            if (made[i]) Destroy(made[i].gameObject);
    }

    // Handle resizing at runtime
    void OnRectTransformDimensionsChange()
    {
        if (boardRect == null) boardRect = GetComponent<RectTransform>();
        RecomputeCellMetrics();
        // Reposition any placed tiles
        var toFix = new List<KeyValuePair<Vector2Int, RectTransform>>(placed);
        foreach (var kv in toFix)
            kv.Value.anchoredPosition = CellToAnchoredPos(kv.Key);
        DrawGridOverlay();
    }

    // helper so others can read cell size
    public Vector2 GetCellSize() => cellSize;

    public void SetMonsterAt(Vector2Int cell, MonsterInstance inst)
    {
        monsters[cell] = inst;
    }

    void CleanOrphanedTiles()
    {
        // Any child named "Tile_*" that isn't referenced by `placed` is an orphan ? destroy it.
        var live = new HashSet<RectTransform>(placed.Values);
        for (int i = 0; i < gridRoot.childCount; i++)
        {
            var t = gridRoot.GetChild(i) as RectTransform;
            if (!t) continue;
            if (!t.name.StartsWith("Tile_")) continue; // don't touch grid lines, flashes, etc.
            if (!live.Contains(t)) Destroy(t.gameObject);
        }
    }

    public bool TryGetMonster(Vector2Int cell, out MonsterInstance inst) => monsters.TryGetValue(cell, out inst);
}
