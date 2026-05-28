using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum UITargetInputSource
{
    None,
    MousePointer,
    VirtualCursor,
    ButtonNavigation
}

public class UICursorController : MonoBehaviour
{
    public RectTransform cursorRect;   
    public Canvas rootCanvas;
    public float baseSize = 48f;
    public Vector2 hotspotPixels = Vector2.zero; // (0,0) top-left
    public bool forceLastSibling = true;
    public bool cursorBlocksRaycasts = false; // Keep false so it never blocks dropdown clicks
    public bool hideWhenButtonNavigating = true;
    public bool allowGamepadCursor = true;
    public float gamepadCursorSpeed = 1150f;
    [Range(0.05f, 0.9f)] public float gamepadCursorDeadzone = 0.2f;
    public bool clampCursorVisualInsideScreen = true;
    public Vector2 cursorScreenPadding = Vector2.zero;

    static bool s_virtualCursorMode;
    static bool s_buttonNavigationMode;
    static int s_lastVirtualCursorFrame = -1000;
    static int s_lastVirtualSubmitFrame = -1000;
    static Vector2 s_currentScreenPosition;
    static UITargetInputSource s_targetInputSource = UITargetInputSource.MousePointer;
    static UITargetInputSource s_previousTargetInputSource = UITargetInputSource.None;
    static int s_targetInputSourceChangedFrame = -1000;

    readonly System.Collections.Generic.List<RaycastResult> raycastResults = new();
    float _scale = 1f;
    bool externalVisible = true;
    bool inputVisible = true;
    bool keepVisibleDuringButtonNavigation;
    CanvasGroup cursorCanvasGroup;
    Vector2 virtualScreenPosition;
    GameObject virtualPointerTarget;
    GameObject virtualClickTarget;

    public static bool IsVirtualCursorMode => s_virtualCursorMode;
    public static bool IsButtonNavigationMode => s_buttonNavigationMode;
    public static bool IsPointerTargetMode =>
        s_targetInputSource == UITargetInputSource.MousePointer ||
        s_targetInputSource == UITargetInputSource.VirtualCursor;
    public static UITargetInputSource CurrentTargetInputSource => s_targetInputSource;
    public static UITargetInputSource PreviousTargetInputSource =>
        Time.frameCount == s_targetInputSourceChangedFrame ? s_previousTargetInputSource : s_targetInputSource;
    public static bool TargetInputSourceChangedThisFrame => Time.frameCount == s_targetInputSourceChangedFrame;
    public static bool VirtualCursorActiveThisFrame => Time.frameCount == s_lastVirtualCursorFrame;
    public static bool ConsumedVirtualSubmitThisFrame => Time.frameCount == s_lastVirtualSubmitFrame;
    public static bool VirtualCursorSubmitPressedThisFrame => s_virtualCursorMode && WasGamepadCursorSubmitPressed();
    public static bool ShouldKeepNavigationSelection =>
        TetrabeastsControls.EffectiveProfile != TetrabeastsControlProfile.KeyboardMouse &&
        s_targetInputSource == UITargetInputSource.ButtonNavigation;
    public static Vector2 CurrentScreenPosition => s_currentScreenPosition;
    public static void ConsumeSubmitThisFrame() => s_lastVirtualSubmitFrame = Time.frameCount;

    public static bool ActivateButtonNavigationTargetSource()
    {
        bool changed = SetTargetInputSource(UITargetInputSource.ButtonNavigation);
        s_virtualCursorMode = false;
        s_buttonNavigationMode = true;
        return changed;
    }

    void Awake()
    {
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
        if (cursorRect)
        {
            externalVisible = cursorRect.gameObject.activeSelf;
            inputVisible = externalVisible;
        }

        // Make sure the cursor never blocks UI interactions
        if (cursorRect)
        {
            var img = cursorRect.GetComponent<Image>();
            if (img) img.raycastTarget = cursorBlocksRaycasts;

            cursorCanvasGroup = cursorRect.GetComponent<CanvasGroup>();
            if (!cursorCanvasGroup) cursorCanvasGroup = cursorRect.gameObject.AddComponent<CanvasGroup>();
            cursorCanvasGroup.blocksRaycasts = cursorBlocksRaycasts;
            cursorCanvasGroup.interactable = false;
        }

        virtualScreenPosition = Input.mousePosition;
        s_currentScreenPosition = virtualScreenPosition;
    }

