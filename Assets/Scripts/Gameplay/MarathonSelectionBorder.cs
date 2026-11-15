using UnityEngine;
using UnityEngine.UI;

// A constant-width pixel frame with travelling highlights; no stretched sprite sheet.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MarathonSelectionBorder : MaskableGraphic
{
    [Min(1f)] public float thickness = 4f;
    [Min(2f)] public float pixelLength = 8f;
    [Min(0f)] public float pixelsPerSecond = 160f;
    public Color highlightColor = new Color(1f, .98f, .72f, 1f);
    float distance;

    protected override void OnEnable() { base.OnEnable(); distance = 0f; }
    void Update()
    {
        if (pixelsPerSecond <= 0f) return;
        distance += Time.unscaledDeltaTime * pixelsPerSecond;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        float edge = Mathf.Min(thickness, Mathf.Min(r.width, r.height) * .5f);
        float perimeter = 2f * (r.width + r.height);
        if (perimeter <= 0f) return;
        float along = 0f;
        Edge(r.width, (start, length) => new Rect(r.xMin + start, r.yMax - edge, length, edge));
        Edge(r.height, (start, length) => Vertical(r.xMax - edge, start, length, false));
        Edge(r.width, (start, length) => new Rect(r.xMax - start - length, r.yMin, length, edge));
        Edge(r.height, (start, length) => Vertical(r.xMin, start, length, true));

        Rect Vertical(float x, float start, float length, bool upwards)
        {
            // Top and bottom own the corners, avoiding double alpha on faded cards.
            float bottom = upwards ? r.yMin + start : r.yMax - start - length;
            float top = Mathf.Min(bottom + length, r.yMax - edge);
            bottom = Mathf.Max(bottom, r.yMin + edge);
            return new Rect(x, bottom, edge, Mathf.Max(0f, top - bottom));
        }

        void Edge(float length, System.Func<float, float, Rect> segmentRect)
        {
            for (float start = 0f; start < length; start += Mathf.Max(2f, pixelLength))
            {
                float span = Mathf.Min(Mathf.Max(2f, pixelLength), length - start);
                float phase = Mathf.Repeat(along + start - distance, perimeter * .5f);
                float glow = pixelsPerSecond > 0f ? 1f - Mathf.Clamp01(phase / 100f) : 0f;
                Color tint = Color.Lerp(color, highlightColor, glow);
                Rect segment = segmentRect(start, span);
                int index = mesh.currentVertCount;
                mesh.AddVert(new Vector3(segment.xMin, segment.yMin), tint, Vector2.zero);
                mesh.AddVert(new Vector3(segment.xMin, segment.yMax), tint, Vector2.up);
                mesh.AddVert(new Vector3(segment.xMax, segment.yMax), tint, Vector2.one);
                mesh.AddVert(new Vector3(segment.xMax, segment.yMin), tint, Vector2.right);
                mesh.AddTriangle(index, index + 1, index + 2);
                mesh.AddTriangle(index, index + 2, index + 3);
            }
            along += length;
        }
    }
}
