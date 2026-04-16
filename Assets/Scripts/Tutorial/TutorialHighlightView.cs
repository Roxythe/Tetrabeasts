using UnityEngine;

[DisallowMultipleComponent]
public class TutorialHighlightView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] RectTransform highlightRoot;
    [SerializeField] RectTransform canvasRoot;
    [SerializeField] CanvasGroup canvasGroup;

    [Header("Layout")]
    [SerializeField] Vector2 defaultPadding = new Vector2(24f, 24f);
    [SerializeField] Vector2 minimumSize = new Vector2(96f, 96f);

    readonly Vector3[] _worldCorners = new Vector3[4];
    RectTransform _target;
    Vector2 _padding;
    Canvas _rootCanvas;

    void Awake()
    {
        if (!highlightRoot)
            highlightRoot = transform as RectTransform;

        if (!canvasRoot)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas)
                canvasRoot = canvas.transform as RectTransform;
        }

        if (!canvasGroup)
            canvasGroup = GetComponent<CanvasGroup>();

        if (!canvasGroup)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _rootCanvas = canvasRoot ? canvasRoot.GetComponentInParent<Canvas>() : null;
        SetVisible(false);
    }

    void LateUpdate()
    {
        if (!_target)
        {
            SetVisible(false);
            return;
        }

        RefreshPosition();
    }

    public void Show(RectTransform target, Vector2 padding)
    {
        _target = target;
        _padding = (padding == Vector2.zero) ? defaultPadding : padding;
        RefreshPosition();
    }

    public void Hide()
    {
        _target = null;
        SetVisible(false);
    }

    void RefreshPosition()
    {
        if (!_target || !highlightRoot || !canvasRoot)
        {
            SetVisible(false);
            return;
        }

        var targetCanvas = _target.GetComponentInParent<Canvas>();
        var eventCamera = targetCanvas && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.worldCamera
            : (_rootCanvas && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? _rootCanvas.worldCamera : null);

        _target.GetWorldCorners(_worldCorners);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < _worldCorners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, _worldCorners[i]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, screenPoint, eventCamera, out var localPoint))
                continue;

            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        Vector2 size = (max - min) + (_padding * 2f);
        size.x = Mathf.Max(size.x, minimumSize.x);
        size.y = Mathf.Max(size.y, minimumSize.y);

        highlightRoot.anchoredPosition = (min + max) * 0.5f;
        highlightRoot.sizeDelta = size;
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        if (!canvasGroup) return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
