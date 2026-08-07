using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-9999)]
public sealed class LoadingScreen : MonoBehaviour
{
    const string SettingsResourcePath = "LoadingScreenSettings";
    const int CanvasSortingOrder = 32767;

    static LoadingScreen _instance;
    static LoadingScreenSettings _settings;
    static readonly List<int> _backgroundImageBag = new();
    static LoadingScreenSettings _backgroundBagSettings;
    static int _backgroundBagLength = -1;
    static int _lastBackgroundImageIndex = -1;

    Canvas _canvas;
    CanvasGroup _canvasGroup;
    RectTransform _canvasRect;
    Image _backgroundImage;
    LoadingScreenVignetteGraphic _vignetteGraphic;
    GameObject _iconInstance;
    TMP_Text[] _loadingTexts;
    Color[] _loadingTextBaseColors;
    Coroutine _hideCoroutine;
    Coroutine _sceneLoadCoroutine;
    Coroutine _textBlinkCoroutine;
    float _shownAtRealtime;
    float _timeScaleBeforeShow = 1f;
    bool _timeScalePauseActive;
    bool _restoreNormalTimeScaleOnHide;

    public static bool IsVisible => _instance && _instance._canvas && _instance._canvas.gameObject.activeSelf;
    public static bool IsSceneLoadRunning => _instance && _instance._sceneLoadCoroutine != null;

    static LoadingScreenSettings Settings
    {
        get
        {
            if (!_settings)
                _settings = Resources.Load<LoadingScreenSettings>(SettingsResourcePath);

            return _settings;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Application.isPlaying)
            Ensure()?.PrewarmInternal();
    }

    public static LoadingScreen Ensure()
    {
        if (_instance)
            return _instance;

        var existing = FindFirstObjectByType<LoadingScreen>(FindObjectsInactive.Include);
        if (existing)
        {
            _instance = existing;
            if (!_instance.gameObject.activeSelf)
                _instance.gameObject.SetActive(true);

            return _instance;
        }

        var go = new GameObject("LoadingScreen");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<LoadingScreen>();
        return _instance;
    }

    public static void Show()
    {
        Ensure()?.ShowInternal();
    }

    public static void Hide()
    {
        var loadingScreen = Ensure();
        if (!loadingScreen)
            return;

        if (loadingScreen._sceneLoadCoroutine != null)
            return;

        loadingScreen.HideAfterSettled();
    }

    public static void Prewarm()
    {
        Ensure()?.PrewarmInternal();
    }

    public static IEnumerator WaitUntilMinimumVisible()
    {
        var loadingScreen = Ensure();
        if (!loadingScreen || !IsVisible)
            yield break;

        yield return loadingScreen.WaitForMinimumVisible();
    }

    public static void RestartMinimumVisibleTimer()
    {
        var loadingScreen = Ensure();
        if (!loadingScreen || !IsVisible)
            return;

        loadingScreen._shownAtRealtime = Time.unscaledTime;
    }

    public static IEnumerator HideAndWaitUntilInactive()
    {
        var loadingScreen = Ensure();
        if (!loadingScreen || !IsVisible)
            yield break;

        if (loadingScreen._sceneLoadCoroutine == null)
            loadingScreen.HideAfterSettled();

        while (loadingScreen && loadingScreen._canvas && loadingScreen._canvas.gameObject.activeSelf)
            yield return null;
    }

    public static bool LoadSceneAsync(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return false;

        var loadingScreen = Ensure();
        if (!loadingScreen)
            return false;

        if (loadingScreen._sceneLoadCoroutine != null)
            return true;

        loadingScreen._sceneLoadCoroutine = loadingScreen.StartCoroutine(loadingScreen.LoadSceneRoutine(sceneName));
        return true;
    }

    void Awake()
    {
        if (_instance && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        RestoreLoadingTimeScalePause();

        if (_instance == this)
            _instance = null;
    }

    void LateUpdate()
    {
        if (_timeScalePauseActive && IsVisible && !Mathf.Approximately(Time.timeScale, 0f))
            Time.timeScale = 0f;
    }

    IEnumerator LoadSceneRoutine(string sceneName)
    {
        ShowInternal(restoreNormalTimeScaleOnHide: true);
        Canvas.ForceUpdateCanvases();
        yield return null;

        AsyncOperation operation = null;
        ThreadPriority previousBackgroundPriority = Application.backgroundLoadingPriority;
        Application.backgroundLoadingPriority = ThreadPriority.BelowNormal;

        try
        {
            operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation != null)
                operation.allowSceneActivation = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadingScreen: Failed to start loading scene '{sceneName}'. {e.Message}");
        }

        if (operation == null)
        {
            Application.backgroundLoadingPriority = previousBackgroundPriority;
            _sceneLoadCoroutine = null;
            HideAfterSettled();
            yield break;
        }

        while (operation.progress < 0.9f)
            yield return null;

        yield return WaitForMinimumVisible();

        operation.allowSceneActivation = true;

        while (!operation.isDone)
            yield return null;

        Application.backgroundLoadingPriority = previousBackgroundPriority;
        yield return WaitForSceneSettle();

        _sceneLoadCoroutine = null;
        HideInternal();
    }

