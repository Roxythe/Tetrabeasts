using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodexEntryUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Image backgroundImage;
    [SerializeField] Image iconImage;
    [SerializeField] Image notFoundOverlayImage;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text titleShadowText;
    [SerializeField] TMP_Text descriptionText;

    [Header("Background Colors")]
    [SerializeField] Color notFoundBackgroundColor = Color.white;
    [SerializeField] Color runBuffBackgroundColor = Color.blue;
    [SerializeField] Color runDebuffBackgroundColor = Color.red;
    [SerializeField] Color levelModifierBackgroundColor = Color.yellow;

    public enum EntryCategory
    {
        RunBuff = 0,
        RunDebuff = 1,
        LevelModifier = 2
    }

    public void Bind(RunModifierSO modifier, bool unlocked, EntryCategory category)
    {
        if (!modifier)
            return;

        Bind(modifier.icon, modifier.displayName, modifier.description, unlocked, category);
    }

    public void Bind(LevelModifierSO modifier, bool unlocked)
    {
        if (!modifier)
            return;

        Bind(modifier.icon, modifier.displayName, modifier.description, unlocked, EntryCategory.LevelModifier);
    }

    public void Bind(Sprite icon, string title, string description, bool unlocked, EntryCategory category)
    {
        if (backgroundImage)
            backgroundImage.color = GetBackgroundColor(unlocked, category);

        if (iconImage)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(unlocked && icon != null);
        }

        if (notFoundOverlayImage)
            notFoundOverlayImage.gameObject.SetActive(!unlocked);

        string displayTitle = unlocked
            ? TetrabeastsLocalization.LocalizeText(title ?? string.Empty)
            : "???";
        string displayDescription = unlocked
            ? TetrabeastsLocalization.LocalizeText(description ?? string.Empty)
            : TetrabeastsLocalization.LocalizeText("Modifier not yet discovered.");

        if (titleText)
            titleText.text = displayTitle;

        if (titleShadowText)
            titleShadowText.text = displayTitle;

        if (descriptionText)
            descriptionText.text = displayDescription;
    }

    Color GetBackgroundColor(bool unlocked, EntryCategory category)
    {
        if (!unlocked)
            return notFoundBackgroundColor;

        return category switch
        {
            EntryCategory.RunBuff => runBuffBackgroundColor,
            EntryCategory.RunDebuff => runDebuffBackgroundColor,
            EntryCategory.LevelModifier => levelModifierBackgroundColor,
            _ => notFoundBackgroundColor
        };
    }
}
