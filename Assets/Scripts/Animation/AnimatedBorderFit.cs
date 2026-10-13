using UnityEngine;
using UnityEngine.UI;

// Nine-slices animated artwork without changing the shared sprites or animation clip.
// The existing flame frames have transparent padding around a square inner opening.
[RequireComponent(typeof(Image))]
public sealed class AnimatedBorderFit : BaseMeshEffect
{
    public Vector4 sourceInsets = new Vector4(0.14f, 0.25f, 0.14f, 0.25f);
    [Min(0f)] public float edgeWidth = 22f;
    public override void ModifyMesh(VertexHelper mesh)
    {
        if (!IsActive() || mesh.currentVertCount != 4) return;
        UIVertex lower = default, upper = default;
        mesh.PopulateUIVertex(ref lower, 0); mesh.PopulateUIVertex(ref upper, 2);
        var rect = ((RectTransform)transform).rect;
        float edge = Mathf.Min(edgeWidth, rect.width * 0.5f, rect.height * 0.5f);
        mesh.Clear();
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
        {
            float u = x == 0 ? 0 : x == 1 ? sourceInsets.x : x == 2 ? 1 - sourceInsets.z : 1;
            float v = y == 0 ? 0 : y == 1 ? sourceInsets.y : y == 2 ? 1 - sourceInsets.w : 1;
            var vertex = lower;
            vertex.position = new Vector3(
                x == 0 ? rect.xMin : x == 1 ? rect.xMin + edge : x == 2 ? rect.xMax - edge : rect.xMax,
                y == 0 ? rect.yMin : y == 1 ? rect.yMin + edge : y == 2 ? rect.yMax - edge : rect.yMax);
            vertex.uv0 = new Vector4(Mathf.Lerp(lower.uv0.x, upper.uv0.x, u), Mathf.Lerp(lower.uv0.y, upper.uv0.y, v), 0, 0);
            mesh.AddVert(vertex);
            if (x == 0 || y == 0) continue;
            int i = y * 4 + x;
            mesh.AddTriangle(i - 5, i - 1, i); mesh.AddTriangle(i - 5, i, i - 4);
        }
    }
}
