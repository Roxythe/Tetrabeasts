using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialPopupView : MonoBehaviour
{
    static readonly string[] TutorialDimmerObjectNames =
    {
        "Warning_Dimmer_Panel",
        "Dimmer_Panel",
        "TextPromptBackground_Image"
    };

    public enum PopupAnchorPreset
    {
        Default,
        Top,
        Bottom,
        Custom
    }

    [Header("Refs")]
    [SerializeField] RectTransform popupRoot;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] Button continueButton;
    [SerializeField] Button skipButton;

    [Header("Placement")]
    [SerializeField] Vector2 defaultAnchoredPosition;
    [SerializeField] Vector2 topAnchoredPosition = new Vector2(0f, 400f);
    [SerializeField] Vector2 bottomAnchoredPosition = new Vector2(0f, -400f);

    [Header("Visuals")]
    [SerializeField, Range(0.1f, 1f)] float visibleAlpha = 1f;
    [SerializeField] bool useAnimatedTransition = false;

    [Header("Optional Status Visuals")]
    [SerializeField] GameObject waitingVisual;
    [SerializeField] GameObject readyVisual;

    string rawBody;

    public RectTransform PopupRectTransform
    {
        get
        {
            if (!popupRoot)
                popupRoot = transform as RectTransform;

            return popupRoot;
        }
    }

    public Button ContinueButton => continueButton;
    public Button SkipButton => skipButton;

    void Awake()
    {
        if (!popupRoot)
            popupRoot = transform as RectTransform;

        EnsureCanvasGroup();

        if (popupRoot)
            defaultAnchoredPosition = popupRoot.anchoredPosition;
    }

    void OnEnable()
    {
        TetrabeastsControls.ProfileChanged += HandleControlsChanged;
        TetrabeastsControls.BindingsChanged += HandleControlsChanged;
        TetrabeastsControls.PlatformDefaultProfileChanged += HandleControlsChanged;
        TetrabeastsLocalization.LanguageChanged += RefreshContent;
    }

    void OnDisable()
    {
        TetrabeastsControls.ProfileChanged -= HandleControlsChanged;
        TetrabeastsControls.BindingsChanged -= HandleControlsChanged;
        TetrabeastsControls.PlatformDefaultProfileChanged -= HandleControlsChanged;
        TetrabeastsLocalization.LanguageChanged -= RefreshContent;
    }

    public void Show(bool instant = false)
    {
        SuppressDimmerVisuals();
        transform.SetAsLastSibling();

        EnsureCanvasGroup();
        UIPanelTransition.Show(gameObject, visibleAlpha, instant || !useAnimatedTransition);
        EnsureCanvasGroup();
        SuppressDimmerVisuals();

        if (canvasGroup)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public bool IsShowing
    {
        get
        {
            return gameObject.activeInHierarchy &&
                   canvasGroup != null &&
                   canvasGroup.alpha > 0.001f &&
                   canvasGroup.blocksRaycasts;
        }
    }

    public void Hide(bool instant = false)
    {
        SuppressDimmerVisuals();
        EnsureCanvasGroup();
        if (!canvasGroup) return;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        UIPanelTransition.Hide(gameObject, instant || !useAnimatedTransition);
    }

    public void SuppressDimmerVisuals()
    {
        for (int i = 0; i < TutorialDimmerObjectNames.Length; i++)
        {
            Transform dimmer = FindDeepChild(transform, TutorialDimmerObjectNames[i]);
            if (!dimmer)
                continue;

            var graphic = dimmer.GetComponent<Graphic>();
            if (graphic)
            {
                var color = graphic.color;
                color.a = 0f;
                graphic.color = color;
                graphic.raycastTarget = false;
            }

            if (dimmer.gameObject.activeSelf)
                dimmer.gameObject.SetActive(false);
        }
    }

    public void SetContent(string body)
    {
        rawBody = body ?? string.Empty;
        RefreshContent();
    }

    void HandleControlsChanged(TetrabeastsControlProfile profile)
    {
        RefreshContent();
    }

    void RefreshContent()
    {
        if (!bodyText)
            return;

        string localized = TetrabeastsLocalization.LocalizeText(rawBody ?? string.Empty);
        bodyText.richText = true;
        bodyText.text = TetrabeastsControls.ResolveControlPromptTokens(localized);
    }

    public void SetContinueVisible(bool visible)
    {
        if (continueButton)
            continueButton.gameObject.SetActive(visible);
    }

    public void SetContinueInteractable(bool interactable)
    {
        if (continueButton)
            continueButton.interactable = interactable;
    }

    public void SetSkipVisible(bool visible)
    {
        if (skipButton)
            skipButton.gameObject.SetActive(visible);
    }

    public void SetStepState(bool waitingForInteraction, bool readyToContinue)
    {
        if (waitingVisual)
            waitingVisual.SetActive(waitingForInteraction);

        if (readyVisual)
            readyVisual.SetActive(readyToContinue);
    }

    public void ApplyPresetPosition(PopupAnchorPreset preset, Vector2 customPosition)
    {
        if (!PopupRectTransform)
            return;

        switch (preset)
        {
            case PopupAnchorPreset.Top:
                PopupRectTransform.anchoredPosition = topAnchoredPosition;
                break;

            case PopupAnchorPreset.Bottom:
                PopupRectTransform.anchoredPosition = bottomAnchoredPosition;
                break;

            case PopupAnchorPreset.Custom:
                PopupRectTransform.anchoredPosition = customPosition;
                break;

            default:
                PopupRectTransform.anchoredPosition = defaultAnchoredPosition;
                break;
        }
    }

    public void SetVisibleAlpha(float alpha)
    {
        visibleAlpha = Mathf.Clamp(alpha, 0.1f, 1f);

        if (canvasGroup && gameObject.activeInHierarchy)
            canvasGroup.alpha = visibleAlpha;
    }

    void EnsureCanvasGroup()
    {
        if (!canvasGroup)
            canvasGroup = GetComponent<CanvasGroup>();

        if (!canvasGroup)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    static Transform FindDeepChild(Transform root, string childName)
    {
        if (!root || string.IsNullOrEmpty(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child && child.name == childName)
                return child;

            Transform nested = FindDeepChild(child, childName);
            if (nested)
                return nested;
        }

        return null;
    }
}
