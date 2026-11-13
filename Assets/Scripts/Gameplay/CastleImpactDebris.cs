using UnityEngine;
using UnityEngine.UI;

// One mesh per hit. Each small silhouette is drawn on a pixel grid.
public sealed class CastleImpactDebris : MaskableGraphic
{
    struct Chip
    {
        public Vector2 velocity;
        public float lifetime, brightness;
        public int shape, pixel, turn;
    }

    static readonly string[][] Shapes = {
        new[] { " XX", "XXX", " X " },
        new[] { " X ", " XX", "XXX" },
        new[] { " XX ", "XXXX", " XX " },
        new[] { "X ", "XX", " X" }
    };
    static readonly System.Random Random = new();
    Chip[] chips;
    float elapsed, duration;

    public static void Spawn(RectTransform parent, Vector2 impact, Color wallColor)
    {
        var go = new GameObject("Castle wall chips", typeof(RectTransform), typeof(CanvasRenderer), typeof(CastleImpactDebris));
        var debris = go.GetComponent<CastleImpactDebris>();
        var rect = debris.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = impact;
        rect.SetAsFirstSibling();
        rect.sizeDelta = new Vector2(600f, 800f);
        debris.raycastTarget = false;
        debris.color = wallColor;
        debris.chips = new Chip[Random.Next(1, 6)];
        for (int i = 0; i < debris.chips.Length; i++)
        {
            ref Chip chip = ref debris.chips[i];
            chip.velocity = new Vector2(Random.Next(-126, 127), Random.Next(76, 168));
            chip.lifetime = Random.Next(180, 241) / 100f;
            chip.pixel = Random.Next(2, 5);
            chip.turn = Random.Next(4);
            chip.shape = Random.Next(Shapes.Length);
            chip.brightness = Random.Next(90, 132) / 100f;
            debris.duration = Mathf.Max(debris.duration, chip.lifetime);
        }
    }

    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= duration) Destroy(gameObject);
        else SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (chips == null) return;
        foreach (var chip in chips)
        {
            if (elapsed >= chip.lifetime) continue;
            Vector2 pos = chip.velocity * elapsed + Vector2.down * (110f * elapsed * elapsed);
            pos = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
            string[] shape = Shapes[chip.shape];
            int turn = (chip.turn + Mathf.FloorToInt(elapsed * 4f)) % 4;
            for (int y = 0; y < shape.Length; y++)
            for (int x = 0; x < shape[y].Length; x++)
            {
                if (shape[y][x] == ' ') continue;
                Vector2 offset = new Vector2(x - 1, 1 - y);
                for (int k = 0; k < turn; k++) offset = new Vector2(-offset.y, offset.x);
                Color tint = color * (chip.brightness * (1.24f - .21f * y));
                tint.a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.55f * chip.lifetime, chip.lifetime, elapsed));
                Vector2 corner = pos + offset * chip.pixel;
                int n = vh.currentVertCount;
                vh.AddVert(corner, tint, Vector2.zero);
                vh.AddVert(corner + Vector2.up * chip.pixel, tint, Vector2.zero);
                vh.AddVert(corner + Vector2.one * chip.pixel, tint, Vector2.zero);
                vh.AddVert(corner + Vector2.right * chip.pixel, tint, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2);
                vh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}
