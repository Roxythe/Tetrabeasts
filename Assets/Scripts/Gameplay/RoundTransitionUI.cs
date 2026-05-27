using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum RoundTransitionVariant
{
    Default,
    Win,
    Loss
}

public class RoundTransitionUI : MonoBehaviour
{
    const int RuntimeSortingOrder = 30000;

    [Header("Root")]
    [SerializeField] GameObject rootPanel;
    [SerializeField] Image backgroundImage;
    [SerializeField] TMP_Text messageText;
    [SerializeField] Button continueButton;
    [SerializeField] CanvasGroup continueButtonGroup;
    [SerializeField] Toggle optOutToggle;
    [SerializeField] TMP_Text optOutToggleLabel;
    [SerializeField] CanvasGroup optOutToggleGroup;
    [SerializeField] RectTransform contentRoot;
    [SerializeField] RectTransform actionRoot;

    [Header("Optional Assets")]
    [SerializeField] TMP_FontAsset transitionFont;
    [SerializeField] Button continueButtonPrefab;

    [Header("Animation")]
    [SerializeField] Animator transitionAnimator;
    [SerializeField] GameObject fireworkAnimationRoot;
    [SerializeField] bool useUnscaledAnimationTime = true;
    [SerializeField] string showAnimationStateName;
    [SerializeField] string showAnimationTrigger = "Show";
    [SerializeField] string hideAnimationTrigger;

    [Header("Audio")]
    [SerializeField] AudioClip appearSfxClip;
    [SerializeField, Range(0f, 1f)] float appearSfxVolume = 1f;
    [SerializeField] AudioClip activeLoopSfxClip;
    [SerializeField, Range(0f, 1f)] float activeLoopSfxVolume = 1f;

    [Header("Loss Animation")]
    [SerializeField] Animator lossTransitionAnimator;
    [SerializeField] GameObject lossAnimationRoot;
    [SerializeField] AnimationClip lossShowAnimationClip;
    [SerializeField] string lossShowAnimationStateName;
    [SerializeField] string lossShowAnimationTrigger;
    [SerializeField] string lossHideAnimationTrigger;

