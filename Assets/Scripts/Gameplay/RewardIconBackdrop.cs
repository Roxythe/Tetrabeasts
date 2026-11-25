using UnityEngine;
using UnityEngine.UI;

// The plate sits behind the icon; the rarity-specific celebration sits above it.
[ExecuteAlways]
public sealed class RewardIconBackdrop : MaskableGraphic
{
    public RectTransform icon;
    public Color plateColor = new Color(.025f, .035f, .055f, 1);
    [Min(0f)] public float padding = 6f;
    public RunModRarity rarity;
    public bool isDebuff;
    public Image plate;
    bool highlighted;
    float clock;
    public void SetHighlighted(bool value)
    {
        if (highlighted == value) return;
        highlighted = value; SetVerticesDirty();
    }
    public static RewardIconBackdrop Attach(Image image, Color rarity)
    {
        string name = image.name + " Animated Backdrop";
        var existing = image.transform.parent.Find(name);
        var view = existing ? existing.GetComponent<RewardIconBackdrop>() : null;
        if (!view)
        {
            view = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RewardIconBackdrop)).GetComponent<RewardIconBackdrop>();
            view.transform.SetParent(image.transform.parent, false);
        }
        view.icon = image.rectTransform; view.color = rarity; view.raycastTarget = false;
        var plateObject = image.transform.parent.Find(image.name + " Dark Plate");
        view.plate = plateObject ? plateObject.GetComponent<Image>() : null;
        if (!view.plate)
        {
            view.plate = new GameObject(image.name + " Dark Plate", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            view.plate.transform.SetParent(image.transform.parent, false);
        }
        view.plate.color = view.plateColor; view.plate.raycastTarget = false;
        // The old gold sprite frame covered this rarity-colored frame.
        var oldBorder = image.transform.parent.Find("Icon Border");
        if (oldBorder) oldBorder.gameObject.SetActive(false);
        view.Fit();
        view.plate.transform.SetAsFirstSibling();
        view.transform.SetAsLastSibling();
        view.SetAllDirty();
        return view;
    }
    void Update() { Fit(); if (highlighted) { clock += Time.unscaledDeltaTime; SetVerticesDirty(); } }
    void Fit()
    {
        if (!icon) return;
        rectTransform.anchorMin = icon.anchorMin; rectTransform.anchorMax = icon.anchorMax;
        rectTransform.pivot = icon.pivot; rectTransform.anchoredPosition = icon.anchoredPosition;
        rectTransform.sizeDelta = icon.sizeDelta + Vector2.one * padding * 2;
        rectTransform.localScale = icon.localScale;
        if (plate)
        {
            var r = plate.rectTransform;
            r.anchorMin = rectTransform.anchorMin; r.anchorMax = rectTransform.anchorMax;
            r.pivot = rectTransform.pivot; r.anchoredPosition = rectTransform.anchoredPosition;
            r.sizeDelta = rectTransform.sizeDelta; r.localScale = rectTransform.localScale;
        }
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); Rect r = rectTransform.rect;
        int tier = (int)rarity;
        Color glow = color;
        glow.a = highlighted ? .75f + .25f * Mathf.Sin(clock * (2 + tier)) : 1f;
        float edge = 2f + (highlighted ? tier * .45f : 0);
        FloorAmbientVfx.Quad(vh, new Vector2(r.center.x, r.yMin + edge), new Vector2(r.width - edge * 2, edge), glow);
        FloorAmbientVfx.Quad(vh, new Vector2(r.center.x, r.yMax - edge), new Vector2(r.width - edge * 2, edge), glow);
        FloorAmbientVfx.Quad(vh, new Vector2(r.xMin + edge, r.center.y), new Vector2(edge, r.height - edge * 2), glow);
        FloorAmbientVfx.Quad(vh, new Vector2(r.xMax - edge, r.center.y), new Vector2(edge, r.height - edge * 2), glow);
        if (!highlighted) return;
        if (tier > 0) { DrawRarityEffect(vh, r, tier); return; }
        int sparks = 2 + tier * 2;
        for (int i = 0; i < sparks; i++)
        {
            float p = Mathf.Repeat(clock * (.16f + tier * .06f) + (float)i / sparks, 1) * 4;
            Vector2 point = p < 1 ? new Vector2(Mathf.Lerp(r.xMin + edge, r.xMax - edge, p), r.yMax - edge) :
                p < 2 ? new Vector2(r.xMax - edge, Mathf.Lerp(r.yMax - edge, r.yMin + edge, p - 1)) :
                p < 3 ? new Vector2(Mathf.Lerp(r.xMax - edge, r.xMin + edge, p - 2), r.yMin + edge) :
                new Vector2(r.xMin + edge, Mathf.Lerp(r.yMin + edge, r.yMax - edge, p - 3));
            float size = 3 + tier * 1.2f;
            if (tier > 0)
            {
                Color halo = color; halo.a = .3f;
                FloorAmbientVfx.Quad(vh, point, Vector2.one * size * 2, halo);
                if (tier >= 2)
                {
                    FloorAmbientVfx.Quad(vh, point, new Vector2(size * 2.6f, 1.5f), glow);
                    FloorAmbientVfx.Quad(vh, point, new Vector2(1.5f, size * 2.6f), glow);
                }
            }
            FloorAmbientVfx.Quad(vh, point, Vector2.one * size, Color.Lerp(color, Color.white, .85f));
        }
    }

    void DrawRarityEffect(VertexHelper mesh, Rect rect, int tier)
    {
        float radius = Mathf.Min(rect.width, rect.height) * .52f;
        Color bright = Color.Lerp(color, isDebuff ? new Color(.6f, .05f, .16f) : Color.white, isDebuff ? .32f : .65f);
        Vector2 Polar(float angle, float distance) => rect.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        Color Alpha(Color tint, float a) { tint.a = a; return tint; }
        if (tier == 1)
        {
            // Bright ascending motes / falling embers, staggered around the perimeter.
            for (int i = 0; i < 9; i++)
            {
                float t = Mathf.Repeat(clock * .65f + i * .137f, 1);
                Vector2 p = Polar(i * 2.4f, radius * (.86f + .2f * t));
                p.y += (isDebuff ? -1 : 1) * t * 16;
                float a = Mathf.Sin(t * Mathf.PI);
                Diamond(mesh, p, new Vector2(2.5f, isDebuff ? 5 : 3.5f) * a, Alpha(bright, a));
                if (!isDebuff) Line(mesh, p - Vector2.right * 4 * a, p + Vector2.right * 4 * a, 1, Alpha(color, a));
            }
        }
        else if (tier == 2)
        {
            // Broken arcs leap between three perimeter nodes; cursed arcs claw inward.
            for (int i = 0; i < 3; i++)
            {
                float phase = clock * (isDebuff ? -.55f : .8f) + i * Mathf.PI * 2 / 3;
                Vector2 previous = Polar(phase, radius);
                for (int j = 1; j <= 9; j++)
                {
                    float angle = phase + j * .14f;
                    float r = radius + Mathf.Sin(j * 3.7f + Mathf.Floor(clock * 12)) * 4;
                    if (isDebuff) r -= Mathf.Sin(j / 9f * Mathf.PI) * 9;
                    Vector2 next = Polar(angle, r);
                    Line(mesh, previous, next, 5, Alpha(color, .25f));
                    Line(mesh, previous, next, 1.5f, Alpha(bright, .9f)); previous = next;
                }
                Diamond(mesh, previous, Vector2.one * 3.5f, bright);
            }
        }
        else if (tier == 3)
        {
            // Broad comet ribbons flare outward for buffs and coil inward for curses.
            for (int i = 0; i < 5; i++)
            {
                float burst = Mathf.Repeat(clock * .6f + i * .2f, 1);
                float angle = i * Mathf.PI * .4f + clock * (isDebuff ? -.4f : .4f);
                Vector2 previous = Polar(angle, radius);
                for (int j = 1; j <= 12; j++)
                {
                    float trail = j / 12f;
                    float r = radius * (isDebuff ? 1.28f - .4f * trail : .82f + .42f * trail);
                    Vector2 next = Polar(angle + trail * (isDebuff ? -1.3f : .9f) + burst * .3f, r);
                    Line(mesh, previous, next, (1 - trail) * 7 + 1, Alpha(color, (1 - trail) * .65f));
                    Line(mesh, previous, next, 1.2f, Alpha(bright, 1 - trail)); previous = next;
                }
            }
        }
        else
        {
            // A radiant corona / jagged eclipse with contracting, red-edged shards.
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI / 8 + clock * (isDebuff ? -.2f : .25f);
                float pulse = .5f + .5f * Mathf.Sin(clock * 4 + i * .8f);
                Vector2 inner = Polar(angle, radius * (isDebuff ? .9f : 1));
                Vector2 outer = Polar(angle + (isDebuff ? .13f : 0), radius * (1.14f + pulse * .2f));
                Line(mesh, inner, outer, isDebuff ? 5 : 3, Alpha(color, .55f + pulse * .4f));
                Line(mesh, inner, outer, 1.3f, bright);
                Diamond(mesh, outer, Vector2.one * (1 + pulse * 2), isDebuff ? new Color(.9f, .12f, .2f) : Color.white);
                Vector2 next = Polar(angle + Mathf.PI / 8, radius * .96f);
                Line(mesh, inner, next, isDebuff ? 4 : 2, Alpha(isDebuff ? new Color(.14f, .01f, .07f) : bright, .7f));
            }
        }
    }
    static void Diamond(VertexHelper mesh, Vector2 center, Vector2 size, Color color)
    {
        int n = mesh.currentVertCount;
        mesh.AddVert(center + Vector2.up * size.y, color, Vector2.zero);
        mesh.AddVert(center + Vector2.right * size.x, color, Vector2.zero);
        mesh.AddVert(center - Vector2.up * size.y, color, Vector2.zero);
        mesh.AddVert(center - Vector2.right * size.x, color, Vector2.zero);
        mesh.AddTriangle(n,n+1,n+2); mesh.AddTriangle(n,n+2,n+3);
    }
    static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 d = b-a; if (d.sqrMagnitude < .001f) return;
        Vector2 normal = new Vector2(-d.y,d.x).normalized * width * .5f;
        int n = mesh.currentVertCount;
        mesh.AddVert(a-normal,color,Vector2.zero); mesh.AddVert(a+normal,color,Vector2.zero);
        mesh.AddVert(b+normal,color,Vector2.zero); mesh.AddVert(b-normal,color,Vector2.zero);
        mesh.AddTriangle(n,n+1,n+2); mesh.AddTriangle(n,n+2,n+3);
    }
}