    void PrewarmInternal()
    {
        _ = Settings;
        EnsureCanvas();
        CacheLoadingTexts();
        RefreshVignetteGraphic();
    }

    void ShowInternal(bool restoreNormalTimeScaleOnHide = false)
    {
        EnsureCanvas();

        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        _canvas.gameObject.SetActive(true);
        _canvas.transform.SetAsLastSibling();
        _canvas.enabled = true;
        ApplyLoadingTimeScalePause(restoreNormalTimeScaleOnHide);
        ApplyRandomBackground();
        StartLoadingTextBlink();

        if (_canvasGroup)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = false;
        }

        _shownAtRealtime = Time.unscaledTime;
    }

    void HideAfterSettled()
    {
        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);

        _hideCoroutine = StartCoroutine(HideAfterSettledRoutine());
    }

    IEnumerator HideAfterSettledRoutine()
    {
        yield return WaitForSceneSettle();
        HideInternal();
        _hideCoroutine = null;
    }

    IEnumerator WaitForSceneSettle()
    {
        int frames = Mathf.Max(0, Settings ? Settings.hideDelayFrames : 1);
        while (frames > 0)
        {
            frames--;
            yield return null;
        }

        yield return WaitForMinimumVisible();
    }

    IEnumerator WaitForMinimumVisible()
    {
        float remaining = GetMinimumVisibleRemainingSeconds();
        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);
    }

    float GetMinimumVisibleRemainingSeconds()
    {
        float minimumVisibleSeconds = Mathf.Max(0f, Settings ? Settings.minimumVisibleSeconds : 0f);
        return minimumVisibleSeconds - (Time.unscaledTime - _shownAtRealtime);
    }

    void HideInternal()
    {
        if (!_canvas)
            return;

        if (_canvasGroup)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        _canvas.gameObject.SetActive(false);
        StopLoadingTextBlink();
        RestoreLoadingTimeScalePause();
    }

    void ApplyLoadingTimeScalePause(bool restoreNormalTimeScaleOnHide)
    {
        if (!_timeScalePauseActive)
        {
            _timeScaleBeforeShow = Time.timeScale;
            _timeScalePauseActive = true;
        }

        _restoreNormalTimeScaleOnHide |= restoreNormalTimeScaleOnHide;
        Time.timeScale = 0f;
    }

    void RestoreLoadingTimeScalePause()
    {
        if (!_timeScalePauseActive)
            return;

        Time.timeScale = _restoreNormalTimeScaleOnHide ? 1f : _timeScaleBeforeShow;
        _restoreNormalTimeScaleOnHide = false;
        _timeScalePauseActive = false;
    }

    void EnsureCanvas()
    {
        if (_canvas)
            return;

        var canvasGo = new GameObject(
            "LoadingScreen_Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));

        DontDestroyOnLoad(canvasGo);

        _canvasRect = (RectTransform)canvasGo.transform;
        _canvasRect.anchorMin = Vector2.zero;
        _canvasRect.anchorMax = Vector2.one;
        _canvasRect.offsetMin = Vector2.zero;
        _canvasRect.offsetMax = Vector2.zero;

        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = CanvasSortingOrder;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        _canvasGroup = canvasGo.GetComponent<CanvasGroup>();
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = true;

        CreateSolidBackground();
        CreateBackgroundImage();
        CreateVignette();
        CreateLoadingIcon();

        canvasGo.SetActive(false);
    }

    void CreateSolidBackground()
    {
        var panelGo = new GameObject("LoadingScreen_SolidBlackBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGo.transform.SetParent(_canvasRect, false);

        var rect = (RectTransform)panelGo.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = panelGo.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;
    }

    void CreateBackgroundImage()
    {
        var imageGo = new GameObject("LoadingScreen_BackgroundImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageGo.transform.SetParent(_canvasRect, false);

        var rect = (RectTransform)imageGo.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _backgroundImage = imageGo.GetComponent<Image>();
        _backgroundImage.color = Color.clear;
        _backgroundImage.raycastTarget = false;
    }

    void CreateVignette()
    {
        var vignetteGo = new GameObject("LoadingScreen_Vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(LoadingScreenVignetteGraphic));
        vignetteGo.transform.SetParent(_canvasRect, false);

        var rect = (RectTransform)vignetteGo.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _vignetteGraphic = vignetteGo.GetComponent<LoadingScreenVignetteGraphic>();
        _vignetteGraphic.raycastTarget = false;
        RefreshVignetteGraphic();
    }

    void CreateLoadingIcon()
    {
        var iconPrefab = Settings ? Settings.loadingIconPrefab : null;
        if (!iconPrefab)
            return;

        _iconInstance = Instantiate(iconPrefab, _canvasRect);
        _iconInstance.name = iconPrefab.name;

        var rect = _iconInstance.transform as RectTransform;
        if (!rect)
            return;

        Vector2 size = rect.sizeDelta;
        if (size.sqrMagnitude <= 0.001f)
            size = new Vector2(300f, 100f);

        Vector2 margin = Settings ? Settings.iconMargin : new Vector2(48f, 36f);

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(-Mathf.Max(0f, margin.x), Mathf.Max(0f, margin.y));
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.SetAsLastSibling();

        CacheLoadingTexts();
    }

    void ApplyRandomBackground()
    {
        if (!_backgroundImage)
            return;

        var settings = Settings;
        Sprite selectedSprite = PickRandomBackgroundSprite(settings);

        _backgroundImage.sprite = selectedSprite;
        _backgroundImage.color = selectedSprite ? (settings ? settings.backgroundImageTint : Color.white) : Color.clear;
        _backgroundImage.type = Image.Type.Simple;
        _backgroundImage.preserveAspect = false;

        RefreshVignetteGraphic();
    }

    static Sprite PickRandomBackgroundSprite(LoadingScreenSettings settings)
    {
        if (!settings || settings.backgroundImages == null || settings.backgroundImages.Length == 0)
        {
            _backgroundImageBag.Clear();
            _backgroundBagSettings = null;
            _backgroundBagLength = -1;
            _lastBackgroundImageIndex = -1;
            return null;
        }

        if (_backgroundBagSettings != settings || _backgroundBagLength != settings.backgroundImages.Length)
        {
            _backgroundImageBag.Clear();
            _backgroundBagSettings = settings;
            _backgroundBagLength = settings.backgroundImages.Length;
            _lastBackgroundImageIndex = -1;
        }

        while (true)
        {
            if (_backgroundImageBag.Count == 0)
                RefillBackgroundImageBag(settings);

            if (_backgroundImageBag.Count == 0)
                return null;

            int last = _backgroundImageBag.Count - 1;
            int imageIndex = _backgroundImageBag[last];
            _backgroundImageBag.RemoveAt(last);

            Sprite sprite = imageIndex >= 0 && imageIndex < settings.backgroundImages.Length
                ? settings.backgroundImages[imageIndex]
                : null;

            if (!sprite)
                continue;

            _lastBackgroundImageIndex = imageIndex;
            return sprite;
        }
    }

    static void RefillBackgroundImageBag(LoadingScreenSettings settings)
    {
        _backgroundImageBag.Clear();

        if (!settings || settings.backgroundImages == null)
            return;

        for (int i = 0; i < settings.backgroundImages.Length; i++)
        {
            if (settings.backgroundImages[i])
                _backgroundImageBag.Add(i);
        }

        for (int i = _backgroundImageBag.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            int tmp = _backgroundImageBag[i];
            _backgroundImageBag[i] = _backgroundImageBag[swapIndex];
            _backgroundImageBag[swapIndex] = tmp;
        }

        int lastSlot = _backgroundImageBag.Count - 1;
        if (lastSlot > 0 && _backgroundImageBag[lastSlot] == _lastBackgroundImageIndex)
        {
            int swapIndex = UnityEngine.Random.Range(0, lastSlot);
            int tmp = _backgroundImageBag[lastSlot];
            _backgroundImageBag[lastSlot] = _backgroundImageBag[swapIndex];
            _backgroundImageBag[swapIndex] = tmp;
        }
    }

    void RefreshVignetteGraphic()
    {
        if (!_vignetteGraphic)
            return;

        var settings = Settings;
        bool enabled = !settings || settings.vignetteEnabled;
        _vignetteGraphic.enabled = enabled;
        if (!enabled)
            return;

        Color color = settings ? settings.vignetteColor : Color.black;
        float centerAlpha = Mathf.Clamp01(settings ? settings.vignetteCenterAlpha : 0f);
        float edgeAlpha = Mathf.Clamp01(settings ? settings.vignetteEdgeAlpha : 0.9f);
        float innerRadius = Mathf.Clamp01(settings ? settings.vignetteInnerRadius : 0.25f);
        float outerRadius = Mathf.Max(innerRadius + 0.001f, settings ? settings.vignetteOuterRadius : 1f);
        int subdivisions = Mathf.Clamp(settings ? settings.vignetteMeshSubdivisions : 32, 4, 96);

        _vignetteGraphic.Configure(color, centerAlpha, edgeAlpha, innerRadius, outerRadius, subdivisions);
    }

    void CacheLoadingTexts()
    {
        _loadingTexts = null;
        _loadingTextBaseColors = null;

        if (!_iconInstance)
            return;

        var allTexts = _iconInstance.GetComponentsInChildren<TMP_Text>(true);
        if (allTexts == null || allTexts.Length == 0)
            return;

        var namedLoadingTexts = new List<TMP_Text>();
        for (int i = 0; i < allTexts.Length; i++)
        {
            var text = allTexts[i];
            if (text && text.name == "Loading_Text")
                namedLoadingTexts.Add(text);
        }

        _loadingTexts = namedLoadingTexts.Count > 0 ? namedLoadingTexts.ToArray() : allTexts;
        _loadingTextBaseColors = new Color[_loadingTexts.Length];
        for (int i = 0; i < _loadingTexts.Length; i++)
            _loadingTextBaseColors[i] = _loadingTexts[i] ? _loadingTexts[i].color : Color.white;
    }

    void StartLoadingTextBlink()
    {
        StopLoadingTextBlink();

        if (_loadingTexts == null || _loadingTexts.Length == 0)
            return;

        SetLoadingTextAlpha(1f);
        _textBlinkCoroutine = StartCoroutine(LoadingTextBlinkRoutine());
    }

    void StopLoadingTextBlink()
    {
        if (_textBlinkCoroutine != null)
        {
            StopCoroutine(_textBlinkCoroutine);
            _textBlinkCoroutine = null;
        }

        SetLoadingTextAlpha(1f);
    }

    IEnumerator LoadingTextBlinkRoutine()
    {
        float minAlpha = Mathf.Clamp01(Settings ? Settings.loadingTextMinAlpha : 0.15f);
        float fadeSeconds = Mathf.Max(0.01f, Settings ? Settings.loadingTextFadeSeconds : 1.1f);

        while (true)
        {
            yield return FadeLoadingTextAlpha(1f, minAlpha, fadeSeconds);
            yield return FadeLoadingTextAlpha(minAlpha, 1f, fadeSeconds);
        }
    }

    IEnumerator FadeLoadingTextAlpha(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            SetLoadingTextAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
            yield return null;
        }

        SetLoadingTextAlpha(to);
    }

    void SetLoadingTextAlpha(float alpha)
    {
        if (_loadingTexts == null || _loadingTextBaseColors == null)
            return;

        for (int i = 0; i < _loadingTexts.Length; i++)
        {
            var text = _loadingTexts[i];
            if (!text)
                continue;

            Color color = i < _loadingTextBaseColors.Length ? _loadingTextBaseColors[i] : text.color;
            color.a = Mathf.Clamp01(alpha);
            text.color = color;
        }
    }
}

sealed class LoadingScreenVignetteGraphic : MaskableGraphic
{
    Color vignetteColor = Color.black;
    float centerAlpha;
    float edgeAlpha = 0.9f;
    float innerRadius = 0.25f;
    float outerRadius = 1f;
    int subdivisions = 32;

    public void Configure(Color color, float center, float edge, float inner, float outer, int meshSubdivisions)
    {
        vignetteColor = color;
        centerAlpha = Mathf.Clamp01(center);
        edgeAlpha = Mathf.Clamp01(edge);
        innerRadius = Mathf.Clamp01(inner);
        outerRadius = Mathf.Max(innerRadius + 0.001f, outer);
        subdivisions = Mathf.Clamp(meshSubdivisions, 4, 96);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        int steps = Mathf.Max(4, subdivisions);

        for (int y = 0; y <= steps; y++)
        {
            float v = y / (float)steps;
            float py = Mathf.Lerp(rect.yMin, rect.yMax, v);

            for (int x = 0; x <= steps; x++)
            {
                float u = x / (float)steps;
                float px = Mathf.Lerp(rect.xMin, rect.xMax, u);
                float nx = Mathf.Abs(u - 0.5f) * 2f;
                float ny = Mathf.Abs(v - 0.5f) * 2f;
                float radius = Mathf.Sqrt(nx * nx + ny * ny);
                float t = Mathf.InverseLerp(innerRadius, outerRadius, radius);
                t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

                Color vertexColor = vignetteColor;
                vertexColor.a = Mathf.Lerp(centerAlpha, edgeAlpha, t);
                vh.AddVert(new Vector3(px, py, 0f), vertexColor, new Vector2(u, v));
            }
        }

        int rowSize = steps + 1;
        for (int y = 0; y < steps; y++)
        {
            for (int x = 0; x < steps; x++)
            {
                int lowerLeft = y * rowSize + x;
                int lowerRight = lowerLeft + 1;
                int upperLeft = lowerLeft + rowSize;
                int upperRight = upperLeft + 1;

                vh.AddTriangle(lowerLeft, upperLeft, upperRight);
                vh.AddTriangle(lowerLeft, upperRight, lowerRight);
            }
        }
    }
}
