using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class NextPreviewUI : MonoBehaviour
{
    public RectTransform root;      // NextRoot
    public Image tilePrefab;        // same Tile_UI prefab
    readonly List<RectTransform> tiles = new();

    public void Show(TetrominoData data, Color color)
    {
        // clear old
        for (int i = 0; i < tiles.Count; i++) if (tiles[i]) Destroy(tiles[i].gameObject);
        tiles.Clear();
        if (!data || root == null || tilePrefab == null) return;

        // compute a tight bounding box around the tetromino cells
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var c in data.cells)
        {
            minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
            minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
        }
        int w = (maxX - minX + 1);
        int h = (maxY - minY + 1);

        // choose a square tile size that fits inside NextPanel with a little padding
        var r = ((RectTransform)transform).rect;
        float pad = 8f;
        float s = Mathf.Floor(Mathf.Min((r.width - 2 * pad) / w, (r.height - 2 * pad) / h));
        s = Mathf.Max(1f, s);
        Vector2 tileSize = new(s, s);

        // center the shape
        float contentW = w * s, contentH = h * s;
        Vector2 origin = new(-contentW * 0.5f, -contentH * 0.5f);

        foreach (var cell in data.cells)
        {
            var img = Instantiate(tilePrefab, root);
            img.color = color;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = tileSize;

            int lx = cell.x - minX;          // local coords inside bbox
            int ly = cell.y - minY;
            float x = origin.x + (lx + 0.5f) * s;
            float y = origin.y + (ly + 0.5f) * s;

            rt.anchoredPosition = new Vector2(Mathf.Round(x), Mathf.Round(y));
            tiles.Add(rt);
        }
    }

    public void ClearPreview()
    {
        foreach (Transform c in root) Destroy(c.gameObject);
    }
}
