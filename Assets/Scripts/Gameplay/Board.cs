using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Board : MonoBehaviour
{
    [Header("Shop Buff Monster Values")]
    public int attackShopBuff;
    public int hpShopBuff;
    public int healPowerShopBuff;

    [Header("Grid Size")]
    public int width = 10;
    public int height = 20;
    public float verticalBufferCells = 0.25f;

    [Header("Refs")]
    public RectTransform gridRoot;
    public Image tilePrefab; 

    [Header("Tile Visuals")]
    public Color normalBorderColor = Color.black;
    public Color immuneBorderColor = new Color(1f, 0.84f, 0f, 1f); // Gold

    [Header("Tetromino Background Pulse")]
    public bool animateTetrominoBackgrounds = true;
    public bool enablePassiveTetrominoBackgroundPulse = true;
    [Tooltip("When disabled, tetromino backgrounds use a solid tinted fill instead of the assigned background sprite.")]
    public bool useTetrominoBackgroundImageTextures = false;
    [Min(0.05f)] public float backgroundPassivePulseInterval = 3.25f;
    [Min(0.01f)] public float backgroundPassivePulseSeconds = 1.45f;
    [Range(0f, 1f)] public float backgroundPassivePeakAlpha = 0.24f;
    [Range(0.001f, 1f)] public float backgroundPassiveStartScale = 0.12f;
    [Min(0.001f)] public float backgroundPassiveEndScale = 1f;
    [Range(0f, 1f)] public float backgroundPulseWhiteBlend = 0f;
    [Range(0f, 1f)] public float rowClearBackgroundWhiteBlend = 0f;
    [Range(0f, 1f)] public float rowClearBackgroundMinSaturation = 1f;
    [Range(1f, 2f)] public float rowClearBackgroundValueBoost = 1.6f;
    [Range(0f, 1f)] public float rowClearBackgroundGlowAlpha = 0.56f;
    [Min(1f)] public float rowClearBackgroundGlowScale = 1f;
    [Min(0f)] public float rowClearBackgroundRippleSeconds = 0.16f;
    [Min(0.01f)] public float rowClearBackgroundPulseRiseSeconds = 0.055f;
    [Min(0f)] public float rowClearBackgroundPeakHoldSeconds = 0.08f;
    [Range(0f, 1f)] public float rowClearBackgroundPeakAlpha = 0.88f;
    [Range(0.001f, 1f)] public float rowClearBackgroundStartScale = 1f;
    [Min(0.001f)] public float rowClearBackgroundPeakScale = 1f;

    public Sprite defaultDeadOverlaySprite; 
    public Color deadOverlayTint = Color.white; 
    public bool deadOverlayPreserveAspect = true;

    [Header("SFX")]
    public AudioClip sfxImmuneHit;

    [Header("Line Clear Timing")]
    public float cascadeClearVisualDelay = 0.125f;

    [Header("Monster Alt Portrait Reactions")]
    public float attackLineClearAltHoldSeconds = 0.06f;
    public float roleAltPortraitFlashSeconds = 0.18f;
    public float attackPassiveAltMinSeconds = 3f;
    public float attackPassiveAltMaxSeconds = 6f;
    public float attackPassiveAltFlashSeconds = 0.18f;
    public bool syncAttackPassiveAltByPiece = false;

    bool tilesImmune = false;
    public enum DamageSource
    {
        Generic,
        CastleProjectile,
        FloorPoison,
        FloorBurn,
        FloorSpike,
        FloorLightning,
        BossAbility,
        MagicExplosive,
        Overgrowth,
        Contagion,
        Rations,
        DeathExplosion,
        RearAmbush,
        VolcanicEruption
    }

    // Runtime
    GameController _gc;
    RectTransform boardRect;  
    Vector2 cellSize;  
    Vector2 contentSize;  
    readonly Transform[,] grid = null; // [x,y] grid of tile transforms
    Dictionary<Vector2Int, RectTransform> placed = new();
    readonly Dictionary<Vector2Int, float> healTimers = new();
    readonly Dictionary<Vector2Int, float> attackPortraitIdleTimers = new();
    readonly Dictionary<int, float> attackPiecePortraitIdleTimers = new();
    readonly List<RectTransform> boardTilePool = new();
    readonly HashSet<RectTransform> pooledBoardTiles = new();
    readonly Dictionary<string, List<Image>> overlayImagePools = new();
    readonly HashSet<Image> pooledOverlayImages = new();
    readonly List<Vector2Int> healTimerKeysScratch = new();
    readonly List<Vector2Int> attackPortraitStaleCellsScratch = new();
    readonly HashSet<int> attackPortraitLiveGroupsScratch = new();
    readonly List<int> attackPortraitStaleGroupsScratch = new();
    readonly Dictionary<int, List<Vector2Int>> attackPortraitCellsByGroupScratch = new();
    readonly List<int> attackPortraitEmptyGroupsScratch = new();
    readonly HashSet<RectTransform> cleanOrphanLiveScratch = new();
    int nextPieceGroupId = 1;

    // ================= Obstacles & Floor Effects =================

    [Header("Obstacle & Trap Sprites")]
    public Sprite stoneUndamagedSprite;      // 0 hits taken
    public Sprite stoneDamagedSprite;        // 1 hit taken
    public Sprite stoneCriticalSprite;       // 2 hits taken

    [Header("Boss Obstacles")]
    public Sprite magicPylonSprite;
    public Sprite magicExplosiveSprite;
    public Sprite bossObstacleBackgroundSprite;

    [Header("Magic Explosive Visuals")]
    public Color explosiveFillColor = new Color(0.25f, 0.65f, 1f, 1f); // starting fill
    public Color explosiveBackColor = new Color(0.86f, 0.86f, 0.86f, 1f); // revealed fuse background
    public Color explosiveFlashColor = new Color(1f, 0.1f, 0.1f, 1f);
    [Range(0f, 1f)] public float explosiveFuseOverlayAlpha = 1f;

    struct MagicExplosiveState
    {
        public float startTime;
        public float fuseSeconds;
        public int rowClearBonusDamage;

        // flash/pulse state
        public int pulseStage;          // 0=none, 1=after 25%, 2=after 50%, 3=after 75%
        public float pulseUntil;        // time to keep pulse visible
        public float nextToggleAt;
        public bool flashOn;
        public Sprite detonateVFXSprite;
        public AudioClip detonateSFX;
    }

    readonly Dictionary<Vector2Int, MagicExplosiveState> magicExplosives = new();

    public Sprite poisonBorderSprite; 
    public Sprite fireBorderSprite;  
    public Sprite lightningBorderSprite;
    public Sprite teleportWarningSprite;
    public Sprite zipPadSprite;
    public AudioClip teleportSFX;
    public AudioClip[] zipPadSFXClips;
    [SerializeField, Range(0.1f, 1f)] float teleportFloorVisualScale = 0.9f;
    [SerializeField, Min(1f)] float teleportFloorRotationDegreesPerSecond = 180f;
    [SerializeField, Min(0)] int teleportFloorDestinationExcludedTopRows = 4;
    [SerializeField, Min(0)] int teleportFloorDestinationExcludedBottomRows = 0;
    [SerializeField, Range(0.1f, 1f)] float zipPadVisualScale = 0.95f;
    [SerializeField, Min(1f)] float zipPadScrollPixelsPerSecond = 140f;
    [SerializeField] Color zipPadBackdropTint = new Color(1f, 0.05f, 0.05f, 0.48f);

    public Sprite spikeSpriteLow;            // Spike frame A (lower)
    public Sprite spikeSpriteHigh;           // Spike frame B (raised)
    public float spikeAnimInterval = 0.35f;  // Seconds per toggle
    [Range(0.1f, 1f)] public float spikeTrapVisualScale = 0.9f;

    [Header("Layer Roots (auto-created if empty)")]
    public RectTransform underlayRoot; // Traps under blocks (spikes)
    public RectTransform overlayRoot;  // Borders above blocks (fire/poison)

    [Header("Special Board VFX")]
    [SerializeField] RectTransform boardVfxRoot;
    [SerializeField] Sprite deathBoardVFXSprite;
    [SerializeField] Sprite bombBoardVFXSprite;
    [SerializeField] Sprite boltBoardVFXSprite;
    [SerializeField] Sprite earthquakeBoardVFXSprite;
    [SerializeField] Sprite slowGravityBoardVFXSprite;
    [SerializeField, Range(0f, 1f)] float boardVfxMaxAlpha = 0.55f;
    [SerializeField, Min(0f)] float boardVfxFadeInSeconds = 0.35f;
    [SerializeField, Min(0f)] float boardVfxHoldSeconds = 3f;
    [SerializeField, Min(0f)] float boardVfxFadeOutSeconds = 0.45f;
    [SerializeField, Min(0f)] float slowGravityBoardVfxFadeInSeconds = 0.18f;

    [Header("Monster Tick Damage Flash")]
    public float monsterTickFlashDuration = 0.08f;
    public Color burnTickFlashTint = new Color(1f, 0.25f, 0.25f, 1f);       // red
    public Color poisonTickFlashTint = new Color(0.75f, 0.25f, 1f, 1f);     // purple
    public Color lightningTickFlashTint = new Color(0.25f, 0.25f, 1f, 1f);  // blue
    public Color contagionTickFlashTint = new Color(1f, 0.10f, 0.65f, 1f);  // hot pink

    [Header("Floor Effect Underlay Tints (only visible when cell is empty)")]
    [Range(0f, 1f)] public float floorUnderlayAlpha = 0.30f;
    [Range(0f, 1f)] public float floorSecondaryUnderlayAlpha = 0.18f;
    public Color poisonUnderlayTint = new Color(0.75f, 0.25f, 1f, 1f);       // purple
    public Color fireUnderlayTint = new Color(1f, 0.55f, 0.15f, 1f);         // orange
    public Color lightningUnderlayTint = new Color(0.25f, 0.95f, 1f, 1f);    // cyan
    public Color contagionSecondaryUnderlayTint = new Color(1f, 0.10f, 0.65f, 1f); // hot pink

    [Header("Poison Floor Damage Ramp")]
    [SerializeField, Range(0f, 0.5f)] float poisonDamageRampPerTick = 0.05f;
    [SerializeField, Range(1f, 4f)] float poisonMaxDamageMultiplier = 2f;

    readonly Dictionary<Vector2Int, Coroutine> _monsterFlashCo = new();
    readonly Dictionary<Vector2Int, Coroutine> _portraitAltSwapCo = new();
    static Sprite s_lastTetrominoBackgroundSprite;

    public enum ObstacleType { Stone, MagicPylon, MagicExplosive, SoldierWall, Overgrowth, HardenedLava }

    [System.Serializable]
    public struct ObstacleState
    {
        public ObstacleType type;
        public int hitsRemaining; // Remaining hits until break
        public int maxHits;       // Used to compute hits taken for sprite state
        public bool indestructible;

        public ObstacleState(ObstacleType t, int hits, bool isIndestructible = false)
        {
            type = t;
            maxHits = Mathf.Max(1, hits);
            hitsRemaining = maxHits;
            indestructible = isIndestructible;
        }

        public int HitsTaken => Mathf.Clamp(maxHits - hitsRemaining, 0, maxHits);
    }

    readonly Dictionary<Vector2Int, ObstacleState> obstacles = new();

    public event Action<Vector2Int, ObstacleType> ObstacleDestroyed;

    public enum FloorEffectType { Poison, Burn, Spike, Lightning, Teleport, ZipPad }

    public struct FloorEffectState
    {
        public FloorEffectType type;
        public float damage;        // poison/burn tick damage OR spike one-shot damage
        public float interval;      // poison/burn tick interval
        public int ticksRemaining;  // -1 = infinite
        public float nextTickTime;
        public int poisonTargetDataId;
        public int poisonTargetPieceGroupId;
        public int poisonTicksAppliedToTarget;

        public static FloorEffectState Poison(float dmg, float interval, int ticks)
        {
            return new FloorEffectState
            {
                type = FloorEffectType.Poison,
                damage = Mathf.Max(0f, dmg),
                interval = Mathf.Max(0.05f, interval),
                ticksRemaining = ticks,
                nextTickTime = Time.time + Mathf.Max(0.05f, interval),
                poisonTargetDataId = 0,
                poisonTargetPieceGroupId = 0,
                poisonTicksAppliedToTarget = 0
            };
        }

        public static FloorEffectState Burn(float dmg, float interval, int ticks)
        {
            return new FloorEffectState
            {
                type = FloorEffectType.Burn,
                damage = Mathf.Max(0f, dmg),
                interval = Mathf.Max(0.05f, interval),
                ticksRemaining = ticks,
                nextTickTime = Time.time + Mathf.Max(0.05f, interval)
            };
        }

        // One-shot on placement (interval unused)
        public static FloorEffectState Spike(float oneShotDamage)
        {
            return new FloorEffectState
            {
                type = FloorEffectType.Spike,
                damage = Mathf.Max(0f, oneShotDamage),
                interval = 1f,
                ticksRemaining = 1,
                nextTickTime = 0f
            };
        }

        public static FloorEffectState Lightning(float dmg, float interval, int ticks)
        {
            return new FloorEffectState
            {
                type = FloorEffectType.Lightning,
                damage = Mathf.Max(0f, dmg),
                interval = Mathf.Max(0.05f, interval),
                ticksRemaining = ticks,
                nextTickTime = Time.time + Mathf.Max(0.05f, interval)
            };
        }

        public static FloorEffectState Teleport()
        {
            return new FloorEffectState
            {
                type = FloorEffectType.Teleport,
                damage = 0f,
                interval = 1f,
                ticksRemaining = -1,
                nextTickTime = 0f
            };
        }

        public static FloorEffectState ZipPad()
        {
            return new FloorEffectState
            {
                type = FloorEffectType.ZipPad,
                damage = 0f,
                interval = 1f,
                ticksRemaining = -1,
                nextTickTime = 0f
            };
        }
    }

    readonly Dictionary<Vector2Int, FloorEffectState> floorEffects = new();
    readonly List<Vector2Int> floorEffectKeysScratch = new();
    readonly List<Vector2Int> teleportDestinationScratch = new();
    readonly List<Vector2Int> teleportSafeDestinationScratch = new();
    readonly List<Vector2Int> floorEffectCellScratch = new();
    readonly List<Vector2Int> settledTeleportFloorEffectScratch = new();
    bool activatingSettledTeleportFloorEffects;

    // visuals
    readonly Dictionary<Vector2Int, Image> poisonBorders = new();
    readonly Dictionary<Vector2Int, Image> fireBorders = new();
    readonly Dictionary<Vector2Int, Image> lightningBorders = new();
    readonly Dictionary<Vector2Int, Image> floorTintUnderlays = new(); // poison/burn/lightning tints
    readonly Dictionary<Vector2Int, Image> floorSecondaryTintUnderlays = new();
    readonly Dictionary<SpecialType, Coroutine> boardVfxRoutines = new();
    readonly Dictionary<SpecialType, Image> boardVfxImages = new();
    Coroutine slowGravityBoardVfxRoutine;
    Image slowGravityBoardVfxImage;

    class SpikeVisual
    {
        public RectTransform root;
        public Image low;
        public Image high;
        public bool highOn;
        public float nextToggleTime;
    }

    readonly Dictionary<Vector2Int, SpikeVisual> spikeVisuals = new();

    class TeleportVisual
    {
        public RectTransform root;
        public Image image;
    }

    readonly Dictionary<Vector2Int, TeleportVisual> teleportVisuals = new();

    class ZipPadVisual
    {
        public RectTransform root;
        public Image backdrop;
        public Image[] arrows;
        public float offset;
    }

    readonly Dictionary<Vector2Int, ZipPadVisual> zipPadVisuals = new();

    public bool HasFloorEffect(Vector2Int cell) => floorEffects.ContainsKey(cell);
    public bool HasFloorEffectOfType(Vector2Int cell, FloorEffectType type) =>
        floorEffects.TryGetValue(cell, out var fx) && fx.type == type;

    // Events
    public event Action<Vector2Int, MonsterData> TileDied; // Event called when a tile's HP reaches 0
    public event Action<Vector2Int, MonsterData, float, DamageSource> TileDamaged;
    public event Action<Vector2Int, MonsterData, MonsterData, float> TileHealed;
    readonly List<RectTransform> _activeBuffPopups = new();

    [System.Serializable]
    public struct MonsterInstance
    {
        public MonsterData data;
        public float hp;

        // Buffed runtime stats computed once from leveled data + shop + run mods
        public float maxHp;
        public float attackPower;       // leveled base attackPower used for damage
        public float attackBonus;       // shop additive
        public float healAmount;        // leveled base heal
        public float healRange;         // leveled base healRange
        public float healSpeed;
        public float specialGaugeGain;  // leveled base specialGain used for gauge
        public int soulLinkGroupId;
        public int pieceGroupId;
        public bool contagionInfected;
        public float contagionPercentPerTick;
        public float contagionTickTimer;
        public float contagionSpreadTimer;

        public MonsterInstance(MonsterData d)
        {
            data = d;

            hp = 0f;
            maxHp = 0f;
            attackPower = 0f;
            attackBonus = 0;
            healAmount = 0f;
            healRange = 0f;
            healSpeed = 0f;
            specialGaugeGain = 0f;
            soulLinkGroupId = 0;
            pieceGroupId = 0;
            contagionInfected = false;
            contagionPercentPerTick = 0f;
            contagionTickTimer = 0f;
            contagionSpreadTimer = 0f;

            RecalcFromData(setHpToMax: true);
        }

        public MonsterInstance(MonsterData d, int groupId) : this(d)
        {
            pieceGroupId = groupId;
        }

        public void RecalcFromData(bool setHpToMax)
        {
            if (!data)
            {
                maxHp = 0f;
                attackPower = 0f;
                attackBonus = 0;
                healAmount = 0f;
                healRange = 0f;
                healSpeed = 0f;
                specialGaugeGain = 0f;
                soulLinkGroupId = 0;
                pieceGroupId = 0;
                contagionInfected = false;
                contagionPercentPerTick = 0f;
                contagionTickTimer = 0f;
                contagionSpreadTimer = 0f;
                if (setHpToMax) hp = 0f;
                return;
            }

            int level = RunMonsterProgress.GetCurrentLevel(data.monsterName);
            var leveled = MonsterLeveling.GetLeveledStats(data, level);

            int hpBonus = ShopBuffStore.GetLevel(ShopBuffType.HpUp) * 5;
            float atkBonus = ShopBuffEffects.MonsterAttackBonus;
            int healBonus = ShopBuffStore.GetLevel(ShopBuffType.HealPower) * 2;

            float runMaxHpMult = RunModsStore.MonsterMaxHpMult;
            float runHealMult = RunModsStore.HealPowerMult;
            int runHealRangeAdd = RunModsStore.HealRangeAdd;

            maxHp = (leveled.maxHealth + hpBonus) * runMaxHpMult;

            attackPower = leveled.attackPower;
            attackBonus = atkBonus;

            healAmount = (leveled.healAmount > 0f) ? ((leveled.healAmount + healBonus) * runHealMult) : 0f;
            healRange = leveled.healRange + runHealRangeAdd;
            healSpeed = leveled.healSpeed;

            specialGaugeGain = leveled.specialGaugeGain;

            if (setHpToMax) hp = maxHp;
            else hp = Mathf.Clamp(hp, 0f, maxHp);
        }
    }

    // Single-pixel white sprite for line drawing and Tile fills
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

    public Sprite ResolveTetrominoFillSprite(Sprite backgroundImage)
    {
        return useTetrominoBackgroundImageTextures && backgroundImage ? backgroundImage : OnePx();
    }

    // ================= Inline Border (per-edge thickness) =================
    const float DEFAULT_BORDER = 2f;

    UnityEngine.UI.Image GetOrCreateEdge(RectTransform rt, string name)
    {
        var t = rt.Find(name);
        if (t)
        {
            var img = t.GetComponent<UnityEngine.UI.Image>();
            if (img) return img;
        }

        var go = new GameObject(name, typeof(UnityEngine.UI.Image));
        go.transform.SetParent(rt, false);
        var edge = go.GetComponent<UnityEngine.UI.Image>();
        edge.sprite = OnePx();
        edge.type = UnityEngine.UI.Image.Type.Simple;
        edge.raycastTarget = false;
        return edge;
    }

    void EnsureInlineBorder(RectTransform rt)
    {
        if (!rt) return;

        // Left
        var L = GetOrCreateEdge(rt, "Edge_L");
        var Lrt = L.rectTransform;
        Lrt.anchorMin = new Vector2(0f, 0f);
        Lrt.anchorMax = new Vector2(0f, 1f);
        Lrt.pivot = new Vector2(0f, 0.5f);
        Lrt.anchoredPosition = Vector2.zero;

        // Right
        var R = GetOrCreateEdge(rt, "Edge_R");
        var Rrt = R.rectTransform;
        Rrt.anchorMin = new Vector2(1f, 0f);
        Rrt.anchorMax = new Vector2(1f, 1f);
        Rrt.pivot = new Vector2(1f, 0.5f);
        Rrt.anchoredPosition = Vector2.zero;

        // Top
        var T = GetOrCreateEdge(rt, "Edge_T");
        var Trt = T.rectTransform;
        Trt.anchorMin = new Vector2(0f, 1f);
        Trt.anchorMax = new Vector2(1f, 1f);
        Trt.pivot = new Vector2(0.5f, 1f);
        Trt.anchoredPosition = Vector2.zero;

        // Bottom
        var B = GetOrCreateEdge(rt, "Edge_B");
        var Brt = B.rectTransform;
        Brt.anchorMin = new Vector2(0f, 0f);
        Brt.anchorMax = new Vector2(1f, 0f);
        Brt.pivot = new Vector2(0.5f, 0f);
        Brt.anchoredPosition = Vector2.zero;

        L.rectTransform.SetAsLastSibling();
        R.rectTransform.SetAsLastSibling();
        T.rectTransform.SetAsLastSibling();
        B.rectTransform.SetAsLastSibling();
    }

    void SetEdgeThickness(RectTransform rt, float left, float right, float top, float bottom)
    {
        var L = rt.Find("Edge_L")?.GetComponent<UnityEngine.UI.Image>();
        var R = rt.Find("Edge_R")?.GetComponent<UnityEngine.UI.Image>();
        var T = rt.Find("Edge_T")?.GetComponent<UnityEngine.UI.Image>();
        var B = rt.Find("Edge_B")?.GetComponent<UnityEngine.UI.Image>();

        if (L) L.rectTransform.sizeDelta = new Vector2(left, 0f);
        if (R) R.rectTransform.sizeDelta = new Vector2(right, 0f);
        if (T) T.rectTransform.sizeDelta = new Vector2(0f, top);
        if (B) B.rectTransform.sizeDelta = new Vector2(0f, bottom);
    }

    public void SetInlineBorderColor(RectTransform rt, Color c)
    {
        if (!rt) return;

        EnsureInlineBorder(rt);

        var L = rt.Find("Edge_L")?.GetComponent<UnityEngine.UI.Image>();
        var R = rt.Find("Edge_R")?.GetComponent<UnityEngine.UI.Image>();
        var T = rt.Find("Edge_T")?.GetComponent<UnityEngine.UI.Image>();
        var B = rt.Find("Edge_B")?.GetComponent<UnityEngine.UI.Image>();

        if (L) L.color = c;
        if (R) R.color = c;
        if (T) T.color = c;
        if (B) B.color = c;
    }

    public void ApplySharedEdges(RectTransform rt, bool leftShared, bool rightShared, bool topShared, bool bottomShared, float border = DEFAULT_BORDER)
    {
        if (!rt) return;

        EnsureInlineBorder(rt);

        float outline = border * 1.0f;
        float full = border + outline;
        float half = full * 0.5f;

        float left = leftShared ? half : full;
        float right = rightShared ? half : full;
        float top = topShared ? half : full;
        float bottom = bottomShared ? half : full;

        SetEdgeThickness(rt, left, right, top, bottom);
    }

    public bool ShouldShareInlineBorderEdge(Vector2Int cell, Vector2Int neighbor)
    {
        if (!InBounds(neighbor) || !placed.ContainsKey(neighbor))
            return false;

        return !obstacles.ContainsKey(cell) && !obstacles.ContainsKey(neighbor);
    }

    public bool IsObstacleCell(Vector2Int cell) => obstacles.ContainsKey(cell);

    public void RefreshTileBordersAt(Vector2Int cell)
    {
        if (!placed.TryGetValue(cell, out var rt) || !rt) return;

        bool leftShared = ShouldShareInlineBorderEdge(cell, cell + Vector2Int.left);
        bool rightShared = ShouldShareInlineBorderEdge(cell, cell + Vector2Int.right);
        bool topShared = ShouldShareInlineBorderEdge(cell, cell + Vector2Int.up);
        bool bottomShared = ShouldShareInlineBorderEdge(cell, cell + Vector2Int.down);

        rt.sizeDelta = GetCellSize();
        rt.anchoredPosition = CellToAnchoredPos(cell);

        ApplySharedEdges(rt, leftShared, rightShared, topShared, bottomShared, DEFAULT_BORDER);
        SetInlineBorderColor(rt, TilesDamageImmune ? immuneBorderColor : normalBorderColor);
    }

    public void RefreshTileBordersAround(Vector2Int cell)
    {
        RefreshTileBordersAt(cell);
        if (InBounds(cell + Vector2Int.left)) RefreshTileBordersAt(cell + Vector2Int.left);
        if (InBounds(cell + Vector2Int.right)) RefreshTileBordersAt(cell + Vector2Int.right);
        if (InBounds(cell + Vector2Int.up)) RefreshTileBordersAt(cell + Vector2Int.up);
        if (InBounds(cell + Vector2Int.down)) RefreshTileBordersAt(cell + Vector2Int.down);
    }

    public void RefreshAllTileBorders()
    {
        foreach (var kv in placed)
        {
            if (!kv.Value) continue;
            RefreshTileBordersAt(kv.Key);
        }
    }

    private readonly Dictionary<Vector2Int, MonsterInstance> monsters = new();

    void Awake()
    {
        boardRect = GetComponent<RectTransform>();
        _gc = FindFirstObjectByType<GameController>();

        // Always isolate spawned tiles/lines under a dedicated child of this Board
        if (!gridRoot || gridRoot.transform.parent != transform)
        {
            var go = new GameObject("GridRoot", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            gridRoot = rt;
        }

        // Ensure layer roots exist + share the same coordinate space as gridRoot
        if (!underlayRoot || underlayRoot.transform.parent != transform)
        {
            var go = new GameObject("UnderlayRoot", typeof(RectTransform));
            underlayRoot = go.GetComponent<RectTransform>();
            underlayRoot.SetParent(transform, false);
        }

        if (!overlayRoot || overlayRoot.transform.parent != transform)
        {
            var go = new GameObject("OverlayRoot", typeof(RectTransform));
            overlayRoot = go.GetComponent<RectTransform>();
            overlayRoot.SetParent(transform, false);
        }

        // Stretch these roots to fill the board rect
        void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        Stretch(underlayRoot);
        Stretch(gridRoot);
        Stretch(overlayRoot);

        EnsureBoardVfxRoot();
        MaintainBoardVfxLayering();
    }

    void Start()
    {
        RecomputeCellMetrics();
        ConfigureBoardVfxRoot();
        DrawGridOverlay();
    }

    void Update()
    {
        MaintainBoardVfxLayering();

        if (_gc && !_gc.IsRoundActive) return; // Nothing should tick outside active rounds

        TickFloorEffects();
        AnimateSpikes();
        AnimateTeleportFloorEffects();
        AnimateZipPads();
        TickMagicExplosives();
        RefreshAllTintUnderlays();
        TickAttackPortraitIdleSwaps();

        // ================== Healers pulse over time ==================
        if (monsters.Count == 0 || healTimers.Count == 0) return;

        float healMult = (_gc != null) ? _gc.healPowerMult : 1f;
        int rangeAdd = (_gc != null) ? _gc.healRangeAdd : 0;

        healTimerKeysScratch.Clear();
        foreach (var key in healTimers.Keys)
            healTimerKeysScratch.Add(key);

        foreach (var k in healTimerKeysScratch)
        {
            if (!monsters.TryGetValue(k, out var inst) || inst.data == null)
            { healTimers.Remove(k); continue; }

            if (inst.hp <= 0f)
            { healTimers.Remove(k); continue; }

            var md = inst.data;
            if (inst.healAmount <= 0f || inst.healSpeed <= 0f)
            { healTimers.Remove(k); continue; }

            int finalHeal = Mathf.RoundToInt(inst.healAmount * healMult);
            float interval = inst.healSpeed;

            // Accumulate timer for this healer
            float t = 0f;
            healTimers.TryGetValue(k, out t);
            t += Time.deltaTime;

            if (t < interval)
            {
                healTimers[k] = t;
                continue;
            }

            healTimers[k] = 0f; // Consume one tick
            FlashMonsterAltPortrait(k, MonsterRole.Healer);

            int range = Mathf.Clamp(Mathf.RoundToInt(inst.healRange + rangeAdd), 0, 99);
            Vector2Int? best = null;
            int bestDist = int.MaxValue;
            float bestFrac = 1f; // Lower is better

            for (int y = Mathf.Max(0, k.y - range); y <= Mathf.Min(height - 1, k.y + range); y++)
                for (int x = Mathf.Max(0, k.x - range); x <= Mathf.Min(width - 1, k.x + range); x++)
                {
                    var c = new Vector2Int(x, y);
                    if (c == k) continue;

                    if (!monsters.TryGetValue(c, out var ally) || ally.data == null) continue;
                    if (ally.hp <= 0f) continue;                          // Cannot heal dead
                    if (ally.hp >= ally.data.maxHealth) continue;         // Already full

                    int d = Mathf.Abs(c.x - k.x) + Mathf.Abs(c.y - k.y);
                    if (d > range) continue;

                    float frac = ally.hp / Mathf.Max(1f, ally.data.maxHealth);

                    // Pick nearest, then lowest hp fraction
                    bool take = (d < bestDist) || (d == bestDist && frac < bestFrac);
                    if (take) { best = c; bestDist = d; bestFrac = frac; }
                }

            if (best.HasValue)
            {
                HealTile(best.Value, finalHeal, md);
            }
        }
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
        return InstantiateTileUIBase(color, null);
    }

    RectTransform InstantiateTileUIBase(Color color, Sprite backgroundImage)
    {
        var rt = GetOrCreateBoardTile();

        rt.SetParent(gridRoot, false);
        rt.gameObject.SetActive(true);
        rt.name = $"Tile_{GetInstanceID()}";
        rt.sizeDelta = cellSize;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        var baseImg = rt.GetComponent<UnityEngine.UI.Image>();
        baseImg.sprite = null;
        baseImg.raycastTarget = false;
        baseImg.color = new Color(0f, 0f, 0f, 0f);

        SetInlineBorderColor(rt, TilesDamageImmune ? immuneBorderColor : normalBorderColor);

        // Back (gray)
        var back = GetOrCreateTileChildImage(rt, "BackFill");
        back.gameObject.SetActive(true);
        var backRT = back.rectTransform;
        backRT.anchorMin = backRT.anchorMax = new Vector2(0.5f, 0.5f);
        backRT.sizeDelta = rt.sizeDelta;   // Fill the whole tile
        backRT.anchoredPosition = Vector2.zero;
        backRT.localScale = Vector3.one;
        back.sprite = OnePx(); // Simple 1x1 white pixel
        back.type = UnityEngine.UI.Image.Type.Simple;
        back.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        back.raycastTarget = false;

        // HealthFill
        var fill = GetOrCreateTileChildImage(rt, "HealthFill");
        fill.gameObject.SetActive(true);
        var frt = fill.rectTransform;
        frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.sizeDelta = rt.sizeDelta;      // Fill the whole tile
        frt.anchoredPosition = Vector2.zero;
        frt.localScale = Vector3.one;
        fill.sprite = ResolveTetrominoFillSprite(backgroundImage);
        fill.preserveAspect = false;
        fill.type = UnityEngine.UI.Image.Type.Filled;
        fill.fillMethod = UnityEngine.UI.Image.FillMethod.Vertical;
        fill.fillOrigin = (int)UnityEngine.UI.Image.OriginVertical.Bottom;
        fill.fillAmount = 1f;
        fill.color = color;
        fill.raycastTarget = false;
        ConfigureTetrominoBackgroundPulse(fill, color, backgroundImage);

        HideTileChild(rt, "MonsterPortrait");
        ResetDeadOverlay(rt, frt.sizeDelta);

        ApplySharedEdges(rt, false, false, false, false, DEFAULT_BORDER);
        SetInlineBorderColor(rt, TilesDamageImmune ? immuneBorderColor : normalBorderColor);
        frt.SetAsFirstSibling();
        backRT.SetAsFirstSibling();

        return rt;
    }

    public void ConfigureTetrominoBackgroundPulse(
        Image fill,
        Color tileColor,
        Sprite backgroundImage,
        int offsetSeed = 0,
        bool useSiblingPulseLayers = true)
    {
        if (!fill)
            return;

        if (backgroundImage)
            s_lastTetrominoBackgroundSprite = backgroundImage;

        var pulse = fill.GetComponent<TetrominoBackgroundPulse>();
        bool shouldAnimate = animateTetrominoBackgrounds && backgroundImage != null;

        if (!shouldAnimate)
        {
            if (pulse)
                pulse.ResetPulseVisuals();
            return;
        }

        if (!pulse)
            pulse = fill.gameObject.AddComponent<TetrominoBackgroundPulse>();

        float offset = Mathf.Repeat(offsetSeed * 0.371f, Mathf.Max(0.05f, backgroundPassivePulseInterval));
        pulse.Configure(
            tileColor,
            enablePassiveTetrominoBackgroundPulse,
            backgroundPassivePulseInterval,
            backgroundPassivePulseSeconds,
            backgroundPassivePeakAlpha,
            backgroundPassiveStartScale,
            backgroundPassiveEndScale,
            backgroundPulseWhiteBlend,
            rowClearBackgroundWhiteBlend,
            rowClearBackgroundMinSaturation,
            rowClearBackgroundValueBoost,
            rowClearBackgroundGlowAlpha,
            rowClearBackgroundGlowScale,
            useSiblingPulseLayers,
            offset);
    }

    public RectTransform InstantiateTileUI(Color color, Sprite portrait, float inset = 4f)
    {
        return InstantiateTileUI(color, portrait, null, inset);
    }

    public RectTransform InstantiateTileUI(Color color, Sprite portrait, Sprite backgroundImage, float inset = 4f, float portraitScale = 1f)
    {
        var rt = InstantiateTileUIBase(color, backgroundImage);
        if (portrait)
        {
            var img = GetOrCreateTileChildImage(rt, "MonsterPortrait");
            img.gameObject.SetActive(true);
            img.sprite = portrait;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var prt = img.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);

            // Slight inset so it sits inside the inline border
            prt.sizeDelta = ((RectTransform)rt.Find("HealthFill")).sizeDelta - new Vector2(2f, 2f);
            prt.localScale = Vector3.one * Mathf.Max(0f, portraitScale);
            prt.anchoredPosition = Vector2.zero;

            prt.SetAsLastSibling(); // Ensure the portrait renders on top
        }

        {
            // Add a DeadOverlay layer 
            var fillRT = (RectTransform)rt.Find("HealthFill");
            if (fillRT)
                ResetDeadOverlay(rt, fillRT.sizeDelta);
        }

        return rt;
    }

    public void ReleaseTransientTileUI(RectTransform rt)
    {
        ReleaseBoardTile(rt);
    }

    RectTransform GetOrCreateBoardTile()
    {
        RectTransform rt = null;
        while (boardTilePool.Count > 0 && !rt)
        {
            int last = boardTilePool.Count - 1;
            rt = boardTilePool[last];
            boardTilePool.RemoveAt(last);
            pooledBoardTiles.Remove(rt);
        }

        if (rt)
            return rt;

        rt = new GameObject($"Tile_{GetInstanceID()}",
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image), typeof(BoardOwned))
            .GetComponent<RectTransform>();

        return rt;
    }

    Image GetOrCreateTileChildImage(RectTransform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child && child.TryGetComponent(out Image existing))
            return existing;

        var go = new GameObject(childName, typeof(Image));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    void HideTileChild(RectTransform parent, string childName)
    {
        var child = parent ? parent.Find(childName) : null;
        if (!child)
            return;

        if (child.TryGetComponent(out Image img))
            img.sprite = null;

        child.gameObject.SetActive(false);
    }

    Image GetOrCreateOverlayImage(string objectName, RectTransform parent)
    {
        if (!overlayImagePools.TryGetValue(objectName, out var pool))
        {
            pool = new List<Image>();
            overlayImagePools[objectName] = pool;
        }

        Image img = null;
        while (pool.Count > 0 && !img)
        {
            int last = pool.Count - 1;
            img = pool[last];
            pool.RemoveAt(last);
            pooledOverlayImages.Remove(img);
        }

        if (!img)
        {
            img = new GameObject(objectName, typeof(Image)).GetComponent<Image>();
            img.raycastTarget = false;
        }

        img.name = objectName;
        img.enabled = true;
        img.raycastTarget = false;
        img.sprite = null;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.color = Color.white;
        img.transform.SetParent(parent, false);
        img.gameObject.SetActive(true);

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchoredPosition = Vector2.zero;
        rt.SetAsLastSibling();

        return img;
    }

    void ReleaseOverlayImage(Image img)
    {
        if (!img || pooledOverlayImages.Contains(img))
            return;

        string objectName = img.name;
        img.sprite = null;
        img.enabled = true;
        img.color = Color.white;
        img.rectTransform.localRotation = Quaternion.identity;
        img.rectTransform.localScale = Vector3.one;
        img.gameObject.SetActive(false);

        if (!overlayImagePools.TryGetValue(objectName, out var pool))
        {
            pool = new List<Image>();
            overlayImagePools[objectName] = pool;
        }

        pooledOverlayImages.Add(img);
        pool.Add(img);
    }

    void ResetDeadOverlay(RectTransform parent, Vector2 size)
    {
        var deadImg = GetOrCreateTileChildImage(parent, "DeadOverlay");
        deadImg.raycastTarget = false;
        deadImg.sprite = defaultDeadOverlaySprite;
        deadImg.color = deadOverlayTint;
        deadImg.preserveAspect = deadOverlayPreserveAspect;
        deadImg.type = Image.Type.Simple;

        var drt = deadImg.rectTransform;
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.sizeDelta = size;
        drt.anchoredPosition = Vector2.zero;
        drt.localScale = Vector3.one;
        drt.gameObject.SetActive(false);
        drt.SetAsLastSibling();
    }

    void ReleaseBoardTile(RectTransform rt)
    {
        if (!rt || pooledBoardTiles.Contains(rt))
            return;

        rt.gameObject.SetActive(false);
        rt.SetParent(gridRoot, false);
        ResetTilePulseChild(rt, "HealthFill");
        HideTileChild(rt, "MonsterPortrait");
        HideTileChild(rt, "DeadOverlay");
        pooledBoardTiles.Add(rt);
        boardTilePool.Add(rt);
    }

    void ResetTilePulseChild(RectTransform parent, string childName)
    {
        var child = parent ? parent.Find(childName) : null;
        var pulse = child ? child.GetComponent<TetrominoBackgroundPulse>() : null;
        if (pulse)
            pulse.ResetPulseVisuals();
    }

    void UpdateTileHPVisual(Vector2Int cell, float current, float max)
    {
        if (!placed.TryGetValue(cell, out var rt)) return;
        var fill = rt.Find("HealthFill") as RectTransform;
        if (!fill) return;

        var img = fill.GetComponent<UnityEngine.UI.Image>();
        float amt = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);
        img.fillAmount = amt; // Depleted area shows the gray base underneath

        var pulse = fill.GetComponent<TetrominoBackgroundPulse>();
        if (pulse)
            pulse.SetPassiveEnabled(current > 0f && animateTetrominoBackgrounds && enablePassiveTetrominoBackgroundPulse);

        // Show/hide the dead overlay
        var dead = rt.Find("DeadOverlay");
        if (dead) dead.gameObject.SetActive(current <= 0f);
    }

    void FlashMonsterAltPortrait(Vector2Int cell, MonsterRole requiredRole)
    {
        FlashMonsterAltPortrait(cell, requiredRole, roleAltPortraitFlashSeconds);
    }

    void FlashMonsterAltPortrait(Vector2Int cell, MonsterRole requiredRole, float restoreSeconds)
    {
        if (!TrySetMonsterAltPortrait(cell, requiredRole, out var monster, out var tile))
            return;

        _portraitAltSwapCo[cell] = StartCoroutine(CoRestoreMonsterPortrait(cell, monster, tile, restoreSeconds));
    }

    bool TrySetMonsterAltPortrait(Vector2Int cell, MonsterRole requiredRole, out MonsterData monster, out RectTransform tile)
    {
        return TrySetMonsterAltPortrait(cell, (MonsterRole?)requiredRole, out monster, out tile);
    }

    bool TrySetMonsterAltPortrait(Vector2Int cell, out MonsterData monster, out RectTransform tile)
    {
        return TrySetMonsterAltPortrait(cell, null, out monster, out tile);
    }

    bool TrySetMonsterAltPortrait(Vector2Int cell, MonsterRole? requiredRole, out MonsterData monster, out RectTransform tile)
    {
        monster = null;
        tile = null;

        if (!monsters.TryGetValue(cell, out var inst) || !inst.data || inst.hp <= 0f)
            return false;

        if (requiredRole.HasValue && inst.data.role != requiredRole.Value)
            return false;

        if (!placed.TryGetValue(cell, out tile) || !tile)
            return false;

        var portrait = tile.Find("MonsterPortrait")?.GetComponent<Image>();
        if (!portrait)
            return false;

        var alt = GetCurrentMonsterAltPortrait(inst.data);
        if (!alt)
            return false;

        StopPortraitAltSwap(cell, restoreNormal: true);

        monster = inst.data;
        portrait.sprite = alt;
        return true;
    }

    IEnumerator CoRestoreMonsterPortrait(Vector2Int cell, MonsterData monster, RectTransform tile, float restoreSeconds)
    {
        float seconds = Mathf.Max(0.01f, restoreSeconds);
        yield return new WaitForSecondsRealtime(seconds);

        RestoreMonsterPortrait(cell, monster, tile);
        _portraitAltSwapCo.Remove(cell);
    }

    void StopPortraitAltSwap(Vector2Int cell, bool restoreNormal)
    {
        if (_portraitAltSwapCo.TryGetValue(cell, out var running) && running != null)
            StopCoroutine(running);

        _portraitAltSwapCo.Remove(cell);
        attackPortraitIdleTimers.Remove(cell);

        if (restoreNormal && monsters.TryGetValue(cell, out var inst))
            RestoreMonsterPortrait(cell, inst.data, placed.TryGetValue(cell, out var rt) ? rt : null);
    }

    void StopAllPortraitAltSwaps(bool restoreNormal)
    {
        var cells = new List<Vector2Int>(_portraitAltSwapCo.Keys);
        for (int i = 0; i < cells.Count; i++)
            StopPortraitAltSwap(cells[i], restoreNormal);

        _portraitAltSwapCo.Clear();
    }

    void RestoreMonsterPortrait(Vector2Int cell, MonsterData monster, RectTransform tile)
    {
        if (!monster || !tile)
            return;

        if (!placed.TryGetValue(cell, out var currentTile) || currentTile != tile)
            return;

        if (!monsters.TryGetValue(cell, out var inst) || inst.data != monster)
            return;

        var portrait = tile.Find("MonsterPortrait")?.GetComponent<Image>();
        var normal = GetCurrentMonsterPortrait(monster);
        if (portrait && normal)
            portrait.sprite = normal;
    }

    bool ShowAltPortraitsInRow(int row)
    {
        bool changedAny = false;

        for (int x = 0; x < width; x++)
        {
            var cell = new Vector2Int(x, row);
            if (TrySetMonsterAltPortrait(cell, out _, out _))
                changedAny = true;
        }

        return changedAny;
    }

    float PlayRowClearBackgroundPulse(int row, Dictionary<int, List<int>> clearOriginColumnsByRow)
    {
        if (!animateTetrominoBackgrounds || row < 0 || row >= height)
            return 0f;

        int originX = GetRowClearPulseOriginX(row, clearOriginColumnsByRow);
        int leftDistance = Mathf.Max(0, originX);
        int rightDistance = Mathf.Max(0, width - 1 - originX);
        float travelSeconds = Mathf.Max(0f, rowClearBackgroundRippleSeconds);
        float maxDelay = 0f;
        bool playedAny = false;

        for (int x = 0; x < width; x++)
        {
            var cell = new Vector2Int(x, row);
            if (!placed.TryGetValue(cell, out var tile) || !tile)
                continue;

            if (monsters.TryGetValue(cell, out var inst) && inst.hp <= 0f)
                continue;

            var pulse = tile.Find("HealthFill")?.GetComponent<TetrominoBackgroundPulse>();
            if (!pulse)
                continue;

            int distance = Mathf.Abs(x - originX);
            int sideDistance = x < originX ? leftDistance : (x > originX ? rightDistance : 0);
            float normalizedDistance = sideDistance > 0 ? Mathf.Clamp01(distance / (float)sideDistance) : 0f;
            float delay = normalizedDistance * travelSeconds;

            pulse.PlayRowClearPulse(
                delay,
                rowClearBackgroundPulseRiseSeconds,
                rowClearBackgroundPeakHoldSeconds,
                rowClearBackgroundPeakAlpha,
                rowClearBackgroundStartScale,
                rowClearBackgroundPeakScale);

            maxDelay = Mathf.Max(maxDelay, delay);
            playedAny = true;
        }

        if (!playedAny)
            return 0f;

        return maxDelay + Mathf.Max(0.01f, rowClearBackgroundPulseRiseSeconds) + Mathf.Max(0f, rowClearBackgroundPeakHoldSeconds);
    }

    int GetRowClearPulseOriginX(int row, Dictionary<int, List<int>> clearOriginColumnsByRow)
    {
        int originX = width / 2;

        if (clearOriginColumnsByRow != null &&
            clearOriginColumnsByRow.TryGetValue(row, out var columns) &&
            columns != null &&
            columns.Count > 0)
        {
            originX = columns[columns.Count - 1];
        }

        return Mathf.Clamp(originX, 0, Mathf.Max(0, width - 1));
    }

    void TickAttackPortraitIdleSwaps()
    {
        if (monsters.Count == 0)
        {
            attackPortraitIdleTimers.Clear();
            attackPiecePortraitIdleTimers.Clear();
            return;
        }

        PruneAttackPortraitIdleTimers();

        if (syncAttackPassiveAltByPiece)
            TickSyncedAttackPortraitIdleSwaps();
        else
        {
            attackPiecePortraitIdleTimers.Clear();
            TickIndividualAttackPortraitIdleSwaps();
        }
    }

    void PruneAttackPortraitIdleTimers()
    {
        attackPortraitStaleCellsScratch.Clear();
        foreach (var key in attackPortraitIdleTimers.Keys)
            attackPortraitStaleCellsScratch.Add(key);

        for (int i = 0; i < attackPortraitStaleCellsScratch.Count; i++)
        {
            var cell = attackPortraitStaleCellsScratch[i];
            if (!IsAttackPortraitIdleEligible(cell, out _))
                attackPortraitIdleTimers.Remove(cell);
        }

        if (attackPiecePortraitIdleTimers.Count == 0)
            return;

        attackPortraitLiveGroupsScratch.Clear();
        foreach (var pair in monsters)
        {
            if (IsAttackPortraitIdleEligible(pair.Key, out _, out var inst) && inst.pieceGroupId > 0)
                attackPortraitLiveGroupsScratch.Add(inst.pieceGroupId);
        }

        attackPortraitStaleGroupsScratch.Clear();
        foreach (var key in attackPiecePortraitIdleTimers.Keys)
            attackPortraitStaleGroupsScratch.Add(key);

        for (int i = 0; i < attackPortraitStaleGroupsScratch.Count; i++)
        {
            if (!attackPortraitLiveGroupsScratch.Contains(attackPortraitStaleGroupsScratch[i]))
                attackPiecePortraitIdleTimers.Remove(attackPortraitStaleGroupsScratch[i]);
        }
    }

    void TickIndividualAttackPortraitIdleSwaps()
    {
        foreach (var pair in monsters)
        {
            var cell = pair.Key;
            if (!IsAttackPortraitIdleEligible(cell, out _))
                continue;

            TickAttackPortraitCellTimer(cell);
        }
    }

    void TickSyncedAttackPortraitIdleSwaps()
    {
        attackPortraitEmptyGroupsScratch.Clear();
        foreach (var group in attackPortraitCellsByGroupScratch)
        {
            group.Value.Clear();
            attackPortraitEmptyGroupsScratch.Add(group.Key);
        }

        foreach (var pair in monsters)
        {
            var cell = pair.Key;
            if (!IsAttackPortraitIdleEligible(cell, out _, out var inst))
                continue;

            if (inst.pieceGroupId <= 0)
            {
                TickAttackPortraitCellTimer(cell);
                continue;
            }

            if (!attackPortraitCellsByGroupScratch.TryGetValue(inst.pieceGroupId, out var groupCells))
            {
                groupCells = new List<Vector2Int>();
                attackPortraitCellsByGroupScratch[inst.pieceGroupId] = groupCells;
            }

            groupCells.Add(cell);
        }

        for (int i = 0; i < attackPortraitEmptyGroupsScratch.Count; i++)
        {
            int groupId = attackPortraitEmptyGroupsScratch[i];
            if (attackPortraitCellsByGroupScratch.TryGetValue(groupId, out var groupCells) && groupCells.Count == 0)
                attackPortraitCellsByGroupScratch.Remove(groupId);
        }

        foreach (var group in attackPortraitCellsByGroupScratch)
        {
            if (!attackPiecePortraitIdleTimers.TryGetValue(group.Key, out float timer))
                timer = RandomAttackPortraitIdleSeconds();

            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                attackPiecePortraitIdleTimers[group.Key] = timer;
                continue;
            }

            var groupCells = group.Value;
            for (int i = 0; i < groupCells.Count; i++)
                FlashAttackPassiveAltPortrait(groupCells[i]);

            attackPiecePortraitIdleTimers[group.Key] = RandomAttackPortraitIdleSeconds();
        }
    }

    void TickAttackPortraitCellTimer(Vector2Int cell)
    {
        if (!attackPortraitIdleTimers.TryGetValue(cell, out float timer))
            timer = RandomAttackPortraitIdleSeconds();

        timer -= Time.deltaTime;
        if (timer > 0f)
        {
            attackPortraitIdleTimers[cell] = timer;
            return;
        }

        FlashAttackPassiveAltPortrait(cell);
        attackPortraitIdleTimers[cell] = RandomAttackPortraitIdleSeconds();
    }

    bool IsAttackPortraitIdleEligible(Vector2Int cell, out MonsterData monster)
    {
        return IsAttackPortraitIdleEligible(cell, out monster, out _);
    }

    bool IsAttackPortraitIdleEligible(Vector2Int cell, out MonsterData monster, out MonsterInstance inst)
    {
        monster = null;
        inst = default;

        if (!monsters.TryGetValue(cell, out var current) ||
            !current.data ||
            current.data.role != MonsterRole.Attack ||
            current.hp <= 0f)
        {
            return false;
        }

        if (!placed.TryGetValue(cell, out var tile) || !tile)
            return false;

        if (!GetCurrentMonsterAltPortrait(current.data))
            return false;

        monster = current.data;
        inst = current;
        return true;
    }

    void FlashAttackPassiveAltPortrait(Vector2Int cell)
    {
        FlashMonsterAltPortrait(cell, MonsterRole.Attack, attackPassiveAltFlashSeconds);
    }

    float RandomAttackPortraitIdleSeconds()
    {
        float min = Mathf.Max(0.01f, attackPassiveAltMinSeconds);
        float max = Mathf.Max(min, attackPassiveAltMaxSeconds);
        return UnityEngine.Random.Range(min, max);
    }

    Sprite GetCurrentMonsterPortrait(MonsterData monster)
    {
        if (!monster) return null;
        int skin = MonsterSkinStore.GetValidSelected(monster);
        return MonsterSkinStore.GetPortrait(monster, skin);
    }

    Sprite GetCurrentMonsterAltPortrait(MonsterData monster)
    {
        if (!monster) return null;
        int skin = MonsterSkinStore.GetValidSelected(monster);
        return MonsterSkinStore.GetAltPortrait(monster, skin);
    }

    public void SpawnHealBurst(Vector2Int cell, Sprite sprite, float seconds = 0.5f)
    {
        if (!placed.TryGetValue(cell, out var rt)) return;

        var img = GetOrCreateOverlayImage("HealVFX", rt);
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.color = new Color(1f, 1f, 1f, 0.95f);

        var vfx = img.rectTransform;
        vfx.anchorMin = vfx.anchorMax = new Vector2(0.5f, 0.5f);
        vfx.sizeDelta = rt.sizeDelta * 0.9f;

        StartCoroutine(FadeAndDestroy(img, seconds));
    }

    IEnumerator FadeAndDestroy(Image img, float seconds)
    {
        float t = 0f;
        var c = img.color;
        while (img && t < seconds)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0.95f, 0f, t / seconds);
            img.color = c;
            yield return null;
        }
        ReleaseOverlayImage(img);
    }

    public bool DamageTile(Vector2Int cell, float amount, DamageSource src = DamageSource.Generic,
                           AudioClip sfxOverride = null)
    {
        if (!monsters.TryGetValue(cell, out var inst) || inst.data == null)
            return false;

        if (inst.hp <= 0f)
            return false;

        if (tilesImmune)
        {
            if (AudioManager.I && sfxImmuneHit) AudioManager.I.PlaySFX(sfxImmuneHit);
            return true; // No damage taken
        }

        if (_gc && _gc.levelModifierController)
            amount = _gc.levelModifierController.ModifyIncomingDamage(cell, inst.data, amount, src);

        if (_gc)
            amount *= _gc.AllyMonsterDamageTakenMultiplier;

        amount = Mathf.Max(0f, amount);
        float applied = Mathf.Min(inst.hp, amount);
        if (applied <= 0f)
            return inst.hp > 0f;

        inst.hp = Mathf.Max(0f, inst.hp - amount);
        monsters[cell] = inst;
        UpdateTileHPVisual(cell, inst.hp, inst.maxHp);
        if (inst.hp > 0f)
            FlashMonsterAltPortrait(cell, MonsterRole.Defense);
        TileDamaged?.Invoke(cell, inst.data, applied, src);

        // --- Choose and play a hurt SFX ---
        AudioClip clip = sfxOverride;

        if (AudioManager.I && clip)
            AudioManager.I.PlaySFX(clip);

        if (inst.hp <= 0f)
        {
            StopPortraitAltSwap(cell, restoreNormal: true);
            Debug.Log($"Tile at {cell} ({inst.data.name}) died.");
            TileDied?.Invoke(cell, inst.data);
        }

        return inst.hp > 0f;
    }

    public int CountDeadTiles()
    {
        int count = 0;
        foreach (var kv in monsters)
        {
            var inst = kv.Value;
            if (inst.data == null) continue;
            if (inst.hp <= 0f) count++;
        }
        return count;
    }

    public bool HealTile(Vector2Int cell, float amount, MonsterData source)
    {
        if (!monsters.TryGetValue(cell, out var inst) || inst.data == null) return false;
        if (inst.hp <= 0f) return false; // cannot heal dead tiles

        float before = inst.hp;
        float newHp = Mathf.Min(inst.maxHp, inst.hp + amount);
        float applied = newHp - before;
        if (applied <= 0.0001f) return false;

        inst.hp = newHp;
        monsters[cell] = inst;
        UpdateTileHPVisual(cell, inst.hp, inst.maxHp);

        TileHealed?.Invoke(cell, inst.data, source, applied);
        PlayHealFeedback(cell, source);
        return true;
    }

    void PlayHealFeedback(Vector2Int cell, MonsterData source)
    {
        if (!source)
            return;

        if (source.healSprite)
            PlayHealVFX(cell, source.healSprite, 0.5f);

        if (!AudioManager.I)
            return;

        AudioClip clip = source.PickRandomHealSFX();
        if (clip)
            AudioManager.I.PlaySFX(clip);
    }

    public bool InBounds(Vector2Int c) =>
        c.x >= 0 && c.x < width && c.y >= 0 && c.y < height;

    public bool Valid(Vector2Int[] cells)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            if (!IsValidActivePieceCell(cells[i]))
                return false;
        }
        return true;
    }

    public bool Valid(IReadOnlyList<Vector2Int> cells)
    {
        if (cells == null)
            return false;

        for (int i = 0; i < cells.Count; i++)
        {
            if (!IsValidActivePieceCell(cells[i]))
                return false;
        }
        return true;
    }

    bool IsValidActivePieceCell(Vector2Int c)
    {
        if (c.x < 0 || c.x >= width) return false; // X must always be inside columns

        if (c.y >= height) return true; // Above the board is OK (spawn zone)

        if (c.y < 0) return false; // Below the board is not OK (floor)

        return !placed.ContainsKey(c); // Inside the board: must be empty
    }

    public void Place(Vector2Int c, RectTransform visual)
    {
        placed[c] = visual;
        visual.anchoredPosition = CellToAnchoredPos(c);
        visual.sizeDelta = cellSize;
    }

    public int AllocatePieceGroupId()
    {
        int groupId = nextPieceGroupId++;
        if (nextPieceGroupId == int.MaxValue)
            nextPieceGroupId = 1;

        return groupId;
    }

    public void ClearAll()
    {
        StopAllBoardVFX();
        StopAllPortraitAltSwaps(restoreNormal: false);
        ClearRoundTransientEffects();

        foreach (var tile in placed.Values)
            ReleaseBoardTile(tile);

        placed.Clear();
        monsters.Clear();
        obstacles.Clear();
        floorEffects.Clear();
        magicExplosives.Clear();

        healTimers.Clear();
        attackPortraitIdleTimers.Clear();
        attackPiecePortraitIdleTimers.Clear();

        // Clear grid visuals
        if (gridRoot != null)
        {
            var owned = gridRoot.GetComponentsInChildren<BoardOwned>(true);
            for (int i = owned.Length - 1; i >= 0; i--)
            {
                var rt = owned[i].transform as RectTransform;
                if (rt && pooledBoardTiles.Contains(rt))
                    continue;

                Destroy(owned[i].gameObject);
            }
        }

        // Clear overlays (fire/poison/lightning borders)
        foreach (var kv in poisonBorders) if (kv.Value) Destroy(kv.Value.gameObject);
        poisonBorders.Clear();

        foreach (var kv in fireBorders) if (kv.Value) Destroy(kv.Value.gameObject);
        fireBorders.Clear();

        foreach (var kv in lightningBorders) if (kv.Value) Destroy(kv.Value.gameObject);
        lightningBorders.Clear();

        // Clear spikes (underlay)
        foreach (var kv in spikeVisuals) if (kv.Value != null && kv.Value.root) Destroy(kv.Value.root.gameObject);
        spikeVisuals.Clear();

        foreach (var kv in teleportVisuals) if (kv.Value != null && kv.Value.root) Destroy(kv.Value.root.gameObject);
        teleportVisuals.Clear();

        foreach (var kv in zipPadVisuals) if (kv.Value != null && kv.Value.root) Destroy(kv.Value.root.gameObject);
        zipPadVisuals.Clear();

        foreach (var kv in floorTintUnderlays) if (kv.Value) Destroy(kv.Value.gameObject);
        floorTintUnderlays.Clear();

        foreach (var kv in floorSecondaryTintUnderlays) if (kv.Value) Destroy(kv.Value.gameObject);
        floorSecondaryTintUnderlays.Clear();

        RecomputeCellMetrics();
        ConfigureBoardVfxRoot();
        DrawGridOverlay();
    }

    public void ClearRoundTransientEffects()
    {
        magicExplosives.Clear();
        DestroyOverlayChildrenNamed("BossWarning");
        DestroyOverlayChildrenNamed("BossRotatingWarning");
        DestroyOverlayChildrenNamed("CellWarningTint");
        DestroyOverlayChildrenNamed("CellVFX");
    }

    void DestroyOverlayChildrenNamed(string childName)
    {
        if (!overlayRoot || string.IsNullOrEmpty(childName))
            return;

        for (int i = overlayRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = overlayRoot.GetChild(i);
            if (!child || child.name != childName)
                continue;

            if (child.TryGetComponent(out Image img))
                ReleaseOverlayImage(img);
            else
                Destroy(child.gameObject);
        }
    }

    float CalcMonsterDamageContribution(in MonsterInstance inst, GameController gc)
    {
        if (!inst.data) return 0f;
        if (inst.hp <= 0f) return 0f;

        float baseDmg = inst.attackPower + inst.attackBonus;

        return Mathf.Max(0f, baseDmg);
    }

    float CalcMonsterSpecialChargeContribution(in MonsterInstance inst, GameController gc)
    {
        if (!inst.data) return 0f;
        if (inst.hp <= 0f) return 0f;

        float gaugeMult = (gc ? gc.monsterSpecialGainMult : 1f);
        int gauge = Mathf.RoundToInt(inst.specialGaugeGain * gaugeMult);

        return Mathf.Max(1, gauge);
    }

    static MonsterData PickDominantMonster(Dictionary<MonsterData, int> counts)
    {
        MonsterData dominant = null;
        int max = 0;
        int ties = 0;

        foreach (var kv in counts)
        {
            if (!kv.Key || kv.Value <= 0)
                continue;

            if (kv.Value > max)
            {
                max = kv.Value;
                dominant = kv.Key;
                ties = 1;
            }
            else if (kv.Value == max)
            {
                ties++;
                if (UnityEngine.Random.Range(0, ties) == 0)
                    dominant = kv.Key;
            }
        }

        return dominant;
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
        var gc = _gc ? _gc : (_gc = FindFirstObjectByType<GameController>());

        bool clearedAny = true;
        var stonesDamaged = new HashSet<Vector2Int>();

        while (clearedAny)
        {
            clearedAny = false;

            for (int y = 0; y < height; y++)
            {
                // Row is full if every cell is occupied by something (monster tile or obstacle tile)
                if (!IsClearableOccupiedRow(y))
                    continue;

                clearedAny = true;
                rowsCleared++;

                int clearIndex = rowsCleared - 1;
                int rowKey = (clearIndex * 1000) + y;

                // ===== Tally row damage/special from monsters only =====
                float dmgRow = 0f;
                var counts = new Dictionary<MonsterData, int>();

                for (int x = 0; x < width; x++)
                {
                    var key = new Vector2Int(x, y);

                    // Obstacles do not contribute
                    if (obstacles.ContainsKey(key))
                        continue;

                    if (monsters.TryGetValue(key, out var inst) && inst.data)
                    {
                        if (inst.hp > 0f)
                        {
                            float dmg = CalcMonsterDamageContribution(in inst, gc);
                            if (dmg > 0f)
                                dmgRow += dmg;

                            float gauge = CalcMonsterSpecialChargeContribution(in inst, gc);
                            if (gauge > 0f)
                                specialChargeFromMonsters += gauge;
                        }

                        if (!counts.ContainsKey(inst.data))
                            counts[inst.data] = 0;
                        counts[inst.data] += 1;
                    }
                }

                // ===== Magic explosive bonus if player clears the row in time =====
                int explosiveBonus = 0;
                for (int x = 0; x < width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (obstacles.TryGetValue(c, out var o) && o.type == ObstacleType.MagicExplosive)
                    {
                        if (magicExplosives.TryGetValue(c, out var ex))
                            explosiveBonus += Mathf.Max(0, ex.rowClearBonusDamage);
                    }
                }

                if (explosiveBonus > 0)
                    dmgRow += explosiveBonus;

                // ===== Combo scoring and combo damage bonus =====
                int monstersInRow = 0;
                foreach (var kv in counts)
                    monstersInRow += kv.Value;

                int finalRowDamage = gc
                    ? gc.ApplyComboForRowClear(monstersInRow, dmgRow)
                    : (dmgRow > 0f ? Mathf.Max(GameController.MinimumFinalMonsterDamage, Mathf.RoundToInt(dmgRow)) : 0);

                damageFromMonsters += finalRowDamage;
                rowDamage[rowKey] = finalRowDamage;

                if (counts.Count > 0)
                {
                    var dominant = PickDominantMonster(counts);
                    if (dominant)
                        rowDominantMonster[rowKey] = dominant;
                }

                ShowAltPortraitsInRow(y);

                // ===== Remove row contents =====
                for (int x = 0; x < width; x++)
                {
                    var key = new Vector2Int(x, y);

                    // Stone obstacles take 1 hit, update sprite, only destroy when hitsRemaining reaches 0
                    if (TryHandleObstacleAt(key, stonesDamaged, removedCells, detonateExplosive: false))
                        continue;

                    StopPortraitAltSwap(key, restoreNormal: false);
                    monsters.Remove(key); // Normal monster tile, remove completely

                    if (placed.TryGetValue(key, out var rt))
                    {
                        ReleaseBoardTile(rt);
                        placed.Remove(key);
                        removedCells.Add(key);
                    }
                }
  
                SettleAllColumns(false); // Let remaining tiles fall into gaps (stones remain as blockers)
                y = -1; // Board changed, restart scanning from bottom
            } 
        }

        CleanOrphanedTiles();
    }

    public void ClearFullLinesAnimated(
    Action<int, List<Vector2Int>, int, float, Dictionary<int, int>, Dictionary<int, MonsterData>> onComplete,
    float? overrideCascadeDelay = null,
    Dictionary<int, List<int>> clearOriginColumnsByRow = null)
    {
        float delay = overrideCascadeDelay ?? cascadeClearVisualDelay;
        StartCoroutine(ClearFullLinesRoutine(delay, onComplete, clearOriginColumnsByRow));
    }

    IEnumerator ClearFullLinesRoutine(
        float cascadeDelay,
        Action<int, List<Vector2Int>, int, float, Dictionary<int, int>, Dictionary<int, MonsterData>> onComplete,
        Dictionary<int, List<int>> clearOriginColumnsByRow)
    {
        int rowsCleared = 0;
        var removedCells = new List<Vector2Int>();
        int damageFromMonsters = 0;
        float specialChargeFromMonsters = 0f;
        var rowDamage = new Dictionary<int, int>();
        var rowDominantMonster = new Dictionary<int, MonsterData>();
        var gc = _gc ? _gc : (_gc = FindFirstObjectByType<GameController>());

        var stonesDamaged = new HashSet<Vector2Int>();
        var fullRows = new List<int>();

        if (gc)
            gc.SetRowClearComboResolutionActive(true);

        try
        {
        while (true)
        {
            fullRows.Clear();

            for (int y = 0; y < height; y++)
            {
                if (!IsClearableOccupiedRow(y))
                    continue;

                fullRows.Add(y);
            }

            if (fullRows.Count == 0)
                break;

            if (rowsCleared == 0 && gc)
            {
                var tutorialTargets = new List<RectTransform>();
                for (int i = 0; i < fullRows.Count; i++)
                    tutorialTargets.AddRange(GetPlacedVisualsInRow(fullRows[i]));

                yield return gc.ShowFirstFullRowTutorialIfNeeded(tutorialTargets);
            }

            // Animate and score every row that is full in this wave before removing any of them.
            float preClearHoldSeconds = Mathf.Max(0f, cascadeDelay);
            for (int i = 0; i < fullRows.Count; i++)
            {
                int y = fullRows[i];
                rowsCleared++;

                int clearIndex = rowsCleared - 1;
                int rowKey = (clearIndex * 1000) + y;

                // ===== Tally row damage/special from Monsters only =====
                float dmgRow = 0f;
                var counts = new Dictionary<MonsterData, int>();

                for (int x = 0; x < width; x++)
                {
                    var key = new Vector2Int(x, y);

                    // Obstacles do not contribute
                    if (obstacles.ContainsKey(key))
                        continue;

                    if (monsters.TryGetValue(key, out var inst) && inst.data)
                    {
                        if (inst.hp > 0f)
                        {
                            float dmg = CalcMonsterDamageContribution(in inst, gc);
                            if (dmg > 0f)
                                dmgRow += dmg;

                            float gauge = CalcMonsterSpecialChargeContribution(in inst, gc);
                            if (gauge > 0f)
                                specialChargeFromMonsters += gauge;
                        }

                        if (!counts.ContainsKey(inst.data))
                            counts[inst.data] = 0;
                        counts[inst.data] += 1;
                    }
                }

                // ===== Magic explosive bonus if player clears the row in time =====
                int explosiveBonus = 0;
                for (int x = 0; x < width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (obstacles.TryGetValue(c, out var o) && o.type == ObstacleType.MagicExplosive)
                    {
                        if (magicExplosives.TryGetValue(c, out var ex))
                            explosiveBonus += Mathf.Max(0, ex.rowClearBonusDamage);
                    }
                }

                if (explosiveBonus > 0)
                    dmgRow += explosiveBonus;

                // ===== Combo scoring and combo damage bonus =====
                int monstersInRow = 0;
                foreach (var kv in counts)
                    monstersInRow += kv.Value;

                int finalRowDamage = gc
                    ? gc.ApplyComboForRowClear(monstersInRow, dmgRow)
                    : (dmgRow > 0f ? Mathf.Max(GameController.MinimumFinalMonsterDamage, Mathf.RoundToInt(dmgRow)) : 0);

                damageFromMonsters += finalRowDamage;
                rowDamage[rowKey] = finalRowDamage;

                if (counts.Count > 0)
                {
                    var dominant = PickDominantMonster(counts);
                    if (dominant)
                        rowDominantMonster[rowKey] = dominant;
                }

                float backgroundPulseSeconds = PlayRowClearBackgroundPulse(y, clearOriginColumnsByRow);
                bool rowAltShown = ShowAltPortraitsInRow(y);
                float altHoldSeconds = (rowAltShown && attackLineClearAltHoldSeconds > 0f) ? attackLineClearAltHoldSeconds : 0f;
                preClearHoldSeconds = Mathf.Max(preClearHoldSeconds, backgroundPulseSeconds, altHoldSeconds);
            }

            // Keep these reactions in one shared timing window. The full background
            // flash stays visible, but the cascade pause no longer adds extra time after it.
            if (preClearHoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(preClearHoldSeconds);

            // ===== Remove all row contents in this wave =====
            for (int i = 0; i < fullRows.Count; i++)
            {
                int y = fullRows[i];
                for (int x = 0; x < width; x++)
                {
                    var key = new Vector2Int(x, y);

                    if (TryHandleObstacleAt(key, stonesDamaged, removedCells, detonateExplosive: false))
                        continue;

                    StopPortraitAltSwap(key, restoreNormal: false);
                    monsters.Remove(key);

                    if (placed.TryGetValue(key, out var rt))
                    {
                        ReleaseBoardTile(rt);
                        placed.Remove(key);
                        removedCells.Add(key);
                    }
                }
            }

            SettleAllColumns(false); // Let remaining tiles fall into gaps (stones remain as blockers)

            // Redraw at least one frame so cascade-created full rows are visible before being cleared.
            // The cascade pause is already included in the shared pre-clear timing above.
            yield return null;
        }
        }
        finally
        {
            if (gc)
                gc.SetRowClearComboResolutionActive(false);
        }

        CleanOrphanedTiles();

        onComplete?.Invoke(rowsCleared, removedCells, damageFromMonsters, specialChargeFromMonsters, rowDamage, rowDominantMonster);
    }

    // ================ Character Special Abilities ================

    public int ClearBottomRowsWithCombat(
    int rows,
    out int totalMonsterDamage,
    out float specialChargeFromMonsters,
    out Dictionary<int, int> rowDamage,
    out Dictionary<int, MonsterData> rowDominantMonster,
    out Dictionary<int, List<int>> colsByRow)
    {
        totalMonsterDamage = 0;
        specialChargeFromMonsters = 0f;
        rowDamage = new Dictionary<int, int>();
        rowDominantMonster = new Dictionary<int, MonsterData>();
        colsByRow = new Dictionary<int, List<int>>();

        if (rows <= 0) return 0;
        rows = Mathf.Min(rows, height);

        var gc = _gc ? _gc : (_gc = FindFirstObjectByType<GameController>());

        // ===== Tally combat stats from bottom rows before deletion =====
        for (int y = 0; y < rows; y++)
        {
            float dmgRow = 0f;

            // Count prevalence for dominant selection
            var countsRow = new Dictionary<MonsterData, int>();

            for (int x = 0; x < width; x++)
            {
                var key = new Vector2Int(x, y);

                if (!placed.ContainsKey(key))
                    continue;

                // Obstacles contribute nothing
                if (obstacles.ContainsKey(key))
                    continue;

                if (monsters.TryGetValue(key, out var inst) && inst.data)
                {
                    // Count for dominant visuals
                    if (!countsRow.ContainsKey(inst.data)) countsRow[inst.data] = 0;
                    countsRow[inst.data] += 1;

                    // Living monsters contribute damage/gauge
                    if (inst.hp > 0f)
                    {
                        float dmg = CalcMonsterDamageContribution(in inst, gc);
                        if (dmg > 0f)
                            dmgRow += dmg;

                        float gauge = CalcMonsterSpecialChargeContribution(in inst, gc);
                        if (gauge > 0f)
                            specialChargeFromMonsters += gauge;
                    }

                    // Collect candidate start columns for this row�s projectile
                    if (!colsByRow.TryGetValue(y, out var cols))
                    {
                        cols = new List<int>();
                        colsByRow[y] = cols;
                    }
                    if (!cols.Contains(x)) cols.Add(x);
                }
            }

            int rowKey = (y * 1000) + y;

            // ===== Combo scoring and combo damage bonus =====
            int monstersInRow = 0;
            foreach (var kv in countsRow)
                monstersInRow += kv.Value;

            int finalRowDamage = gc
                ? gc.ApplyComboForRowClear(monstersInRow, dmgRow)
                : (dmgRow > 0f ? Mathf.Max(GameController.MinimumFinalMonsterDamage, Mathf.RoundToInt(dmgRow)) : 0);

            if (finalRowDamage > 0)
            {
                rowDamage[rowKey] = finalRowDamage;
                totalMonsterDamage += finalRowDamage;
            }

            // Pick dominant monster for this cleared row
            if (countsRow.Count > 0)
            {
                var dominant = PickDominantMonster(countsRow);
                if (dominant)
                    rowDominantMonster[rowKey] = dominant;
            }
        }

        // ===== Perform the actual clear and shift =====
        int squaresCleared = 0;
        var stonesDamaged = new HashSet<Vector2Int>();

        var toDelete = new List<Vector2Int>();
        foreach (var kv in placed)
            if (kv.Key.y < rows) toDelete.Add(kv.Key);

        foreach (var key in toDelete)
        {
            if (obstacles.ContainsKey(key))
            {
                bool occupiedBefore = placed.ContainsKey(key);
                if (TryHandleObstacleAt(key, stonesDamaged, null, detonateExplosive: false))
                {
                    if (occupiedBefore && !placed.ContainsKey(key))
                        squaresCleared++;

                    continue;
                }
            }

            if (placed.TryGetValue(key, out var rt))
                ReleaseBoardTile(rt);

            StopPortraitAltSwap(key, restoreNormal: false);
            placed.Remove(key);
            monsters.Remove(key);
            healTimers.Remove(key);
            squaresCleared++;
        }

        SettleAllColumns(false);

        return squaresCleared;
    }

    public int ClearBottomRows(int rows)
    {
        if (rows <= 0) return 0;
        rows = Mathf.Min(rows, height);

        int squaresCleared = 0;
        var stonesDamaged = new HashSet<Vector2Int>();

        var toDelete = new List<Vector2Int>();
        foreach (var kv in placed)
            if (kv.Key.y < rows) toDelete.Add(kv.Key);

        foreach (var key in toDelete)
        {
            // If this cell is a stone obstacle, destroy it via the proper path to keep dictionaries synced
            if (obstacles.ContainsKey(key))
            {
                bool occupiedBefore = placed.ContainsKey(key);
                if (TryHandleObstacleAt(key, stonesDamaged, null, detonateExplosive: false))
                {
                    if (occupiedBefore && !placed.ContainsKey(key))
                        squaresCleared++;

                    continue;
                }
            }

            if (placed.TryGetValue(key, out var rt))
                ReleaseBoardTile(rt);

            StopPortraitAltSwap(key, restoreNormal: false);
            placed.Remove(key);
            monsters.Remove(key);
            healTimers.Remove(key);

            squaresCleared++;
        }

        SettleAllColumns(false);

        return squaresCleared;
    }

    public int ReviveAllTilesToFull(List<Vector2Int> changedCells)
    {
        return ReviveAllTilesToFull(changedCells, out _);
    }

    public int ReviveAllTilesToFull(List<Vector2Int> changedCells, out float totalRestored)
    {
        if (changedCells == null) changedCells = new List<Vector2Int>();
        totalRestored = 0f;

        var keys = new List<Vector2Int>(monsters.Keys);

        foreach (var cell in keys)
        {
            if (!monsters.TryGetValue(cell, out var inst) || inst.data == null) continue;

            float max = Mathf.Max(0f, inst.maxHp);
            if (inst.hp < max) // Includes reviving from 0
            {
                totalRestored += Mathf.Max(0f, max - inst.hp);
                inst.hp = max;
                monsters[cell] = inst;

                UpdateTileHPVisual(cell, inst.hp, max); // Update visual
                changedCells.Add(cell);
            }
        }
        return changedCells.Count;
    }

    public void SetTilesDamageImmunity(bool on)
    {
        tilesImmune = on;
        RefreshAllTileBorders();
    }

    public bool TilesDamageImmune => tilesImmune;

    public void SetAllTileBorderColor(Color c)
    {
        foreach (var kv in placed)
        {
            var rt = kv.Value;
            if (!rt) continue;
            SetInlineBorderColor(rt, c);
        }
    }

    public void StyleInlineBorder(RectTransform rt, Color c)
    {
        SetInlineBorderColor(rt, c);
    }

    void DrawGridOverlay()
    {
        // Remove old grid lines by marker
        if (gridRoot)
        {
            var olds = gridRoot.GetComponentsInChildren<BoardOwned>(true);
            foreach (var m in olds)
            {
                if (m.name.StartsWith("GridLine_")) Destroy(m.gameObject);
            }
        }

        var col = new Color(1, 1, 1, 0.65f);
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

        MaintainBoardVfxLayering();
    }

    public void MultiplyAllMonsterHpAndMax(float mult)
    {
        if (mult <= 0f) return;

        var keys = new List<Vector2Int>(monsters.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            var cell = keys[i];

            var inst = monsters[cell];
            if (!inst.data) continue;

            inst.maxHp *= mult;
            inst.hp *= mult;

            inst.hp = Mathf.Clamp(inst.hp, 0f, inst.maxHp);

            monsters[cell] = inst;
            UpdateTileHPVisual(cell, inst.hp, inst.maxHp);
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
        var gc = _gc ? _gc : (_gc = FindFirstObjectByType<GameController>());
        var stonesDamaged = new HashSet<Vector2Int>();
        float monsterDamageTotal = 0f;

        // Remove & tally
        var removeSet = new HashSet<Vector2Int>(toRemove);
        foreach (var key in removeSet)
        {
            if (monsters.TryGetValue(key, out var inst))
            {
                if (inst.data)
                {
                    if (inst.hp > 0f)
                    {
                        float dmg = CalcMonsterDamageContribution(in inst, gc);
                        if (dmg > 0f)
                            monsterDamageTotal += dmg;

                        float gauge = CalcMonsterSpecialChargeContribution(in inst, gc);
                        if (gauge > 0f)
                            specialChargeFromMonsters += gauge;
                    }
                }

                StopPortraitAltSwap(key, restoreNormal: false);
                monsters.Remove(key);
            }

            if (TryHandleObstacleAt(key, stonesDamaged, removedCells, detonateExplosive: true))
                continue;

            if (placed.TryGetValue(key, out var rt))
            {
                StopPortraitAltSwap(key, restoreNormal: false);
                ReleaseBoardTile(rt);
                placed.Remove(key);
                removedCells.Add(key);
            }
        }

        damageFromMonsters = Mathf.RoundToInt(monsterDamageTotal);

        var cols = new HashSet<int>();
        foreach (var c in removeSet)
            cols.Add(c.x);

        SettleColumns(cols, false);
        CleanOrphanedTiles();
    }

    public void SettleAllColumns() => SettleAllColumns(false);

    public void SettleAllColumns(bool allowObstaclesToFall)
    {
        for (int x = 0; x < width; x++)
        {
            int writeY = 0;

            for (int y = 0; y < height; y++)
            {
                var from = new Vector2Int(x, y);

                // If obstacles are not allowed to fall, they are fixed blockers
                if (!allowObstaclesToFall && obstacles.ContainsKey(from))
                {
                    writeY = y + 1;
                    continue;
                }

                if (!placed.TryGetValue(from, out var rt) || !rt)
                    continue;

                // Never move obstacle visuals in non-obstacle mode
                if (!allowObstaclesToFall && obstacles.ContainsKey(from))
                    continue;

                // Don't write into obstacle cells when obstacles are fixed
                if (!allowObstaclesToFall)
                {
                    while (writeY < height && obstacles.ContainsKey(new Vector2Int(x, writeY)))
                        writeY++;
                }

                var to = new Vector2Int(x, writeY);

                if (to != from)
                {
                    StopPortraitAltSwap(from, restoreNormal: true);
                    placed.Remove(from);
                    placed[to] = rt;
                    rt.anchoredPosition = CellToAnchoredPos(to);

                    if (monsters.TryGetValue(from, out var inst))
                    {
                        monsters.Remove(from);
                        monsters[to] = inst;
                    }

                    if (healTimers.TryGetValue(from, out var t))
                    {
                        healTimers.Remove(from);
                        healTimers[to] = t;
                    }

                    // When obstacles are allowed to fall, keep obstacle state synced
                    if (allowObstaclesToFall && obstacles.TryGetValue(from, out var obs))
                    {
                        obstacles.Remove(from);
                        obstacles[to] = obs;
                    }
                }

                writeY++;
            }
        }

        RefreshAllTileBorders();
        CleanOrphanedTiles();
        ActivateTeleportFloorEffectsForSettledMonsters();
    }

    public void ShiftBoardUpOneRowAndFillBottom(Sprite soldierSprite)
    {
        var snapshot = new List<Vector2Int>(placed.Keys);
        snapshot.Sort((a, b) => b.y.CompareTo(a.y));

        for (int i = 0; i < snapshot.Count; i++)
        {
            Vector2Int from = snapshot[i];
            if (!placed.TryGetValue(from, out var rt) || !rt)
                continue;

            Vector2Int to = from + Vector2Int.up;
            if (!InBounds(to))
            {
                if (monsters.TryGetValue(from, out var inst) && inst.data != null && inst.hp > 0f)
                    ForceKillMonsterAndRemove(from, DamageSource.RearAmbush);
                else
                    RemovePlacedCellImmediate(from);

                continue;
            }

            StopPortraitAltSwap(from, restoreNormal: true);
            placed.Remove(from);
            placed[to] = rt;
            rt.anchoredPosition = CellToAnchoredPos(to);

            if (monsters.TryGetValue(from, out var movedMonster))
            {
                monsters.Remove(from);
                monsters[to] = movedMonster;
            }

            if (healTimers.TryGetValue(from, out var healTimer))
            {
                healTimers.Remove(from);
                healTimers[to] = healTimer;
            }

            if (obstacles.TryGetValue(from, out var obs))
            {
                obstacles.Remove(from);
                obstacles[to] = obs;
            }

            if (magicExplosives.TryGetValue(from, out var explosive))
            {
                magicExplosives.Remove(from);
                magicExplosives[to] = explosive;
            }
        }

        for (int x = 0; x < width; x++)
        {
            Vector2Int bottom = new Vector2Int(x, 0);
            if (IsFree(bottom))
                TrySpawnCustomObstacle(bottom, ObstacleType.SoldierWall, soldierSprite, 1, true);
        }

        RefreshAllTileBorders();
        CleanOrphanedTiles();
        ActivateTeleportFloorEffectsForSettledMonsters();
    }

    // Variant of SettleAllColumns that only processes specified columns for efficiency
    public void SettleColumns(IEnumerable<int> columns, bool allowObstaclesToFall)
    {
        if (columns == null) return;

        foreach (int x in columns)
        {
            if (x < 0 || x >= width) continue;

            int writeY = 0;

            for (int y = 0; y < height; y++)
            {
                var from = new Vector2Int(x, y);

                // If obstacles are not allowed to fall, they are fixed blockers
                if (!allowObstaclesToFall && obstacles.ContainsKey(from))
                {
                    writeY = y + 1;
                    continue;
                }

                if (!placed.TryGetValue(from, out var rt) || !rt)
                    continue;

                // Never move obstacle visuals in non-obstacle mode
                if (!allowObstaclesToFall && obstacles.ContainsKey(from))
                    continue;

                // Don't write into obstacle cells when obstacles are fixed
                if (!allowObstaclesToFall)
                {
                    while (writeY < height && obstacles.ContainsKey(new Vector2Int(x, writeY)))
                        writeY++;
                }

                var to = new Vector2Int(x, writeY);

                if (to != from)
                {
                    StopPortraitAltSwap(from, restoreNormal: true);
                    placed.Remove(from);
                    placed[to] = rt;
                    rt.anchoredPosition = CellToAnchoredPos(to);

                    if (monsters.TryGetValue(from, out var inst))
                    {
                        monsters.Remove(from);
                        monsters[to] = inst;
                    }

                    if (healTimers.TryGetValue(from, out var t))
                    {
                        healTimers.Remove(from);
                        healTimers[to] = t;
                    }

                    // When obstacles are allowed to fall, keep obstacle state synced
                    if (allowObstaclesToFall && obstacles.TryGetValue(from, out var obs))
                    {
                        obstacles.Remove(from);
                        obstacles[to] = obs;
                    }
                }

                writeY++;
            }
        }

        RefreshAllTileBorders();
        CleanOrphanedTiles();
    }

    public void FlashCells(IEnumerable<Vector2Int> cells, Sprite sprite, bool onlyOccupied)
    {
        if (sprite == null) return;
        StartCoroutine(FlashCellsCo(cells, sprite, onlyOccupied));
    }

    public void PlaySpecialBoardVFX(SpecialType special)
    {
        Sprite sprite = GetBoardVfxSprite(special);
        if (!sprite)
            return;

        if (boardVfxRoutines.TryGetValue(special, out var running) && running != null)
            StopCoroutine(running);

        DestroyBoardVfxImage(special);

        Coroutine routine = special == SpecialType.Bomb
            ? StartCoroutine(CoCenterRevealBoardVFX(special, sprite))
            : StartCoroutine(CoVerticalRevealBoardVFX(special, sprite));

        boardVfxRoutines[special] = routine;
    }

    public void PlaySlowGravityBoardVFX(float durationSeconds, Func<float> remainingSecondsProvider)
    {
        if (!slowGravityBoardVFXSprite)
            return;

        StopSlowGravityBoardVFX();
        slowGravityBoardVfxRoutine = StartCoroutine(CoSlowGravityBoardVFX(
            Mathf.Max(0.1f, durationSeconds),
            remainingSecondsProvider));
    }

    public void StopSlowGravityBoardVFX()
    {
        if (slowGravityBoardVfxRoutine != null)
        {
            StopCoroutine(slowGravityBoardVfxRoutine);
            slowGravityBoardVfxRoutine = null;
        }

        if (slowGravityBoardVfxImage)
        {
            ReleaseOverlayImage(slowGravityBoardVfxImage);
            slowGravityBoardVfxImage = null;
        }
    }

    void StopAllBoardVFX()
    {
        foreach (var routine in boardVfxRoutines.Values)
        {
            if (routine != null)
                StopCoroutine(routine);
        }

        boardVfxRoutines.Clear();
        boardVfxImages.Clear();
        StopSlowGravityBoardVFX();

        if (!boardVfxRoot)
            return;

        for (int i = boardVfxRoot.childCount - 1; i >= 0; i--)
        {
            var child = boardVfxRoot.GetChild(i);
            if (child && child.TryGetComponent(out Image img))
                ReleaseOverlayImage(img);
            else if (child)
                Destroy(child.gameObject);
        }
    }

    Sprite GetBoardVfxSprite(SpecialType special)
    {
        switch (special)
        {
            case SpecialType.Death:
                return deathBoardVFXSprite;
            case SpecialType.Bomb:
                return bombBoardVFXSprite;
            case SpecialType.Bolt:
                return boltBoardVFXSprite;
            case SpecialType.Earthquake:
                return earthquakeBoardVFXSprite;
            default:
                return null;
        }
    }

    IEnumerator CoVerticalRevealBoardVFX(SpecialType special, Sprite sprite)
    {
        Image img = CreateBoardVfxImage($"{special}Board_VFX", sprite);
        if (!img)
        {
            boardVfxRoutines.Remove(special);
            yield break;
        }

        boardVfxImages[special] = img;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Vertical;
        img.fillOrigin = (int)Image.OriginVertical.Bottom;
        img.fillAmount = 0f;

        float maxAlpha = Mathf.Clamp01(boardVfxMaxAlpha);
        yield return AnimateBoardVfxFillAndAlpha(img, 0f, 1f, 0f, maxAlpha, boardVfxFadeInSeconds);

        if (img && boardVfxHoldSeconds > 0f)
            yield return new WaitForSeconds(boardVfxHoldSeconds);

        if (img)
            yield return AnimateBoardVfxAlpha(img, maxAlpha, 0f, boardVfxFadeOutSeconds);

        ReleaseOverlayImage(img);

        boardVfxImages.Remove(special);
        boardVfxRoutines.Remove(special);
    }

    IEnumerator CoCenterRevealBoardVFX(SpecialType special, Sprite sprite)
    {
        Image img = CreateBoardVfxImage($"{special}Board_VFX", sprite);
        if (!img)
        {
            boardVfxRoutines.Remove(special);
            yield break;
        }

        boardVfxImages[special] = img;
        img.type = Image.Type.Simple;
        img.rectTransform.localScale = Vector3.zero;

        float maxAlpha = Mathf.Clamp01(boardVfxMaxAlpha);
        yield return AnimateBoardVfxScaleAndAlpha(img, Vector3.zero, Vector3.one, 0f, maxAlpha, boardVfxFadeInSeconds);

        if (img && boardVfxHoldSeconds > 0f)
            yield return new WaitForSeconds(boardVfxHoldSeconds);

        if (img)
            yield return AnimateBoardVfxAlpha(img, maxAlpha, 0f, boardVfxFadeOutSeconds);

        ReleaseOverlayImage(img);

        boardVfxImages.Remove(special);
        boardVfxRoutines.Remove(special);
    }

    IEnumerator CoSlowGravityBoardVFX(float durationSeconds, Func<float> remainingSecondsProvider)
    {
        Image img = CreateBoardVfxImage("SlowGravityBoard_VFX", slowGravityBoardVFXSprite);
        slowGravityBoardVfxImage = img;
        if (!img)
        {
            slowGravityBoardVfxRoutine = null;
            yield break;
        }

        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = (int)Image.Origin360.Top;
        img.fillClockwise = false;
        img.fillAmount = 1f;

        float maxAlpha = Mathf.Clamp01(boardVfxMaxAlpha);
        yield return AnimateBoardVfxAlpha(img, 0f, maxAlpha, slowGravityBoardVfxFadeInSeconds);

        float fallbackRemaining = durationSeconds;
        while (img)
        {
            float remaining = remainingSecondsProvider != null
                ? remainingSecondsProvider()
                : fallbackRemaining;

            remaining = Mathf.Clamp(remaining, 0f, durationSeconds);
            img.fillAmount = durationSeconds > 0f ? remaining / durationSeconds : 0f;
            SetImageAlpha(img, maxAlpha);

            if (remaining <= 0f)
                break;

            fallbackRemaining -= Time.deltaTime;
            yield return null;
        }

        ReleaseOverlayImage(img);

        if (slowGravityBoardVfxImage == img)
            slowGravityBoardVfxImage = null;

        slowGravityBoardVfxRoutine = null;
    }

    Image CreateBoardVfxImage(string objectName, Sprite sprite)
    {
        if (!sprite)
            return null;

        EnsureBoardVfxRoot();
        ConfigureBoardVfxRoot();
        if (!boardVfxRoot)
            return null;

        var img = GetOrCreateOverlayImage(objectName, boardVfxRoot);
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.color = new Color(1f, 1f, 1f, 0f);

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = contentSize;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;

        MaintainBoardVfxLayering();
        return img;
    }

    IEnumerator AnimateBoardVfxFillAndAlpha(Image img, float fromFill, float toFill, float fromAlpha, float toAlpha, float seconds)
    {
        float duration = Mathf.Max(0f, seconds);
        if (duration <= 0f)
        {
            if (img)
            {
                img.fillAmount = toFill;
                SetImageAlpha(img, toAlpha);
            }
            yield break;
        }

        float t = 0f;
        while (img && t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = Mathf.SmoothStep(0f, 1f, p);
            img.fillAmount = Mathf.Lerp(fromFill, toFill, eased);
            SetImageAlpha(img, Mathf.Lerp(fromAlpha, toAlpha, eased));
            yield return null;
        }

        if (img)
        {
            img.fillAmount = toFill;
            SetImageAlpha(img, toAlpha);
        }
    }

    IEnumerator AnimateBoardVfxScaleAndAlpha(Image img, Vector3 fromScale, Vector3 toScale, float fromAlpha, float toAlpha, float seconds)
    {
        float duration = Mathf.Max(0f, seconds);
        if (duration <= 0f)
        {
            if (img)
            {
                img.rectTransform.localScale = toScale;
                SetImageAlpha(img, toAlpha);
            }
            yield break;
        }

        float t = 0f;
        while (img && t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = Mathf.SmoothStep(0f, 1f, p);
            img.rectTransform.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
            SetImageAlpha(img, Mathf.Lerp(fromAlpha, toAlpha, eased));
            yield return null;
        }

        if (img)
        {
            img.rectTransform.localScale = toScale;
            SetImageAlpha(img, toAlpha);
        }
    }

    IEnumerator AnimateBoardVfxAlpha(Image img, float fromAlpha, float toAlpha, float seconds)
    {
        float duration = Mathf.Max(0f, seconds);
        if (duration <= 0f)
        {
            if (img)
                SetImageAlpha(img, toAlpha);
            yield break;
        }

        float t = 0f;
        while (img && t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            SetImageAlpha(img, Mathf.Lerp(fromAlpha, toAlpha, Mathf.SmoothStep(0f, 1f, p)));
            yield return null;
        }

        if (img)
            SetImageAlpha(img, toAlpha);
    }

    static void SetImageAlpha(Image img, float alpha)
    {
        if (!img)
            return;

        Color color = img.color;
        color.a = Mathf.Clamp01(alpha);
        img.color = color;
    }

    void DestroyBoardVfxImage(SpecialType special)
    {
        if (!boardVfxImages.TryGetValue(special, out var img))
            return;

        ReleaseOverlayImage(img);

        boardVfxImages.Remove(special);
    }

    void EnsureBoardVfxRoot()
    {
        if (boardVfxRoot)
        {
            boardVfxRoot.name = "BoardVFXRoot";
            if (boardVfxRoot.transform.parent != transform)
                boardVfxRoot.SetParent(transform, false);
            return;
        }

        Transform existing = transform.Find("BoardVFXRoot");

        if (!existing && gridRoot)
            existing = gridRoot.Find("BoardVFXRoot");

        if (existing)
        {
            boardVfxRoot = existing as RectTransform;
            if (boardVfxRoot)
            {
                if (boardVfxRoot.transform.parent != transform)
                    boardVfxRoot.SetParent(transform, false);
                return;
            }
        }

        var go = new GameObject("BoardVFXRoot", typeof(RectTransform));
        boardVfxRoot = go.GetComponent<RectTransform>();
        boardVfxRoot.SetParent(transform, false);
    }

    void ConfigureBoardVfxRoot()
    {
        if (!boardVfxRoot)
            return;

        boardVfxRoot.anchorMin = boardVfxRoot.anchorMax = new Vector2(0.5f, 0.5f);
        boardVfxRoot.pivot = new Vector2(0.5f, 0.5f);
        boardVfxRoot.sizeDelta = contentSize;
        boardVfxRoot.anchoredPosition = Vector2.zero;
        boardVfxRoot.localScale = Vector3.one;
        MaintainBoardVfxLayering();
    }

    void MaintainBoardVfxLayering()
    {
        if (!boardVfxRoot)
            return;

        if (boardVfxRoot.transform.parent != transform)
            boardVfxRoot.SetParent(transform, false);

        if (boardVfxRoot.transform.parent == transform)
            boardVfxRoot.SetAsFirstSibling();

        if (underlayRoot && underlayRoot.transform.parent == transform)
            underlayRoot.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));

        if (gridRoot && gridRoot.transform.parent == transform)
            gridRoot.SetSiblingIndex(Mathf.Min(2, transform.childCount - 1));

        if (overlayRoot && overlayRoot.transform.parent == transform)
            overlayRoot.SetAsLastSibling();

        if (!gridRoot)
            return;

        var gridLines = new List<Transform>();
        for (int i = 0; i < gridRoot.childCount; i++)
        {
            Transform child = gridRoot.GetChild(i);
            if (child && child.name.StartsWith("GridLine_", StringComparison.Ordinal))
                gridLines.Add(child);
        }

        for (int i = 0; i < gridLines.Count; i++)
            if (gridLines[i])
                gridLines[i].SetAsLastSibling();
    }

    System.Collections.IEnumerator FlashCellsCo(IEnumerable<Vector2Int> cells, Sprite sprite, bool onlyOccupied)
    {
        var made = new List<UnityEngine.UI.Image>();
        foreach (var c in cells)
        {
            if (onlyOccupied && !placed.ContainsKey(c)) continue;

            var img = GetOrCreateOverlayImage("SpecialFlash", gridRoot);
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = new Color(1, 1, 1, 0.9f);
            var rt = img.rectTransform;
            rt.sizeDelta = GetCellSize();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = CellToAnchoredPos(c);
            made.Add(img);
        }

        yield return new WaitForSeconds(0.15f);

        for (int i = 0; i < made.Count; i++)
            ReleaseOverlayImage(made[i]);
    }

    // Handle resizing at runtime
    void OnRectTransformDimensionsChange()
    {
        if (boardRect == null) boardRect = GetComponent<RectTransform>();
        RecomputeCellMetrics();
        ConfigureBoardVfxRoot();

        // Reposition any placed tiles
        var toFix = new List<KeyValuePair<Vector2Int, RectTransform>>(placed);
        foreach (var kv in toFix)
            if (kv.Value) kv.Value.anchoredPosition = CellToAnchoredPos(kv.Key);

        RepositionEffectVisuals();

        DrawGridOverlay();
    }

    public Vector2 GetCellSize() => cellSize; // Cell size getter

    public void SetGridSize(int newWidth, int newHeight)
    {
        newWidth = Mathf.Max(1, newWidth);
        newHeight = Mathf.Max(1, newHeight);

        if (width == newWidth && height == newHeight)
            return;

        width = newWidth;
        height = newHeight;

        if (boardRect == null) boardRect = GetComponent<RectTransform>();

        RecomputeCellMetrics();

        // Reposition any placed tiles
        var toFix = new List<KeyValuePair<Vector2Int, RectTransform>>(placed);
        foreach (var kv in toFix)
            if (kv.Value) kv.Value.anchoredPosition = CellToAnchoredPos(kv.Key);

        RepositionEffectVisuals();
        ConfigureBoardVfxRoot();
        DrawGridOverlay();
    }


    public void SetMonsterAt(Vector2Int cell, MonsterInstance inst)
    {
        if (inst.data)
        {
            inst.RecalcFromData(setHpToMax: false); // Ensure runtime stats exist (and clamp hp to new max)

            // If hp was not initialized, start at full
            if (inst.hp <= 0f)
                inst.hp = inst.maxHp;
        }

        monsters[cell] = inst;
        UpdateTileHPVisual(cell, inst.hp, inst.data ? inst.maxHp : 0f);

        // Initialize healer timer if needed (uses buffed healAmount)
        if (inst.data && inst.healAmount > 0f)
        {
            if (!healTimers.ContainsKey(cell))
                healTimers[cell] = 0f;
        }
        else
        {
            healTimers.Remove(cell);
        }
    }

    void CleanOrphanedTiles()
    {
        cleanOrphanLiveScratch.Clear();
        foreach (var tile in placed.Values)
            if (tile)
                cleanOrphanLiveScratch.Add(tile);

        for (int i = gridRoot.childCount - 1; i >= 0; i--)
        {
            var t = gridRoot.GetChild(i) as RectTransform;
            if (!t) continue;

            if (!t.GetComponent<BoardOwned>()) // Not a tile or grid line, ignore
                continue;

            if (!t.name.StartsWith("Tile_")) // Grid line exception
                continue;

            if (pooledBoardTiles.Contains(t))
                continue;

            if (!cleanOrphanLiveScratch.Contains(t))
                ReleaseBoardTile(t);
        }

        cleanOrphanLiveScratch.Clear();
    }

    public bool TryGetMonster(Vector2Int cell, out MonsterInstance inst) => monsters.TryGetValue(cell, out inst);

    public bool TryGetTileRect(Vector2Int cell, out RectTransform rectTransform)
    {
        return placed.TryGetValue(cell, out rectTransform) && rectTransform;
    }

    public List<Vector2Int> GetMonsterCells(bool includeDead = false)
    {
        var result = new List<Vector2Int>();
        GetMonsterCellsNonAlloc(result, includeDead);
        return result;
    }

    public void GetMonsterCellsNonAlloc(List<Vector2Int> result, bool includeDead = false)
    {
        if (result == null)
            return;

        result.Clear();
        foreach (var kv in monsters)
        {
            if (kv.Value.data == null)
                continue;

            if (!includeDead && kv.Value.hp <= 0f)
                continue;

            result.Add(kv.Key);
        }
    }

    public void RemovePlacedCellImmediate(Vector2Int cell)
    {
        if (obstacles.ContainsKey(cell))
        {
            DestroyObstacleImmediate(cell);
            return;
        }

        if (placed.TryGetValue(cell, out var rt) && rt)
            ReleaseBoardTile(rt);

        StopPortraitAltSwap(cell, restoreNormal: false);
        placed.Remove(cell);
        monsters.Remove(cell);
        healTimers.Remove(cell);
        magicExplosives.Remove(cell);

        RefreshTileBordersAround(cell);
    }

    public bool ForceKillMonsterAndRemove(Vector2Int cell, DamageSource src)
    {
        bool hadMonster = monsters.TryGetValue(cell, out var inst) && inst.data != null;
        if (hadMonster && inst.hp > 0f)
        {
            float applied = inst.hp;
            inst.hp = 0f;
            monsters[cell] = inst;
            UpdateTileHPVisual(cell, inst.hp, inst.maxHp);
            StopPortraitAltSwap(cell, restoreNormal: true);
            TileDamaged?.Invoke(cell, inst.data, applied, src);
            TileDied?.Invoke(cell, inst.data);
        }

        RemovePlacedCellImmediate(cell);
        return hadMonster;
    }

    // ======== Healing VFX ========

    public void PlayHealVFX(Vector2Int cell, Sprite sprite, float duration = 0.5f)
    {
        if (!sprite) return;
        StartCoroutine(PlayHealVFXCo(cell, sprite, duration));
    }

    private System.Collections.IEnumerator PlayHealVFXCo(Vector2Int cell, Sprite sprite, float duration)
    {
        if (!InBounds(cell)) yield break;

        RectTransform parent = overlayRoot ? overlayRoot : gridRoot;
        var img = GetOrCreateOverlayImage("HealVFX", parent);
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.transform.SetAsLastSibling();

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = GetCellSize() - new Vector2(6f, 6f);  // Slight inset
        rt.anchoredPosition = CellToAnchoredPos(cell);

        duration = Mathf.Max(0.01f, duration);
        float t = 0f;
        while (t < duration && img)
        {
            t += Time.deltaTime;
            float a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            img.color = new Color(1f, 1f, 1f, 0.95f * a);
            yield return null;
        }
        ReleaseOverlayImage(img);
    }

    // ================= Public API for ObstacleManager / Piece =================

    public bool HasObstacle(Vector2Int cell) => obstacles.ContainsKey(cell);

    public bool IsFree(Vector2Int c) => InBounds(c) && !placed.ContainsKey(c);

    public int CountFreeCellsInRow(int y)
    {
        if (y < 0 || y >= height) return 0;

        int free = 0;
        for (int x = 0; x < width; x++)
        {
            if (IsFree(new Vector2Int(x, y)))
                free++;
        }
        return free;
    }

    public bool WouldCompleteRowIfFilled(Vector2Int cell)
    {
        if (!InBounds(cell)) return false;
        if (!IsFree(cell)) return false;

        int filled = 0;
        for (int x = 0; x < width; x++)
        {
            var c = new Vector2Int(x, cell.y);
            if (placed.ContainsKey(c))
                filled++;
        }

        return filled >= (width - 1); // If all other cells are already filled
    }

    public bool WouldCompleteRowIfMoved(Vector2Int from, Vector2Int to)
    {
        if (!InBounds(from) || !InBounds(to)) return false;
        if (!IsFree(to)) return false;

        int filled = 0;
        for (int x = 0; x < width; x++)
        {
            var c = new Vector2Int(x, to.y);
            if (c == from)
                continue;

            if (placed.ContainsKey(c))
                filled++;
        }

        return filled >= (width - 1);
    }

    public bool TryGetRandomCellForFloorEffect(
        int excludedTopRows,
        int excludedBottomRows,
        bool requireEmpty,
        out Vector2Int cell)
    {
        cell = default;
        floorEffectCellScratch.Clear();

        if (!TryGetAllowedRowRange(excludedTopRows, excludedBottomRows, out int minY, out int maxY))
            return false;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int c = new Vector2Int(x, y);
                if (!InBounds(c)) continue;
                if (HasFloorEffect(c)) continue;
                if (HasObstacle(c)) continue;
                if (requireEmpty && !IsFree(c)) continue;

                floorEffectCellScratch.Add(c);
            }
        }

        if (floorEffectCellScratch.Count == 0)
            return false;

        cell = floorEffectCellScratch[UnityEngine.Random.Range(0, floorEffectCellScratch.Count)];
        return true;
    }

    public bool TryGetPreferredZipPadBossSpawnCell(
        int excludedTopRows,
        int excludedBottomRows,
        out Vector2Int cell)
    {
        cell = default;

        if (!TryGetAllowedRowRange(excludedTopRows, excludedBottomRows, out int minY, out int maxY))
            return false;

        for (int pass = 0; pass < 2; pass++)
        {
            bool allowRowsWithZipPads = pass > 0;

            for (int y = maxY; y >= minY; y--)
            {
                bool rowHasZipPad = RowHasFloorEffectOfType(y, FloorEffectType.ZipPad);
                if (rowHasZipPad && !allowRowsWithZipPads)
                    continue;

                floorEffectCellScratch.Clear();
                for (int x = 0; x < width; x++)
                {
                    Vector2Int c = new Vector2Int(x, y);
                    if (!IsValidZipPadBossSpawnCell(c))
                        continue;

                    floorEffectCellScratch.Add(c);
                }

                if (floorEffectCellScratch.Count == 0)
                    continue;

                cell = floorEffectCellScratch[UnityEngine.Random.Range(0, floorEffectCellScratch.Count)];
                return true;
            }
        }

        return false;
    }

    bool IsValidZipPadBossSpawnCell(Vector2Int cell)
    {
        if (!InBounds(cell)) return false;
        if (HasFloorEffect(cell)) return false;
        if (HasObstacle(cell)) return false;
        if (!IsFree(cell)) return false;
        return true;
    }

    bool RowHasFloorEffectOfType(int y, FloorEffectType type)
    {
        if (y < 0 || y >= height)
            return false;

        for (int x = 0; x < width; x++)
        {
            if (HasFloorEffectOfType(new Vector2Int(x, y), type))
                return true;
        }

        return false;
    }

    public bool HasAnyFloorEffectSpawnCell(int excludedTopRows, int excludedBottomRows, bool requireEmpty)
    {
        if (!TryGetAllowedRowRange(excludedTopRows, excludedBottomRows, out int minY, out int maxY))
            return false;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int c = new Vector2Int(x, y);
                if (HasFloorEffect(c)) continue;
                if (HasObstacle(c)) continue;
                if (requireEmpty && !IsFree(c)) continue;

                return true;
            }
        }

        return false;
    }

    public bool HasTeleportableMonsterDestination(
        int excludedTopRows,
        int excludedBottomRows,
        bool requireNonCompletingDestination)
    {
        foreach (var kv in monsters)
        {
            if (kv.Value.data == null || kv.Value.hp <= 0f)
                continue;

            CollectTeleportDestinations(
                kv.Key,
                excludedTopRows,
                excludedBottomRows,
                requireNonCompletingDestination,
                teleportSafeDestinationScratch,
                teleportDestinationScratch);

            if (teleportSafeDestinationScratch.Count > 0 ||
                (!requireNonCompletingDestination && teleportDestinationScratch.Count > 0))
                return true;
        }

        return false;
    }

    public bool HasTeleportDestinationForMonster(
        Vector2Int from,
        int excludedTopRows,
        int excludedBottomRows,
        bool requireNonCompletingDestination)
    {
        if (!monsters.TryGetValue(from, out var inst) || inst.data == null || inst.hp <= 0f)
            return false;

        CollectTeleportDestinations(
            from,
            excludedTopRows,
            excludedBottomRows,
            requireNonCompletingDestination,
            teleportSafeDestinationScratch,
            teleportDestinationScratch);

        return teleportSafeDestinationScratch.Count > 0 ||
               (!requireNonCompletingDestination && teleportDestinationScratch.Count > 0);
    }

    public bool TryTeleportMonsterToRandomDestination(
        Vector2Int from,
        int excludedTopRows,
        int excludedBottomRows,
        bool requireNonCompletingDestination,
        out Vector2Int destination,
        bool playSfx = true)
    {
        destination = default;

        if (!InBounds(from)) return false;
        if (!placed.TryGetValue(from, out var rect) || !rect) return false;
        if (!monsters.TryGetValue(from, out var inst) || inst.data == null || inst.hp <= 0f) return false;

        CollectTeleportDestinations(
            from,
            excludedTopRows,
            excludedBottomRows,
            requireNonCompletingDestination,
            teleportSafeDestinationScratch,
            teleportDestinationScratch);

        List<Vector2Int> source = teleportSafeDestinationScratch.Count > 0
            ? teleportSafeDestinationScratch
            : teleportDestinationScratch;

        if (source.Count == 0)
            return false;

        destination = source[UnityEngine.Random.Range(0, source.Count)];
        MoveMonsterTile(from, destination, rect, inst);

        if (playSfx)
            PlayTeleportSFX();

        return true;
    }

    public bool TryActivateTeleportFloorEffect(
        Vector2Int cell,
        int excludedTopRows,
        int excludedBottomRows,
        bool requireNonCompletingDestination,
        bool clearAfterActivation = true)
    {
        if (!HasFloorEffectOfType(cell, FloorEffectType.Teleport))
            return false;

        if (!TryTeleportMonsterToRandomDestination(
            cell,
            excludedTopRows,
            excludedBottomRows,
            requireNonCompletingDestination,
            out _,
            playSfx: true))
            return false;

        if (clearAfterActivation)
            ClearFloorEffect(cell);

        return true;
    }

    void ActivateTeleportFloorEffectsForSettledMonsters()
    {
        if (activatingSettledTeleportFloorEffects || floorEffects.Count == 0)
            return;

        settledTeleportFloorEffectScratch.Clear();

        foreach (var kv in floorEffects)
        {
            if (kv.Value.type != FloorEffectType.Teleport)
                continue;

            if (!monsters.TryGetValue(kv.Key, out var inst) || inst.data == null || inst.hp <= 0f)
                continue;

            settledTeleportFloorEffectScratch.Add(kv.Key);
        }

        if (settledTeleportFloorEffectScratch.Count == 0)
            return;

        activatingSettledTeleportFloorEffects = true;
        try
        {
            for (int i = 0; i < settledTeleportFloorEffectScratch.Count; i++)
            {
                Vector2Int cell = settledTeleportFloorEffectScratch[i];

                if (!floorEffects.TryGetValue(cell, out var fx) || fx.type != FloorEffectType.Teleport)
                    continue;

                if (!monsters.TryGetValue(cell, out var inst) || inst.data == null || inst.hp <= 0f)
                    continue;

                TryActivateTeleportFloorEffect(
                    cell,
                    teleportFloorDestinationExcludedTopRows,
                    teleportFloorDestinationExcludedBottomRows,
                    requireNonCompletingDestination: false,
                    clearAfterActivation: true);
            }
        }
        finally
        {
            activatingSettledTeleportFloorEffects = false;
            settledTeleportFloorEffectScratch.Clear();
        }
    }

    public bool TryTriggerZipPadForActivePiece(Vector2Int cell)
    {
        if (!HasFloorEffectOfType(cell, FloorEffectType.ZipPad))
            return false;

        PlayRandomZipPadSFX();
        return true;
    }

    void CollectTeleportDestinations(
        Vector2Int from,
        int excludedTopRows,
        int excludedBottomRows,
        bool requireNonCompletingDestination,
        List<Vector2Int> safe,
        List<Vector2Int> fallback)
    {
        safe.Clear();
        fallback.Clear();

        if (!TryGetAllowedRowRange(excludedTopRows, excludedBottomRows, out int minY, out int maxY))
            return;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int to = new Vector2Int(x, y);
                if (to == from) continue;
                if (!InBounds(to)) continue;
                if (!IsFree(to)) continue;
                if (HasFloorEffect(to)) continue;
                if (HasObstacle(to)) continue;

                if (!WouldCompleteRowIfMoved(from, to))
                    safe.Add(to);
                else if (!requireNonCompletingDestination)
                    fallback.Add(to);
            }
        }
    }

    bool TryGetAllowedRowRange(int excludedTopRows, int excludedBottomRows, out int minY, out int maxY)
    {
        minY = Mathf.Max(0, excludedBottomRows);
        maxY = height - 1 - Mathf.Max(0, excludedTopRows);

        if (height <= 0 || minY >= height || maxY < 0 || maxY < minY)
            return false;

        minY = Mathf.Clamp(minY, 0, height - 1);
        maxY = Mathf.Clamp(maxY, 0, height - 1);
        return maxY >= minY;
    }

    void MoveMonsterTile(Vector2Int from, Vector2Int to, RectTransform rect, MonsterInstance inst)
    {
        StopPortraitAltSwap(from, restoreNormal: true);

        placed.Remove(from);
        monsters.Remove(from);

        placed[to] = rect;
        monsters[to] = inst;
        rect.anchoredPosition = CellToAnchoredPos(to);
        rect.sizeDelta = cellSize;

        if (healTimers.TryGetValue(from, out var healTimer))
        {
            healTimers.Remove(from);
            healTimers[to] = healTimer;
        }

        if (attackPortraitIdleTimers.TryGetValue(from, out var attackTimer))
        {
            attackPortraitIdleTimers.Remove(from);
            attackPortraitIdleTimers[to] = attackTimer;
        }

        RefreshTileBordersAround(from);
        RefreshTileBordersAround(to);
        RefreshAllTintUnderlays();
    }

    void PlayTeleportSFX()
    {
        if (AudioManager.I && teleportSFX)
            AudioManager.I.PlaySFX(teleportSFX);
    }

    void PlayRandomZipPadSFX()
    {
        if (!AudioManager.I || zipPadSFXClips == null || zipPadSFXClips.Length == 0)
            return;

        for (int i = 0; i < 8; i++)
        {
            AudioClip clip = zipPadSFXClips[UnityEngine.Random.Range(0, zipPadSFXClips.Length)];
            if (!clip) continue;

            AudioManager.I.PlaySFX(clip);
            return;
        }
    }

    public int CountObstaclesOfType(ObstacleType t)
    {
        int count = 0;
        foreach (var kv in obstacles)
            if (kv.Value.type == t)
                count++;
        return count;
    }

    public bool TrySpawnCustomObstacle(Vector2Int cell, ObstacleType type, Sprite sprite, int hitsToBreak = 1, bool indestructible = false)
    {
        return TrySpawnCustomObstacle(cell, type, sprite, hitsToBreak, indestructible, null);
    }

    public bool TrySpawnCustomObstacle(Vector2Int cell, ObstacleType type, Sprite sprite, int hitsToBreak, bool indestructible, Sprite backgroundSprite)
    {
        if (!InBounds(cell)) return false;
        if (!IsFree(cell)) return false;

        var rt = InstantiateTileUI(Color.white, sprite, backgroundSprite ? backgroundSprite : GetBossObstacleBackgroundSprite());
        rt.anchoredPosition = CellToAnchoredPos(cell);
        Place(cell, rt);

        obstacles[cell] = new ObstacleState(type, hitsToBreak, indestructible);
        RefreshTileBordersAround(cell);
        return true;
    }

    public bool HasObstacleOfType(Vector2Int cell, ObstacleType type)
    {
        return obstacles.TryGetValue(cell, out var obs) && obs.type == type;
    }

    public bool TryHandleEnemyProjectileObstacleImpact(Vector2Int cell)
    {
        if (!obstacles.TryGetValue(cell, out var obs))
            return false;

        if (obs.type == ObstacleType.HardenedLava)
            DestroyObstacleImmediate(cell);

        return true;
    }

    public bool TryHandlePlayerAttackObstacleImpact(Vector2Int cell)
    {
        if (!obstacles.TryGetValue(cell, out var obs))
            return false;

        if (obs.type != ObstacleType.HardenedLava)
            return false;

        DestroyObstacleImmediate(cell);
        return true;
    }

    public bool TrySpawnStoneObstacle(Vector2Int cell, int hitsToBreak = 3)
    {
        if (!InBounds(cell)) return false;
        if (!IsFree(cell)) return false;

        var rt = InstantiateTileUI(new Color(0.45f, 0.45f, 0.45f, 1f), stoneUndamagedSprite);
        rt.anchoredPosition = CellToAnchoredPos(cell);
        Place(cell, rt);

        obstacles[cell] = new ObstacleState(ObstacleType.Stone, hitsToBreak);
        UpdateStoneObstacleVisual(cell);
        RefreshTileBordersAround(cell);
        return true;
    }

    public bool TrySpawnMagicPylonObstacle(Vector2Int cell)
    {
        if (!InBounds(cell)) return false;
        if (!IsFree(cell)) return false;
        if (!magicPylonSprite) return false;

        var rt = InstantiateTileUI(new Color(1f, 1f, 1f, 1f), magicPylonSprite, GetBossObstacleBackgroundSprite());
        rt.anchoredPosition = CellToAnchoredPos(cell);
        Place(cell, rt);

        obstacles[cell] = new ObstacleState(ObstacleType.MagicPylon, 1);
        RefreshTileBordersAround(cell);
        return true;
    }

    public bool TrySpawnMagicExplosiveObstacle(Vector2Int cell, float fuseSeconds, int rowClearBonusDamage,
                                               Sprite detonateVFXSprite, AudioClip detonateSFX)
    {
        if (!InBounds(cell)) return false;
        if (!IsFree(cell)) return false;
        if (!magicExplosiveSprite) return false;

        var rt = InstantiateTileUI(explosiveFillColor, magicExplosiveSprite, GetBossObstacleBackgroundSprite());
        rt.anchoredPosition = CellToAnchoredPos(cell);
        Place(cell, rt);

        obstacles[cell] = new ObstacleState(ObstacleType.MagicExplosive, 1);

        // Visual setup
        ConfigureMagicExplosiveVisual(rt);

        magicExplosives[cell] = new MagicExplosiveState
        {
            startTime = Time.time,
            fuseSeconds = Mathf.Max(0.1f, fuseSeconds),
            rowClearBonusDamage = Mathf.Max(0, rowClearBonusDamage),

            detonateVFXSprite = detonateVFXSprite,
            detonateSFX = detonateSFX,

            pulseStage = 0,
            pulseUntil = 0f,
            nextToggleAt = 0f,
            flashOn = false
        };

        RefreshTileBordersAround(cell);
        return true;
    }

    Sprite GetBossObstacleBackgroundSprite()
    {
        if (bossObstacleBackgroundSprite)
            return bossObstacleBackgroundSprite;

        return s_lastTetrominoBackgroundSprite;
    }

    public void SpawnCellOverlayVFX(Vector2Int cell, Sprite sprite, float seconds = 0.5f, float alpha = 0.95f)
    {
        if (!InBounds(cell) || sprite == null) return;

        if (!overlayRoot) overlayRoot = gridRoot;

        var img = GetOrCreateOverlayImage("CellVFX", overlayRoot);
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.color = new Color(1f, 1f, 1f, alpha);

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = cellSize * 0.95f;
        rt.anchoredPosition = CellToAnchoredPos(cell);

        StartCoroutine(FadeAndDestroy(img, seconds));
    }

    void ConfigureMagicExplosiveVisual(RectTransform rt)
    {
        if (!rt) return;

        var backFill = rt.Find("BackFill")?.GetComponent<Image>();
        var fill = rt.Find("HealthFill")?.GetComponent<Image>();
        Sprite textureSprite = fill ? fill.sprite : GetBossObstacleBackgroundSprite();
        bool hasTextureSprite = textureSprite != null && textureSprite != OnePx();

        if (backFill)
        {
            backFill.sprite = hasTextureSprite ? textureSprite : OnePx();
            backFill.color = explosiveBackColor;
            backFill.type = Image.Type.Simple;
            backFill.fillAmount = 1f;
        }

        if (fill)
        {
            fill.sprite = hasTextureSprite ? textureSprite : OnePx();
            fill.color = explosiveBackColor;
            fill.type = Image.Type.Simple;
            fill.fillAmount = 1f;
            ConfigureTetrominoBackgroundPulse(fill, explosiveBackColor, hasTextureSprite ? textureSprite : null);
        }

        var fuse = rt.Find("ExplosiveFuseFill")?.GetComponent<Image>();
        if (!fuse)
        {
            fuse = new GameObject("ExplosiveFuseFill", typeof(Image)).GetComponent<Image>();
            fuse.transform.SetParent(rt, false);

            var fuseRt = fuse.rectTransform;
            fuseRt.anchorMin = fuseRt.anchorMax = new Vector2(0.5f, 0.5f);
            fuseRt.sizeDelta = rt.sizeDelta;
            fuseRt.anchoredPosition = Vector2.zero;
        }

        fuse.sprite = hasTextureSprite ? textureSprite : OnePx();
        fuse.type = Image.Type.Filled;
        fuse.fillMethod = Image.FillMethod.Radial360;
        fuse.fillOrigin = (int)Image.Origin360.Top;
        fuse.fillClockwise = false;
        fuse.fillAmount = 1f;
        fuse.raycastTarget = false;
        fuse.color = new Color(
            explosiveFillColor.r,
            explosiveFillColor.g,
            explosiveFillColor.b,
            Mathf.Clamp01(explosiveFillColor.a * explosiveFuseOverlayAlpha));

        var fillTransform = fill ? fill.rectTransform : null;
        if (fillTransform)
            fuse.rectTransform.SetSiblingIndex(Mathf.Min(fillTransform.GetSiblingIndex() + 1, rt.childCount - 1));

        // Flash overlay (red)
        var flash = rt.Find("ExplosiveFlash")?.GetComponent<Image>();
        if (!flash)
        {
            flash = new GameObject("ExplosiveFlash", typeof(Image)).GetComponent<Image>();
            flash.transform.SetParent(rt, false);

            var frt = flash.rectTransform;
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = rt.sizeDelta;
            frt.anchoredPosition = Vector2.zero;

            flash.sprite = OnePx();
            flash.type = Image.Type.Simple;
            flash.raycastTarget = false;
        }

        flash.color = new Color(explosiveFlashColor.r, explosiveFlashColor.g, explosiveFlashColor.b, 0f);
        flash.rectTransform.SetAsLastSibling();
    }

    void TickMagicExplosives()
    {
        if (magicExplosives.Count == 0) return;

        float now = Time.time;

        var keys = new List<Vector2Int>(magicExplosives.Keys);
        foreach (var cell in keys)
        {
            // If the tile is gone, cleanup
            if (!placed.TryGetValue(cell, out var rt) || !rt || !obstacles.TryGetValue(cell, out var obs) || obs.type != ObstacleType.MagicExplosive)
            {
                magicExplosives.Remove(cell);
                continue;
            }

            var st = magicExplosives[cell];

            float t = Mathf.Clamp01((now - st.startTime) / Mathf.Max(0.1f, st.fuseSeconds));
            float remaining = Mathf.Max(0f, st.fuseSeconds - (now - st.startTime));

            // Radial wipe (1 to 0 over time)
            var fuseFill = rt.Find("ExplosiveFuseFill")?.GetComponent<Image>();
            if (fuseFill)
                fuseFill.fillAmount = 1f - t;

            // Single pulses at 25%, 50%, 75%
            float nextPulseT = (st.pulseStage == 0) ? 0.25f : (st.pulseStage == 1) ? 0.50f : (st.pulseStage == 2) ? 0.75f : 999f;
            if (t >= nextPulseT && st.pulseStage < 3)
            {
                st.pulseStage += 1;
                st.pulseUntil = now + 0.12f; // quick pulse
            }

            // After 75%, start accelerating flash toggles until detonation
            if (t >= 0.75f)
            {
                // Frequency ramps from 2Hz at 75% to 10Hz at 100%
                float ramp = Mathf.InverseLerp(0.75f, 1f, t);
                float hz = Mathf.Lerp(2f, 10f, ramp);
                float interval = 1f / Mathf.Max(0.1f, hz);

                if (st.nextToggleAt <= 0f) st.nextToggleAt = now; // Start immediately

                if (now >= st.nextToggleAt)
                {
                    st.flashOn = !st.flashOn;
                    st.nextToggleAt = now + interval;
                }
            }
            else
            {
                st.flashOn = false;
                st.nextToggleAt = 0f;
            }

            var flash = rt.Find("ExplosiveFlash")?.GetComponent<Image>();
            if (flash)
            {
                float a = 0f;

                if (now <= st.pulseUntil) a = 0.55f; // Pulse wins

                if (st.flashOn) a = Mathf.Max(a, 0.35f); // Accelerating flash

                var c = flash.color;
                c.a = a;
                flash.color = c;
            }

            // Detonate when time is up
            if (remaining <= 0f)
            {
                DetonateMagicExplosive(cell);
                magicExplosives.Remove(cell);
                continue;
            }

            magicExplosives[cell] = st;
        }
    }

    public void DetonateMagicExplosive(Vector2Int cell)
    {
        Sprite vfxSprite = null;
        AudioClip sfx = null;

        if (magicExplosives.TryGetValue(cell, out var st))
        {
            vfxSprite = st.detonateVFXSprite;
            sfx = st.detonateSFX;
        }

        // Play detonation SFX once
        if (AudioManager.I && sfx)
            AudioManager.I.PlaySFX(sfx);

        // Affect surrounding cells (8 neighbors)
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var c = new Vector2Int(cell.x + dx, cell.y + dy);
                if (!InBounds(c)) continue;

                // VFX overlay on each affected cell
                if (vfxSprite && monsters.TryGetValue(c, out var mi) && mi.hp >= 1f)
                    SpawnCellOverlayVFX(c, vfxSprite, 1.0f);

                float before = 0f;
                bool hadLiving = false;
                if (monsters.TryGetValue(c, out var inst) && inst.data != null && inst.hp > 0f)
                {
                    hadLiving = true;
                    before = inst.hp;
                }

                DamageTile(c, 999999f, DamageSource.MagicExplosive); // Massive damage to ensure destruction

                if (hadLiving && monsters.TryGetValue(c, out var instAfter) && instAfter.hp <= 0f)
                {
                    if (PlayerProgress.I)
                        PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.MagicExplosiveKills, 1);
                }
            }

        DestroyObstacleImmediate(cell); // Destroy the explosive tile itself
    }

    public bool SetFloorEffect(Vector2Int cell, FloorEffectType type, float damage, float interval, int ticks)
    {
        if (!InBounds(cell)) return false;

        if (HasFloorEffect(cell)) return false; // Never allow overlapping effects

        switch (type)
        {
            case FloorEffectType.Poison:
                floorEffects[cell] = FloorEffectState.Poison(damage, interval, ticks);
                EnsureBorderOverlay(cell, poisonBorderSprite, poisonBorders);
                EnsureTintUnderlay(cell, poisonUnderlayTint);
                break;

            case FloorEffectType.Burn:
                floorEffects[cell] = FloorEffectState.Burn(damage, interval, ticks);
                EnsureBorderOverlay(cell, fireBorderSprite, fireBorders);
                EnsureTintUnderlay(cell, fireUnderlayTint);
                break;

            case FloorEffectType.Spike:
                floorEffects[cell] = FloorEffectState.Spike(damage);
                EnsureSpikeUnderlay(cell);
                break;

            case FloorEffectType.Lightning:
                floorEffects[cell] = FloorEffectState.Lightning(damage, interval, ticks);
                EnsureBorderOverlay(cell, lightningBorderSprite, lightningBorders);
                EnsureTintUnderlay(cell, lightningUnderlayTint);
                break;

            case FloorEffectType.Teleport:
                floorEffects[cell] = FloorEffectState.Teleport();
                EnsureTeleportUnderlay(cell);
                break;

            case FloorEffectType.ZipPad:
                floorEffects[cell] = FloorEffectState.ZipPad();
                EnsureZipPadUnderlay(cell);
                break;
        }

        return true;
    }

    public void ClearFloorEffect(Vector2Int cell)
    {
        floorEffects.Remove(cell);

        if (poisonBorders.TryGetValue(cell, out var p) && p) Destroy(p.gameObject);
        poisonBorders.Remove(cell);

        if (fireBorders.TryGetValue(cell, out var f) && f) Destroy(f.gameObject);
        fireBorders.Remove(cell);

        if (spikeVisuals.TryGetValue(cell, out var sv) && sv != null && sv.root) Destroy(sv.root.gameObject);
        spikeVisuals.Remove(cell);

        if (lightningBorders.TryGetValue(cell, out var l) && l) Destroy(l.gameObject);
        lightningBorders.Remove(cell);

        if (teleportVisuals.TryGetValue(cell, out var tv) && tv != null && tv.root) Destroy(tv.root.gameObject);
        teleportVisuals.Remove(cell);

        if (zipPadVisuals.TryGetValue(cell, out var zv) && zv != null && zv.root) Destroy(zv.root.gameObject);
        zipPadVisuals.Remove(cell);

        if (floorTintUnderlays.TryGetValue(cell, out var tint) && tint) Destroy(tint.gameObject);
        floorTintUnderlays.Remove(cell);
        ClearSecondaryTintUnderlay(cell);
    }

    public void ApplyFloorEffectOnPlacement(Vector2Int cell)
    {
        if (!floorEffects.TryGetValue(cell, out var fx)) return;

        if (fx.type == FloorEffectType.Spike)
        {
            TryDamageMonsterFromFloorEffect(cell, fx.type, fx.damage);

            ClearFloorEffect(cell);
        }
        else if (fx.type == FloorEffectType.Teleport)
        {
            TryActivateTeleportFloorEffect(
                cell,
                teleportFloorDestinationExcludedTopRows,
                teleportFloorDestinationExcludedBottomRows,
                requireNonCompletingDestination: false,
                clearAfterActivation: true);
        }
    }

    public void ApplySpecialToEnvironment(IEnumerable<Vector2Int> affectedCells, SpecialType special)
    {
        if (affectedCells == null) return;

        if (special == SpecialType.Bomb)
        {
            foreach (var c in affectedCells)
            {
                if (!InBounds(c)) continue;
                if (obstacles.TryGetValue(c, out var obs) &&
                    (obs.type == ObstacleType.Stone || obs.type == ObstacleType.HardenedLava))
                {
                    DestroyObstacleImmediate(c);
                }
            }
        }
        else if (special == SpecialType.Bolt)
        {
            foreach (var c in affectedCells)
            {
                if (!InBounds(c)) continue;
                if (floorEffects.TryGetValue(c, out var fx) && fx.type == FloorEffectType.Spike)
                {
                    ClearFloorEffect(c);
                }

                if (obstacles.TryGetValue(c, out var obs) && obs.type == ObstacleType.HardenedLava)
                    DestroyObstacleImmediate(c);
            }
        }

        if (_gc && _gc.levelModifierController)
            _gc.levelModifierController.OnSpecialEnvironmentHit(affectedCells, special);
    }

    // ================= Visuals =================

    void EnsureBorderOverlay(Vector2Int cell, Sprite sprite, Dictionary<Vector2Int, Image> dict)
    {
        if (!overlayRoot) return;

        // If sprite not provided, just clear any existing
        if (!sprite)
        {
            if (dict.TryGetValue(cell, out var old) && old) Destroy(old.gameObject);
            dict.Remove(cell);
            return;
        }

        if (dict.TryGetValue(cell, out var existing) && existing)
        {
            existing.sprite = sprite;
            existing.rectTransform.sizeDelta = GetCellSize();
            existing.rectTransform.anchoredPosition = CellToAnchoredPos(cell);
            existing.rectTransform.SetAsLastSibling();
            return;
        }

        var img = new GameObject($"Border_{cell.x}_{cell.y}", typeof(Image)).GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.SetParent(overlayRoot, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = GetCellSize();
        rt.anchoredPosition = CellToAnchoredPos(cell);
        rt.SetAsLastSibling(); // always above blocks

        dict[cell] = img;
    }

    void EnsureSpikeUnderlay(Vector2Int cell)
    {
        if (!underlayRoot) return;

        Vector2 cellSize = GetCellSize();
        Vector2 spikeSize = GetSpikeVisualSize(cellSize);

        if (spikeVisuals.TryGetValue(cell, out var existing) && existing != null && existing.root)
        {
            // Update sizing/pos
            existing.root.sizeDelta = cellSize;
            existing.root.anchoredPosition = CellToAnchoredPos(cell);
            if (existing.low) existing.low.rectTransform.sizeDelta = spikeSize;
            if (existing.high) existing.high.rectTransform.sizeDelta = spikeSize;
            return;
        }

        var root = new GameObject($"Spike_{cell.x}_{cell.y}", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(underlayRoot, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = cellSize;
        root.anchoredPosition = CellToAnchoredPos(cell);

        Image MakeFrame(string name, Sprite s, bool enabled)
        {
            var img = new GameObject(name, typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(root, false);
            img.sprite = s;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = false;

            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = spikeSize;
            rt.anchoredPosition = Vector2.zero;

            img.enabled = enabled;
            return img;
        }

        var low = MakeFrame("SpikeLow", spikeSpriteLow, true);
        var high = MakeFrame("SpikeHigh", spikeSpriteHigh, false);

        spikeVisuals[cell] = new SpikeVisual
        {
            root = root,
            low = low,
            high = high,
            highOn = false,
            nextToggleTime = Time.time + spikeAnimInterval
        };
    }

    Vector2 GetSpikeVisualSize(Vector2 cellSize)
    {
        return cellSize * Mathf.Clamp(spikeTrapVisualScale, 0.1f, 1f);
    }

    void AnimateSpikes()
    {
        if (spikeVisuals.Count == 0) return;

        float now = Time.time;
        foreach (var kv in spikeVisuals)
        {
            var sv = kv.Value;
            if (sv == null || !sv.root) continue;

            if (now >= sv.nextToggleTime)
            {
                sv.highOn = !sv.highOn;
                if (sv.low) sv.low.enabled = !sv.highOn;
                if (sv.high) sv.high.enabled = sv.highOn;
                sv.nextToggleTime = now + spikeAnimInterval;
            }
        }
    }

    void EnsureTeleportUnderlay(Vector2Int cell)
    {
        if (!underlayRoot) return;

        if (!teleportWarningSprite)
        {
            if (teleportVisuals.TryGetValue(cell, out var old) && old != null && old.root)
                Destroy(old.root.gameObject);
            teleportVisuals.Remove(cell);
            return;
        }

        Vector2 cellSize = GetCellSize();
        Vector2 visualSize = cellSize * Mathf.Clamp(teleportFloorVisualScale, 0.1f, 1f);

        if (teleportVisuals.TryGetValue(cell, out var existing) && existing != null && existing.root)
        {
            existing.root.sizeDelta = cellSize;
            existing.root.anchoredPosition = CellToAnchoredPos(cell);
            if (existing.image)
            {
                existing.image.sprite = teleportWarningSprite;
                existing.image.rectTransform.sizeDelta = visualSize;
            }
            return;
        }

        var img = new GameObject($"TeleportFloor_{cell.x}_{cell.y}", typeof(Image)).GetComponent<Image>();
        img.sprite = teleportWarningSprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.color = Color.white;

        var rt = img.rectTransform;
        rt.SetParent(underlayRoot, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = visualSize;
        rt.anchoredPosition = CellToAnchoredPos(cell);
        rt.localScale = Vector3.one;

        teleportVisuals[cell] = new TeleportVisual { root = rt, image = img };
    }

    void AnimateTeleportFloorEffects()
    {
        if (teleportVisuals.Count == 0) return;

        float degrees = Mathf.Abs(teleportFloorRotationDegreesPerSecond) * Time.deltaTime;
        foreach (var kv in teleportVisuals)
        {
            var visual = kv.Value;
            if (visual == null || !visual.root) continue;
            visual.root.Rotate(0f, 0f, -degrees);
        }
    }

    void EnsureZipPadUnderlay(Vector2Int cell)
    {
        if (!underlayRoot) return;

        if (!zipPadSprite)
        {
            if (zipPadVisuals.TryGetValue(cell, out var old) && old != null && old.root)
                Destroy(old.root.gameObject);
            zipPadVisuals.Remove(cell);
            return;
        }

        Vector2 cellSize = GetCellSize();

        if (zipPadVisuals.TryGetValue(cell, out var existing) && existing != null && existing.root)
        {
            existing.root.sizeDelta = cellSize;
            existing.root.anchoredPosition = CellToAnchoredPos(cell);
            ConfigureZipPadBackdrop(existing, cellSize);
            ResizeZipPadArrows(existing, cellSize);
            PositionZipPadArrows(existing, cellSize);
            return;
        }

        var root = new GameObject($"ZipPad_{cell.x}_{cell.y}", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        root.SetParent(underlayRoot, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = cellSize;
        root.anchoredPosition = CellToAnchoredPos(cell);
        root.localScale = Vector3.one;
        root.localRotation = Quaternion.identity;

        var backdrop = new GameObject("ZipPadBackdrop", typeof(Image)).GetComponent<Image>();
        backdrop.transform.SetParent(root, false);
        backdrop.sprite = OnePx();
        backdrop.type = Image.Type.Simple;
        backdrop.preserveAspect = false;
        backdrop.raycastTarget = false;

        const int arrowCopies = 5;
        var arrows = new Image[arrowCopies];
        for (int i = 0; i < arrows.Length; i++)
        {
            var img = new GameObject($"ZipPadArrow_{i}", typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(root, false);
            img.sprite = zipPadSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = Color.white;

            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            arrows[i] = img;
        }

        var visual = new ZipPadVisual
        {
            root = root,
            backdrop = backdrop,
            arrows = arrows,
            offset = 0f
        };

        ConfigureZipPadBackdrop(visual, cellSize);
        ResizeZipPadArrows(visual, cellSize);
        PositionZipPadArrows(visual, cellSize);
        zipPadVisuals[cell] = visual;
    }

    void ConfigureZipPadBackdrop(ZipPadVisual visual, Vector2 cellSize)
    {
        if (visual == null || !visual.root)
            return;

        if (!visual.backdrop)
        {
            visual.backdrop = new GameObject("ZipPadBackdrop", typeof(Image)).GetComponent<Image>();
            visual.backdrop.transform.SetParent(visual.root, false);
        }

        visual.backdrop.sprite = OnePx();
        visual.backdrop.type = Image.Type.Simple;
        visual.backdrop.preserveAspect = false;
        visual.backdrop.raycastTarget = false;
        visual.backdrop.color = zipPadBackdropTint;

        RectTransform rt = visual.backdrop.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = cellSize;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.SetAsFirstSibling();
    }

    void ResizeZipPadArrows(ZipPadVisual visual, Vector2 cellSize)
    {
        if (visual == null || visual.arrows == null) return;

        Vector2 arrowSize = cellSize * Mathf.Clamp(zipPadVisualScale, 0.1f, 1f);
        for (int i = 0; i < visual.arrows.Length; i++)
        {
            if (!visual.arrows[i]) continue;
            visual.arrows[i].sprite = zipPadSprite;
            visual.arrows[i].rectTransform.sizeDelta = arrowSize;
        }
    }

    void PositionZipPadArrows(ZipPadVisual visual, Vector2 cellSize)
    {
        if (visual == null || visual.arrows == null || visual.arrows.Length == 0) return;

        float spacing = Mathf.Max(1f, cellSize.y / 3f);
        float top = cellSize.y * 0.5f + spacing;
        float wrap = spacing * visual.arrows.Length;
        float offset = Mathf.Repeat(visual.offset, spacing);

        for (int i = 0; i < visual.arrows.Length; i++)
        {
            if (!visual.arrows[i]) continue;

            float y = top - (i * spacing) - offset;
            while (y < -cellSize.y * 0.5f - spacing)
                y += wrap;

            visual.arrows[i].rectTransform.anchoredPosition = new Vector2(0f, y);
        }
    }

    void AnimateZipPads()
    {
        if (zipPadVisuals.Count == 0) return;

        Vector2 cellSize = GetCellSize();
        float spacing = Mathf.Max(1f, cellSize.y / 3f);
        float speed = Mathf.Max(1f, zipPadScrollPixelsPerSecond);

        foreach (var kv in zipPadVisuals)
        {
            var visual = kv.Value;
            if (visual == null || !visual.root) continue;

            visual.offset = Mathf.Repeat(visual.offset + speed * Time.deltaTime, spacing);
            PositionZipPadArrows(visual, cellSize);
        }
    }

    // ================= Floor Effect Ticking =================

    void TickFloorEffects()
    {
        if (floorEffects.Count == 0) return;

        float now = Time.time;

        // Snapshot keys to allow mutation without allocating each frame.
        floorEffectKeysScratch.Clear();
        foreach (var key in floorEffects.Keys)
            floorEffectKeysScratch.Add(key);

        foreach (var cell in floorEffectKeysScratch)
        {
            if (!floorEffects.TryGetValue(cell, out var fx)) continue;

            if (fx.type == FloorEffectType.Spike ||
                fx.type == FloorEffectType.Teleport ||
                fx.type == FloorEffectType.ZipPad)
                continue; // These effects are handled by placement/activation logic only

            // Keep border overlays on top
            if (fx.type == FloorEffectType.Poison && poisonBorders.TryGetValue(cell, out var p) && p)
                p.rectTransform.SetAsLastSibling();
            else if (fx.type == FloorEffectType.Burn && fireBorders.TryGetValue(cell, out var f) && f)
                f.rectTransform.SetAsLastSibling();
            else if (fx.type == FloorEffectType.Lightning && lightningBorders.TryGetValue(cell, out var l) && l)
                l.rectTransform.SetAsLastSibling();

            if (now < fx.nextTickTime)
                continue;

            float tickDamage = fx.damage;
            bool poisonHadTarget = false;
            if (fx.type == FloorEffectType.Poison)
                tickDamage = GetPoisonTickDamage(cell, ref fx, out poisonHadTarget);

            TryDamageMonsterFromFloorEffect(cell, fx.type, tickDamage);

            if (poisonHadTarget)
                AdvancePoisonDamageRamp(ref fx);

            if (fx.ticksRemaining > 0)
                fx.ticksRemaining--;

            if (fx.ticksRemaining == 0)
            {
                ClearFloorEffect(cell);
                continue;
            }

            fx.nextTickTime = now + fx.interval;
            floorEffects[cell] = fx;
        }
    }

    float GetPoisonTickDamage(Vector2Int cell, ref FloorEffectState fx, out bool hadTarget)
    {
        hadTarget = false;

        if (!monsters.TryGetValue(cell, out var inst) || inst.data == null || inst.hp <= 0f)
        {
            ResetPoisonDamageRamp(ref fx);
            return fx.damage;
        }

        hadTarget = true;
        int targetDataId = inst.data.GetInstanceID();
        if (fx.poisonTargetDataId != targetDataId || fx.poisonTargetPieceGroupId != inst.pieceGroupId)
        {
            fx.poisonTargetDataId = targetDataId;
            fx.poisonTargetPieceGroupId = inst.pieceGroupId;
            fx.poisonTicksAppliedToTarget = 0;
        }

        float maxMultiplier = Mathf.Max(1f, poisonMaxDamageMultiplier);
        float multiplier = 1f + Mathf.Max(0, fx.poisonTicksAppliedToTarget) * Mathf.Max(0f, poisonDamageRampPerTick);
        return fx.damage * Mathf.Min(maxMultiplier, multiplier);
    }

    void AdvancePoisonDamageRamp(ref FloorEffectState fx)
    {
        float maxMultiplier = Mathf.Max(1f, poisonMaxDamageMultiplier);
        float rampPerTick = Mathf.Max(0f, poisonDamageRampPerTick);
        if (rampPerTick <= 0f || maxMultiplier <= 1f)
            return;

        int maxRampTicks = Mathf.CeilToInt((maxMultiplier - 1f) / rampPerTick);
        fx.poisonTicksAppliedToTarget = Mathf.Min(maxRampTicks, fx.poisonTicksAppliedToTarget + 1);
    }

    void ResetPoisonDamageRamp(ref FloorEffectState fx)
    {
        fx.poisonTargetDataId = 0;
        fx.poisonTargetPieceGroupId = 0;
        fx.poisonTicksAppliedToTarget = 0;
    }

    public void FlashMonsterTick(Vector2Int cell, Color tint)
    {
        if (!placed.TryGetValue(cell, out var rt) || !rt) return;

        // Prefer flashing the portrait if present, otherwise flash the tile back/fill
        var portrait = rt.Find("MonsterPortrait")?.GetComponent<Image>();
        var backFill = rt.Find("BackFill")?.GetComponent<Image>();
        var healthFill = rt.Find("HealthFill")?.GetComponent<Image>();

        if (!portrait && !backFill && !healthFill) return; // If nothing to flash, bail

        // Restart if already flashing this cell
        if (_monsterFlashCo.TryGetValue(cell, out var running) && running != null)
            StopCoroutine(running);

        _monsterFlashCo[cell] = StartCoroutine(CoFlashMonsterTick(cell, portrait, backFill, healthFill, tint));
    }

    IEnumerator CoFlashMonsterTick(Vector2Int cell, Image portrait, Image backFill, Image healthFill, Color tint)
    {
        // Cache originals
        Color p0 = portrait ? portrait.color : Color.white;
        Color b0 = backFill ? backFill.color : Color.white;
        Color h0 = healthFill ? healthFill.color : Color.white;

        // Apply tint
        if (portrait) portrait.color = tint;
        if (backFill) backFill.color = tint;
        if (healthFill) healthFill.color = tint;

        float t = 0f;
        float dur = Mathf.Max(0.01f, monsterTickFlashDuration);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime; // Tick flashes should still work during pause/slow time
            float a = t / dur;

            if (portrait) portrait.color = Color.Lerp(tint, p0, a);
            if (backFill) backFill.color = Color.Lerp(tint, b0, a);
            if (healthFill) healthFill.color = Color.Lerp(tint, h0, a);

            yield return null;
        }

        // Restore exact originals
        if (portrait) portrait.color = p0;
        if (backFill) backFill.color = b0;
        if (healthFill) healthFill.color = h0;

        _monsterFlashCo.Remove(cell);
    }

    bool TryDamageMonsterFromFloorEffect(Vector2Int cell, FloorEffectType fxType, float damage)
    {
        if (!monsters.TryGetValue(cell, out var inst) || inst.data == null || inst.hp <= 0f)
            return false;

        float before = inst.hp;

        DamageSource src = fxType switch
        {
            FloorEffectType.Poison => DamageSource.FloorPoison,
            FloorEffectType.Burn => DamageSource.FloorBurn,
            FloorEffectType.Spike => DamageSource.FloorSpike,
            FloorEffectType.Lightning => DamageSource.FloorLightning,
            _ => DamageSource.Generic
        };

        DamageTile(cell, damage, src);

        float after = monsters.TryGetValue(cell, out var instAfter) ? instAfter.hp : 0f;
        float applied = Mathf.Max(0f, before - after);
        if (applied <= 0f)
            return true;

        if (PlayerProgress.I)
        {
            if (fxType == FloorEffectType.Poison)
                PlayerProgress.I.AddLifetimeFloat(AchievementSystem.Stat.PoisonDamage, applied);
            else if (fxType == FloorEffectType.Burn)
                PlayerProgress.I.AddLifetimeFloat(AchievementSystem.Stat.FireDamage, applied);
            else if (fxType == FloorEffectType.Spike)
                PlayerProgress.I.AddLifetimeFloat(AchievementSystem.Stat.SpikeDamage, applied);
        }

        // Tint flash
        if (fxType == FloorEffectType.Burn)
            FlashMonsterTick(cell, burnTickFlashTint);
        else if (fxType == FloorEffectType.Poison)
            FlashMonsterTick(cell, poisonTickFlashTint);
        else if (fxType == FloorEffectType.Lightning)
            FlashMonsterTick(cell, lightningTickFlashTint);

        return true;
    }

    // ================= Obstacle destruction + sprite updates =================

    void DestroyObstacleImmediate(Vector2Int cell)
    {
        ObstacleType type = ObstacleType.Stone;
        if (obstacles.TryGetValue(cell, out var obs))
            type = obs.type;

        if (placed.TryGetValue(cell, out var rt) && rt)
            ReleaseBoardTile(rt);

        StopPortraitAltSwap(cell, restoreNormal: false);
        placed.Remove(cell);
        monsters.Remove(cell);

        obstacles.Remove(cell);
        magicExplosives.Remove(cell);

        // Achievement tracking for stone destruction
        if (type == ObstacleType.Stone && PlayerProgress.I)
            PlayerProgress.I.AddLifetimeInt(AchievementSystem.Stat.StoneDestroyed, 1);

        ObstacleDestroyed?.Invoke(cell, type);

        RefreshTileBordersAround(cell);
    }

    bool TryHandleObstacleAt(Vector2Int cell,
                         HashSet<Vector2Int> stoneGuard,
                         List<Vector2Int> removedCells,
                         bool detonateExplosive)
    {
        if (!obstacles.TryGetValue(cell, out var obs))
            return false;

        switch (obs.type)
        {
            case ObstacleType.Stone:
                {
                    if (obs.indestructible)
                    {
                        StopPortraitAltSwap(cell, restoreNormal: false);
                        monsters.Remove(cell);
                        return true;
                    }

                    if (stoneGuard != null && !stoneGuard.Add(cell))
                    {
                        StopPortraitAltSwap(cell, restoreNormal: false);
                        monsters.Remove(cell);
                        return true;
                    }

                    obs.hitsRemaining = Mathf.Max(0, obs.hitsRemaining - 1);

                    if (obs.hitsRemaining <= 0)
                    {
                        DestroyObstacleImmediate(cell);
                        removedCells?.Add(cell);
                    }
                    else
                    {
                        obstacles[cell] = obs;
                        UpdateStoneObstacleVisual(cell);
                    }

                    StopPortraitAltSwap(cell, restoreNormal: false);
                    monsters.Remove(cell);
                    return true;
                }

            case ObstacleType.MagicPylon:
                {
                    DestroyObstacleImmediate(cell);
                    removedCells?.Add(cell);
                    return true;
                }

            case ObstacleType.MagicExplosive:
                {
                    // Row-clear success = no detonation
                    // Special-block removal = detonate immediately
                    if (detonateExplosive)
                        DetonateMagicExplosive(cell);
                    else
                        DestroyObstacleImmediate(cell);

                    removedCells?.Add(cell);
                    return true;
                }

            case ObstacleType.SoldierWall:
                {
                    StopPortraitAltSwap(cell, restoreNormal: false);
                    monsters.Remove(cell);
                    return true;
                }

            case ObstacleType.Overgrowth:
                {
                    if (detonateExplosive)
                    {
                        DestroyObstacleImmediate(cell);
                        removedCells?.Add(cell);
                    }
                    else
                    {
                        StopPortraitAltSwap(cell, restoreNormal: false);
                        monsters.Remove(cell);
                    }

                    return true;
                }

            case ObstacleType.HardenedLava:
                {
                    if (obs.indestructible)
                    {
                        StopPortraitAltSwap(cell, restoreNormal: false);
                        monsters.Remove(cell);
                        return true;
                    }

                    obs.hitsRemaining = Mathf.Max(0, obs.hitsRemaining - 1);

                    if (obs.hitsRemaining <= 0)
                    {
                        DestroyObstacleImmediate(cell);
                        removedCells?.Add(cell);
                    }
                    else
                    {
                        obstacles[cell] = obs;
                    }

                    StopPortraitAltSwap(cell, restoreNormal: false);
                    monsters.Remove(cell);
                    return true;
                }
        }

        return false;
    }

    void UpdateStoneObstacleVisual(Vector2Int cell)
    {
        if (!obstacles.TryGetValue(cell, out var obs)) return;
        if (obs.type != ObstacleType.Stone) return;

        if (!placed.TryGetValue(cell, out var rt) || !rt) return;

        var img = rt.Find("MonsterPortrait")?.GetComponent<Image>();
        if (!img) return;

        Sprite s = stoneUndamagedSprite;

        // 0 hits -> undamaged, 1 hit -> damaged, 2+ hits -> critical
        int taken = Mathf.Clamp(obs.HitsTaken, 0, 2);
        if (taken == 0) s = stoneUndamagedSprite;
        else if (taken == 1) s = stoneDamagedSprite ? stoneDamagedSprite : stoneUndamagedSprite;
        else s = stoneCriticalSprite ? stoneCriticalSprite : (stoneDamagedSprite ? stoneDamagedSprite : stoneUndamagedSprite);

        img.sprite = s;
        img.enabled = (img.sprite != null);
    }

    public void SetContagionFloorUnderlays(Vector2Int cell, Color mainTint, Color secondaryTint)
    {
        EnsureTintUnderlay(cell, mainTint);
        EnsureSecondaryTintUnderlay(cell, secondaryTint);
    }

    public void ClearContagionFloorUnderlays(Vector2Int cell)
    {
        if (floorTintUnderlays.TryGetValue(cell, out var img) && img)
            Destroy(img.gameObject);

        floorTintUnderlays.Remove(cell);
        ClearSecondaryTintUnderlay(cell);
    }

    // ================= Boss Warning Flashing =================

    public Coroutine FlashWarningAtCell(Vector2Int cell, Sprite warningSprite, float seconds, float toggleInterval = 0.08f, float alpha = 0.65f)
    {
        return StartCoroutine(FlashWarningRoutine(cell, warningSprite, seconds, toggleInterval, alpha));
    }

    public Coroutine FlashRotatingWarningAtCell(
        Vector2Int cell,
        Sprite warningSprite,
        float seconds,
        float toggleInterval = 0.08f,
        float alpha = 0.65f,
        float rotationDegreesPerSecond = 360f)
    {
        return StartCoroutine(FlashRotatingWarningRoutine(
            cell,
            warningSprite,
            seconds,
            toggleInterval,
            alpha,
            rotationDegreesPerSecond));
    }

    public Coroutine FlashTintAtCell(Vector2Int cell, Color tint, float seconds, float toggleInterval = 0.08f)
    {
        return StartCoroutine(FlashTintRoutine(cell, tint, seconds, toggleInterval));
    }

    IEnumerator FlashWarningRoutine(Vector2Int cell, Sprite warningSprite, float seconds, float toggleInterval, float alpha)
    {
        if (!InBounds(cell) || warningSprite == null) yield break;

        if (!overlayRoot) overlayRoot = gridRoot;

        var img = GetOrCreateOverlayImage("BossWarning", overlayRoot);
        img.sprite = warningSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        // Slightly transparent
        alpha = Mathf.Clamp01(alpha);
        img.color = new Color(1f, 1f, 1f, alpha);

        var rt = img.rectTransform;
        rt.sizeDelta = cellSize;
        rt.anchoredPosition = CellToAnchoredPos(cell);

        float t = 0f;
        float tog = 0f;
        bool on = true;

        while (img && t < seconds)
        {
            float dt = Time.deltaTime;
            t += dt;
            tog += dt;
            if (tog >= toggleInterval)
            {
                tog = 0f;
                on = !on;
                if (!img)
                    yield break;

                img.enabled = on;
            }
            yield return null;
        }

        ReleaseOverlayImage(img);
    }

    IEnumerator FlashRotatingWarningRoutine(
        Vector2Int cell,
        Sprite warningSprite,
        float seconds,
        float toggleInterval,
        float alpha,
        float rotationDegreesPerSecond)
    {
        if (!InBounds(cell) || warningSprite == null) yield break;

        if (!overlayRoot) overlayRoot = gridRoot;

        var img = GetOrCreateOverlayImage("BossRotatingWarning", overlayRoot);
        img.sprite = warningSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));

        var rt = img.rectTransform;
        rt.sizeDelta = cellSize;
        rt.anchoredPosition = CellToAnchoredPos(cell);
        rt.localEulerAngles = Vector3.zero;

        float t = 0f;
        float tog = 0f;
        bool on = true;
        float degreesPerSecond = Mathf.Abs(rotationDegreesPerSecond);

        while (img && t < seconds)
        {
            float dt = Time.deltaTime;
            t += dt;
            tog += dt;

            rt.localEulerAngles = new Vector3(0f, 0f, -degreesPerSecond * t);

            if (tog >= toggleInterval)
            {
                tog = 0f;
                on = !on;
                if (!img)
                    yield break;

                img.enabled = on;
            }
            yield return null;
        }

        ReleaseOverlayImage(img);
    }

    IEnumerator FlashTintRoutine(Vector2Int cell, Color tint, float seconds, float toggleInterval)
    {
        if (!InBounds(cell)) yield break;

        if (!overlayRoot) overlayRoot = gridRoot;

        var img = GetOrCreateOverlayImage("CellWarningTint", overlayRoot);
        img.sprite = OnePx();
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.raycastTarget = false;
        img.color = tint;

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = cellSize;
        rt.anchoredPosition = CellToAnchoredPos(cell);

        float t = 0f;
        float tog = 0f;
        bool on = true;

        while (img && t < seconds)
        {
            float dt = Time.deltaTime;
            t += dt;
            tog += dt;
            if (tog >= toggleInterval)
            {
                tog = 0f;
                on = !on;
                if (!img)
                    yield break;

                img.enabled = on;
            }
            yield return null;
        }

        ReleaseOverlayImage(img);
    }

    // ================= Floor Tint Underlays =================

    void EnsureTintUnderlay(Vector2Int cell, Color tint)
    {
        if (!underlayRoot) return;

        // Create if missing
        if (!floorTintUnderlays.TryGetValue(cell, out var img) || !img)
        {
            img = new GameObject("FloorTint", typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            img.raycastTarget = false;
            img.sprite = OnePx();
            img.type = UnityEngine.UI.Image.Type.Simple;

            var rt = img.rectTransform;
            rt.SetParent(underlayRoot, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = GetCellSize();
            rt.anchoredPosition = CellToAnchoredPos(cell);

            floorTintUnderlays[cell] = img;
        }

        // Apply tint + alpha
        var c = tint;
        c.a = floorUnderlayAlpha;
        img.color = c;

        // Only visible when empty
        img.enabled = IsFree(cell);
    }

    void EnsureSecondaryTintUnderlay(Vector2Int cell, Color tint)
    {
        if (!InBounds(cell)) return;

        if (!floorSecondaryTintUnderlays.TryGetValue(cell, out var img) || !img)
        {
            img = new GameObject($"FloorSecondaryTint_{cell.x}_{cell.y}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();

            img.raycastTarget = false;
            img.sprite = OnePx();
            img.type = UnityEngine.UI.Image.Type.Simple;

            var rt = img.rectTransform;
            rt.SetParent(underlayRoot, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = GetCellSize();
            rt.anchoredPosition = CellToAnchoredPos(cell);

            floorSecondaryTintUnderlays[cell] = img;
        }

        var c = tint;
        c.a = floorSecondaryUnderlayAlpha;
        img.color = c;

        img.enabled = IsFree(cell);
    }

    void ClearSecondaryTintUnderlay(Vector2Int cell)
    {
        if (floorSecondaryTintUnderlays.TryGetValue(cell, out var img) && img)
            Destroy(img.gameObject);

        floorSecondaryTintUnderlays.Remove(cell);
    }

    void RefreshSecondaryTintUnderlayVisibility(Vector2Int cell)
    {
        if (floorSecondaryTintUnderlays.TryGetValue(cell, out var img) && img)
            img.enabled = IsFree(cell);
    }

    void RefreshAllTintUnderlays()
    {
        foreach (var kv in floorTintUnderlays)
            if (kv.Value) kv.Value.enabled = IsFree(kv.Key);

        foreach (var kv in floorSecondaryTintUnderlays)
            if (kv.Value) kv.Value.enabled = IsFree(kv.Key);
    }

    // ================= Resize support =================

    void RepositionEffectVisuals()
    {
        foreach (var kv in poisonBorders)
            if (kv.Value) { kv.Value.rectTransform.sizeDelta = GetCellSize(); kv.Value.rectTransform.anchoredPosition = CellToAnchoredPos(kv.Key); kv.Value.rectTransform.SetAsLastSibling(); }

        foreach (var kv in fireBorders)
            if (kv.Value) { kv.Value.rectTransform.sizeDelta = GetCellSize(); kv.Value.rectTransform.anchoredPosition = CellToAnchoredPos(kv.Key); kv.Value.rectTransform.SetAsLastSibling(); }

        foreach (var kv in lightningBorders)
            if (kv.Value) { kv.Value.rectTransform.sizeDelta = GetCellSize(); kv.Value.rectTransform.anchoredPosition = CellToAnchoredPos(kv.Key); kv.Value.rectTransform.SetAsLastSibling(); }

        foreach (var kv in spikeVisuals)
            if (kv.Value != null && kv.Value.root)
            {
                Vector2 cellSize = GetCellSize();
                Vector2 spikeSize = GetSpikeVisualSize(cellSize);
                kv.Value.root.sizeDelta = cellSize;
                kv.Value.root.anchoredPosition = CellToAnchoredPos(kv.Key);
                if (kv.Value.low) kv.Value.low.rectTransform.sizeDelta = spikeSize;
                if (kv.Value.high) kv.Value.high.rectTransform.sizeDelta = spikeSize;
            }

        foreach (var kv in teleportVisuals)
            if (kv.Value != null && kv.Value.root)
            {
                Vector2 size = GetCellSize();
                kv.Value.root.sizeDelta = size * Mathf.Clamp(teleportFloorVisualScale, 0.1f, 1f);
                kv.Value.root.anchoredPosition = CellToAnchoredPos(kv.Key);
            }

        foreach (var kv in zipPadVisuals)
            if (kv.Value != null && kv.Value.root)
            {
                Vector2 size = GetCellSize();
                kv.Value.root.sizeDelta = size;
                kv.Value.root.anchoredPosition = CellToAnchoredPos(kv.Key);
                ConfigureZipPadBackdrop(kv.Value, size);
                ResizeZipPadArrows(kv.Value, size);
                PositionZipPadArrows(kv.Value, size);
            }

        foreach (var kv in floorTintUnderlays)
            if (kv.Value) { kv.Value.rectTransform.sizeDelta = GetCellSize(); kv.Value.rectTransform.anchoredPosition = CellToAnchoredPos(kv.Key); }

        foreach (var kv in floorSecondaryTintUnderlays)
            if (kv.Value) { kv.Value.rectTransform.sizeDelta = GetCellSize(); kv.Value.rectTransform.anchoredPosition = CellToAnchoredPos(kv.Key); }
    }

    // ================= Buff Popup (Prefab-based) =================

    public Coroutine ShowBuffPopupAtCell(Vector2Int cell, string message, BuffPopupStyleSO style, RunModRarity rarity)
    {
        if (!InBounds(cell)) return null;
        if (string.IsNullOrWhiteSpace(message)) return null;
        if (!style || !style.popupPrefab) return null;

        if (!overlayRoot) overlayRoot = gridRoot;

        var instance = Instantiate(style.popupPrefab, overlayRoot);
        var view = instance.GetComponent<BuffPopupView>();
        if (!view || !view.IsValid)
        {
            Destroy(instance);
            return null;
        }

        var rootRT = view.RectTransform;
        rootRT.anchorMin = rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.localScale = Vector3.one;

        // Size first so overlap resolution can use real popup height
        Vector2 cellSize = GetCellSize();
        rootRT.sizeDelta = new Vector2(cellSize.x * style.sizeMultiplier.x, cellSize.y * style.sizeMultiplier.y);

        Vector2 desired = CellToAnchoredPos(cell);
        rootRT.anchoredPosition = ResolveBuffPopupPosition(desired, style, rootRT.sizeDelta.y);
        RegisterBuffPopup(rootRT);

        view.mainText.text = message;
        view.shadowText.text = message;

        var shadowRT = view.shadowText.rectTransform;
        shadowRT.anchorMin = shadowRT.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRT.anchoredPosition = style.shadowOffset;

        var colors = style.GetColors(rarity);
        view.mainText.color = colors.textA;
        view.shadowText.color = colors.shadowA;

        Vector3 baseMainScale = view.mainText.rectTransform.localScale;
        Vector3 baseShadowScale = view.shadowText.rectTransform.localScale;

        return StartCoroutine(BuffPopupPrefabRoutine(
            rootRT,
            view.mainText,
            view.shadowText,
            style,
            colors,
            baseMainScale,
            baseShadowScale));
    }

    System.Collections.IEnumerator BuffPopupPrefabRoutine(
    RectTransform rootRT,
    TMPro.TMP_Text main,
    TMPro.TMP_Text shadow,
    BuffPopupStyleSO style,
    BuffPopupStyleSO.RarityColorPair colors,
    Vector3 baseMainScale,
    Vector3 baseShadowScale)
    {
        float dur = Mathf.Max(0.05f, style.duration);
        float floatPx = Mathf.Max(0f, style.floatPixels);
        float flash = Mathf.Max(0.03f, style.flashInterval);

        // Pulse config
        float amp = Mathf.Max(0f, style.pulseScaleAmplitude);
        float hz = Mathf.Max(0f, style.pulseSpeedHz);

        Vector2 start = rootRT.anchoredPosition;
        Vector2 end = start + new Vector2(0f, floatPx);

        float t = 0f;
        float flashT = 0f;
        bool useA = true;

        float mainAlpha0 = main.color.a;
        float shadowAlpha0 = shadow.color.a;

        while (rootRT && t < dur)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            flashT += dt;

            float p = Mathf.Clamp01(t / dur);

            rootRT.anchoredPosition = Vector2.Lerp(start, end, p); // Float up

            // Flash colors
            if (flashT >= flash)
            {
                flashT = 0f;
                useA = !useA;
            }

            // Pulse scale
            if (amp > 0f && hz > 0f)
            {
                float s = 1f + (Mathf.Sin(t * (Mathf.PI * 2f) * hz) * amp);
                main.rectTransform.localScale = baseMainScale * s;
                shadow.rectTransform.localScale = baseShadowScale * s;
            }

            Color mc = useA ? colors.textA : colors.textB;
            Color sc = useA ? colors.shadowA : colors.shadowB;

            // Fade out
            float fade = 1f - p;
            mc.a = mainAlpha0 * fade;
            sc.a = shadowAlpha0 * fade;

            main.color = mc;
            shadow.color = sc;

            yield return null;
        }

        if (rootRT) UnregisterBuffPopup(rootRT);
        if (rootRT) Destroy(rootRT.gameObject);
    }

    void CleanupDeadBuffPopups()
    {
        for (int i = _activeBuffPopups.Count - 1; i >= 0; i--)
            if (!_activeBuffPopups[i])
                _activeBuffPopups.RemoveAt(i);
    }

    Vector2 ResolveBuffPopupPosition(Vector2 desired, BuffPopupStyleSO style, float popupHeight)
    {
        if (!style || !style.avoidOverlap) return desired;

        CleanupDeadBuffPopups();

        float minX = Mathf.Max(0f, style.overlapMinX);
        float minY = Mathf.Max(Mathf.Max(0f, style.overlapMinY), popupHeight * 0.95f);

        // Step should be at least the min gap
        float stepY = Mathf.Max(Mathf.Max(0f, style.overlapStepY), minY);

        int max = Mathf.Max(0, style.overlapMaxShifts);

        Vector2 pos = desired;

        for (int attempt = 0; attempt <= max; attempt++)
        {
            bool overlaps = false;

            for (int i = 0; i < _activeBuffPopups.Count; i++)
            {
                var other = _activeBuffPopups[i];
                if (!other) continue;

                Vector2 o = other.anchoredPosition;

                if (Mathf.Abs(pos.x - o.x) < minX && Mathf.Abs(pos.y - o.y) < minY)
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
                return pos;

            pos.y += stepY;
        }

        return pos;
    }

    void RegisterBuffPopup(RectTransform rt)
    {
        if (!rt) return;
        _activeBuffPopups.Add(rt);
    }

    void UnregisterBuffPopup(RectTransform rt)
    {
        if (!rt) return;
        _activeBuffPopups.Remove(rt);
    }

    public bool IsClearableOccupiedRow(int y)
    {
        bool full = true;
        bool hasClearableContent = false;

        for (int x = 0; x < width; x++)
        {
            var c = new Vector2Int(x, y);
            if (!placed.TryGetValue(c, out var rt) || !rt)
            {
                full = false;
                break;
            }

            if (!obstacles.TryGetValue(c, out var obs))
            {
                hasClearableContent = true;
                continue;
            }

            if (obs.type != ObstacleType.SoldierWall)
                hasClearableContent = true;
        }

        return full && hasClearableContent;
    }

    public List<RectTransform> GetPlacedVisualsInRow(int rowY)
    {
        var result = new List<RectTransform>();

        for (int x = 0; x < width; x++)
        {
            var key = new Vector2Int(x, rowY);
            if (placed.TryGetValue(key, out var rt) && rt)
                result.Add(rt);
        }

        return result;
    }

    public bool HasPlacedTiles()
    {
        return placed != null && placed.Count > 0;
    }
}

[DisallowMultipleComponent]
public class TetrominoBackgroundPulse : MonoBehaviour
{
    Image targetImage;
    RectTransform targetRect;
    Image glowImage;
    RectTransform glowRect;
    Image pulseImage;
    RectTransform pulseRect;

    Color passivePulseColor = Color.white;
    Color rowClearPulseColor = Color.white;
    float rowClearGlowAlpha = 0.42f;
    float rowClearGlowScale = 1.22f;
    bool useSiblingPulseLayers = true;
    bool passiveEnabled;
    float passiveClock;
    float passiveCycleOffset;
    float passiveInterval = 3f;
    float passiveDuration = 1.4f;
    float passivePeakAlpha = 0.22f;
    float passiveStartScale = 0.08f;
    float passiveEndScale = 1.18f;

    bool clearPulseActive;
    float clearTimer;
    float clearDelay;
    float clearRiseSeconds;
    float clearHoldSeconds;
    float clearPeakAlpha;
    float clearStartScale;
    float clearPeakScale;

    public void Configure(
        Color tileColor,
        bool enablePassive,
        float intervalSeconds,
        float durationSeconds,
        float peakAlpha,
        float startScale,
        float endScale,
        float whiteBlend,
        float rowClearWhiteBlend,
        float rowClearMinSaturation,
        float rowClearValueBoost,
        float rowClearGlowAlpha,
        float rowClearGlowScale,
        bool useSiblingPulseLayers,
        float cycleOffset)
    {
        targetImage = GetComponent<Image>();
        targetRect = transform as RectTransform;

        passiveEnabled = enablePassive;
        passiveInterval = Mathf.Max(0.05f, intervalSeconds);
        passiveDuration = Mathf.Clamp(durationSeconds, 0.01f, passiveInterval);
        passivePeakAlpha = Mathf.Clamp01(peakAlpha);
        passiveStartScale = Mathf.Max(1f, startScale);
        passiveEndScale = Mathf.Max(passiveStartScale, endScale, 1.12f);
        this.rowClearGlowAlpha = Mathf.Clamp01(rowClearGlowAlpha);
        this.rowClearGlowScale = Mathf.Max(1f, rowClearGlowScale);
        this.useSiblingPulseLayers = useSiblingPulseLayers;

        passivePulseColor = Color.Lerp(tileColor, Color.white, Mathf.Clamp01(whiteBlend));
        passivePulseColor.a = 1f;
        rowClearPulseColor = BuildNeonPulseColor(
            tileColor,
            rowClearWhiteBlend,
            rowClearMinSaturation,
            rowClearValueBoost);
        passiveCycleOffset = Mathf.Repeat(cycleOffset, passiveInterval);
        passiveClock = Mathf.Repeat(Time.unscaledTime + passiveCycleOffset, passiveInterval);

        EnsurePulseImage();
        SetPulseVisual(0f, passiveStartScale);
    }

    public void SetPassiveEnabled(bool enabled)
    {
        passiveEnabled = enabled;
        if (!enabled && !clearPulseActive)
            SetPulseVisual(0f, passiveStartScale);
    }

    public void ResetPulseVisuals()
    {
        passiveEnabled = false;
        clearPulseActive = false;
        SetPulseVisual(0f, passiveStartScale);
    }

    public void PlayRowClearPulse(
        float delaySeconds,
        float riseSeconds,
        float holdSeconds,
        float peakAlpha,
        float startScale,
        float peakScale)
    {
        EnsurePulseImage();

        clearPulseActive = true;
        clearTimer = 0f;
        clearDelay = Mathf.Max(0f, delaySeconds);
        clearRiseSeconds = Mathf.Max(0.01f, riseSeconds);
        clearHoldSeconds = Mathf.Max(0f, holdSeconds);
        clearPeakAlpha = Mathf.Clamp01(peakAlpha);
        clearStartScale = Mathf.Max(0.001f, startScale);
        clearPeakScale = Mathf.Max(clearStartScale, peakScale);

        SetPulseVisual(0f, clearStartScale);
    }

    void Awake()
    {
        targetImage = GetComponent<Image>();
        targetRect = transform as RectTransform;
    }

    void Update()
    {
        if (!targetImage)
            targetImage = GetComponent<Image>();

        if (!targetRect)
            targetRect = transform as RectTransform;

        if (!targetImage || !targetRect)
            return;

        EnsurePulseImage();
        SyncPulseImageToTarget();

        if (clearPulseActive)
        {
            TickClearPulse();
            return;
        }

        TickPassivePulse();
    }

    void TickPassivePulse()
    {
        if (!passiveEnabled)
        {
            SetPulseVisual(0f, passiveStartScale);
            return;
        }

        passiveClock = Mathf.Repeat(Time.unscaledTime + passiveCycleOffset, passiveInterval);
        if (passiveClock > passiveDuration)
        {
            SetPulseVisual(0f, passiveStartScale);
            return;
        }

        float t = Mathf.Clamp01(passiveClock / passiveDuration);
        float eased = Mathf.SmoothStep(0f, 1f, t);
        float alpha = Mathf.Sin(t * Mathf.PI) * passivePeakAlpha;
        float scale = Mathf.Lerp(passiveStartScale, passiveEndScale, eased);
        SetPulseVisual(alpha, scale);
    }

    void TickClearPulse()
    {
        clearTimer += Time.unscaledDeltaTime;

        if (clearTimer < clearDelay)
        {
            SetPulseVisual(0f, clearStartScale);
            return;
        }

        float localTime = clearTimer - clearDelay;
        if (localTime < clearRiseSeconds)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(localTime / clearRiseSeconds));
            SetPulseVisual(Mathf.Lerp(0f, clearPeakAlpha, t), Mathf.Lerp(clearStartScale, clearPeakScale, t));
            return;
        }

        if (localTime <= clearRiseSeconds + clearHoldSeconds)
        {
            SetPulseVisual(clearPeakAlpha, clearPeakScale);
            return;
        }

        clearPulseActive = false;
        SetPulseVisual(0f, passiveStartScale);
    }

    void EnsurePulseImage()
    {
        if (!targetRect)
            targetRect = transform as RectTransform;

        if (!targetRect)
            return;

        if (pulseImage && glowImage)
            return;

        Transform layerParent = GetPulseLayerParent();

        if (!glowImage)
        {
            var glowGo = new GameObject("BackgroundPulseGlow", typeof(RectTransform), typeof(Image));
            glowImage = glowGo.GetComponent<Image>();
            glowImage.raycastTarget = false;
            glowImage.preserveAspect = false;
            glowImage.type = Image.Type.Simple;

            glowRect = glowImage.rectTransform;
            glowRect.SetParent(layerParent, false);
        }

        if (!pulseImage)
        {
            var go = new GameObject("BackgroundPulseOverlay", typeof(RectTransform), typeof(Image));
            pulseImage = go.GetComponent<Image>();
            pulseImage.raycastTarget = false;
            pulseImage.preserveAspect = false;
            pulseImage.type = Image.Type.Simple;

            pulseRect = pulseImage.rectTransform;
            pulseRect.SetParent(layerParent, false);
        }
    }

    void SyncPulseImageToTarget()
    {
        if (!pulseImage || !pulseRect || !targetImage || !targetRect)
            return;

        Transform layerParent = GetPulseLayerParent();

        SyncPulseLayer(glowImage, glowRect, layerParent);
        SyncPulseLayer(pulseImage, pulseRect, layerParent);

        if (useSiblingPulseLayers)
        {
            if (clearPulseActive)
            {
                int targetIndex = targetRect.GetSiblingIndex();
                if (glowRect)
                    glowRect.SetSiblingIndex(Mathf.Min(targetIndex + 1, glowRect.parent.childCount - 1));
                if (pulseRect)
                    pulseRect.SetSiblingIndex(Mathf.Min((glowRect ? glowRect.GetSiblingIndex() : targetIndex) + 1, pulseRect.parent.childCount - 1));
            }
            else
            {
                if (glowRect)
                    glowRect.SetAsFirstSibling();
                if (pulseRect)
                    pulseRect.SetSiblingIndex(glowRect ? glowRect.GetSiblingIndex() + 1 : 0);
            }
        }
        else
        {
            if (glowRect)
                glowRect.SetAsFirstSibling();
            if (pulseRect)
                pulseRect.SetSiblingIndex(glowRect ? glowRect.GetSiblingIndex() + 1 : 0);
        }
    }

    Transform GetPulseLayerParent()
    {
        return useSiblingPulseLayers && targetRect && targetRect.parent
            ? targetRect.parent
            : targetRect;
    }

    void SyncPulseLayer(Image image, RectTransform rect, Transform layerParent)
    {
        if (!image || !rect || !targetRect || !layerParent)
            return;

        if (rect.parent != layerParent)
            rect.SetParent(layerParent, false);

        image.sprite = targetImage.sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;

        if (layerParent == targetRect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = targetRect.rect.size;
            rect.anchoredPosition = Vector2.zero;
        }
        else
        {
            rect.anchorMin = targetRect.anchorMin;
            rect.anchorMax = targetRect.anchorMax;
            rect.pivot = targetRect.pivot;
            rect.sizeDelta = targetRect.sizeDelta;
            rect.anchoredPosition = targetRect.anchoredPosition;
        }

        rect.localRotation = targetRect.localRotation;
    }

    static Color BuildNeonPulseColor(
        Color baseColor,
        float whiteBlend,
        float minSaturation,
        float valueBoost)
    {
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);

        s = Mathf.Clamp01(Mathf.Max(s, minSaturation));
        v = Mathf.Clamp01(Mathf.Max(v, 0.55f) * Mathf.Max(1f, valueBoost));

        var neon = Color.HSVToRGB(h, s, v);
        neon = Color.Lerp(neon, Color.white, Mathf.Clamp01(whiteBlend));
        neon.a = 1f;
        return neon;
    }

    void SetPulseVisual(float alpha, float scale)
    {
        if (!pulseImage || !pulseRect)
            return;

        bool rowClear = clearPulseActive;
        var color = rowClear ? rowClearPulseColor : passivePulseColor;
        color.a = Mathf.Clamp01(alpha);
        pulseImage.color = color;
        pulseImage.enabled = color.a > 0.001f;
        pulseRect.localScale = Vector3.one * Mathf.Max(0.001f, scale);

        if (!glowImage || !glowRect)
            return;

        var glowColor = rowClearPulseColor;
        glowColor.a = rowClear ? Mathf.Clamp01(alpha * rowClearGlowAlpha) : 0f;
        glowImage.color = glowColor;
        glowImage.enabled = glowColor.a > 0.001f;
        glowRect.localScale = Vector3.one * Mathf.Max(0.001f, scale * rowClearGlowScale);
    }
}
