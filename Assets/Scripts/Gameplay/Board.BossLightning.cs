using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class Board
{
    readonly Dictionary<Vector2Int, FloorEffectState> bossLightningEffects = new();
    readonly Dictionary<Vector2Int, Image> bossLightningBorders = new();
    readonly List<Vector2Int> bossLightningKeys = new();

    public bool AddBossLightning(Vector2Int cell, float damage, float interval, int ticks)
    {
        if (!InBounds(cell) || IsCraterCell(cell)) return false;
        bossLightningEffects[cell] = FloorEffectState.Lightning(damage, interval, ticks);
        EnsureBorderOverlay(cell, lightningBorderSprite, bossLightningBorders);
        return true;
    }
    void ClearBossLightning(Vector2Int cell)
    {
        bossLightningEffects.Remove(cell);
        if (bossLightningBorders.TryGetValue(cell, out var visual) && visual)
        {
            if (Application.isPlaying) Destroy(visual.gameObject); else DestroyImmediate(visual.gameObject);
        }
        bossLightningBorders.Remove(cell);
    }
    void TickBossLightning(float now)
    {
        bossLightningKeys.Clear(); bossLightningKeys.AddRange(bossLightningEffects.Keys);
        foreach (var cell in bossLightningKeys)
        {
            if (!bossLightningEffects.TryGetValue(cell, out var effect)) continue;
            if (bossLightningBorders.TryGetValue(cell, out var border) && border) border.rectTransform.SetAsLastSibling();
            if (now < effect.nextTickTime) continue;
            TryDamageMonsterFromFloorEffect(cell, FloorEffectType.Lightning, effect.damage);
            if (effect.ticksRemaining > 0) effect.ticksRemaining--;
            if (effect.ticksRemaining == 0) { ClearBossLightning(cell); continue; }
            effect.nextTickTime = now + effect.interval;
            bossLightningEffects[cell] = effect;
        }
    }
    void ClearAllBossLightning()
    {
        foreach (var cell in new List<Vector2Int>(bossLightningEffects.Keys)) ClearBossLightning(cell);
    }
}