    [Header("Loss Audio")]
    [SerializeField] AudioClip lossAppearSfxClip;
    [SerializeField, Range(0f, 1f)] float lossAppearSfxVolume = 1f;
    [SerializeField] AudioClip lossActiveLoopSfxClip;
    [SerializeField, Range(0f, 1f)] float lossActiveLoopSfxVolume = 1f;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] float fadeInSeconds = 1.85f;
    [SerializeField, Min(0f)] float autoHoldSeconds = 3f;
    [SerializeField, Min(0.01f)] float autoFadeOutSeconds = 1.35f;
    [SerializeField, Range(0f, 1f)] float backgroundAlpha = 0.72f;
    [SerializeField, Min(0f)] float circularRevealSeconds = 0.24f;
    [SerializeField, Min(1f)] float circularRevealStartDiameter = 48f;
    [SerializeField] RectTransform revealMaskRoot;
    [SerializeField] Image revealMaskImage;
    [SerializeField] Mask revealMask;
    [SerializeField] Image revealBackgroundImage;

    Action _onContinue;
    Action<bool> _onOptOutContinue;
    Coroutine _fadeRoutine;
    ScopedMenuNavigator _navigator;
    AudioClip _currentActiveLoopSfxClip;
    RoundTransitionVariant _currentVariant = RoundTransitionVariant.Default;
    bool _builtRuntimeUi;
    bool _continueButtonUsesPrefab;
    const float RuntimeActionGap = 22f;
    static Sprite s_revealCircleSprite;

    public bool IsShowing => rootPanel && rootPanel.activeSelf;

    public static RoundTransitionUI CreateRuntimeInstance(TMP_FontAsset font = null, Button buttonPrefab = null)
    {
        var canvasGo = new GameObject(
            "RoundTransitionUI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = RuntimeSortingOrder;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var ui = canvasGo.AddComponent<RoundTransitionUI>();
        ui.Configure(font, buttonPrefab);
        ui.EnsureBuilt();
        ui.HideImmediate();
        return ui;
    }

    void Awake()
    {
        EnsureBuilt();
        HideImmediate();
    }

    public void Show(string message, Action onContinue)
    {
        Show(message, onContinue, string.Empty, false, null);
    }

    public void Show(string message, Action onContinue, RoundTransitionVariant variant)
    {
        Show(message, onContinue, string.Empty, false, null, variant);
    }

    public void ShowTimed(string message, Action onComplete)
    {
        ShowTimed(message, autoHoldSeconds, autoFadeOutSeconds, onComplete);
    }

    public void ShowTimed(string message, float holdSeconds, float fadeOutSeconds, Action onComplete)
    {
        EnsureBuilt();

        if (!rootPanel)
        {
            onComplete?.Invoke();
            return;
        }

        ApplyFont();
        _onContinue = null;
        _onOptOutContinue = null;

        gameObject.SetActive(true);
        rootPanel.SetActive(true);
        rootPanel.transform.SetAsLastSibling();
        _currentVariant = RoundTransitionVariant.Default;
        SetVariantAnimationVisible(false);
        SetDefaultTransitionAnimatorEnabled(false);

        var rootGroup = rootPanel.GetComponent<CanvasGroup>();
        if (rootGroup)
        {
            rootGroup.alpha = 1f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = true;
        }

        SetBackgroundAlpha(0f);

        if (messageText)
        {
            messageText.text = TetrabeastsLocalization.LocalizeText(message);
            SetTextAlpha(0f);
        }

        PrepareCircularReveal();
        SetActionControlsVisible(false);
        ConfigureOptOutToggle(string.Empty, false);

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(CoTimedTransition(
            Mathf.Max(0f, holdSeconds),
            Mathf.Max(0.01f, fadeOutSeconds),
            onComplete));
    }

    public void Show(string message, Action onContinue, string optOutLabel, bool optOutInitialValue,
                     Action<bool> onOptOutContinue, RoundTransitionVariant variant = RoundTransitionVariant.Default)
    {
        EnsureBuilt();

        if (!rootPanel)
        {
            onContinue?.Invoke();
            return;
        }

        ApplyFont();
        _currentVariant = variant;
        _onContinue = onContinue;
        _onOptOutContinue = onOptOutContinue;
        ConfigureOptOutToggle(optOutLabel, optOutInitialValue);

        gameObject.SetActive(true);
        rootPanel.SetActive(true);
        rootPanel.transform.SetAsLastSibling();
        SetVariantAnimationVisible(true);
        SetDefaultTransitionAnimatorEnabled(true);
        PlayShowAnimation();
        PlayShowAudio();

        var rootGroup = rootPanel.GetComponent<CanvasGroup>();
        if (rootGroup)
        {
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
        }

        SetCircularRevealProgress(1f);
        SetBackgroundAlpha(backgroundAlpha);

        if (messageText)
        {
            messageText.text = TetrabeastsLocalization.LocalizeText(message);
            SetTextAlpha(0f);
        }

        if (continueButtonGroup)
        {
            continueButtonGroup.alpha = 0f;
            continueButtonGroup.blocksRaycasts = false;
            continueButtonGroup.interactable = false;
        }

        if (continueButton)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.interactable = false;
        }

        if (optOutToggleGroup && optOutToggle && optOutToggle.gameObject.activeSelf)
        {
            optOutToggleGroup.alpha = 0f;
            optOutToggleGroup.blocksRaycasts = false;
            optOutToggleGroup.interactable = false;
            optOutToggle.interactable = false;
        }

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(CoFadeIn());
    }

    public void HideImmediate()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        _onContinue = null;
        _onOptOutContinue = null;
        StopActiveLoopSfx();
        PlayHideAnimation();

        if (continueButton)
        {
            continueButton.interactable = false;
        }

        if (optOutToggle)
            optOutToggle.interactable = false;

        DisableNavigation();

        if (rootPanel)
        {
            var rootGroup = rootPanel.GetComponent<CanvasGroup>();
            if (rootGroup)
            {
                rootGroup.interactable = false;
                rootGroup.blocksRaycasts = false;
            }

            rootPanel.SetActive(false);
        }
    }

    [ContextMenu("Build Missing UI For Inspector")]
    public void BuildMissingUiForInspector()
    {
        EnsureBuilt();
        HideImmediate();
    }

    void OnDisable()
    {
        StopActiveLoopSfx();
        DisableNavigation();
    }

    System.Collections.IEnumerator CoFadeIn()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeInSeconds);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.SmoothStep(0f, 1f, t);

            SetTextAlpha(alpha);

            yield return null;
        }

        SetCircularRevealProgress(1f);
        SetBackgroundAlpha(backgroundAlpha);
        SetTextAlpha(1f);

        if (continueButton)
            continueButton.gameObject.SetActive(true);

        if (continueButtonGroup)
        {
            continueButtonGroup.alpha = 1f;
            continueButtonGroup.blocksRaycasts = true;
            continueButtonGroup.interactable = true;
        }

        if (continueButton)
            continueButton.interactable = true;

        if (optOutToggleGroup && optOutToggle && optOutToggle.gameObject.activeSelf)
        {
            optOutToggleGroup.alpha = 1f;
            optOutToggleGroup.blocksRaycasts = true;
            optOutToggleGroup.interactable = true;
            optOutToggle.interactable = true;
        }

        EnableNavigation();
        _fadeRoutine = null;
    }

    System.Collections.IEnumerator CoTimedTransition(float holdSeconds, float fadeOutSeconds, Action onComplete)
    {
        float elapsed = 0f;
        float inDuration = GetCircularRevealDuration();

        while (elapsed < inDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = GetCircularRevealProgress(elapsed);

            SetCircularRevealProgress(alpha);
            SetTextAlpha(alpha);
            SetBackgroundAlpha(backgroundAlpha * alpha);

            yield return null;
        }

        SetCircularRevealProgress(1f);
        SetTextAlpha(1f);
        SetBackgroundAlpha(backgroundAlpha);

        elapsed = 0f;
        while (elapsed < holdSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < fadeOutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutSeconds);
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, t);

            SetTextAlpha(alpha);
            SetBackgroundAlpha(backgroundAlpha * alpha);

            yield return null;
        }

        SetTextAlpha(0f);
        SetBackgroundAlpha(0f);
        _fadeRoutine = null;

        HideAfterTimedTransition();
        onComplete?.Invoke();
    }

    void Continue()
    {
        var callback = _onContinue;
        var optOutCallback = _onOptOutContinue;
        bool optOut = optOutToggle && optOutToggle.gameObject.activeSelf && optOutToggle.isOn;

        optOutCallback?.Invoke(optOut);
        HideImmediate();
        callback?.Invoke();
    }

    public void Configure(TMP_FontAsset font, Button buttonPrefab)
    {
        if (font)
            transitionFont = font;

        bool buttonPrefabChanged = buttonPrefab && buttonPrefab != continueButtonPrefab;
        if (buttonPrefabChanged)
            continueButtonPrefab = buttonPrefab;

        if (_builtRuntimeUi)
        {
            if (buttonPrefabChanged && contentRoot)
                RebuildContinueButton();

            ApplyFont();
            WireContinueButton();
            WireOptOutToggle();
        }
    }

    void EnsureBuilt()
    {
        if (_builtRuntimeUi)
            return;

        if (!rootPanel)
            BuildRuntimeUi();

        ResolveAnimator();
        EnsureRevealMask();
        ApplyFont();
        WireContinueButton();
        HookContinueButtonSfx();
        WireOptOutToggle();

        _builtRuntimeUi = true;
    }

    void BuildRuntimeUi()
    {
        var rootGo = new GameObject(
            "RoundTransition_Panel",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image));

        rootGo.transform.SetParent(transform, false);
        rootPanel = rootGo;

        var rootRect = rootGo.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        backgroundImage = rootGo.GetComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, backgroundAlpha);
        backgroundImage.raycastTarget = true;

        var contentGo = new GameObject(
            "Content",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup));

        contentGo.transform.SetParent(rootGo.transform, false);

        contentRoot = contentGo.GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
        contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        contentRoot.pivot = new Vector2(0.5f, 0.5f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(1120f, 360f);

        var layout = contentGo.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 24f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var messageGo = new GameObject("RoundTransition_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        messageGo.transform.SetParent(contentGo.transform, false);

        var messageRect = messageGo.GetComponent<RectTransform>();
        messageRect.sizeDelta = new Vector2(920f, 230f);

        messageText = messageGo.GetComponent<TextMeshProUGUI>();
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.enableAutoSizing = true;
        messageText.fontSizeMin = 32f;
        messageText.fontSizeMax = 66f;
        messageText.fontStyle = FontStyles.Bold;
        messageText.raycastTarget = false;
        messageText.color = Color.white;

        var actionGo = new GameObject(
            "RoundTransition_ActionRow",
            typeof(RectTransform));

        actionGo.transform.SetParent(contentGo.transform, false);
        actionRoot = actionGo.GetComponent<RectTransform>();
        actionRoot.sizeDelta = new Vector2(1120f, 82f);

        BuildContinueButton(actionGo.transform);
        BuildOptOutToggle(actionGo.transform);
    }

    void EnsureRevealMask()
    {
        if (!rootPanel || !contentRoot)
            return;

        if (!revealMaskRoot)
        {
            var maskGo = new GameObject(
                "RoundTransition_RevealMask",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask));

            maskGo.transform.SetParent(rootPanel.transform, false);
            revealMaskRoot = maskGo.GetComponent<RectTransform>();
            revealMaskRoot.anchorMin = new Vector2(0.5f, 0.5f);
            revealMaskRoot.anchorMax = new Vector2(0.5f, 0.5f);
            revealMaskRoot.pivot = new Vector2(0.5f, 0.5f);
            revealMaskRoot.anchoredPosition = Vector2.zero;
            revealMaskRoot.sizeDelta = Vector2.one * GetCircularRevealEndDiameter();

            revealMaskImage = maskGo.GetComponent<Image>();
            revealMask = maskGo.GetComponent<Mask>();
        }

        if (!revealMaskImage)
            revealMaskImage = revealMaskRoot.GetComponent<Image>();

        if (revealMaskImage)
        {
            revealMaskImage.sprite = GetRevealCircleSprite();
            revealMaskImage.type = Image.Type.Simple;
            revealMaskImage.preserveAspect = true;
            revealMaskImage.color = Color.white;
            revealMaskImage.raycastTarget = false;
        }

        if (!revealMask)
            revealMask = revealMaskRoot.GetComponent<Mask>();

        if (revealMask)
            revealMask.showMaskGraphic = false;

        if (!revealBackgroundImage)
        {
            var backgroundGo = new GameObject(
                "RoundTransition_RevealBackground",
                typeof(RectTransform),
                typeof(Image));

            backgroundGo.transform.SetParent(revealMaskRoot, false);
            var backgroundRect = backgroundGo.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            revealBackgroundImage = backgroundGo.GetComponent<Image>();
            revealBackgroundImage.color = new Color(0f, 0f, 0f, 0f);
            revealBackgroundImage.raycastTarget = false;
        }

        if (revealBackgroundImage)
            revealBackgroundImage.transform.SetAsFirstSibling();

        if (contentRoot.parent != revealMaskRoot)
        {
            contentRoot.SetParent(revealMaskRoot, false);
            contentRoot.SetAsLastSibling();
        }
        else
        {
            contentRoot.SetAsLastSibling();
        }
    }

    void PrepareCircularReveal()
    {
        EnsureRevealMask();
        SetCircularRevealProgress(0f);
    }

    void SetCircularRevealProgress(float progress)
    {
        if (!revealMaskRoot)
            return;

        float t = Mathf.Clamp01(progress);
        float startDiameter = Mathf.Max(1f, circularRevealStartDiameter);
        float endDiameter = Mathf.Max(startDiameter, GetCircularRevealEndDiameter());
        float diameter = Mathf.Lerp(startDiameter, endDiameter, t);
        revealMaskRoot.sizeDelta = Vector2.one * diameter;
    }

    float GetCircularRevealProgress(float elapsed)
    {
        if (circularRevealSeconds <= 0f)
            return 1f;

        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / circularRevealSeconds));
    }

    float GetCircularRevealDuration()
    {
        return Mathf.Max(0.01f, circularRevealSeconds);
    }

    float GetCircularRevealEndDiameter()
    {
        Vector2 size = Vector2.zero;
        var rootRect = rootPanel ? rootPanel.transform as RectTransform : null;
        if (rootRect)
            size = rootRect.rect.size;

        if (size.x <= 1f || size.y <= 1f)
            size = new Vector2(Mathf.Max(1f, Screen.width), Mathf.Max(1f, Screen.height));

        return Mathf.Sqrt(size.x * size.x + size.y * size.y) * 1.1f;
    }

    static Sprite GetRevealCircleSprite()
    {
        if (s_revealCircleSprite)
            return s_revealCircleSprite;

        const int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "RoundTransition_RevealCircle";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        float radius = center;
        float feather = 1.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((radius - distance) / feather);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, true);
        s_revealCircleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f);
        s_revealCircleSprite.hideFlags = HideFlags.HideAndDontSave;
        return s_revealCircleSprite;
    }

    void RebuildContinueButton()
    {
        if (continueButton)
            Destroy(continueButton.gameObject);

        continueButton = null;
        continueButtonGroup = null;

        BuildContinueButton(actionRoot ? actionRoot : contentRoot);
    }

    void BuildContinueButton(Transform parent)
    {
        if (continueButtonPrefab)
        {
            _continueButtonUsesPrefab = true;

            continueButton = Instantiate(continueButtonPrefab, parent);
            continueButton.name = "Continue_Button";

            var rect = continueButton.transform as RectTransform;
            if (rect)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;

                if (rect.sizeDelta.x <= 0f || rect.sizeDelta.y <= 0f)
                    rect.sizeDelta = new Vector2(310f, 76f);
            }

            continueButtonGroup = continueButton.GetComponent<CanvasGroup>();
            if (!continueButtonGroup)
                continueButtonGroup = continueButton.gameObject.AddComponent<CanvasGroup>();

            ApplyContinueLabel();
            WireContinueButton();
            HookContinueButtonSfx();
            continueButton.transform.SetAsFirstSibling();
            PositionActionControls();
            return;
        }

        _continueButtonUsesPrefab = false;

        var buttonGo = new GameObject(
            "Continue_Button",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image),
            typeof(Button));

        buttonGo.transform.SetParent(parent, false);

        var buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(310f, 76f);

        continueButtonGroup = buttonGo.GetComponent<CanvasGroup>();

        var buttonImage = buttonGo.GetComponent<Image>();
        buttonImage.color = Color.white;

        continueButton = buttonGo.GetComponent<Button>();
        continueButton.targetGraphic = buttonImage;

        var colors = continueButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        continueButton.colors = colors;

        var labelGo = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(buttonGo.transform, false);

        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var labelText = labelGo.GetComponent<TextMeshProUGUI>();
        labelText.text = "Continue";
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = 30f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.raycastTarget = false;
        labelText.color = new Color(0.196f, 0.196f, 0.196f, 1f);

        ApplyContinueLabel();
        WireContinueButton();
        HookContinueButtonSfx();
        continueButton.transform.SetAsFirstSibling();
        PositionActionControls();
    }

    void BuildOptOutToggle(Transform parent)
    {
        if (!parent)
            return;

        var toggleGo = new GameObject(
            "DoNotShowSurvivalMessage_Toggle",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image),
            typeof(Toggle),
            typeof(HorizontalLayoutGroup));

        toggleGo.transform.SetParent(parent, false);

        var toggleRect = toggleGo.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.5f, 0.5f);
        toggleRect.anchorMax = new Vector2(0.5f, 0.5f);
        toggleRect.pivot = new Vector2(0.5f, 0.5f);
        toggleRect.anchoredPosition = Vector2.zero;
        toggleRect.sizeDelta = new Vector2(420f, 60f);

        var toggleHitArea = toggleGo.GetComponent<Image>();
        toggleHitArea.color = new Color(1f, 1f, 1f, 0f);
        toggleHitArea.raycastTarget = true;

        optOutToggleGroup = toggleGo.GetComponent<CanvasGroup>();

        var layout = toggleGo.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 12f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var boxGo = new GameObject("Box", typeof(RectTransform), typeof(Image));
        boxGo.transform.SetParent(toggleGo.transform, false);

        var boxRect = boxGo.GetComponent<RectTransform>();
        boxRect.sizeDelta = new Vector2(32f, 32f);

        var boxImage = boxGo.GetComponent<Image>();
        boxImage.color = new Color(1f, 1f, 1f, 0.92f);

        var checkGo = new GameObject("Check", typeof(RectTransform), typeof(Image));
        checkGo.transform.SetParent(boxGo.transform, false);

        var checkRect = checkGo.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.anchoredPosition = Vector2.zero;
        checkRect.sizeDelta = new Vector2(20f, 20f);

        var checkImage = checkGo.GetComponent<Image>();
        checkImage.color = new Color(0.10f, 0.10f, 0.10f, 1f);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(toggleGo.transform, false);

        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(370f, 54f);

        optOutToggleLabel = labelGo.GetComponent<TextMeshProUGUI>();
        optOutToggleLabel.text = "Do not show this message again";
        optOutToggleLabel.alignment = TextAlignmentOptions.MidlineLeft;
        optOutToggleLabel.fontSize = 26f;
        optOutToggleLabel.enableAutoSizing = true;
        optOutToggleLabel.fontSizeMin = 18f;
        optOutToggleLabel.fontSizeMax = 26f;
        optOutToggleLabel.color = Color.white;
        optOutToggleLabel.raycastTarget = false;

        optOutToggle = toggleGo.GetComponent<Toggle>();
        optOutToggle.targetGraphic = boxImage;
        optOutToggle.graphic = checkImage;
        optOutToggle.isOn = false;

        WireOptOutToggle();
        PositionActionControls();
        toggleGo.SetActive(false);
    }

    void SetTextAlpha(float alpha)
    {
        if (!messageText)
            return;

        var color = messageText.color;
        color.a = alpha;
        messageText.color = color;
    }

    void SetBackgroundAlpha(float alpha)
    {
        float clamped = Mathf.Clamp01(alpha);

        if (backgroundImage)
        {
            var color = backgroundImage.color;
            color.a = revealBackgroundImage ? 0f : clamped;
            backgroundImage.color = color;
        }

        if (revealBackgroundImage)
        {
            var color = revealBackgroundImage.color;
            color.r = 0f;
            color.g = 0f;
            color.b = 0f;
            color.a = clamped;
            revealBackgroundImage.color = color;
        }
    }

    void SetActionControlsVisible(bool visible)
    {
        if (continueButtonGroup)
        {
            continueButtonGroup.alpha = visible ? 1f : 0f;
            continueButtonGroup.blocksRaycasts = visible;
            continueButtonGroup.interactable = visible;
        }

        if (continueButton)
        {
            continueButton.gameObject.SetActive(visible);
            continueButton.interactable = visible;
        }

        if (optOutToggleGroup)
        {
            optOutToggleGroup.alpha = visible ? 1f : 0f;
            optOutToggleGroup.blocksRaycasts = visible;
            optOutToggleGroup.interactable = visible;
        }

        if (optOutToggle)
        {
            optOutToggle.gameObject.SetActive(visible && optOutToggle.gameObject.activeSelf);
            optOutToggle.interactable = visible;
        }
    }

    void HideAfterTimedTransition()
    {
        _onContinue = null;
        _onOptOutContinue = null;
        SetVariantAnimationVisible(false);

        if (continueButton)
            continueButton.interactable = false;

        if (optOutToggle)
            optOutToggle.interactable = false;

        DisableNavigation();

        if (rootPanel)
        {
            var rootGroup = rootPanel.GetComponent<CanvasGroup>();
            if (rootGroup)
            {
                rootGroup.interactable = false;
                rootGroup.blocksRaycasts = false;
            }

            rootPanel.SetActive(false);
        }
    }

    void ApplyFont()
    {
        if (!transitionFont)
            return;

        if (messageText)
            messageText.font = transitionFont;

        if (continueButton)
        {
            var labels = continueButton.GetComponentsInChildren<TMP_Text>(true);
            if (!_continueButtonUsesPrefab)
            {
                for (int i = 0; i < labels.Length; i++)
                    labels[i].font = transitionFont;
            }
        }

        if (optOutToggleLabel)
            optOutToggleLabel.font = transitionFont;
    }

    void ApplyContinueLabel()
    {
        if (!continueButton)
            return;

        var labels = continueButton.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i].text = TetrabeastsLocalization.LocalizeText("Continue");
            labels[i].alignment = TextAlignmentOptions.Center;
        }

        ApplyFont();
    }

    void WireContinueButton()
    {
        if (!continueButton)
            return;

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(Continue);
    }

    void WireOptOutToggle()
    {
        if (!optOutToggle)
            return;

        optOutToggle.onValueChanged.RemoveAllListeners();
    }

    void ConfigureOptOutToggle(string label, bool initialValue)
    {
        bool show = !string.IsNullOrWhiteSpace(label);

        if (show && !optOutToggle)
            BuildOptOutToggle(continueButton ? continueButton.transform.parent : contentRoot);

        if (!optOutToggle)
            return;

        optOutToggle.gameObject.SetActive(show);
        optOutToggle.isOn = initialValue;

        if (optOutToggleLabel)
            optOutToggleLabel.text = show ? TetrabeastsLocalization.LocalizeText(label) : string.Empty;

        PositionActionControls();

        optOutToggle.interactable = false;

        if (optOutToggleGroup)
        {
            optOutToggleGroup.alpha = 0f;
            optOutToggleGroup.blocksRaycasts = false;
            optOutToggleGroup.interactable = false;
        }
    }

    void PositionActionControls()
    {
        if (!continueButton || !optOutToggle)
            return;

        var buttonRect = continueButton.transform as RectTransform;
        var toggleRect = optOutToggle.transform as RectTransform;
        if (!buttonRect || !toggleRect)
            return;

        float buttonWidth = Mathf.Max(1f, buttonRect.rect.width, buttonRect.sizeDelta.x);
        float toggleWidth = Mathf.Max(1f, toggleRect.rect.width, toggleRect.sizeDelta.x);
        float toggleX = buttonWidth * 0.5f + RuntimeActionGap + toggleWidth * 0.5f;
        toggleRect.anchoredPosition = new Vector2(toggleX, 0f);
    }

    void HookContinueButtonSfx()
    {
        if (!continueButton)
            return;

        var sfxHook = GetComponentInParent<GameplayUI_SFXHook>();
        if (!sfxHook)
            sfxHook = FindFirstObjectByType<GameplayUI_SFXHook>(FindObjectsInactive.Include);

        if (sfxHook)
            sfxHook.HookButton(continueButton);
    }

    void EnableNavigation()
    {
        if (!rootPanel)
            return;

        if (!_navigator)
            _navigator = ScopedMenuNavigator.Attach(rootPanel, rootPanel);

        if (_navigator)
        {
            _navigator.enabled = true;
            _navigator.SetNavigationRoot(rootPanel);
        }
    }

    void DisableNavigation()
    {
        if (_navigator)
            _navigator.enabled = false;
    }

    void ResolveAnimator()
    {
        if (!transitionAnimator && rootPanel)
            transitionAnimator = rootPanel.GetComponent<Animator>();

        if (!transitionAnimator)
            transitionAnimator = GetComponent<Animator>();

        if (transitionAnimator && useUnscaledAnimationTime)
            transitionAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    void SetDefaultTransitionAnimatorEnabled(bool enabled)
    {
        ResolveAnimator();
        if (transitionAnimator)
            transitionAnimator.enabled = enabled;
    }

    void SetVariantAnimationVisible(bool visible)
    {
        ResolveFireworkAnimationRoot();

        if (UsesLossVariant())
            ResolveLossAnimationRoot();

        if (fireworkAnimationRoot)
            fireworkAnimationRoot.SetActive(visible && !UsesLossVariant());

        if (lossAnimationRoot)
            lossAnimationRoot.SetActive(visible && UsesLossVariant());
    }

    void ResolveFireworkAnimationRoot()
    {
        if (fireworkAnimationRoot)
            return;

        Transform root = rootPanel ? rootPanel.transform : transform;
        fireworkAnimationRoot = FindChildByName(root, "Firework_Animation");
    }

    GameObject FindChildByName(Transform root, string childName)
    {
        if (!root || string.IsNullOrWhiteSpace(childName))
            return null;

        if (string.Equals(root.name, childName, StringComparison.OrdinalIgnoreCase))
            return root.gameObject;

        for (int i = 0; i < root.childCount; i++)
        {
            GameObject match = FindChildByName(root.GetChild(i), childName);
            if (match)
                return match;
        }

        return null;
    }

    void PlayShowAnimation()
    {
        Animator animator = GetVariantAnimator();
        if (!animator)
            return;

        if (useUnscaledAnimationTime)
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        RestartAnimator(animator);

        string stateName = GetShowAnimationStateName(animator);
        string triggerName = GetShowAnimationTriggerName(animator);

        if (!string.IsNullOrWhiteSpace(stateName))
            animator.Play(stateName, 0, 0f);

        if (!string.IsNullOrWhiteSpace(triggerName) && HasAnimatorTrigger(animator, triggerName))
        {
            animator.ResetTrigger(triggerName);
            animator.SetTrigger(triggerName);
        }
    }

    void PlayHideAnimation()
    {
        Animator animator = GetVariantAnimator();
        string triggerName = GetHideAnimationTriggerName(animator);

        if (!animator || string.IsNullOrWhiteSpace(triggerName) || !HasAnimatorTrigger(animator, triggerName))
            return;

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
    }

    void PlayShowAudio()
    {
        if (!AudioManager.I)
            return;

        AudioClip appearClip = UsesLossVariant() && lossAppearSfxClip ? lossAppearSfxClip : appearSfxClip;
        float appearVolume = UsesLossVariant() && lossAppearSfxClip ? lossAppearSfxVolume : appearSfxVolume;
        AudioClip loopClip = UsesLossVariant() && lossActiveLoopSfxClip ? lossActiveLoopSfxClip : activeLoopSfxClip;
        float loopVolume = UsesLossVariant() && lossActiveLoopSfxClip ? lossActiveLoopSfxVolume : activeLoopSfxVolume;

        if (appearClip)
            AudioManager.I.PlayRoundTransitionAppearSFX(appearClip, appearVolume);

        if (loopClip)
        {
            _currentActiveLoopSfxClip = loopClip;
            AudioManager.I.PlayRoundTransitionLoopSFX(loopClip, loopVolume);
        }
    }

    void StopActiveLoopSfx()
    {
        if (!AudioManager.I)
            return;

        if (_currentActiveLoopSfxClip)
        {
            AudioManager.I.StopRoundTransitionLoopSFX(_currentActiveLoopSfxClip);
            _currentActiveLoopSfxClip = null;
            return;
        }

        if (activeLoopSfxClip)
            AudioManager.I.StopRoundTransitionLoopSFX(activeLoopSfxClip);

        if (lossActiveLoopSfxClip && lossActiveLoopSfxClip != activeLoopSfxClip)
            AudioManager.I.StopRoundTransitionLoopSFX(lossActiveLoopSfxClip);
    }

    Animator GetVariantAnimator()
    {
        ResolveAnimator();

        if (UsesLossVariant())
            ResolveLossAnimator();

        if (UsesLossVariant() && lossTransitionAnimator)
        {
            if (useUnscaledAnimationTime)
                lossTransitionAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

            return lossTransitionAnimator;
        }

        return transitionAnimator;
    }

    void ResolveLossAnimationRoot()
    {
        if (!lossAnimationRoot)
        {
            Transform root = rootPanel ? rootPanel.transform : transform;
            lossAnimationRoot = FindChildByName(root, "GameOver_Animation");

            if (!lossAnimationRoot)
                lossAnimationRoot = FindChildByName(root, "Loss_Animation");
        }

        if (!lossAnimationRoot && lossTransitionAnimator)
            lossAnimationRoot = lossTransitionAnimator.gameObject;

        if (lossAnimationRoot && !lossAnimationRoot.scene.IsValid())
            InstantiateLossAnimationRoot(lossAnimationRoot);
    }

    void ResolveLossAnimator()
    {
        ResolveLossAnimationRoot();

        bool animatorIsSceneObject = lossTransitionAnimator && lossTransitionAnimator.gameObject.scene.IsValid();
        if ((!lossTransitionAnimator || !animatorIsSceneObject) && lossAnimationRoot)
            lossTransitionAnimator = lossAnimationRoot.GetComponentInChildren<Animator>(true);

        if (lossTransitionAnimator && useUnscaledAnimationTime)
            lossTransitionAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    void InstantiateLossAnimationRoot(GameObject source)
    {
        if (!source)
            return;

        Transform parent = rootPanel ? rootPanel.transform : transform;
        var instance = Instantiate(source, parent);
        instance.name = source.name;
        lossAnimationRoot = instance;
        lossTransitionAnimator = instance.GetComponentInChildren<Animator>(true);

        foreach (var graphic in instance.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }

    string GetShowAnimationStateName(Animator animator)
    {
        if (!UsesLossVariant())
            return showAnimationStateName;

        if (!string.IsNullOrWhiteSpace(lossShowAnimationStateName))
            return lossShowAnimationStateName;

        if (lossShowAnimationClip && AnimatorHasClip(animator, lossShowAnimationClip))
            return lossShowAnimationClip.name;

        bool usingSeparateLossAnimator = lossTransitionAnimator &&
                                         animator == lossTransitionAnimator &&
                                         lossTransitionAnimator != transitionAnimator;
        return usingSeparateLossAnimator ? GetFirstAnimationClipName(animator) : showAnimationStateName;
    }

    string GetShowAnimationTriggerName(Animator animator)
    {
        if (!UsesLossVariant())
            return showAnimationTrigger;

        if (!string.IsNullOrWhiteSpace(lossShowAnimationTrigger))
            return lossShowAnimationTrigger;

        bool usingSeparateLossAnimator = lossTransitionAnimator &&
                                         animator == lossTransitionAnimator &&
                                         lossTransitionAnimator != transitionAnimator;
        return usingSeparateLossAnimator ? string.Empty : showAnimationTrigger;
    }

    string GetHideAnimationTriggerName(Animator animator)
    {
        if (!UsesLossVariant())
            return hideAnimationTrigger;

        if (!string.IsNullOrWhiteSpace(lossHideAnimationTrigger))
            return lossHideAnimationTrigger;

        bool usingSeparateLossAnimator = lossTransitionAnimator &&
                                         animator == lossTransitionAnimator &&
                                         lossTransitionAnimator != transitionAnimator;
        return usingSeparateLossAnimator ? string.Empty : hideAnimationTrigger;
    }

    void RestartAnimator(Animator animator)
    {
        if (!animator)
            return;

        animator.enabled = true;
        animator.Rebind();
        animator.Update(0f);
    }

    bool AnimatorHasClip(Animator animator, AnimationClip clip)
    {
        if (!animator || !clip || !animator.runtimeAnimatorController)
            return false;

        var clips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == clip || string.Equals(clips[i].name, clip.name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    string GetFirstAnimationClipName(Animator animator)
    {
        if (!animator || !animator.runtimeAnimatorController)
            return string.Empty;

        var clips = animator.runtimeAnimatorController.animationClips;
        return clips != null && clips.Length > 0 && clips[0] ? clips[0].name : string.Empty;
    }

    bool HasAnimatorTrigger(Animator animator, string triggerName)
    {
        if (!animator || string.IsNullOrWhiteSpace(triggerName))
            return false;

        var parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (parameter.type == AnimatorControllerParameterType.Trigger &&
                string.Equals(parameter.name, triggerName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    bool UsesLossVariant()
    {
        return _currentVariant == RoundTransitionVariant.Loss;
    }
}
