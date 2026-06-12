using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ConfirmationPopupUI : MonoBehaviour
{
    const string RuntimeWarningDimmerName = "Warning_Dimmer_Panel";
    const string RuntimeWarningPopupName = "Warning_Popup";
    const string BodyTextObjectName = "TutorialPrompt_Text";
    const string TextFrameRootObjectName = "TextFrameBorder";
    const string GuidePortraitRootName = "GuidePortraitBG_Image";
    const string GuidePortraitImageName = "GuidePortrait_Image";
    const string GuideNameTextName = "Name_Text";
    const string GuideShadowNameTextName = "ShadowName_Text";
    const float WarningButtonGap = 24f;
    static ConfirmationPopupUI s_showingPopup;

    [SerializeField] TutorialPopupView popupView;
    [SerializeField] TMP_Text continueButtonLabel;
    [SerializeField] TMP_Text cancelButtonLabel;
    [SerializeField] GameObject warningVisual;
    [SerializeField] TutorialPopupView.PopupAnchorPreset anchorPreset = TutorialPopupView.PopupAnchorPreset.Default;
    [SerializeField] Vector2 anchoredPosition;
    [SerializeField, Range(0.1f, 1f)] float popupAlpha = 1f;
    [SerializeField, Range(0f, 1f)] float warningDimmerAlpha = 0.65f;
    [SerializeField, HideInInspector] bool dedicatedWarningInstance;

    Action _onConfirm;
    Action _onCancel;
    bool _explicitShowInProgress;
    bool _ownsCurrentPopup;
    ButtonLayoutState _continueButtonDefaultLayout;
    ButtonLayoutState _cancelButtonDefaultLayout;
    RectTransformLayoutState _popupDefaultLayout;
    TMP_Text _warningBodyText;
    bool _buttonLayoutsCaptured;
    bool _popupLayoutCaptured;

    public bool IsShowing => _ownsCurrentPopup && popupView && popupView.IsShowingForOwner(this);
    public GameObject NavigationRoot => popupView ? popupView.gameObject : gameObject;

    public static bool IsAnyShowing
    {
        get
        {
            return s_showingPopup && s_showingPopup.IsShowing;
        }
    }

    void Awake()
    {
        EnsureReferences();

        if (dedicatedWarningInstance && popupView)
            popupView.SetReservedForWarnings(true);

        SetWarningVisualVisible(false);

        if (!_explicitShowInProgress)
            popupView?.Hide(true, this);
    }

    void OnDisable()
    {
        if (s_showingPopup == this)
            s_showingPopup = null;

        popupView?.ReleaseOwner(this);
    }

    public static ConfirmationPopupUI FindOrCreate()
    {
        if (s_showingPopup)
            return s_showingPopup;

        var popups = UnityEngine.Object.FindObjectsByType<ConfirmationPopupUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < popups.Length; i++)
        {
            var popup = popups[i];
            if (popup && popup.IsDedicatedWarningCandidate())
                return popup;
        }

        ConfirmationPopupUI template = null;
        for (int i = 0; i < popups.Length; i++)
        {
            if (!popups[i])
                continue;

            template = popups[i];
            break;
        }

        if (template)
            return CreateDedicatedWarningInstance(template);

        var templatePopupView = FindTemplateTutorialPopupView();
        if (!templatePopupView)
            return null;

        var created = templatePopupView.GetComponent<ConfirmationPopupUI>();
        if (!created)
            created = templatePopupView.gameObject.AddComponent<ConfirmationPopupUI>();

        return CreateDedicatedWarningInstance(created);
    }

    public void ShowAlert(string body, Action onClosed = null, string continueText = "OK", bool showWarningVisual = false)
    {
        Show(body, onClosed, null, continueText, string.Empty, showWarningVisual);
    }

    public void ShowConfirmation(string body, Action onConfirm, Action onCancel = null,
    string continueText = "Continue", string cancelText = "Cancel", bool showWarningVisual = false)
    {
        Show(body, onConfirm, onCancel, continueText, cancelText, showWarningVisual);
    }

    public static bool TryCancelShowingPopup()
    {
        if (s_showingPopup && s_showingPopup.CancelFromInput())
            return true;

        var popups = UnityEngine.Object.FindObjectsByType<ConfirmationPopupUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < popups.Length; i++)
        {
            if (popups[i] && popups[i].CancelFromInput())
                return true;
        }

        return false;
    }

    public static bool TryGetShowingRoot(out GameObject root)
    {
        root = null;
        if (!s_showingPopup || !s_showingPopup.IsShowing)
            return false;

        root = s_showingPopup.NavigationRoot;
        return root != null;
    }

    public bool CancelFromInput()
    {
        if (!_ownsCurrentPopup || !IsShowing)
            return false;

        var cancel = _onCancel;
        var confirm = _onConfirm;

        Hide();

        if (cancel != null)
            cancel.Invoke();
        else
            confirm?.Invoke();

        return true;
    }

    void Show(string body, Action onConfirm, Action onCancel, string continueText, string cancelText, bool showWarningVisual)
    {
        EnsureReferences();

        if (!popupView)
        {
            Debug.LogWarning("ConfirmationPopupUI: No TutorialPopupView was found for confirmation popup.");
            onConfirm?.Invoke();
            return;
        }

        popupView.TryClaimOwner(this, replaceExisting: true);

        if (popupView.IsShowing)
        {
            Debug.LogWarning("ConfirmationPopupUI: Replacing an already visible confirmation popup.");
            Hide();
            popupView.TryClaimOwner(this, replaceExisting: true);
        }

        CleanupListeners();
        CapturePopupLayoutIfNeeded();
        CaptureButtonLayoutsIfNeeded();
        ApplyButtonLayout(onCancel != null);

        _onConfirm = onConfirm;
        _onCancel = onCancel;
        _ownsCurrentPopup = true;
        s_showingPopup = this;

        popupView.ApplyPresetPosition(anchorPreset, anchoredPosition);
        ApplyConfirmationPopupLayout();
        popupView.SetVisibleAlpha(popupAlpha);
        SetBodyTextStrict(body ?? string.Empty);
        popupView.SetContinueVisible(true);
        popupView.SetContinueInteractable(true);
        popupView.SetSkipVisible(onCancel != null);
        popupView.SetStepState(waitingForInteraction: false, readyToContinue: true);

        SetLabel(continueButtonLabel, continueText);
        SetLabel(cancelButtonLabel, cancelText);

        if (popupView.ContinueButton)
            popupView.ContinueButton.onClick.AddListener(OnConfirmPressed);

        if (popupView.SkipButton)
            popupView.SkipButton.onClick.AddListener(OnCancelPressed);

        SetWarningVisualVisible(false);

        _explicitShowInProgress = true;
        try
        {
            popupView.Show(owner: this);
        }
        finally
        {
            _explicitShowInProgress = false;
        }

        if (showWarningVisual)
            popupView.SuppressDimmerVisuals();

        SetBodyTextStrict(body ?? string.Empty);
        ApplyConfirmationPopupLayout();
        SetWarningVisualVisible(showWarningVisual);
        UpdatePopupNavigationSelection();
    }

    void OnConfirmPressed()
    {
        var cb = _onConfirm;
        Hide();
        cb?.Invoke();
    }

    void OnCancelPressed()
    {
        var cb = _onCancel;
        Hide();
        cb?.Invoke();
    }

    void Hide()
    {
        CleanupListeners();

        SetWarningVisualVisible(false);

        RestoreButtonLayouts();
        popupView?.Hide(owner: this);
        RestorePopupLayout();
        _ownsCurrentPopup = false;
        popupView?.ReleaseOwner(this);

        if (s_showingPopup == this)
            s_showingPopup = null;
    }

    void CleanupListeners()
    {
        if (popupView && popupView.ContinueButton)
            popupView.ContinueButton.onClick.RemoveListener(OnConfirmPressed);

        if (popupView && popupView.SkipButton)
            popupView.SkipButton.onClick.RemoveListener(OnCancelPressed);

        _onConfirm = null;
        _onCancel = null;
    }

    void CaptureButtonLayoutsIfNeeded()
    {
        if (_buttonLayoutsCaptured || !popupView)
            return;

        _continueButtonDefaultLayout = ButtonLayoutState.Capture(popupView.ContinueButton);
        _cancelButtonDefaultLayout = ButtonLayoutState.Capture(popupView.SkipButton);
        _buttonLayoutsCaptured = true;
    }

    void CapturePopupLayoutIfNeeded()
    {
        if (_popupLayoutCaptured || !popupView)
            return;

        _popupDefaultLayout = RectTransformLayoutState.Capture(popupView.PopupRectTransform);
        _popupLayoutCaptured = true;
    }

    void ApplyConfirmationPopupLayout()
    {
        if (!popupView || !popupView.PopupRectTransform)
            return;

        RectTransform popupRect = popupView.PopupRectTransform;
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = anchorPreset == TutorialPopupView.PopupAnchorPreset.Custom
            ? anchoredPosition
            : Vector2.zero;

        CenterDedicatedWarningMessageFrame();
    }

    void ApplyButtonLayout(bool hasCancel)
    {
        if (dedicatedWarningInstance)
        {
            ApplyCenteredWarningButtonLayout(hasCancel);
            return;
        }

        if (hasCancel)
        {
            RestoreButtonLayouts();
            return;
        }

        var continueRect = popupView && popupView.ContinueButton
            ? popupView.ContinueButton.transform as RectTransform
            : null;

        if (!continueRect)
            return;

        var position = continueRect.anchoredPosition;
        position.x = 0f;
        continueRect.anchoredPosition = position;
    }

    void ApplyCenteredWarningButtonLayout(bool hasCancel)
    {
        var continueRect = popupView && popupView.ContinueButton
            ? popupView.ContinueButton.transform as RectTransform
            : null;

        var cancelRect = popupView && popupView.SkipButton
            ? popupView.SkipButton.transform as RectTransform
            : null;

        if (!continueRect)
            return;

        float y = continueRect.anchoredPosition.y;
        CenterPopupChildRect(continueRect);

        if (!hasCancel || !cancelRect)
        {
            continueRect.anchoredPosition = new Vector2(0f, y);
            return;
        }

        CenterPopupChildRect(cancelRect);

        float continueWidth = GetRectWidth(continueRect);
        float cancelWidth = GetRectWidth(cancelRect);
        continueRect.anchoredPosition = new Vector2(-(cancelWidth + WarningButtonGap) * 0.5f, y);
        cancelRect.anchoredPosition = new Vector2((continueWidth + WarningButtonGap) * 0.5f, y);
    }

    void RestoreButtonLayouts()
    {
        if (!_buttonLayoutsCaptured || !popupView)
            return;

        _continueButtonDefaultLayout.Apply(popupView.ContinueButton);
        _cancelButtonDefaultLayout.Apply(popupView.SkipButton);
    }

    void RestorePopupLayout()
    {
        if (!_popupLayoutCaptured || !popupView)
            return;

        _popupDefaultLayout.Apply(popupView.PopupRectTransform);
    }

    void EnsureReferences()
    {
        if (!popupView)
            popupView = GetComponent<TutorialPopupView>();

        if (!popupView)
            popupView = dedicatedWarningInstance
                ? UnityEngine.Object.FindFirstObjectByType<TutorialPopupView>(FindObjectsInactive.Include)
                : FindTemplateTutorialPopupView();

        if (!continueButtonLabel && popupView && popupView.ContinueButton)
            continueButtonLabel = popupView.ContinueButton.GetComponentInChildren<TMP_Text>(true);

        if (!cancelButtonLabel && popupView && popupView.SkipButton)
            cancelButtonLabel = popupView.SkipButton.GetComponentInChildren<TMP_Text>(true);

        EnsureWarningVisual();
    }

    bool IsDedicatedWarningCandidate()
    {
        EnsureReferences();
        return dedicatedWarningInstance && popupView && popupView.IsReservedForWarnings;
    }

    static ConfirmationPopupUI CreateDedicatedWarningInstance(ConfirmationPopupUI template)
    {
        if (!template)
            return null;

        template.EnsureReferences();
        var sourcePopup = template.popupView;
        var sourceObject = sourcePopup ? sourcePopup.gameObject : template.gameObject;
        if (!sourceObject)
            return null;

        Transform parent = sourceObject.transform.parent;
        var clone = UnityEngine.Object.Instantiate(sourceObject, parent);
        clone.name = RuntimeWarningPopupName;

        var created = clone.GetComponent<ConfirmationPopupUI>();
        if (!created)
            created = clone.AddComponent<ConfirmationPopupUI>();

        created.dedicatedWarningInstance = true;
        created.popupView = clone.GetComponent<TutorialPopupView>();
        created.EnsureReferences();
        created.ConfigureDedicatedWarningVisuals();

        if (created.popupView)
        {
            created.popupView.SetReservedForWarnings(true);
            created.popupView.TryClaimOwner(created, replaceExisting: true);
            created.popupView.ClearContent(created);
            created.ClearBodyTextStrict();
            created.popupView.Hide(true, created);
            created.popupView.ReleaseOwner(created);
        }

        clone.SetActive(false);
        return created;
    }

    static TutorialPopupView FindTemplateTutorialPopupView()
    {
        var views = UnityEngine.Object.FindObjectsByType<TutorialPopupView>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < views.Length; i++)
        {
            var view = views[i];
            if (view && !view.IsReservedForWarnings)
                return view;
        }

        return null;
    }

    void ConfigureDedicatedWarningVisuals()
    {
        if (!popupView)
            return;

        HideDeepChild(popupView.transform, GuidePortraitRootName);
        HideDeepChild(popupView.transform, GuidePortraitImageName);
        HideDeepChild(popupView.transform, GuideNameTextName);
        HideDeepChild(popupView.transform, GuideShadowNameTextName);
        ResolveWarningBodyText();
        CenterDedicatedWarningMessageFrame();
    }

    void CenterDedicatedWarningMessageFrame()
    {
        if (!dedicatedWarningInstance || !popupView)
            return;

        RectTransform frameRect = FindDeepChild(popupView.transform, TextFrameRootObjectName) as RectTransform;
        if (!frameRect)
        {
            var bodyLabel = ResolveWarningBodyText();
            frameRect = bodyLabel && bodyLabel.transform.parent
                ? bodyLabel.transform.parent as RectTransform
                : null;
        }

        CenterPopupChildRect(frameRect);
    }

    void SetBodyTextStrict(string body)
    {
        if (body == null)
            body = string.Empty;

        popupView.SetContent(body, this);

        var bodyLabel = ResolveWarningBodyText();
        if (!bodyLabel)
            return;

        string localized = TetrabeastsLocalization.LocalizeText(body);
        bodyLabel.gameObject.SetActive(true);
        bodyLabel.enabled = true;
        bodyLabel.richText = true;
        bodyLabel.text = TetrabeastsControls.ResolveControlPromptTokens(localized);

        var color = bodyLabel.color;
        if (color.a <= 0.001f)
        {
            color.a = 1f;
            bodyLabel.color = color;
        }

        bodyLabel.ForceMeshUpdate(true, true);
    }

    void ClearBodyTextStrict()
    {
        var bodyLabel = ResolveWarningBodyText();
        if (!bodyLabel)
            return;

        bodyLabel.text = string.Empty;
        bodyLabel.ForceMeshUpdate(true, true);
    }

    TMP_Text ResolveWarningBodyText()
    {
        if (_warningBodyText && popupView && _warningBodyText.transform.IsChildOf(popupView.transform))
            return _warningBodyText;

        _warningBodyText = null;

        if (!popupView)
            return null;

        Transform prompt = FindDeepChild(popupView.transform, BodyTextObjectName);
        if (prompt)
            _warningBodyText = prompt.GetComponent<TMP_Text>();

        if (_warningBodyText)
            return _warningBodyText;

        var labels = popupView.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text fallback = null;
        float fallbackWidth = -1f;

        for (int i = 0; i < labels.Length; i++)
        {
            var label = labels[i];
            if (!label || label == continueButtonLabel || label == cancelButtonLabel)
                continue;

            if (IsButtonLabel(label, popupView.ContinueButton) || IsButtonLabel(label, popupView.SkipButton))
                continue;

            if (string.Equals(label.name, GuideNameTextName, StringComparison.Ordinal) ||
                string.Equals(label.name, GuideShadowNameTextName, StringComparison.Ordinal))
                continue;

            if (label.name.IndexOf("Prompt", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _warningBodyText = label;
                return _warningBodyText;
            }

            var rect = label.transform as RectTransform;
            float width = rect ? rect.rect.width : 0f;
            if (width > fallbackWidth)
            {
                fallback = label;
                fallbackWidth = width;
            }
        }

        _warningBodyText = fallback;
        return _warningBodyText;
    }

    static bool IsButtonLabel(TMP_Text label, Button button)
    {
        return label && button && label.transform.IsChildOf(button.transform);
    }

    static void HideDeepChild(Transform root, string childName)
    {
        Transform child = FindDeepChild(root, childName);
        if (child)
            child.gameObject.SetActive(false);
    }

    void EnsureWarningVisual()
    {
        Transform layerParent = GetWarningLayerParent();
        if (!layerParent)
            return;

        if (warningVisual && IsTransformInsidePopup(warningVisual.transform))
        {
            SuppressGraphic(warningVisual.GetComponent<Graphic>());
            warningVisual.SetActive(false);
            warningVisual = null;
        }

        if (warningVisual)
            return;

        warningVisual = FindDirectChild(layerParent, RuntimeWarningDimmerName)?.gameObject;

        if (!warningVisual)
            warningVisual = CreateRuntimeWarningDimmer(layerParent);

        ConfigureWarningVisual(false);
    }

    GameObject CreateRuntimeWarningDimmer(Transform layerParent)
    {
        if (!layerParent)
            return null;

        var go = new GameObject(RuntimeWarningDimmerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(layerParent, false);
        go.transform.SetAsFirstSibling();

        var rect = go.transform as RectTransform;
        StretchToParent(rect);

        return go;
    }

    void SetWarningVisualVisible(bool visible)
    {
        EnsureWarningVisual();
        if (!warningVisual)
            return;

        ConfigureWarningVisual(visible);
        warningVisual.SetActive(visible);

        if (visible)
            PlaceWarningVisualUnderPopup();
    }

    void ConfigureWarningVisual(bool visible)
    {
        if (!warningVisual)
            return;

        var image = warningVisual.GetComponent<Image>();
        if (!image)
            image = warningVisual.AddComponent<Image>();

        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = new Color(0f, 0f, 0f, visible ? warningDimmerAlpha : 0f);
        image.raycastTarget = visible;

        StretchToParent(warningVisual.transform as RectTransform);
    }

    void UpdatePopupNavigationSelection()
    {
        if (!EventSystem.current || !popupView)
            return;

        if (!UICursorController.ShouldKeepNavigationSelection)
        {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        Button button = popupView.ContinueButton && popupView.ContinueButton.gameObject.activeInHierarchy
            ? popupView.ContinueButton
            : popupView.SkipButton;

        if (!button || !button.gameObject.activeInHierarchy || !button.interactable)
            return;

        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    Transform GetWarningLayerParent()
    {
        var canvas = popupView
            ? popupView.GetComponentInParent<Canvas>(true)
            : GetComponentInParent<Canvas>(true);

        if (canvas)
            return canvas.transform;

        return popupView && popupView.transform.parent
            ? popupView.transform.parent
            : transform.parent;
    }

    void PlaceWarningVisualUnderPopup()
    {
        if (!warningVisual)
            return;

        Transform layerParent = GetWarningLayerParent();
        RectTransform warningRect = warningVisual.transform as RectTransform;
        if (layerParent && warningVisual.transform.parent != layerParent)
            warningVisual.transform.SetParent(layerParent, false);

        StretchToParent(warningRect);

        Transform popupLayerSibling = GetLayerSiblingTransform(
            popupView ? popupView.transform : transform,
            warningVisual.transform.parent);

        if (popupLayerSibling)
        {
            warningVisual.transform.SetSiblingIndex(Mathf.Max(0, popupLayerSibling.GetSiblingIndex()));
            popupLayerSibling.SetAsLastSibling();
        }
        else
        {
            warningVisual.transform.SetAsFirstSibling();
        }
    }

    bool IsTransformInsidePopup(Transform candidate)
    {
        return candidate && popupView && candidate.IsChildOf(popupView.transform);
    }

    static void StretchToParent(RectTransform rect)
    {
        if (!rect)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    static void CenterPopupChildRect(RectTransform rect)
    {
        if (!rect)
            return;

        var position = rect.anchoredPosition;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, position.y);
    }

    static float GetRectWidth(RectTransform rect)
    {
        if (!rect)
            return 180f;

        float width = Mathf.Abs(rect.sizeDelta.x);
        if (width <= 0.001f)
            width = Mathf.Abs(rect.rect.width);

        return width > 0.001f ? width : 180f;
    }

    static Transform FindDirectChild(Transform root, string childName)
    {
        if (!root || string.IsNullOrEmpty(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child && string.Equals(child.name, childName, StringComparison.Ordinal))
                return child;
        }

        return null;
    }

    static Transform FindDeepChild(Transform root, string childName)
    {
        if (!root || string.IsNullOrEmpty(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child && string.Equals(child.name, childName, StringComparison.Ordinal))
                return child;

            Transform nested = FindDeepChild(child, childName);
            if (nested)
                return nested;
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

    static void SuppressGraphic(Graphic graphic)
    {
        if (!graphic)
            return;

        var color = graphic.color;
        color.a = 0f;
        graphic.color = color;
        graphic.raycastTarget = false;
    }

    static void SetLabel(TMP_Text label, string text)
    {
        if (label)
            label.text = TetrabeastsLocalization.LocalizeText(text ?? string.Empty);
    }

    struct ButtonLayoutState
    {
        public bool IsValid;
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;
        public Vector2 Pivot;

        public static ButtonLayoutState Capture(Button button)
        {
            var rect = button ? button.transform as RectTransform : null;
            if (!rect)
                return default;

            return new ButtonLayoutState
            {
                IsValid = true,
                AnchorMin = rect.anchorMin,
                AnchorMax = rect.anchorMax,
                AnchoredPosition = rect.anchoredPosition,
                SizeDelta = rect.sizeDelta,
                Pivot = rect.pivot
            };
        }

        public void Apply(Button button)
        {
            if (!IsValid)
                return;

            var rect = button ? button.transform as RectTransform : null;
            if (!rect)
                return;

            rect.anchorMin = AnchorMin;
            rect.anchorMax = AnchorMax;
            rect.anchoredPosition = AnchoredPosition;
            rect.sizeDelta = SizeDelta;
            rect.pivot = Pivot;
        }
    }

    struct RectTransformLayoutState
    {
        public bool IsValid;
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;
        public Vector2 Pivot;

        public static RectTransformLayoutState Capture(RectTransform rect)
        {
            if (!rect)
                return default;

            return new RectTransformLayoutState
            {
                IsValid = true,
                AnchorMin = rect.anchorMin,
                AnchorMax = rect.anchorMax,
                AnchoredPosition = rect.anchoredPosition,
                SizeDelta = rect.sizeDelta,
                Pivot = rect.pivot
            };
        }

        public void Apply(RectTransform rect)
        {
            if (!IsValid || !rect)
                return;

            rect.anchorMin = AnchorMin;
            rect.anchorMax = AnchorMax;
            rect.anchoredPosition = AnchoredPosition;
            rect.sizeDelta = SizeDelta;
            rect.pivot = Pivot;
        }
    }
}
