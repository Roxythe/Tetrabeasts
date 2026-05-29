using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NextPreviewUI : MonoBehaviour
{
    public RectTransform root;
    public Image tilePrefab;
    readonly List<RectTransform> tiles = new();

    static Sprite _onePx; // Single-pixel white sprite for edge drawing

    static Sprite OnePx()
    {
        if (_onePx) return _onePx;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);
        _onePx = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _onePx;
    }

    Image GetOrCreateEdge(RectTransform rt, string name)
    {
        var t = rt.Find(name);
        if (t)
        {
            var existing = t.GetComponent<Image>();
            if (existing) return existing;
        }

        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(rt, false);

        var edge = go.GetComponent<Image>();
        edge.sprite = OnePx();
        edge.type = Image.Type.Simple;
        edge.raycastTarget = false;
        return edge;
    }

    void EnsureEdges(RectTransform rt)
    {
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
    }

    void SetEdgeColor(RectTransform rt, Color c)
    {
        EnsureEdges(rt);
        foreach (var n in new[] { "Edge_L", "Edge_R", "Edge_T", "Edge_B" })
        {
            var img = rt.Find(n)?.GetComponent<Image>();
            if (img) img.color = c;
        }
    }

    void ApplySharedEdges(RectTransform rt, bool leftShared, bool rightShared, bool topShared, bool bottomShared, float border)
    {
        EnsureEdges(rt);

        float half = border * 0.5f;
        const float OUTER_OVERLAP = 0f;

        float left = leftShared ? half : (border + OUTER_OVERLAP);
        float right = rightShared ? half : (border + OUTER_OVERLAP);
        float top = topShared ? half : (border + OUTER_OVERLAP);
        float bottom = bottomShared ? half : (border + OUTER_OVERLAP);

        rt.Find("Edge_L").GetComponent<RectTransform>().sizeDelta = new Vector2(left, 0f);
        rt.Find("Edge_R").GetComponent<RectTransform>().sizeDelta = new Vector2(right, 0f);
        rt.Find("Edge_T").GetComponent<RectTransform>().sizeDelta = new Vector2(0f, top);
        rt.Find("Edge_B").GetComponent<RectTransform>().sizeDelta = new Vector2(0f, bottom);
    }

    public void Show(TetrominoData data, Color color, MonsterData[] monsters)
    {
        if (!data || root == null || tilePrefab == null)
            return;

        bool isSpecial = data.special != SpecialType.None;

        // Clear old
        foreach (var t in tiles) if (t) Destroy(t.gameObject);
        tiles.Clear();

        // Calculate bounds
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var c in data.cells)
        {
            minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
            minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
        }
        int w = (maxX - minX + 1);
        int h = (maxY - minY + 1);

        var r = root.rect;
        float pad = 0.25f;
        float s = Mathf.Min((r.width - 2 * pad) / w, (r.height - 2 * pad) / h);
        s = Mathf.Max(1f, s);
        Vector2 tileSize = new(s, s);

        float contentW = w * s, contentH = h * s;
        Vector2 origin = new(-contentW * 0.5f, -contentH * 0.5f);

        var previewSet = new HashSet<Vector2Int>(data.cells);

        // Match board outline color when immune
        var gc = GetComponent<GameController>();
        var boardCmp = GetComponent<Board>();
        if (!boardCmp)
            boardCmp = FindFirstObjectByType<Board>();

        Color borderColor = Color.black;
        if (boardCmp)
            borderColor = (gc && gc.immunityActive) ? boardCmp.immuneBorderColor : boardCmp.normalBorderColor;

        for (int i = 0; i < data.cells.Length; i++)
        {
            var cell = data.cells[i];

            var tileImg = Instantiate(tilePrefab, root);
            tileImg.sprite = null;
            tileImg.raycastTarget = false;

            var outline = tileImg.GetComponent<Outline>();
            if (outline) Destroy(outline);

            var rt = tileImg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = tileSize;
            rt.anchoredPosition = new Vector2(
                Mathf.Round(origin.x + (cell.x - minX + 0.5f) * s),
                Mathf.Round(origin.y + (cell.y - minY + 0.5f) * s)
            );

            // Root image invisible edges draw the border
            tileImg.color = new Color(0f, 0f, 0f, 0f);
            SetEdgeColor(rt, borderColor);

            // Inner fill
            var fillGO = new GameObject("PreviewFill", typeof(Image));
            var fill = fillGO.GetComponent<Image>();
            fill.sprite = data.backgroundImage ? data.backgroundImage : OnePx();
            fill.type = Image.Type.Simple;
            fill.preserveAspect = false;
            fill.raycastTarget = false;
            fill.color = color;

            var frt = fill.rectTransform;
            frt.SetParent(rt, false);
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = rt.sizeDelta;
            frt.anchoredPosition = Vector2.zero;
            frt.SetAsFirstSibling();
            if (boardCmp)
                boardCmp.ConfigureTetrominoBackgroundPulse(fill, color, data.backgroundImage, i);

            // Shared-edge halving
            bool Ls = previewSet.Contains(cell + Vector2Int.left);
            bool Rs = previewSet.Contains(cell + Vector2Int.right);
            bool Us = previewSet.Contains(cell + Vector2Int.up);
            bool Ds = previewSet.Contains(cell + Vector2Int.down);

            float border = Mathf.Max(2f, Mathf.Round(s * 0.08f));
            ApplySharedEdges(rt, Ls, Rs, Us, Ds, border);

            // Special icon or monster portrait
            if (isSpecial && data.specialSprite != null)
            {
                var innerRT = (rt.Find("PreviewFill") as RectTransform) ?? rt;

                var go = new GameObject("SpecialIcon", typeof(Image));
                var iconImg = go.GetComponent<Image>();
                iconImg.sprite = data.specialSprite;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;

                var prt = iconImg.rectTransform;
                prt.SetParent(innerRT, false);
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.sizeDelta = innerRT.sizeDelta - new Vector2(2f, 2f);
                prt.localScale = Vector3.one * 0.9f;
                prt.anchoredPosition = Vector2.zero;
            }
            else if (monsters != null && i < monsters.Length && monsters[i])
            {
                var innerRT = (rt.Find("PreviewFill") as RectTransform) ?? rt;

                var go = new GameObject("MonsterPortrait", typeof(Image));
                var portraitImg = go.GetComponent<Image>();

                // Use selected skin variant 
                var portrait = GetCurrentMonsterPortrait(monsters[i]);
                if (!portrait) portrait = monsters[i].portrait; // Fallback
                portraitImg.sprite = portrait;

                portraitImg.preserveAspect = true;
                portraitImg.raycastTarget = false;

                var prt = portraitImg.rectTransform;
                prt.SetParent(innerRT, false);
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.sizeDelta = innerRT.sizeDelta - new Vector2(2f, 2f);
                prt.anchoredPosition = Vector2.zero;
            }

            tiles.Add(rt);
        }
    }

    public void SyncBorderToImmunity(bool immune, Color immuneColor, Color normalColor)
    {
        var c = immune ? immuneColor : normalColor;
        foreach (var rt in tiles)
        {
            if (rt) SetEdgeColor(rt, c);
        }
    }

    public void ClearPreview()
    {
        foreach (Transform c in root) Destroy(c.gameObject);
        tiles.Clear();
    }

    Sprite GetCurrentMonsterPortrait(MonsterData md)
    {
        if (!md) return null;
        int skin = MonsterSkinStore.GetValidSelected(md);
        return MonsterSkinStore.GetPortrait(md, skin);
    }
}
