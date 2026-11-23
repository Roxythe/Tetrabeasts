using UnityEngine;
using UnityEngine.UI;

// Small deterministic pixel loops: these never consume the gameplay random stream.
public sealed class FloorAmbientVfx : MaskableGraphic
{
    public Board.FloorEffectType kind;
    float clock;
    protected override void Awake() { base.Awake(); raycastTarget = false; }
    void Update() { clock += Time.deltaTime; SetVerticesDirty(); }
    public static void Attach(Image border, Board.FloorEffectType type)
    {
        var existing = border.GetComponentInChildren<FloorAmbientVfx>(true);
        if (existing) { existing.kind = type; return; }
        var fx = new GameObject("Ambient pixels", typeof(RectTransform), typeof(CanvasRenderer), typeof(FloorAmbientVfx)).GetComponent<FloorAmbientVfx>();
        fx.transform.SetParent(border.transform, false);
        fx.rectTransform.anchorMin = Vector2.zero; fx.rectTransform.anchorMax = Vector2.one;
        fx.rectTransform.sizeDelta = Vector2.zero; fx.kind = type;
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); Rect r = rectTransform.rect;
        float pixel = Mathf.Max(1f, r.width / 28f);
        // Keep sparks inside their own cell and lift the rise origin above its bottom edge.
        if (kind == Board.FloorEffectType.Lightning)
            r = new Rect(r.xMin + pixel * 3, r.yMin + pixel * 4, r.width - pixel * 6, r.height - pixel * 6);
        else
            r = new Rect(r.xMin + pixel * 2, r.yMin + r.height * .08f + pixel * 2,
                r.width - pixel * 4, r.height * .92f - pixel * 5);
        for (int i = 0; i < 9; i++)
        {
            float phase = Mathf.Repeat(clock * (kind == Board.FloorEffectType.Poison ? .6f : 1.2f) + i * .137f, 1f);
            float x = Mathf.Lerp(r.xMin + pixel * 2, r.xMax - pixel * 2, Mathf.Repeat(i * .618f, 1));
            if (kind == Board.FloorEffectType.Lightning)
            {
                float along = Mathf.Repeat(i * .113f + clock * .27f, 1f) * 4;
                Vector2 p = along < 1 ? new Vector2(Mathf.Lerp(r.xMin, r.xMax, along), r.yMax) :
                    along < 2 ? new Vector2(r.xMax, Mathf.Lerp(r.yMax, r.yMin, along - 1)) :
                    along < 3 ? new Vector2(Mathf.Lerp(r.xMax, r.xMin, along - 2), r.yMin) :
                    new Vector2(r.xMin, Mathf.Lerp(r.yMin, r.yMax, along - 3));
                Color tint = Color.Lerp(new Color(.1f, .55f, 1, .5f), Color.white, Mathf.PingPong(clock * 7 + i, 1));
                for (int j = -2; j <= 2; j++)
                    Quad(vh, p + new Vector2(j * pixel, (j % 2) * pixel), new Vector2(pixel * 1.5f, pixel), tint);
            }
            else if (kind == Board.FloorEffectType.Poison)
            {
                Vector2 p = new Vector2(x, Mathf.Lerp(r.yMin, r.yMax, phase));
                float radius = pixel * (1 + phase * 1.4f);
                Color tint = new Color(.72f, .2f, 1f, (1 - phase) * .85f);
                for (int j = 0; j < 8; j++)
                {
                    float a = j * Mathf.PI / 4;
                    Quad(vh, p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, Vector2.one * pixel, tint);
                }
            }
            else
            {
                Vector2 p = new Vector2(x + Mathf.Sin(clock * 4 + i) * pixel * 2, Mathf.Lerp(r.yMin, r.yMax, phase));
                Color tint = Color.Lerp(new Color(1, .85f, .1f, 1), new Color(1, .1f, 0, 0), phase);
                Quad(vh, p, new Vector2(pixel, pixel * (3 - phase * 2)), tint);
            }
        }
    }
    public static void Quad(VertexHelper mesh, Vector2 p, Vector2 size, Color tint)
    {
        int n = mesh.currentVertCount; Vector2 h = size * .5f;
        mesh.AddVert(p + new Vector2(-h.x, -h.y), tint, Vector2.zero);
        mesh.AddVert(p + new Vector2(-h.x, h.y), tint, Vector2.up);
        mesh.AddVert(p + new Vector2(h.x, h.y), tint, Vector2.one);
        mesh.AddVert(p + new Vector2(h.x, -h.y), tint, Vector2.right);
        mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
    }
}
