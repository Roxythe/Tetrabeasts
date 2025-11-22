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
        for (int i = 0; i < tiles.Count; i++)
            if (tiles[i])
                Destroy(tiles[i].gameObject);

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
            var tileImg = Instantiate(tilePrefab, root); 
            tileImg.color = color;
            var rt = tileImg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = tileSize;

            int lx = cell.x - minX, ly = cell.y - minY;
            float x = origin.x + (lx + 0.5f) * s;
            float y = origin.y + (ly + 0.5f) * s;
            rt.anchoredPosition = new Vector2(Mathf.Round(x), Mathf.Round(y));
            tiles.Add(rt);

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
        }
    }

    public void ClearPreview()
    {
        foreach (Transform c in root) Destroy(c.gameObject);
        tiles.Clear();
    }
}
