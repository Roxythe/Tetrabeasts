using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class UIPanelTransition : MonoBehaviour
{
    [SerializeField, Min(0f)] float fadeInSeconds = 0.12f;
    [SerializeField, Min(0f)] float fadeOutSeconds = 0.10f;

    CanvasGroup _canvasGroup;
    Coroutine _fadeRoutine;
    bool _isHiding;

    public bool IsHiding => _isHiding;

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

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }
    }

    void ShowPanel(float targetAlpha, bool instant)
    {
        EnsureCanvasGroup();

        targetAlpha = Mathf.Clamp01(targetAlpha);
        _isHiding = false;

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        bool wasActive = gameObject.activeSelf;
        if (!wasActive)
            gameObject.SetActive(true);

        if (!_canvasGroup)
            return;

        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        if (instant || fadeInSeconds <= 0f || !Application.isPlaying || !isActiveAndEnabled)
        {
            _canvasGroup.alpha = targetAlpha;
            return;
        }

        float startAlpha = wasActive ? _canvasGroup.alpha : 0f;
        _canvasGroup.alpha = startAlpha;
        _fadeRoutine = StartCoroutine(FadeTo(startAlpha, targetAlpha, fadeInSeconds, deactivateOnComplete: false));
    }

    void HidePanel(bool instant)
    {
        EnsureCanvasGroup();

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        if (!_canvasGroup || instant || fadeOutSeconds <= 0f || !Application.isPlaying || !isActiveAndEnabled)
        {
            _isHiding = false;
            gameObject.SetActive(false);
            return;
        }

        _isHiding = true;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        _fadeRoutine = StartCoroutine(FadeTo(_canvasGroup.alpha, 0f, fadeOutSeconds, deactivateOnComplete: true));
    }

    IEnumerator FadeTo(float from, float to, float duration, bool deactivateOnComplete)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.001f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (_canvasGroup)
                _canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));

            yield return null;
        }

        if (_canvasGroup)
            _canvasGroup.alpha = to;

        _fadeRoutine = null;

        if (deactivateOnComplete)
        {
            _isHiding = false;
            gameObject.SetActive(false);
        }
    }

    void EnsureCanvasGroup()
    {
        if (_canvasGroup)
            return;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (!_canvasGroup)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }
}
