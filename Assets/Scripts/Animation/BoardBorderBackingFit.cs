using UnityEngine;
using UnityEngine.UI;

// The city animation includes wide transparent margins in each 720 x 1280 frame.
// Fill only the masonry strip, using the same aspect fit as the animated Image.
[ExecuteAlways, RequireComponent(typeof(Image))]
public sealed class BoardBorderBackingFit : BaseMeshEffect
{
    public Image artwork;
    public Rect normalizedBounds = new Rect(1f / 3f, 0f, 1f / 3f, 1f);
    Rect lastRect;
    Vector2 lastSpriteSize;

    void LateUpdate()
    {
        if (!artwork) return;
        var sprite = artwork.overrideSprite;
        var size = sprite ? sprite.rect.size : Vector2.zero;
        var rect = artwork.GetPixelAdjustedRect();
        if (rect == lastRect && size == lastSpriteSize) return;
        lastRect = rect;
        lastSpriteSize = size;
        graphic.SetVerticesDirty();
    }

    public Rect ArtworkBackingRect()
    {
        if (!artwork) return Rect.zero;
        var rect = artwork.GetPixelAdjustedRect();
        var sprite = artwork.overrideSprite;
        if (artwork.preserveAspect && sprite && sprite.rect.width > 0f && sprite.rect.height > 0f)
        {
            var size = sprite.rect.size;
            float scale = Mathf.Min(rect.width / size.x, rect.height / size.y);
            var fitted = size * scale;
            var pivot = artwork.rectTransform.pivot;
            rect.position += Vector2.Scale(rect.size - fitted, pivot);
            rect.size = fitted;
        }
        return new Rect(rect.x + rect.width * normalizedBounds.x,
            rect.y + rect.height * normalizedBounds.y,
            rect.width * normalizedBounds.width, rect.height * normalizedBounds.height);
    }

    public override void ModifyMesh(VertexHelper mesh)
    {
        if (!IsActive() || !artwork || mesh.currentVertCount != 4) return;
        Rect bounds = ArtworkBackingRect();
        var vertex = default(UIVertex);
        for (int i = 0; i < 4; i++)
        {
            mesh.PopulateUIVertex(ref vertex, i);
            var point = new Vector3(i < 2 ? bounds.xMin : bounds.xMax,
                i == 0 || i == 3 ? bounds.yMin : bounds.yMax);
            vertex.position = transform.InverseTransformPoint(artwork.transform.TransformPoint(point));
            mesh.SetUIVertex(vertex, i);
        }
    }
}
