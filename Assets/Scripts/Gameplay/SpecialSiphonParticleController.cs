using UnityEngine;
using UnityEngine.UI;

public sealed class SpecialSiphonParticleController : MonoBehaviour
{
    Slider sourceSlider;
    RectTransform sourceFallbackRect;
    RectTransform targetRect;
    Camera renderCamera;
    Canvas sourceCanvas;
    Canvas targetCanvas;

    ParticleSystem[] systems;
    ParticleSystem.Particle[][] particleBuffers;

    float particleSpeed = 12f;
    float arrivalRadius = 0.15f;
    bool stoppingEmission;
    const float ParticleWorldZ = -1f;
    readonly Vector3[] corners = new Vector3[4];

    public void Configure(
        Slider sourceSlider,
        RectTransform sourceFallbackRect,
        RectTransform targetRect,
        Camera renderCamera,
        float particleSpeed,
        float arrivalRadius,
        int sortingOrder)
    {
        this.sourceSlider = sourceSlider;
        this.sourceFallbackRect = sourceFallbackRect;
        this.targetRect = targetRect;
        this.renderCamera = renderCamera ? renderCamera : Camera.main;
        this.particleSpeed = Mathf.Max(0.1f, particleSpeed);
        this.arrivalRadius = Mathf.Max(0.01f, arrivalRadius);
        sourceCanvas = sourceSlider ? sourceSlider.GetComponentInParent<Canvas>() : null;
        if (!sourceCanvas && sourceFallbackRect)
            sourceCanvas = sourceFallbackRect.GetComponentInParent<Canvas>();
        targetCanvas = targetRect ? targetRect.GetComponentInParent<Canvas>() : null;

        EnsureParticleSystems();
        ConfigureRenderers(sortingOrder);
    }

    public void Begin()
    {
        EnsureParticleSystems();
        gameObject.SetActive(true);
        stoppingEmission = false;
        MoveEmitterToSource();

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            if (!ps) continue;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
            ps.Play(true);
        }
    }

    public void StopEmittingAndDrain()
    {
        EnsureParticleSystems();
        stoppingEmission = true;

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            if (ps)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void ForceDestroy()
    {
        Destroy(gameObject);
    }

    void Update()
    {
        if (!renderCamera)
            renderCamera = Camera.main;

        MoveEmitterToSource();
        MoveParticlesToTarget(Time.deltaTime);

        if (stoppingEmission && !HasLiveParticles())
            gameObject.SetActive(false);
    }

    void EnsureParticleSystems()
    {
        if (systems != null && systems.Length > 0)
            return;

        systems = GetComponentsInChildren<ParticleSystem>(true);
        particleBuffers = new ParticleSystem.Particle[systems.Length][];

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            if (!ps) continue;

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            int maxParticles = Mathf.Max(1, main.maxParticles);
            particleBuffers[i] = new ParticleSystem.Particle[maxParticles];
        }
    }

    void ConfigureRenderers(int sortingOrder)
    {
        SetLayerRecursively(gameObject, LayerMask.NameToLayer("UI"));

        ParticleSystemRenderer[] renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            ParticleSystemRenderer renderer = renderers[i];
            if (!renderer) continue;

            renderer.sortingOrder = sortingOrder;
        }
    }

    void SetLayerRecursively(GameObject root, int layer)
    {
        if (!root || layer < 0)
            return;

        root.layer = layer;
        for (int i = 0; i < root.transform.childCount; i++)
            SetLayerRecursively(root.transform.GetChild(i).gameObject, layer);
    }

    void MoveEmitterToSource()
    {
        if (!renderCamera)
            return;

        transform.position = ScreenToWorld(GetSourceScreenPoint());
    }

    void MoveParticlesToTarget(float deltaTime)
    {
        if (systems == null || systems.Length == 0 || !targetRect || deltaTime <= 0f)
            return;

        Vector3 target = ScreenToWorld(GetRectCenterScreenPoint(targetRect, targetCanvas));
        float step = Mathf.Max(0.01f, particleSpeed) * deltaTime;
        float radius = Mathf.Max(0.01f, arrivalRadius);

        for (int systemIndex = 0; systemIndex < systems.Length; systemIndex++)
        {
            ParticleSystem ps = systems[systemIndex];
            ParticleSystem.Particle[] buffer = particleBuffers != null && systemIndex < particleBuffers.Length
                ? particleBuffers[systemIndex]
                : null;

            if (!ps || buffer == null || buffer.Length == 0)
                continue;

            int count = ps.GetParticles(buffer);
            bool changed = false;

            for (int i = 0; i < count; i++)
            {
                Vector3 toTarget = target - buffer[i].position;
                float distance = toTarget.magnitude;
                if (distance <= radius)
                {
                    buffer[i].remainingLifetime = 0f;
                    changed = true;
                    continue;
                }

                Vector3 direction = toTarget / Mathf.Max(distance, 0.0001f);
                float move = Mathf.Min(step, distance);
                buffer[i].position += direction * move;
                buffer[i].velocity = Vector3.zero;
                changed = true;
            }

            if (changed)
                ps.SetParticles(buffer, count);
        }
    }

    bool HasLiveParticles()
    {
        if (systems == null || systems.Length == 0)
            return false;

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            if (ps && ps.IsAlive(true))
                return true;
        }

        return false;
    }

    Vector2 GetSourceScreenPoint()
    {
        if (sourceSlider && sourceSlider.fillRect)
        {
            RectTransform fillRect = sourceSlider.fillRect;
            fillRect.GetWorldCorners(corners);

            switch (sourceSlider.direction)
            {
                case Slider.Direction.BottomToTop:
                    return WorldToScreen((corners[1] + corners[2]) * 0.5f, sourceCanvas);
                case Slider.Direction.TopToBottom:
                    return WorldToScreen((corners[0] + corners[3]) * 0.5f, sourceCanvas);
                case Slider.Direction.LeftToRight:
                    return WorldToScreen((corners[2] + corners[3]) * 0.5f, sourceCanvas);
                case Slider.Direction.RightToLeft:
                    return WorldToScreen((corners[0] + corners[1]) * 0.5f, sourceCanvas);
            }
        }

        RectTransform rect = sourceFallbackRect
            ? sourceFallbackRect
            : (sourceSlider ? sourceSlider.transform as RectTransform : null);

        if (!rect)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        return GetRectCenterScreenPoint(rect, sourceCanvas);
    }

    Vector2 GetRectCenterScreenPoint(RectTransform rect, Canvas canvas)
    {
        Vector3 world = rect.TransformPoint(rect.rect.center);
        return WorldToScreen(world, canvas);
    }

    Vector2 WorldToScreen(Vector3 world, Canvas canvas)
    {
        Camera canvasCamera = null;
        if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvasCamera = canvas.worldCamera ? canvas.worldCamera : renderCamera;

        return RectTransformUtility.WorldToScreenPoint(canvasCamera, world);
    }

    Vector3 ScreenToWorld(Vector2 screen)
    {
        if (!renderCamera)
            return Vector3.zero;

        float depth = Mathf.Abs(ParticleWorldZ - renderCamera.transform.position.z);
        if (depth <= 0.001f)
            depth = Mathf.Abs(renderCamera.transform.position.z);
        if (depth <= 0.001f)
            depth = 10f;

        Vector3 world = renderCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        world.z = ParticleWorldZ;
        return world;
    }
}
