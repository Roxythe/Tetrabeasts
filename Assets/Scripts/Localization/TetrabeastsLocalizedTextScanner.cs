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
    const int TextsPerFrame = 32;
    const double ScanBudgetMilliseconds = 2d;

    public static bool IsScanning => instance && (instance.scanQueued || instance.scanRoutine != null);

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
        if (scanRoutine == null)
            scanRoutine = StartCoroutine(ScanNextFrame());
    }

    IEnumerator ScanNextFrame()
    {
        yield return null;

        // Requests made during a scan schedule another pass, including dynamically
        // created UI, without restarting the work already completed.
        try
        {
            while (scanQueued)
            {
                scanQueued = false;
                TetrabeastsLocalization.EnsureInitialized();
                var texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                var legacyTexts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                // Iterate here so failures also release the loading-screen wait in finally.
                var scan = BindTexts(texts, legacyTexts, true);
                while (scan.MoveNext())
                    yield return scan.Current;
            }
        }
        finally
        {
            scanRoutine = null;
            scanQueued = false;
        }
    }

    void ScanAllText()
    {
        TetrabeastsLocalization.EnsureInitialized();

        var texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var legacyTexts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var scan = BindTexts(texts, legacyTexts, false);
        while (scan.MoveNext()) { }
    }

    static IEnumerator BindTexts(TMP_Text[] texts, Text[] legacyTexts, bool spreadAcrossFrames)
    {
        var budget = System.Diagnostics.Stopwatch.StartNew();
        int processed = 0;
        for (int i = 0; i < texts.Length + legacyTexts.Length; i++)
        {
            if (i < texts.Length)
                BindIfKnown(texts[i]);
            else
                BindIfKnown(legacyTexts[i - texts.Length]);

            processed++;
            if (spreadAcrossFrames &&
                (processed >= TextsPerFrame || budget.Elapsed.TotalMilliseconds >= ScanBudgetMilliseconds))
            {
                yield return null;
                processed = 0;
                budget.Restart();
            }
        }
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
