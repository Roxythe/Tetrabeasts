using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialPopupView : MonoBehaviour
{
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

    [Header("Optional Status Visuals")]
    [SerializeField] GameObject waitingVisual;
    [SerializeField] GameObject readyVisual;

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

        if (!canvasGroup)
            canvasGroup = GetComponent<CanvasGroup>();

        if (!canvasGroup)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (popupRoot)
            defaultAnchoredPosition = popupRoot.anchoredPosition;
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if (!canvasGroup)
            return;

        canvasGroup.alpha = visibleAlpha;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        if (!canvasGroup) return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void SetContent(string body)
    {
        if (bodyText)
            bodyText.text = body ?? string.Empty;
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
}
