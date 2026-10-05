using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class Board
{
    // Kept outside placed/obstacles: craters block pieces but never count toward
    // full rows, take special damage, or intercept either team's projectiles.
    readonly HashSet<Vector2Int> craterCells = new();
    readonly List<Vector2Int> feelGoodCells = new();
    readonly Dictionary<RectTransform, Color> feelGoodOriginalTints = new();

    LevelModifierSO FeelGoodModifier => _gc && _gc.levelModifierController &&
        _gc.levelModifierController.NewModifierEffectsActive &&
        _gc.levelModifierController.ActiveModifier &&
        _gc.levelModifierController.ActiveModifier.kind == LevelModifierKind.DrFeelGood
            ? _gc.levelModifierController.ActiveModifier : null;

    public bool IsCraterCell(Vector2Int cell) => craterCells.Contains(cell);
    bool PreventEnvironmentOverlap => _gc && _gc.levelModifierController &&
        _gc.levelModifierController.ActiveModifier &&
        _gc.levelModifierController.ActiveModifier.kind == LevelModifierKind.WatchYourStep;
    public void SetCraterCell(Vector2Int cell, bool occupied)
    {
        if (occupied && InBounds(cell)) craterCells.Add(cell);
        else craterCells.Remove(cell);
    }
    public void ClearCraterCells() => craterCells.Clear();

    public RectTransform CraterVisualRoot
    {
        get { EnsureBoardVfxRoot(); return boardVfxRoot; }
    }

    void TickFeelGood()
    {
        var modifier = FeelGoodModifier;
        if (!modifier) return;
        GetMonsterCellsNonAlloc(feelGoodCells, includeDead: true);
        foreach (var cell in feelGoodCells)
        {
            if (!monsters.TryGetValue(cell, out var inst) || inst.feelGoodStacks <= 0) continue;
            if (inst.hp <= 0f)
            {
                inst.feelGoodStacks = 0;
                inst.feelGoodTickTimer = 0f;
                monsters[cell] = inst;
                if (placed.TryGetValue(cell, out var deadTile)) RestoreFeelGoodTint(deadTile);
                continue;
            }
            inst.feelGoodTickTimer += Time.deltaTime;
            float interval = Mathf.Max(0.1f, modifier.feelGoodTickSeconds);
            int ticks = Mathf.FloorToInt(inst.feelGoodTickTimer / interval);
            inst.feelGoodTickTimer -= ticks * interval;
            monsters[cell] = inst;
            if (ticks > 0)
                DamageTile(cell, ticks * inst.feelGoodStacks * Mathf.Max(0.01f, modifier.feelGoodDamagePerStack));
        }
    }

    void LateUpdate()
    {
        // Renderer tint composes with portrait/health flashes without changing
        // the shared sprite, material, or the Image's original foreground color.
        var modifier = FeelGoodModifier;
        if (!modifier) return;
        foreach (var pair in monsters)
        {
            if (pair.Value.feelGoodStacks <= 0 || pair.Value.hp <= 0f ||
                !placed.TryGetValue(pair.Key, out var tile) || !tile) continue;
            var portrait = tile.Find("MonsterPortrait")?.GetComponent<Image>();
            if (!portrait) continue;
            if (!feelGoodOriginalTints.TryGetValue(tile, out var original))
            {
                original = portrait.canvasRenderer.GetColor();
                feelGoodOriginalTints[tile] = original;
            }
            float brightness = Mathf.Clamp01(modifier.feelGoodForegroundBrightness);
            portrait.canvasRenderer.SetColor(new Color(original.r * brightness,
                original.g * brightness, original.b * brightness, original.a));
        }
    }

    void RestoreFeelGoodTint(RectTransform tile)
    {
        if (!tile || !feelGoodOriginalTints.TryGetValue(tile, out var original)) return;
        var portrait = tile.Find("MonsterPortrait")?.GetComponent<Image>();
        if (portrait) portrait.canvasRenderer.SetColor(original);
        feelGoodOriginalTints.Remove(tile);
    }

    public void ClearFeelGood()
    {
        foreach (var pair in feelGoodOriginalTints)
        {
            if (!pair.Key) continue;
            var portrait = pair.Key.Find("MonsterPortrait")?.GetComponent<Image>();
            if (portrait) portrait.canvasRenderer.SetColor(pair.Value);
        }
        feelGoodOriginalTints.Clear();
        GetMonsterCellsNonAlloc(feelGoodCells, includeDead: true);
        foreach (var cell in feelGoodCells)
        {
            var inst = monsters[cell];
            inst.feelGoodStacks = 0;
            inst.feelGoodTickTimer = 0f;
            monsters[cell] = inst;
        }
    }
}
