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
    public void Refresh(int level, float damage, float required)
    {
        if (liquidSlosh && previousRequired == required && damage > previousDamage)
            liquidSlosh.AddImpulse((damage - previousDamage) / Mathf.Max(1f, required));
        previousDamage = damage;
        previousRequired = required;
        gauge.normalizedValue = Mathf.Clamp01(damage / Mathf.Max(1f, required));
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
