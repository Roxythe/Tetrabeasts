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

    public bool InBounds(Vector2Int c) =>
        c.x >= 0 && c.x < width && c.y >= 0 && c.y < height;

    public bool IsFree(Vector2Int c) => InBounds(c) && !placed.ContainsKey(c);

    public bool Valid(Vector2Int[] cells)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            var c = cells[i];

            // X must always be inside columns
            if (c.x < 0 || c.x >= width) return false;

            // Above the board is OK (spawn zone)
            if (c.y >= height) continue;

            // Below the board is not OK (floor)
            if (c.y < 0) return false;

            // Inside the board: must be empty
            if (placed.ContainsKey(c)) return false;
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

    public void ClearFullLines(out int rowsCleared, out List<Vector2Int> removedCells)
    {
        rowsCleared = 0;
        removedCells = new List<Vector2Int>();

        for (int y = 0; y < height; y++)
        {
            bool full = true;
            for (int x = 0; x < width; x++)
                if (!placed.ContainsKey(new Vector2Int(x, y))) { full = false; break; }

            if (!full) continue;

            rowsCleared++;

            // mark cells to delete
            for (int x = 0; x < width; x++)
            {
                var key = new Vector2Int(x, y);
                if (placed.TryGetValue(key, out var rt))
                {
                    Destroy(rt.gameObject);
                    placed.Remove(key);
                    removedCells.Add(key);
                }
                monsters.Remove(key); // Remove monster at this cell
            }

            // shift everything above down by 1 (tiles + monsters)
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

            y--; // re-check this row after the drop
        }
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

    public bool TryGetMonster(Vector2Int cell, out MonsterInstance inst) => monsters.TryGetValue(cell, out inst);
}
