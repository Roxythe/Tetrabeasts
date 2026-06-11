using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class TutorialSequenceController : MonoBehaviour
{
    public enum TutorialStepCompletionMode
    {
        NextButton,
        AnyKey,
        AllKeys,
        AnyGameplayEvent,
        AllGameplayEvents,
        ButtonClick,
        PanelOpened
    }

    [Serializable]
    public class TutorialStep
    {
        public string stepId;

        [TextArea(3, 8)]
        public string body;

        [Header("Flow")]
        public bool pauseGameplay = true;
        public bool allowSkip = true;
        public bool autoAdvanceOnComplete = true;
        public bool allowSoftDropDuringStep = false;
        public bool allowHardDropDuringStep = false;
        [Tooltip("Disables all scene buttons except the watched button and tutorial popup buttons while this step is active.")]
        public bool lockOtherButtonsDuringStep = false;

        [Header("Popup")]
        public TutorialPopupView.PopupAnchorPreset popupAnchorPreset = TutorialPopupView.PopupAnchorPreset.Default;
        public Vector2 popupAnchoredPosition;
        [Range(0.1f, 1f)] public float popupAlpha = 1f;

        [Header("Highlight")]
        public RectTransform highlightTarget;
        public Vector2 highlightPadding = new Vector2(24f, 24f);
        public bool highlightActivePiece;
        public bool freezePieceGravity;

        [Header("Completion")]
        public TutorialStepCompletionMode completionMode = TutorialStepCompletionMode.NextButton;
        public List<KeyCode> requiredKeys = new();
        public List<TutorialGameplayEvent> requiredGameplayEvents = new();
        public Button watchedButton;
        public GameObject watchedPanel;

        [Header("Panel Guard")]
        [Tooltip("Keeps the watched panel at the requested visibility while this step is active. On Button Click steps with Should Be Visible off, the panel closes after the watched button click.")]
        public bool enforceWatchedPanelState = false;
        [Tooltip("The visibility enforced when Enforce Watched Panel State is enabled.")]
        public bool watchedPanelShouldBeVisible = true;
    }

    [Header("Sequence")]
    [SerializeField] bool autoStart = true;
    [FormerlySerializedAs("ignoreSavedCompletion")]
    [Tooltip("Testing only. Ignores saved tutorial completion, but this sequence still appears only once per run/test pass.")]
    [SerializeField] bool ignoreSavedCompletionForTesting = false;
    [SerializeField] bool markSequenceCompleteOnSkip = true;
    [SerializeField] string completionKey = "gameplay_intro";

    [Header("Refs")]
    [SerializeField] GameController gameController;
    [SerializeField] TutorialPopupView popupView;
    [SerializeField] TutorialHighlightView highlightView;

    [Header("Steps")]
    [SerializeField] List<TutorialStep> steps = new();
    [SerializeField] Vector2 activePieceHighlightSizeAdjustment = new Vector2(-6f, 6f);

    readonly HashSet<KeyCode> _pressedKeys = new();
    readonly HashSet<TutorialGameplayEvent> _receivedGameplayEvents = new();

    Button _currentWatchedButton;
    UnityAction _watchedButtonAction;
    Coroutine _watchedButtonCompletionRoutine;
    Canvas _watchedButtonOverlayCanvas;
    GraphicRaycaster _watchedButtonOverlayRaycaster;
    bool _watchedButtonOverlayCanvasWasExisting;
    bool _watchedButtonOverlayRaycasterWasExisting;
    bool _watchedButtonOverlayOriginalOverrideSorting;
    int _watchedButtonOverlayOriginalSortingOrder;
    Vector2 _defaultPopupPosition;
    int _currentStepIndex = -1;
    bool _sequenceRunning;
    bool _stepRequirementMet;
    bool _panelGuardReleasedForCurrentStep;
    TutorialStepCompletionMode _resolvedCompletionMode = TutorialStepCompletionMode.NextButton;
    int _lastWatchedButtonNavigationFrame = -1000;

    readonly List<Button> _temporarilyDisabledButtons = new();
    static int s_runningSequenceCount;
    static int s_lastSequenceClosedFrame = -1000;
    bool _countedAsRunning;
    public bool IsSequenceRunning => _sequenceRunning;
    public bool AllowsGameplayPauseInput => IsCurrentStepWaitingForKey(KeyCode.Escape);
    public static bool IsAnySequenceBlockingUi =>
        s_runningSequenceCount > 0 || Time.frameCount <= s_lastSequenceClosedFrame;

    void Awake()
    {
        if (!popupView)
            popupView = GetComponentInChildren<TutorialPopupView>(true);

        if (!highlightView)
            highlightView = GetComponentInChildren<TutorialHighlightView>(true);

        if (!gameController)
            gameController = FindFirstObjectByType<GameController>(FindObjectsInactive.Include);

        if (popupView && popupView.PopupRectTransform)
            _defaultPopupPosition = popupView.PopupRectTransform.anchoredPosition;

        popupView?.Hide(true);
        highlightView?.Hide();
    }

    void OnEnable()
    {
        BindPopupButtons();
        BindGameplayEvents();
    }

    void OnDisable()
    {
        UnbindPopupButtons();
        UnbindGameplayEvents();
        CleanupCurrentStepBindings();
        SetGameplaySuspended(false);

        if (gameController)
        {
            gameController.SetTutorialFreezePieceGravity(false);
            gameController.SetTutorialDropPermissions(false, false);
        }         

        if (popupView && popupView.gameObject.activeInHierarchy)
            popupView.Hide(true);

        highlightView?.Hide();

        _sequenceRunning = false;
        _currentStepIndex = -1;
        UnregisterSequenceRunning();
    }

    IEnumerator Start()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        if (autoStart)
            yield return StartSequenceWhenReady();
    }

    IEnumerator StartSequenceWhenReady()
    {
        EnsureGameController();

        while (gameController && gameController.IsGameplaySuspended)
            yield return null;

        StartSequenceIfNeeded();
    }

    void Update()
    {
        if (!_sequenceRunning || _currentStepIndex < 0 || _currentStepIndex >= steps.Count)
            return;

        var step = steps[_currentStepIndex];

        switch (_resolvedCompletionMode)
        {
            case TutorialStepCompletionMode.AnyKey:
                if (CheckAnyRequiredKeyPressed(step))
                    MarkCurrentStepRequirementMet();
                break;

            case TutorialStepCompletionMode.AllKeys:
                CheckAllRequiredKeysPressed(step);
                break;

            case TutorialStepCompletionMode.PanelOpened:
                if (step.watchedPanel && step.watchedPanel.activeInHierarchy)
                    MarkCurrentStepRequirementMet();
                break;
        }

        if (ShouldAcceptDirectContinueInput() && WasContinueInputPressedThisFrame())
            OnContinueClicked();

        GuideWatchedButtonNavigation(step);
    }

    void LateUpdate()
    {
        EnsureCurrentStepDisplayed();
        EnforceCurrentStepGuards();
    }

    public bool StartSequenceIfNeeded()
    {
        if (_sequenceRunning)
            return true;

        if (steps == null || steps.Count == 0 || popupView == null)
            return false;

        if (IsSequenceCompletedForCurrentMode())
            return false;

        EnsureGameController();
        BindGameplayEvents();

        _sequenceRunning = true;
        RegisterSequenceRunning();
        ShowStep(0);
        return true;
    }

    public IEnumerator StartSequenceAndWaitIfNeeded()
    {
        if (!StartSequenceIfNeeded())
            yield break;

        yield return new WaitWhile(() => _sequenceRunning);
    }

    public void RestartSequence()
    {
        StopSequence(markComplete: false);
        StartSequenceIfNeeded();
    }

    public void StopSequence(bool markComplete)
    {
        CleanupCurrentStepBindings();
        SetGameplaySuspended(false);

        if (gameController)
        {   gameController.SetTutorialFreezePieceGravity(false);
            gameController.SetTutorialSuspended(false);
        }

        popupView?.Hide();
        highlightView?.Hide();

        if (markComplete && !string.IsNullOrWhiteSpace(completionKey))
        {
            if (ignoreSavedCompletionForTesting)
                TutorialTestingScope.MarkCompletedThisTestPass(completionKey);
            else
                PlayerProgress.Ensure()?.SetTutorialCompleted(completionKey);
        }

        _sequenceRunning = false;
        _currentStepIndex = -1;
        _stepRequirementMet = false;
        _pressedKeys.Clear();
        _receivedGameplayEvents.Clear();
        UnregisterSequenceRunning();
    }

    public void ConfigureManualSequence(
        string sequenceCompletionKey,
        IReadOnlyList<string> bodyPages,
        TutorialPopupView sharedPopupView,
        TutorialHighlightView sharedHighlightView,
        GameController ownerGameController,
        RectTransform highlightTarget,
        TutorialPopupView.PopupAnchorPreset popupAnchorPreset = TutorialPopupView.PopupAnchorPreset.Top,
        bool pauseGameplay = true,
        bool freezePieceGravity = false,
        bool lockOtherButtonsDuringStep = true,
        float popupAlpha = 1f,
        Vector2 highlightPadding = default)
    {
        autoStart = false;
        completionKey = sequenceCompletionKey ?? string.Empty;
        popupView = sharedPopupView;
        highlightView = sharedHighlightView;
        gameController = ownerGameController;

        steps.Clear();
        if (bodyPages != null)
        {
            for (int i = 0; i < bodyPages.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(bodyPages[i]))
                    continue;

                steps.Add(CreateManualStep(
                    bodyPages[i],
                    highlightTarget,
                    popupAnchorPreset,
                    pauseGameplay,
                    freezePieceGravity,
                    lockOtherButtonsDuringStep,
                    popupAlpha,
                    highlightPadding));
            }
        }

        BindPopupButtons();
        BindGameplayEvents();
    }

    static TutorialStep CreateManualStep(
        string body,
        RectTransform highlightTarget,
        TutorialPopupView.PopupAnchorPreset popupAnchorPreset,
        bool pauseGameplay,
        bool freezePieceGravity,
        bool lockOtherButtonsDuringStep,
        float popupAlpha,
        Vector2 highlightPadding)
    {
        return new TutorialStep
        {
            stepId = string.Empty,
            body = body,
            pauseGameplay = pauseGameplay,
            allowSkip = false,
            autoAdvanceOnComplete = true,
            lockOtherButtonsDuringStep = lockOtherButtonsDuringStep,
            popupAnchorPreset = popupAnchorPreset,
            popupAlpha = popupAlpha,
            highlightTarget = highlightTarget,
            highlightPadding = highlightPadding == default ? new Vector2(12f, 12f) : highlightPadding,
            freezePieceGravity = freezePieceGravity,
            completionMode = TutorialStepCompletionMode.NextButton
        };
    }

    void BindPopupButtons()
    {
        if (!popupView) return;

        if (popupView.ContinueButton)
        {
            popupView.ContinueButton.onClick.RemoveListener(OnContinueClicked);
            popupView.ContinueButton.onClick.AddListener(OnContinueClicked);
        }

        if (popupView.SkipButton)
        {
            popupView.SkipButton.onClick.RemoveListener(OnSkipClicked);
            popupView.SkipButton.onClick.AddListener(OnSkipClicked);
        }
    }

    void UnbindPopupButtons()
    {
        if (!popupView) return;

        if (popupView.ContinueButton)
            popupView.ContinueButton.onClick.RemoveListener(OnContinueClicked);

        if (popupView.SkipButton)
            popupView.SkipButton.onClick.RemoveListener(OnSkipClicked);
    }

    void BindGameplayEvents()
    {
        EnsureGameController();
        if (!gameController) return;

        gameController.TutorialGameplayEventRaised -= HandleGameplayEvent;
        gameController.TutorialGameplayEventRaised += HandleGameplayEvent;
    }

    void UnbindGameplayEvents()
    {
        if (!gameController) return;
        gameController.TutorialGameplayEventRaised -= HandleGameplayEvent;
    }

    void EnsureGameController()
    {
        if (!gameController)
            gameController = FindFirstObjectByType<GameController>(FindObjectsInactive.Include);
    }

    void ShowStep(int stepIndex)
    {
        CleanupCurrentStepBindings();

        if (stepIndex >= steps.Count)
        {
            StopSequence(markComplete: true);
            return;
        }

        _currentStepIndex = stepIndex;
        _stepRequirementMet = false;
        _panelGuardReleasedForCurrentStep = false;
        _pressedKeys.Clear();
        _receivedGameplayEvents.Clear();
        _lastWatchedButtonNavigationFrame = -1000;

        var step = steps[_currentStepIndex];
        _resolvedCompletionMode = ResolveCompletionMode(step);

        popupView.SetContent(step.body);
        popupView.SetContinueVisible(false);
        popupView.SetContinueInteractable(false);
        popupView.SetSkipVisible(false);

        ApplyPopupPosition(step);
        popupView.Show();
        ApplyHighlight(step);
        HookCurrentStepBindings(step);
        ApplyButtonInteractionLock(step);
        ApplyWatchedButtonOverlay(step);
        ApplyPanelStateGuard(step, instant: true);
        SetGameplaySuspended(step.pauseGameplay);
        RefreshPopupState(step);

        EnsureGameController();

        if (gameController)
        { 
            gameController.SetTutorialFreezePieceGravity(step.freezePieceGravity);
            gameController.SetTutorialDropPermissions(step.allowSoftDropDuringStep, step.allowHardDropDuringStep);
        }

        if (_resolvedCompletionMode == TutorialStepCompletionMode.PanelOpened && step.watchedPanel && step.watchedPanel.activeInHierarchy)
            MarkCurrentStepRequirementMet();
    }

    void EnsureCurrentStepDisplayed()
    {
        if (!_sequenceRunning || _currentStepIndex < 0 || _currentStepIndex >= steps.Count || !popupView)
            return;

        if (popupView.IsShowing)
            return;

        var step = steps[_currentStepIndex];

        popupView.SetContent(step.body);
        popupView.SetContinueVisible(false);
        popupView.SetContinueInteractable(false);
        popupView.SetSkipVisible(false);
        ApplyPopupPosition(step);
        popupView.Show(true);
        ApplyHighlight(step);
        RefreshPopupState(step);
    }

    void EnforceCurrentStepGuards()
    {
        if (!_sequenceRunning || _currentStepIndex < 0 || _currentStepIndex >= steps.Count)
            return;

        var step = steps[_currentStepIndex];
        MaintainButtonInteractionLock(step);
        ApplyPanelStateGuard(step, instant: true);
    }

    void ApplyPopupPosition(TutorialStep step)
    {
        if (!popupView)
            return;

        popupView.ApplyPresetPosition(step.popupAnchorPreset, step.popupAnchoredPosition);
        popupView.SetVisibleAlpha(step.popupAlpha);
    }

    void ApplyHighlight(TutorialStep step)
    {
        if (!highlightView)
            return;

        if (step.highlightActivePiece)
        {
            EnsureGameController();
            var targets = gameController ? gameController.GetTutorialActivePieceHighlightTargets() : null;

            if (targets != null && targets.Count > 0)
                highlightView.Show(targets, step.highlightPadding, false, activePieceHighlightSizeAdjustment);
            else
                highlightView.Hide();

            return;
        }

        if (step.highlightTarget)
            highlightView.Show(step.highlightTarget, step.highlightPadding);
        else
            highlightView.Hide();
    }

    void HookCurrentStepBindings(TutorialStep step)
    {
        if (_resolvedCompletionMode != TutorialStepCompletionMode.ButtonClick || !step.watchedButton)
            return;

        _currentWatchedButton = step.watchedButton;
        _watchedButtonAction = OnWatchedButtonClicked;
        _currentWatchedButton.onClick.AddListener(_watchedButtonAction);
    }

    void CleanupCurrentStepBindings()
    {
        if (_currentWatchedButton != null && _watchedButtonAction != null)
            _currentWatchedButton.onClick.RemoveListener(_watchedButtonAction);

        _currentWatchedButton = null;
        _watchedButtonAction = null;

        if (_watchedButtonCompletionRoutine != null)
        {
            StopCoroutine(_watchedButtonCompletionRoutine);
            _watchedButtonCompletionRoutine = null;
        }

        _panelGuardReleasedForCurrentStep = false;
        RestoreButtonInteractionLock();
        RestoreWatchedButtonOverlay();
        _lastWatchedButtonNavigationFrame = -1000;
    }

    void HandleGameplayEvent(TutorialGameplayEvent gameplayEvent)
    {
        if (!_sequenceRunning || _currentStepIndex < 0 || _currentStepIndex >= steps.Count)
            return;

        var step = steps[_currentStepIndex];
        if (_resolvedCompletionMode != TutorialStepCompletionMode.AnyGameplayEvent &&
            _resolvedCompletionMode != TutorialStepCompletionMode.AllGameplayEvents)
            return;

        if (!step.requiredGameplayEvents.Contains(gameplayEvent))
            return;

        _receivedGameplayEvents.Add(gameplayEvent);

        if (_resolvedCompletionMode == TutorialStepCompletionMode.AnyGameplayEvent)
        {
            MarkCurrentStepRequirementMet();
            return;
        }

        for (int i = 0; i < step.requiredGameplayEvents.Count; i++)
        {
            if (!_receivedGameplayEvents.Contains(step.requiredGameplayEvents[i]))
                return;
        }

        MarkCurrentStepRequirementMet();
    }

    void OnContinueClicked()
    {
        if (!_sequenceRunning || _currentStepIndex < 0 || _currentStepIndex >= steps.Count)
            return;

        if (_resolvedCompletionMode == TutorialStepCompletionMode.NextButton || _stepRequirementMet)
            ShowStep(_currentStepIndex + 1);
    }

    bool ShouldAcceptDirectContinueInput()
    {
        if (!popupView)
            return false;

        var step = GetCurrentStep();
        if (step == null)
            return false;

        return _resolvedCompletionMode == TutorialStepCompletionMode.NextButton ||
            (!step.autoAdvanceOnComplete && _stepRequirementMet);
    }

    bool WasContinueInputPressedThisFrame()
    {
        return TetrabeastsControls.WasPressed(TetrabeastsControlAction.MenuSubmit) ||
            WasKeyPressedThisFrame(KeyCode.F);
    }

    void OnSkipClicked()
    {
        if (!_sequenceRunning)
            return;

        StopSequence(markSequenceCompleteOnSkip);
    }

    void OnWatchedButtonClicked()
    {
        if (_watchedButtonCompletionRoutine != null)
            return;

        var step = GetCurrentStep();
        _panelGuardReleasedForCurrentStep = ShouldCloseWatchedPanelAfterButtonClick(step);
        _watchedButtonCompletionRoutine = StartCoroutine(CompleteWatchedButtonClickAfterUiSettles());
    }

    IEnumerator CompleteWatchedButtonClickAfterUiSettles()
    {
        yield return null;

        var step = GetCurrentStep();
        if (ShouldCloseWatchedPanelAfterButtonClick(step))
            UIPanelTransition.Hide(step.watchedPanel, true);

        _watchedButtonCompletionRoutine = null;
        MarkCurrentStepRequirementMet();
    }

    TutorialStep GetCurrentStep()
    {
        if (!_sequenceRunning || _currentStepIndex < 0 || _currentStepIndex >= steps.Count)
            return null;

        return steps[_currentStepIndex];
    }

    void MarkCurrentStepRequirementMet()
    {
        if (_stepRequirementMet || _currentStepIndex < 0 || _currentStepIndex >= steps.Count)
            return;

        _stepRequirementMet = true;

        var step = steps[_currentStepIndex];
        RefreshPopupState(step);

        if (_resolvedCompletionMode != TutorialStepCompletionMode.NextButton && step.autoAdvanceOnComplete)
            ShowStep(_currentStepIndex + 1);
    }

    void RefreshPopupState(TutorialStep step)
    {
        if (!popupView)
            return;

        bool waitingForInteraction = !_stepRequirementMet && _resolvedCompletionMode != TutorialStepCompletionMode.NextButton;

        popupView.SetContinueVisible(false);
        popupView.SetContinueInteractable(false);
        popupView.SetSkipVisible(false);
        popupView.SetStepState(waitingForInteraction, _stepRequirementMet);
    }

    public bool IsCurrentStepWaitingForKey(KeyCode key)
    {
        if (!_sequenceRunning || _currentStepIndex < 0 || _currentStepIndex >= steps.Count)
            return false;

        if (_resolvedCompletionMode != TutorialStepCompletionMode.AnyKey &&
            _resolvedCompletionMode != TutorialStepCompletionMode.AllKeys)
            return false;

        var requiredKeys = steps[_currentStepIndex].requiredKeys;
        return requiredKeys != null && requiredKeys.Contains(key);
    }

    void SetGameplaySuspended(bool suspended)
    {
        EnsureGameController();
        if (!gameController) return;

        gameController.SetTutorialSuspended(suspended);
    }

    bool IsSequenceCompletedForCurrentMode()
    {
        if (string.IsNullOrWhiteSpace(completionKey))
            return false;

        if (ignoreSavedCompletionForTesting)
            return TutorialTestingScope.WasCompletedThisTestPass(completionKey);

        var progress = PlayerProgress.Ensure();
        return progress != null && progress.IsTutorialCompleted(completionKey);
    }

    TutorialStepCompletionMode ResolveCompletionMode(TutorialStep step)
    {
        switch (step.completionMode)
        {
            case TutorialStepCompletionMode.AnyKey:
            case TutorialStepCompletionMode.AllKeys:
                if (step.requiredKeys == null || step.requiredKeys.Count == 0)
                    return TutorialStepCompletionMode.NextButton;
                break;

            case TutorialStepCompletionMode.AnyGameplayEvent:
            case TutorialStepCompletionMode.AllGameplayEvents:
                if (step.requiredGameplayEvents == null || step.requiredGameplayEvents.Count == 0)
                    return TutorialStepCompletionMode.NextButton;
                break;

            case TutorialStepCompletionMode.ButtonClick:
                if (!step.watchedButton)
                    return TutorialStepCompletionMode.NextButton;
                break;

            case TutorialStepCompletionMode.PanelOpened:
                if (!step.watchedPanel)
                    return TutorialStepCompletionMode.NextButton;
                break;
        }

        return step.completionMode;
    }

    bool CheckAnyRequiredKeyPressed(TutorialStep step)
    {
        for (int i = 0; i < step.requiredKeys.Count; i++)
        {
            if (WasKeyPressedThisFrame(step.requiredKeys[i]))
                return true;
        }

        return false;
    }

    void CheckAllRequiredKeysPressed(TutorialStep step)
    {
        for (int i = 0; i < step.requiredKeys.Count; i++)
        {
            if (WasKeyPressedThisFrame(step.requiredKeys[i]))
                _pressedKeys.Add(step.requiredKeys[i]);
        }

        for (int i = 0; i < step.requiredKeys.Count; i++)
        {
            if (!_pressedKeys.Contains(step.requiredKeys[i]))
                return;
        }

        MarkCurrentStepRequirementMet();
    }

    void ApplyButtonInteractionLock(TutorialStep step)
    {
        RestoreButtonInteractionLock();
        MaintainButtonInteractionLock(step);
    }

    void MaintainButtonInteractionLock(TutorialStep step)
    {
        if (!ShouldLockButtons(step))
            return;

        var allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allButtons.Length; i++)
        {
            var button = allButtons[i];
            if (!button || ShouldKeepButtonInteractable(button, step))
                continue;

            if (!button.interactable)
                continue;

            button.interactable = false;
            if (!_temporarilyDisabledButtons.Contains(button))
                _temporarilyDisabledButtons.Add(button);
        }

        if (step.watchedButton)
            step.watchedButton.interactable = true;
    }

    bool ShouldLockButtons(TutorialStep step)
    {
        if (step == null)
            return false;

        return step.lockOtherButtonsDuringStep ||
            (_resolvedCompletionMode == TutorialStepCompletionMode.ButtonClick && step.watchedButton != null);
    }

    bool ShouldKeepButtonInteractable(Button button, TutorialStep step)
    {
        if (!button)
            return false;

        if (step != null && button == step.watchedButton && IsWatchedButtonAllowedThisStep(step))
            return true;

        if (popupView)
        {
            if (button == popupView.ContinueButton || button == popupView.SkipButton)
                return true;
        }

        return false;
    }

    bool IsWatchedButtonAllowedThisStep(TutorialStep step)
    {
        if (step == null || !step.watchedButton)
            return false;

        return _resolvedCompletionMode == TutorialStepCompletionMode.ButtonClick ||
            _resolvedCompletionMode == TutorialStepCompletionMode.PanelOpened;
    }

    void GuideWatchedButtonNavigation(TutorialStep step)
    {
        if (!ShouldGuideWatchedButtonNavigation(step))
            return;

        bool navigationPressed = TetrabeastsControls.WasButtonNavigationPressedThisFrame();
        bool navigationHeld = TetrabeastsControls.IsButtonNavigationHeld();
        if (!navigationPressed && !navigationHeld)
            return;

        if (!EventSystem.current || Time.frameCount == _lastWatchedButtonNavigationFrame)
            return;

        UICursorController.ActivateButtonNavigationTargetSource();

        var current = EventSystem.current.currentSelectedGameObject;
        if (current == step.watchedButton.gameObject)
            return;

        EventSystem.current.SetSelectedGameObject(step.watchedButton.gameObject);
        _lastWatchedButtonNavigationFrame = Time.frameCount;
    }

    bool ShouldGuideWatchedButtonNavigation(TutorialStep step)
    {
        if (step == null || !step.watchedButton)
            return false;

        if (!IsWatchedButtonAllowedThisStep(step))
            return false;

        return step.watchedButton.isActiveAndEnabled &&
            step.watchedButton.gameObject.activeInHierarchy &&
            step.watchedButton.interactable;
    }

    void RestoreButtonInteractionLock()
    {
        for (int i = 0; i < _temporarilyDisabledButtons.Count; i++)
        {
            var button = _temporarilyDisabledButtons[i];
            if (button)
                button.interactable = true;
        }

        _temporarilyDisabledButtons.Clear();
    }

    void ApplyWatchedButtonOverlay(TutorialStep step)
    {
        RestoreWatchedButtonOverlay();

        if (step == null || !step.watchedButton || !IsWatchedButtonAllowedThisStep(step))
            return;

        _watchedButtonOverlayCanvas = step.watchedButton.GetComponent<Canvas>();
        _watchedButtonOverlayCanvasWasExisting = _watchedButtonOverlayCanvas;
        if (!_watchedButtonOverlayCanvas)
            _watchedButtonOverlayCanvas = step.watchedButton.gameObject.AddComponent<Canvas>();

        _watchedButtonOverlayOriginalOverrideSorting = _watchedButtonOverlayCanvas.overrideSorting;
        _watchedButtonOverlayOriginalSortingOrder = _watchedButtonOverlayCanvas.sortingOrder;
        _watchedButtonOverlayCanvas.overrideSorting = true;
        _watchedButtonOverlayCanvas.sortingOrder = 5000;

        _watchedButtonOverlayRaycaster = step.watchedButton.GetComponent<GraphicRaycaster>();
        _watchedButtonOverlayRaycasterWasExisting = _watchedButtonOverlayRaycaster;
        if (!_watchedButtonOverlayRaycaster)
            _watchedButtonOverlayRaycaster = step.watchedButton.gameObject.AddComponent<GraphicRaycaster>();
    }

    void RestoreWatchedButtonOverlay()
    {
        if (_watchedButtonOverlayRaycaster && !_watchedButtonOverlayRaycasterWasExisting)
            Destroy(_watchedButtonOverlayRaycaster);

        if (_watchedButtonOverlayCanvas)
        {
            if (_watchedButtonOverlayCanvasWasExisting)
            {
                _watchedButtonOverlayCanvas.overrideSorting = _watchedButtonOverlayOriginalOverrideSorting;
                _watchedButtonOverlayCanvas.sortingOrder = _watchedButtonOverlayOriginalSortingOrder;
            }
            else
            {
                Destroy(_watchedButtonOverlayCanvas);
            }
        }

        _watchedButtonOverlayCanvas = null;
        _watchedButtonOverlayRaycaster = null;
        _watchedButtonOverlayCanvasWasExisting = false;
        _watchedButtonOverlayRaycasterWasExisting = false;
        _watchedButtonOverlayOriginalOverrideSorting = false;
        _watchedButtonOverlayOriginalSortingOrder = 0;
    }

    void ApplyPanelStateGuard(TutorialStep step, bool instant)
    {
        if (step == null || _panelGuardReleasedForCurrentStep || !step.enforceWatchedPanelState || !step.watchedPanel)
            return;

        if (ShouldCloseWatchedPanelAfterButtonClick(step) && !_stepRequirementMet)
            return;

        bool isVisible = UIPanelTransition.IsVisible(step.watchedPanel);
        if (isVisible == step.watchedPanelShouldBeVisible)
            return;

        UIPanelTransition.SetVisible(step.watchedPanel, step.watchedPanelShouldBeVisible, instant);
    }

    bool ShouldCloseWatchedPanelAfterButtonClick(TutorialStep step)
    {
        return step != null &&
            _resolvedCompletionMode == TutorialStepCompletionMode.ButtonClick &&
            step.watchedPanel &&
            step.enforceWatchedPanelState &&
            !step.watchedPanelShouldBeVisible;
    }

    bool WasKeyPressedThisFrame(KeyCode key)
    {
        if (TetrabeastsControls.EffectiveProfile != TetrabeastsControlProfile.KeyboardMouse &&
            TryGetControlActionForTutorialKey(key, out var action) &&
            TetrabeastsControls.PeekWasPressed(action))
            return true;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    var keyboard = Keyboard.current;
    if (keyboard == null)
        return false;

    switch (key)
    {
        case KeyCode.A: return keyboard.aKey.wasPressedThisFrame;
        case KeyCode.D: return keyboard.dKey.wasPressedThisFrame;
        case KeyCode.S: return keyboard.sKey.wasPressedThisFrame;
        case KeyCode.F: return keyboard.fKey.wasPressedThisFrame;
        case KeyCode.Q: return keyboard.qKey.wasPressedThisFrame;
        case KeyCode.E: return keyboard.eKey.wasPressedThisFrame;
        case KeyCode.R: return keyboard.rKey.wasPressedThisFrame;
        case KeyCode.Space: return keyboard.spaceKey.wasPressedThisFrame;
        case KeyCode.Escape: return keyboard.escapeKey.wasPressedThisFrame;
        case KeyCode.LeftArrow: return keyboard.leftArrowKey.wasPressedThisFrame;
        case KeyCode.RightArrow: return keyboard.rightArrowKey.wasPressedThisFrame;
        case KeyCode.DownArrow: return keyboard.downArrowKey.wasPressedThisFrame;
        case KeyCode.UpArrow: return keyboard.upArrowKey.wasPressedThisFrame;
        case KeyCode.Z: return keyboard.zKey.wasPressedThisFrame;
        case KeyCode.Return: return keyboard.enterKey.wasPressedThisFrame;
        case KeyCode.KeypadEnter: return keyboard.numpadEnterKey.wasPressedThisFrame;
        default: return false;
    }
#else
        return Input.GetKeyDown(key);
#endif
    }

    static bool TryGetControlActionForTutorialKey(KeyCode key, out TetrabeastsControlAction action)
    {
        switch (key)
        {
            case KeyCode.A:
            case KeyCode.LeftArrow:
                action = TetrabeastsControlAction.MoveLeft;
                return true;

            case KeyCode.D:
            case KeyCode.RightArrow:
                action = TetrabeastsControlAction.MoveRight;
                return true;

            case KeyCode.S:
            case KeyCode.DownArrow:
                action = TetrabeastsControlAction.SoftDrop;
                return true;

            case KeyCode.Q:
            case KeyCode.Z:
                action = TetrabeastsControlAction.RotateCounterClockwise;
                return true;

            case KeyCode.E:
            case KeyCode.UpArrow:
                action = TetrabeastsControlAction.RotateClockwise;
                return true;

            case KeyCode.Space:
                action = TetrabeastsControlAction.HardDrop;
                return true;

            case KeyCode.R:
                action = TetrabeastsControlAction.Special;
                return true;

            case KeyCode.Escape:
                action = TetrabeastsControlAction.Pause;
                return true;

            case KeyCode.F:
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                action = TetrabeastsControlAction.MenuSubmit;
                return true;

            default:
                action = default;
                return false;
        }
    }

    void RegisterSequenceRunning()
    {
        if (_countedAsRunning)
            return;

        _countedAsRunning = true;
        s_runningSequenceCount++;
    }

    void UnregisterSequenceRunning()
    {
        if (!_countedAsRunning)
            return;

        _countedAsRunning = false;
        s_runningSequenceCount = Mathf.Max(0, s_runningSequenceCount - 1);
        s_lastSequenceClosedFrame = Time.frameCount;
    }
}
