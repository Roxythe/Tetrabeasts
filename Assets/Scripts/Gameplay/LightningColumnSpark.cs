using UnityEngine;
using UnityEngine.UI;

// A descending blue-white discharge on both edges, including empty portions of the column.
public sealed class LightningColumnSpark : MaskableGraphic
{
    float progress, opacity = 1;
    float cellHeight;
    public static LightningColumnSpark Create(RectTransform parent, Vector2 contact, Vector2 bottom, Vector2 cellSize)
    {
        var spark = new GameObject("Lightning column travelling spark", typeof(RectTransform), typeof(CanvasRenderer), typeof(LightningColumnSpark)).GetComponent<LightningColumnSpark>();
        spark.transform.SetParent(parent, false);
        spark.rectTransform.anchorMin = spark.rectTransform.anchorMax = new Vector2(.5f, .5f);
        spark.rectTransform.sizeDelta = new Vector2(cellSize.x, Mathf.Max(1, contact.y - bottom.y));
        spark.rectTransform.anchoredPosition = (contact + bottom) * .5f;
        spark.cellHeight = cellSize.y; spark.raycastTarget = false;
        return spark;
    }
    public void SetProgress(float value, float alpha)
    {
        progress = Mathf.Clamp01(value); opacity = Mathf.Clamp01(alpha); SetVerticesDirty();
    }
    public float ProgressPastCell(float bottomInParent)
    {
        Rect r = rectTransform.rect;
        float y = bottomInParent - rectTransform.anchoredPosition.y;
        return y <= r.yMin + .01f ? 1 : Mathf.Clamp01((r.width * .5f + r.yMax - y) / (r.height + r.width));
    }
    Vector2 Path(float distance, int side)
    {
        Rect r = rectTransform.rect;
        float half = r.width * .5f, sign = side == 0 ? -1 : 1;
        distance = Mathf.Clamp(distance, 0, r.height + r.width);
        if (distance < half) return new Vector2(sign * distance, r.yMax);
        if (distance < half + r.height) return new Vector2(sign * half, r.yMax - (distance - half));
        return new Vector2(sign * (half - (distance - half - r.height)), r.yMin);
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear(); Rect r = rectTransform.rect;
        float pixel = Mathf.Max(1, r.width / 32f);
        float head = progress * (r.height + r.width);
        for (int side = 0; side < 2; side++)
        {
            Vector2 headPoint = Path(head, side);
            Vector2 previous = headPoint;
            for (int i = 1; i <= 12; i++)
            {
                float distance = Mathf.Max(0, head - i * cellHeight * .12f);
                Vector2 next = Path(distance, side);
                if (distance > r.width * .5f && distance < r.width * .5f + r.height)
                    next.x += Mathf.Sin(i * 4.7f + progress * 70 + side) * pixel * .7f;
                float alpha = (1 - i / 13f) * opacity;
                Line(mesh, previous, next, pixel * 3.5f, new Color(.05f, .45f, 1f, alpha * .4f));
                Line(mesh, previous, next, pixel, new Color(.8f, .95f, 1f, alpha));
                previous = next;
                if (distance <= 0) break;
            }
            FloorAmbientVfx.Quad(mesh, headPoint, Vector2.one * pixel * 4, new Color(.15f, .6f, 1, opacity * .45f));
            FloorAmbientVfx.Quad(mesh, headPoint, Vector2.one * pixel * 2, new Color(.9f, 1, 1, opacity));
        }
    }
    static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color tint)
    {
        Vector2 d = b - a;
        if (d.sqrMagnitude < .001f) return;
        Vector2 normal = new Vector2(-d.y, d.x).normalized * width * .5f;
        int n = mesh.currentVertCount;
        mesh.AddVert(a - normal, tint, Vector2.zero); mesh.AddVert(a + normal, tint, Vector2.up);
        mesh.AddVert(b + normal, tint, Vector2.one); mesh.AddVert(b - normal, tint, Vector2.right);
        mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
    }
}
