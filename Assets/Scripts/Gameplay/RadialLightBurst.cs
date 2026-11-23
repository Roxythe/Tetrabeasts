using UnityEngine;
using UnityEngine.UI;

// Soft, overlapping sectors: a shared center, curved ends, and alpha fading
// toward both the outer radius and the sides. All dimensions are prefab-editable.
[ExecuteAlways]
public sealed class RadialLightBurst : MaskableGraphic
{
    [Range(3, 24)] public int coneCount = 12;
    [Range(5f, 90f)] public float coneAngle = 44f;
    [Range(0f, 1f)] public float opacity = .65f;
    [Range(0f, .95f)] public float fadeStart = .4f;
    [Range(0f, 1f)] public float minimumReach = .04f;
    [Min(.1f)] public float pulseSeconds = .9f;
    [Range(0f, 1f)] public float saturation = .8f;
    [Range(0f, 45f)] public float aimJitterDegrees = 14f;
    [Range(0f, .8f)] public float timingVariation = .4f;
    public int seed = 17;
    float animationTime;

    public void SetAnimationTime(float seconds) { animationTime = seconds; SetVerticesDirty(); }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        const int radialSteps = 18, angularSteps = 16;
        float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
        for (int cone = 0; cone < coneCount; cone++)
        {
            // Each fixture has its own clock and random phase, rather than a
            // sequential phase offset that reads as one rotating rainbow.
            float period = Mathf.Max(.1f, pulseSeconds) * Mathf.Lerp(1f - timingVariation, 1f + timingVariation, Noise(cone, 0));
            float clock = Mathf.Max(0f, animationTime) / period + Noise(cone, 1);
            int shot = Mathf.FloorToInt(clock);
            float progress = clock - shot;
            float launch = 1f - Mathf.Pow(1f - Mathf.Clamp01(progress / .55f), 2f);
            float reach = radius * Mathf.Lerp(minimumReach, Mathf.Lerp(.75f, 1f, Noise(cone, shot + 13)), launch);
            // Fast attack, outward travel, then extinction before the next shot.
            // Direction and color changes happen during the dark interval.
            float brightness = Mathf.SmoothStep(0f, 1f, progress / .06f) *
                (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.22f, .76f, progress)));
            float centerAngle = cone * 360f / coneCount + (Noise(cone, shot + 31) * 2f - 1f) * aimJitterDegrees;
            float spread = coneAngle * Mathf.Lerp(.65f, 1f, launch);
            var tint = Color.HSVToRGB(Noise(cone, shot + 57), saturation, 1f);
            int start = mesh.currentVertCount;
            for (int ring = 0; ring <= radialSteps; ring++)
            {
                float r = ring / (float)radialSteps;
                float radialAlpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fadeStart, 1f, r));
                for (int step = 0; step <= angularSteps; step++)
                {
                    float side = step / (float)angularSteps;
                    float angle = (centerAngle + (side - .5f) * spread) * Mathf.Deg2Rad;
                    float sideAlpha = Mathf.SmoothStep(0f, 1f, Mathf.Min(side, 1f - side) / .22f);
                    var vertexColor = tint * color;
                    vertexColor.a = color.a * opacity * brightness * radialAlpha * sideAlpha;
                    mesh.AddVert(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * (reach * r), vertexColor, Vector2.zero);
                    if (ring == 0 || step == 0) continue;
                    int i = start + ring * (angularSteps + 1) + step;
                    mesh.AddTriangle(i - angularSteps - 2, i - angularSteps - 1, i);
                    mesh.AddTriangle(i - angularSteps - 2, i, i - 1);
                }
            }
        }
    }

    float Noise(int cone, int sample)
    {
        // Local deterministic randomness keeps preview/replay stable and never
        // consumes the gameplay RNG used for pieces, rewards, or obstacles.
        unchecked
        {
            uint value = (uint)(cone * 374761393 + sample * 668265263 + seed * 1442695041);
            value = (value ^ (value >> 13)) * 1274126177u;
            value ^= value >> 16;
            return (value & 0x00ffffff) / 16777216f;
        }
    }
}
