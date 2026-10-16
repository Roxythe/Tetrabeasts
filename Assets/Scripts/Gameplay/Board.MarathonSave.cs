using UnityEngine;
using UnityEngine.UI;

public partial class Board
{
    public MarathonMatchSnapshot.BoardState CaptureMarathonBoard()
    {
        var result = new MarathonMatchSnapshot.BoardState { width = width, height = height, nextGroupId = nextPieceGroupId };
        foreach (var entry in placed)
        {
            var tile = new MarathonMatchSnapshot.TileState { cell = entry.Key, color = Color.white };
            var fill = entry.Value.Find("HealthFill")?.GetComponent<Image>();
            var portrait = entry.Value.Find("MonsterPortrait")?.GetComponent<Image>();
            if (fill) { tile.color = fill.color; tile.background = MatchSnapshotAssets.Key(fill.sprite); }
            if (portrait) tile.portrait = MatchSnapshotAssets.Key(portrait.sprite);
            tile.hasMonster = monsters.TryGetValue(entry.Key, out tile.stats);
            tile.monster = MatchSnapshotAssets.Key(tile.stats.data);
            tile.stats.data = null; // Never persist a Unity instance ID.
            tile.hasObstacle = obstacles.TryGetValue(entry.Key, out tile.obstacle);
            healTimers.TryGetValue(entry.Key, out tile.healTimer);
            tile.hasExplosive = magicExplosives.TryGetValue(entry.Key, out var bomb);
            if (tile.hasExplosive)
                tile.explosive = new MarathonMatchSnapshot.ExplosiveState {
                    elapsed = Time.time - bomb.startTime, fuse = bomb.fuseSeconds, damage = bomb.rowClearBonusDamage,
                    pulseRemaining = bomb.pulseUntil - Time.time, toggleRemaining = bomb.nextToggleAt - Time.time,
                    pulseStage = bomb.pulseStage, flashOn = bomb.flashOn,
                    sprite = MatchSnapshotAssets.Key(bomb.detonateVFXSprite), sound = MatchSnapshotAssets.Key(bomb.detonateSFX) };
            result.tiles.Add(tile);
        }
        foreach (var entry in floorEffects)
        {
            var state = entry.Value;
            state.nextTickTime -= Time.time; // Remaining duration, independent of the next session's clock.
            state.poisonTargetDataId = state.poisonTargetDataId != 0 && monsters.TryGetValue(entry.Key, out var target) &&
                target.data && state.poisonTargetDataId == target.data.GetInstanceID() ? 1 : 0;
            result.floors.Add(new MarathonMatchSnapshot.FloorState { cell = entry.Key, state = state });
        }
        return result;
    }

    public void RestoreMarathonBoard(MarathonMatchSnapshot.BoardState state, MatchSnapshotAssets assets)
    {
        ClearAll();
        SetGridSize(state.width, state.height);
        RecomputeCellMetrics();
        nextPieceGroupId = state.nextGroupId;
        foreach (var tile in state.tiles)
        {
            var rt = InstantiateTileUI(tile.color, assets.Get<Sprite>(tile.portrait), assets.Get<Sprite>(tile.background));
            rt.anchoredPosition = CellToAnchoredPos(tile.cell);
            Place(tile.cell, rt);
            if (tile.hasMonster)
            {
                var stats = tile.stats;
                stats.data = assets.Get<MonsterData>(tile.monster);
                // SetMonsterAt recalculates stats and revives zero-HP tiles; a snapshot
                // must retain dead tiles and temporary health/stat multipliers exactly.
                monsters[tile.cell] = stats;
                healTimers[tile.cell] = tile.healTimer;
                UpdateTileHPVisual(tile.cell, stats.hp, stats.maxHp);
            }
            if (tile.hasObstacle)
            {
                obstacles[tile.cell] = tile.obstacle;
                UpdateStoneObstacleVisual(tile.cell);
            }
            if (tile.hasExplosive && tile.explosive != null)
            {
                var bomb = tile.explosive;
                ConfigureMagicExplosiveVisual(rt);
                magicExplosives[tile.cell] = new MagicExplosiveState {
                    startTime = Time.time - bomb.elapsed, fuseSeconds = bomb.fuse, rowClearBonusDamage = bomb.damage,
                    pulseUntil = Time.time + bomb.pulseRemaining, nextToggleAt = Time.time + bomb.toggleRemaining,
                    pulseStage = bomb.pulseStage, flashOn = bomb.flashOn,
                    detonateVFXSprite = assets.Get<Sprite>(bomb.sprite), detonateSFX = assets.Get<AudioClip>(bomb.sound) };
            }
        }
        foreach (var floor in state.floors)
        {
            var fx = floor.state;
            SetFloorEffect(floor.cell, fx.type, fx.damage, fx.interval, fx.ticksRemaining);
            fx.nextTickTime += Time.time;
            if (fx.poisonTargetDataId != 0 && monsters.TryGetValue(floor.cell, out var target) && target.data)
                fx.poisonTargetDataId = target.data.GetInstanceID();
            floorEffects[floor.cell] = fx;
        }
        RefreshAllTileBorders();
    }
}
