using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TetrabeastsLocalizedTextScanner : MonoBehaviour
{
    static TetrabeastsLocalizedTextScanner instance;
    Coroutine scanRoutine;
    bool scanQueued;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        EnsureInstance().QueueScan();
    }

    public static void RequestScan()
    {
        EnsureInstance().QueueScan();
    }

    public static void ScanNow()
    {
        EnsureInstance().ScanAllText();
    }

    static TetrabeastsLocalizedTextScanner EnsureInstance()
    {
        if (instance)
            return instance;

        var go = new GameObject(nameof(TetrabeastsLocalizedTextScanner));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<TetrabeastsLocalizedTextScanner>();
        return instance;
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

        if (scanRoutine != null)
            StopCoroutine(scanRoutine);

        scanRoutine = null;
        scanQueued = false;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        QueueScan();
    }

    void QueueScan()
    {
        if (scanQueued)
            return;

        if (!isActiveAndEnabled)
        {
            ScanAllText();
            return;
        }

        scanQueued = true;
        scanRoutine = StartCoroutine(ScanNextFrame());
    }

    IEnumerator ScanNextFrame()
    {
        yield return null;

        scanRoutine = null;
        scanQueued = false;
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
