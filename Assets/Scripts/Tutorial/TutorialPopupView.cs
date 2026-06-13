using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialPopupView : MonoBehaviour
{
    const string RuntimeTutorialDimmerName = "TutorialPopup_Dimmer";
    const string RuntimeContinueCueName = "TutorialContinue_Cue";
    const float ContinueCuePulseFrequency = 1.6f;
    const float ContinueCuePulseAmplitude = 0.08f;
    const float ContinueCueBlinkFrequency = 2f;

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
    [SerializeField] Sprite continueCueArrowSprite;

    string rawBody;
    string _lastRenderedBody;
    TetrabeastsControlProfile _lastRenderedPromptProfile;
    UITargetInputSource _lastRenderedInputSource = UITargetInputSource.None;
    RectTransform _dimmerRect;
    object _contentOwner;
    bool _reservedForWarnings;
    RectTransform _continueCueRoot;
    RectTransform _continueCueLabelRect;
    TMP_Text _continueCueLabel;
    Image _continueCueArrow;
    Vector4 _bodyTextDefaultMargin;
    bool _bodyTextDefaultMarginCaptured;
    bool _continueCueVisible;
    static Sprite s_continueArrowSprite;

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
    public bool IsReservedForWarnings => _reservedForWarnings;

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
        UpdateContinueCueAnimation();

        if (!bodyText || string.IsNullOrEmpty(rawBody))
            return;

        if (!string.Equals(_lastRenderedBody, rawBody, System.StringComparison.Ordinal) ||
            _lastRenderedPromptProfile != TetrabeastsControls.ControlPromptProfile ||
            _lastRenderedInputSource != UICursorController.CurrentTargetInputSource)
            RefreshContent();
    }

    public void SetReservedForWarnings(bool reserved)
    {
        _reservedForWarnings = reserved;
        if (reserved)
            SetContinueCueVisible(false);
    }

    public bool TryClaimOwner(object owner, bool replaceExisting = false)
    {
        if (owner == null)
            return true;

        if (_contentOwner == null || ReferenceEquals(_contentOwner, owner))
        {
            _contentOwner = owner;
            return true;
        }

        if (!replaceExisting)
            return false;

        _contentOwner = owner;
        ClearRenderedContent();
        return true;
    }

    public void ReleaseOwner(object owner)
    {
        if (owner == null || ReferenceEquals(_contentOwner, owner))
            _contentOwner = null;
    }

    public bool IsOwnedBy(object owner)
    {
        return owner != null && ReferenceEquals(_contentOwner, owner);
    }

    public bool IsOwnedByOther(object owner)
    {
        return _contentOwner != null && (owner == null || !ReferenceEquals(_contentOwner, owner));
    }

    public bool IsShowingForOwner(object owner)
    {
        return IsOwnedBy(owner) && IsShowing;
    }

    public void Show(bool instant = false, object owner = null)
    {
        if (IsOwnedByOther(owner))
            return;

        RenderContent(force: true);
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

    public void Hide(bool instant = false, object owner = null)
    {
        if (IsOwnedByOther(owner))
            return;

        SetContinueCueVisible(false);
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

    public void ClearContent(object owner = null)
    {
        if (IsOwnedByOther(owner))
            return;

        rawBody = string.Empty;
        ClearRenderedContent();
    }

    public void SetContent(string body, object owner = null)
    {
        if (IsOwnedByOther(owner))
            return;

        rawBody = body ?? string.Empty;
        RenderContent(force: true);
    }

    void HandleControlsChanged(TetrabeastsControlProfile profile)
    {
        RefreshContent();
    }

    void RefreshContent()
    {
        RenderContent(force: false);
    }

    void RenderContent(bool force)
    {
        if (!bodyText)
            return;

        if (!force &&
            string.Equals(_lastRenderedBody, rawBody, System.StringComparison.Ordinal) &&
            _lastRenderedPromptProfile == TetrabeastsControls.ControlPromptProfile &&
            _lastRenderedInputSource == UICursorController.CurrentTargetInputSource)
            return;

        bodyText.richText = true;
        bodyText.text = FormatBodyForDisplay(rawBody ?? string.Empty, updateContinueCue: true);
        bodyText.ForceMeshUpdate(true, true);
        _lastRenderedBody = rawBody;
        _lastRenderedPromptProfile = TetrabeastsControls.ControlPromptProfile;
        _lastRenderedInputSource = UICursorController.CurrentTargetInputSource;
    }

    public string FormatBodyForDisplay(string body, bool updateContinueCue)
    {
        body ??= string.Empty;

        string localized = TetrabeastsLocalization.LocalizeText(body);
        bool showContinueCue = !_reservedForWarnings && HasTutorialContinuePrompt(body);
        string displayBody = localized;

        if (showContinueCue)
            TryStripTutorialContinuePrompt(localized, out displayBody);

        if (updateContinueCue)
        {
            SetContinueCueVisible(showContinueCue);
            if (showContinueCue)
                UpdateContinueCueLabel();
        }

        return TetrabeastsControls.ResolveControlPromptTokens(displayBody);
    }

    void ClearRenderedContent()
    {
        rawBody = string.Empty;
        _lastRenderedBody = null;

        if (bodyText)
        {
            bodyText.text = string.Empty;
            bodyText.ForceMeshUpdate(true, true);
        }

        SetContinueCueVisible(false);
    }

    static bool HasTutorialContinuePrompt(string text)
    {
        return TryStripTutorialContinuePrompt(text, out _);
    }

    static bool TryStripTutorialContinuePrompt(string text, out string stripped)
    {
        stripped = text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text) ||
            text.IndexOf("[F]", System.StringComparison.OrdinalIgnoreCase) < 0)
            return false;

        if (TryStripFinalParentheticalContinuePrompt(text, '(', ')', out stripped) ||
            TryStripFinalParentheticalContinuePrompt(text, '\uff08', '\uff09', out stripped))
            return true;

        int tokenIndex = text.LastIndexOf("[F]", System.StringComparison.OrdinalIgnoreCase);
        if (tokenIndex < 0)
            return false;

        string tail = text.Substring(tokenIndex);
        if (tail.Length > 80 || !LooksLikeContinuePromptTail(tail))
            return false;

        int promptStart = FindContinuePromptStart(text, tokenIndex);
        if (promptStart < 0)
            return false;

        stripped = text.Substring(0, promptStart).TrimEnd();
        return true;
    }

    static bool TryStripFinalParentheticalContinuePrompt(string text, char open, char close, out string stripped)
    {
        stripped = text;
        int end = text.Length - 1;
        while (end >= 0 && char.IsWhiteSpace(text[end]))
            end--;

        if (end < 0 || text[end] != close)
            return false;

        int start = text.LastIndexOf(open, end);
        if (start < 0)
            return false;

        string suffix = text.Substring(start, end - start + 1);
        if (suffix.IndexOf("[F]", System.StringComparison.OrdinalIgnoreCase) < 0)
            return false;

        stripped = (text.Substring(0, start) + text.Substring(end + 1)).TrimEnd();
        return true;
    }

    static bool LooksLikeContinuePromptTail(string tail)
    {
        if (string.IsNullOrWhiteSpace(tail))
            return false;

        if (tail.IndexOf("continue", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            tail.IndexOf("continuar", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            tail.IndexOf("continuer", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            tail.IndexOf("continuare", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            tail.IndexOf("weiter", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            tail.IndexOf("fortfahr", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        string compactTail = RemoveWhitespace(tail);
        return compactTail.Contains("\u7ee7\u7eed") ||
               compactTail.Contains("\u7e7c\u7e8c") ||
               compactTail.Contains("\u043f\u0440\u043e\u0434\u043e\u043b\u0436") ||
               compactTail.Contains("\uacc4\uc18d");
    }

    static int FindContinuePromptStart(string text, int tokenIndex)
    {
        int sentenceStart = FindPreviousSentenceStart(text, tokenIndex);
        int pressIndex = LastIndexOfBefore(text, "Press", tokenIndex);
        if (pressIndex >= sentenceStart)
            return pressIndex;

        int localizedPressIndex = LastIndexOfBefore(text, "\u6309", tokenIndex);
        if (localizedPressIndex >= sentenceStart)
            return localizedPressIndex;

        return sentenceStart;
    }

    static int FindPreviousSentenceStart(string text, int beforeIndex)
    {
        int boundary = -1;
        char[] boundaries = { '.', '!', '?', '\u3002', '\uff01', '\uff1f', '\n' };
        for (int i = 0; i < boundaries.Length; i++)
            boundary = Mathf.Max(boundary, text.LastIndexOf(boundaries[i], Mathf.Max(0, beforeIndex - 1)));

        int start = boundary + 1;
        while (start < text.Length && char.IsWhiteSpace(text[start]))
            start++;

        return start;
    }

    static int LastIndexOfBefore(string text, string value, int beforeIndex)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(value) || beforeIndex <= 0)
            return -1;

        int count = Mathf.Min(beforeIndex, text.Length);
        return text.LastIndexOf(value, count, System.StringComparison.OrdinalIgnoreCase);
    }

    static string RemoveWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var builder = new System.Text.StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
                builder.Append(text[i]);
        }

        return builder.ToString();
    }

    void SetContinueCueVisible(bool visible)
    {
        _continueCueVisible = visible;

        if (visible)
        {
            EnsureContinueCue();
            ApplyContinueCueBodyMargin();
            UpdateContinueCueAnimation();
        }
        else
        {
            ResolveExistingContinueCue();
            RestoreBodyTextMargin();
        }

        if (_continueCueRoot)
            _continueCueRoot.gameObject.SetActive(visible);
    }

    void ResolveExistingContinueCue()
    {
        if (_continueCueRoot && _continueCueLabel && _continueCueArrow)
            return;

        RectTransform parent = bodyText && bodyText.transform.parent
            ? bodyText.transform.parent as RectTransform
            : PopupRectTransform;

        if (!parent)
            parent = transform as RectTransform;

        Transform existing = parent ? FindDirectChild(parent, RuntimeContinueCueName) : null;
        if (!existing && PopupRectTransform && PopupRectTransform != parent)
            existing = FindDirectChild(PopupRectTransform, RuntimeContinueCueName);

        if (!existing)
            return;

        _continueCueRoot = existing as RectTransform;
        _continueCueLabel = existing.GetComponentInChildren<TMP_Text>(true);
        _continueCueLabelRect = _continueCueLabel ? _continueCueLabel.transform as RectTransform : null;
        _continueCueArrow = existing.GetComponentInChildren<Image>(true);
    }

    void EnsureContinueCue()
    {
        if (_continueCueRoot && _continueCueLabel && _continueCueArrow)
            return;

        RectTransform parent = bodyText && bodyText.transform.parent
            ? bodyText.transform.parent as RectTransform
            : PopupRectTransform;

        if (!parent)
            parent = transform as RectTransform;

        Transform existing = parent ? FindDirectChild(parent, RuntimeContinueCueName) : null;
        if (existing)
        {
            _continueCueRoot = existing as RectTransform;
            _continueCueLabel = existing.GetComponentInChildren<TMP_Text>(true);
            _continueCueLabelRect = _continueCueLabel ? _continueCueLabel.transform as RectTransform : null;
            _continueCueArrow = existing.GetComponentInChildren<Image>(true);
            return;
        }

        var root = new GameObject(RuntimeContinueCueName, typeof(RectTransform));
        root.layer = gameObject.layer;
        root.transform.SetParent(parent, false);
        _continueCueRoot = root.transform as RectTransform;
        _continueCueRoot.anchorMin = new Vector2(1f, 0f);
        _continueCueRoot.anchorMax = new Vector2(1f, 0f);
        _continueCueRoot.pivot = new Vector2(1f, 0f);
        _continueCueRoot.anchoredPosition = new Vector2(-82f, 46f);
        _continueCueRoot.sizeDelta = new Vector2(110f, 52f);

        var labelObject = new GameObject("Input_Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.layer = gameObject.layer;
        labelObject.transform.SetParent(root.transform, false);
        _continueCueLabelRect = labelObject.transform as RectTransform;
        _continueCueLabelRect.anchorMin = new Vector2(0f, 0.5f);
        _continueCueLabelRect.anchorMax = new Vector2(0f, 0.5f);
        _continueCueLabelRect.pivot = new Vector2(0f, 0.5f);
        _continueCueLabelRect.anchoredPosition = new Vector2(0f, 0f);
        _continueCueLabelRect.sizeDelta = new Vector2(70f, 48f);

        _continueCueLabel = labelObject.GetComponent<TMP_Text>();
        _continueCueLabel.raycastTarget = false;
        _continueCueLabel.richText = true;
        _continueCueLabel.alignment = TextAlignmentOptions.Center;
        _continueCueLabel.enableAutoSizing = true;
        _continueCueLabel.fontSizeMin = 18f;
        _continueCueLabel.fontSizeMax = 34f;
        _continueCueLabel.fontSize = 34f;

        if (bodyText)
        {
            _continueCueLabel.font = bodyText.font;
            _continueCueLabel.fontSharedMaterial = bodyText.fontSharedMaterial;
            _continueCueLabel.color = bodyText.color;
        }

        var arrowObject = new GameObject("Arrow_Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        arrowObject.layer = gameObject.layer;
        arrowObject.transform.SetParent(root.transform, false);
        var arrowRect = arrowObject.transform as RectTransform;
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(0f, 0f);
        arrowRect.sizeDelta = new Vector2(34f, 28f);
        arrowRect.localRotation = Quaternion.Euler(0f, 0f, 90f);

        _continueCueArrow = arrowObject.GetComponent<Image>();
        _continueCueArrow.raycastTarget = false;
        _continueCueArrow.sprite = GetContinueArrowSprite();
        _continueCueArrow.preserveAspect = true;
        _continueCueArrow.color = Color.white;
    }

    void UpdateContinueCueLabel()
    {
        EnsureContinueCue();
        if (!_continueCueLabel)
            return;

        _continueCueLabel.text = TetrabeastsControls.GetTutorialContinueIndicatorLabel();
        _continueCueLabel.ForceMeshUpdate(true, true);
    }

    void UpdateContinueCueAnimation()
    {
        if (!_continueCueVisible || !_continueCueRoot)
            return;

        _continueCueRoot.localScale = Vector3.one;
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * ContinueCuePulseFrequency) * ContinueCuePulseAmplitude;
        if (_continueCueLabelRect)
            _continueCueLabelRect.localScale = new Vector3(pulse, pulse, 1f);

        if (_continueCueArrow)
        {
            _continueCueArrow.transform.localScale = Vector3.one;
            bool arrowVisible = Mathf.FloorToInt(Time.unscaledTime * ContinueCueBlinkFrequency) % 2 == 0;
            _continueCueArrow.enabled = arrowVisible;
        }
    }

    void ApplyContinueCueBodyMargin()
    {
        if (!bodyText)
            return;

        if (!_bodyTextDefaultMarginCaptured)
        {
            _bodyTextDefaultMargin = bodyText.margin;
            _bodyTextDefaultMarginCaptured = true;
        }

        bodyText.margin = new Vector4(
            _bodyTextDefaultMargin.x,
            _bodyTextDefaultMargin.y,
            Mathf.Max(_bodyTextDefaultMargin.z, 122f),
            Mathf.Max(_bodyTextDefaultMargin.w, 42f));
    }

    void RestoreBodyTextMargin()
    {
        if (bodyText && _bodyTextDefaultMarginCaptured)
            bodyText.margin = _bodyTextDefaultMargin;

        if (_continueCueRoot)
            _continueCueRoot.localScale = Vector3.one;

        if (_continueCueLabelRect)
            _continueCueLabelRect.localScale = Vector3.one;

        if (_continueCueArrow)
        {
            _continueCueArrow.enabled = true;
            _continueCueArrow.transform.localScale = Vector3.one;
        }
    }

    Sprite GetContinueArrowSprite()
    {
        if (continueCueArrowSprite)
            return continueCueArrowSprite;

        if (s_continueArrowSprite)
            return s_continueArrowSprite;

        const int width = 18;
        const int height = 14;
        bool[,] fill = new bool[width, height];
        for (int y = 4; y <= 9; y++)
        {
            for (int x = 0; x <= 9; x++)
                fill[x, y] = true;
        }

        int center = height / 2;
        for (int y = 1; y < height - 1; y++)
        {
            int halfHeight = Mathf.Abs(y - center);
            int start = 8 + halfHeight;
            for (int x = start; x < width - 1; x++)
                fill[x, y] = true;
        }

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = RuntimeContinueCueName + "_ArrowSprite";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color outline = new Color(0f, 0f, 0f, 0.85f);
        Color arrow = new Color(1f, 0.88f, 0.08f, 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, clear);
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!fill[x, y])
                    continue;

                for (int oy = -1; oy <= 1; oy++)
                {
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int px = x + ox;
                        int py = y + oy;
                        if (px >= 0 && px < width && py >= 0 && py < height && !fill[px, py])
                            texture.SetPixel(px, py, outline);
                    }
                }
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (fill[x, y])
                    texture.SetPixel(x, y, arrow);
            }
        }

        texture.Apply(false, true);
        s_continueArrowSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), height, 0, SpriteMeshType.FullRect);
        s_continueArrowSprite.name = RuntimeContinueCueName + "_ArrowSprite";
        s_continueArrowSprite.hideFlags = HideFlags.HideAndDontSave;
        return s_continueArrowSprite;
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
