using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MarathonGaugeUI : MonoBehaviour
{
    public Slider gauge;
    public TMP_Text levelLabel, damageLabel;
    [Tooltip("Canvas-space offset toward the board from the mirrored ultimate gauge position.")]
    public float horizontalOffset = 28f;
    public MarathonLiquidSlosh liquidSlosh;
    public RectTransform glassOverlay;
    float previousDamage;
    float previousRequired;
    GaugeLiquidMotion motion;
    void Awake() => EnsureMotion();
    void EnsureMotion()
    {
        if (!motion && gauge) motion = gauge.GetComponent<GaugeLiquidMotion>() ?? gauge.gameObject.AddComponent<GaugeLiquidMotion>();
    }
    public System.Collections.IEnumerator FlashLevelUp(float seconds = .65f)
    {
        var fill = gauge && gauge.fillRect ? gauge.fillRect.GetComponent<UnityEngine.UI.Graphic>() : null;
        if (!fill) yield break;
        Color original = fill.color;
        try
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                fill.color = Color.Lerp(original, new Color(.8f, .4f, 1f, original.a), .5f + .5f * Mathf.Sin(elapsed * Mathf.PI * 8f));
                if (liquidSlosh) liquidSlosh.AddImpulse(.03f);
                yield return null;
            }
        }
        finally { if (fill) fill.color = original; }
    }
    public void Refresh(int level, float damage, float required)
    {
        EnsureMotion();
        if (previousRequired <= 0 || damage != previousDamage || required != previousRequired)
            motion.SetTarget(Mathf.Clamp01(damage / Mathf.Max(1f, required)), immediate: previousRequired <= 0);
        previousDamage = damage;
        previousRequired = required;
        if (levelLabel) levelLabel.gameObject.SetActive(false);
        damageLabel.text = Mathf.FloorToInt(damage) + " / " + Mathf.CeilToInt(required);
        // Keep the sideways label centered when runtime layout matches the ultimate gauge height.
        damageLabel.rectTransform.sizeDelta = new Vector2(((RectTransform)transform).rect.height - 40f, 36f);
        if (glassOverlay)
        {
            var rect = ((RectTransform)transform).rect;
            glassOverlay.sizeDelta = new Vector2(rect.height * 1.049158f, rect.width * 0.577755f);
        }
    }
}
