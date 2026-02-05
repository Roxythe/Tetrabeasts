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

    public event Action<Vector2Int, MonsterData> TileDied; // Event called when a tile's HP reaches 0

    [System.Serializable]
    public struct MonsterInstance
    {
        public MonsterData data;

        // Current runtime HP
        public float hp;

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
    }

    void Start()
    {
        RecomputeCellMetrics();
        DrawGridOverlay();
    }

    void Update()
    {
        // Healers pulse over time
        if (monsters.Count == 0 || healTimers.Count == 0) return;

        if (_gc && !_gc.IsRoundActive) return; // No healing outside active rounds

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

            healTimers[k] = Time.time; // Tick consumed (even if no target)

            if (best.HasValue)
            {
                // perform one heal
                if (HealTile(best.Value, finalHeal))
                {
                    PlayHealVFX(best.Value, md.healSprite, 0.5f); // VFX call

                    // Random heal SFX
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

    public bool IsFree(Vector2Int c) => InBounds(c) && !placed.ContainsKey(c);

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
        var gc = FindFirstObjectByType<GameController>(); // or cached reference

        for (int y = 0; y < height; y++)
        {
            bool full = true;
            for (int x = 0; x < width; x++)
                if (!placed.ContainsKey(new Vector2Int(x, y))) { full = false; break; }
            if (!full) continue;

            rowsCleared++;

            // Tally this row BEFORE removal
            int dmgRow = 0;
            var counts = new Dictionary<MonsterData, int>();
            for (int x = 0; x < width; x++)
            {
                var key = new Vector2Int(x, y);
                if (monsters.TryGetValue(key, out var inst) && inst.data)
                {
                    if (inst.hp > 0f)
                    {
                        float dmgMult = (gc ? gc.monsterDamageMult : 1f);
                        float gaugeMult = (gc ? gc.monsterSpecialGainMult : 1f);

                        int dmg = Mathf.RoundToInt(inst.data.attackPower * dmgMult) + inst.attackBonus;
                        dmgRow += Mathf.Max(1, dmg); // Alive monsters always contribute at least 1 damage

                        float gauge = inst.data.specialGaugeGain * gaugeMult;
                        specialChargeFromMonsters += Mathf.Max(1f, gauge); // Alive monsters always contribute at least 1 gauge
                    }

                    // Count for dominant monster pick
                    if (!counts.ContainsKey(inst.data))
                        counts[inst.data] = 0;

                    counts[inst.data] += 1;
                }
            }

            damageFromMonsters += dmgRow;
            rowDamage[y] = dmgRow;

            // Pick dominant
            if (counts.Count > 0)
            {
                int max = 0;
                foreach (var kv in counts) if (kv.Value > max) max = kv.Value;
                var candidates = new List<MonsterData>();
                foreach (var kv in counts) if (kv.Value == max) candidates.Add(kv.Key);
                var dominant = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                rowDominantMonster[y] = dominant;
            }

            // Remove this row 
            for (int x = 0; x < width; x++)
            {
                var key = new Vector2Int(x, y);

                if (monsters.ContainsKey(key)) monsters.Remove(key);

                if (placed.TryGetValue(key, out var rt))
                {
                    UnityEngine.Object.Destroy(rt.gameObject);
                    placed.Remove(key);
                    removedCells.Add(key);
                }
            }

            // Shift down tiles + monsters above 
            for (int yy = y + 1; yy < height; yy++)
            {
                for (int x = 0; x < width; x++)
                {
                    var from = new Vector2Int(x, yy);
                    var to = new Vector2Int(x, yy - 1);

                    if (healTimers.TryGetValue(from, out var t))
                    {
                        healTimers.Remove(from);
                        healTimers[to] = t;
                    }

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

            y--; // Re-check same row after shift
        }

        CleanOrphanedTiles();
    }

    // ================ Character Special Abilities ================

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

    public int ReviveAllTilesToFull(List<Vector2Int> changedCells)
    {
        if (changedCells == null) changedCells = new List<Vector2Int>();

        // Snapshot the keys so we can safely write back into the dictionary
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

    public void RemoveCellsAndFall(IEnumerable<Vector2Int> toRemove,
                               out List<Vector2Int> removedCells,
                               out int damageFromMonsters,
                               out float specialChargeFromMonsters)
    {
        removedCells = new List<Vector2Int>();
        damageFromMonsters = 0;
        specialChargeFromMonsters = 0f;
        var gc = FindFirstObjectByType<GameController>();

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
                        float dmgMult = (gc ? gc.monsterDamageMult : 1f);
                        float gaugeMult = (gc ? gc.monsterSpecialGainMult : 1f);

                        int dmg = Mathf.RoundToInt(inst.data.attackPower * dmgMult) + inst.attackBonus;
                        damageFromMonsters += Mathf.Max(1, dmg); // Alive monsters always contribute at least 1 damage

                        float gauge = inst.data.specialGaugeGain * gaugeMult;
                        specialChargeFromMonsters += Mathf.Max(1f, gauge); // Alive monsters always contribute at least 1 gauge
                    }
                    // dead monsters contribute 0 to damage/gauge
                }

                monsters.Remove(key);
            }

            if (placed.TryGetValue(key, out var rt))
            {
                Destroy(rt.gameObject);
                placed.Remove(key);
                removedCells.Add(key);
            }
        }

        // Sparse drop only move tiles down by the count of removed rows below them
        for (int x = 0; x < width; x++)
        {
            // Build a sorted list of removed rows in this column
            var removedRows = new List<int>();
            for (int y = 0; y < height; y++)
            {
                var key = new Vector2Int(x, y);
                if (!placed.ContainsKey(key))
                    continue;
            }

            // Find removed rows in this column
            foreach (var rc in removedCells)
                if (rc.x == x)
                    removedRows.Add(rc.y);
            if (removedRows.Count == 0)
                continue;
            removedRows.Sort(); // Ascending order

            var moves = new List<(Vector2Int from, Vector2Int to, RectTransform rt)>();
            var monsterMoves = new List<(Vector2Int from, Vector2Int to, Board.MonsterInstance inst)>();

            for (int y = 0; y < height; y++)
            {
                var from = new Vector2Int(x, y);
                if (!placed.TryGetValue(from, out var rt)) continue;

                int drop = 0;
                for (int i = 0; i < removedRows.Count; i++)
                    if (removedRows[i] < y) drop++;
                    else break;

                if (drop > 0)
                {
                    var to = new Vector2Int(x, y - drop);
                    moves.Add((from, to, rt));

                    if (monsters.TryGetValue(from, out var inst))
                        monsterMoves.Add((from, to, inst));

                    if (healTimers.TryGetValue(from, out var t))
                    {
                        healTimers.Remove(from);
                        healTimers[to] = t;
                    }
                }
            }

            // Apply moves
            foreach (var m in moves)
            {
                placed.Remove(m.from);
                placed[m.to] = m.rt;
                m.rt.anchoredPosition = CellToAnchoredPos(m.to);
            }
            foreach (var mm in monsterMoves)
            {
                monsters.Remove(mm.from);
                monsters[mm.to] = mm.inst;
            }
        }

        CleanOrphanedTiles();
    }

    public void SettleAllColumns()
    {
        for (int x = 0; x < width; x++)
        {
            int writeY = 0;
            for (int y = 0; y < height; y++)
            {
                var from = new Vector2Int(x, y);
                if (placed.TryGetValue(from, out var rt))
                {
                    var to = new Vector2Int(x, writeY);
                    if (to != from)
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

                    if (healTimers.TryGetValue(from, out var t))
                    {
                        healTimers.Remove(from);
                        healTimers[to] = t;
                    }

                    writeY++;
                }
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
            kv.Value.anchoredPosition = CellToAnchoredPos(kv.Key);
        DrawGridOverlay();
    }

    
    public Vector2 GetCellSize() => cellSize; // Helper so others can read cell size

    public void SetMonsterAt(Vector2Int cell, MonsterInstance inst)
    {
        if (inst.data)
        {
            inst.RecalcFromData(setHpToMax: false); // Ensure runtime stats exist (and clamp hp to new max)

            // If hp was not initialized, start at full.
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
}
