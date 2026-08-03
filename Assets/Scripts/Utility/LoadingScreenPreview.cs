using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class LoadingScreenPreview : MonoBehaviour
{
    const string SettingsResourcePath = "LoadingScreenSettings";

    [SerializeField] LoadingScreenSettings settings;
    [SerializeField, Min(0)] int backgroundIndex;
    [SerializeField] bool showLoadingIcon = true;
    [SerializeField] bool keepPreviewOnTop = true;

    RectTransform canvasRect;
    Image backgroundImage;
    LoadingScreenVignetteGraphic vignetteGraphic;
    GameObject iconInstance;
    GameObject currentIconPrefab;

    void OnEnable()
    {
        EnsureSettings();
        EnsurePreview();
        ApplySettings();
    }

    void OnValidate()
    {
        EnsureSettings();
        EnsurePreview();
        ApplySettings();
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            EnsureSettings();
            EnsurePreview();
            ApplySettings();
        }
    }

    void EnsureSettings()
    {
        if (!settings)
            settings = Resources.Load<LoadingScreenSettings>(SettingsResourcePath);
    }

    void EnsurePreview()
    {
        var canvas = GetComponentInChildren<Canvas>(true);
        if (!canvas)
        {
            var canvasGo = new GameObject(
                "LoadingScreenPreview_Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        if (keepPreviewOnTop)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 20000;
        }

        canvasRect = canvas.transform as RectTransform;
        StretchToParent(canvasRect);

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }

        EnsureSolidBackground();
        EnsureBackgroundImage();
        EnsureVignette();
        EnsureIcon();
    }

    void EnsureSolidBackground()
    {
        var rect = FindChildRect("SolidBlackBackground");
        if (!rect)
        {
            var go = new GameObject("SolidBlackBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvasRect, false);
            rect = go.transform as RectTransform;
        }

        StretchToParent(rect);

        var image = rect.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
        rect.SetSiblingIndex(0);
    }

    void EnsureBackgroundImage()
    {
        var rect = FindChildRect("RandomBackgroundImage");
        if (!rect)
        {
            var go = new GameObject("RandomBackgroundImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvasRect, false);
            rect = go.transform as RectTransform;
        }

        StretchToParent(rect);
        backgroundImage = rect.GetComponent<Image>();
        backgroundImage.raycastTarget = false;
        rect.SetSiblingIndex(1);
    }

    void EnsureVignette()
    {
        var rect = FindChildRect("Vignette");
        if (!rect)
        {
            var go = new GameObject("Vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(LoadingScreenVignetteGraphic));
            go.transform.SetParent(canvasRect, false);
            rect = go.transform as RectTransform;
        }

        StretchToParent(rect);
        vignetteGraphic = rect.GetComponent<LoadingScreenVignetteGraphic>();
        if (!vignetteGraphic)
            vignetteGraphic = rect.gameObject.AddComponent<LoadingScreenVignetteGraphic>();

        vignetteGraphic.raycastTarget = false;
        rect.SetSiblingIndex(2);
    }

    void EnsureIcon()
    {
        var iconPrefab = settings ? settings.loadingIconPrefab : null;
        if (!showLoadingIcon || !iconPrefab)
        {
            DestroyIconIfNeeded();
            return;
        }

        if (iconInstance && currentIconPrefab == iconPrefab)
            return;

        DestroyIconIfNeeded();

#if UNITY_EDITOR
        if (!Application.isPlaying)
            iconInstance = PrefabUtility.InstantiatePrefab(iconPrefab, canvasRect) as GameObject;
        else
#endif
            iconInstance = Instantiate(iconPrefab, canvasRect);

        currentIconPrefab = iconPrefab;
        iconInstance.name = "LoadingIcon_Preview";
    }

    void ApplySettings()
    {
        if (!settings)
            return;

        ApplyBackground();
        ApplyVignette();
        ApplyIconLayout();
    }

    void ApplyBackground()
    {
        if (!backgroundImage)
            return;

        Sprite sprite = null;
        if (settings.backgroundImages != null && settings.backgroundImages.Length > 0)
        {
            int index = Mathf.Clamp(backgroundIndex, 0, settings.backgroundImages.Length - 1);
            sprite = settings.backgroundImages[index];
        }

        backgroundImage.sprite = sprite;
        backgroundImage.color = sprite ? settings.backgroundImageTint : Color.clear;
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.preserveAspect = false;
    }

    void ApplyVignette()
    {
        if (!vignetteGraphic)
            return;

        vignetteGraphic.enabled = settings.vignetteEnabled;
        if (!settings.vignetteEnabled)
            return;

        vignetteGraphic.Configure(
            settings.vignetteColor,
            settings.vignetteCenterAlpha,
            settings.vignetteEdgeAlpha,
            settings.vignetteInnerRadius,
            Mathf.Max(settings.vignetteInnerRadius + 0.001f, settings.vignetteOuterRadius),
            settings.vignetteMeshSubdivisions);
    }

    void ApplyIconLayout()
    {
        if (!iconInstance)
            return;

        var rect = iconInstance.transform as RectTransform;
        if (!rect)
            return;

        Vector2 size = rect.sizeDelta;
        if (size.sqrMagnitude <= 0.001f)
            size = new Vector2(300f, 100f);

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(-Mathf.Max(0f, settings.iconMargin.x), Mathf.Max(0f, settings.iconMargin.y));
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.SetAsLastSibling();
    }

    RectTransform FindChildRect(string childName)
    {
        if (!canvasRect)
            return null;

        var child = canvasRect.Find(childName);
        return child as RectTransform;
    }

    static void StretchToParent(RectTransform rect)
    {
        if (!rect)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    void DestroyIconIfNeeded()
    {
        if (!iconInstance)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(iconInstance);
        else
#endif
            Destroy(iconInstance);

        iconInstance = null;
        currentIconPrefab = null;
    }
}
