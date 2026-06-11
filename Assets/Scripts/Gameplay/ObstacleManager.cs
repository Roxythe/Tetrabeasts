using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    [Header("Auto Spawn")]
    public bool enableAutoSpawns = true;

    [Tooltip("How many random attempts to find an empty cell for each spawn.")]
    public int maxAttemptsPerSpawn = 200;

    [Header("Spawn Bias (Bottom Rows)")]
    [Tooltip("Only spawn obstacles/traps in rows 0 to maxSpawnRowInclusive. Set to 2 to allow rows 0,1,2 only.")]
    [Range(0, 19)]
    public int maxSpawnRowInclusive = 2;

    [Tooltip("Relative weights for rows 0,1,2 (only used when maxSpawnRowInclusive >= 2).")]
    public float row0Weight = 0.65f; // highest chance (bottom row)
    public float row1Weight = 0.25f;
    public float row2Weight = 0.10f; // lowest chance (3rd row)

    [Header("Stone Obstacles")]
    public int stoneBaseCount = 3;
    public int stonePerLevel = 1;
    [Range(1, 5)] public int stoneHitsToBreak = 3;

    [Header("Floor Effects")]
    public int poisonBaseCount = 0;
    public int poisonPerLevel = 0;
    public float poisonTickDamage = 1f;
    public float poisonTickInterval = 1f;
    public int poisonTicks = -1;

    public int fireBaseCount = 0;
    public int firePerLevel = 0;
    public float fireTickDamage = 1f;
    public float fireTickInterval = 1f;
    public int fireTicks = -1;

    public int spikeBaseCount = 0;
    public int spikePerLevel = 0;
    public float spikeOneShotDamage = 8f;

    GameController _gc;
    Board _board;
    int _stoneSpawnRowGrowthBonus;

    public void Initialize(GameController gc, Board board)
    {
        _gc = gc;
        _board = board;
    }

    public void OnLevelStart(int levelIndex)
    {
        if (!enableAutoSpawns) return;
        if (_board == null) return;

        OnLevelStart(levelIndex, null);

        int stones = Mathf.Max(0, stoneBaseCount + stonePerLevel * Mathf.Max(0, levelIndex - 1));
        int poisons = Mathf.Max(0, poisonBaseCount + poisonPerLevel * Mathf.Max(0, levelIndex - 1));
        int fires = Mathf.Max(0, fireBaseCount + firePerLevel * Mathf.Max(0, levelIndex - 1));
        int spikes = Mathf.Max(0, spikeBaseCount + spikePerLevel * Mathf.Max(0, levelIndex - 1));

        SpawnRandomStones(stones);
        SpawnRandomFloorEffect(Board.FloorEffectType.Poison, poisons);
        SpawnRandomFloorEffect(Board.FloorEffectType.Burn, fires);
        SpawnRandomFloorEffect(Board.FloorEffectType.Spike, spikes);
    }

    // Overload that accepts CastleData for optional per-level overrides
    public void OnLevelStart(int levelIndex, CastleData castleData)
    {
        if (!enableAutoSpawns) return;
        if (_board == null) return;

        int stones = Mathf.Max(0, stoneBaseCount + stonePerLevel * Mathf.Max(0, levelIndex - 1));
        int poisons = Mathf.Max(0, poisonBaseCount + poisonPerLevel * Mathf.Max(0, levelIndex - 1));
        int fires = Mathf.Max(0, fireBaseCount + firePerLevel * Mathf.Max(0, levelIndex - 1));
        int spikes = Mathf.Max(0, spikeBaseCount + spikePerLevel * Mathf.Max(0, levelIndex - 1));

        // Optional per-level overrides from CastleData
        if (castleData && castleData.overrideInitialObstacles)
        {
            bool allow = true;
            if (castleData.overridesOnlyForLevel1 && levelIndex != 1) allow = false;

            if (allow)
            {
                if (castleData.overrideStoneCount >= 0) stones = castleData.overrideStoneCount;
                if (castleData.overridePoisonCount >= 0) poisons = castleData.overridePoisonCount;
                if (castleData.overrideFireCount >= 0) fires = castleData.overrideFireCount;
                if (castleData.overrideSpikeCount >= 0) spikes = castleData.overrideSpikeCount;
            }
        }

        SpawnRandomStones(stones);
        SpawnRandomFloorEffect(Board.FloorEffectType.Poison, poisons);
        SpawnRandomFloorEffect(Board.FloorEffectType.Burn, fires);
        SpawnRandomFloorEffect(Board.FloorEffectType.Spike, spikes);
    }

    public bool SpawnStoneAt(Vector2Int cell)
    {
        if (_board == null) return false;
        return _board.TrySpawnStoneObstacle(cell, stoneHitsToBreak);
    }

    public void SetStoneSpawnRowGrowthBonus(int additionalRows)
    {
        _stoneSpawnRowGrowthBonus = Mathf.Max(0, additionalRows);
    }

    public bool SpawnFloorEffectAt(Vector2Int cell, Board.FloorEffectType type)
    {
        if (_board == null) return false;
        if (!_board.InBounds(cell)) return false;

        // Allow floor effects on empty OR occupied cells (effect is independent of blocking)
        switch (type)
        {
            case Board.FloorEffectType.Poison:
                _board.SetFloorEffect(cell, type, GetScaledFloorEffectDamage(poisonTickDamage), poisonTickInterval, poisonTicks);
                return true;

            case Board.FloorEffectType.Burn:
                _board.SetFloorEffect(cell, type, GetScaledFloorEffectDamage(fireTickDamage), fireTickInterval, fireTicks);
                return true;

            case Board.FloorEffectType.Spike:
                _board.SetFloorEffect(cell, type, GetScaledFloorEffectDamage(spikeOneShotDamage), 1f, 1);
                return true;
        }

        return false;
    }

    float GetScaledFloorEffectDamage(float amount)
    {
        return _gc ? _gc.GetScaledFloorEffectDamage(amount) : Mathf.Max(0f, amount);
    }

    void SpawnRandomStones(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!TryGetRandomEmptyCell(out var cell)) break;
            _board.TrySpawnStoneObstacle(cell, stoneHitsToBreak);
        }
    }

    void SpawnRandomFloorEffect(Board.FloorEffectType type, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!TryGetRandomSpawnCellForEffect(out var cell)) break;
            SpawnFloorEffectAt(cell, type);
        }
    }

    bool TryGetRandomEmptyCell(out Vector2Int cell)
    {
        cell = default;
        if (_board == null) return false;

        for (int a = 0; a < maxAttemptsPerSpawn; a++)
        {
            int x = Random.Range(0, _board.width);
            int y = PickSpawnRow(includeStoneGrowth: true);
            var c = new Vector2Int(x, y);
            if (_board.IsFree(c))
            {
                cell = c;
                return true;
            }
        }
        return false;
    }

    bool TryGetRandomSpawnCellForEffect(out Vector2Int cell)
    {
        cell = default;
        if (_board == null) return false;

        for (int a = 0; a < maxAttemptsPerSpawn; a++)
        {
            int x = Random.Range(0, _board.width);
            int y = PickSpawnRow(includeStoneGrowth: false); // Bottom-biased like stones
            var c = new Vector2Int(x, y);

            if (!_board.InBounds(c)) continue; // Skip out of bounds

            if (_board.HasFloorEffect(c)) continue; // Don't stack multiple effects on the same cell

            // Spikes only appear on empty cells
            // if (!_board.IsFree(c)) continue;

            cell = c;
            return true;
        }

        return false;
    }

    int PickSpawnRow(bool includeStoneGrowth)
    {
        if (_board == null) return 0;

        int maxRowLimit = maxSpawnRowInclusive + (includeStoneGrowth ? _stoneSpawnRowGrowthBonus : 0);
        int maxRow = Mathf.Clamp(maxRowLimit, 0, _board.height - 1);

        if (maxRow <= 0) return 0;  // If only one row allowed

        // If only 2 rows allowed, use row0Weight/row1Weight
        if (maxRow == 1)
        {
            float a = Mathf.Max(0f, row0Weight);
            float b = Mathf.Max(0f, row1Weight);
            float sum = a + b;
            if (sum <= 0f) return Random.Range(0, 2);

            float r = Random.value * sum;
            return (r < a) ? 0 : 1;
        }

        float total = 0f;
        for (int row = 0; row <= maxRow; row++)
            total += GetRowWeight(row);

        if (total <= 0f)
            return Random.Range(0, maxRow + 1);

        float roll = Random.value * total;
        for (int row = 0; row <= maxRow; row++)
        {
            roll -= GetRowWeight(row);
            if (roll <= 0f)
                return row;
        }

        return maxRow;
    }

    float GetRowWeight(int row)
    {
        if (row <= 0) return Mathf.Max(0f, row0Weight);
        if (row == 1) return Mathf.Max(0f, row1Weight);
        return Mathf.Max(0f, row2Weight);
    }

    bool TryGetRandomAnyCell(out Vector2Int cell)
    {
        cell = default;
        if (_board == null) return false;

        for (int a = 0; a < maxAttemptsPerSpawn; a++)
        {
            int x = Random.Range(0, _board.width);
            int y = Random.Range(0, _board.height);
            var c = new Vector2Int(x, y);
            if (_board.InBounds(c))
            {
                cell = c;
                return true;
            }
        }
        return false;
    }
}
