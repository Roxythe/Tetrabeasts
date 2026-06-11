using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialPopupView : MonoBehaviour
{
    const string RuntimeTutorialDimmerName = "TutorialPopup_Dimmer";

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
    [SerializeField] bool showDimmer = true;
    [SerializeField] Graphic dimmerGraphic;
    [SerializeField] Color dimmerColor = new Color(0f, 0f, 0f, 0.55f);

    [Header("Optional Status Visuals")]
    [SerializeField] GameObject waitingVisual;
    [SerializeField] GameObject readyVisual;

    string rawBody;
    string _lastRenderedBody;
    TetrabeastsControlProfile _lastRenderedPromptProfile;
    UITargetInputSource _lastRenderedInputSource = UITargetInputSource.None;
    RectTransform _dimmerRect;

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
        HideDimmerVisual();
    }

    void OnDestroy()
    {
        HideDimmerVisual();
    }

    void LateUpdate()
    {
        if (!bodyText || string.IsNullOrEmpty(rawBody))
            return;

        if (!string.Equals(_lastRenderedBody, rawBody, System.StringComparison.Ordinal) ||
            _lastRenderedPromptProfile != TetrabeastsControls.ControlPromptProfile ||
            _lastRenderedInputSource != UICursorController.CurrentTargetInputSource)
            RefreshContent();
    }

    public void Show(bool instant = false)
    {
        ShowDimmerVisual();
        transform.SetAsLastSibling();

        EnsureCanvasGroup();
        UIPanelTransition.Show(gameObject, visibleAlpha, instant || !useAnimatedTransition);
        EnsureCanvasGroup();
        ShowDimmerVisual();

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
        HideDimmerVisual();
        EnsureCanvasGroup();
        if (!canvasGroup) return;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        UIPanelTransition.Hide(gameObject, instant || !useAnimatedTransition);
    }

    public void SuppressDimmerVisuals()
    {
        HideDimmerVisual();
    }

    void ShowDimmerVisual()
    {
        if (!showDimmer)
        {
            HideDimmerVisual();
            return;
        }

        EnsureDimmerGraphic();
        if (!dimmerGraphic)
            return;

        dimmerGraphic.gameObject.SetActive(true);
        dimmerGraphic.color = dimmerColor;
        dimmerGraphic.raycastTarget = false;
        SuppressLocalDimmerVisuals();
        PlaceDimmerUnderPopup();
    }

    void HideDimmerVisual()
    {
        SuppressLocalDimmerVisuals();

        if (dimmerGraphic)
        {
            var color = dimmerGraphic.color;
            color.a = 0f;
            dimmerGraphic.color = color;
            dimmerGraphic.raycastTarget = false;
            dimmerGraphic.gameObject.SetActive(false);
        }
    }

    void PlaceDimmerUnderPopup()
    {
        if (!dimmerGraphic)
            return;

        _dimmerRect = dimmerGraphic.transform as RectTransform;
        if (!_dimmerRect)
            return;

        Transform layerParent = GetDimmerLayerParent();
        if (layerParent && _dimmerRect.parent != layerParent)
            _dimmerRect.SetParent(layerParent, false);

        StretchDimmerToParent(_dimmerRect);

        Transform popupLayerSibling = GetLayerSiblingTransform(transform, _dimmerRect.parent);
        if (popupLayerSibling)
        {
            int popupIndex = popupLayerSibling.GetSiblingIndex();
            _dimmerRect.SetSiblingIndex(Mathf.Max(0, popupIndex));
            popupLayerSibling.SetAsLastSibling();
        }
        else
        {
            _dimmerRect.SetAsFirstSibling();
        }
    }

    Transform GetDimmerLayerParent()
    {
        var canvas = GetComponentInParent<Canvas>(true);
        if (canvas)
            return canvas.transform;

        return transform.parent;
    }

    static void StretchDimmerToParent(RectTransform rect)
    {
        if (!rect)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    void EnsureDimmerGraphic()
    {
        if (dimmerGraphic && !IsTransformInsidePopup(dimmerGraphic.transform))
            return;

        if (dimmerGraphic && IsTransformInsidePopup(dimmerGraphic.transform))
        {
            SuppressGraphic(dimmerGraphic);
            dimmerGraphic = null;
        }

        Transform layerParent = GetDimmerLayerParent();
        if (!layerParent)
            return;

        Transform existing = FindDirectChild(layerParent, RuntimeTutorialDimmerName);
        if (existing)
        {
            dimmerGraphic = existing.GetComponent<Graphic>();
            if (!dimmerGraphic)
                dimmerGraphic = existing.gameObject.AddComponent<Image>();

            _dimmerRect = existing as RectTransform;
            return;
        }

        var go = new GameObject(RuntimeTutorialDimmerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(layerParent, false);
        _dimmerRect = go.transform as RectTransform;
        dimmerGraphic = go.GetComponent<Image>();
        dimmerGraphic.raycastTarget = false;
        go.SetActive(false);
    }

    void SuppressLocalDimmerVisuals()
    {
        for (int i = 0; i < TutorialDimmerObjectNames.Length; i++)
        {
            Transform dimmer = FindDeepChild(transform, TutorialDimmerObjectNames[i]);
            if (!dimmer)
                continue;

            var graphic = dimmer.GetComponent<Graphic>();
            if (graphic)
                SuppressGraphic(graphic);

            if (dimmer.gameObject.activeSelf)
                dimmer.gameObject.SetActive(false);
        }
    }

    bool IsTransformInsidePopup(Transform candidate)
    {
        return candidate && (candidate == transform || candidate.IsChildOf(transform));
    }

    static void SuppressGraphic(Graphic graphic)
    {
        if (!graphic)
            return;

        var color = graphic.color;
        color.a = 0f;
        graphic.color = color;
        graphic.raycastTarget = false;
    }

    static Transform FindDirectChild(Transform root, string childName)
    {
        if (!root || string.IsNullOrEmpty(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child && child.name == childName)
                return child;
        }

        return null;
    }

    static Transform GetLayerSiblingTransform(Transform child, Transform layerParent)
    {
        if (!child || !layerParent)
            return null;

        Transform current = child;
        while (current && current.parent && current.parent != layerParent)
            current = current.parent;

        return current && current.parent == layerParent ? current : null;
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
        _lastRenderedBody = rawBody;
        _lastRenderedPromptProfile = TetrabeastsControls.ControlPromptProfile;
        _lastRenderedInputSource = UICursorController.CurrentTargetInputSource;
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
