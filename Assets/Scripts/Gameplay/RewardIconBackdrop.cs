using UnityEngine;
using UnityEngine.UI;

// A dark plate and slow travelling glints, always drawn behind the reward icon.
[ExecuteAlways]
public sealed class RewardIconBackdrop : MaskableGraphic
{
    public RectTransform icon;
    public Color plateColor = new Color(.025f, .035f, .055f, 1);
    [Min(0f)] public float padding = 6f;
    public RunModRarity rarity;
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
        // The old gold sprite frame covered this rarity-colored frame.
        var oldBorder = image.transform.parent.Find("Icon Border");
        if (oldBorder) oldBorder.gameObject.SetActive(false);
        view.Fit();
        int index = image.transform.GetSiblingIndex();
        if (view.transform.GetSiblingIndex() < index) index--;
        view.transform.SetSiblingIndex(index);
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
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); Rect r = rectTransform.rect;
        FloorAmbientVfx.Quad(vh, r.center, r.size, plateColor);
        int tier = (int)rarity;
        Color glow = color;
        glow.a = highlighted ? .75f + .25f * Mathf.Sin(clock * (2 + tier)) : 1f;
        float edge = 2f + (highlighted ? tier * .45f : 0);
        FloorAmbientVfx.Quad(vh, new Vector2(r.center.x, r.yMin + edge), new Vector2(r.width - edge * 2, edge), glow);
        FloorAmbientVfx.Quad(vh, new Vector2(r.center.x, r.yMax - edge), new Vector2(r.width - edge * 2, edge), glow);
        FloorAmbientVfx.Quad(vh, new Vector2(r.xMin + edge, r.center.y), new Vector2(edge, r.height - edge * 2), glow);
        FloorAmbientVfx.Quad(vh, new Vector2(r.xMax - edge, r.center.y), new Vector2(edge, r.height - edge * 2), glow);
        if (!highlighted) return;
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
}