    void Update()
    {
        if (!cursorRect || !rootCanvas) return;

        bool wasInputHidden = !inputVisible;
        bool useScreenOverrideThisFrame = false;
        bool mouseUsed = WasMouseUsedThisFrame();
        Vector2 stick = ReadGamepadCursorStick();
        bool virtualCursorUsed = allowGamepadCursor && stick.sqrMagnitude >= gamepadCursorDeadzone * gamepadCursorDeadzone;
        bool buttonNavigationUsed = hideWhenButtonNavigating &&
            (TetrabeastsControls.WasButtonNavigationPressedThisFrame() || TetrabeastsControls.IsButtonNavigationHeld());

        if (mouseUsed)
        {
            Vector2 selectedCenter = default;
            bool hasSelectedCenter = wasInputHidden && TryGetSelectedScreenCenter(out selectedCenter);
            ActivateMousePointerTargetSource();
            s_virtualCursorMode = false;
            s_buttonNavigationMode = false;
            inputVisible = true;
            if (hasSelectedCenter)
            {
                virtualScreenPosition = selectedCenter;
                WarpMousePosition(selectedCenter);
                useScreenOverrideThisFrame = true;
            }
            else
            {
                virtualScreenPosition = Input.mousePosition;
            }
        }
        else if (virtualCursorUsed)
        {
            Vector2 selectedCenter = default;
            bool hasSelectedCenter = wasInputHidden && TryGetSelectedScreenCenter(out selectedCenter);
            ActivateVirtualCursorTargetSource();
            s_virtualCursorMode = true;
            s_buttonNavigationMode = false;
            s_lastVirtualCursorFrame = Time.frameCount;
            inputVisible = true;

            if (hasSelectedCenter)
                virtualScreenPosition = selectedCenter;

            virtualScreenPosition += stick * (gamepadCursorSpeed * Time.unscaledDeltaTime);
            virtualScreenPosition = ClampVirtualCursorScreenPosition(virtualScreenPosition);

            if (EventSystem.current)
                EventSystem.current.SetSelectedGameObject(null);
        }
        else if (buttonNavigationUsed)
        {
            ActivateButtonNavigationTargetSource();
            inputVisible = keepVisibleDuringButtonNavigation;
            ClearVirtualPointerTarget();
        }

        ApplyCursorVisibility();

        if (!cursorRect.gameObject.activeSelf)
            return;

        Vector2 screen = s_virtualCursorMode || useScreenOverrideThisFrame
            ? virtualScreenPosition
            : (Vector2)Input.mousePosition;
        s_currentScreenPosition = screen;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            screen,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
            out var localPoint
        );

        // Apply hotspot offset
        float s = _scale;
        Vector2 hotspot = hotspotPixels * s;

        cursorRect.anchoredPosition = localPoint - hotspot;

        if (s_virtualCursorMode)
            ProcessVirtualPointer(screen);
    }

    public void SetScale(float scale)
    {
        _scale = Mathf.Clamp(scale, 0.25f, 4f);
        if (cursorRect)
            cursorRect.sizeDelta = Vector2.one * (baseSize * _scale);
    }

    public void SetVisible(bool visible)
    {
        externalVisible = visible;

        if (visible)
            inputVisible = true;
        else
            ClearVirtualPointerTarget();

        ApplyCursorVisibility();
    }

    public void SetKeepVisibleDuringButtonNavigation(bool keepVisible)
    {
        keepVisibleDuringButtonNavigation = keepVisible;

        if (keepVisible && externalVisible)
            inputVisible = true;

        ApplyCursorVisibility();
    }

    void ApplyCursorVisibility()
    {
        if (!cursorRect)
            return;

        if (!externalVisible)
        {
            cursorRect.gameObject.SetActive(false);
            return;
        }

        if (!cursorRect.gameObject.activeSelf)
            cursorRect.gameObject.SetActive(true);

        if (!cursorCanvasGroup)
            cursorCanvasGroup = cursorRect.GetComponent<CanvasGroup>();

        if (cursorCanvasGroup)
            cursorCanvasGroup.alpha = inputVisible ? 1f : 0f;
    }

    void ProcessVirtualPointer(Vector2 screenPosition)
    {
        var eventSystem = EventSystem.current;
        if (!eventSystem)
            return;

        var pointerData = new PointerEventData(eventSystem)
        {
            position = screenPosition,
            button = PointerEventData.InputButton.Left
        };

        raycastResults.Clear();
        eventSystem.RaycastAll(pointerData, raycastResults);

        GameObject target = null;
        GameObject clickTarget = null;
        if (raycastResults.Count > 0)
        {
            pointerData.pointerCurrentRaycast = raycastResults[0];
            var hitObject = raycastResults[0].gameObject;
            target = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(hitObject);
            if (!target)
                target = hitObject;

            clickTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitObject);
        }

        virtualClickTarget = clickTarget;

        if (target != virtualPointerTarget)
        {
            if (virtualPointerTarget)
                ExecuteEvents.Execute(virtualPointerTarget, pointerData, ExecuteEvents.pointerExitHandler);

            virtualPointerTarget = target;

            if (virtualPointerTarget)
                ExecuteEvents.Execute(virtualPointerTarget, pointerData, ExecuteEvents.pointerEnterHandler);
        }

        if (ConsumedVirtualSubmitThisFrame || !VirtualCursorSubmitPressedThisFrame)
            return;

        ConsumeSubmitThisFrame();
        eventSystem.SetSelectedGameObject(null);

        if (!virtualPointerTarget)
            return;

        if (!virtualClickTarget)
            return;

        pointerData.pointerPressRaycast = pointerData.pointerCurrentRaycast;
        pointerData.pointerPress = virtualClickTarget;
        ExecuteEvents.Execute(virtualClickTarget, pointerData, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(virtualClickTarget, pointerData, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(virtualClickTarget, pointerData, ExecuteEvents.pointerClickHandler);

        if (eventSystem)
            eventSystem.SetSelectedGameObject(null);
    }

    void ClearVirtualPointerTarget()
    {
        if (!virtualPointerTarget || !EventSystem.current)
        {
            virtualPointerTarget = null;
            virtualClickTarget = null;
            return;
        }

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = virtualScreenPosition,
            button = PointerEventData.InputButton.Left
        };

        ExecuteEvents.Execute(virtualPointerTarget, pointerData, ExecuteEvents.pointerExitHandler);
        virtualPointerTarget = null;
        virtualClickTarget = null;
    }

    static bool WasMouseUsedThisFrame()
    {
        bool used = false;

#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        used |= mouse != null &&
            (mouse.leftButton.wasPressedThisFrame ||
             mouse.rightButton.wasPressedThisFrame ||
             mouse.middleButton.wasPressedThisFrame ||
             mouse.delta.ReadValue().sqrMagnitude > 0.01f ||
             mouse.scroll.ReadValue().sqrMagnitude > 0.01f);
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        used |= Input.GetMouseButtonDown(0) ||
                Input.GetMouseButtonDown(1) ||
                Input.GetMouseButtonDown(2) ||
                Mathf.Abs(Input.GetAxisRaw("Mouse X")) > 0.01f ||
                Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.01f;
#endif

        return used;
    }

    static void ActivateMousePointerTargetSource()
    {
        if (SetTargetInputSource(UITargetInputSource.MousePointer))
            ClearEventSystemSelection();
    }

    static void ActivateVirtualCursorTargetSource()
    {
        if (SetTargetInputSource(UITargetInputSource.VirtualCursor))
            ClearEventSystemSelection();
    }

    static bool SetTargetInputSource(UITargetInputSource source)
    {
        if (s_targetInputSource == source)
            return false;

        s_previousTargetInputSource = s_targetInputSource;
        s_targetInputSource = source;
        s_targetInputSourceChangedFrame = Time.frameCount;
        return true;
    }

    static void ClearEventSystemSelection()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem && eventSystem.currentSelectedGameObject)
            eventSystem.SetSelectedGameObject(null);
    }

    static Vector2 ReadGamepadCursorStick()
    {
        Vector2 stick = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        var gamepad = Gamepad.current;
        if (gamepad != null)
            stick += gamepad.leftStick.ReadValue();
#endif

        return stick.sqrMagnitude > 1f ? stick.normalized : stick;
    }

    static bool TryGetSelectedScreenCenter(out Vector2 screenCenter)
    {
        screenCenter = default;

        var eventSystem = EventSystem.current;
        GameObject selected = eventSystem ? eventSystem.currentSelectedGameObject : null;
        if (!selected)
            return false;

        var rectTransform = selected.GetComponentInParent<RectTransform>();
        if (!rectTransform)
            return false;

        Camera camera = null;
        var canvas = selected.GetComponentInParent<Canvas>();
        if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            camera = canvas.worldCamera;

        screenCenter = RectTransformUtility.WorldToScreenPoint(
            camera,
            rectTransform.TransformPoint(rectTransform.rect.center));
        return true;
    }

    static void WarpMousePosition(Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null)
            mouse.WarpCursorPosition(screenPosition);
