using UnityEngine;

[DisallowMultipleComponent]
public class TutorialHighlightView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] RectTransform highlightContainerRoot;
    [SerializeField] RectTransform highlightTemplate;
    [SerializeField] RectTransform canvasRoot;
    [SerializeField] CanvasGroup canvasGroup;
    
    [Header("Layout")]
    [SerializeField] Vector2 defaultPadding = new Vector2(24f, 24f);
    [SerializeField] Vector2 minimumSize = new Vector2(96f, 96f);

    readonly Vector3[] _worldCorners = new Vector3[4];
    readonly System.Collections.Generic.List<RectTransform> _targets = new();
    readonly System.Collections.Generic.List<RectTransform> _instances = new();
    Vector2 _padding;
    Canvas _rootCanvas;

    void Awake()
    {
        if (!highlightContainerRoot)
            highlightContainerRoot = transform as RectTransform;

        if (highlightTemplate)
            highlightTemplate.gameObject.SetActive(false);

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
        if (_targets.Count == 0)
        {
            SetVisible(false);
            return;
        }

        RefreshPositions();
    }

    public void Show(RectTransform target, Vector2 padding)
    {
        _targets.Clear();

        if (target)
            _targets.Add(target);

        _padding = (padding == Vector2.zero) ? defaultPadding : padding;
        RefreshPositions();
    }

    public void Show(System.Collections.Generic.IReadOnlyList<RectTransform> targets, Vector2 padding)
    {
        _targets.Clear();

        if (targets != null)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i])
                    _targets.Add(targets[i]);
            }
        }

        _padding = (padding == Vector2.zero) ? defaultPadding : padding;
        RefreshPositions();
    }

    public void Hide()
    {
        _targets.Clear();

        for (int i = 0; i < _instances.Count; i++)
        {
            if (_instances[i])
                SetInstanceVisible(_instances[i], false);
        }

        SetVisible(false);
    }

    void RefreshPositions()
    {
        if (!highlightTemplate || !highlightContainerRoot || !canvasRoot || _targets.Count == 0)
        {
            SetVisible(false);
            return;
        }

        EnsureInstanceCount(_targets.Count);

        for (int i = 0; i < _instances.Count; i++)
        {
            bool active = i < _targets.Count && _targets[i];
            if (_instances[i])
                SetInstanceVisible(_instances[i], active);

            if (!active)
                continue;

            RefreshInstancePosition(_instances[i], _targets[i]);
        }

        SetVisible(true);
    }

    void EnsureInstanceCount(int count)
    {
        while (_instances.Count < count)
        {
            if (!highlightTemplate || !highlightContainerRoot)
                return;

            var clone = Instantiate(highlightTemplate, highlightContainerRoot);
            clone.name = $"{highlightTemplate.name}_Instance_{_instances.Count}";
            clone.SetAsFirstSibling();
            SetInstanceVisible(clone, true);
            _instances.Add(clone);
        }
    }

    void RefreshInstancePosition(RectTransform instance, RectTransform target)
    {
        if (!instance || !target || !canvasRoot)
            return;

        var targetCanvas = target.GetComponentInParent<Canvas>();
        var eventCamera = targetCanvas && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.worldCamera
            : (_rootCanvas && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? _rootCanvas.worldCamera : null);

        target.GetWorldCorners(_worldCorners);

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

        instance.anchoredPosition = (min + max) * 0.5f;
        instance.sizeDelta = size;
        instance.SetAsFirstSibling();
    }

    void SetInstanceVisible(RectTransform instance, bool visible)
    {
        if (!instance)
            return;

        instance.gameObject.SetActive(visible);
    }

    void SetVisible(bool visible)
    {
        if (!canvasGroup) return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
