using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TriggeredTutorialPopupController : MonoBehaviour
{
    const string BodyTextObjectName = "TutorialPrompt_Text";

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
        public bool allowWhileGameplaySuspended;
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
    [Tooltip("Testing only. Ignores saved tutorial completion, but each matching tutorial still appears only once per run/test pass.")]
    [SerializeField] bool ignoreSavedCompletionForTesting = false;
    [SerializeField, Tooltip("Optional exact tutorial id list for testing. Leave blank or use * to replay every triggered tutorial once per run/test pass.")]
    string testingTutorialIdFilter = string.Empty;

    readonly Queue<PendingRequest> _queue = new();
    readonly HashSet<string> _pendingTutorialIds = new();

    bool _processingQueue;
    bool _popupShowing;
    bool _anonymousPopupQueuedOrShowing;
    string _activeTutorialId;
    GameObject _inputBlocker;
    RectTransform _inputBlockerRect;
    TMP_Text _popupBodyText;
    static int s_lastPopupClosedFrame = -1000;
    public bool IsPopupShowing => _popupShowing;
    public static bool IsAnyPopupShowing { get; private set; }
    public static bool IsAnyPopupBlockingUi =>
        IsAnyPopupShowing || Time.frameCount <= s_lastPopupClosedFrame;

    void Awake()
    {
        EnsureReferences();
    }

    void OnDisable()
    {
        if (_popupShowing)
            IsAnyPopupShowing = false;

        s_lastPopupClosedFrame = Time.frameCount;
        HideInputBlocker();
        popupView?.ReleaseOwner(this);
    }

    void Update()
    {
        if (_popupShowing)
            ClearSelectedUiOutsidePopup();
    }

    public void QueueShowOnce(
        string tutorialId,
        string body,
        TutorialPopupView.PopupAnchorPreset popupAnchorPreset = TutorialPopupView.PopupAnchorPreset.Default,
        Vector2 defaultPopupAnchoredPosition = default,
        float popupAlpha = 1f,
        bool pauseGameplay = true,
        bool freezePieceGravity = false,
        bool allowWhileGameplaySuspended = false,
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
            allowWhileGameplaySuspended = allowWhileGameplaySuspended,
            allowSkip = allowSkip,
            highlightTarget = highlightTarget,
            highlightPadding = highlightPadding
        });
    }

    public void QueueShowOnce(
        string tutorialId,
        IReadOnlyList<string> bodyPages,
        TutorialPopupView.PopupAnchorPreset popupAnchorPreset = TutorialPopupView.PopupAnchorPreset.Default,
        Vector2 defaultPopupAnchoredPosition = default,
        float popupAlpha = 1f,
        bool pauseGameplay = true,
        bool freezePieceGravity = false,
        bool allowWhileGameplaySuspended = false,
        bool allowSkip = true,
        RectTransform highlightTarget = null,
        Vector2 highlightPadding = default)
    {
        if (!ShouldQueueTutorial(tutorialId))
            return;

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
            allowWhileGameplaySuspended = allowWhileGameplaySuspended,
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
        bool allowWhileGameplaySuspended = false,
        bool allowSkip = true,
        RectTransform highlightTarget = null,
        Vector2 highlightPadding = default)
    {
        if (IsTutorialCompletedForCurrentMode(tutorialId))
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
            allowWhileGameplaySuspended = allowWhileGameplaySuspended,
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
        if (IsTutorialCompletedForCurrentMode(tutorialId))
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
        if (IsTutorialCompletedForCurrentMode(tutorialId))
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

        return !IsTutorialCompletedForCurrentMode(tutorialId);
    }

    IEnumerator ProcessQueue()
    {
        _processingQueue = true;

        while (_queue.Count > 0)
        {
            var request = _queue.Dequeue();
            if (!string.IsNullOrWhiteSpace(request.tutorialId))
                _pendingTutorialIds.Remove(request.tutorialId);

            if (IsTutorialCompletedForCurrentMode(request.tutorialId))
            {
                if (string.IsNullOrWhiteSpace(request.tutorialId))
                    _anonymousPopupQueuedOrShowing = false;

                request.onClosed?.Invoke();
                continue;
            }

            yield return new WaitUntil(() => !HasBlockingTutorialSequence());
            yield return new WaitUntil(() => CanShowQueuedPopup(request.allowWhileGameplaySuspended));

            EnsureReferences();
            if (!popupView)
            {
                Debug.LogWarning($"TriggeredTutorialPopupController: No TutorialPopupView found for tutorial '{request.tutorialId}'.");
                if (string.IsNullOrWhiteSpace(request.tutorialId))
                    _anonymousPopupQueuedOrShowing = false;

                request.onClosed?.Invoke();
                continue;
            }

            yield return new WaitUntil(() => popupView && popupView.TryClaimOwner(this));

            _activeTutorialId = request.tutorialId;
            _popupShowing = true;
            IsAnyPopupShowing = true;
            yield return ShowRequestRoutine(request);
            _popupShowing = false;
            IsAnyPopupShowing = false;
            s_lastPopupClosedFrame = Time.frameCount;
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
                    SetPopupBodyTextStrict(pages[pageIndex]);
                return;
            }

            dismissed = true;
        }

        popupView.ApplyPresetPosition(request.popupAnchorPreset, request.popupAnchoredPosition);
        popupView.SetVisibleAlpha(request.popupAlpha);
        SetPopupBodyTextStrict(pages[pageIndex]);
        popupView.SetContinueVisible(false);
        popupView.SetContinueInteractable(false);
        popupView.SetSkipVisible(false);
        popupView.SetStepState(waitingForInteraction: false, readyToContinue: true);
        ShowInputBlocker();
        popupView.Show(owner: this);
        SetPopupBodyTextStrict(pages[pageIndex]);
        ClearSelectedUiOutsidePopup();

        if (highlightView)
        {
            if (request.highlightTargets != null && request.highlightTargets.Count > 0)
                highlightView.Show(request.highlightTargets, request.highlightPadding);
            else if (request.highlightTarget)
                highlightView.Show(request.highlightTarget, request.highlightPadding);
            else
                highlightView.Hide();
        }

        SetGameplaySuspended(request.pauseGameplay, request.freezePieceGravity || request.pauseGameplay);

        yield return new WaitUntil(() =>
        {
            if (popupView && !popupView.IsShowing)
            {
                popupView.ApplyPresetPosition(request.popupAnchorPreset, request.popupAnchoredPosition);
                popupView.SetVisibleAlpha(request.popupAlpha);
                SetPopupBodyTextStrict(pages[Mathf.Clamp(pageIndex, 0, pages.Count - 1)]);
                popupView.SetContinueVisible(false);
                popupView.SetContinueInteractable(false);
                popupView.SetSkipVisible(false);
                popupView.SetStepState(waitingForInteraction: false, readyToContinue: true);
                ShowInputBlocker();
                popupView.Show(true, this);
                SetPopupBodyTextStrict(pages[Mathf.Clamp(pageIndex, 0, pages.Count - 1)]);
            }

            ClearSelectedUiOutsidePopup();

            if (dismissed)
                return true;

            if (WasDismissKeyPressedThisFrame())
            {
                AdvanceOrDismiss();
                return dismissed;
            }

            return false;
        });

        popupView.Hide(owner: this);
        popupView.ReleaseOwner(this);
        HideInputBlocker();
        highlightView?.Hide();
        SetGameplaySuspended(false, false);

        if (!string.IsNullOrWhiteSpace(request.tutorialId))
        {
            if (IsTestingBypassEnabledFor(request.tutorialId))
                TutorialTestingScope.MarkCompletedThisTestPass(request.tutorialId);
            else
                PlayerProgress.Ensure()?.SetTutorialCompleted(request.tutorialId);
        }

        request.onClosed?.Invoke();
    }

    void SetPopupBodyTextStrict(string body)
    {
        if (!popupView)
            return;

        body ??= string.Empty;
        popupView.SetContent(body, this);

        var bodyLabel = ResolvePopupBodyText();
        if (!bodyLabel)
            return;

        bodyLabel.gameObject.SetActive(true);
        bodyLabel.enabled = true;
        bodyLabel.richText = true;
        bodyLabel.text = popupView.FormatBodyForDisplay(body, updateContinueCue: true);

        var color = bodyLabel.color;
        if (color.a <= 0.001f)
        {
            color.a = 1f;
            bodyLabel.color = color;
        }

        bodyLabel.ForceMeshUpdate(true, true);
    }

    TMP_Text ResolvePopupBodyText()
    {
        if (_popupBodyText && popupView && _popupBodyText.transform.IsChildOf(popupView.transform))
            return _popupBodyText;

        _popupBodyText = null;

        if (!popupView)
            return null;

        Transform prompt = FindDeepChild(popupView.transform, BodyTextObjectName);
        if (prompt)
            _popupBodyText = prompt.GetComponent<TMP_Text>();

        if (_popupBodyText)
            return _popupBodyText;

        var labels = popupView.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text fallback = null;
        float fallbackWidth = -1f;

        for (int i = 0; i < labels.Length; i++)
        {
            var label = labels[i];
            if (!label)
                continue;

            if (popupView.ContinueButton && label.transform.IsChildOf(popupView.ContinueButton.transform))
                continue;

            if (popupView.SkipButton && label.transform.IsChildOf(popupView.SkipButton.transform))
                continue;

            if (label.name.IndexOf("Prompt", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _popupBodyText = label;
                return _popupBodyText;
            }

            var rect = label.transform as RectTransform;
            float width = rect ? rect.rect.width : 0f;
            if (width > fallbackWidth)
            {
                fallback = label;
                fallbackWidth = width;
            }
        }

        _popupBodyText = fallback;
        return _popupBodyText;
    }

    public bool IsTutorialCompletedForCurrentMode(string tutorialId)
    {
        if (string.IsNullOrWhiteSpace(tutorialId))
            return false;

        if (IsTestingBypassEnabledFor(tutorialId))
            return TutorialTestingScope.WasCompletedThisTestPass(tutorialId);

        var progress = PlayerProgress.Ensure();
        return progress != null && progress.IsTutorialCompleted(tutorialId);
    }

    bool IsTestingBypassEnabledFor(string tutorialId)
    {
        return ignoreSavedCompletionForTesting &&
               TutorialTestingScope.MatchesFilter(tutorialId, testingTutorialIdFilter);
    }

    void ShowInputBlocker()
    {
        EnsureReferences();

        Transform parent = null;
        if (popupView)
        {
            var canvas = popupView.GetComponentInParent<Canvas>();
            parent = canvas ? canvas.transform : popupView.transform.parent;
        }

        if (!parent)
            return;

        if (!_inputBlocker)
        {
            _inputBlocker = new GameObject("TutorialPopup_InputBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _inputBlockerRect = _inputBlocker.transform as RectTransform;
            var image = _inputBlocker.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
        }

        if (!_inputBlockerRect)
            _inputBlockerRect = _inputBlocker.transform as RectTransform;

        if (_inputBlocker.transform.parent != parent)
            _inputBlocker.transform.SetParent(parent, false);

        if (_inputBlockerRect)
        {
            _inputBlockerRect.anchorMin = Vector2.zero;
            _inputBlockerRect.anchorMax = Vector2.one;
            _inputBlockerRect.offsetMin = Vector2.zero;
            _inputBlockerRect.offsetMax = Vector2.zero;
            _inputBlockerRect.localScale = Vector3.one;
            _inputBlockerRect.localRotation = Quaternion.identity;
        }

        _inputBlocker.SetActive(true);
        _inputBlocker.transform.SetAsLastSibling();
        if (popupView)
            popupView.transform.SetAsLastSibling();
    }

    void HideInputBlocker()
    {
        if (_inputBlocker)
            _inputBlocker.SetActive(false);
    }

    void ClearSelectedUiOutsidePopup()
    {
        if (!EventSystem.current)
            return;

        var selected = EventSystem.current.currentSelectedGameObject;
        if (!selected)
            return;

        EventSystem.current.SetSelectedGameObject(null);
    }

    void EnsureReferences()
    {
        if (!gameController)
            gameController = GetComponent<GameController>();

        if (!gameController)
            gameController = FindFirstObjectByType<GameController>(FindObjectsInactive.Include);

        if (!popupView)
            popupView = FindTutorialPopupView();

        if (!highlightView)
            highlightView = FindFirstObjectByType<TutorialHighlightView>(FindObjectsInactive.Include);
    }

    static TutorialPopupView FindTutorialPopupView()
    {
        var views = FindObjectsByType<TutorialPopupView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < views.Length; i++)
        {
            var view = views[i];
            if (view && !view.IsReservedForWarnings)
                return view;
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
            if (child && string.Equals(child.name, childName, System.StringComparison.Ordinal))
                return child;

            Transform nested = FindDeepChild(child, childName);
            if (nested)
                return nested;
        }

        return null;
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

    bool CanShowQueuedPopup(bool allowWhileGameplaySuspended)
    {
        EnsureReferences();
        return allowWhileGameplaySuspended || !gameController || !gameController.IsGameplaySuspended;
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
        if (TetrabeastsControls.WasPressed(TetrabeastsControlAction.MenuSubmit))
            return true;

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
