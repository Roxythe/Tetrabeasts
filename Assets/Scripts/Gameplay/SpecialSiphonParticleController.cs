using System.Collections.Generic;
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
    GameObject[] overlayVisibilityBlockers;
    AudioClip[] arrivalPopSFXClips;

    ParticleSystem[] systems;
    ParticleSystem.Particle[][] particleBuffers;
    ParticleSystemRenderer[] particleRenderers;

    readonly List<UiParticle> uiParticles = new List<UiParticle>();
    RectTransform overlayRoot;
    Sprite uiParticleSprite;
    ParticleSystem.MinMaxCurve uiStartLifetimeCurve = new ParticleSystem.MinMaxCurve(2f);
    ParticleSystem.MinMaxCurve uiStartSizeCurve = new ParticleSystem.MinMaxCurve(10f);
    ParticleSystem.MinMaxCurve uiStartSpeedCurve = new ParticleSystem.MinMaxCurve(0f);
    ParticleSystem.MinMaxCurve uiSizeOverLifetimeCurve = new ParticleSystem.MinMaxCurve(1f);
    ParticleSystem.MinMaxGradient uiStartColorGradient = new ParticleSystem.MinMaxGradient(Color.white);
    ParticleSystem.MinMaxGradient uiColorOverLifetimeGradient = new ParticleSystem.MinMaxGradient(Color.white);
    bool uiUseSizeOverLifetime;
    bool uiUseColorOverLifetime;
    float uiRootScale = 1f;
    float uiEmitRate = 10f;
    float uiSimulationSpeed = 1f;
    float uiParticleSize = 24f;
    int uiTargetLiveParticleCount = 24;
    float uiShapeRadius;
    float uiShapeAngle;
    float uiShapeRandomPositionAmount;
    float uiShapeRandomDirectionAmount;
    float uiEmitAccumulator;
    bool useUiOverlay;
    bool ownsUiParticleSprite;

    float particleSpeed = 12f;
    float arrivalRadius = 0.15f;
    float arrivalPopSFXVolume = 0.22f;
    float nextArrivalPopTime;
    bool stoppingEmission;
    const float ArrivalPopMinInterval = 0.06f;
    const float ParticleWorldZ = -1f;
    readonly Vector3[] corners = new Vector3[4];

    struct UiParticle
    {
        public RectTransform rect;
        public Image image;
        public Vector2 position;
        public Vector2 velocity;
        public Color startColor;
        public float startSize;
        public float age;
        public float lifetime;
        public float startDistance;
        public float destinationFade;
        public float sizeRandom;
        public float colorRandom;
        public float wobblePhase;
        public float wobbleFrequency;
        public float wobbleAmplitude;
    }

    public void Configure(
        Slider sourceSlider,
        RectTransform sourceFallbackRect,
        RectTransform targetRect,
        Camera renderCamera,
        float particleSpeed,
        float arrivalRadius,
        int sortingOrder,
        AudioClip[] arrivalPopSFXClips = null,
        float arrivalPopSFXVolume = 0.22f,
        GameObject[] overlayVisibilityBlockers = null)
    {
        this.sourceSlider = sourceSlider;
        this.sourceFallbackRect = sourceFallbackRect;
        this.targetRect = targetRect;
        this.renderCamera = renderCamera ? renderCamera : Camera.main;
        this.particleSpeed = Mathf.Max(0.1f, particleSpeed);
        this.arrivalRadius = Mathf.Max(0.01f, arrivalRadius);
        this.arrivalPopSFXClips = arrivalPopSFXClips;
        this.arrivalPopSFXVolume = Mathf.Clamp01(arrivalPopSFXVolume);
        this.overlayVisibilityBlockers = overlayVisibilityBlockers;

        sourceCanvas = sourceSlider ? sourceSlider.GetComponentInParent<Canvas>() : null;
        if (!sourceCanvas && sourceFallbackRect)
            sourceCanvas = sourceFallbackRect.GetComponentInParent<Canvas>();
        targetCanvas = targetRect ? targetRect.GetComponentInParent<Canvas>() : null;
        useUiOverlay = UsesScreenSpaceOverlay(sourceCanvas) || UsesScreenSpaceOverlay(targetCanvas);

        EnsureParticleSystems();
        ConfigureRenderers(sortingOrder);
        ConfigureUiParticleDefaults();
        SetWorldRenderersVisible(!useUiOverlay);

        if (useUiOverlay)
        {
            EnsureUiOverlay();
        }
        else
        {
            ClearUiParticles();
            SetUiOverlayVisible(false);
        }
    }

    public void Begin()
    {
        EnsureParticleSystems();
        gameObject.SetActive(true);
        stoppingEmission = false;
        nextArrivalPopTime = 0f;
        MoveEmitterToSource();

        if (useUiOverlay)
        {
            EnsureUiOverlay();
            SetUiOverlayVisible(true);
            ClearUiParticles();
            uiEmitAccumulator = 0f;

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (!ps) continue;

                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear(true);
            }

            EmitUiParticleBurst(Mathf.Clamp(uiTargetLiveParticleCount / 4, 8, 30));
            return;
        }

        SetUiOverlayVisible(false);
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
        DestroyUiOverlay();
        Destroy(gameObject);
    }

    void Update()
    {
        if (!renderCamera)
            renderCamera = Camera.main;

        MoveEmitterToSource();

        if (useUiOverlay)
        {
            bool hiddenForPause = ShouldHideUiOverlayForPause();
            SetUiOverlayVisible(!hiddenForPause);
            if (!hiddenForPause)
            {
                float deltaTime = Time.deltaTime > Mathf.Epsilon ? Time.deltaTime : Time.unscaledDeltaTime;
                UpdateUiParticles(deltaTime);
            }
        }
        else
        {
            MoveParticlesToTarget(Time.deltaTime);
        }

        if (stoppingEmission && !HasLiveParticles())
        {
            SetUiOverlayVisible(false);
            gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        DestroyUiOverlay();
        if (ownsUiParticleSprite && uiParticleSprite)
            Destroy(uiParticleSprite);
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

        particleRenderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < particleRenderers.Length; i++)
        {
            ParticleSystemRenderer renderer = particleRenderers[i];
            if (!renderer) continue;

            renderer.sortingOrder = sortingOrder;
        }
    }

    void ConfigureUiParticleDefaults()
    {
        ParticleSystem primary = systems != null && systems.Length > 0 ? systems[0] : null;
        if (!primary)
        {
            EnsureUiParticleSprite();
            return;
        }

        ParticleSystem.MainModule main = primary.main;
        uiSimulationSpeed = Mathf.Max(0.01f, main.simulationSpeed);
        ParticleSystem.EmissionModule emission = primary.emission;
        uiEmitRate = emission.enabled
            ? Mathf.Clamp(SampleCurve(emission.rateOverTime, 0f, 0.5f) * uiSimulationSpeed, 1f, 240f)
            : 10f * uiSimulationSpeed;

        uiStartLifetimeCurve = main.startLifetime;
        uiStartSizeCurve = main.startSize;
        uiStartSpeedCurve = main.startSpeed;
        uiStartColorGradient = main.startColor;
        uiRootScale = Mathf.Max(0.01f, Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y)));
        uiParticleSize = ConvertParticleSizeToUiPixels(SampleCurve(uiStartSizeCurve, 0f, 0.5f));
        float averageLifetimeSeconds = Mathf.Max(0.1f, SampleCurve(uiStartLifetimeCurve, 0f, 0.5f) / uiSimulationSpeed);
        int particleLimit = main.maxParticles > 0 ? main.maxParticles : 100;
        int maxLiveParticles = Mathf.Max(8, Mathf.Min(180, particleLimit));
        uiTargetLiveParticleCount = Mathf.Clamp(
            Mathf.RoundToInt(uiEmitRate * averageLifetimeSeconds),
            8,
            maxLiveParticles);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = primary.sizeOverLifetime;
        uiUseSizeOverLifetime = sizeOverLifetime.enabled;
        uiSizeOverLifetimeCurve = sizeOverLifetime.size;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = primary.colorOverLifetime;
        uiUseColorOverLifetime = colorOverLifetime.enabled;
        uiColorOverLifetimeGradient = colorOverLifetime.color;

        ParticleSystem.ShapeModule shape = primary.shape;
        if (shape.enabled)
        {
            uiShapeRadius = Mathf.Max(0f, shape.radius);
            uiShapeAngle = Mathf.Max(0f, shape.angle);
            uiShapeRandomPositionAmount = Mathf.Max(0f, shape.randomPositionAmount);
            uiShapeRandomDirectionAmount = Mathf.Max(0f, shape.randomDirectionAmount);
        }
        else
        {
            uiShapeRadius = 0f;
            uiShapeAngle = 0f;
            uiShapeRandomPositionAmount = 0f;
            uiShapeRandomDirectionAmount = 0f;
        }

        ParticleSystemRenderer renderer = primary.GetComponent<ParticleSystemRenderer>();
        if (renderer && renderer.sharedMaterial && renderer.sharedMaterial.mainTexture is Texture2D texture)
            SetUiParticleSprite(texture);
        else
            EnsureUiParticleSprite();
    }

    void SetWorldRenderersVisible(bool visible)
    {
        if (particleRenderers == null)
            return;

        for (int i = 0; i < particleRenderers.Length; i++)
        {
            ParticleSystemRenderer renderer = particleRenderers[i];
            if (renderer)
                renderer.enabled = visible;
        }
    }

    void EnsureUiParticleSprite()
    {
        if (uiParticleSprite)
            return;

        SetUiParticleSprite(Texture2D.whiteTexture);
    }

    void SetUiParticleSprite(Texture2D texture)
    {
        if (!texture)
            return;

        if (ownsUiParticleSprite && uiParticleSprite)
            Destroy(uiParticleSprite);

        uiParticleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        ownsUiParticleSprite = true;
    }

    void EnsureUiOverlay()
    {
        if (overlayRoot)
        {
            PositionUiOverlayInCanvasHierarchy();
            return;
        }

        Transform parent = sourceCanvas
            ? sourceCanvas.transform
            : (targetCanvas ? targetCanvas.transform : null);

        GameObject overlay = new GameObject("SpecialSiphon_UIOverlay", typeof(RectTransform));
        overlayRoot = overlay.GetComponent<RectTransform>();
        if (parent)
            overlayRoot.SetParent(parent, false);

        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;
        overlayRoot.localScale = Vector3.one;
        overlayRoot.localRotation = Quaternion.identity;
        PositionUiOverlayInCanvasHierarchy();
        overlay.SetActive(false);
    }

    void SetUiOverlayVisible(bool visible)
    {
        if (overlayRoot)
        {
            if (visible)
                PositionUiOverlayInCanvasHierarchy();
            overlayRoot.gameObject.SetActive(visible);
        }
    }

    void DestroyUiOverlay()
    {
        ClearUiParticles();

        if (overlayRoot)
            Destroy(overlayRoot.gameObject);

        overlayRoot = null;
    }

    void PositionUiOverlayInCanvasHierarchy()
    {
        if (!overlayRoot || !overlayRoot.parent)
            return;

        Transform parent = overlayRoot.parent;
        Transform firstVisibleBlocker = GetFirstVisibleOverlayBlockerUnder(parent);
        if (!firstVisibleBlocker)
        {
            overlayRoot.SetAsLastSibling();
            return;
        }

        int desiredIndex = firstVisibleBlocker.GetSiblingIndex();
        if (overlayRoot.GetSiblingIndex() < desiredIndex)
            desiredIndex--;
        overlayRoot.SetSiblingIndex(Mathf.Clamp(desiredIndex, 0, parent.childCount - 1));
    }

    bool ShouldHideUiOverlayForPause()
    {
        return AudioListener.pause || HasVisibleUiOverlayBlocker();
    }

    bool HasVisibleUiOverlayBlocker()
    {
        if (overlayVisibilityBlockers == null)
            return false;

        for (int i = 0; i < overlayVisibilityBlockers.Length; i++)
        {
            if (IsOverlayVisibilityBlockerVisible(overlayVisibilityBlockers[i]))
                return true;
        }

        return false;
    }

    Transform GetFirstVisibleOverlayBlockerUnder(Transform parent)
    {
        if (!parent || overlayVisibilityBlockers == null)
            return null;

        Transform first = null;
        for (int i = 0; i < overlayVisibilityBlockers.Length; i++)
        {
            GameObject blocker = overlayVisibilityBlockers[i];
            if (!IsOverlayVisibilityBlockerVisible(blocker))
                continue;

            Transform blockerTransform = blocker.transform;
            if (blockerTransform.parent != parent)
                continue;

            if (!first || blockerTransform.GetSiblingIndex() < first.GetSiblingIndex())
                first = blockerTransform;
        }

        return first;
    }

    bool IsOverlayVisibilityBlockerVisible(GameObject blocker)
    {
        return blocker && UIPanelTransition.IsVisible(blocker);
    }

    void ClearUiParticles()
    {
        for (int i = 0; i < uiParticles.Count; i++)
        {
            RectTransform rect = uiParticles[i].rect;
            if (rect)
                Destroy(rect.gameObject);
        }

        uiParticles.Clear();
    }

    void EmitUiParticleBurst(int count)
    {
        Vector2 source = GetSourceScreenPoint();
        for (int i = 0; i < count; i++)
            SpawnUiParticle(source);
    }

    void UpdateUiParticles(float deltaTime)
    {
        if (!overlayRoot || deltaTime <= 0f)
            return;

        if (!stoppingEmission)
        {
            uiEmitAccumulator += Mathf.Max(1f, uiEmitRate) * deltaTime;
            int emitCount = Mathf.Min(8, Mathf.FloorToInt(uiEmitAccumulator));
            uiEmitAccumulator -= emitCount;

            Vector2 source = GetSourceScreenPoint();
            for (int i = 0; i < emitCount; i++)
                SpawnUiParticle(source);

            int refillCount = Mathf.Clamp(
                uiTargetLiveParticleCount - uiParticles.Count,
                0,
                Mathf.CeilToInt(uiEmitRate * 0.25f));
            for (int i = 0; i < refillCount; i++)
                SpawnUiParticle(source);
        }

        MoveUiParticles(deltaTime);
    }

    void SpawnUiParticle(Vector2 screenPoint)
    {
        if (!overlayRoot || !ScreenToOverlayLocal(screenPoint, out Vector2 localPoint))
            return;

        EnsureUiParticleSprite();
        Vector2 spawnPosition = localPoint + GetUiShapeSpawnOffset();
        Vector2 target = spawnPosition;
        if (targetRect)
            ScreenToOverlayLocal(GetRectCenterScreenPoint(targetRect, targetCanvas), out target);

        float random = UnityEngine.Random.value;
        float pixelsPerWorldUnit = GetPixelsPerWorldUnit();
        float speed = Mathf.Max(1f, particleSpeed * pixelsPerWorldUnit);
        float startDistance = Vector2.Distance(spawnPosition, target);
        float travelSeconds = startDistance / speed;
        float prefabLifetimeSeconds = SampleCurve(uiStartLifetimeCurve, 0f, random) / Mathf.Max(0.01f, uiSimulationSpeed);
        float lifetime = Mathf.Clamp(Mathf.Max(prefabLifetimeSeconds, travelSeconds * 1.2f), 0.5f, 12f);
        float startSize = ConvertParticleSizeToUiPixels(SampleCurve(uiStartSizeCurve, 0f, random));
        Color startColor = SampleGradient(uiStartColorGradient, 0f, random);
        if (startColor.a <= 0.01f)
            startColor = Color.white;

        GameObject particle = new GameObject("SpecialSiphon_UIParticle", typeof(RectTransform), typeof(Image));
        RectTransform rect = particle.GetComponent<RectTransform>();
        rect.SetParent(overlayRoot, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.one * Mathf.Max(1f, startSize);
        rect.anchoredPosition = spawnPosition;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        Image image = particle.GetComponent<Image>();
        image.sprite = uiParticleSprite;
        image.color = startColor;
        image.preserveAspect = true;
        image.raycastTarget = false;

        uiParticles.Add(new UiParticle
        {
            rect = rect,
            image = image,
            position = spawnPosition,
            velocity = GetUiInitialVelocity(spawnPosition, target, random),
            startColor = startColor,
            startSize = startSize,
            age = 0f,
            lifetime = lifetime,
            startDistance = startDistance,
            destinationFade = 0f,
            sizeRandom = UnityEngine.Random.value,
            colorRandom = UnityEngine.Random.value,
            wobblePhase = UnityEngine.Random.value * Mathf.PI * 2f,
            wobbleFrequency = UnityEngine.Random.Range(1.35f, 2.75f),
            wobbleAmplitude = Mathf.Clamp(
                GetUiShapeRadiusPixels() * 0.9f + startSize * UnityEngine.Random.Range(0.75f, 1.45f),
                12f,
                64f)
        });

        UiParticle added = uiParticles[uiParticles.Count - 1];
        ApplyUiParticleVisuals(ref added);
        uiParticles[uiParticles.Count - 1] = added;
    }

    void MoveUiParticles(float deltaTime)
    {
        if (!targetRect || !ScreenToOverlayLocal(GetRectCenterScreenPoint(targetRect, targetCanvas), out Vector2 target))
            return;

        float pixelsPerWorldUnit = GetPixelsPerWorldUnit();
        float speed = Mathf.Max(0.01f, particleSpeed) * pixelsPerWorldUnit;
        float radius = Mathf.Max(8f, arrivalRadius * pixelsPerWorldUnit);

        for (int i = uiParticles.Count - 1; i >= 0; i--)
        {
            UiParticle particle = uiParticles[i];
            if (!particle.rect)
            {
                uiParticles.RemoveAt(i);
                continue;
            }

            particle.age += deltaTime;
            Vector2 toTarget = target - particle.position;
            float distance = toTarget.magnitude;
            bool reachedTarget = distance <= radius;
            if (reachedTarget || particle.age >= particle.lifetime)
            {
                if (reachedTarget)
                    PlayArrivalPopSFX();

                Destroy(particle.rect.gameObject);
                uiParticles.RemoveAt(i);
                continue;
            }

            Vector2 direction = toTarget / Mathf.Max(distance, 0.0001f);
            Vector2 desiredVelocity = direction * speed;
            float steering = Mathf.Clamp01(deltaTime * 4.5f);
            particle.velocity = Vector2.Lerp(particle.velocity, desiredVelocity, steering);

            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Vector2 nextPosition = particle.position + particle.velocity * deltaTime;

            if ((target - nextPosition).sqrMagnitude > toTarget.sqrMagnitude && distance <= speed * deltaTime)
                nextPosition = target;

            float remainingDistance = (target - nextPosition).magnitude;
            float travelProgress = 1f - Mathf.Clamp01(remainingDistance / Mathf.Max(1f, particle.startDistance));
            float swayEnvelope = Mathf.Sin(Mathf.Clamp01(travelProgress) * Mathf.PI);
            float wobble = Mathf.Sin(particle.wobblePhase + particle.age * particle.wobbleFrequency * Mathf.PI * 2f)
                * particle.wobbleAmplitude
                * swayEnvelope;

            particle.destinationFade = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(0.78f, 0.98f, travelProgress));
            particle.position = nextPosition;
            particle.rect.anchoredPosition = particle.position + perpendicular * wobble;
            ApplyUiParticleVisuals(ref particle);

            uiParticles[i] = particle;
        }
    }

    void ApplyUiParticleVisuals(ref UiParticle particle)
    {
        float lifetime = Mathf.Max(0.0001f, particle.lifetime);
        float normalizedAge = Mathf.Clamp01(particle.age / lifetime);
        float size = particle.startSize;
        if (uiUseSizeOverLifetime)
            size *= Mathf.Max(0f, SampleCurve(uiSizeOverLifetimeCurve, normalizedAge, particle.sizeRandom));

        if (particle.rect)
            particle.rect.sizeDelta = Vector2.one * Mathf.Max(0.5f, size);

        if (particle.image)
        {
            Color color = particle.startColor;
            if (uiUseColorOverLifetime)
                color = MultiplyColors(color, SampleGradient(uiColorOverLifetimeGradient, normalizedAge, particle.colorRandom));

            color.a = Mathf.Lerp(
                Mathf.Clamp01(particle.startColor.a),
                0f,
                Mathf.Clamp01(particle.destinationFade));
            particle.image.color = color;
        }
    }

    Vector2 GetUiShapeSpawnOffset()
    {
        float radius = GetUiShapeRadiusPixels();
        return radius > 0.001f
            ? UnityEngine.Random.insideUnitCircle * radius
            : Vector2.zero;
    }

    float GetUiShapeRadiusPixels()
    {
        if (uiShapeRadius <= 0f)
            return 0f;

        float baseRadius = Mathf.Max(4f, uiParticleSize * 0.75f);
        float randomPositionScale = 1f + uiShapeRandomPositionAmount;
        return Mathf.Clamp(uiShapeRadius * baseRadius * randomPositionScale, 2f, 64f);
    }

    Vector2 GetUiInitialVelocity(Vector2 start, Vector2 target, float random)
    {
        Vector2 direction = target - start;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.up;
        else
            direction.Normalize();

        float angle = UnityEngine.Random.Range(-uiShapeAngle, uiShapeAngle);
        direction = Rotate(direction, angle);

        float startSpeed = Mathf.Max(0f, SampleCurve(uiStartSpeedCurve, 0f, random));
        float speedPixels = Mathf.Clamp(startSpeed * Mathf.Max(6f, uiParticleSize * 0.75f), 0f, 420f);
        float randomVelocity = Mathf.Clamp01(uiShapeRandomDirectionAmount + 0.2f);
        return direction * speedPixels + UnityEngine.Random.insideUnitCircle * speedPixels * randomVelocity;
    }

    Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos);
    }

    float ConvertParticleSizeToUiPixels(float particleSize)
    {
        return Mathf.Clamp(Mathf.Max(0.01f, particleSize) * uiRootScale * 70f, 1f, 36f);
    }

    void PlayArrivalPopSFX()
    {
        if (!AudioManager.I || arrivalPopSFXClips == null || arrivalPopSFXClips.Length == 0)
            return;

        if (Time.unscaledTime < nextArrivalPopTime)
            return;

        nextArrivalPopTime = Time.unscaledTime + ArrivalPopMinInterval;

        for (int i = 0; i < 8; i++)
        {
            AudioClip clip = arrivalPopSFXClips[UnityEngine.Random.Range(0, arrivalPopSFXClips.Length)];
            if (!clip) continue;

            AudioManager.I.PlaySFX(clip, arrivalPopSFXVolume, pitch: 1f, jitter: false);
            return;
        }
    }

    float SampleCurve(ParticleSystem.MinMaxCurve curve, float time, float random)
    {
        time = Mathf.Clamp01(time);
        random = Mathf.Clamp01(random);

        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return curve.constant;
            case ParticleSystemCurveMode.TwoConstants:
                return Mathf.Lerp(curve.constantMin, curve.constantMax, random);
            case ParticleSystemCurveMode.Curve:
                return curve.curve != null ? curve.curve.Evaluate(time) * curve.curveMultiplier : curve.constant;
            case ParticleSystemCurveMode.TwoCurves:
                float min = curve.curveMin != null ? curve.curveMin.Evaluate(time) : curve.constantMin;
                float max = curve.curveMax != null ? curve.curveMax.Evaluate(time) : curve.constantMax;
                return Mathf.Lerp(min, max, random) * curve.curveMultiplier;
            default:
                return curve.constant;
        }
    }

    Color SampleGradient(ParticleSystem.MinMaxGradient gradient, float time, float random)
    {
        time = Mathf.Clamp01(time);
        random = Mathf.Clamp01(random);

        switch (gradient.mode)
        {
            case ParticleSystemGradientMode.Color:
                return gradient.color;
            case ParticleSystemGradientMode.TwoColors:
                return Color.Lerp(gradient.colorMin, gradient.colorMax, random);
            case ParticleSystemGradientMode.Gradient:
                return gradient.gradient != null ? gradient.gradient.Evaluate(time) : gradient.color;
            case ParticleSystemGradientMode.TwoGradients:
                Color min = gradient.gradientMin != null ? gradient.gradientMin.Evaluate(time) : gradient.colorMin;
                Color max = gradient.gradientMax != null ? gradient.gradientMax.Evaluate(time) : gradient.colorMax;
                return Color.Lerp(min, max, random);
            case ParticleSystemGradientMode.RandomColor:
                return gradient.gradient != null ? gradient.gradient.Evaluate(random) : gradient.color;
            default:
                return gradient.color;
        }
    }

    Color MultiplyColors(Color a, Color b)
    {
        return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
    }

    bool ScreenToOverlayLocal(Vector2 screenPoint, out Vector2 localPoint)
    {
        localPoint = default;
        if (!overlayRoot)
            return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRoot,
            screenPoint,
            null,
            out localPoint);
    }

    float GetPixelsPerWorldUnit()
    {
        if (renderCamera && renderCamera.orthographic && renderCamera.orthographicSize > 0.001f)
        {
            float canvasHeight = overlayRoot && overlayRoot.rect.height > 1f
                ? overlayRoot.rect.height
                : Screen.height;
            return canvasHeight / (renderCamera.orthographicSize * 2f);
        }

        return 100f;
    }

    bool UsesScreenSpaceOverlay(Canvas canvas)
    {
        return canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay;
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
                    PlayArrivalPopSFX();
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
        if (useUiOverlay)
            return uiParticles.Count > 0;

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
