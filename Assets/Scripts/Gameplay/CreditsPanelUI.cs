using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CreditsPanelUI : MonoBehaviour
{
    const string DefaultLocalizationKey = "credits_body";
    const string DefaultEnglishBody =
        "Solo Development Project By:\n" +
        "Roxythe (Rocky) Harding\n\n" +
        "Big Thank You To:\n" +
        "Andrue Harless\n" +
        "Joyce Shen\n" +
        "Nick Blackwell\n" +
        "Trent Mcconnaughhay\n" +
        "Zach Warren\n\n" +
        "For Beta Testing And Providing Feedback To Improve The Game!";

    [SerializeField] TMP_Text creditsText;
    [SerializeField] string localizationKey = DefaultLocalizationKey;
    [SerializeField, TextArea(4, 10)] string englishFallback = DefaultEnglishBody;
    [SerializeField] Color nameColor = new(1f, 0.78f, 0.18f, 1f);
    [SerializeField] string[] highlightedNames =
    {
        "Roxythe (Rocky) Harding",
        "Andrue Harless",
        "Joyce Shen",
        "Nick Blackwell",
        "Trent Mcconnaughhay",
        "Zach Warren"
    };

    public void ResolveReferences()
    {
        if (!creditsText)
            creditsText = GetComponentInChildren<TMP_Text>(true);
    }

    void Awake()
    {
        ResolveReferences();
    }

    void OnEnable()
    {
        TetrabeastsLocalization.EnsureInitialized();
        TetrabeastsLocalization.LanguageChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        TetrabeastsLocalization.LanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        ResolveReferences();
        if (!creditsText)
            return;

        creditsText.richText = true;
        creditsText.text = ApplyNameHighlight(
            TetrabeastsLocalization.GetText(localizationKey, englishFallback));
    }

    string ApplyNameHighlight(string text)
    {
        if (string.IsNullOrEmpty(text) || highlightedNames == null || highlightedNames.Length == 0)
            return text;

        string colorHex = ColorUtility.ToHtmlStringRGB(nameColor);
        for (int i = 0; i < highlightedNames.Length; i++)
        {
            string name = highlightedNames[i];
            if (string.IsNullOrWhiteSpace(name))
                continue;

            text = text.Replace(name, $"<color=#{colorHex}>{name}</color>");
        }

        return text;
    }
}
