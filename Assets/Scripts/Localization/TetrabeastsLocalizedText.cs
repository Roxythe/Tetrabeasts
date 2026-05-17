using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class TetrabeastsLocalizedText : MonoBehaviour
{
    [SerializeField] TMP_Text target;
    [SerializeField] string localizationKey;
    [SerializeField] string englishFallback;

    public string LocalizationKey => localizationKey;

    public void Configure(TMP_Text text, string key, string fallback)
    {
        target = text ? text : GetComponent<TMP_Text>();
        localizationKey = key;
        englishFallback = fallback;
        Refresh();
    }

    void Reset()
    {
        target = GetComponent<TMP_Text>();
    }

    void Awake()
    {
        if (!target)
            target = GetComponent<TMP_Text>();

        if (string.IsNullOrWhiteSpace(localizationKey) &&
            target &&
            TetrabeastsLocalization.TryGetStaticTextKey(target.text, out string key, out string fallback))
        {
            localizationKey = key;
            englishFallback = fallback;
        }
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
        if (!target || string.IsNullOrWhiteSpace(localizationKey))
            return;

        target.text = TetrabeastsLocalization.GetText(localizationKey, englishFallback);
    }
}
