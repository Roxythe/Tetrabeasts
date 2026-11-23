using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class PixelDebrisVfx : MaskableGraphic
{
    struct Particle { public Vector2 position, velocity; public Color tint; public float size, age, lifetime, spin; public int shape; public bool bomb; }
    readonly List<Particle> particles = new List<Particle>();
    public static PixelDebrisVfx Create(Canvas canvas)
    {
        var fx = new GameObject("Special block pixel debris", typeof(RectTransform), typeof(CanvasRenderer), typeof(PixelDebrisVfx)).GetComponent<PixelDebrisVfx>();
        fx.transform.SetParent(canvas.transform, false);
        fx.rectTransform.anchorMin = Vector2.zero; fx.rectTransform.anchorMax = Vector2.one; fx.rectTransform.sizeDelta = Vector2.zero;
        fx.raycastTarget = false; return fx;
    }
    public void Burst(Vector3 world, float width, Color tint, Vector2 direction, bool bomb)
    {
        Vector2 center = rectTransform.InverseTransformPoint(world);
        int count = bomb ? 3 + Mathf.Abs(Mathf.RoundToInt(center.x * 7 + center.y * 11) % 4) : 25;
        int arcCount = bomb ? 2 + count % 2 : 0;
        for (int i = 0; i < count + arcCount; i++)
        {
            Vector2 offset = bomb ? new Vector2(Mathf.Sin(i * 2.4f), Mathf.Cos(i * 2.4f)) * .3f : new Vector2(i % 5 - 2, i / 5 - 2) / 5f;
            Vector2 radial = offset.normalized;
            float variety = .65f + Mathf.Repeat(i * .618f, 1) * .7f;
            particles.Add(new Particle { position = center + offset * width,
                velocity = bomb ? (i < count ? (direction.normalized * .85f + radial * .4f) * width * 12f * variety :
                    new Vector2((Mathf.Abs(direction.x) > .1f ? Mathf.Sign(direction.x) : (i % 2 == 0 ? -1 : 1)) * width * (2f + (i - count) * 2f), width * (10f + (i - count) * 2f))) : radial * width * 1.7f + Vector2.up * width,
                tint = Color.Lerp(tint, Color.white, Mathf.Repeat(i * .37f, 1) * .12f), size = width / 7f * variety,
                lifetime = bomb ? 8f : .35f + variety * .3f, bomb = bomb,
                shape = i + count, spin = (i % 2 == 0 ? 1 : -1) * (2f + variety * 3f) });
        }
        SetVerticesDirty();
    }
    void Update()
        => Advance(Time.deltaTime);
    void Advance(float dt)
    {
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i]; p.age += dt;
            if (p.bomb) p.velocity += Vector2.down * rectTransform.rect.height * 1.3f * dt;
            p.position += p.velocity * dt;
            Rect bounds = rectTransform.rect;
            if (p.age >= p.lifetime || (p.bomb && (p.position.y < bounds.yMin - p.size || p.position.x < bounds.xMin - p.size || p.position.x > bounds.xMax + p.size)))
                particles.RemoveAt(i);
            else particles[i] = p;
        }
        SetVerticesDirty();
        if (particles.Count == 0 && !keepAlive) Destroy(gameObject);
    }
    public bool keepAlive = true;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        foreach (var p in particles)
        {
            Color tint = p.tint;
            float shrink = p.bomb ? 1 : 1 - p.age / p.lifetime;
            tint.a = p.bomb ? 1 : shrink;
            if (!p.bomb)
            {
                FloorAmbientVfx.Quad(vh, p.position, Vector2.one * (p.size * shrink + 2), new Color(0, 0, 0, tint.a));
                FloorAmbientVfx.Quad(vh, p.position, Vector2.one * p.size * shrink, tint);
            }
            else
            {
                // Uneven, chipped silhouettes with differently shaded faces, rather than square confetti.
                int sides = 5 + p.shape % 3;
                float rotation = p.shape * 2.4f + p.age * p.spin;
                Vector2 Point(int index)
                {
                    float angle = rotation + index * Mathf.PI * 2f / sides;
                    float radius = p.size * (.35f + .28f * Mathf.Repeat(index * .618f + p.shape * .137f, 1f));
                    return p.position + new Vector2(Mathf.Cos(angle) * radius * 1.2f, Mathf.Sin(angle) * radius * .85f);
                }
                for (int j = 0; j < sides; j++)
                {
                    Vector2 a = Point(j), b = Point((j + 1) % sides);
                    Vector2 outerA = a + (a - p.position).normalized * 1.5f;
                    Vector2 outerB = b + (b - p.position).normalized * 1.5f;
                    int border = vh.currentVertCount;
                    vh.AddVert(a, Color.black, Vector2.zero); vh.AddVert(b, Color.black, Vector2.zero);
                    vh.AddVert(outerB, Color.black, Vector2.zero); vh.AddVert(outerA, Color.black, Vector2.zero);
                    vh.AddTriangle(border, border + 1, border + 2); vh.AddTriangle(border, border + 2, border + 3);
                    int n = vh.currentVertCount;
                    Color face = Color.Lerp(tint, new Color(tint.r * .65f, tint.g * .65f, tint.b * .65f, 1), j % 3 * .3f);
                    vh.AddVert(p.position, tint, Vector2.zero);
                    vh.AddVert(Point(j), face, Vector2.zero);
                    vh.AddVert(Point((j + 1) % sides), face, Vector2.zero);
                    vh.AddTriangle(n, n + 1, n + 2);
                }
            }
        }
    }
}
