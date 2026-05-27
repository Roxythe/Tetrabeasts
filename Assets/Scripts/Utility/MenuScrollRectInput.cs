using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(ScrollRect))]
public class MenuScrollRectInput : MonoBehaviour
{
    [SerializeField] ScrollRect targetScrollRect;
    [SerializeField] GameObject focusRoot;
    [SerializeField] float controllerScrollSpeed = 900f;
    [SerializeField, Range(0.05f, 0.9f)] float controllerDeadzone = 0.2f;
    [SerializeField] bool autoCenterSelectedItem;

    GameObject lastCenteredSelection;
    Vector2 lastContentSize;
    Vector2 lastViewportSize;

    public static MenuScrollRectInput Attach(ScrollRect scrollRect, GameObject root = null, bool autoCenterSelected = false)
    {
        if (!scrollRect)
            return null;

        var input = scrollRect.GetComponent<MenuScrollRectInput>();
        if (!input)
            input = scrollRect.gameObject.AddComponent<MenuScrollRectInput>();

        input.Configure(scrollRect, root);
        if (autoCenterSelected)
            input.SetAutoCenterSelectedItem(true);

        return input;
    }

    public static void AttachToScrollRects(GameObject root)
    {
        if (!root)
            return;

        var scrollRects = root.GetComponentsInChildren<ScrollRect>(true);
        for (int i = 0; i < scrollRects.Length; i++)
            Attach(scrollRects[i], root);
    }

    public void Configure(ScrollRect scrollRect, GameObject root)
    {
        targetScrollRect = scrollRect ? scrollRect : GetComponent<ScrollRect>();
        focusRoot = root;
    }

    public void SetAutoCenterSelectedItem(bool enabled)
    {
        autoCenterSelectedItem = enabled;
        lastCenteredSelection = null;
    }

    void Awake()
    {
        if (!targetScrollRect)
            targetScrollRect = GetComponent<ScrollRect>();
    }

    void Update()
    {
        if (!CanScroll())
            return;

        Vector2 stick = TetrabeastsControls.GetMenuScrollDirection();
        bool hasVerticalInput = Mathf.Abs(stick.y) >= controllerDeadzone;
        bool hasHorizontalInput = Mathf.Abs(stick.x) >= controllerDeadzone;

        if (!hasVerticalInput && !hasHorizontalInput)
        {
            AutoCenterSelectedItemIfNeeded();
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;

        if (targetScrollRect.vertical && hasVerticalInput)
        {
            float scrollableHeight = GetScrollableHeight();
            if (scrollableHeight > 0.01f)
            {
                float delta = stick.y * controllerScrollSpeed * deltaTime / scrollableHeight;
                targetScrollRect.verticalNormalizedPosition =
                    Mathf.Clamp01(targetScrollRect.verticalNormalizedPosition + delta);
            }
        }

        if (targetScrollRect.horizontal && hasHorizontalInput)
        {
            float scrollableWidth = GetScrollableWidth();
            if (scrollableWidth > 0.01f)
            {
                float delta = stick.x * controllerScrollSpeed * deltaTime / scrollableWidth;
                targetScrollRect.horizontalNormalizedPosition =
                    Mathf.Clamp01(targetScrollRect.horizontalNormalizedPosition + delta);
            }
        }

        targetScrollRect.velocity = Vector2.zero;
    }

    void LateUpdate()
    {
        AutoCenterSelectedItemIfNeeded();
    }

    bool CanScroll()
    {
        if (!targetScrollRect || !targetScrollRect.isActiveAndEnabled || !targetScrollRect.gameObject.activeInHierarchy)
            return false;

        if (!targetScrollRect.content)
            return false;

        if (!focusRoot)
            return true;

        if (!UIPanelTransition.IsVisible(focusRoot))
            return false;

        var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (!selected)
            return true;

        if (!selected.transform.IsChildOf(focusRoot.transform))
            return false;

        return selected.transform.IsChildOf(targetScrollRect.transform);
    }

    float GetScrollableHeight()
    {
        var viewport = targetScrollRect.viewport
            ? targetScrollRect.viewport
            : targetScrollRect.transform as RectTransform;

        if (!viewport || !targetScrollRect.content)
            return 0f;

        return Mathf.Max(0f, targetScrollRect.content.rect.height - viewport.rect.height);
    }

    float GetScrollableWidth()
    {
        var viewport = targetScrollRect.viewport
            ? targetScrollRect.viewport
            : targetScrollRect.transform as RectTransform;

        if (!viewport || !targetScrollRect.content)
            return 0f;

        return Mathf.Max(0f, targetScrollRect.content.rect.width - viewport.rect.width);
    }

    void AutoCenterSelectedItemIfNeeded()
    {
        if (!autoCenterSelectedItem || !targetScrollRect || !targetScrollRect.vertical)
            return;

        if (!CanScroll())
            return;

        var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (!selected || !selected.transform.IsChildOf(targetScrollRect.content))
        {
            lastCenteredSelection = null;
            return;
        }

        var viewport = targetScrollRect.viewport
            ? targetScrollRect.viewport
            : targetScrollRect.transform as RectTransform;

        if (!viewport || !targetScrollRect.content)
            return;

        Vector2 contentSize = targetScrollRect.content.rect.size;
        Vector2 viewportSize = viewport.rect.size;
        if (selected == lastCenteredSelection &&
            contentSize == lastContentSize &&
            viewportSize == lastViewportSize)
        {
            return;
        }

        var item = GetDirectContentChild(selected.transform);
        if (!item)
            return;

        Canvas.ForceUpdateCanvases();
        CenterContentItem(item, viewport);

        lastCenteredSelection = selected;
        lastContentSize = targetScrollRect.content.rect.size;
        lastViewportSize = viewport.rect.size;
    }

    RectTransform GetDirectContentChild(Transform selected)
    {
        if (!selected || !targetScrollRect || !targetScrollRect.content)
            return null;

        Transform current = selected;
        while (current && current.parent && current.parent != targetScrollRect.content)
            current = current.parent;

        return current && current.parent == targetScrollRect.content
            ? current as RectTransform
            : selected as RectTransform;
    }

    void CenterContentItem(RectTransform item, RectTransform viewport)
    {
        float scrollableHeight = GetScrollableHeight();
        if (scrollableHeight <= 0.01f)
        {
            targetScrollRect.verticalNormalizedPosition = 1f;
            targetScrollRect.velocity = Vector2.zero;
            return;
        }

        Vector3 itemCenter = targetScrollRect.content.InverseTransformPoint(item.TransformPoint(item.rect.center));
        float itemCenterFromTop = targetScrollRect.content.rect.yMax - itemCenter.y;
        float desiredTopOffset = Mathf.Clamp(itemCenterFromTop - (viewport.rect.height * 0.5f), 0f, scrollableHeight);

        targetScrollRect.verticalNormalizedPosition = Mathf.Clamp01(1f - desiredTopOffset / scrollableHeight);
        targetScrollRect.velocity = Vector2.zero;
    }
}
