using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ConfirmationPopupUI : MonoBehaviour
{
    const string RuntimeWarningDimmerName = "Warning_Dimmer_Panel";
    static ConfirmationPopupUI s_showingPopup;

    [SerializeField] TutorialPopupView popupView;
    [SerializeField] TMP_Text continueButtonLabel;
    [SerializeField] TMP_Text cancelButtonLabel;
    [SerializeField] GameObject warningVisual;
    [SerializeField] TutorialPopupView.PopupAnchorPreset anchorPreset = TutorialPopupView.PopupAnchorPreset.Default;
    [SerializeField] Vector2 anchoredPosition;
    [SerializeField, Range(0.1f, 1f)] float popupAlpha = 1f;
    [SerializeField, Range(0f, 1f)] float warningDimmerAlpha = 0.65f;

    Action _onConfirm;
    Action _onCancel;
    bool _explicitShowInProgress;
    bool _ownsCurrentPopup;
    ButtonLayoutState _continueButtonDefaultLayout;
    ButtonLayoutState _cancelButtonDefaultLayout;
    RectTransformLayoutState _popupDefaultLayout;
    bool _buttonLayoutsCaptured;
    bool _popupLayoutCaptured;

    public bool IsShowing => popupView && popupView.IsShowing;
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

        SetWarningVisualVisible(false);

        if (!_explicitShowInProgress)
            popupView?.Hide(true);
    }

    void OnDisable()
    {
        if (s_showingPopup == this)
            s_showingPopup = null;
    }

    public static ConfirmationPopupUI FindOrCreate()
    {
        var existing = UnityEngine.Object.FindFirstObjectByType<ConfirmationPopupUI>(FindObjectsInactive.Include);
        if (existing)
            return existing;

        var popup = UnityEngine.Object.FindFirstObjectByType<TutorialPopupView>(FindObjectsInactive.Include);
        if (!popup)
            return null;

        var created = popup.GetComponent<ConfirmationPopupUI>();
        if (!created)
            created = popup.gameObject.AddComponent<ConfirmationPopupUI>();

        created.EnsureReferences();
        return created;
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
        var existing = UnityEngine.Object.FindFirstObjectByType<ConfirmationPopupUI>(FindObjectsInactive.Include);
        return existing && existing.CancelFromInput();
    }

    public static bool TryGetShowingRoot(out GameObject root)
    {
        root = null;
        var existing = UnityEngine.Object.FindFirstObjectByType<ConfirmationPopupUI>(FindObjectsInactive.Include);
        if (!existing || !existing.IsShowing)
            return false;

        root = existing.NavigationRoot;
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

        if (popupView.IsShowing)
        {
            Debug.LogWarning("ConfirmationPopupUI: Replacing an already visible confirmation popup.");
            Hide();
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
        popupView.SetContent(body ?? string.Empty);
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
            popupView.Show();
        }
        finally
        {
            _explicitShowInProgress = false;
        }

        if (showWarningVisual)
            popupView.SuppressDimmerVisuals();

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
        popupView?.Hide();
        RestorePopupLayout();
        _ownsCurrentPopup = false;

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
    }

    void ApplyButtonLayout(bool hasCancel)
    {
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
            popupView = UnityEngine.Object.FindFirstObjectByType<TutorialPopupView>(FindObjectsInactive.Include);

        if (!continueButtonLabel && popupView && popupView.ContinueButton)
            continueButtonLabel = popupView.ContinueButton.GetComponentInChildren<TMP_Text>(true);

        if (!cancelButtonLabel && popupView && popupView.SkipButton)
            cancelButtonLabel = popupView.SkipButton.GetComponentInChildren<TMP_Text>(true);

        EnsureWarningVisual();
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
