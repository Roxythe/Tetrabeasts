using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public Sprite defaultDeadOverlaySprite; 
    public Color deadOverlayTint = Color.white; 
    public bool deadOverlayPreserveAspect = true;

    [Header("SFX")]
    public AudioClip sfxImmuneHit;

    [Header("Line Clear Timing")]
    public float cascadeClearVisualDelay = 0.25f;

    bool tilesImmune = false;
    public enum DamageSource { Generic, CastleProjectile } 

    // Runtime
    GameController _gc;
    RectTransform boardRect;  
    Vector2 cellSize;  
    Vector2 contentSize;  
    readonly Transform[,] grid = null; // [x,y] grid of tile transforms
    Dictionary<Vector2Int, RectTransform> placed = new();
    readonly Dictionary<Vector2Int, float> healTimers = new();

    // ================= Obstacles & Floor Effects =================

    [Header("Obstacle & Trap Sprites")]
    public Sprite stoneUndamagedSprite;      // 0 hits taken
    public Sprite stoneDamagedSprite;        // 1 hit taken
    public Sprite stoneCriticalSprite;       // 2 hits taken

    public Sprite poisonBorderSprite; 
    public Sprite fireBorderSprite;  
    public Sprite lightningBorderSprite;

    public Sprite spikeSpriteLow;            // Spike frame A (lower)
    public Sprite spikeSpriteHigh;           // Spike frame B (raised)
    public float spikeAnimInterval = 0.35f;  // Seconds per toggle

    [Header("Layer Roots (auto-created if empty)")]
    public RectTransform underlayRoot; // Traps under blocks (spikes)
    public RectTransform overlayRoot;  // Borders above blocks (fire/poison)

    [Header("Monster Tick Damage Flash")]
    public float monsterTickFlashDuration = 0.08f;
    public Color burnTickFlashTint = new Color(1f, 0.25f, 0.25f, 1f);      // red
    public Color poisonTickFlashTint = new Color(0.75f, 0.25f, 1f, 1f);    // purple
    public Color lightningTickFlashTint = new Color(0.25f, 0.25f, 1f, 1f); // blue

    readonly Dictionary<Vector2Int, Coroutine> _monsterFlashCo = new();

    public enum ObstacleType { Stone }

    [System.Serializable]
    public struct ObstacleState
    {
        public ObstacleType type;
        public int hitsRemaining; // Remaining hits until break
        public int maxHits;       // Used to compute hits taken for sprite state

        public ObstacleState(ObstacleType t, int hits)
        {
            type = t;
            maxHits = Mathf.Max(1, hits);
            hitsRemaining = maxHits;
        }

        public int HitsTaken => Mathf.Clamp(maxHits - hitsRemaining, 0, maxHits);
    }

    readonly Dictionary<Vector2Int, ObstacleState> obstacles = new();

    public event Action<Vector2Int, ObstacleType> ObstacleDestroyed;

    public enum FloorEffectType { Poison, Burn, Spike, Lightning }

    public struct FloorEffectState
    {
        public FloorEffectType type;
        public float damage;        // poison/burn tick damage OR spike one-shot damage
        public float interval;      // poison/burn tick interval
        public int ticksRemaining;  // -1 = infinite
        public float nextTickTime;

        public static FloorEffectState Poison(float dmg, float interval, int ticks)
        {
            return new FloorEffectState
            {
                type = FloorEffectType.Poison,
                damage = Mathf.Max(0f, dmg),
                interval = Mathf.Max(0.05f, interval),
                ticksRemaining = ticks,
                nextTickTime = Time.time + Mathf.Max(0.05f, interval)
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
    }

    readonly Dictionary<Vector2Int, FloorEffectState> floorEffects = new();

    // visuals
    readonly Dictionary<Vector2Int, Image> poisonBorders = new();
    readonly Dictionary<Vector2Int, Image> fireBorders = new();
    readonly Dictionary<Vector2Int, Image> lightningBorders = new();

    class SpikeVisual
    {
        public RectTransform root;
        public Image low;
        public Image high;
        public bool highOn;
        public float nextToggleTime;
    }

    readonly Dictionary<Vector2Int, SpikeVisual> spikeVisuals = new();

    public bool HasFloorEffect(Vector2Int cell) => floorEffects.ContainsKey(cell);

    // Events
    public event Action<Vector2Int, MonsterData> TileDied; // Event called when a tile's HP reaches 0

    [System.Serializable]
    public struct MonsterInstance
    {
        public MonsterData data;
        public float hp; // Current runtime HP

        // Buffed runtime stats (computed once from data + shop)
        public float maxHp;         // +5 HP per shop level
        public int attackBonus;     // +1 damage per shop level
        public float healAmount;    // Buffed heal amount (only for healer monsters)
        public float healRange;
        public float healSpeed;

        public MonsterInstance(MonsterData d)
        {
            data = d;

            hp = 0f;
            maxHp = 0f;
            attackBonus = 0;
            healAmount = 0f;
            healRange = 0f;
            healSpeed = 0f;

            RecalcFromData(setHpToMax: true);
        }

        public void RecalcFromData(bool setHpToMax)
        {
            if (!data)
            {
                maxHp = 0f;
                attackBonus = 0;
                healAmount = 0f;
                healRange = 0f;
                healSpeed = 0f;
                if (setHpToMax) hp = 0f;
                return;
            }

            int hpBonus = ShopBuffStore.GetLevel(ShopBuffType.HpUp) * 5;
            int atkBonus = ShopBuffStore.GetLevel(ShopBuffType.AttackUp) * 1;
            int healBonus = ShopBuffStore.GetLevel(ShopBuffType.HealPower) * 2;

            maxHp = data.maxHealth + hpBonus;
            attackBonus = atkBonus;

            // Healer stats increased only if healAmount > 0 originally
            healAmount = (data.healAmount > 0f) ? (data.healAmount + healBonus) : 0f;

            healRange = data.healRange;
            healSpeed = data.healSpeed;

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

    public void RefreshTileBordersAt(Vector2Int cell)
    {
        if (!placed.TryGetValue(cell, out var rt) || !rt) return;

        bool leftShared = InBounds(cell + Vector2Int.left) && placed.ContainsKey(cell + Vector2Int.left);
        bool rightShared = InBounds(cell + Vector2Int.right) && placed.ContainsKey(cell + Vector2Int.right);
        bool topShared = InBounds(cell + Vector2Int.up) && placed.ContainsKey(cell + Vector2Int.up);
        bool bottomShared = InBounds(cell + Vector2Int.down) && placed.ContainsKey(cell + Vector2Int.down);

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

        underlayRoot.SetSiblingIndex(0);
        gridRoot.SetSiblingIndex(1);
        overlayRoot.SetAsLastSibling();
    }

    void Start()
    {
        RecomputeCellMetrics();
        DrawGridOverlay();
    }

    void Update()
    {
        if (_gc && !_gc.IsRoundActive) return; // Nothing should tick outside active rounds

        TickFloorEffects();
        AnimateSpikes();

        // ================== Healers pulse over time ==================
        if (monsters.Count == 0 || healTimers.Count == 0) return;

        float healMult = (_gc != null) ? _gc.healPowerMult : 1f;
        int rangeAdd = (_gc != null) ? _gc.healRangeAdd : 0;

        var toKeys = new List<Vector2Int>(healTimers.Keys);
        foreach (var k in toKeys)
        {
            if (!monsters.TryGetValue(k, out var inst) || inst.data == null)
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
                if (HealTile(best.Value, finalHeal))
                {
                    PlayHealVFX(best.Value, md.healSprite, 0.5f);

                    if (AudioManager.I && md)
                    {
                        var clip = md.PickRandomHealSFX();
                        if (clip) AudioManager.I.PlaySFX(clip);
                    }
                }
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
        // Root rect (1 tile)
        var rt = new GameObject($"Tile_{GetInstanceID()}",
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image), typeof(BoardOwned))
            .GetComponent<RectTransform>();

        rt.SetParent(gridRoot, false);
        rt.sizeDelta = cellSize;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;

        var baseImg = rt.GetComponent<UnityEngine.UI.Image>();
        baseImg.sprite = null;
        baseImg.raycastTarget = false;
        baseImg.color = new Color(0f, 0f, 0f, 0f);

        SetInlineBorderColor(rt, TilesDamageImmune ? immuneBorderColor : normalBorderColor);

        // Back (gray)
        var back = new GameObject("BackFill", typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        back.transform.SetParent(rt, false);
        var backRT = back.rectTransform;
        backRT.anchorMin = backRT.anchorMax = new Vector2(0.5f, 0.5f);
        backRT.sizeDelta = rt.sizeDelta;   // Fill the whole tile
        backRT.anchoredPosition = Vector2.zero;
        back.sprite = OnePx(); // Simple 1x1 white pixel
        back.type = UnityEngine.UI.Image.Type.Simple;
        back.color = new Color(0.6f, 0.6f, 0.6f, 1f);

        // HealthFill
        var fill = new GameObject("HealthFill", typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        fill.transform.SetParent(rt, false);
        var frt = fill.rectTransform;
        frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.sizeDelta = rt.sizeDelta;      // Fill the whole tile
        frt.anchoredPosition = Vector2.zero;
        fill.sprite = OnePx(); // Simple 1x1 white pixel
        fill.type = UnityEngine.UI.Image.Type.Filled;
        fill.fillMethod = UnityEngine.UI.Image.FillMethod.Vertical;
        fill.fillOrigin = (int)UnityEngine.UI.Image.OriginVertical.Bottom;
        fill.fillAmount = 1f;
        fill.color = color;

        ApplySharedEdges(rt, false, false, false, false, DEFAULT_BORDER);
        SetInlineBorderColor(rt, TilesDamageImmune ? immuneBorderColor : normalBorderColor);
        frt.SetAsFirstSibling();
        backRT.SetAsFirstSibling();

        return rt;
    }

    public RectTransform InstantiateTileUI(Color color, Sprite portrait, float inset = 4f)
    {
        var rt = InstantiateTileUI(color);
        if (portrait)
        {
            var go = new GameObject("MonsterPortrait", typeof(UnityEngine.UI.Image));
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.sprite = portrait;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var prt = img.rectTransform;
            prt.SetParent(rt, false);
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);

            // Slight inset so it sits inside the inline border
            prt.sizeDelta = ((RectTransform)rt.Find("HealthFill")).sizeDelta - new Vector2(2f, 2f);
            prt.anchoredPosition = Vector2.zero;

            prt.SetAsLastSibling(); // Ensure the portrait renders on top
        }

        {
            // Add a DeadOverlay layer 
            var fillRT = (RectTransform)rt.Find("HealthFill");
            if (fillRT)
            {
                var deadGO = new GameObject("DeadOverlay", typeof(UnityEngine.UI.Image));
                var deadImg = deadGO.GetComponent<UnityEngine.UI.Image>();
                deadImg.raycastTarget = false;
                deadImg.sprite = defaultDeadOverlaySprite; 
                deadImg.color = deadOverlayTint; 
                deadImg.preserveAspect = deadOverlayPreserveAspect;
                deadImg.type = UnityEngine.UI.Image.Type.Simple; 

                var drt = deadImg.rectTransform;
                drt.SetParent(rt, false);
                drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
                drt.sizeDelta = fillRT.sizeDelta;     // Cover the inner area
                drt.anchoredPosition = Vector2.zero;

                deadGO.SetActive(false);              // Hidden until HP == 0
                drt.SetAsLastSibling();               // Above portrait/fill
            }
        }

        return rt;
    }

    void UpdateTileHPVisual(Vector2Int cell, float current, float max)
    {
        if (!placed.TryGetValue(cell, out var rt)) return;
        var fill = rt.Find("HealthFill") as RectTransform;
        if (!fill) return;

        var img = fill.GetComponent<UnityEngine.UI.Image>();
        float amt = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);
        img.fillAmount = amt; // Depleted area shows the gray base underneath

        // Show/hide the dead overlay
        var dead = rt.Find("DeadOverlay");
        if (dead) dead.gameObject.SetActive(current <= 0f);
    }

    public void SpawnHealBurst(Vector2Int cell, Sprite sprite, float seconds = 0.5f)
    {
        if (!placed.TryGetValue(cell, out var rt)) return;

        var go = new GameObject("HealVFX", typeof(Image));
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.color = new Color(1f, 1f, 1f, 0.95f);

        var vfx = img.rectTransform;
        vfx.SetParent(rt, false);
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
        if (img) Destroy(img.gameObject);
    }

    public bool DamageTile(Vector2Int cell, float amount, DamageSource src = DamageSource.Generic,
                           AudioClip sfxOverride = null)
    {
        if (tilesImmune)
        {
            if (AudioManager.I && sfxImmuneHit) AudioManager.I.PlaySFX(sfxImmuneHit);
            return true; // No damage taken
        }

        if (!monsters.TryGetValue(cell, out var inst) || inst.data == null)
            return false;

        if (inst.hp <= 0f)
            return false;

        inst.hp = Mathf.Max(0f, inst.hp - amount);
        monsters[cell] = inst;
        UpdateTileHPVisual(cell, inst.hp, inst.maxHp);

        // --- Choose and play a hurt SFX ---
        AudioClip clip = sfxOverride;

        if (AudioManager.I && clip)
            AudioManager.I.PlaySFX(clip);

        if (inst.hp <= 0f)
        {
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

    public bool HealTile(Vector2Int cell, float amount)
    {
        if (!monsters.TryGetValue(cell, out var inst) || inst.data == null) return false;
        if (inst.hp <= 0f) return false; // cannot heal dead tiles
        float newHp = Mathf.Min(inst.maxHp, inst.hp + amount);
        if (Mathf.Approximately(newHp, inst.hp)) return false;

        inst.hp = newHp;
        monsters[cell] = inst;
        UpdateTileHPVisual(cell, inst.hp, inst.maxHp);
        return true;
    }

    public bool InBounds(Vector2Int c) =>
        c.x >= 0 && c.x < width && c.y >= 0 && c.y < height;

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
        obstacles.Clear();
        floorEffects.Clear();

        healTimers.Clear();

        // Clear grid visuals
        if (gridRoot != null)
        {
            var owned = gridRoot.GetComponentsInChildren<BoardOwned>(true);
            for (int i = owned.Length - 1; i >= 0; i--)
                Destroy(owned[i].gameObject);
        }

        // Clear overlays (fire/poison borders)
        foreach (var kv in poisonBorders) if (kv.Value) Destroy(kv.Value.gameObject);
        poisonBorders.Clear();

        foreach (var kv in fireBorders) if (kv.Value) Destroy(kv.Value.gameObject);
        fireBorders.Clear();

        // Clear spikes (underlay)
        foreach (var kv in spikeVisuals) if (kv.Value != null && kv.Value.root) Destroy(kv.Value.root.gameObject);
        spikeVisuals.Clear();

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
        var gc = FindFirstObjectByType<GameController>();

        bool clearedAny = true;
        var stonesDamaged = new HashSet<Vector2Int>();

        while (clearedAny)
        {
            clearedAny = false;

            for (int y = 0; y < height; y++)
            {
                // Row is full if every cell is occupied by something (monster tile or obstacle tile)
                bool full = true;
                for (int x = 0; x < width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (!placed.TryGetValue(c, out var rt) || !rt)
                    {
                        full = false;
                        break;
                    }
                }

                if (!full) continue;

                clearedAny = true;
                rowsCleared++;

                int clearIndex = rowsCleared - 1;
                int rowKey = (clearIndex * 1000) + y;

                // ===== Tally row damage/special from MONSTERS ONLY =====
                int dmgRow = 0;
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
                            float gaugeMult = (gc ? gc.monsterSpecialGainMult : 1f);

                            float dmgMult = (gc ? gc.monsterDamageMult : 1f);
                            float tempMult = (gc ? gc.PlayerMonsterAttackMult : 1f);

                            float baseDmg = (inst.data.attackPower * dmgMult) + inst.attackBonus; // Includes run mods and shop buffs
                            int dmg = Mathf.RoundToInt(baseDmg * tempMult);

                            dmgRow += Mathf.Max(1, dmg);

                            float gauge = inst.data.specialGaugeGain * gaugeMult;
                            specialChargeFromMonsters += Mathf.Max(1f, gauge);
                        }

                        if (!counts.ContainsKey(inst.data))
                            counts[inst.data] = 0;
                        counts[inst.data] += 1;
                    }
                }

                damageFromMonsters += dmgRow;
                rowDamage[rowKey] = dmgRow;

                if (counts.Count > 0)
                {
                    int max = 0;
                    foreach (var kv in counts) if (kv.Value > max) max = kv.Value;
                    var candidates = new List<MonsterData>();
                    foreach (var kv in counts) if (kv.Value == max) candidates.Add(kv.Key);
                    var dominant = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    rowDominantMonster[rowKey] = dominant;
                }

                // ===== Remove row contents =====
                for (int x = 0; x < width; x++)
                {
                    var key = new Vector2Int(x, y);

                    // Stone obstacles take 1 hit, update sprite, only destroy when hitsRemaining reaches 0
                    if (TryDamageStoneAt(key, stonesDamaged, removedCells))
                        continue;

                    // Normal monster tile, remove completely
                    monsters.Remove(key);

                    if (placed.TryGetValue(key, out var rt))
                    {
                        UnityEngine.Object.Destroy(rt.gameObject);
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
    float? overrideCascadeDelay = null)
    {
        float delay = overrideCascadeDelay ?? cascadeClearVisualDelay;
        StartCoroutine(ClearFullLinesRoutine(delay, onComplete));
    }

    IEnumerator ClearFullLinesRoutine(
        float cascadeDelay,
        Action<int, List<Vector2Int>, int, float, Dictionary<int, int>, Dictionary<int, MonsterData>> onComplete)
    {
        int rowsCleared = 0;
        var removedCells = new List<Vector2Int>();
        int damageFromMonsters = 0;
        float specialChargeFromMonsters = 0f;
        var rowDamage = new Dictionary<int, int>();
        var rowDominantMonster = new Dictionary<int, MonsterData>();
        var gc = FindFirstObjectByType<GameController>();

        bool clearedAny = true;
        var stonesDamaged = new HashSet<Vector2Int>();

        while (clearedAny)
        {
            clearedAny = false;

            for (int y = 0; y < height; y++)
            {
                bool full = true;
                for (int x = 0; x < width; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (!placed.TryGetValue(c, out var rt) || !rt)
                    {
                        full = false;
                        break;
                    }
                }

                if (!full) continue;

                clearedAny = true;
                rowsCleared++;

                int clearIndex = rowsCleared - 1;
                int rowKey = (clearIndex * 1000) + y;

                // ===== Tally row damage/special from Monsters only =====
                int dmgRow = 0;
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
                            float gaugeMult = (gc ? gc.monsterSpecialGainMult : 1f);

                            float dmgMult = (gc ? gc.monsterDamageMult : 1f);
                            float tempMult = (gc ? gc.PlayerMonsterAttackMult : 1f);

                            float baseDmg = (inst.data.attackPower * dmgMult) + inst.attackBonus;
                            int dmg = Mathf.RoundToInt(baseDmg * tempMult);

                            dmgRow += Mathf.Max(1, dmg);

                            float gauge = inst.data.specialGaugeGain * gaugeMult;
                            specialChargeFromMonsters += Mathf.Max(1f, gauge);
                        }

                        if (!counts.ContainsKey(inst.data))
                            counts[inst.data] = 0;
                        counts[inst.data] += 1;
                    }
                }

                damageFromMonsters += dmgRow;
                rowDamage[rowKey] = dmgRow;

                if (counts.Count > 0)
                {
                    int max = 0;
                    foreach (var kv in counts) if (kv.Value > max) max = kv.Value;
                    var candidates = new List<MonsterData>();
                    foreach (var kv in counts) if (kv.Value == max) candidates.Add(kv.Key);
                    var dominant = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    rowDominantMonster[rowKey] = dominant;
                }

                // ===== Remove row contents =====
                for (int x = 0; x < width; x++)
                {
                    var key = new Vector2Int(x, y);

                    if (TryDamageStoneAt(key, stonesDamaged, removedCells))
                        continue;

                    monsters.Remove(key);

                    if (placed.TryGetValue(key, out var rt))
                    {
                        UnityEngine.Object.Destroy(rt.gameObject);
                        placed.Remove(key);
                        removedCells.Add(key);
                    }
                }

                // Let remaining tiles fall into gaps (stones remain as blockers)
                SettleAllColumns(false);

                // Redraw at least one frame so cascade-created full rows are visible before being cleared
                yield return null;
                if (cascadeDelay > 0f)
                    yield return new WaitForSecondsRealtime(cascadeDelay);

                y = -1; // Board changed, restart scanning from bottom
            }
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

        var gc = FindFirstObjectByType<GameController>();

        // ===== Tally combat stats from bottom rows before deletion =====
        for (int y = 0; y < rows; y++)
        {
            int dmgRow = 0;

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
                        float gaugeMult = (gc ? gc.monsterSpecialGainMult : 1f);
                        float dmgMult = (gc ? gc.monsterDamageMult : 1f);
                        float tempMult = (gc ? gc.PlayerMonsterAttackMult : 1f);

                        float baseDmg = (inst.data.attackPower * dmgMult) + inst.attackBonus;
                        int dmg = Mathf.RoundToInt(baseDmg * tempMult);

                        dmgRow += Mathf.Max(1, dmg);

                        float gauge = inst.data.specialGaugeGain * gaugeMult;
                        specialChargeFromMonsters += Mathf.Max(1f, gauge);
                    }

                    // Collect candidate start columns for this row’s projectile
                    if (!colsByRow.TryGetValue(y, out var cols))
                    {
                        cols = new List<int>();
                        colsByRow[y] = cols;
                    }
                    if (!cols.Contains(x)) cols.Add(x);
                }
            }

            int rowKey = (y * 1000) + y;

            if (dmgRow > 0)
            {
                rowDamage[rowKey] = dmgRow;
                totalMonsterDamage += dmgRow;
            }

            // Pick dominant monster for this cleared row
            if (countsRow.Count > 0)
            {
                int max = 0;
                foreach (var kv in countsRow) if (kv.Value > max) max = kv.Value;

                var candidates = new List<MonsterData>();
                foreach (var kv in countsRow) if (kv.Value == max) candidates.Add(kv.Key);

                if (candidates.Count > 0)
                    rowDominantMonster[rowKey] = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            }
        }

        // ===== Perform the actual clear and shift =====
        int squaresCleared = 0;

        var toDelete = new List<Vector2Int>();
        foreach (var kv in placed)
            if (kv.Key.y < rows) toDelete.Add(kv.Key);

        foreach (var key in toDelete)
        {
            if (obstacles.ContainsKey(key))
            {
                DestroyObstacleImmediate(key);
                squaresCleared++;
                continue;
            }

            if (placed.TryGetValue(key, out var rt))
                Destroy(rt.gameObject);

            placed.Remove(key);
            monsters.Remove(key);
            healTimers.Remove(key);
            squaresCleared++;
        }

        var snapshot = new List<KeyValuePair<Vector2Int, RectTransform>>(placed);
        foreach (var kv in snapshot)
        {
            var from = kv.Key;
            if (from.y < rows) continue;

            var rt = kv.Value;
            var to = new Vector2Int(from.x, from.y - rows);

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

            if (obstacles.TryGetValue(from, out var obs))
            {
                obstacles.Remove(from);
                obstacles[to] = obs;
            }
        }

        return squaresCleared;
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
            // If this cell is a stone obstacle, destroy it via the proper path to keep dictionaries synced
            if (obstacles.ContainsKey(key))
            {
                DestroyObstacleImmediate(key);
                squaresCleared++;
                continue;
            }

            if (placed.TryGetValue(key, out var rt))
                Destroy(rt.gameObject);

            placed.Remove(key);
            monsters.Remove(key);
            healTimers.Remove(key);

            squaresCleared++;
        }

        // Move using a snapshot to avoid key-collision while iterating
        var snapshot = new List<KeyValuePair<Vector2Int, RectTransform>>(placed);
        foreach (var kv in snapshot)
        {
            var from = kv.Key;
            if (from.y < rows) continue;

            var rt = kv.Value;
            var to = new Vector2Int(from.x, from.y - rows);

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

            // Keep obstacle dictionary synced if an obstacle tile moved
            if (obstacles.TryGetValue(from, out var obs))
            {
                obstacles.Remove(from);
                obstacles[to] = obs;
            }
        }

        return squaresCleared;
    }

    public int ReviveAllTilesToFull(List<Vector2Int> changedCells)
    {
        if (changedCells == null) changedCells = new List<Vector2Int>();

        var keys = new List<Vector2Int>(monsters.Keys);

        foreach (var cell in keys)
        {
            if (!monsters.TryGetValue(cell, out var inst) || inst.data == null) continue;

            float max = Mathf.Max(0f, inst.maxHp);
            if (inst.hp < max) // Includes reviving from 0
            {
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
    }

    public bool TilesDamageImmune => tilesImmune;

    public void SetAllTileBorderColor(Color c)
    {
        // Apply to all placed tiles
        foreach (var kv in placed)
        {
            var rt = kv.Value;
            if (!rt) continue;

            // Border is the Image on the root of the tile
            var img = rt.GetComponent<UnityEngine.UI.Image>();
            if (img) img.color = c;
        }
    }

    public void StyleInlineBorder(RectTransform rt, Color c)
    {
        var img = rt ? rt.GetComponent<UnityEngine.UI.Image>() : null;
        if (img) img.color = c; // Border only
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
        var gc = FindFirstObjectByType<GameController>();
        var stonesDamaged = new HashSet<Vector2Int>();

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
                        float gaugeMult = (gc ? gc.monsterSpecialGainMult : 1f);
                        float dmgMult = (gc ? gc.monsterDamageMult : 1f);
                        float tempMult = (gc ? gc.PlayerMonsterAttackMult : 1f);

                        float baseDmg = (inst.data.attackPower * dmgMult) + inst.attackBonus;
                        int dmg = Mathf.RoundToInt(baseDmg * tempMult);

                        damageFromMonsters += Mathf.Max(1, dmg);

                        float gauge = inst.data.specialGaugeGain * gaugeMult;
                        specialChargeFromMonsters += Mathf.Max(1f, gauge);
                    }
                }

                monsters.Remove(key);
            }

            if (TryDamageStoneAt(key, stonesDamaged, removedCells))
                continue;

            if (placed.TryGetValue(key, out var rt))
            {
                Destroy(rt.gameObject);
                placed.Remove(key);
                removedCells.Add(key);
            }
        }

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

        CleanOrphanedTiles();
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
            if (kv.Value) kv.Value.anchoredPosition = CellToAnchoredPos(kv.Key);

        RepositionEffectVisuals();

        DrawGridOverlay();
    }

    public Vector2 GetCellSize() => cellSize; // Helper so others can read cell size

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
            healTimers[cell] = Time.time;
    }

    void CleanOrphanedTiles()
    {
        // Remove any tiles in gridRoot that are not in placed[]
        var live = new HashSet<RectTransform>(placed.Values);
        for (int i = 0; i < gridRoot.childCount; i++)
        {
            var t = gridRoot.GetChild(i) as RectTransform;
            if (!t) continue;
            if (!t.name.StartsWith("Tile_")) continue; // Only care about tiles
            if (!live.Contains(t)) Destroy(t.gameObject);
        }
    }

    public bool TryGetMonster(Vector2Int cell, out MonsterInstance inst) => monsters.TryGetValue(cell, out inst);

    // ======== Healing VFX ========

    public void PlayHealVFX(Vector2Int cell, Sprite sprite, float duration = 0.5f)
    {
        if (!sprite) return;
        StartCoroutine(PlayHealVFXCo(cell, sprite, duration));
    }

    private System.Collections.IEnumerator PlayHealVFXCo(Vector2Int cell, Sprite sprite, float duration)
    {
        if (!InBounds(cell)) yield break;

        var img = new GameObject("HealVFX", typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.SetParent(gridRoot, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = GetCellSize() - new Vector2(6f, 6f);  // Slight inset
        rt.anchoredPosition = CellToAnchoredPos(cell);

        float t = 0f;
        while (t < duration && img)
        {
            t += Time.deltaTime;

            // Quick pop-in/out 
            float a = 1f - Mathf.Abs((t / duration) * 2f - 1f);
            img.color = new Color(1f, 1f, 1f, 0.35f + 0.65f * a);
            yield return null;
        }
        if (img) Destroy(img.gameObject);
    }

    // ================= Public API for ObstacleManager / Piece =================

    public bool HasObstacle(Vector2Int cell) => obstacles.ContainsKey(cell);
    public bool IsFree(Vector2Int c) => InBounds(c) && !placed.ContainsKey(c);

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

    public bool SetFloorEffect(Vector2Int cell, FloorEffectType type, float damage, float interval, int ticks)
    {
        if (!InBounds(cell)) return false;

        if (HasFloorEffect(cell)) return false; // Never allow overlapping effects

        switch (type)
        {
            case FloorEffectType.Poison:
                floorEffects[cell] = FloorEffectState.Poison(damage, interval, ticks);
                EnsureBorderOverlay(cell, poisonBorderSprite, poisonBorders);
                break;

            case FloorEffectType.Burn:
                floorEffects[cell] = FloorEffectState.Burn(damage, interval, ticks);
                EnsureBorderOverlay(cell, fireBorderSprite, fireBorders);
                break;

            case FloorEffectType.Spike:
                floorEffects[cell] = FloorEffectState.Spike(damage);
                EnsureSpikeUnderlay(cell);
                break;

            case FloorEffectType.Lightning:
                floorEffects[cell] = FloorEffectState.Lightning(damage, interval, ticks);
                EnsureBorderOverlay(cell, lightningBorderSprite, lightningBorders);
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
    }

    public void ApplyFloorEffectOnPlacement(Vector2Int cell)
    {
        if (!floorEffects.TryGetValue(cell, out var fx)) return;

        if (fx.type == FloorEffectType.Spike)
        {
            TryDamageMonsterFromFloorEffect(cell, fx.type, fx.damage);

            ClearFloorEffect(cell);
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
                if (obstacles.TryGetValue(c, out var obs) && obs.type == ObstacleType.Stone)
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
            }
        }
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

        if (spikeVisuals.TryGetValue(cell, out var existing) && existing != null && existing.root)
        {
            // Update sizing/pos
            existing.root.sizeDelta = GetCellSize();
            existing.root.anchoredPosition = CellToAnchoredPos(cell);
            return;
        }

        var root = new GameObject($"Spike_{cell.x}_{cell.y}", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(underlayRoot, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = GetCellSize();
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
            rt.sizeDelta = GetCellSize();
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

    // ================= Floor Effect Ticking =================

    void TickFloorEffects()
    {
        if (floorEffects.Count == 0) return;

        float now = Time.time;

        // Snapshot keys to allow mutation
        var keys = new List<Vector2Int>(floorEffects.Keys);
        foreach (var cell in keys)
        {
            if (!floorEffects.TryGetValue(cell, out var fx)) continue;

            if (fx.type == FloorEffectType.Spike)
                continue; // Spikes are handled on placement only

            // Keep border overlays on top
            if (fx.type == FloorEffectType.Poison && poisonBorders.TryGetValue(cell, out var p) && p)
                p.rectTransform.SetAsLastSibling();
            else if (fx.type == FloorEffectType.Burn && fireBorders.TryGetValue(cell, out var f) && f)
                f.rectTransform.SetAsLastSibling();
            else if (fx.type == FloorEffectType.Lightning && lightningBorders.TryGetValue(cell, out var l) && l)
                l.rectTransform.SetAsLastSibling();

            if (now < fx.nextTickTime)
                continue;

            // Tick damage only if there's a living monster in this cell
            TryDamageMonsterFromFloorEffect(cell, fx.type, fx.damage);

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

    void FlashMonsterTick(Vector2Int cell, Color tint)
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

        DamageTile(cell, damage, DamageSource.Generic);

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
        if (placed.TryGetValue(cell, out var rt) && rt)
            Destroy(rt.gameObject);

        placed.Remove(cell);
        monsters.Remove(cell);

        obstacles.Remove(cell);

        ObstacleDestroyed?.Invoke(cell, ObstacleType.Stone);

        RefreshTileBordersAround(cell);
    }

    bool TryDamageStoneAt(Vector2Int cell, HashSet<Vector2Int> guard, List<Vector2Int> removedCells)
    {
        if (!obstacles.TryGetValue(cell, out var obs) || obs.type != ObstacleType.Stone)
            return false;

        if (guard != null && !guard.Add(cell))
        {
            // Already damaged this stone during this call, just ensure monsters aren't left behind
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

        monsters.Remove(cell);
        return true; // handled as stone obstacle
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

    // ================= Boss Warning Flashing =================

    public Coroutine FlashWarningAtCell(Vector2Int cell, Sprite warningSprite, float seconds, float toggleInterval = 0.08f, float alpha = 0.65f)
    {
        return StartCoroutine(FlashWarningRoutine(cell, warningSprite, seconds, toggleInterval, alpha));
    }

    IEnumerator FlashWarningRoutine(Vector2Int cell, Sprite warningSprite, float seconds, float toggleInterval, float alpha)
    {
        if (!InBounds(cell) || warningSprite == null) yield break;

        if (!overlayRoot) overlayRoot = gridRoot;

        var go = new GameObject("BossWarning");
        go.transform.SetParent(overlayRoot, false);
        var img = go.AddComponent<Image>();
        img.sprite = warningSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        // NEW: slightly transparent
        alpha = Mathf.Clamp01(alpha);
        img.color = new Color(1f, 1f, 1f, alpha);

        var rt = img.rectTransform;
        rt.sizeDelta = cellSize;
        rt.anchoredPosition = CellToAnchoredPos(cell);

        float t = 0f;
        float tog = 0f;
        bool on = true;

        while (t < seconds)
        {
            t += Time.deltaTime;
            tog += Time.deltaTime;
            if (tog >= toggleInterval)
            {
                tog = 0f;
                on = !on;
                img.enabled = on;
            }
            yield return null;
        }

        if (img) Destroy(img.gameObject);
    }

    // ================= Resize support =================

    void RepositionEffectVisuals()
    {
        foreach (var kv in poisonBorders)
            if (kv.Value) { kv.Value.rectTransform.sizeDelta = GetCellSize(); kv.Value.rectTransform.anchoredPosition = CellToAnchoredPos(kv.Key); kv.Value.rectTransform.SetAsLastSibling(); }

        foreach (var kv in fireBorders)
            if (kv.Value) { kv.Value.rectTransform.sizeDelta = GetCellSize(); kv.Value.rectTransform.anchoredPosition = CellToAnchoredPos(kv.Key); kv.Value.rectTransform.SetAsLastSibling(); }

        foreach (var kv in spikeVisuals)
            if (kv.Value != null && kv.Value.root)
            {
                kv.Value.root.sizeDelta = GetCellSize();
                kv.Value.root.anchoredPosition = CellToAnchoredPos(kv.Key);
                if (kv.Value.low) kv.Value.low.rectTransform.sizeDelta = GetCellSize();
                if (kv.Value.high) kv.Value.high.rectTransform.sizeDelta = GetCellSize();
            }
    }
    
}
