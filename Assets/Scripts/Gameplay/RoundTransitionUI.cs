using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundTransitionUI : MonoBehaviour
{
    const int RuntimeSortingOrder = 30000;

    [Header("Root")]
    [SerializeField] GameObject rootPanel;
    [SerializeField] Image backgroundImage;
    [SerializeField] TMP_Text messageText;
    [SerializeField] Button continueButton;
    [SerializeField] CanvasGroup continueButtonGroup;
    [SerializeField] RectTransform contentRoot;

    [Header("Optional Assets")]
    [SerializeField] TMP_FontAsset transitionFont;
    [SerializeField] Button continueButtonPrefab;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] float fadeInSeconds = 1.85f;
    [SerializeField, Range(0f, 1f)] float backgroundAlpha = 0.72f;

    Action _onContinue;
    Coroutine _fadeRoutine;
    bool _builtRuntimeUi;
    bool _continueButtonUsesPrefab;

    public bool IsShowing => rootPanel && rootPanel.activeSelf;

    public static RoundTransitionUI CreateRuntimeInstance(TMP_FontAsset font = null, Button buttonPrefab = null)
    {
        var canvasGo = new GameObject(
            "RoundTransition_Canvas",
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
        EnsureBuilt();

        if (!rootPanel)
        {
            onContinue?.Invoke();
            return;
        }

        ApplyFont();
        _onContinue = onContinue;

        gameObject.SetActive(true);
        rootPanel.SetActive(true);
        rootPanel.transform.SetAsLastSibling();

        var rootGroup = rootPanel.GetComponent<CanvasGroup>();
        if (rootGroup)
        {
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
        }

        if (backgroundImage)
            backgroundImage.color = new Color(0f, 0f, 0f, backgroundAlpha);

        if (messageText)
        {
            messageText.text = message;
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

        if (continueButton)
        {
            continueButton.interactable = false;
        }

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

        _fadeRoutine = null;
    }

    void Continue()
    {
        var callback = _onContinue;
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
        }
    }

    void EnsureBuilt()
    {
        if (_builtRuntimeUi)
            return;

        if (!rootPanel)
            BuildRuntimeUi();

        ApplyFont();
        WireContinueButton();

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
        contentRoot.sizeDelta = new Vector2(1320f, 260f);

        var layout = contentGo.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 34f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var messageGo = new GameObject("RoundTransition_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        messageGo.transform.SetParent(contentGo.transform, false);

        var messageRect = messageGo.GetComponent<RectTransform>();
        messageRect.sizeDelta = new Vector2(1320f, 140f);

        messageText = messageGo.GetComponent<TextMeshProUGUI>();
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.enableAutoSizing = true;
        messageText.fontSizeMin = 36f;
        messageText.fontSizeMax = 78f;
        messageText.fontStyle = FontStyles.Bold;
        messageText.raycastTarget = false;
        messageText.color = Color.white;

        BuildContinueButton(contentGo.transform);
    }

    void RebuildContinueButton()
    {
        if (continueButton)
            Destroy(continueButton.gameObject);

        continueButton = null;
        continueButtonGroup = null;

        BuildContinueButton(contentRoot);
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
    }

    void SetTextAlpha(float alpha)
    {
        if (!messageText)
            return;

        var color = messageText.color;
        color.a = alpha;
        messageText.color = color;
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
    }

    void ApplyContinueLabel()
    {
        if (!continueButton)
            return;

        var labels = continueButton.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i].text = "Continue";
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
}
