using System.Collections;
using TMPro;
using UnityEngine;

public sealed class ComboPulse : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField, Min(1f)] private float pulseScale = 1.25f;
    [SerializeField, Min(0.01f)] private float upTime = 0.08f;
    [SerializeField, Min(0.01f)] private float downTime = 0.12f;

    private Vector3 _baseScale;
    private Coroutine _cr;

    private void Awake()
    {
        if (!targetText) targetText = GetComponentInChildren<TMP_Text>(true);
        _baseScale = targetText ? targetText.rectTransform.localScale : Vector3.one;
    }

    public void Pulse()
    {
        if (!targetText) return;

        if (_cr != null) StopCoroutine(_cr);
        _cr = StartCoroutine(PulseCo());
    }

    private IEnumerator PulseCo()
    {
        var rt = targetText.rectTransform;

        float t = 0f;
        while (t < upTime)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / upTime);
            rt.localScale = Vector3.Lerp(_baseScale, _baseScale * pulseScale, EaseOutBack(a));
            yield return null;
        }

        t = 0f;
        while (t < downTime)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / downTime);
            rt.localScale = Vector3.Lerp(_baseScale * pulseScale, _baseScale, EaseOutCubic(a));
            yield return null;
        }

        rt.localScale = _baseScale;
        _cr = null;
    }

    private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}