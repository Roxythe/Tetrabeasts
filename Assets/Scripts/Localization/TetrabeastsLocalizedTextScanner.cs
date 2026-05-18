using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TetrabeastsLocalizedTextScanner : MonoBehaviour
{
    const float ScanInterval = 0.75f;

    static TetrabeastsLocalizedTextScanner instance;
    float nextScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        EnsureInstance();
        instance.ScanAllText();
    }

    static void EnsureInstance()
    {
        if (instance)
            return;

        var go = new GameObject(nameof(TetrabeastsLocalizedTextScanner));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<TetrabeastsLocalizedTextScanner>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TetrabeastsLocalization.LanguageChanged += ScanAllText;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        TetrabeastsLocalization.LanguageChanged -= ScanAllText;
    }

    void Update()
    {
        if (Time.unscaledTime < nextScanTime)
            return;

        nextScanTime = Time.unscaledTime + ScanInterval;
        ScanAllText();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ScanAllText();
    }

    void ScanAllText()
    {
        TetrabeastsLocalization.EnsureInitialized();

        var texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
            BindIfKnown(texts[i]);

        var legacyTexts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < legacyTexts.Length; i++)
            BindIfKnown(legacyTexts[i]);
    }

    static void BindIfKnown(TMP_Text text)
    {
        if (!text)
            return;

        if (text.GetComponent<TetrabeastsLocalizedText>() || text.GetComponent<TetrabeastsLocalizedRawText>())
            return;

        if (!TetrabeastsLocalization.TryGetStaticTextKey(text.text, out string key, out string fallback))
        {
            if (TetrabeastsLocalization.HasRuntimeTranslation(text.text))
                text.gameObject.AddComponent<TetrabeastsLocalizedRawText>().Configure(text, text.text);

            return;
        }

        text.gameObject.AddComponent<TetrabeastsLocalizedText>().Configure(text, key, fallback);
    }

    static void BindIfKnown(Text text)
    {
        if (!text)
            return;

        if (text.GetComponent<TetrabeastsLocalizedLegacyText>() || text.GetComponent<TetrabeastsLocalizedRawText>())
            return;

        if (!TetrabeastsLocalization.TryGetStaticTextKey(text.text, out string key, out string fallback))
        {
            if (TetrabeastsLocalization.HasRuntimeTranslation(text.text))
                text.gameObject.AddComponent<TetrabeastsLocalizedRawText>().Configure(text, text.text);

            return;
        }

        text.gameObject.AddComponent<TetrabeastsLocalizedLegacyText>().Configure(text, key, fallback);
    }
}