#endif
    }

    static bool WasGamepadCursorSubmitPressed()
    {
        bool pressed = false;

        pressed |= TetrabeastsControls.WasPressed(TetrabeastsControlAction.MenuSubmit);

#if ENABLE_INPUT_SYSTEM
        var gamepad = Gamepad.current;
        pressed |= gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.JoystickButton0);
#endif

        return pressed;
    }

    Vector2 ClampVirtualCursorScreenPosition(Vector2 screenPosition)
    {
        if (!clampCursorVisualInsideScreen || !cursorRect)
        {
            screenPosition.x = Mathf.Clamp(screenPosition.x, 0f, Screen.width);
            screenPosition.y = Mathf.Clamp(screenPosition.y, 0f, Screen.height);
            return screenPosition;
        }

        Vector2 size = cursorRect.rect.size;
        if (size.x <= 0f || size.y <= 0f)
            size = Vector2.one * (baseSize * _scale);

        Vector2 hotspot = hotspotPixels * _scale;
        Vector2 pivot = cursorRect.pivot;

        float scaleFactor = rootCanvas ? Mathf.Max(0.01f, rootCanvas.scaleFactor) : 1f;
        float leftExtent = Mathf.Max(0f, (hotspot.x + pivot.x * size.x) * scaleFactor);
        float rightExtent = Mathf.Max(0f, ((1f - pivot.x) * size.x - hotspot.x) * scaleFactor);
        float bottomExtent = Mathf.Max(0f, (hotspot.y + pivot.y * size.y) * scaleFactor);
        float topExtent = Mathf.Max(0f, ((1f - pivot.y) * size.y - hotspot.y) * scaleFactor);

        float minX = cursorScreenPadding.x + leftExtent;
        float maxX = Screen.width - cursorScreenPadding.x - rightExtent;
        float minY = cursorScreenPadding.y + bottomExtent;
        float maxY = Screen.height - cursorScreenPadding.y - topExtent;

        if (minX > maxX)
            screenPosition.x = Screen.width * 0.5f;
        else
            screenPosition.x = Mathf.Clamp(screenPosition.x, minX, maxX);

        if (minY > maxY)
            screenPosition.y = Screen.height * 0.5f;
        else
            screenPosition.y = Mathf.Clamp(screenPosition.y, minY, maxY);

        return screenPosition;
    }

    void LateUpdate()
    {
        if (!forceLastSibling) return;
        if (!gameObject.activeInHierarchy) return;

        transform.SetAsLastSibling();
    }

}

