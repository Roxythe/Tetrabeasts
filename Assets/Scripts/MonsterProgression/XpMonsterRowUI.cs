using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XpMonsterRowUI : MonoBehaviour
{
    [Header("Refs")]
    public Image portraitImage;
    public TMP_Text nameText;
    public TMP_Text levelText;
    public Slider xpSlider;
    public TMP_Text xpText;

    public TMP_Text xpDistText;
    public TMP_Text xpDrainText;

    public void BindStatic(Sprite portrait, string displayName)
    {
        if (portraitImage) portraitImage.sprite = portrait;
        if (nameText) nameText.text = displayName ?? "";

        HideDeltaTexts();
    }

    public void SetLevel(int level)
    {
        if (levelText) levelText.text = $"Lv.{level}";
    }

    public void SetXp(float xpInto, float xpPerLevel = 100f)
    {
        xpInto = Mathf.Clamp(xpInto, 0f, xpPerLevel);
        if (xpSlider) xpSlider.value = xpPerLevel > 0 ? (xpInto / xpPerLevel) : 0f;
        if (xpText) xpText.text = $"{xpInto:0.#}/{xpPerLevel:0}";
    }

    public void HideDeltaTexts()
    {
        if (xpDistText) xpDistText.gameObject.SetActive(false);
        if (xpDrainText) xpDrainText.gameObject.SetActive(false);
    }

    public void ShowXpDist(float gainedXp)
    {
        if (xpDrainText) xpDrainText.gameObject.SetActive(false);

        if (!xpDistText) return;
        xpDistText.gameObject.SetActive(true);
        xpDistText.text = $"+{FormatXp(gainedXp)} Exp";
    }

    public void ShowXpDrainPreserved(float preservedXp)
    {
        if (xpDistText) xpDistText.gameObject.SetActive(false);

        if (!xpDrainText) return;
        xpDrainText.gameObject.SetActive(true);
        xpDrainText.text = $"+{FormatXp(preservedXp)} Exp Preserved";
    }

    static string FormatXp(float xp)
    {
        // Prefer integers when close otherwise show 1 decimal
        float r = Mathf.Round(xp);
        if (Mathf.Abs(xp - r) < 0.01f) return r.ToString("0");
        return xp.ToString("0.#");
    }
}