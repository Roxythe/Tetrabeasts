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

    public void Set(AchievementDefSO def, bool unlocked)
    {
        if (!def) return;

        if (icon)
        {
            icon.sprite = def.icon;
            icon.color = unlocked ? unlockedIconColor : lockedIconColor;
        }

        if (lockedOverlay)
            lockedOverlay.gameObject.SetActive(!unlocked);

        if (titleText) titleText.text = def.title;
        if (descText) descText.text = def.description;
    }
}