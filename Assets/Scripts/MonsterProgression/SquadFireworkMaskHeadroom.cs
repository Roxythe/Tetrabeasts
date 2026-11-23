using UnityEngine;
using UnityEngine.UI;

// Extends an invisible ScrollRect stencil without moving its content or changing
// scroll/layout bounds. This gives the first portrait's fireworks room overhead.
[DisallowMultipleComponent]
public sealed class SquadFireworkMaskHeadroom : BaseMeshEffect
{
    float top;

    public void ExpandTop(float pixels)
    {
        if (pixels <= top) return;
        top = pixels;
        if (graphic) graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper mesh)
    {
        if (!IsActive() || top <= 0) return;
        var rect = ((RectTransform)transform).rect;
        var vertex = new UIVertex();
        for (int i = 0; i < mesh.currentVertCount; i++)
        {
            mesh.PopulateUIVertex(ref vertex, i);
            float upperHalf = Mathf.InverseLerp(rect.center.y, rect.yMax, vertex.position.y);
            vertex.position += Vector3.up * (top * upperHalf);
            mesh.SetUIVertex(vertex, i);
        }
    }
}
