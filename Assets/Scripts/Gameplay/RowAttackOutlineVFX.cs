using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A pixel-stepped perimeter; no internal cell dividers or input hitbox.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RowAttackOutlineVFX : MaskableGraphic
{
    readonly List<float> origins = new();
    readonly List<Vector2> spans = new();
    float sweepSeconds, holdSeconds, fadeSeconds, thickness, reach;
    float elapsed, releaseElapsed;
    bool released;
    Color outerGlow, innerGlow, flashTint, coreTint, trailTint, sparkTint, sparkCore;
    public float LeadSeconds => sweepSeconds + holdSeconds;

    public void Initialize(Vector2 size, int columns, IList<int> completedColumns,
        float sweep, float hold, float fade, float lineWidth, Color gold)
    {
        raycastTarget = false;
        color = gold;
        outerGlow = TintGold(new Color(1f, .45f, .025f), gold);
        innerGlow = TintGold(new Color(1f, .64f, .04f), gold);
        flashTint = TintGold(new Color(1f, .98f, .7f), gold);
        coreTint = TintGold(new Color(1f, 1f, .83f), gold);
        trailTint = TintGold(new Color(1f, .72f, .1f), gold);
        sparkTint = TintGold(new Color(1f, .86f, .25f), gold);
        sparkCore = TintGold(new Color(1f, 1f, .9f), gold);
        rectTransform.sizeDelta = size;
        sweepSeconds = Mathf.Max(.01f, sweep);
        holdSeconds = Mathf.Max(0f, hold);
        fadeSeconds = Mathf.Max(.01f, fade);
        thickness = Mathf.Max(1f, lineWidth);
        origins.Clear();
        columns = Mathf.Max(1, columns);
        if (completedColumns != null)
            foreach (int column in completedColumns)
            {
                if (column < 0 || column >= columns) continue;
                float x = -size.x * .5f + (column + .5f) * size.x / columns;
                if (!origins.Contains(x)) origins.Add(x);
            }
        if (origins.Count == 0) origins.Add(0f);
        origins.Sort();
        reach = Mathf.Max(origins[0] + size.x * .5f, size.x * .5f - origins[origins.Count - 1]);
        for (int i = 1; i < origins.Count; i++) reach = Mathf.Max(reach, (origins[i] - origins[i - 1]) * .5f);
        elapsed = releaseElapsed = 0;
        released = false;
        SetVerticesDirty();
    }

    public void Release()
    {
        if (released) return;
        released = true;
        elapsed = Mathf.Max(elapsed, sweepSeconds);
        releaseElapsed = 0;
        SetVerticesDirty();
    }

    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        if (released)
        {
            releaseElapsed += Time.unscaledDeltaTime;
            if (releaseElapsed >= fadeSeconds) { Destroy(gameObject); return; }
        }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (origins.Count == 0) return;
        var size = rectTransform.rect.size;
        float left = -size.x * .5f, right = size.x * .5f, top = size.y * .5f;
        float progress = Mathf.Clamp01(elapsed / sweepSeconds);
        float distance = reach * progress;
        float fade = released ? 1f - Mathf.Clamp01(releaseElapsed / fadeSeconds) : 1f;
        // One sharp closure flash, followed by an afterglow at the attack release.
        float closure = Mathf.Clamp01((elapsed - sweepSeconds) / .09f);
        float flash = progress >= 1f ? 1f - closure : 0f;
        if (released) flash = Mathf.Max(flash, 1f - Mathf.Clamp01(releaseElapsed / .08f));
        spans.Clear();
        foreach (float origin in origins)
        {
            float a = Mathf.Max(left, origin - distance), b = Mathf.Min(right, origin + distance);
            if (spans.Count > 0 && a <= spans[spans.Count - 1].y)
            {
                var last = spans[spans.Count - 1]; last.y = Mathf.Max(last.y, b); spans[spans.Count - 1] = last;
            }
            else spans.Add(new Vector2(a, b));
        }
        foreach (var span in spans)
        {
            GlowLine(mesh, span.x, span.y, top, false, fade, flash);
            GlowLine(mesh, span.x, span.y, -top, false, fade, flash);
            if (span.x <= left)
                GlowLine(mesh, -top, top, left, true, fade, flash);
            else if (progress < 1f) SweepTip(mesh, span.x, top, -1, fade);
            if (span.y >= right)
                GlowLine(mesh, -top, top, right, true, fade, flash);
            else if (progress < 1f) SweepTip(mesh, span.y, top, 1, fade);
        }
        if (progress >= 1f)
        {
            for (int horizontal = -1; horizontal <= 1; horizontal += 2)
                for (int vertical = -1; vertical <= 1; vertical += 2)
                    Spark(mesh, horizontal * right, vertical * top, 4f + flash * 5f, fade * (.6f + .4f * flash));
        }
    }

    void GlowLine(VertexHelper mesh, float a, float b, float edge, bool vertical, float alpha, float flash)
    {
        if (b <= a) return;
        float t = thickness;
        Line(mesh, a, b, edge, t * 5f, vertical, WithAlpha(outerGlow, .16f * alpha));
        Line(mesh, a, b, edge, t * 2.5f, vertical, WithAlpha(innerGlow, .48f * alpha));
        var gold = Color.Lerp(color, flashTint, flash * .7f); gold.a *= alpha;
        Line(mesh, a, b, edge, t, vertical, gold);
        Line(mesh, a, b, edge, Mathf.Max(1f, t * .4f), vertical, WithAlpha(coreTint, (.8f + .2f * flash) * alpha));
    }

    void SweepTip(VertexHelper mesh, float x, float top, int direction, float alpha)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Spark(mesh, x, top * side, thickness * 2.4f, alpha);
            for (int i = 1; i <= 3; i++)
            {
                float trailX = x - direction * (i * 9f);
                float trailY = top * side + side * (i % 2 == 0 ? 7f : 4f);
                Quad(mesh, trailX - 1.5f, trailY - 1.5f, trailX + 1.5f, trailY + 1.5f,
                    WithAlpha(trailTint, alpha * (1f - i * .2f)));
            }
        }
    }

    void Spark(VertexHelper mesh, float x, float y, float radius, float alpha)
    {
        Quad(mesh, x - radius, y - 1, x + radius, y + 1, WithAlpha(sparkTint, alpha));
        Quad(mesh, x - 1, y - radius, x + 1, y + radius, WithAlpha(sparkTint, alpha));
        Quad(mesh, x - 2, y - 2, x + 2, y + 2, WithAlpha(sparkCore, alpha));
    }

    static Color WithAlpha(Color tint, float alpha) { tint.a *= alpha; return tint; }

    static Color TintGold(Color original, Color accent)
    {
        Color.RGBToHSV(new Color(1f, .76f, .08f), out float goldHue, out float goldSaturation, out _);
        Color.RGBToHSV(original, out float h, out float s, out float v);
        Color.RGBToHSV(accent, out float accentHue, out float accentSaturation, out float accentValue);
        var tint = Color.HSVToRGB(Mathf.Repeat(h + accentHue - goldHue, 1f),
            Mathf.Clamp01(s * accentSaturation / goldSaturation), v * accentValue);
        tint.a = accent.a;
        return tint;
    }

    static void Line(VertexHelper mesh, float a, float b, float edge, float width, bool vertical, Color tint)
    {
        if (vertical) Quad(mesh, edge - width * .5f, a, edge + width * .5f, b, tint);
        else Quad(mesh, a, edge - width * .5f, b, edge + width * .5f, tint);
    }

    static void Quad(VertexHelper mesh, float x0, float y0, float x1, float y1, Color tint)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert(new Vector3(Mathf.Round(x0), Mathf.Round(y0)), tint, Vector2.zero);
        mesh.AddVert(new Vector3(Mathf.Round(x0), Mathf.Round(y1)), tint, Vector2.zero);
        mesh.AddVert(new Vector3(Mathf.Round(x1), Mathf.Round(y1)), tint, Vector2.zero);
        mesh.AddVert(new Vector3(Mathf.Round(x1), Mathf.Round(y0)), tint, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
        mesh.AddTriangle(start + 2, start + 3, start);
    }
}
