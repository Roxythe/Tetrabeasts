using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class Board
{
    // Kept outside placed/obstacles: craters fill rows and block pieces, but stay
    // fixed during clears/settling and never intercept projectiles.
    readonly HashSet<Vector2Int> craterCells = new();
    readonly List<Vector2Int> feelGoodCells = new();
    readonly Dictionary<RectTransform, Color> feelGoodOriginalTints = new();

    LevelModifierSO FeelGoodModifier => _gc && _gc.levelModifierController &&
        _gc.levelModifierController.NewModifierEffectsActive ? _gc.levelModifierController.GetModifier(LevelModifierKind.DrFeelGood) : null;
    float FeelGoodHealingMultiplier(MonsterInstance unit) =>
        FeelGoodModifier ? Mathf.Clamp01(1f - unit.feelGoodStacks * .01f) : 1f;
    public bool IsCraterCell(Vector2Int cell) => craterCells.Contains(cell);
    bool PreventEnvironmentOverlap => _gc && _gc.levelModifierController && _gc.levelModifierController.HasModifier(LevelModifierKind.WatchYourStep);
    public void SetCraterCell(Vector2Int cell, bool occupied)
    {
        if (occupied && InBounds(cell)) craterCells.Add(cell);
        else craterCells.Remove(cell);
    }
    public void ClearCraterCells() => craterCells.Clear();

    public Sprite FloorWarningSprite(FloorEffectType type) => type switch
    {
        FloorEffectType.Poison => poisonBorderSprite,
        FloorEffectType.Burn => fireBorderSprite,
        FloorEffectType.Spike => spikeSpriteHigh,
        FloorEffectType.Lightning => lightningBorderSprite,
        FloorEffectType.Teleport => teleportWarningSprite,
        FloorEffectType.ZipPad => zipPadSprite,
        _ => null
    };

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
            float severity = 1f - 1f / (1f + pair.Value.feelGoodStacks * .12f);
            float brightness = Mathf.Lerp(modifier.feelGoodForegroundBrightness, .45f, severity);
            portrait.canvasRenderer.SetColor(new Color(original.r * brightness,
                original.g * brightness * .65f, original.b * brightness, original.a));
            var feedback = tile.GetComponent<FeelGoodDebuffVFX>();
            if (!feedback) feedback = tile.gameObject.AddComponent<FeelGoodDebuffVFX>();
            feedback.Show(pair.Value.feelGoodStacks, severity);
        }
    }

    void RestoreFeelGoodTint(RectTransform tile)
    {
        if (tile && tile.TryGetComponent<FeelGoodDebuffVFX>(out var feedback)) feedback.Hide();
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
            if (pair.Key.TryGetComponent<FeelGoodDebuffVFX>(out var feedback)) feedback.Hide();
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
