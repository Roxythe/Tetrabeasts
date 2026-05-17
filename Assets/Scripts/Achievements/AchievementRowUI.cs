using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementRowUI : MonoBehaviour
{
    public Image icon;
    public Image lockedOverlay;
    public TMP_Text titleText;
    public TMP_Text descText;

    [Header("Colors")]
    public Color unlockedIconColor = Color.white;
    public Color lockedIconColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Optional Visuals")]
    public Material grayscaleMaterial; // Assign in Inspector if you want TRUE grayscale (desaturate)

    public void Set(AchievementDefSO def, bool unlocked)
    {
        if (!def) return;
        SetCustom(def.icon, def.title, def.description, unlocked, showLockedOverlay: !unlocked);
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
    }
}
