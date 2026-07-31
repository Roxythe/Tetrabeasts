using TMPro;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class CodexEntryUI : MonoBehaviour
{
    static readonly Regex LeadingIntensityRegex = new Regex(
        @"^\s*(?:slightly|modestly|moderately|moderatley|greatly|significantly|massively|massivley)\s+",
        RegexOptions.IgnoreCase);
    static readonly Regex MultiplierVerbRegex = new Regex(
        @"^\s*(?:double|doubles|triple|triples|quadruple|quadruples|quintuple|quintuples|qunituple|qunituples)\b",
        RegexOptions.IgnoreCase);
    static readonly Regex LeadingDecreaseSynonymRegex = new Regex(
        @"^\s*(?:reduce|reduces)\b",
        RegexOptions.IgnoreCase);
    static readonly Regex LeadingIncreaseInfinitiveRegex = new Regex(
        @"^\s*(?:increase|incecrease|incecreases)\b",
        RegexOptions.IgnoreCase);
    static readonly Regex LeadingDecreaseInfinitiveRegex = new Regex(
        @"^\s*decrease\b",
        RegexOptions.IgnoreCase);
    static readonly Regex NumericAmountSuffixRegex = new Regex(
        @"\s+by\s+\d+(?:\.\d+)?%?(?=\s*[.!?]?$)",
        RegexOptions.IgnoreCase);
    static readonly Regex RepeatedWhitespaceRegex = new Regex(@"\s{2,}");

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
        if (unlocked && IsRunModifierCategory(category))
            displayDescription = BuildGenericRunModifierDescription(displayDescription);

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

    static bool IsRunModifierCategory(EntryCategory category)
    {
        return category == EntryCategory.RunBuff || category == EntryCategory.RunDebuff;
    }

    static string BuildGenericRunModifierDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return description ?? string.Empty;

        string text = description.Trim();
        text = LeadingIntensityRegex.Replace(text, string.Empty);
        text = MultiplierVerbRegex.Replace(text, "Increases");
        text = LeadingDecreaseSynonymRegex.Replace(text, "Decreases");
        text = LeadingIncreaseInfinitiveRegex.Replace(text, "Increases");
        text = LeadingDecreaseInfinitiveRegex.Replace(text, "Decreases");
        text = NumericAmountSuffixRegex.Replace(text, string.Empty);
        text = RepeatedWhitespaceRegex.Replace(text, " ").Trim();

        return CapitalizeFirstLetter(text);
    }

    static string CapitalizeFirstLetter(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        char first = text[0];
        if (!char.IsLetter(first) || char.IsUpper(first))
            return text;

        return char.ToUpperInvariant(first) + text.Substring(1);
    }
}
