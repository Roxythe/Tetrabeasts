using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public sealed class GaugeLiquidMotion : MonoBehaviour
{
    Slider slider;
    MarathonLiquidSlosh slosh;
    [SerializeField, Min(.01f)] float fillSmoothSeconds = .22f;
    float target, velocity;
    bool animatingFill;
    static event System.Action<float> ScreenShaken;

    public static void NotifyScreenShake(float intensity) => ScreenShaken?.Invoke(Mathf.Clamp01(intensity));

    public void SetTarget(float value, bool immediate = false)
    {
        if (!slider) slider = GetComponent<Slider>();
        float next = Mathf.Clamp01(value);
        if (!immediate && slosh) slosh.AddImpulse(next - target);
        target = next;
        animatingFill = !immediate;
        if (immediate)
        {
            velocity = 0f;
            SetVisualValue(target);
        }
    }
    void OnEnable()
    {
        slider = GetComponent<Slider>();
        slosh = slider.fillRect ? slider.fillRect.GetComponent<MarathonLiquidSlosh>() : null;
        target = slider.normalizedValue;
        velocity = 0f;
        animatingFill = false;
        slider.onValueChanged.RemoveListener(OnExternalValueChanged);
        slider.onValueChanged.AddListener(OnExternalValueChanged);
        ScreenShaken -= ReactToShake;
        ScreenShaken += ReactToShake;
    }
    void OnDisable()
    {
        if (slider) slider.onValueChanged.RemoveListener(OnExternalValueChanged);
        ScreenShaken -= ReactToShake;
    }
    void OnExternalValueChanged(float value)
    {
        // Castle health and reserves write directly to their sliders. Observe those
        // changes without ever restoring an older animation target over them.
        float next = slider.normalizedValue;
        if (slosh) slosh.AddImpulse(next - target);
        target = next;
        velocity = 0f;
        animatingFill = false;
    }
    void SetVisualValue(float normalizedValue) =>
        slider.SetValueWithoutNotify(Mathf.Lerp(slider.minValue, slider.maxValue, normalizedValue));
    void ReactToShake(float intensity) { if (slosh) slosh.AddImpulse(intensity * .3f); }
    void LateUpdate()
    {
        Shader.SetGlobalFloat("_TetrabeastsLiquidTime", Time.unscaledTime);
        AdvanceVisual(Time.unscaledDeltaTime);
    }

    void AdvanceVisual(float deltaTime)
    {
        if (!animatingFill) return;
        float current = Mathf.SmoothDamp(slider.normalizedValue, target, ref velocity,
            fillSmoothSeconds, Mathf.Infinity, deltaTime);
        if (Mathf.Abs(current - target) < .0001f) { current = target; velocity = 0f; animatingFill = false; }
        // Visual interpolation must not create additional impulses each frame.
        SetVisualValue(current);
    }
}
