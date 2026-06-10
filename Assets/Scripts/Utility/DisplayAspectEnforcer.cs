using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-10000)]
public sealed class DisplayAspectEnforcer : MonoBehaviour
{
    const int TargetWidth = 1920;
    const int TargetHeight = 1080;
    const float TargetAspect = (float)TargetWidth / TargetHeight;
    const int BarSortingOrder = 32767;
    const float AspectTolerance = 0.001f;
    const float CameraRefreshSeconds = 0.5f;

    static DisplayAspectEnforcer _instance;
    static bool _startupResolutionRequested;

    Canvas _barCanvas;
    RectTransform _leftBar;
    RectTransform _rightBar;
    RectTransform _topBar;
    RectTransform _bottomBar;
    int _lastScreenWidth;
    int _lastScreenHeight;
    float _nextCameraRefreshTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Application.isPlaying)
            Ensure();
    }

    public static DisplayAspectEnforcer Ensure()
    {
        if (_instance)
            return _instance;

        var existing = FindFirstObjectByType<DisplayAspectEnforcer>(FindObjectsInactive.Include);
        if (existing)
        {
            _instance = existing;
            if (!_instance.gameObject.activeSelf)
                _instance.gameObject.SetActive(true);

            return _instance;
        }

        var go = new GameObject("DisplayAspectEnforcer");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<DisplayAspectEnforcer>();
        return _instance;
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
        RequestStartupResolution();
        EnsureBarCanvas();
        ApplyLayout(forceCameraRefresh: true);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void LateUpdate()
    {
        bool resolutionChanged =
            Screen.width != _lastScreenWidth ||
            Screen.height != _lastScreenHeight;

        bool refreshCameras = resolutionChanged || Time.unscaledTime >= _nextCameraRefreshTime;
        if (resolutionChanged || refreshCameras)
            ApplyLayout(refreshCameras);
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyLayout(forceCameraRefresh: true);
    }

    static void RequestStartupResolution()
    {
        if (_startupResolutionRequested || Application.isEditor)
            return;

        _startupResolutionRequested = true;
        Screen.SetResolution(TargetWidth, TargetHeight, FullScreenMode.FullScreenWindow);
    }

    void ApplyLayout(bool forceCameraRefresh)
    {
        _lastScreenWidth = Mathf.Max(1, Screen.width);
        _lastScreenHeight = Mathf.Max(1, Screen.height);

        Rect pixelFrame = CalculatePixelFrame(_lastScreenWidth, _lastScreenHeight);
        UpdateBars(pixelFrame, _lastScreenWidth, _lastScreenHeight);

        if (forceCameraRefresh)
        {
            ApplyCameraViewport(pixelFrame, _lastScreenWidth, _lastScreenHeight);
            _nextCameraRefreshTime = Time.unscaledTime + CameraRefreshSeconds;
        }
    }

    static Rect CalculatePixelFrame(int screenWidth, int screenHeight)
    {
        float screenAspect = (float)screenWidth / screenHeight;
        if (Mathf.Abs(screenAspect - TargetAspect) <= AspectTolerance)
            return new Rect(0f, 0f, screenWidth, screenHeight);

        if (screenAspect > TargetAspect)
        {
            float frameWidth = screenHeight * TargetAspect;
            float frameX = (screenWidth - frameWidth) * 0.5f;
            return new Rect(frameX, 0f, frameWidth, screenHeight);
        }

        float frameHeight = screenWidth / TargetAspect;
        float frameY = (screenHeight - frameHeight) * 0.5f;
        return new Rect(0f, frameY, screenWidth, frameHeight);
    }

    void ApplyCameraViewport(Rect pixelFrame, int screenWidth, int screenHeight)
    {
        var viewport = new Rect(
            pixelFrame.x / screenWidth,
            pixelFrame.y / screenHeight,
            pixelFrame.width / screenWidth,
            pixelFrame.height / screenHeight);

        foreach (var camera in Camera.allCameras)
        {
            if (!camera || camera.targetTexture)
                continue;

            camera.rect = viewport;
            if (camera.clearFlags == CameraClearFlags.Skybox ||
                camera.clearFlags == CameraClearFlags.SolidColor)
            {
                camera.backgroundColor = Color.black;
            }
        }
    }

    void EnsureBarCanvas()
    {
        if (_barCanvas)
            return;

        var canvasGo = new GameObject(
            "DisplayAspectBars_Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        DontDestroyOnLoad(canvasGo);

        var rect = (RectTransform)canvasGo.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _barCanvas = canvasGo.GetComponent<Canvas>();
        _barCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _barCanvas.overrideSorting = true;
        _barCanvas.sortingOrder = BarSortingOrder;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;

        _leftBar = CreateBar("LeftBar", rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        _rightBar = CreateBar("RightBar", rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        _topBar = CreateBar("TopBar", rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        _bottomBar = CreateBar("BottomBar", rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
    }

    static RectTransform CreateBar(string name, Transform parent, Vector2 anchor, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;

        return rect;
    }

    void UpdateBars(Rect pixelFrame, int screenWidth, int screenHeight)
    {
        EnsureBarCanvas();

        float leftWidth = Mathf.Round(Mathf.Max(0f, pixelFrame.xMin));
        float rightWidth = Mathf.Round(Mathf.Max(0f, screenWidth - pixelFrame.xMax));
        float topHeight = Mathf.Round(Mathf.Max(0f, screenHeight - pixelFrame.yMax));
        float bottomHeight = Mathf.Round(Mathf.Max(0f, pixelFrame.yMin));

        SetBar(_leftBar, leftWidth, screenHeight);
        SetBar(_rightBar, rightWidth, screenHeight);
        SetBar(_topBar, screenWidth, topHeight);
        SetBar(_bottomBar, screenWidth, bottomHeight);
    }

    static void SetBar(RectTransform bar, float width, float height)
    {
        if (!bar)
            return;

        bool active = width > 0.5f && height > 0.5f;
        if (bar.gameObject.activeSelf != active)
            bar.gameObject.SetActive(active);

        if (active)
            bar.sizeDelta = new Vector2(width, height);
    }
}
