using System;
using System.Collections.Generic;
using UnityEngine;

// Only durable game data belongs here. Scene objects, coroutines and instance IDs
// are reconstructed by their owning component when the gameplay scene loads.
[Serializable]
public sealed class MarathonMatchSnapshot
{
    public int version = 1;
    public BoardState board;
    public PieceState piece;
    public List<QueuedPiece> queue = new();
    public List<ProjectileState> projectiles = new();
    public BoulderState boulders;
    public UnityEngine.Random.State randomState;
    public float levelTimer, baseFallInterval, castleAttackTimer, bossAbilityTimer, bossNextAbilityAt;
    public float comboTimer, rouletteTimer;
    public int comboCount, maxCombo, obstaclesDestroyed, levelStartMaxLives, levelStartReserveUnits, normalPiecesSinceSpecial;
    public bool pylonShield, specialSiphon;
    public string forcedPiece;
    public int forcedPieceCount;
    public float bossGravityBonus, bossGravityRemaining, doubleStatsRemaining, immunityRemaining;
    public float castleInvulnerabilityRemaining;
    public int slowGravitySource;
    public float slowGravityRemaining, slowGravityMultiplier, slowGravityRampMultiplier;
    public int lastBossAbility;
    public List<BossCooldown> bossCooldowns = new();
    public bool firstFullRowTutorialShown;

    [Serializable] public sealed class BossCooldown { public int ability; public float remaining; }

    public bool IsValid()
    {
        if (version != 1 || board == null || piece == null || queue == null || projectiles == null || boulders == null ||
            board.width < 1 || board.width > 100 || board.height < 1 || board.height > 200 || board.tiles == null || board.floors == null ||
            piece.cells == null || piece.cells.Count == 0 || string.IsNullOrEmpty(piece.asset) || piece.monsters == null ||
            !Finite(levelTimer) || levelTimer < 0 || !Finite(baseFallInterval) || baseFallInterval <= 0 ||
            !Finite(piece.fallTimer) || !Finite(piece.lockTimer) || !Finite(piece.fallInterval) || piece.fallInterval <= 0 ||
            !Finite(castleAttackTimer) || !Finite(bossAbilityTimer) || !Finite(bossNextAbilityAt) ||
            bossCooldowns == null || boulders.rocks == null || boulders.rocks.Count > BoulderController.Capacity) return false;
        var occupied = new HashSet<Vector2Int>();
        foreach (var tile in board.tiles)
            if (tile == null || !InBounds(tile.cell) || !occupied.Add(tile.cell) ||
                (tile.hasMonster && (string.IsNullOrEmpty(tile.monster) || !Finite(tile.stats.hp) || !Finite(tile.stats.maxHp))) ||
                (tile.hasExplosive && (tile.explosive == null || !Finite(tile.explosive.fuse) || tile.explosive.fuse <= 0))) return false;
        var active = new HashSet<Vector2Int>();
        foreach (var cell in piece.cells)
            if (cell.x < 0 || cell.x >= board.width || cell.y < 0 || cell.y > board.height + 8 || occupied.Contains(cell) || !active.Add(cell)) return false;
        var floors = new HashSet<Vector2Int>();
        foreach (var floor in board.floors)
            if (floor == null || !InBounds(floor.cell) || !floors.Add(floor.cell) || !Finite(floor.state.nextTickTime) ||
                !Enum.IsDefined(typeof(Board.FloorEffectType), floor.state.type)) return false;
        foreach (var next in queue)
            if (next == null || string.IsNullOrEmpty(next.asset) || next.monsters == null) return false;
        foreach (var projectile in projectiles)
            if (projectile == null || !Finite(projectile.position) || !Finite(projectile.target) ||
                (projectile.enemy && (projectile.column < 0 || projectile.column >= board.width))) return false;
        foreach (var rock in boulders.rocks)
            if (rock == null || rock.contacts == null || !Finite(rock.position) || !Finite(rock.velocity)) return false;
        foreach (var cooldown in bossCooldowns)
            if (cooldown == null || !Finite(cooldown.remaining)) return false;
        return true;
    }

    bool InBounds(Vector2Int cell) => cell.x >= 0 && cell.x < board.width && cell.y >= 0 && cell.y < board.height;
    static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    static bool Finite(Vector2 value) => Finite(value.x) && Finite(value.y);

    [Serializable] public sealed class BoardState
    {
        public int width, height, nextGroupId;
        public List<TileState> tiles = new();
        public List<FloorState> floors = new();
    }
    [Serializable] public sealed class TileState
    {
        public Vector2Int cell;
        public Color color;
        public string portrait, background, monster;
        public bool hasMonster, hasObstacle, hasExplosive;
        public Board.MonsterInstance stats;
        public Board.ObstacleState obstacle;
        public float healTimer;
        public ExplosiveState explosive;
    }
    [Serializable] public sealed class FloorState
    {
        public Vector2Int cell;
        public Board.FloorEffectState state;
    }
    [Serializable] public sealed class ExplosiveState
    {
        public float elapsed, fuse, pulseRemaining, toggleRemaining;
        public int damage, pulseStage;
        public bool flashOn;
        public string sprite, sound;
    }
    [Serializable] public sealed class PieceState
    {
        public string asset;
        public Vector2Int origin;
        public List<Vector2Int> cells = new();
        public List<string> monsters = new();
        public Color color;
        public float fallTimer, lockTimer, fallInterval, lockDelay;
        public bool hardDropPending;
    }
    [Serializable] public sealed class QueuedPiece
    {
        public string asset;
        public List<string> monsters = new();
    }
    [Serializable] public sealed class ProjectileState
    {
        public bool enemy;
        public Vector2 position, target;
        public string sprite, altSprite, monster;
        public int animation, damage, column;
    }
    [Serializable] public sealed class BoulderState
    {
        public float countdown, elapsed;
        public bool pending;
        public List<RockState> rocks = new();
    }
    [Serializable] public sealed class RockState
    {
        public Vector2 position, velocity;
        public int bounces;
        public List<Vector2Int> contacts = new();
    }
}

// Assets referenced by the gameplay scene and its roster are already loaded.
// Names survive process restarts; Unity instance IDs do not. Sprite keys also
// include their texture name to distinguish identically named sprite slices.
public sealed class MatchSnapshotAssets
{
    readonly Dictionary<string, UnityEngine.Object> assets = new();
    public MatchSnapshotAssets()
    {
        Add(Resources.FindObjectsOfTypeAll<MonsterData>());
        Add(Resources.FindObjectsOfTypeAll<TetrominoData>());
        Add(Resources.FindObjectsOfTypeAll<Sprite>());
        Add(Resources.FindObjectsOfTypeAll<AudioClip>());
    }
    void Add<T>(T[] source) where T : UnityEngine.Object
    {
        foreach (var asset in source) assets[typeof(T).Name + ":" + Key(asset)] = asset;
    }
    public static string Key(UnityEngine.Object asset) => !asset ? "" : asset is Sprite sprite
        ? sprite.texture.name + "/" + sprite.name : asset.name;
    public T Get<T>(string key) where T : UnityEngine.Object => !string.IsNullOrEmpty(key) &&
        assets.TryGetValue(typeof(T).Name + ":" + key, out var asset) ? asset as T : null;
}