[DisallowMultipleComponent]
public class ScopedMenuNavigator : MonoBehaviour
{
    const float NavigationRepeatDelay = 0.28f;
    const float NavigationRepeatInterval = 0.12f;

    [SerializeField] GameObject navigationRoot;

    readonly Dictionary<Selectable, Navigation> savedNavigation = new();

    GameObject automaticNavigationRoot;
    bool eventSystemNavigationSuppressed;
    Vector2 heldNavigationDirection;
    float nextNavigationTime;

    public GameObject NavigationRoot => navigationRoot;

    public static ScopedMenuNavigator Attach(GameObject owner, GameObject root = null)
    {
        if (!owner)
            return null;

        var navigator = owner.GetComponent<ScopedMenuNavigator>();
        if (!navigator)
            navigator = owner.AddComponent<ScopedMenuNavigator>();

        navigator.SetNavigationRoot(root ? root : owner, selectFirst: false);
        return navigator;
    }

    public void SetNavigationRoot(GameObject root, bool selectFirst = false)
    {
        if (navigationRoot == root)
        {
            if (selectFirst)
                SelectFirstUsable();
            return;
        }

        RestoreAutomaticNavigationScope();
        navigationRoot = root;
        MenuScrollRectInput.AttachToScrollRects(navigationRoot);
        ResetNavigationRepeat();

        if (selectFirst)
            SelectFirstUsable();
    }

    public bool SelectFirstUsable()
    {
        if (!navigationRoot || !navigationRoot.activeInHierarchy)
            return false;

        return UINavigationUtility.SelectFirstUsable(navigationRoot);
    }

    void OnDisable()
    {
        ClearSelectionIfInside();
        RestoreAutomaticNavigationScope();
        ResetNavigationRepeat();
    }

    void Update()
    {
        if (!navigationRoot || !navigationRoot.activeInHierarchy)
            return;

        TetrabeastsControls.RefreshActiveInputProfile();
        RefreshAutomaticNavigationScope(navigationRoot);

        var current = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (current && !UINavigationUtility.IsSelectionUsableInside(current, navigationRoot))
            EventSystem.current.SetSelectedGameObject(null);

        if (TetrabeastsControls.WasPressed(TetrabeastsControlAction.MenuSubmit))
        {
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (UINavigationUtility.IsSelectionUsableInside(selected, navigationRoot))
            {
                if (UINavigationUtility.TrySubmitSelected())
                    return;
            }
            else if (SelectFirstOrSubmitOnlyControl())
            {
                return;
            }
        }

        HandleSpatialNavigation();
    }

    void LateUpdate()
    {
        if (!navigationRoot || !navigationRoot.activeInHierarchy)
            return;

        if (UICursorController.IsButtonNavigationMode)
            UINavigationUtility.ReleasePointerHoverForNavigation(navigationRoot, force: true);
    }

    void HandleSpatialNavigation()
    {
        bool tabPressed = WasTabPressedThisFrame();
        bool navigationInput = tabPressed ||
            TetrabeastsControls.WasButtonNavigationPressedThisFrame() ||
            TetrabeastsControls.IsButtonNavigationHeld();

        if (!navigationInput)
        {
            ResetNavigationRepeat();
            return;
        }

        UINavigationUtility.BeginButtonNavigation(navigationRoot);

        var current = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (!UINavigationUtility.IsSelectionUsableInside(current, navigationRoot))
        {
            SelectFirstUsable();
            ResetNavigationRepeat();
            return;
        }

        if (tabPressed)
        {
            UINavigationUtility.SelectNextInOrder(navigationRoot, IsShiftHeld() ? -1 : 1);
            ResetNavigationRepeat();
            return;
        }

        if (!TryConsumeDirectionalNavigation(out Vector2 direction))
            return;

        UINavigationUtility.SelectInDirection(navigationRoot, direction);
    }

    bool SelectFirstOrSubmitOnlyControl()
    {
        var selectables = UINavigationUtility.GetUsableSelectables(navigationRoot);
        if (selectables.Count == 0)
            return false;

        if (!UINavigationUtility.SelectFirstUsable(navigationRoot))
            return false;

        return selectables.Count > 1 || UINavigationUtility.TrySubmitSelected();
    }

    bool TryConsumeDirectionalNavigation(out Vector2 direction)
    {
        direction = UINavigationUtility.GetPrimaryDirection(TetrabeastsControls.GetButtonNavigationDirection());
        if (direction.sqrMagnitude < 0.5f)
        {
            ResetNavigationRepeat();
            return false;
        }

        float now = Time.unscaledTime;
        if (direction != heldNavigationDirection)
        {
            heldNavigationDirection = direction;
            nextNavigationTime = now + NavigationRepeatDelay;
            return true;
        }

        if (now < nextNavigationTime)
            return false;

        nextNavigationTime = now + NavigationRepeatInterval;
        return true;
    }

    void ResetNavigationRepeat()
    {
        heldNavigationDirection = Vector2.zero;
        nextNavigationTime = 0f;
    }

