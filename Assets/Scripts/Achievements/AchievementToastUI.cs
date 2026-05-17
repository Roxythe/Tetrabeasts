using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementToastUI : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public TMP_Text titleText;
    public TMP_Text descText;

    public void Set(AchievementDefSO def)
    {
        if (def == null) return;

        if (iconImage) iconImage.sprite = def.icon;
        if (titleText) titleText.text = TetrabeastsLocalization.LocalizeText(def.title);
        if (descText) descText.text = TetrabeastsLocalization.LocalizeText(def.description);
    }
}
