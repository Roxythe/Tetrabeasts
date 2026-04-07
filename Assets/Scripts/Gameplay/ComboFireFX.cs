using UnityEngine;
using UnityEngine.UI;

public sealed class ComboFireFX : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image[] layers;

    [Header("Scaling")]
    [SerializeField] private float baseScale = 1.0f;
    [SerializeField] private float scalePerCombo = 0.06f;
    [SerializeField, Min(0)] private int showAtCombo = 2;
    [SerializeField] private float maxScale = 2.5f;

    [Header("Color Flicker")]
    [SerializeField] private float baseColorFlickerSpeed = 2.0f;
    [SerializeField] private float flickerSpeedPerComboAboveThreshold = 0.2f;
    [SerializeField] private float colorFlickerSpeedCap = 5.0f;
    [SerializeField, Min(0.01f)] private float flickerSpeedRampTime = 0.25f;
    [SerializeField, Range(0f, 1f)] private float colorFlickerAmount = 0.25f;

    [Header("Layer Grouping")]
    [SerializeField] private int[] groupSizes = { 1, 1, 1 }; 
    [SerializeField] private float layerPhaseOffset = 0.77f;

    [SerializeField] private Color yellow = new(1f, 0.78f, 0.10f, 0.85f);
    [SerializeField] private Color orange = new(1f, 0.45f, 0.06f, 0.85f);
    [SerializeField] private Color red = new(1f, 0.12f, 0.03f, 0.85f);

    private int _combo;

    private float _currentFlickerSpeed;
    private float _targetFlickerSpeed;
    private float _flickerSpeedVelocity;

    private void Awake()
    {
        if (!root) root = (RectTransform)transform;

        _currentFlickerSpeed = baseColorFlickerSpeed;
        _targetFlickerSpeed = baseColorFlickerSpeed;
        _flickerSpeedVelocity = 0f;

        ApplyVisibility();
        ApplyScale();
        ApplyColors();
    }

    public void SetCombo(int combo)
    {
        _combo = Mathf.Max(0, combo);

        int above = Mathf.Max(0, _combo - showAtCombo);
        float desired = baseColorFlickerSpeed + above * flickerSpeedPerComboAboveThreshold;
        _targetFlickerSpeed = Mathf.Min(desired, colorFlickerSpeedCap);

        if (_combo < showAtCombo)
            _targetFlickerSpeed = baseColorFlickerSpeed;

        ApplyVisibility();
        ApplyScale();
        ApplyColors();
    }

    private void Update()
    {
        if (!root || layers == null || layers.Length == 0) return;

        _currentFlickerSpeed = Mathf.SmoothDamp(
            _currentFlickerSpeed,
            _targetFlickerSpeed,
            ref _flickerSpeedVelocity,
            flickerSpeedRampTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );

        if (_combo < showAtCombo) return;

        ApplyColors();
    }

    private void ApplyVisibility()
    {
        if (root) root.gameObject.SetActive(_combo >= showAtCombo);
    }

    private void ApplyScale()
    {
        if (!root) return;

        int effective = Mathf.Max(0, _combo - showAtCombo);
        float s = baseScale + (effective * scalePerCombo);
        s = Mathf.Min(s, Mathf.Max(baseScale, maxScale));

        root.localScale = new Vector3(s, s, 1f);
        root.localRotation = Quaternion.identity;
    }

    private void ApplyColors()
    {
        if (layers == null || layers.Length == 0) return;

        float t = Time.unscaledTime * _currentFlickerSpeed;

        int idx = 0;
        int band = 0;

        var sizes = (groupSizes == null || groupSizes.Length == 0)
            ? new[] { layers.Length }
            : groupSizes;

        while (idx < layers.Length)
        {
            int size = 1;

            if (band < sizes.Length)
                size = Mathf.Max(1, sizes[band]);
            else
                size = layers.Length - idx;

            float phase = t + band * layerPhaseOffset;
            Color warm = ComputeWarmFlickerColor(phase);

            int end = Mathf.Min(idx + size, layers.Length);
            for (int i = idx; i < end; i++)
            {
                var img = layers[i];
                if (!img) continue;

                img.color = warm;
                img.rectTransform.localRotation = Quaternion.identity;
                img.rectTransform.localScale = Vector3.one;
            }

            idx = end;
            band++;
        }
    }

    private Color ComputeWarmFlickerColor(float phase)
    {
        float k = 0.5f + 0.5f * Mathf.Sin(phase);

        Color warm = k < 0.5f
            ? Color.Lerp(red, orange, k * 2f)
            : Color.Lerp(orange, yellow, (k - 0.5f) * 2f);

        float intensity = 1f - colorFlickerAmount + colorFlickerAmount * (0.5f + 0.5f * Mathf.Sin(phase * 1.7f));
        warm.r *= intensity;
        warm.g *= intensity;
        warm.b *= intensity;

        return warm;
    }
}