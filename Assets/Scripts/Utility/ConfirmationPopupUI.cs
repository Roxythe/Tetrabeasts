using System;
using TMPro;
using UnityEngine;

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

    public bool IsShowing => popupView && popupView.IsShowing;

    void Awake()
    {
        EnsureReferences();

        if (warningVisual)
            warningVisual.SetActive(false);

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
        if (!IsShowing)
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

        _onConfirm = onConfirm;
        _onCancel = onCancel;

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

        popupView.Show();

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

        popupView?.Hide();
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
            label.text = text ?? string.Empty;
    }
}
