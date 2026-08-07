using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NextPreviewUI : MonoBehaviour
{
    public RectTransform root;
    public Image tilePrefab;
    readonly List<RectTransform> tiles = new();
    readonly List<RectTransform> tilePool = new();
    readonly HashSet<Vector2Int> previewCells = new();
    Board cachedBoard;
    GameController cachedGameController;

    static readonly string[] EdgeNames = { "Edge_L", "Edge_R", "Edge_T", "Edge_B" };

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
        foreach (var n in EdgeNames)
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

        ReleaseActiveTiles();

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

        previewCells.Clear();
        foreach (var cell in data.cells)
            previewCells.Add(cell);

        // Match board outline color when immune
        var gc = cachedGameController ? cachedGameController : (cachedGameController = GetComponent<GameController>());
        var boardCmp = cachedBoard ? cachedBoard : (cachedBoard = GetComponent<Board>());
        if (!boardCmp)
            boardCmp = cachedBoard = FindFirstObjectByType<Board>();

        Color borderColor = Color.black;
        if (boardCmp)
            borderColor = (gc && gc.immunityActive) ? boardCmp.immuneBorderColor : boardCmp.normalBorderColor;

        for (int i = 0; i < data.cells.Length; i++)
        {
            var cell = data.cells[i];

            var rt = GetOrCreateTile();
            var tileImg = rt.GetComponent<Image>();
            tileImg.sprite = null;
            tileImg.raycastTarget = false;
            tileImg.gameObject.SetActive(true);

            var outline = tileImg.GetComponent<Outline>();
            if (outline) Destroy(outline);

            rt.SetParent(root, false);
            rt.SetAsLastSibling();
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
            var fill = GetOrCreateChildImage(rt, "PreviewFill");
            fill.sprite = data.backgroundImage ? data.backgroundImage : OnePx();
            fill.type = Image.Type.Simple;
            fill.preserveAspect = false;
            fill.raycastTarget = false;
            fill.color = color;

            var frt = fill.rectTransform;
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = rt.sizeDelta;
            frt.anchoredPosition = Vector2.zero;
            frt.localScale = Vector3.one;
            frt.SetAsFirstSibling();
            if (boardCmp)
                boardCmp.ConfigureTetrominoBackgroundPulse(fill, color, data.backgroundImage);

            // Shared-edge halving
            bool Ls = previewCells.Contains(cell + Vector2Int.left);
            bool Rs = previewCells.Contains(cell + Vector2Int.right);
            bool Us = previewCells.Contains(cell + Vector2Int.up);
            bool Ds = previewCells.Contains(cell + Vector2Int.down);

            float border = Mathf.Max(2f, Mathf.Round(s * 0.08f));
            ApplySharedEdges(rt, Ls, Rs, Us, Ds, border);

            var content = GetOrCreateChildImage(rt, "PreviewContent");
            content.gameObject.SetActive(false);

            // Special icon or monster portrait
            if (isSpecial && data.specialSprite != null)
            {
                content.gameObject.SetActive(true);
                content.name = "PreviewContent";
                content.sprite = data.specialSprite;
                content.preserveAspect = true;
                content.raycastTarget = false;

                var prt = content.rectTransform;
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.sizeDelta = frt.sizeDelta - new Vector2(2f, 2f);
                prt.localScale = Vector3.one * 0.9f;
                prt.anchoredPosition = Vector2.zero;
            }
            else if (monsters != null && i < monsters.Length && monsters[i])
            {
                content.gameObject.SetActive(true);
                content.name = "PreviewContent";

                // Use selected skin variant 
                var portrait = GetCurrentMonsterPortrait(monsters[i]);
                if (!portrait) portrait = monsters[i].portrait; // Fallback
                content.sprite = portrait;

                content.preserveAspect = true;
                content.raycastTarget = false;

                var prt = content.rectTransform;
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.sizeDelta = frt.sizeDelta - new Vector2(2f, 2f);
                prt.localScale = Vector3.one * 0.9f;
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
        ReleaseActiveTiles();
    }

    RectTransform GetOrCreateTile()
    {
        RectTransform rt = null;
        while (tilePool.Count > 0 && !rt)
        {
            int last = tilePool.Count - 1;
            rt = tilePool[last];
            tilePool.RemoveAt(last);
        }

        if (rt)
            return rt;

        return Instantiate(tilePrefab, root).rectTransform;
    }

    Image GetOrCreateChildImage(RectTransform rt, string childName)
    {
        var child = rt.Find(childName);
        if (child && child.TryGetComponent(out Image existing))
        {
            existing.gameObject.SetActive(true);
            return existing;
        }

        var go = new GameObject(childName, typeof(Image));
        go.transform.SetParent(rt, false);

        var img = go.GetComponent<Image>();
        img.sprite = OnePx();
        img.type = Image.Type.Simple;
        img.raycastTarget = false;
        return img;
    }

    void ReleaseActiveTiles()
    {
        for (int i = tiles.Count - 1; i >= 0; i--)
            ReleaseTile(tiles[i]);

        tiles.Clear();
        previewCells.Clear();
    }

    void ReleaseTile(RectTransform rt)
    {
        if (!rt)
            return;

        var content = rt.Find("PreviewContent");
        if (content)
        {
            var img = content.GetComponent<Image>();
            if (img) img.sprite = null;
            content.gameObject.SetActive(false);
        }

        rt.gameObject.SetActive(false);
        tilePool.Add(rt);
    }

    void OnDestroy()
    {
        DestroyTiles(tiles);
        DestroyTiles(tilePool);
        previewCells.Clear();
    }

    void DestroyTiles(List<RectTransform> list)
    {
        for (int i = list.Count - 1; i >= 0; i--)
            if (list[i])
                Destroy(list[i].gameObject);

        list.Clear();
    }

    Sprite GetCurrentMonsterPortrait(MonsterData md)
    {
        if (!md) return null;
        int skin = MonsterSkinStore.GetValidSelected(md);
        return MonsterSkinStore.GetPortrait(md, skin);
    }
}
