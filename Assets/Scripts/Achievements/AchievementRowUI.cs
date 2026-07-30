using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementRowUI : MonoBehaviour
{
    public Image icon;
    public Image lockedOverlay;
    public TMP_Text titleText;
    public TMP_Text descText;
    public Slider progressSlider;
    public TMP_Text progressText;
    public Image progressFill;

    [Header("Colors")]
    public Color unlockedIconColor = Color.white;
    public Color lockedIconColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    public Color unlockedProgressFillColor = new Color(0.1137255f, 0.9058824f, 0.07450981f, 1f);
    public Color lockedProgressFillColor = new Color(0.7362523f, 0f, 1f, 1f);

    [Header("Optional Visuals")]
    public Material grayscaleMaterial; // Assign in Inspector if you want TRUE grayscale (desaturate)

    public void Set(AchievementDefSO def, bool unlocked)
    {
        if (!def) return;
        SetCustom(def.icon, def.title, def.description, unlocked, showLockedOverlay: !unlocked);
        SetProgress(AchievementSystem.GetProgress(def.achievementId), unlocked);
    }

    // Use this for special rows
    public void SetCustom(Sprite customIcon, string title, string desc, bool unlocked, bool showLockedOverlay)
    {
        if (icon)
        {
            icon.sprite = customIcon;
            icon.color = unlocked ? unlockedIconColor : lockedIconColor;

            // Assign a true grayscale material for locked icons
            if (!unlocked && grayscaleMaterial != null) icon.material = grayscaleMaterial;
            else icon.material = null;
        }

        if (lockedOverlay)
            lockedOverlay.gameObject.SetActive(showLockedOverlay);

        if (titleText) titleText.text = TetrabeastsLocalization.LocalizeText(title);
        if (descText) descText.text = TetrabeastsLocalization.LocalizeText(desc);
        ClearProgress();
    }

    void SetProgress(AchievementSystem.ProgressInfo progress, bool unlocked)
    {
        if (progressSlider)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.wholeNumbers = false;
            progressSlider.interactable = false;
            progressSlider.SetValueWithoutNotify(Mathf.Clamp01(progress.normalized));
        }

        if (progressText)
            progressText.text = $"{FormatProgressNumber(progress.current)} / {FormatProgressNumber(progress.target)}";

        if (progressFill)
            progressFill.color = unlocked ? unlockedProgressFillColor : lockedProgressFillColor;
    }

    void ClearProgress()
    {
        if (progressSlider)
            progressSlider.SetValueWithoutNotify(0f);

        if (progressText)
            progressText.text = string.Empty;

        if (progressFill)
            progressFill.color = lockedProgressFillColor;
    }

    static string FormatProgressNumber(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "0";

        return ((long)System.Math.Floor(System.Math.Max(0, value) + 0.0001)).ToString();
    }
}
