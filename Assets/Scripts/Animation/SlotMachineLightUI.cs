using System;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class SlotMachineLightUI : MonoBehaviour
{
    const float TwoPi = Mathf.PI * 2f;

    struct AnimationSettings
    {
        public float initialBrightness;
        public float phaseOffset;
        public float pulsesPerSecond;
        public float colorCyclesPerSecond;
        public float colorOffset;
        public float pulseScale;
        public float speed;
    }

    [Header("Playback")]
    [SerializeField] bool animate = true;
    [SerializeField] bool previewInEditMode = true;
    [SerializeField] bool useUnscaledTime = true;
    [SerializeField, Min(0f)] float speed = 1f;

    [Header("Brightness")]
    [SerializeField, Range(0f, 1f)] float initialBrightness = 0.5f;
    [SerializeField, Range(0f, 1f)] float phaseOffset;
    [SerializeField, Min(0f)] float pulsesPerSecond = 1.35f;
    [SerializeField, Range(0f, 1f)] float minBrightness = 0.25f;
    [SerializeField, Range(0f, 1f)] float maxBrightness = 1f;

    [Header("Color")]
    [SerializeField] Color[] colors =
    {
        new Color(1f, 0.15f, 0.2f, 1f),
        new Color(1f, 0.82f, 0.18f, 1f),
        new Color(0.15f, 1f, 0.45f, 1f),
        new Color(0.15f, 0.7f, 1f, 1f),
        new Color(0.85f, 0.2f, 1f, 1f)
    };

    [SerializeField, Min(0f)] float colorCyclesPerSecond = 0.4f;
    [SerializeField] float colorOffset;
    [SerializeField, Range(0f, 1f)] float alpha = 1f;

    [Header("Scale Pulse")]
    [SerializeField, Range(0f, 0.5f)] float pulseScale = 0.08f;

    [Header("Setup")]
    [SerializeField] bool disableRaycastTarget = true;
    [SerializeField] bool preserveAspect = true;

    Image _image;
    RectTransform _rectTransform;
    Vector3 _baseScale = Vector3.one;
    AnimationSettings _startingSettings;
    float _startTime;
    bool _hasStartingSettings;
    bool _hasBaseScale;

    void Reset()
    {
        EnsureReferences();
        ApplyImageDefaults();
        ApplyVisual(0f);
    }

    void OnEnable()
    {
        EnsureReferences();
        ApplyImageDefaults();
        CaptureStartingSettingsIfNeeded();
        CaptureBaseScaleIfNeeded();

        _startTime = GetTime();
        ApplyVisual(0f);
    }

    void OnDisable()
    {
        if (Application.isPlaying)
            RestoreStartingSettings();
    }

    void OnValidate()
    {
        EnsureReferences();
        ApplyImageDefaults();

        if (!Application.isPlaying && previewInEditMode)
            ApplyVisual(0f);
    }

    void Update()
    {
        bool shouldAnimate = animate && (Application.isPlaying || previewInEditMode);
        if (!shouldAnimate)
            return;

        float elapsed = Mathf.Max(0f, GetTime() - _startTime) * Mathf.Max(0f, speed);
        ApplyVisual(elapsed);
    }

    [ContextMenu("Randomize Starting Brightness")]
    public void RandomizeStartingBrightness()
    {
        initialBrightness = UnityEngine.Random.Range(0.08f, 0.95f);
        phaseOffset = UnityEngine.Random.Range(0f, 1f);
        colorOffset = UnityEngine.Random.Range(0f, Mathf.Max(1, colors != null ? colors.Length : 0));
        _startTime = GetTime();
        ApplyVisual(0f);
    }

    [ContextMenu("Snap To Brightest Preview")]
    public void SnapToBrightestPreview()
    {
        initialBrightness = 1f;
        phaseOffset = 0f;
        ApplyVisual(0f);
    }

    public void SetAnimating(bool shouldAnimate)
    {
        animate = shouldAnimate;
    }

    public void CaptureStartingSettings()
    {
        _startingSettings = ReadCurrentSettings();
        _hasStartingSettings = true;
        CaptureBaseScaleIfNeeded();
    }

    public void RestoreStartingSettings()
    {
        CaptureStartingSettingsIfNeeded();

        ApplySettings(_startingSettings);
        _startTime = GetTime();

        if (_rectTransform && _hasBaseScale)
            _rectTransform.localScale = _baseScale;

        ApplyVisual(0f);
    }

    public void PlaySyncedFlash(
        float pulseSpeedMultiplier,
        float colorSpeedMultiplier,
        float syncedPulseScale,
        float syncedInitialBrightness = 1f,
        float syncedPhaseOffset = 0f,
        float syncedColorOffset = 0f)
    {
        CaptureStartingSettingsIfNeeded();

        initialBrightness = Mathf.Clamp01(syncedInitialBrightness);
        phaseOffset = Mathf.Repeat(syncedPhaseOffset, 1f);
        colorOffset = syncedColorOffset;
        pulsesPerSecond = _startingSettings.pulsesPerSecond * Mathf.Max(0f, pulseSpeedMultiplier);
        colorCyclesPerSecond = _startingSettings.colorCyclesPerSecond * Mathf.Max(0f, colorSpeedMultiplier);
        pulseScale = Mathf.Max(0f, syncedPulseScale);
        speed = _startingSettings.speed;

        _startTime = GetTime();
        ApplyVisual(0f);
    }

    void EnsureReferences()
    {
        if (!_image)
            _image = GetComponent<Image>();

        if (!_rectTransform)
            _rectTransform = transform as RectTransform;
    }

    void CaptureStartingSettingsIfNeeded()
    {
        if (_hasStartingSettings)
            return;

        CaptureStartingSettings();
    }

    void CaptureBaseScaleIfNeeded()
    {
        if (_hasBaseScale)
            return;

        _baseScale = _rectTransform ? _rectTransform.localScale : Vector3.one;
        _hasBaseScale = true;
    }

    AnimationSettings ReadCurrentSettings()
    {
        return new AnimationSettings
        {
            initialBrightness = initialBrightness,
            phaseOffset = phaseOffset,
            pulsesPerSecond = pulsesPerSecond,
            colorCyclesPerSecond = colorCyclesPerSecond,
            colorOffset = colorOffset,
            pulseScale = pulseScale,
            speed = speed
        };
    }

    void ApplySettings(AnimationSettings settings)
    {
        initialBrightness = settings.initialBrightness;
        phaseOffset = settings.phaseOffset;
        pulsesPerSecond = settings.pulsesPerSecond;
        colorCyclesPerSecond = settings.colorCyclesPerSecond;
        colorOffset = settings.colorOffset;
        pulseScale = settings.pulseScale;
        speed = settings.speed;
    }

    void ApplyImageDefaults()
    {
        if (!_image)
            return;

        if (disableRaycastTarget)
            _image.raycastTarget = false;

        _image.preserveAspect = preserveAspect;
    }

    void ApplyVisual(float elapsed)
    {
        if (!_image)
            return;

        float wave = EvaluateBrightnessWave(elapsed);
        float brightness = Mathf.Clamp01(Mathf.Lerp(minBrightness, maxBrightness, wave));
        Color color = EvaluatePalette(elapsed);
        color.a = Mathf.Clamp01(alpha * brightness);
        _image.color = color;

        if (_rectTransform)
        {
            float scale = 1f + pulseScale * wave;
            _rectTransform.localScale = _baseScale * scale;
        }
    }

    float EvaluateBrightnessWave(float elapsed)
    {
        float phase = BrightnessToPhase(initialBrightness) + phaseOffset;
        return 0.5f + 0.5f * Mathf.Sin((elapsed * pulsesPerSecond + phase) * TwoPi);
    }

    Color EvaluatePalette(float elapsed)
    {
        if (colors == null || colors.Length == 0)
            return Color.white;

        if (colors.Length == 1 || colorCyclesPerSecond <= 0f)
        {
            int index = Mathf.FloorToInt(Mathf.Repeat(colorOffset, colors.Length));
            return colors[index];
        }

        float palettePosition = Mathf.Repeat(elapsed * colorCyclesPerSecond + colorOffset, colors.Length);
        int currentIndex = Mathf.FloorToInt(palettePosition);
        int nextIndex = (currentIndex + 1) % colors.Length;
        float t = palettePosition - currentIndex;
        t = t * t * (3f - 2f * t);

        return Color.Lerp(colors[currentIndex], colors[nextIndex], t);
    }

    float GetTime()
    {
        return useUnscaledTime ? Time.unscaledTime : Time.time;
    }

    static float BrightnessToPhase(float brightness)
    {
        float normalized = Mathf.Clamp01(brightness) * 2f - 1f;
        return Mathf.Asin(normalized) / TwoPi;
    }
}
