using UnityEngine;
using UnityEngine.UI;

// Deforms the surface of the filled Image, preserving Slider fill, bubble shading and masks.
[RequireComponent(typeof(Image))]
public sealed class MarathonLiquidSlosh : BaseMeshEffect
{
    [Min(0f)] public float maxWaveHeight = 18f;
    [Min(0.1f)] public float damping = 2.8f;
    [Min(0.1f)] public float waveSpeed = 12f;
    float energy, phase;
    public float WaveEnergy => energy;

    public void AddImpulse(float gainedFraction)
    {
        gainedFraction = Mathf.Abs(gainedFraction);
        if (gainedFraction <= 0f) return;
        energy = Mathf.Clamp01(energy + 0.3f + gainedFraction * 3f);
        graphic.SetVerticesDirty();
    }

    void Update()
    {
        if (energy <= 0f || Time.deltaTime <= 0f) return;
        phase += Time.deltaTime * waveSpeed;
        energy *= Mathf.Exp(-damping * Time.deltaTime);
        if (energy < 0.001f) energy = 0f;
        graphic.SetVerticesDirty();
    }

    protected override void OnDisable()
    {
        energy = phase = 0f;
        base.OnDisable();
    }

    public override void ModifyMesh(VertexHelper mesh)
    {
        if (!IsActive() || energy <= 0f || mesh.currentVertCount != 4) return;
        bool horizontal = GetComponent<Image>().fillMethod == Image.FillMethod.Horizontal;
        UIVertex vertex = default, bottomLeft = default, topRight = default;
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            mesh.PopulateUIVertex(ref vertex, i);
            if (horizontal) SwapAxes(ref vertex);
            if (vertex.position.x <= minX && vertex.position.y <= minY) bottomLeft = vertex;
            if (vertex.position.x >= maxX && vertex.position.y >= maxY) topRight = vertex;
            minX = Mathf.Min(minX, vertex.position.x); maxX = Mathf.Max(maxX, vertex.position.x);
            minY = Mathf.Min(minY, vertex.position.y); maxY = Mathf.Max(maxY, vertex.position.y);
        }
        // Resolve exact corners regardless of Unity's quad vertex order.
        for (int i = 0; i < 4; i++)
        {
            mesh.PopulateUIVertex(ref vertex, i);
            if (horizontal) SwapAxes(ref vertex);
            if (vertex.position.x == minX && vertex.position.y == minY) bottomLeft = vertex;
            if (vertex.position.x == maxX && vertex.position.y == maxY) topRight = vertex;
        }
        var bounds = ((RectTransform)transform).rect;
        if (horizontal) bounds = new Rect(bounds.y, bounds.x, bounds.height, bounds.width);
        float amplitude = Mathf.Min(maxWaveHeight * energy, (maxY - minY) * 0.45f, (bounds.yMax - maxY) * 0.8f);
        if (amplitude <= 0f) return;
        mesh.Clear();
        const int segments = 24;
        for (int i = 0; i <= segments; i++)
        {
            float x = (float)i / segments;
            float height = maxY + amplitude * (Mathf.Sin(phase) * (2f * x - 1f) * 0.65f +
                Mathf.Sin(x * Mathf.PI * 2f + phase * 1.3f) * 0.35f);
            var lower = bottomLeft;
            lower.position.x = Mathf.Lerp(minX, maxX, x);
            lower.uv0.x = Mathf.Lerp(bottomLeft.uv0.x, topRight.uv0.x, x);
            var upper = lower;
            upper.position.y = height;
            upper.uv0.y = Mathf.LerpUnclamped(bottomLeft.uv0.y, topRight.uv0.y, (height - minY) / (maxY - minY));
            if (horizontal) { SwapAxes(ref lower); SwapAxes(ref upper); }
            mesh.AddVert(lower); mesh.AddVert(upper);
            if (i == 0) continue;
            int index = i * 2;
            mesh.AddTriangle(index - 2, index - 1, index + 1);
            mesh.AddTriangle(index - 2, index + 1, index);
        }
    }
    static void SwapAxes(ref UIVertex vertex)
    {
        vertex.position = new Vector3(vertex.position.y, vertex.position.x, vertex.position.z);
        vertex.uv0 = new Vector4(vertex.uv0.y, vertex.uv0.x, vertex.uv0.z, vertex.uv0.w);
    }
}
