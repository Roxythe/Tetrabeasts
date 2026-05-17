using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TetrabeastsLocalizedRawText : MonoBehaviour
{
    [SerializeField] TMP_Text tmpTarget;
    [SerializeField] Text legacyTarget;
    [SerializeField] string englishFallback;

    public void Configure(TMP_Text text, string fallback)
    {
        tmpTarget = text;
        legacyTarget = null;
        englishFallback = fallback;
        Refresh();
    }

    public void Configure(Text text, string fallback)
    {
        legacyTarget = text;
        tmpTarget = null;
        englishFallback = fallback;
        Refresh();
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
        if (string.IsNullOrEmpty(englishFallback))
            return;

        string value = TetrabeastsLocalization.LocalizeText(englishFallback);

        if (tmpTarget)
            tmpTarget.text = value;

        if (legacyTarget)
            legacyTarget.text = value;
    }
}
