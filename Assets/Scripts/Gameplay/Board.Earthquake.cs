using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    [Header("Earthquake ripple")]
    [Min(0.01f)] public float earthquakeBounceDuration = .28f;
    [Min(0f)] public float earthquakeColumnDelay = .055f;
    [Range(0f, 1f)] public float earthquakeBounceHeightInCells = .4f;
    [Min(1f)] public float earthquakeFallAccelerationInCells = 55f;
    public bool IsEarthquakeAnimating { get; private set; }
    public bool IsColumnSettling { get; private set; }
    int earthquakeGeneration;

    sealed class EarthquakeTile
    {
        public RectTransform rect;
        public Vector2Int cell;
        public Vector2 origin;
    }

    // Resolve occupancy once, then let each visual accelerate into its final cell.
    // The row-clear caller waits here before finding the next cascade or spawning a piece.
    IEnumerator AnimateRowClearSettle(Dictionary<int, List<int>> filledColumnsByRow, System.Func<float> deltaTime)
        => AnimateColumnSettle(filledColumnsByRow, deltaTime, null);

    public IEnumerator SettleAfterSpecial(IEnumerable<Vector2Int> affectedCells)
    {
        var columns = new HashSet<int>();
        foreach (var cell in affectedCells) columns.Add(cell.x);
        yield return AnimateColumnSettle(null, () => Time.deltaTime, columns);
    }

    IEnumerator AnimateColumnSettle(Dictionary<int, List<int>> filledColumnsByRow, System.Func<float> deltaTime, HashSet<int> columns)
    {
        int generation = earthquakeGeneration;
        var origins = new Dictionary<RectTransform, Vector2>();
        foreach (var pair in placed)
            if (pair.Value) origins[pair.Value] = pair.Value.anchoredPosition;
        for (int x = 0; x < width; x++)
            if (columns == null || columns.Contains(x)) SettleColumn(x, false, filledColumnsByRow);
        var falling = new List<EarthquakeTile>();
        foreach (var pair in placed)
            if (pair.Value && origins.TryGetValue(pair.Value, out var origin) && origin.y > CellToAnchoredPos(pair.Key).y + .01f)
                falling.Add(new EarthquakeTile { rect = pair.Value, cell = pair.Key, origin = origin });
        IsColumnSettling = falling.Count > 0;
        float acceleration = Mathf.Max(1f, earthquakeFallAccelerationInCells) * GetCellSize().y;
        float elapsed = 0;
        try
        {
            foreach (var tile in falling) tile.rect.anchoredPosition = tile.origin;
            while (IsColumnSettling && generation == earthquakeGeneration)
            {
                if (_gc && _gc.IsPaused) { yield return null; continue; }
                elapsed += deltaTime();
                bool landed = true;
                foreach (var tile in falling)
                {
                    if (!tile.rect || !placed.TryGetValue(tile.cell, out var current) || current != tile.rect) continue;
                    Vector2 destination = CellToAnchoredPos(tile.cell);
                    float y = Mathf.Max(destination.y, tile.origin.y - .5f * acceleration * elapsed * elapsed);
                    tile.rect.anchoredPosition = new Vector2(destination.x, y);
                    if (y > destination.y) landed = false;
                }
                if (landed) break;
                yield return null;
            }
        }
        finally
        {
            if (generation == earthquakeGeneration)
            {
                foreach (var tile in falling)
                    if (tile.rect && placed.TryGetValue(tile.cell, out var current) && current == tile.rect)
                        tile.rect.anchoredPosition = CellToAnchoredPos(tile.cell);
                IsColumnSettling = false;
                RefreshAllTileBorders();
                CleanOrphanedTiles();
                ActivateTeleportFloorEffectsForSettledMonsters();
            }
        }
    }

    public IEnumerator PlayEarthquakeRipple(int epicenterColumn)
        => AnimateEarthquakeRipple(epicenterColumn, () => Time.unscaledDeltaTime);

    IEnumerator AnimateEarthquakeRipple(int epicenterColumn, System.Func<float> deltaTime)
    {
        if (IsEarthquakeAnimating) yield break;
        int generation = ++earthquakeGeneration;
        epicenterColumn = Mathf.Clamp(epicenterColumn, 0, width - 1);
        var columns = new Dictionary<int, List<EarthquakeTile>>();
        var byVisual = new Dictionary<RectTransform, EarthquakeTile>();
        foreach (var tile in placed)
        {
            if (!tile.Value || IsCraterCell(tile.Key)) continue;
            if (!columns.TryGetValue(tile.Key.x, out var column))
                columns[tile.Key.x] = column = new List<EarthquakeTile>();
            var target = new EarthquakeTile { rect = tile.Value, cell = tile.Key, origin = CellToAnchoredPos(tile.Key) };
            column.Add(target); byVisual.Add(tile.Value, target);
        }
        if (columns.Count == 0) yield break;
        IsEarthquakeAnimating = true;
        float liftSeconds = Mathf.Max(.01f, earthquakeBounceDuration * .5f);
        float delay = Mathf.Max(0f, earthquakeColumnDelay);
        float height = GetCellSize().y * earthquakeBounceHeightInCells;
        float acceleration = Mathf.Max(1f, earthquakeFallAccelerationInCells) * GetCellSize().y;
        var settling = new HashSet<int>();
        bool completed = false;
        try
        {
            float elapsed = 0f;
            while (true)
            {
                if (generation != earthquakeGeneration) yield break;
                if (_gc && _gc.IsPaused) { yield return null; continue; }
                elapsed += deltaTime();
                bool allLanded = true;
                foreach (var column in columns)
                {
                    float local = elapsed - Mathf.Abs(column.Key - epicenterColumn) * delay;
                    if (local < 0) { allLanded = false; continue; }
                    if (local >= liftSeconds && settling.Add(column.Key))
                    {
                        // Commit only this column; there is no frame between its lift and descent.
                        SettleColumn(column.Key, true);
                        foreach (var tile in placed)
                            if (tile.Key.x == column.Key && tile.Value && byVisual.TryGetValue(tile.Value, out var target))
                                target.cell = tile.Key;
                    }
                    foreach (var tile in column.Value)
                    {
                        if (!tile.rect || !placed.TryGetValue(tile.cell, out var current) || current != tile.rect) continue;
                        Vector2 destination = CellToAnchoredPos(tile.cell);
                        if (local < liftSeconds)
                        {
                            tile.rect.anchoredPosition = tile.origin + Vector2.up *
                                (Mathf.Sin(local / liftSeconds * Mathf.PI * .5f) * height);
                            allLanded = false;
                        }
                        else
                        {
                            float fallingTime = local - liftSeconds;
                            float y = tile.origin.y + height - .5f * acceleration * fallingTime * fallingTime;
                            tile.rect.anchoredPosition = new Vector2(destination.x, Mathf.Max(destination.y, y));
                            if (y > destination.y) allLanded = false;
                        }
                    }
                }
                if (allLanded) { completed = true; break; }
                yield return null;
            }
        }
        finally
        {
            // Cancellation restores uncommitted columns and completes already committed moves.
            // A cleared/reused board belongs to the new generation and must not be touched.
            if (generation == earthquakeGeneration)
            {
                foreach (var column in columns.Values)
                    foreach (var tile in column)
                        if (tile.rect && placed.TryGetValue(tile.cell, out var current) && current == tile.rect)
                            tile.rect.anchoredPosition = CellToAnchoredPos(tile.cell);
                IsEarthquakeAnimating = false;
                RefreshAllTileBorders();
                CleanOrphanedTiles();
                if (completed) ActivateTeleportFloorEffectsForSettledMonsters();
            }
        }
    }
}
