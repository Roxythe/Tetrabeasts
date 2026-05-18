using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class TriggeredTutorialPopupController : MonoBehaviour
{
    struct PendingRequest
    {
        public string tutorialId;
        public string body;
        public List<string> bodyPages;
        public TutorialPopupView.PopupAnchorPreset popupAnchorPreset;
        public Vector2 popupAnchoredPosition;
        public float popupAlpha;
        public bool pauseGameplay;
        public bool freezePieceGravity;
        public bool allowSkip;
        public RectTransform highlightTarget;
        public List<RectTransform> highlightTargets;
        public Vector2 highlightPadding;
        public System.Action onClosed;
    }

    [SerializeField] GameController gameController;
    [SerializeField] TutorialPopupView popupView;
    [SerializeField] TutorialHighlightView highlightView;
    [SerializeField] KeyCode dismissTutorialKey = KeyCode.F;
    [SerializeField] bool ignoreSavedCompletionForTesting = true;

    readonly Queue<PendingRequest> _queue = new();
    readonly HashSet<string> _pendingTutorialIds = new();

    bool _processingQueue;
    bool _popupShowing;
    bool _anonymousPopupQueuedOrShowing;
    string _activeTutorialId;
    public bool IsPopupShowing => _popupShowing;

    void Awake()
    {
        EnsureReferences();
    }

    public void QueueShowOnce(
        string tutorialId,
        string body,
        TutorialPopupView.PopupAnchorPreset popupAnchorPreset = TutorialPopupView.PopupAnchorPreset.Default,
        Vector2 defaultPopupAnchoredPosition = default,
        float popupAlpha = 1f,
        bool pauseGameplay = true,
        bool freezePieceGravity = false,
        bool allowSkip = true,
        RectTransform highlightTarget = null,
        Vector2 highlightPadding = default)
    {
        if (!ShouldQueueTutorial(tutorialId))
            return;

        Enqueue(new PendingRequest
        {
            tutorialId = tutorialId,
            body = body,
            popupAnchorPreset = popupAnchorPreset,
            popupAnchoredPosition = defaultPopupAnchoredPosition,
            popupAlpha = popupAlpha,
            pauseGameplay = pauseGameplay,
            freezePieceGravity = freezePieceGravity,
            allowSkip = allowSkip,
            highlightTarget = highlightTarget,
            highlightPadding = highlightPadding
        });
    }

    // Overload for multiple highlight targets
    public void QueueShowOnce(
    string tutorialId,
    string body,
    TutorialPopupView.PopupAnchorPreset popupAnchorPreset,
    Vector2 defaultPopupAnchoredPosition,
    float popupAlpha,
    bool pauseGameplay,
    bool freezePieceGravity,
    bool allowSkip,
    IReadOnlyList<RectTransform> highlightTargets,
    Vector2 highlightPadding)
    {
        if (!ShouldQueueTutorial(tutorialId))
            return;

        Enqueue(new PendingRequest
        {
            tutorialId = tutorialId,
            body = body,
            popupAnchorPreset = popupAnchorPreset,
            popupAnchoredPosition = defaultPopupAnchoredPosition,
            popupAlpha = popupAlpha,
            pauseGameplay = pauseGameplay,
            freezePieceGravity = freezePieceGravity,
            allowSkip = allowSkip,
            highlightTarget = null,
            highlightTargets = highlightTargets != null ? new List<RectTransform>(highlightTargets) : null,
            highlightPadding = highlightPadding
        });
    }

    public IEnumerator ShowOnceAndWait(
        string tutorialId,
        string body,
        TutorialPopupView.PopupAnchorPreset popupAnchorPreset = TutorialPopupView.PopupAnchorPreset.Default,
        Vector2 defaultPopupAnchoredPosition = default,
        float popupAlpha = 1f,
        bool pauseGameplay = true,
        bool freezePieceGravity = false,
        bool allowSkip = true,
        RectTransform highlightTarget = null,
        Vector2 highlightPadding = default)
    {
        if (!ignoreSavedCompletionForTesting && PlayerProgress.I != null && PlayerProgress.I.IsTutorialCompleted(tutorialId))
            yield break;

        if (!string.IsNullOrWhiteSpace(tutorialId) &&
            (_activeTutorialId == tutorialId || _pendingTutorialIds.Contains(tutorialId)))
        {
            yield return new WaitUntil(() => _activeTutorialId != tutorialId && !_pendingTutorialIds.Contains(tutorialId));
            yield break;
        }

        bool closed = false;

        Enqueue(new PendingRequest
        {
            tutorialId = tutorialId,
            body = body,
            popupAnchorPreset = popupAnchorPreset,
            popupAnchoredPosition = defaultPopupAnchoredPosition,
            popupAlpha = popupAlpha,
            pauseGameplay = pauseGameplay,
            freezePieceGravity = freezePieceGravity,
            allowSkip = allowSkip,
            highlightTarget = highlightTarget,
            highlightPadding = highlightPadding,
            onClosed = () => closed = true
        });

        yield return new WaitUntil(() => closed);
    }

    // Overload for multiple highlight targets
    public IEnumerator ShowOnceAndWait(
    string tutorialId,
    string body,
    TutorialPopupView.PopupAnchorPreset popupAnchorPreset,
    Vector2 defaultPopupAnchoredPosition,
    float popupAlpha,
    bool pauseGameplay,
    bool freezePieceGravity,
    bool allowSkip,
    IReadOnlyList<RectTransform> highlightTargets,
    Vector2 highlightPadding)
    {
        if (!ignoreSavedCompletionForTesting && PlayerProgress.I != null && PlayerProgress.I.IsTutorialCompleted(tutorialId))
            yield break;

        if (!string.IsNullOrWhiteSpace(tutorialId) &&
            (_activeTutorialId == tutorialId || _pendingTutorialIds.Contains(tutorialId)))
        {
            yield return new WaitUntil(() => _activeTutorialId != tutorialId && !_pendingTutorialIds.Contains(tutorialId));
            yield break;
        }

        bool closed = false;

        Enqueue(new PendingRequest
        {
            tutorialId = tutorialId,
            body = body,
            popupAnchorPreset = popupAnchorPreset,
            popupAnchoredPosition = defaultPopupAnchoredPosition,
            popupAlpha = popupAlpha,
            pauseGameplay = pauseGameplay,
            freezePieceGravity = freezePieceGravity,
            allowSkip = allowSkip,
            highlightTarget = null,
            highlightTargets = highlightTargets != null ? new List<RectTransform>(highlightTargets) : null,
            highlightPadding = highlightPadding,
            onClosed = () => closed = true
        });

        yield return new WaitUntil(() => closed);
    }

    // Overloads multiple body text pages
    public IEnumerator ShowOnceAndWait(
    string tutorialId,
    IReadOnlyList<string> bodyPages,
    TutorialPopupView.PopupAnchorPreset popupAnchorPreset,
    Vector2 defaultPopupAnchoredPosition,
    float popupAlpha,
    bool pauseGameplay,
    bool freezePieceGravity,
    bool allowSkip,
    IReadOnlyList<RectTransform> highlightTargets,
    Vector2 highlightPadding)
    {
        if (!ignoreSavedCompletionForTesting && PlayerProgress.I != null && PlayerProgress.I.IsTutorialCompleted(tutorialId))
            yield break;

        if (!string.IsNullOrWhiteSpace(tutorialId) &&
            (_activeTutorialId == tutorialId || _pendingTutorialIds.Contains(tutorialId)))
        {
            yield return new WaitUntil(() => _activeTutorialId != tutorialId && !_pendingTutorialIds.Contains(tutorialId));
            yield break;
        }

        bool closed = false;

        Enqueue(new PendingRequest
        {
            tutorialId = tutorialId,
            body = null,
            bodyPages = bodyPages != null ? new List<string>(bodyPages) : null,
            popupAnchorPreset = popupAnchorPreset,
            popupAnchoredPosition = defaultPopupAnchoredPosition,
            popupAlpha = popupAlpha,
            pauseGameplay = pauseGameplay,
            freezePieceGravity = freezePieceGravity,
            allowSkip = allowSkip,
            highlightTarget = null,
            highlightTargets = highlightTargets != null ? new List<RectTransform>(highlightTargets) : null,
            highlightPadding = highlightPadding,
            onClosed = () => closed = true
        });

        yield return new WaitUntil(() => closed);
    }

    void Enqueue(PendingRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.tutorialId))
            _pendingTutorialIds.Add(request.tutorialId);
        else
            _anonymousPopupQueuedOrShowing = true;

        _queue.Enqueue(request);

        if (!_processingQueue)
            StartCoroutine(ProcessQueue());
    }

    bool ShouldQueueTutorial(string tutorialId)
    {
        if (string.IsNullOrWhiteSpace(tutorialId))
            return !_anonymousPopupQueuedOrShowing;

        if (_activeTutorialId == tutorialId || _pendingTutorialIds.Contains(tutorialId))
            return false;

        return ignoreSavedCompletionForTesting || PlayerProgress.I == null || !PlayerProgress.I.IsTutorialCompleted(tutorialId);
    }

    IEnumerator ProcessQueue()
    {
        _processingQueue = true;

        while (_queue.Count > 0)
        {
            var request = _queue.Dequeue();
            if (!string.IsNullOrWhiteSpace(request.tutorialId))
                _pendingTutorialIds.Remove(request.tutorialId);

            if (!ignoreSavedCompletionForTesting && PlayerProgress.I != null && PlayerProgress.I.IsTutorialCompleted(request.tutorialId))
            {
                if (string.IsNullOrWhiteSpace(request.tutorialId))
                    _anonymousPopupQueuedOrShowing = false;

                request.onClosed?.Invoke();
                continue;
            }

            yield return new WaitUntil(() => !HasBlockingTutorialSequence());
            yield return new WaitUntil(CanShowQueuedPopup);

            EnsureReferences();
            if (!popupView)
            {
                Debug.LogWarning($"TriggeredTutorialPopupController: No TutorialPopupView found for tutorial '{request.tutorialId}'.");
                if (string.IsNullOrWhiteSpace(request.tutorialId))
                    _anonymousPopupQueuedOrShowing = false;

                request.onClosed?.Invoke();
                continue;
            }

            _activeTutorialId = request.tutorialId;
            _popupShowing = true;
            yield return ShowRequestRoutine(request);
            _popupShowing = false;
            if (string.IsNullOrWhiteSpace(request.tutorialId))
                _anonymousPopupQueuedOrShowing = false;

            _activeTutorialId = null;
        }

        _processingQueue = false;
    }

    IEnumerator ShowRequestRoutine(PendingRequest request)
    {
        if (!popupView)
            yield break;

        List<string> pages = request.bodyPages;
        if (pages == null || pages.Count == 0)
            pages = new List<string> { request.body ?? string.Empty };

        int pageIndex = 0;
        bool dismissed = false;

        void AdvanceOrDismiss()
        {
            if (dismissed)
                return;

            if (pageIndex < pages.Count - 1)
            {
                pageIndex++;
                if (popupView)
                    popupView.SetContent(pages[pageIndex]);
                return;
            }

            dismissed = true;
        }

        UnityAction continueAction = AdvanceOrDismiss;
        UnityAction skipAction = AdvanceOrDismiss;

        if (popupView.ContinueButton)
            popupView.ContinueButton.onClick.AddListener(continueAction);

        if (popupView.SkipButton)
            popupView.SkipButton.onClick.AddListener(skipAction);

        popupView.ApplyPresetPosition(request.popupAnchorPreset, request.popupAnchoredPosition);
        popupView.SetVisibleAlpha(request.popupAlpha);
        popupView.SetContent(pages[pageIndex]);
        popupView.SetContinueVisible(true);
        popupView.SetContinueInteractable(true);
        popupView.SetSkipVisible(request.allowSkip);
        popupView.SetStepState(waitingForInteraction: false, readyToContinue: true);
        popupView.Show();

        if (highlightView)
        {
            if (request.highlightTargets != null && request.highlightTargets.Count > 0)
                highlightView.Show(request.highlightTargets, request.highlightPadding);
            else if (request.highlightTarget)
                highlightView.Show(request.highlightTarget, request.highlightPadding);
            else
                highlightView.Hide();
        }

        SetGameplaySuspended(request.pauseGameplay, request.freezePieceGravity);

        yield return new WaitUntil(() =>
        {
            if (popupView && !popupView.IsShowing)
            {
                popupView.ApplyPresetPosition(request.popupAnchorPreset, request.popupAnchoredPosition);
                popupView.SetVisibleAlpha(request.popupAlpha);
                popupView.SetContent(pages[Mathf.Clamp(pageIndex, 0, pages.Count - 1)]);
                popupView.SetContinueVisible(true);
                popupView.SetContinueInteractable(true);
                popupView.SetSkipVisible(request.allowSkip);
                popupView.SetStepState(waitingForInteraction: false, readyToContinue: true);
                popupView.Show(true);
            }

            if (dismissed)
                return true;

            if (WasDismissKeyPressedThisFrame())
            {
                AdvanceOrDismiss();
                return dismissed;
            }

            return false;
        });

        if (popupView.ContinueButton)
            popupView.ContinueButton.onClick.RemoveListener(continueAction);

        if (popupView.SkipButton)
            popupView.SkipButton.onClick.RemoveListener(skipAction);

        popupView.Hide();
        highlightView?.Hide();
        SetGameplaySuspended(false, false);

        if (!string.IsNullOrWhiteSpace(request.tutorialId))
            PlayerProgress.I?.SetTutorialCompleted(request.tutorialId);

        request.onClosed?.Invoke();
    }

    void EnsureReferences()
    {
        if (!gameController)
            gameController = GetComponent<GameController>();

        if (!gameController)
            gameController = FindFirstObjectByType<GameController>(FindObjectsInactive.Include);

        if (!popupView)
            popupView = FindFirstObjectByType<TutorialPopupView>(FindObjectsInactive.Include);

        if (!highlightView)
            highlightView = FindFirstObjectByType<TutorialHighlightView>(FindObjectsInactive.Include);
    }

    bool HasBlockingTutorialSequence()
    {
        var sequences = FindObjectsByType<TutorialSequenceController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sequences.Length; i++)
        {
            var sequence = sequences[i];
            if (sequence && sequence.IsSequenceRunning)
                return true;
        }

        return false;
    }

    bool CanShowQueuedPopup()
    {
        EnsureReferences();
        return !gameController || !gameController.IsGameplaySuspended;
    }

    void SetGameplaySuspended(bool suspended, bool freezePieceGravity)
    {
        if (!gameController)
            return;

        gameController.SetTutorialSuspended(suspended);
        gameController.SetTutorialFreezePieceGravity(freezePieceGravity);
    }

    bool WasDismissKeyPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    var keyboard = UnityEngine.InputSystem.Keyboard.current;
    if (keyboard == null)
        return false;

    return dismissTutorialKey switch
    {
        KeyCode.F => keyboard.fKey.wasPressedThisFrame,
        KeyCode.Return => keyboard.enterKey.wasPressedThisFrame,
        KeyCode.KeypadEnter => keyboard.numpadEnterKey.wasPressedThisFrame,
        KeyCode.Space => keyboard.spaceKey.wasPressedThisFrame,
        _ => Input.GetKeyDown(dismissTutorialKey)
    };
#else
        return Input.GetKeyDown(dismissTutorialKey);
#endif
    }
}
