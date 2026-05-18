using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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
        [Tooltip("Keeps the watched panel at the requested visibility while this step is active.")]
        public bool enforceWatchedPanelState = false;
        [Tooltip("The visibility enforced when Enforce Watched Panel State is enabled.")]
        public bool watchedPanelShouldBeVisible = true;
    }

    [Header("Sequence")]
    [SerializeField] bool autoStart = true;
    [SerializeField] bool ignoreSavedCompletion = false;
    [SerializeField] bool markSequenceCompleteOnSkip = true;
    [SerializeField] string completionKey = "gameplay_intro";

    [Header("Refs")]
    [SerializeField] GameController gameController;
    [SerializeField] TutorialPopupView popupView;
    [SerializeField] TutorialHighlightView highlightView;

    [Header("Steps")]
    [SerializeField] List<TutorialStep> steps = new();

    readonly HashSet<KeyCode> _pressedKeys = new();
    readonly HashSet<TutorialGameplayEvent> _receivedGameplayEvents = new();

    Button _currentWatchedButton;
    UnityAction _watchedButtonAction;
    Vector2 _defaultPopupPosition;
    int _currentStepIndex = -1;
    bool _sequenceRunning;
    bool _stepRequirementMet;
    TutorialStepCompletionMode _resolvedCompletionMode = TutorialStepCompletionMode.NextButton;

    readonly List<Button> _temporarilyDisabledButtons = new();
    public bool IsSequenceRunning => _sequenceRunning;
    public bool AllowsGameplayPauseInput => IsCurrentStepWaitingForKey(KeyCode.Escape);

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
    }

    void LateUpdate()
    {
        EnsureCurrentStepDisplayed();
        EnforceCurrentStepGuards();
    }

    public void StartSequenceIfNeeded()
    {
        if (_sequenceRunning)
            return;

        if (steps == null || steps.Count == 0 || popupView == null)
            return;

        if (!ignoreSavedCompletion && IsSequenceCompleted())
            return;

        EnsureGameController();
        BindGameplayEvents();

        _sequenceRunning = true;
        ShowStep(0);
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
            PlayerProgress.I?.SetTutorialCompleted(completionKey);

        _sequenceRunning = false;
        _currentStepIndex = -1;
        _stepRequirementMet = false;
        _pressedKeys.Clear();
        _receivedGameplayEvents.Clear();
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
        _pressedKeys.Clear();
        _receivedGameplayEvents.Clear();

        var step = steps[_currentStepIndex];
        _resolvedCompletionMode = ResolveCompletionMode(step);

        popupView.SetContent(step.body);
        popupView.SetSkipVisible(step.allowSkip);

        ApplyPopupPosition(step);
        popupView.Show();
        ApplyHighlight(step);
        HookCurrentStepBindings(step);
        ApplyButtonInteractionLock(step);
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
        popupView.SetSkipVisible(step.allowSkip);
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
                highlightView.Show(targets, step.highlightPadding);
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

        RestoreButtonInteractionLock();
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

    void OnSkipClicked()
    {
        if (!_sequenceRunning)
            return;

        StopSequence(markSequenceCompleteOnSkip);
    }

    void OnWatchedButtonClicked()
    {
        MarkCurrentStepRequirementMet();
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

        bool showContinueButton = _resolvedCompletionMode == TutorialStepCompletionMode.NextButton || !step.autoAdvanceOnComplete;
        bool continueInteractable = _resolvedCompletionMode == TutorialStepCompletionMode.NextButton || _stepRequirementMet;
        bool waitingForInteraction = !_stepRequirementMet && _resolvedCompletionMode != TutorialStepCompletionMode.NextButton;

        popupView.SetContinueVisible(showContinueButton);
        popupView.SetContinueInteractable(continueInteractable);
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

    bool IsSequenceCompleted()
    {
        if (string.IsNullOrWhiteSpace(completionKey))
            return false;

        return PlayerProgress.I != null && PlayerProgress.I.IsTutorialCompleted(completionKey);
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

    void ApplyPanelStateGuard(TutorialStep step, bool instant)
    {
        if (step == null || !step.enforceWatchedPanelState || !step.watchedPanel)
            return;

        bool isVisible = UIPanelTransition.IsVisible(step.watchedPanel);
        if (isVisible == step.watchedPanelShouldBeVisible)
            return;

        UIPanelTransition.SetVisible(step.watchedPanel, step.watchedPanelShouldBeVisible, instant);
    }

    bool WasKeyPressedThisFrame(KeyCode key)
    {
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
}
