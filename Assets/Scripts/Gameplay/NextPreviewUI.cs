using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NextPreviewUI : MonoBehaviour
{
    public RectTransform root;      // NextRoot
    public Image tilePrefab;        // same Tile_UI prefab
    readonly List<RectTransform> tiles = new();

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
        float pad = 0.25f; // Padding in preview area
        float s = Mathf.Min((r.width - 2 * pad) / w, (r.height - 2 * pad) / h); // no Floor
        s = Mathf.Max(1f, s);
        Vector2 tileSize = new(s, s);

        float contentW = w * s, contentH = h * s;
        Vector2 origin = new(-contentW * 0.5f, -contentH * 0.5f);

        for (int i = 0; i < data.cells.Length; i++)
        {
            var cell = data.cells[i];

            // root == border
            var tileImg = Instantiate(tilePrefab, root);
            tileImg.sprite = null;                     // border only
            tileImg.raycastTarget = false;
            var outline = tileImg.GetComponent<UnityEngine.UI.Outline>();
            if (outline) Destroy(outline);

            var rt = tileImg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = tileSize;
            rt.anchoredPosition = new Vector2(
                Mathf.Round(origin.x + (cell.x - minX + 0.5f) * s),
                Mathf.Round(origin.y + (cell.y - minY + 0.5f) * s)
            );

            // match board outline color when immune (optional)
            var gc = GetComponent<GameController>();
            var boardCmp = GetComponent<Board>();
            Color borderColor = Color.black;
            if (boardCmp)
                borderColor = (gc && gc.immunityActive) ? boardCmp.immuneBorderColor : boardCmp.normalBorderColor;
            tileImg.color = borderColor;

            // inner fill
            float BORDER = Mathf.Max(2f, Mathf.Round(s * 0.08f));
            var fillGO = new GameObject("PreviewFill", typeof(Image));
            var fill = fillGO.GetComponent<Image>();
            fill.sprite = null;
            fill.raycastTarget = false;
            fill.color = color;

            var frt = fill.rectTransform;
            frt.SetParent(rt, false);
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = rt.sizeDelta - new Vector2(BORDER * 2f, BORDER * 2f);
            frt.anchoredPosition = Vector2.zero;
            frt.SetAsFirstSibling();

            if (isSpecial && data.specialSprite != null)
            {
                var go = new GameObject("SpecialIcon", typeof(Image));
                var iconImg = go.GetComponent<Image>(); 
                iconImg.sprite = data.specialSprite;
                iconImg.preserveAspect = true;

                var prt = iconImg.rectTransform;
                prt.SetParent(rt, false);
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.sizeDelta = rt.sizeDelta - new Vector2(4f, 4f);
                prt.anchoredPosition = Vector2.zero;
            }
            else if (monsters != null && i < monsters.Length && monsters[i] && monsters[i].portrait)
            {
                var go = new GameObject("MonsterPortrait", typeof(Image));
                var portraitImg = go.GetComponent<Image>(); 
                portraitImg.sprite = monsters[i].portrait;
                portraitImg.preserveAspect = true;

                var prt = portraitImg.rectTransform;
                prt.SetParent(rt, false);
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.sizeDelta = rt.sizeDelta - new Vector2(4f, 4f);
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
            var img = rt.GetComponent<UnityEngine.UI.Image>();
            if (img) img.color = c;
        }
    }

    public void ClearPreview()
    {
        foreach (Transform c in root) Destroy(c.gameObject);
        tiles.Clear();
    }
}