    void RefreshAutomaticNavigationScope(GameObject root)
    {
        if (root != automaticNavigationRoot)
        {
            RestoreAutomaticNavigationScope();
            automaticNavigationRoot = root;
            UINavigationUtility.SuppressEventSystemNavigation();
            eventSystemNavigationSuppressed = true;
        }

        UINavigationUtility.SuppressAutomaticNavigation(root, savedNavigation);
    }

    void RestoreAutomaticNavigationScope()
    {
        UINavigationUtility.RestoreAutomaticNavigation(savedNavigation);
        if (eventSystemNavigationSuppressed)
        {
            UINavigationUtility.RestoreEventSystemNavigation();
            eventSystemNavigationSuppressed = false;
        }

        automaticNavigationRoot = null;
    }

    void ClearSelectionIfInside()
    {
        var eventSystem = EventSystem.current;
        if (!eventSystem || !navigationRoot)
            return;

        var current = eventSystem.currentSelectedGameObject;
        if (current && current.transform.IsChildOf(navigationRoot.transform))
        {
            UINavigationUtility.RememberSelection(navigationRoot, current);
            eventSystem.SetSelectedGameObject(null);
        }
    }

    static bool WasTabPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard.tabKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Tab);
#else
        return false;
#endif
    }

    static bool IsShiftHeld()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
        return false;
#endif
    }
}

public static class UINavigationUtility
{
    const float MinForwardDistance = 0.5f;
    const float SubmitDebounceSeconds = 0.08f;

    static readonly List<Selectable> SelectableScratch = new();
    static readonly List<RaycastResult> PointerHoverRaycastScratch = new();
    static readonly HashSet<GameObject> PointerHoverExitTargetsScratch = new();
    static readonly Dictionary<EventSystem, EventSystemNavigationState> EventSystemNavigationStates = new();
    static readonly Dictionary<int, string> LastSelectionPathsByRoot = new();
    static int s_lastSubmitFrame = -1000;
    static float s_lastSubmitTime = -999f;
    static GameObject s_lastSubmitObject;
    static int s_lastPointerHoverReleaseFrame = -1000;

    public static bool ShouldKeepNavigationSelection => UICursorController.ShouldKeepNavigationSelection;

    public static bool IsSelectionUsableInside(GameObject selected, GameObject root)
    {
        if (!selected || !root || !selected.transform.IsChildOf(root.transform))
            return false;

        var selectable = selected.GetComponentInParent<Selectable>();
        return IsUsable(selectable);
    }

    public static bool SelectFirstUsable(GameObject root, params string[] preferredNameParts)
    {
        var selectables = GetUsableSelectables(root);
        if (selectables.Count == 0 || !EventSystem.current)
            return false;

        var remembered = ShouldKeepNavigationSelection ? FindRememberedSelectable(root, selectables) : null;
        if (remembered)
            return SelectSelectable(root, remembered);

        var preferred = FindPreferredSelectable(selectables, preferredNameParts);
        if (preferred)
            return SelectSelectable(root, preferred);

        selectables.Sort(CompareTopToBottom);
        return SelectSelectable(root, selectables[0]);
    }

