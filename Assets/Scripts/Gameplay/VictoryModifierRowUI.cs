using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VictoryModifierRowUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text shadowTitleText;
    [SerializeField] TMP_Text counterText;
    [SerializeField] TMP_Text shadowCounterText;

    void OnValidate()
    {
        AutoWire();
    }

    void Awake()
    {
        AutoWire();
    }

    public void Bind(Sprite icon, string title, string counter, Color titleColor)
    {
        AutoWire();

        if (iconImage)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        string localizedTitle = TetrabeastsLocalization.LocalizeText(title ?? string.Empty);

        if (titleText)
        {
            titleText.text = localizedTitle;
            titleText.color = titleColor;
        }

        if (shadowTitleText)
            shadowTitleText.text = localizedTitle;

        if (counterText)
            counterText.text = counter ?? string.Empty;

        if (shadowCounterText)
            shadowCounterText.text = counter ?? string.Empty;
    }

    void AutoWire()
    {
        if (!iconImage)
            iconImage = FindImage(transform, "iconimage");

        if (!titleText)
            titleText = FindText(transform, "titletext");

        if (!shadowTitleText)
            shadowTitleText = FindText(transform, "shadowtitletext");

        if (!counterText)
            counterText = FindText(transform, "countertext");

        if (!shadowCounterText)
            shadowCounterText = FindText(transform, "shadowcountertext");
    }

    static TMP_Text FindText(Transform rootTransform, string normalizedName)
    {
        var found = FindDescendant(rootTransform, normalizedName, exactMatch: false);
        return found ? found.GetComponent<TMP_Text>() : null;
    }

    static Image FindImage(Transform rootTransform, string normalizedName)
    {
        var found = FindDescendant(rootTransform, normalizedName, exactMatch: false);
        return found ? found.GetComponent<Image>() : null;
    }

    static Transform FindDescendant(Transform current, string normalizedName, bool exactMatch)
    {
        if (!current)
            return null;

        string currentName = NormalizeName(current.name);
        bool matches = exactMatch
            ? currentName == normalizedName
            : currentName.Contains(normalizedName);

        if (matches)
            return current;

        for (int i = 0; i < current.childCount; i++)
        {
            var found = FindDescendant(current.GetChild(i), normalizedName, exactMatch);
            if (found)
                return found;
        }

        return null;
    }

    static string NormalizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var buffer = new char[value.Length];
        int length = 0;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (!char.IsLetterOrDigit(c))
                continue;

            buffer[length++] = char.ToLowerInvariant(c);
        }

        return new string(buffer, 0, length);
    }
}
