using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SlotMachineLightAnimator : MonoBehaviour
{
    const string GeneratedRootName = "Generated_SlotMachineLights";
    const float TwoPi = Mathf.PI * 2f;

    [Serializable]
    public class LightDefinition
    {
        public string name = "Light";

        [Tooltip("Position on the slot machine image. X/Y are 0 to 1 from bottom-left to top-right.")]
        public Vector2 normalizedPosition = new Vector2(0.5f, 0.5f);

        [Tooltip("Size of the solid bulb core in UI pixels.")]
        public Vector2 size = new Vector2(30f, 30f);

        [Tooltip("How large the soft outer glow is compared with Size.")]
        [Min(1f)] public float glowScale = 2.4f;

        [Tooltip("How large the cover patch is compared with Size when Cover Original Lights is enabled.")]
        [Min(0.1f)] public float coverScale = 1.15f;

        [Tooltip("Palette to cycle through. A single color will only pulse brightness.")]
        public Color[] colors =
        {
            new Color(1f, 0.15f, 0.2f, 1f),
            new Color(1f, 0.82f, 0.2f, 1f),
            new Color(0.15f, 1f, 0.45f, 1f),
            new Color(0.15f, 0.75f, 1f, 1f),
            new Color(0.85f, 0.2f, 1f, 1f)
        };

        [Tooltip("Brightness at the exact moment the animator turns on.")]
        [Range(0f, 1f)] public float initialBrightness = 0.5f;

        [Tooltip("Extra phase offset, in cycles. Use this to stagger lights with the same starting brightness.")]
        [Range(0f, 1f)] public float phaseOffset;

        [Tooltip("Brightness pulse speed.")]
        [Min(0f)] public float pulsesPerSecond = 1.25f;

        [Tooltip("Color cycle speed. Set to 0 to hold the first color.")]
        [Min(0f)] public float colorCyclesPerSecond = 0.35f;

        [Tooltip("Color cycle offset. Whole numbers start on the next palette color.")]
        public float colorOffset;

        [Range(0f, 1f)] public float minBrightness = 0.25f;
        [Range(0f, 1f)] public float maxBrightness = 1f;
        [Range(0f, 1f)] public float coreAlpha = 1f;
        [Range(0f, 1f)] public float glowAlpha = 0.55f;
        [Range(0f, 0.35f)] public float corePulseScale = 0.08f;
        [Range(0f, 0.35f)] public float glowPulseScale = 0.16f;

        public bool useCustomCoverColor;
        public Color coverColor = new Color(0.06f, 0.015f, 0.01f, 0.92f);
    }

    class LightView
    {
        public LightDefinition definition;
        public Image cover;
        public Image glow;
        public Image core;
    }

    [Header("Playback")]
    [SerializeField] bool autoBuildOnEnable = true;
    [SerializeField] bool useUnscaledTime = true;
    [SerializeField] bool animate = true;
    [SerializeField, Min(0f)] float globalSpeed = 1f;
    [SerializeField, Range(0f, 1.5f)] float globalBrightness = 1f;

    [Header("Replacement")]
    [SerializeField] bool coverOriginalLights = true;
    [SerializeField] Color defaultCoverColor = new Color(0.06f, 0.015f, 0.01f, 0.92f);

    [Header("Lights")]
    [SerializeField] LightDefinition[] lights;

    readonly List<LightView> _views = new();

    RectTransform _slotRect;
    RectTransform _generatedRoot;
    Sprite _glowSprite;
    Sprite _coverSprite;
    Vector2 _lastRootSize;
    float _startTime;
    bool _built;

    public IReadOnlyList<LightDefinition> Lights => lights;

    void Reset()
    {
        lights = CreateDefaultLightDefinitions();
    }

    void OnEnable()
    {
        _slotRect = transform as RectTransform;
        _startTime = GetTime();

        if (autoBuildOnEnable)
            RebuildLights();
    }

    void OnDisable()
    {
        if (_generatedRoot)
            _generatedRoot.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (_generatedRoot)
            DestroyGeneratedObject(_generatedRoot.gameObject);
    }

    void Update()
    {
        if (!_built && autoBuildOnEnable)
            RebuildLights();

        if (!_built || !_generatedRoot)
            return;

        if (!_generatedRoot.gameObject.activeSelf)
            _generatedRoot.gameObject.SetActive(true);

        RefreshLayoutIfNeeded();

        if (animate)
            AnimateLights();
    }

    [ContextMenu("Rebuild Lights")]
    public void RebuildLights()
    {
        _slotRect = transform as RectTransform;
        if (!_slotRect)
            return;

        if (lights == null || lights.Length == 0)
            lights = CreateDefaultLightDefinitions();

        EnsureSprites();
        EnsureRoot();
        ClearGeneratedChildren();

        _views.Clear();

        for (int i = 0; i < lights.Length; i++)
        {
            LightDefinition definition = lights[i];
            if (definition == null)
                continue;

            var view = new LightView
            {
                definition = definition,
                cover = CreateLayer($"Cover_{definition.name}", _coverSprite),
                glow = CreateLayer($"Glow_{definition.name}", _glowSprite),
                core = CreateLayer($"Core_{definition.name}", _glowSprite)
            };

            view.cover.gameObject.SetActive(coverOriginalLights);
            _views.Add(view);
        }

        _lastRootSize = Vector2.zero;
        RefreshLayoutIfNeeded(true);
        AnimateLights();
        _built = true;
    }

    [ContextMenu("Use Default Slot Machine Layout")]
    public void UseDefaultSlotMachineLayout()
    {
        lights = CreateDefaultLightDefinitions();
        RebuildLights();
    }

    [ContextMenu("Randomize Starting Brightness")]
    public void RandomizeStartingBrightness()
    {
        if (lights == null || lights.Length == 0)
            lights = CreateDefaultLightDefinitions();

        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].initialBrightness = UnityEngine.Random.Range(0.12f, 0.95f);
            lights[i].phaseOffset = UnityEngine.Random.Range(0f, 1f);
            lights[i].colorOffset = UnityEngine.Random.Range(0f, Mathf.Max(1, GetColorCount(lights[i])));
        }

        _startTime = GetTime();
        AnimateLights();
    }

    public void SetAnimating(bool shouldAnimate)
    {
        animate = shouldAnimate;
    }

    public void SetLightsVisible(bool visible)
    {
        if (_generatedRoot)
            _generatedRoot.gameObject.SetActive(visible);
    }

    void EnsureRoot()
    {
        if (_generatedRoot)
            return;

        Transform existing = transform.Find(GeneratedRootName);
        if (existing)
        {
            _generatedRoot = existing as RectTransform;
            if (!_generatedRoot)
                _generatedRoot = existing.gameObject.AddComponent<RectTransform>();
        }
        else
        {
            var rootGO = new GameObject(GeneratedRootName, typeof(RectTransform));
            rootGO.transform.SetParent(transform, false);
            _generatedRoot = rootGO.GetComponent<RectTransform>();
        }

        _generatedRoot.name = GeneratedRootName;
        _generatedRoot.anchorMin = Vector2.zero;
        _generatedRoot.anchorMax = Vector2.one;
        _generatedRoot.pivot = new Vector2(0.5f, 0.5f);
        _generatedRoot.anchoredPosition = Vector2.zero;
        _generatedRoot.sizeDelta = Vector2.zero;
        _generatedRoot.localScale = Vector3.one;
        _generatedRoot.localRotation = Quaternion.identity;
        _generatedRoot.SetAsLastSibling();
        _generatedRoot.gameObject.SetActive(true);
    }

    void EnsureSprites()
    {
        if (!_glowSprite)
            _glowSprite = CreateRadialSprite(64, 2.35f, false);

        if (!_coverSprite)
            _coverSprite = CreateRadialSprite(64, 12f, true);
    }

    void ClearGeneratedChildren()
    {
        if (!_generatedRoot)
            return;

        for (int i = _generatedRoot.childCount - 1; i >= 0; i--)
            DestroyGeneratedObject(_generatedRoot.GetChild(i).gameObject);
    }

    Image CreateLayer(string layerName, Sprite sprite)
    {
        var layerGO = new GameObject(layerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        layerGO.transform.SetParent(_generatedRoot, false);

        var image = layerGO.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;

        var rt = image.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        return image;
    }

    void RefreshLayoutIfNeeded(bool force = false)
    {
        if (!_generatedRoot)
            return;

        Vector2 currentSize = _generatedRoot.rect.size;
        if (!force && Approximately(currentSize, _lastRootSize))
            return;

        _lastRootSize = currentSize;

        for (int i = 0; i < _views.Count; i++)
            LayoutLight(_views[i]);
    }

    void LayoutLight(LightView view)
    {
        if (view?.definition == null)
            return;

        LightDefinition definition = view.definition;
        Vector2 position = NormalizedToAnchoredPosition(definition.normalizedPosition);

        LayoutLayer(view.cover, position, definition.size * definition.coverScale);
        LayoutLayer(view.glow, position, definition.size * definition.glowScale);
        LayoutLayer(view.core, position, definition.size);
    }

    static void LayoutLayer(Image image, Vector2 position, Vector2 size)
    {
        if (!image)
            return;

        RectTransform rt = image.rectTransform;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    Vector2 NormalizedToAnchoredPosition(Vector2 normalized)
    {
        Rect rect = _generatedRoot ? _generatedRoot.rect : _slotRect.rect;
        float x = Mathf.Lerp(rect.xMin, rect.xMax, Mathf.Clamp01(normalized.x));
        float y = Mathf.Lerp(rect.yMin, rect.yMax, Mathf.Clamp01(normalized.y));
        return new Vector2(x, y);
    }

    void AnimateLights()
    {
        float localTime = Mathf.Max(0f, GetTime() - _startTime) * Mathf.Max(0f, globalSpeed);

        for (int i = 0; i < _views.Count; i++)
        {
            LightView view = _views[i];
            LightDefinition definition = view.definition;
            if (definition == null)
                continue;

            float wave = EvaluateBrightnessWave(definition, localTime);
            float brightness = Mathf.Clamp01(Mathf.Lerp(definition.minBrightness, definition.maxBrightness, wave) * globalBrightness);
            Color paletteColor = EvaluatePalette(definition, localTime);

            Color coverColor = definition.useCustomCoverColor ? definition.coverColor : defaultCoverColor;
            SetImageColor(view.cover, coverOriginalLights ? coverColor : Color.clear);
            SetImageColor(view.glow, WithAlpha(paletteColor, paletteColor.a * definition.glowAlpha * brightness));
            SetImageColor(view.core, WithAlpha(paletteColor, paletteColor.a * definition.coreAlpha * brightness));

            float corePulse = 1f + definition.corePulseScale * wave;
            float glowPulse = 1f + definition.glowPulseScale * wave;
            LayoutLayer(view.core, view.core.rectTransform.anchoredPosition, definition.size * corePulse);
            LayoutLayer(view.glow, view.glow.rectTransform.anchoredPosition, definition.size * definition.glowScale * glowPulse);
        }
    }

    float EvaluateBrightnessWave(LightDefinition definition, float localTime)
    {
        float startPhase = BrightnessToPhase(definition.initialBrightness);
        float phase = startPhase + definition.phaseOffset;
        return 0.5f + 0.5f * Mathf.Sin((localTime * definition.pulsesPerSecond + phase) * TwoPi);
    }

    static float BrightnessToPhase(float brightness)
    {
        float normalized = Mathf.Clamp01(brightness) * 2f - 1f;
        return Mathf.Asin(normalized) / TwoPi;
    }

    static Color EvaluatePalette(LightDefinition definition, float localTime)
    {
        if (definition.colors == null || definition.colors.Length == 0)
            return Color.white;

        if (definition.colors.Length == 1 || definition.colorCyclesPerSecond <= 0f)
            return definition.colors[Mathf.Clamp(Mathf.FloorToInt(definition.colorOffset), 0, definition.colors.Length - 1)];

        float palettePosition = Mathf.Repeat(localTime * definition.colorCyclesPerSecond + definition.colorOffset, definition.colors.Length);
        int currentIndex = Mathf.FloorToInt(palettePosition);
        int nextIndex = (currentIndex + 1) % definition.colors.Length;
        float t = palettePosition - currentIndex;
        t = t * t * (3f - 2f * t);

        return Color.Lerp(definition.colors[currentIndex], definition.colors[nextIndex], t);
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }

    static void SetImageColor(Image image, Color color)
    {
        if (image)
            image.color = color;
    }

    float GetTime()
    {
        return useUnscaledTime ? Time.unscaledTime : Time.time;
    }

    static bool Approximately(Vector2 a, Vector2 b)
    {
        return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
    }

    static int GetColorCount(LightDefinition definition)
    {
        return definition.colors != null ? definition.colors.Length : 0;
    }

    static Sprite CreateRadialSprite(int size, float falloff, bool solidCore)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = solidCore ? "GeneratedSlotLightCover" : "GeneratedSlotLightGlow",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[size * size];
        float center = (size - 1) * 0.5f;
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha;

                if (solidCore)
                {
                    alpha = distance <= 0.74f ? 1f : Mathf.Clamp01(1f - (distance - 0.74f) / 0.26f);
                }
                else
                {
                    alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), falloff);
                }

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    static void DestroyGeneratedObject(GameObject go)
    {
        if (!go)
            return;

        if (Application.isPlaying)
            Destroy(go);
        else
            DestroyImmediate(go);
    }

    static LightDefinition[] CreateDefaultLightDefinitions()
    {
        var result = new List<LightDefinition>();

        Color[] carnivalPalette =
        {
            new Color(1f, 0.12f, 0.16f, 1f),
            new Color(1f, 0.78f, 0.18f, 1f),
            new Color(0.16f, 1f, 0.38f, 1f),
            new Color(0.12f, 0.7f, 1f, 1f),
            new Color(0.9f, 0.18f, 1f, 1f)
        };

        float[] topXs = { 0.31f, 0.37f, 0.43f, 0.50f, 0.57f, 0.63f, 0.69f };
        for (int i = 0; i < topXs.Length; i++)
        {
            result.Add(new LightDefinition
            {
                name = $"Top_{i + 1}",
                normalizedPosition = new Vector2(topXs[i], 0.78f),
                size = new Vector2(32f, 24f),
                glowScale = 2.8f,
                coverScale = 1.2f,
                colors = carnivalPalette,
                initialBrightness = Mathf.Lerp(0.18f, 0.94f, (i % 4) / 3f),
                phaseOffset = i * 0.13f,
                colorOffset = i,
                pulsesPerSecond = 1.25f,
                colorCyclesPerSecond = 0.42f,
                minBrightness = 0.18f,
                maxBrightness = 1f,
                glowAlpha = 0.6f
            });
        }

        result.Add(new LightDefinition
        {
            name = "Left_Side",
            normalizedPosition = new Vector2(0.14f, 0.64f),
            size = new Vector2(56f, 34f),
            glowScale = 2.25f,
            colors = carnivalPalette,
            initialBrightness = 0.85f,
            phaseOffset = 0.37f,
            colorOffset = 3f,
            pulsesPerSecond = 1.6f,
            colorCyclesPerSecond = 0.28f
        });

        result.Add(new LightDefinition
        {
            name = "Right_Side",
            normalizedPosition = new Vector2(0.86f, 0.64f),
            size = new Vector2(56f, 34f),
            glowScale = 2.25f,
            colors = carnivalPalette,
            initialBrightness = 0.25f,
            phaseOffset = 0.71f,
            colorOffset = 1f,
            pulsesPerSecond = 1.6f,
            colorCyclesPerSecond = 0.28f
        });

        float[] buttonXs = { 0.38f, 0.44f, 0.50f, 0.56f };
        for (int i = 0; i < buttonXs.Length; i++)
        {
            result.Add(new LightDefinition
            {
                name = $"Button_{i + 1}",
                normalizedPosition = new Vector2(buttonXs[i], 0.22f),
                size = new Vector2(30f, 24f),
                glowScale = 2.1f,
                coverScale = 1.1f,
                colors = carnivalPalette,
                initialBrightness = i % 2 == 0 ? 0.35f : 0.9f,
                phaseOffset = i * 0.22f,
                colorOffset = i + 2f,
                pulsesPerSecond = 1.9f,
                colorCyclesPerSecond = 0.55f,
                minBrightness = 0.22f,
                maxBrightness = 1f,
                glowAlpha = 0.5f
            });
        }

        return result.ToArray();
    }
}
