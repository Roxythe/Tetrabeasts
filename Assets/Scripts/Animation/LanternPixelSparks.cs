using UnityEngine;
using UnityEngine.UI;

// A tiny UI mesh keeps the sparks crisp, inexpensive, and editable with the lantern.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class LanternPixelSparks : MaskableGraphic
{
    [SerializeField] Vector2 burstInterval = new Vector2(1.3f, 3.2f);
    [SerializeField] Vector2 lifetimeRange = new Vector2(.55f, 1f);
    [SerializeField] Color emberRed = new Color(1f, .15f, .02f);
    [SerializeField] Color emberOrange = new Color(1f, .48f, .04f);
    [SerializeField] Color emberYellow = new Color(1f, .9f, .25f);
    struct Spark { public Vector2 start, velocity; public float age, lifetime, size; }
    readonly Spark[] sparks = new Spark[3];
    readonly System.Random random = new System.Random();
    float countdown;
    int count;

    float Between(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;
        count = 0;
        countdown = Between(.3f, 1.4f);
    }

    protected override void OnDisable()
    {
        count = 0;
        base.OnDisable();
    }

    void Update()
    {
        // Pause-menu flames must keep moving while gameplay time is stopped.
        float dt = Time.unscaledDeltaTime;
        countdown -= dt;
        if (countdown <= 0f)
        {
            count = random.Next(1, 4);
            for (int i = 0; i < count; i++)
                sparks[i] = new Spark {
                    start = new Vector2(Between(-5f, 5f), Between(5f, 13f)),
                    velocity = new Vector2(Between(-13f, 13f), Between(23f, 39f)),
                    lifetime = Between(lifetimeRange.x, lifetimeRange.y), size = random.Next(1, 4)
                };
            countdown = Between(burstInterval.x, burstInterval.y);
        }
        bool dirty = false;
        for (int i = 0; i < count; i++)
        {
            if (sparks[i].age >= sparks[i].lifetime) continue;
            sparks[i].age += dt;
            dirty = true;
        }
        if (dirty) SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        for (int i = 0; i < count; i++)
        {
            Spark spark = sparks[i];
            if (spark.age >= spark.lifetime) continue;
            float t = spark.age / spark.lifetime;
            Vector2 pos = spark.start + spark.velocity * spark.age + Vector2.down * (12f * spark.age * spark.age);
            pos = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
            Color tint = t < .4f ? Color.Lerp(emberYellow, emberOrange, t / .4f)
                : Color.Lerp(emberOrange, emberRed, (t - .4f) / .6f);
            tint.a *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.65f, 1f, t));
            int n = vh.currentVertCount;
            vh.AddVert(pos, tint, Vector2.zero);
            vh.AddVert(pos + Vector2.up * spark.size, tint, Vector2.zero);
            vh.AddVert(pos + Vector2.one * spark.size, tint, Vector2.zero);
            vh.AddVert(pos + Vector2.right * spark.size, tint, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2);
            vh.AddTriangle(n, n + 2, n + 3);
        }
    }
}
