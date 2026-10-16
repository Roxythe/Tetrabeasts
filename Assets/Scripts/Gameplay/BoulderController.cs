using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// One shared simulation for modifier and boss boulders. Coordinates are board cells.
public sealed partial class BoulderController : MonoBehaviour
{
    public Sprite boulderSprite;
    public AudioClip impactSfx;
    public AudioClip[] alternateImpactSfx;
    [Range(0f, 1f)] public float impactVolume = 1f;
    public const int Capacity = 4;
    const int BossVolleySize = 3;
    public const float FlatHitDamage = 5f;
    public static float CalculateHitDamage(float maxHealth, float fraction) => FlatHitDamage + Mathf.Max(0f, maxHealth) * Mathf.Clamp(fraction, 0.10f, 0.15f);
    public static float SpawnIntervalScale(float elapsed) => Mathf.Lerp(1f, 0.4f, Mathf.Clamp01(elapsed / 180f));
    const float Radius = 0.42f;
    sealed class Rock
    {
        public Vector2 position, velocity;
        public Image image;
        public int bounces;
        public readonly HashSet<Vector2Int> contacts = new();
        public readonly System.Predicate<Vector2Int> endedContact;
        public Rock() { endedContact = cell => !Overlaps(position, cell, Radius + .04f); }
    }
    sealed class Pixel
    {
        public Image image;
        public Vector2 velocity;
        public float life = 0.5f;
        public float shade;
        public float spin;
    }
    readonly List<Rock> rocks = new();
    readonly List<Pixel> pixels = new();
    readonly Queue<Pixel> sparePixels = new();
    const int DebrisCapacity = 96;
    RectTransform effectsRoot;
    AudioClip lastImpactClip;
    float nextImpactTime;
    readonly Vector3[] screenCorners = new Vector3[4];
    GameController gc;
    Board board;
    float countdown = 5f;
    float modifierElapsed;
    bool bossVolleyPending;
    bool lastFlipped;
    bool Flipped => gc && gc.levelModifierController && gc.levelModifierController.IsBoardFlipped;
    public int ActiveCount => rocks.Count;
    public bool CanSpawnVolley => rocks.Count == 0 && !bossVolleyPending;