    public static bool SelectNextInOrder(GameObject root, int direction)
    {
        var selectables = GetUsableSelectables(root);
        if (selectables.Count == 0 || !EventSystem.current)
            return false;

        selectables.Sort(CompareTopToBottom);

        GameObject current = EventSystem.current.currentSelectedGameObject;
        int currentIndex = -1;
        if (current)
        {
            for (int i = 0; i < selectables.Count; i++)
            {
                if (selectables[i] && selectables[i].gameObject == current)
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        int nextIndex = currentIndex < 0
            ? (direction >= 0 ? 0 : selectables.Count - 1)
            : (currentIndex + direction + selectables.Count) % selectables.Count;

        return SelectSelectable(root, selectables[nextIndex]);
    }

    public static bool SelectInDirection(GameObject root, Vector2 direction)
    {
        direction = GetPrimaryDirection(direction);
        if (direction.sqrMagnitude < 0.5f || !EventSystem.current)
            return false;

        var selectables = GetUsableSelectables(root);
        if (selectables.Count == 0)
            return false;

        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (!IsSelectionUsableInside(current, root))
            return SelectFirstUsable(root);

        var currentSelectable = current.GetComponentInParent<Selectable>();
        if (!currentSelectable)
            return SelectFirstUsable(root);

        Vector2 currentCenter = GetScreenCenter(currentSelectable);
        RectTransform currentScrollItem = GetScrollContentItem(currentSelectable.transform);
        Selectable best = null;
        float bestScore = float.PositiveInfinity;

        for (int i = 0; i < selectables.Count; i++)
        {
            var candidate = selectables[i];
            if (!candidate || candidate == currentSelectable)
                continue;

            Vector2 delta = GetScreenCenter(candidate) - currentCenter;
            float forward = Vector2.Dot(delta, direction);
            if (forward <= MinForwardDistance)
                continue;

            float perpendicular = Mathf.Abs(delta.x * direction.y - delta.y * direction.x);
            bool sameScrollItem = currentScrollItem &&
                GetScrollContentItem(candidate.transform) == currentScrollItem;
            bool perpendicularOverlap = RectsOverlapOnPerpendicularAxis(currentSelectable, candidate, direction);
            float score = (perpendicular * 8f) + forward;

            if (sameScrollItem)
                score -= 100000f;

            if (perpendicularOverlap)
                score -= 1000f;

            if (score >= bestScore)
                continue;

            bestScore = score;
            best = candidate;
        }

        if (!best)
            best = GetWrappedSelectable(selectables, currentSelectable, direction);

        if (!best)
            return false;

        return SelectSelectable(root, best);
    }

    public static void RememberSelection(GameObject root, GameObject selected)
    {
        if (!ShouldKeepNavigationSelection)
            return;

        if (!root || !selected || !selected.transform.IsChildOf(root.transform))
            return;

        var selectable = selected.GetComponentInParent<Selectable>();
        if (!IsUsable(selectable) || !selectable.transform.IsChildOf(root.transform))
            return;

        string path = BuildRelativePath(root.transform, selectable.transform);
        if (!string.IsNullOrEmpty(path))
            LastSelectionPathsByRoot[root.GetInstanceID()] = path;
    }

    public static bool TrySubmitSelected()
    {
        var eventSystem = EventSystem.current;
        if (!eventSystem ||
            UICursorController.ConsumedVirtualSubmitThisFrame ||
            UICursorController.VirtualCursorSubmitPressedThisFrame ||
            s_lastSubmitFrame == Time.frameCount)
            return false;

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (!selected)
            return false;

        if (selected == s_lastSubmitObject &&
            Time.unscaledTime - s_lastSubmitTime < SubmitDebounceSeconds)
        {
            return false;
        }

        var selectable = selected.GetComponentInParent<Selectable>();
        if (!IsUsable(selectable))
            return false;

        var eventData = new BaseEventData(eventSystem);
        if (ExecuteEvents.Execute(selected, eventData, ExecuteEvents.submitHandler))
        {
            MarkSubmit(selected);
            return true;
        }

        var button = selected.GetComponentInParent<Button>();
        if (!TryPressButton(button))
            return false;

        MarkSubmit(selected);
        return true;
    }

    public static bool TryPressButton(Button button)
    {
        if (!IsUsable(button))
            return false;

        var eventSystem = EventSystem.current;
        if (eventSystem)
        {
            var eventData = new BaseEventData(eventSystem);
            if (ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.submitHandler))
                return true;
        }

        button.onClick.Invoke();
        return true;
    }

    public static bool TryPressNamedButton(GameObject root, params string[] nameParts)
    {
        Button button = FindNamedButton(root, nameParts);
        return TryPressButton(button);
    }

    public static void BeginButtonNavigation(GameObject root)
    {
        UITargetInputSource previousSource = UICursorController.TargetInputSourceChangedThisFrame
            ? UICursorController.PreviousTargetInputSource
            : UICursorController.CurrentTargetInputSource;
        Vector2 pointerPosition = GetPointerScreenPosition(previousSource);
        bool sourceChanged = UICursorController.ActivateButtonNavigationTargetSource();
        bool adoptPointerTarget = sourceChanged ||
            (UICursorController.TargetInputSourceChangedThisFrame &&
             IsPointerSource(UICursorController.PreviousTargetInputSource));

        if (adoptPointerTarget && !SelectPointerTarget(root, pointerPosition))
            ClearSelectionInside(root);

        ReleasePointerHoverForNavigation(root, pointerPosition);
    }

    public static void ReleasePointerHoverForNavigation(GameObject root)
    {
        ReleasePointerHoverForNavigation(root, force: false);
    }

    public static void ReleasePointerHoverForNavigation(GameObject root, bool force)
    {
        ReleasePointerHoverForNavigation(root, GetPointerScreenPosition(UICursorController.CurrentTargetInputSource), force);
    }

    static void ReleasePointerHoverForNavigation(GameObject root, Vector2 pointerPosition, bool force = false)
    {
        var eventSystem = EventSystem.current;
        if (!eventSystem || (!force && Time.frameCount == s_lastPointerHoverReleaseFrame))
            return;

        s_lastPointerHoverReleaseFrame = Time.frameCount;

        var pointerEventData = new PointerEventData(eventSystem)
        {
            position = pointerPosition
        };

        PointerHoverRaycastScratch.Clear();
        PointerHoverExitTargetsScratch.Clear();
        eventSystem.RaycastAll(pointerEventData, PointerHoverRaycastScratch);

        for (int i = 0; i < PointerHoverRaycastScratch.Count; i++)
        {
            var hit = PointerHoverRaycastScratch[i].gameObject;
            if (!hit || (root && !hit.transform.IsChildOf(root.transform)))
                continue;

            var selectable = hit.GetComponentInParent<Selectable>();
            if (!IsUsable(selectable))
                continue;

            ExecutePointerExitHandlers(hit.transform, selectable.transform, pointerEventData);
        }

        PointerHoverRaycastScratch.Clear();
        PointerHoverExitTargetsScratch.Clear();
    }

    static bool SelectPointerTarget(GameObject root, Vector2 pointerPosition)
    {
        var selectable = FindSelectableUnderPointer(root, pointerPosition);
        if (!selectable)
            return false;

        return SelectSelectable(root, selectable);
    }

    static void ClearSelectionInside(GameObject root)
    {
        var eventSystem = EventSystem.current;
        if (!eventSystem)
            return;

        var selected = eventSystem.currentSelectedGameObject;
        if (selected && (!root || selected.transform.IsChildOf(root.transform)))
            eventSystem.SetSelectedGameObject(null);
    }

    static Selectable FindSelectableUnderPointer(GameObject root, Vector2 pointerPosition)
    {
        var eventSystem = EventSystem.current;
        if (!eventSystem)
            return null;

        var pointerEventData = new PointerEventData(eventSystem)
        {
            position = pointerPosition
        };

        PointerHoverRaycastScratch.Clear();
        eventSystem.RaycastAll(pointerEventData, PointerHoverRaycastScratch);

        for (int i = 0; i < PointerHoverRaycastScratch.Count; i++)
        {
            var hit = PointerHoverRaycastScratch[i].gameObject;
            if (!hit || (root && !hit.transform.IsChildOf(root.transform)))
                continue;

            var selectable = hit.GetComponentInParent<Selectable>();
            if (IsUsable(selectable) && (!root || selectable.transform.IsChildOf(root.transform)))
            {
                PointerHoverRaycastScratch.Clear();
                return selectable;
            }
        }

        PointerHoverRaycastScratch.Clear();
        return null;
    }

    public static List<Selectable> GetUsableSelectables(GameObject root)
    {
        SelectableScratch.Clear();
        if (!root)
            return new List<Selectable>();

        var selectables = root.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            var selectable = selectables[i];
            if (!IsUsable(selectable))
                continue;

            SelectableScratch.Add(selectable);
        }

        return new List<Selectable>(SelectableScratch);
    }

