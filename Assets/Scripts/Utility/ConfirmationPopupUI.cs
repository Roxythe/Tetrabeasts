using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ConfirmationPopupUI : MonoBehaviour
{
    [SerializeField] TutorialPopupView popupView;
    [SerializeField] TMP_Text continueButtonLabel;
    [SerializeField] TMP_Text cancelButtonLabel;
    [SerializeField] GameObject warningVisual;
    [SerializeField] TutorialPopupView.PopupAnchorPreset anchorPreset = TutorialPopupView.PopupAnchorPreset.Default;
    [SerializeField] Vector2 anchoredPosition;
    [SerializeField, Range(0.1f, 1f)] float popupAlpha = 1f;

    Action _onConfirm;
    Action _onCancel;
    bool _explicitShowInProgress;
    bool _ownsCurrentPopup;
    ButtonLayoutState _continueButtonDefaultLayout;
    ButtonLayoutState _cancelButtonDefaultLayout;
    bool _buttonLayoutsCaptured;

    public bool IsShowing => popupView && popupView.IsShowing;

    void Awake()
    {
        EnsureReferences();

        if (warningVisual)
            warningVisual.SetActive(false);

        if (!_explicitShowInProgress)
            popupView?.Hide(true);
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
            Debug.LogWarning("ConfirmationPopupUI: Popup requested while another popup is already visible.");
            return;
        }

        CleanupListeners();
        CaptureButtonLayoutsIfNeeded();
        ApplyButtonLayout(onCancel != null);

        _onConfirm = onConfirm;
        _onCancel = onCancel;
        _ownsCurrentPopup = true;

        popupView.ApplyPresetPosition(anchorPreset, anchoredPosition);
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

        if (warningVisual)
            warningVisual.SetActive(false);

        _explicitShowInProgress = true;
        try
        {
            popupView.Show();
        }
        finally
        {
            _explicitShowInProgress = false;
        }

        if (warningVisual)
            warningVisual.SetActive(showWarningVisual);
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

        if (warningVisual)
            warningVisual.SetActive(false);

        RestoreButtonLayouts();
        popupView?.Hide();
        _ownsCurrentPopup = false;
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
}