    void Awake() { gc = GetComponent<GameController>(); }
    public void RequestBossVolley() { bossVolleyPending = true; }
    void Update()
    {
        if (!gc) return;
        if (!board) board = gc.gameBoard;
        // Warm a bounded pool over several frames before the first modifier wave.
        if (board && board.gridRoot) WarmDebrisPool();
        if (!board || !gc.IsRoundActive) return;
        if (lastFlipped != Flipped)
        {
            foreach (var rock in rocks) rock.contacts.Clear();
            lastFlipped = Flipped;
        }
        if (bossVolleyPending && rocks.Count == 0)
        {
            bossVolleyPending = false;
            for (int i = 0; i < BossVolleySize; i++) Spawn((i + 0.5f) * board.width / BossVolleySize);
        }
        bool enabledByModifier = gc.levelModifierController &&
            gc.levelModifierController.HasModifier(LevelModifierKind.BouldersAway);
        if (enabledByModifier) modifierElapsed += Time.deltaTime;
        if (enabledByModifier && rocks.Count < Capacity && !bossVolleyPending)
        {
            countdown -= Time.deltaTime;
            if (countdown <= 0f)
            {
                SpawnModifierWave();
                countdown = Random.Range(5f, 10f) * SpawnIntervalScale(modifierElapsed);
            }
        }
        // Substeps prevent fast rocks tunnelling through a tile or each other.
        float remaining = Mathf.Min(Time.deltaTime, 0.25f);
        while (remaining > 0f)
        {
            float dt = Mathf.Min(remaining, 1f / 120f);
            Simulate(dt);
            remaining -= dt;
        }
        foreach (var rock in rocks)
        {
            rock.image.rectTransform.anchoredPosition = ToLocal(rock.position);
            rock.image.rectTransform.Rotate(0f, 0f, -rock.velocity.x * 90f * Time.deltaTime);
        }
        if (effectsRoot && effectsRoot.GetSiblingIndex() != board.gridRoot.childCount - 1)
            effectsRoot.SetAsLastSibling();
        for (int i = pixels.Count - 1; i >= 0; i--)
        {
            var p = pixels[i];
            p.life -= Time.deltaTime;
            if (p.life <= 0f) { p.image.gameObject.SetActive(false); sparePixels.Enqueue(p); pixels.RemoveAt(i); continue; }
            p.velocity += (Flipped ? Vector2.up : Vector2.down) * 500f * Time.deltaTime;
            p.image.rectTransform.anchoredPosition += p.velocity * Time.deltaTime;
            p.image.color = new Color(p.shade, p.shade, p.shade, Mathf.Clamp01(p.life / 0.2f));
            p.image.rectTransform.Rotate(0, 0, p.spin * Time.deltaTime);
        }
    }
    void SpawnModifierWave()
    {
        int count = Mathf.Min(Random.Range(1, 4), Capacity - rocks.Count);
        // Pick separated lanes and reject lanes occupied by a newly spawned rock.
        var lanes = new List<float>();
        for (float x = Radius; x <= board.width - Radius; x += Radius * 2f + 0.15f)
            if (!rocks.Exists(r => Vector2.Distance(r.position, new Vector2(x, board.height + Radius)) < Radius * 2f + 0.1f)) lanes.Add(x);
        for (int i = 0; i < count && lanes.Count > 0; i++)
        {
            int index = Random.Range(0, lanes.Count);
            Spawn(lanes[index]); lanes.RemoveAt(index);
        }
    }
    void Spawn(float x)
    {
        if (rocks.Count >= Capacity) return;
        var rock = new Rock { position = new Vector2(x, board.height + Radius),
            velocity = new Vector2(Random.Range(-2f, 2f), -1f), bounces = Random.Range(4, 8) };
        rock.image = CreateImage("Boulder", board.GetCellSize() * (Radius * 2f));
        rock.image.sprite = boulderSprite;
        rock.image.preserveAspect = true;
        rock.image.rectTransform.anchoredPosition = ToLocal(rock.position);
        rocks.Add(rock);
    }
    void Simulate(float dt)
    {
        foreach (var rock in rocks)
        {
            rock.velocity += Vector2.down * (12f * dt);
            rock.velocity = Vector2.ClampMagnitude(rock.velocity, 18f);
            rock.position += rock.velocity * dt;
            Vector2 normal = Vector2.zero;
            if (rock.position.x < Radius) { rock.position.x = Radius; normal = Vector2.right; }
            else if (rock.position.x > board.width - Radius) { rock.position.x = board.width - Radius; normal = Vector2.left; }
            if (normal != Vector2.zero && Vector2.Dot(rock.velocity, normal) < 0f) Bounce(rock, normal);
            if (rock.bounces <= 0) continue;
            // Prevent successive damage while still touching the same target.
            rock.contacts.RemoveWhere(rock.endedContact);
            int minX = Mathf.Max(0, Mathf.FloorToInt(rock.position.x - Radius));
            int maxX = Mathf.Min(board.width - 1, Mathf.FloorToInt(rock.position.x + Radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(rock.position.y - Radius));
            int maxY = Mathf.Min(board.height - 1, Mathf.FloorToInt(rock.position.y + Radius));
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                if (rock.bounces <= 0) break;
                var cell = new Vector2Int(x, y);
                var targetCell = Flipped ? new Vector2Int(board.width - 1 - x, board.height - 1 - y) : cell;
                if (!board.IsBoulderTarget(targetCell) || !Overlaps(rock.position, cell, Radius)) continue;
                Vector2 nearest = new Vector2(Mathf.Clamp(rock.position.x, x, x + 1), Mathf.Clamp(rock.position.y, y, y + 1));
                Vector2 delta = rock.position - nearest;
                Vector2 n = delta.sqrMagnitude > 0.00001f ? delta.normalized : -rock.velocity.normalized;
                if (n == Vector2.zero) n = Vector2.up;
                rock.position += n * Mathf.Max(0.001f, Radius - delta.magnitude);
                if (!rock.contacts.Add(cell)) continue;
                if (board.StrikeWithBoulder(targetCell)) rock.bounces--;
                Bounce(rock, n);
            }
        }
        for (int i = 0; i < rocks.Count; i++)
        for (int j = i + 1; j < rocks.Count; j++)
        {
            var a = rocks[i]; var b = rocks[j];
            if (a.bounces <= 0 || b.bounces <= 0) continue;
            Vector2 delta = a.position - b.position;
            float distance = delta.magnitude;
            if (distance >= Radius * 2) continue;
            Vector2 n = distance > 0.00001f ? delta / distance : Vector2.right;
            a.position += n * (Radius - distance * 0.5f + 0.001f);
            b.position -= n * (Radius - distance * 0.5f + 0.001f);
            float approach = Vector2.Dot(a.velocity - b.velocity, n);
            if (approach >= 0f) continue;
            a.velocity -= n * approach; b.velocity += n * approach;
            Burst((a.position + b.position) * 0.5f, n);
            Burst((a.position + b.position) * 0.5f, -n);
            PlayImpact();
        }
        for (int i = rocks.Count - 1; i >= 0; i--)
            if (rocks[i].bounces <= 0 || IsBelowScreen(rocks[i])) { Destroy(rocks[i].image.gameObject); rocks.RemoveAt(i); }
    }
    bool IsBelowScreen(Rock rock)
    {
        if (rock.position.y >= -Radius) return false;
        var canvas = board.gridRoot.GetComponentInParent<Canvas>();
        if (!canvas) return rock.position.y < -board.height;
        canvas = canvas.rootCanvas;
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        // Project the entire image at its simulated position, including board rotation.
        var rect = rock.image.rectTransform;
        rect.anchoredPosition = ToLocal(rock.position);
        rect.GetWorldCorners(screenCorners);
        foreach (var corner in screenCorners)
            if (RectTransformUtility.WorldToScreenPoint(camera, corner).y >= 0f) return false;
        return true;
    }
    void PlayImpact()
    {
        if (!AudioManager.I || Time.unscaledTime < nextImpactTime) return;
        // Reservoir selection avoids allocations and immediate repeats, including the original cue.
        AudioClip selected = null;
        int candidates = 0;
        if (impactSfx && impactSfx != lastImpactClip) { selected = impactSfx; candidates++; }
        if (alternateImpactSfx != null)
            foreach (var clip in alternateImpactSfx)
                if (clip && clip != lastImpactClip && Random.Range(0, ++candidates) == 0) selected = clip;
        if (!selected) selected = impactSfx ? impactSfx : lastImpactClip;
        if (!selected) return;
        lastImpactClip = selected;
        nextImpactTime = Time.unscaledTime + .04f;
        AudioManager.I.PlaySFX(selected, impactVolume);
    }
    static bool Overlaps(Vector2 p, Vector2Int c, float radius)
    {
        var near = new Vector2(Mathf.Clamp(p.x, c.x, c.x + 1), Mathf.Clamp(p.y, c.y, c.y + 1));
        return (p - near).sqrMagnitude < radius * radius;
    }
    void Bounce(Rock rock, Vector2 normal)
    {
        float speed = Vector2.Dot(rock.velocity, normal);
        if (speed < 0f) rock.velocity -= normal * speed * 1.8f;
        if (normal.y > 0.5f) rock.velocity.y = Mathf.Max(4f, rock.velocity.y);
        Burst(rock.position - normal * Radius, normal);
        PlayImpact();
    }
    Vector2 ToLocal(Vector2 p)
    {
        // Gravity remains screen-down when Chaos rotates the board underneath us.
        if (Flipped) p = new Vector2(board.width - p.x, board.height - p.y);
        return board.CellToAnchoredPos(Vector2Int.zero) + Vector2.Scale(p - Vector2.one * 0.5f, board.GetCellSize());
    }
    Image CreateImage(string label, Vector2 size)
    {
        EnsureEffectsRoot();
        var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(effectsRoot, false);
        var image = go.GetComponent<Image>(); image.raycastTarget = false;
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        image.rectTransform.sizeDelta = size;
        return image;
    }
    void EnsureEffectsRoot()
    {
        if (effectsRoot) return;
        // Isolate changing debris geometry from the board's much larger UI canvas.
        effectsRoot = new GameObject("Boulder Effects", typeof(RectTransform), typeof(Canvas)).GetComponent<RectTransform>();
        effectsRoot.SetParent(board.gridRoot, false);
        effectsRoot.anchorMin = effectsRoot.anchorMax = effectsRoot.pivot = Vector2.one * .5f;
        effectsRoot.sizeDelta = Vector2.zero;
    }
    Pixel CreateDebris()
    {
        var image = CreateImage("Rock chip", Vector2.one);
        image.gameObject.SetActive(false);
        return new Pixel { image = image };
    }
    void WarmDebrisPool()
    {
        for (int i = 0; i < 8 && pixels.Count + sparePixels.Count < DebrisCapacity; i++)
            sparePixels.Enqueue(CreateDebris());
    }
    void Burst(Vector2 p, Vector2 normal)
    {
        for (int i = 0; i < 8; i++)
        {
            Pixel chip;
            if (sparePixels.Count > 0) chip = sparePixels.Dequeue();
            else if (pixels.Count < DebrisCapacity) chip = CreateDebris();
            else { chip = pixels[0]; pixels.RemoveAt(0); }
            var image = chip.image;
            // Reuse the actual irregular rock art instead of a solid UI quad.
            image.sprite = boulderSprite;
            image.preserveAspect = true;
            float size = Random.Range(3f, 5.5f);
            image.rectTransform.sizeDelta = new Vector2(size, size * Random.Range(.75f, 1.15f));
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            image.rectTransform.anchoredPosition = ToLocal(p);
            Vector2 direction = (Quaternion.Euler(0, 0, Random.Range(-75f, 75f)) * normal).normalized;
            if (Flipped) direction = -direction;
            float shade = Random.Range(0.7f, 1f);
            image.color = new Color(shade, shade, shade, 1f);
            chip.velocity = direction * Random.Range(90f, 190f);
            chip.shade = shade;
            chip.life = Random.Range(.25f, .4f);
            chip.spin = Random.Range(-240f, 240f);
            image.gameObject.SetActive(true);
            pixels.Add(chip);
        }
    }
    public void Clear()
    {
        foreach (var r in rocks) if (r.image) Destroy(r.image.gameObject);
        foreach (var p in pixels) if (p.image) { p.image.gameObject.SetActive(false); sparePixels.Enqueue(p); }
        rocks.Clear(); pixels.Clear(); bossVolleyPending = false;
        countdown = Random.Range(5f, 10f);
        modifierElapsed = 0f;
        nextImpactTime = 0f;
    }
    void OnDisable() => Clear();
    void OnDestroy()
    {
        if (!effectsRoot) return;
        if (Application.isPlaying) Destroy(effectsRoot.gameObject);
        else DestroyImmediate(effectsRoot.gameObject);
    }
}
