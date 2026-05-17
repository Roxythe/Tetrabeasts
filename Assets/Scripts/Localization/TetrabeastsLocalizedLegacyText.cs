using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Text))]
public sealed class TetrabeastsLocalizedLegacyText : MonoBehaviour
{
    [SerializeField] Text target;
    [SerializeField] string localizationKey;
    [SerializeField] string englishFallback;

    public void Configure(Text text, string key, string fallback)
    {
        target = text ? text : GetComponent<Text>();
        localizationKey = key;
        englishFallback = fallback;
        Refresh();
    }

    void Reset()
    {
        target = GetComponent<Text>();
    }

    void Awake()
    {
        if (!target)
            target = GetComponent<Text>();

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
