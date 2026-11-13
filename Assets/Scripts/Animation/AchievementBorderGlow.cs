using UnityEngine;
using UnityEngine.UI;

// Only an edge halo: the achievement art and text remain unobscured.
public sealed class AchievementBorderGlow : MaskableGraphic
{
    [SerializeField, Min(1f)] float glowWidth = 12f;
    [SerializeField, Min(.1f)] float pulseSeconds = 2.2f;
    [SerializeField, Range(0f, 1f)] float minimumAlpha = .3f;
    float pulse = 1f;

    protected override void OnEnable() { base.OnEnable(); raycastTarget = false; }
    void Update()
    {
        pulse = Mathf.Lerp(minimumAlpha, 1f, .5f + .5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / pulseSeconds));
        SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        const int bands = 8;
        for (int i = 0; i < bands; i++)
        {
            float distance = (float)i / bands;
            float outer = glowWidth * (1f - distance);
            float inner = glowWidth * (1f - (float)(i + 1) / bands);
            Color tint = color;
            tint.a *= pulse * distance * distance * .7f;
            AddQuad(mesh, r.xMin - outer, r.yMin - outer, r.xMax + outer, r.yMin - inner, tint);
            AddQuad(mesh, r.xMin - outer, r.yMax + inner, r.xMax + outer, r.yMax + outer, tint);
            AddQuad(mesh, r.xMin - outer, r.yMin - inner, r.xMin - inner, r.yMax + inner, tint);
            AddQuad(mesh, r.xMax + inner, r.yMin - inner, r.xMax + outer, r.yMax + inner, tint);
        }
    }
    static void AddQuad(VertexHelper mesh, float left, float bottom, float right, float top, Color tint)
    {
        int n = mesh.currentVertCount;
        mesh.AddVert(new Vector3(left, bottom), tint, Vector2.zero);
        mesh.AddVert(new Vector3(left, top), tint, Vector2.zero);
        mesh.AddVert(new Vector3(right, top), tint, Vector2.zero);
        mesh.AddVert(new Vector3(right, bottom), tint, Vector2.zero);
        mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
    }
}