    static Button FindNamedButton(GameObject root, string[] nameParts)
    {
        if (!root || nameParts == null || nameParts.Length == 0)
            return null;

        var buttons = root.GetComponentsInChildren<Button>(true);
        for (int p = 0; p < nameParts.Length; p++)
        {
            string namePart = nameParts[p];
            if (string.IsNullOrWhiteSpace(namePart))
                continue;

            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (!IsUsable(button))
                    continue;

                if (button.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ObjectMatchesNameParts(button.gameObject, nameParts))
                    return button;
            }
        }

        return null;
    }

    static void ExecutePointerExitHandlers(Transform hit, Transform selectable, PointerEventData pointerEventData)
    {
        Transform current = hit;
        while (current)
        {
            if (PointerHoverExitTargetsScratch.Add(current.gameObject))
                ExecuteEvents.Execute(current.gameObject, pointerEventData, ExecuteEvents.pointerExitHandler);

            if (current == selectable)
                return;

            current = current.parent;
        }
    }

    static bool IsPointerSource(UITargetInputSource source)
    {
        return source == UITargetInputSource.MousePointer ||
               source == UITargetInputSource.VirtualCursor ||
               source == UITargetInputSource.None;
    }

    static Vector2 GetPointerScreenPosition(UITargetInputSource source)
    {
        if (source == UITargetInputSource.VirtualCursor)
            return UICursorController.CurrentScreenPosition;

#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null)
            return mouse.position.ReadValue();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return UICursorController.CurrentScreenPosition;
#endif
    }

    public static void SuppressAutomaticNavigation(GameObject root, Dictionary<Selectable, Navigation> savedNavigation)
    {
        if (!root || savedNavigation == null)
            return;

        var selectables = root.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            var selectable = selectables[i];
            if (!selectable || savedNavigation.ContainsKey(selectable))
                continue;

            savedNavigation[selectable] = selectable.navigation;
            var navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.None;
            selectable.navigation = navigation;
        }
    }

    public static void RestoreAutomaticNavigation(Dictionary<Selectable, Navigation> savedNavigation)
    {
        if (savedNavigation == null)
            return;

        foreach (var pair in savedNavigation)
        {
            if (pair.Key)
                pair.Key.navigation = pair.Value;
        }

        savedNavigation.Clear();
    }

    public static void SuppressEventSystemNavigation()
    {
        var eventSystem = EventSystem.current;
        if (!eventSystem)
            return;

        if (!EventSystemNavigationStates.TryGetValue(eventSystem, out var state))
        {
            state = new EventSystemNavigationState
            {
                OriginalSendNavigationEvents = eventSystem.sendNavigationEvents
            };
            EventSystemNavigationStates[eventSystem] = state;
        }

        state.UseCount++;
        eventSystem.sendNavigationEvents = false;
    }

    public static void RestoreEventSystemNavigation()
    {
        var eventSystem = EventSystem.current;
        if (!eventSystem)
            return;

        if (!EventSystemNavigationStates.TryGetValue(eventSystem, out var state))
            return;

        state.UseCount = Mathf.Max(0, state.UseCount - 1);
        if (state.UseCount > 0)
            return;

        eventSystem.sendNavigationEvents = state.OriginalSendNavigationEvents;
        EventSystemNavigationStates.Remove(eventSystem);
    }

    public static Vector2 GetPrimaryDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.25f)
            return Vector2.zero;

        return Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0f)
            : new Vector2(0f, Mathf.Sign(direction.y));
    }

    static Selectable GetWrappedSelectable(List<Selectable> selectables, Selectable current, Vector2 direction)
    {
        Selectable wrapped = null;
        float bestProjection = float.PositiveInfinity;

        for (int i = 0; i < selectables.Count; i++)
        {
            var candidate = selectables[i];
            if (!candidate || candidate == current)
                continue;

            float projection = Vector2.Dot(GetScreenCenter(candidate), direction);
            if (projection >= bestProjection)
                continue;

            bestProjection = projection;
            wrapped = candidate;
        }

        return wrapped;
    }

    static void MarkSubmit(GameObject selected)
    {
        s_lastSubmitFrame = Time.frameCount;
        s_lastSubmitTime = Time.unscaledTime;
        s_lastSubmitObject = selected;
    }

    static RectTransform GetScrollContentItem(Transform target)
    {
        if (!target)
            return null;

        var scrollRect = target.GetComponentInParent<ScrollRect>();
        var content = scrollRect ? scrollRect.content : null;
        if (!content || !target.IsChildOf(content))
            return null;

        Transform current = target;
        while (current && current.parent && current.parent != content)
            current = current.parent;

        return current && current.parent == content
            ? current as RectTransform
            : null;
    }

    static bool RectsOverlapOnPerpendicularAxis(Selectable current, Selectable candidate, Vector2 direction)
    {
        var currentRect = current ? current.transform as RectTransform : null;
        var candidateRect = candidate ? candidate.transform as RectTransform : null;
        if (!currentRect || !candidateRect)
            return false;

        var currentBounds = GetScreenBounds(currentRect);
        var candidateBounds = GetScreenBounds(candidateRect);

        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
            return currentBounds.min.x <= candidateBounds.max.x &&
                   currentBounds.max.x >= candidateBounds.min.x;

        return currentBounds.min.y <= candidateBounds.max.y &&
               currentBounds.max.y >= candidateBounds.min.y;
    }

    static Bounds GetScreenBounds(RectTransform rectTransform)
    {
        var corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Camera camera = null;
        var canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            camera = canvas.worldCamera;

        Bounds bounds = new Bounds(RectTransformUtility.WorldToScreenPoint(camera, corners[0]), Vector3.zero);
        for (int i = 1; i < corners.Length; i++)
            bounds.Encapsulate(RectTransformUtility.WorldToScreenPoint(camera, corners[i]));

        return bounds;
    }

    static bool SelectSelectable(GameObject root, Selectable selectable)
    {
        if (!IsUsable(selectable) || !EventSystem.current)
            return false;

        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        if (ShouldKeepNavigationSelection)
            RememberSelection(root, selectable.gameObject);
        return true;
    }

    static Selectable FindRememberedSelectable(GameObject root, List<Selectable> selectables)
    {
        if (!root || selectables == null || selectables.Count == 0)
            return null;

        if (!LastSelectionPathsByRoot.TryGetValue(root.GetInstanceID(), out string rememberedPath) ||
            string.IsNullOrEmpty(rememberedPath))
            return null;

        for (int i = 0; i < selectables.Count; i++)
        {
            var selectable = selectables[i];
            if (!selectable)
                continue;

            if (BuildRelativePath(root.transform, selectable.transform) == rememberedPath)
                return selectable;
        }

        return null;
    }

    static Selectable FindPreferredSelectable(List<Selectable> selectables, string[] preferredNameParts)
    {
        if (selectables == null || selectables.Count == 0 ||
            preferredNameParts == null || preferredNameParts.Length == 0)
            return null;

        for (int i = 0; i < selectables.Count; i++)
        {
            var selectable = selectables[i];
            if (selectable && ObjectMatchesNameParts(selectable.gameObject, preferredNameParts))
                return selectable;
        }

        return null;
    }

    static bool ObjectMatchesNameParts(GameObject target, string[] nameParts)
    {
        if (!target || nameParts == null || nameParts.Length == 0)
            return false;

        string searchable = target.name;
        var labels = target.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] && !string.IsNullOrWhiteSpace(labels[i].text))
                searchable += " " + labels[i].text;
        }

        for (int i = 0; i < nameParts.Length; i++)
        {
            string part = nameParts[i];
            if (string.IsNullOrWhiteSpace(part))
                continue;

            if (searchable.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) < 0)
                return false;
        }

        return true;
    }

    static string BuildRelativePath(Transform root, Transform child)
    {
        if (!root || !child || !child.IsChildOf(root))
            return null;

        var parts = new List<string>();
        Transform current = child;
        while (current && current != root)
        {
            parts.Add(current.name);
            current = current.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    static int CompareTopToBottom(Selectable left, Selectable right)
    {
        Vector2 leftCenter = GetScreenCenter(left);
        Vector2 rightCenter = GetScreenCenter(right);

        int y = -leftCenter.y.CompareTo(rightCenter.y);
        return y != 0 ? y : leftCenter.x.CompareTo(rightCenter.x);
    }

    static Vector2 GetScreenCenter(Selectable selectable)
    {
        if (!selectable)
            return Vector2.zero;

        var rectTransform = selectable.transform as RectTransform;
        if (!rectTransform)
            return selectable.transform.position;

        Camera camera = null;
        var canvas = selectable.GetComponentInParent<Canvas>();
        if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            camera = canvas.worldCamera;

        return RectTransformUtility.WorldToScreenPoint(camera, rectTransform.TransformPoint(rectTransform.rect.center));
    }

    static bool IsUsable(Selectable selectable)
    {
        return selectable &&
               selectable.isActiveAndEnabled &&
               selectable.IsInteractable() &&
               selectable.gameObject.activeInHierarchy;
    }

    class EventSystemNavigationState
    {
        public bool OriginalSendNavigationEvents;
        public int UseCount;
    }
}
