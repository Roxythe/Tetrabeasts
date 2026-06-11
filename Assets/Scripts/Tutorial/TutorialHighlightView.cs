using UnityEngine;
using UnityEngine.UI;

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

    [Header("Visuals")]
    [SerializeField, Range(0, 255)] int highlightAlpha = 200;

    [Header("Layering")]
    [SerializeField] bool renderDirectlyUnderTarget = true;
    [SerializeField] bool renderAboveTutorialDimmer = true;

    readonly Vector3[] _worldCorners = new Vector3[4];
    readonly System.Collections.Generic.List<RectTransform> _targets = new();
    readonly System.Collections.Generic.List<RectTransform> _instances = new();
    Vector2 _padding;
    Vector2 _sizeAdjustment;
    bool _useMinimumSize = true;
    Canvas _rootCanvas;

    void Awake()
    {
        if (!highlightContainerRoot)
            highlightContainerRoot = transform as RectTransform;

        if (highlightTemplate && highlightTemplate.gameObject.scene.IsValid())
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
        Show(target, padding, true, Vector2.zero);
    }

    public void Show(RectTransform target, Vector2 padding, bool useMinimumSize, Vector2 sizeAdjustment)
    {
        _targets.Clear();

        if (target)
            _targets.Add(target);

        _padding = (padding == Vector2.zero) ? defaultPadding : padding;
        _useMinimumSize = useMinimumSize;
        _sizeAdjustment = sizeAdjustment;
        RefreshPositions();
    }

    public void Show(System.Collections.Generic.IReadOnlyList<RectTransform> targets, Vector2 padding)
    {
        Show(targets, padding, true, Vector2.zero);
    }

    public void Show(
        System.Collections.Generic.IReadOnlyList<RectTransform> targets,
        Vector2 padding,
        bool useMinimumSize,
        Vector2 sizeAdjustment)
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
        _useMinimumSize = useMinimumSize;
        _sizeAdjustment = sizeAdjustment;
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
            ApplyHighlightVisualSettings(clone);
            SetInstanceVisible(clone, true);
            _instances.Add(clone);
        }
    }

    void RefreshInstancePosition(RectTransform instance, RectTransform target)
    {
        if (!instance || !target)
            return;

        var renderParent = GetRenderParent(target);
        if (!renderParent)
            return;

        MoveInstanceToRenderParent(instance, target, renderParent);

        var targetCanvas = target.GetComponentInParent<Canvas>();
        var targetCamera = targetCanvas && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.worldCamera
            : (_rootCanvas && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? _rootCanvas.worldCamera : null);
        var renderCamera = GetEventCamera(renderParent);

        target.GetWorldCorners(_worldCorners);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < _worldCorners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(targetCamera, _worldCorners[i]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(renderParent, screenPoint, renderCamera, out var localPoint))
                continue;

            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        Vector2 size = (max - min) + (_padding * 2f) + _sizeAdjustment;
        if (_useMinimumSize)
        {
            size.x = Mathf.Max(size.x, minimumSize.x);
            size.y = Mathf.Max(size.y, minimumSize.y);
        }

        size.x = Mathf.Max(1f, size.x);
        size.y = Mathf.Max(1f, size.y);

        instance.anchorMin = new Vector2(0.5f, 0.5f);
        instance.anchorMax = new Vector2(0.5f, 0.5f);
        instance.pivot = new Vector2(0.5f, 0.5f);
        instance.localRotation = Quaternion.identity;
        instance.localScale = Vector3.one;
        instance.anchoredPosition = (min + max) * 0.5f;
        instance.sizeDelta = size;
    }

    void SetInstanceVisible(RectTransform instance, bool visible)
    {
        if (!instance)
            return;

        ApplyHighlightVisualSettings(instance);

        bool wasVisible = instance.gameObject.activeSelf;
        if (wasVisible != visible)
            instance.gameObject.SetActive(visible);

        if (visible && !wasVisible)
            RestartAnimators(instance);
    }

    void ApplyHighlightVisualSettings(RectTransform instance)
    {
        if (!instance)
            return;

        float alpha = Mathf.Clamp01(highlightAlpha / 255f);

        var graphics = instance.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i])
                graphics[i].raycastTarget = false;
        }

        var images = instance.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            var color = images[i].color;
            color.a = alpha;
            images[i].color = color;
        }

        var group = instance.GetComponent<CanvasGroup>();
        if (!group)
            group = instance.gameObject.AddComponent<CanvasGroup>();

        group.interactable = false;
        group.blocksRaycasts = false;

        var layoutElement = instance.GetComponent<LayoutElement>();
        if (!layoutElement)
            layoutElement = instance.gameObject.AddComponent<LayoutElement>();

        layoutElement.ignoreLayout = true;
    }

    void SetVisible(bool visible)
    {
        if (!canvasGroup) return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    void RestartAnimators(RectTransform instance)
    {
        var animators = instance.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            if (!animators[i])
                continue;

            animators[i].Rebind();
            animators[i].Update(0f);
        }
    }

    RectTransform GetRenderParent(RectTransform target)
    {
        if (renderAboveTutorialDimmer && canvasRoot)
            return canvasRoot;

        if (renderDirectlyUnderTarget && target && target.parent is RectTransform targetParent)
            return targetParent;

        return highlightContainerRoot ? highlightContainerRoot : canvasRoot;
    }

    Camera GetEventCamera(RectTransform rectTransform)
    {
        var canvas = rectTransform ? rectTransform.GetComponentInParent<Canvas>() : null;
        if (!canvas || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    void MoveInstanceToRenderParent(RectTransform instance, RectTransform target, RectTransform renderParent)
    {
        if (!instance || !target || !renderParent)
            return;

        if (instance.parent != renderParent)
            instance.SetParent(renderParent, false);

        if (renderAboveTutorialDimmer && renderParent == canvasRoot)
        {
            PlaceInstanceBelowActivePopup(instance, renderParent);
            return;
        }

        int targetIndex = target.GetSiblingIndex();
        int instanceIndex = instance.GetSiblingIndex();

        if (instance.parent == target.parent && instanceIndex < targetIndex)
            targetIndex--;

        instance.SetSiblingIndex(Mathf.Max(0, targetIndex));
    }

    void PlaceInstanceBelowActivePopup(RectTransform instance, RectTransform renderParent)
    {
        if (!instance || !renderParent)
            return;

        RectTransform popupRect = FindActivePopupRect(renderParent);
        Transform popupLayerSibling = GetLayerSiblingTransform(popupRect, renderParent);
        if (popupLayerSibling)
        {
            int popupIndex = popupLayerSibling.GetSiblingIndex();
            instance.SetSiblingIndex(Mathf.Max(0, popupIndex));
            return;
        }

        instance.SetAsLastSibling();
    }

    static RectTransform FindActivePopupRect(RectTransform renderParent)
    {
        var popups = FindObjectsByType<TutorialPopupView>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < popups.Length; i++)
        {
            var popup = popups[i];
            if (!popup || !popup.IsShowing || !popup.PopupRectTransform)
                continue;

            if (GetLayerSiblingTransform(popup.PopupRectTransform, renderParent))
                return popup.PopupRectTransform;
        }

        return null;
    }

    static Transform GetLayerSiblingTransform(Transform child, Transform layerParent)
    {
        if (!child || !layerParent)
            return null;

        Transform current = child;
        while (current && current.parent && current.parent != layerParent)
            current = current.parent;

        return current && current.parent == layerParent ? current : null;
    }
}
