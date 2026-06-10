using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class UIPanelTransition : MonoBehaviour
{
    static readonly HashSet<UIPanelTransition> RunningTransitions = new();

    [SerializeField, Min(0f)] float moveToSavedPositionSeconds = 0.45f;
    [SerializeField, Min(0f)] float growToFullSizeSeconds = 0.18f;
    [SerializeField] bool animateFromBottomRight = true;
    [SerializeField, Min(0.0001f)] float bottomRightStartScale = 0.005f;
    [SerializeField, Min(0.0001f)] float travelEndScale = 0.05f;
    [SerializeField, Min(0f)] float uCurveDepth = 180f;
    [SerializeField] Vector2 bottomRightScreenPadding = new Vector2(18f, 18f);

    CanvasGroup _canvasGroup;
    RectTransform _rectTransform;
    Coroutine _fadeRoutine;
    bool _isHiding;
    bool _hasTargetPose;
    Vector3 _targetLocalPosition;
    Vector3 _targetLocalScale;

    public bool IsHiding => _isHiding;
    public bool IsTransitioning => _fadeRoutine != null;
    public static bool IsAnyTransitioning => RunningTransitions.Count > 0;

    public static void Show(GameObject panel, bool instant = false)
    {
        Show(panel, 1f, instant);
    }

    public static void Show(GameObject panel, float targetAlpha, bool instant = false)
    {
        if (!panel) return;

        var transition = Ensure(panel);
        transition.ShowPanel(targetAlpha, instant);
    }

    public static void Hide(GameObject panel, bool instant = false)
    {
        if (!panel) return;

        var transition = Ensure(panel);
        transition.HidePanel(instant);
    }

    public static void SetVisible(GameObject panel, bool visible, bool instant = false)
    {
        if (visible)
            Show(panel, instant);
        else
            Hide(panel, instant);
    }

    public static bool IsVisible(GameObject panel)
    {
        if (!panel || !panel.activeSelf || !panel.activeInHierarchy)
            return false;

        var transition = panel.GetComponent<UIPanelTransition>();
        return !transition || !transition.IsHiding;
    }

    public static bool IsFullyShown(GameObject panel)
    {
        if (!IsVisible(panel))
            return false;

        var transition = panel.GetComponent<UIPanelTransition>();
        return !transition || (!transition._isHiding && transition._fadeRoutine == null);
    }

    public static bool IsPanelTransitioning(GameObject panel)
    {
        if (!panel || !panel.activeInHierarchy)
            return false;

        var transition = panel.GetComponent<UIPanelTransition>();
        return transition && transition.IsTransitioning;
    }

    static UIPanelTransition Ensure(GameObject panel)
    {
        var transition = panel.GetComponent<UIPanelTransition>();
        if (!transition)
            transition = panel.AddComponent<UIPanelTransition>();

        transition.EnsureCanvasGroup();
        return transition;
    }

    void Awake()
    {
        EnsureCanvasGroup();
    }

    void OnDisable()
    {
        _isHiding = false;
        RestoreTargetPose();

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
            UnregisterRunningTransition();
        }
    }

    void OnDestroy()
    {
        UnregisterRunningTransition();
    }

    void ShowPanel(float targetAlpha, bool instant)
    {
        EnsureCanvasGroup();
        EnsureRectTransform();

        if (!instant && _fadeRoutine != null)
            return;

        targetAlpha = Mathf.Clamp01(targetAlpha);
        bool wasHiding = _isHiding;
        bool animationRunning = _fadeRoutine != null;
        _isHiding = false;

        if (!_hasTargetPose || (!animationRunning && !wasHiding))
            CaptureTargetPose();

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
            UnregisterRunningTransition();
        }

        bool wasActive = gameObject.activeSelf;
        if (!wasActive)
            gameObject.SetActive(true);

        if (!_canvasGroup)
            return;

        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        bool continuingFromHide = wasHiding || animationRunning;
        if (wasActive && !continuingFromHide)
        {
            _canvasGroup.alpha = targetAlpha;
            RestoreTargetPose();
            return;
        }

        if (instant || !Application.isPlaying || !isActiveAndEnabled)
        {
            _canvasGroup.alpha = targetAlpha;
            RestoreTargetPose();
            return;
        }

        Vector3 startPosition = wasActive || !animateFromBottomRight
            ? transform.localPosition
            : GetBottomRightStartLocalPosition();
        Vector3 startScale = wasActive || !animateFromBottomRight
            ? transform.localScale
            : _targetLocalScale * bottomRightStartScale;
        Vector3 travelScale = _targetLocalScale * travelEndScale;

        _canvasGroup.alpha = targetAlpha;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = true;
        transform.localPosition = startPosition;
        transform.localScale = startScale;

        _fadeRoutine = StartCoroutine(AnimateOpen(
            targetAlpha,
            startPosition,
            startScale,
            travelScale));
        RegisterRunningTransition();
    }

    void HidePanel(bool instant)
    {
        EnsureCanvasGroup();
        EnsureRectTransform();

        if (!instant && _fadeRoutine != null)
            return;

        bool animationRunning = _fadeRoutine != null;
        if (!_hasTargetPose || (!animationRunning && !_isHiding))
            CaptureTargetPose();

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
            UnregisterRunningTransition();
        }

        if (!_canvasGroup || instant || !Application.isPlaying || !isActiveAndEnabled)
        {
            _isHiding = false;
            RestoreTargetPose();
            gameObject.SetActive(false);
            return;
        }

        _isHiding = true;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        Vector3 fromScale = transform.localScale;
        Vector3 cornerPosition = animateFromBottomRight
            ? GetBottomRightStartLocalPosition()
            : _targetLocalPosition;
        Vector3 cornerScale = animateFromBottomRight
            ? _targetLocalScale * bottomRightStartScale
            : _targetLocalScale;
        Vector3 travelScale = animateFromBottomRight
            ? _targetLocalScale * travelEndScale
            : _targetLocalScale;

        _fadeRoutine = StartCoroutine(AnimateClose(
            fromScale,
            cornerPosition,
            cornerScale,
            travelScale));
        RegisterRunningTransition();
    }

    IEnumerator AnimateOpen(float targetAlpha, Vector3 startPosition, Vector3 startScale, Vector3 travelScale)
    {
        yield return AnimateTravel(
            startPosition,
            _targetLocalPosition,
            startScale,
            travelScale,
            moveToSavedPositionSeconds,
            forward: true);

        ApplyAnimatedPose(_targetLocalPosition, travelScale);

        yield return AnimateScaleAtTarget(
            travelScale,
            _targetLocalScale,
            growToFullSizeSeconds);

        if (_canvasGroup)
        {
            _canvasGroup.alpha = targetAlpha;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        ApplyAnimatedPose(_targetLocalPosition, _targetLocalScale);
        _fadeRoutine = null;
        UnregisterRunningTransition();
    }

    IEnumerator AnimateClose(Vector3 startScale, Vector3 cornerPosition, Vector3 cornerScale, Vector3 travelScale)
    {
        yield return AnimateScaleAtTarget(
            startScale,
            travelScale,
            growToFullSizeSeconds);

        yield return AnimateTravel(
            cornerPosition,
            _targetLocalPosition,
            travelScale,
            cornerScale,
            moveToSavedPositionSeconds,
            forward: false);

        _fadeRoutine = null;
        UnregisterRunningTransition();
        _isHiding = false;
        RestoreTargetPose();
        gameObject.SetActive(false);
    }

    IEnumerator AnimateTravel(
        Vector3 cornerPosition,
        Vector3 targetPosition,
        Vector3 scaleFrom,
        Vector3 scaleTo,
        float duration,
        bool forward)
    {
        if (!animateFromBottomRight || duration <= 0f)
        {
            ApplyAnimatedPose(
                forward ? targetPosition : cornerPosition,
                scaleTo);
            yield break;
        }

        float elapsed = 0f;
        duration = Mathf.Max(0.001f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            float pathT = forward ? eased : 1f - eased;

            ApplyAnimatedPose(
                EvaluateUCurve(cornerPosition, targetPosition, pathT),
                Vector3.LerpUnclamped(scaleFrom, scaleTo, eased));

            yield return null;
        }

        ApplyAnimatedPose(
            forward ? targetPosition : cornerPosition,
            scaleTo);
    }

    IEnumerator AnimateScaleAtTarget(Vector3 scaleFrom, Vector3 scaleTo, float duration)
    {
        ApplyAnimatedPose(_targetLocalPosition, scaleFrom);

        if (duration <= 0f)
        {
            ApplyAnimatedPose(_targetLocalPosition, scaleTo);
            yield break;
        }

        float elapsed = 0f;
        duration = Mathf.Max(0.001f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            ApplyAnimatedPose(_targetLocalPosition, Vector3.LerpUnclamped(scaleFrom, scaleTo, eased));
            yield return null;
        }

        ApplyAnimatedPose(_targetLocalPosition, scaleTo);
    }

    void EnsureCanvasGroup()
    {
        if (_canvasGroup)
            return;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (!_canvasGroup)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void EnsureRectTransform()
    {
        if (!_rectTransform)
            _rectTransform = transform as RectTransform;
    }

    void CaptureTargetPose()
    {
        _targetLocalPosition = transform.localPosition;
        _targetLocalScale = transform.localScale;
        _hasTargetPose = true;
    }

    void RestoreTargetPose()
    {
        if (!_hasTargetPose)
            return;

        ApplyAnimatedPose(_targetLocalPosition, _targetLocalScale);
    }

    void ApplyAnimatedPose(Vector3 localPosition, Vector3 localScale)
    {
        if (!animateFromBottomRight)
            return;

        transform.localPosition = localPosition;
        transform.localScale = localScale;
    }

    Vector3 GetBottomRightStartLocalPosition()
    {
        if (!animateFromBottomRight)
            return _targetLocalPosition;

        EnsureRectTransform();

        var parentRect = transform.parent as RectTransform;
        if (!parentRect)
            return _targetLocalPosition;

        var screenPoint = new Vector2(
            Mathf.Max(0f, Screen.width - bottomRightScreenPadding.x),
            Mathf.Max(0f, bottomRightScreenPadding.y));

        Camera camera = null;
        var canvas = GetComponentInParent<Canvas>();
        if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            camera = canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, camera, out var localPoint))
            return _targetLocalPosition;

        Vector2 pivotOffset = GetScaledBottomRightPivotOffset();
        return new Vector3(
            localPoint.x - pivotOffset.x,
            localPoint.y - pivotOffset.y,
            _targetLocalPosition.z);
    }

    Vector2 GetScaledBottomRightPivotOffset()
    {
        if (!_rectTransform)
            return Vector2.zero;

        Vector2 size = _rectTransform.rect.size;
        Vector3 startScale = _targetLocalScale * bottomRightStartScale;

        return new Vector2(
            (1f - _rectTransform.pivot.x) * size.x * startScale.x,
            -_rectTransform.pivot.y * size.y * startScale.y);
    }

    Vector3 EvaluateUCurve(Vector3 start, Vector3 end, float t)
    {
        t = Mathf.Clamp01(t);
        Vector3 control = new Vector3(end.x, start.y - uCurveDepth, start.z);
        float inverse = 1f - t;
        return (inverse * inverse * start) + (2f * inverse * t * control) + (t * t * end);
    }

    void RegisterRunningTransition()
    {
        RunningTransitions.Add(this);
    }

    void UnregisterRunningTransition()
    {
        RunningTransitions.Remove(this);
    }
}
