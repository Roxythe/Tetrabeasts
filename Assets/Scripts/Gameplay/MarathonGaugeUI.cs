using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MarathonGaugeUI : MonoBehaviour
{
    public Slider gauge;
    public TMP_Text levelLabel, damageLabel;
    public void Refresh(int level, float damage, float required)
    {
        gauge.normalizedValue = Mathf.Clamp01(damage / Mathf.Max(1f, required));
        levelLabel.text = "Level " + level;
        damageLabel.text = Mathf.FloorToInt(damage) + " / " + Mathf.CeilToInt(required);
    }
}
