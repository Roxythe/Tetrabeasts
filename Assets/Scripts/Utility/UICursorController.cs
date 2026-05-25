using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

    static bool s_virtualCursorMode;
    static int s_lastVirtualCursorFrame = -1000;
    static int s_lastVirtualSubmitFrame = -1000;
    static Vector2 s_currentScreenPosition;

    readonly System.Collections.Generic.List<RaycastResult> raycastResults = new();
    float _scale = 1f;
    bool externalVisible = true;
    bool inputVisible = true;
    CanvasGroup cursorCanvasGroup;
    Vector2 virtualScreenPosition;
    GameObject virtualPointerTarget;
    GameObject virtualClickTarget;

    public static bool IsVirtualCursorMode => s_virtualCursorMode;
    public static bool VirtualCursorActiveThisFrame => Time.frameCount == s_lastVirtualCursorFrame;
    public static bool ConsumedVirtualSubmitThisFrame => Time.frameCount == s_lastVirtualSubmitFrame;
    public static Vector2 CurrentScreenPosition => s_currentScreenPosition;
    public static void ConsumeSubmitThisFrame() => s_lastVirtualSubmitFrame = Time.frameCount;

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
            s_virtualCursorMode = false;
            inputVisible = true;
            if (wasInputHidden && TryGetSelectedScreenCenter(out var selectedCenter))
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
            s_virtualCursorMode = true;
            s_lastVirtualCursorFrame = Time.frameCount;
            inputVisible = true;

            if (wasInputHidden && TryGetSelectedScreenCenter(out var selectedCenter))
                virtualScreenPosition = selectedCenter;

            virtualScreenPosition += stick * (gamepadCursorSpeed * Time.unscaledDeltaTime);
            virtualScreenPosition.x = Mathf.Clamp(virtualScreenPosition.x, 0f, Screen.width);
            virtualScreenPosition.y = Mathf.Clamp(virtualScreenPosition.y, 0f, Screen.height);

            if (EventSystem.current)
                EventSystem.current.SetSelectedGameObject(null);
        }
        else if (buttonNavigationUsed)
        {
            s_virtualCursorMode = false;
            inputVisible = false;
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

        if (ConsumedVirtualSubmitThisFrame || !WasGamepadCursorSubmitPressed() || !virtualPointerTarget)
            return;

        if (!virtualClickTarget)
            return;

        pointerData.pointerPressRaycast = pointerData.pointerCurrentRaycast;
        pointerData.pointerPress = virtualClickTarget;
        ExecuteEvents.Execute(virtualClickTarget, pointerData, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(virtualClickTarget, pointerData, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(virtualClickTarget, pointerData, ExecuteEvents.pointerClickHandler);
        ConsumeSubmitThisFrame();
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

#if ENABLE_INPUT_SYSTEM
        var gamepad = Gamepad.current;
        pressed |= gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.JoystickButton0);
#endif

        return pressed;
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
            eventSystem.SetSelectedGameObject(null);
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

    static readonly List<Selectable> SelectableScratch = new();
    static readonly Dictionary<EventSystem, EventSystemNavigationState> EventSystemNavigationStates = new();
    static int s_lastSubmitFrame = -1000;

    public static bool IsSelectionUsableInside(GameObject selected, GameObject root)
    {
        if (!selected || !root || !selected.transform.IsChildOf(root.transform))
            return false;

        var selectable = selected.GetComponentInParent<Selectable>();
        return IsUsable(selectable);
    }

    public static bool SelectFirstUsable(GameObject root)
    {
        var selectables = GetUsableSelectables(root);
        if (selectables.Count == 0 || !EventSystem.current)
            return false;

        selectables.Sort(CompareTopToBottom);
        EventSystem.current.SetSelectedGameObject(selectables[0].gameObject);
        return true;
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

        EventSystem.current.SetSelectedGameObject(selectables[nextIndex].gameObject);
        return true;
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
            float score = (perpendicular * 8f) + forward;
            if (score >= bestScore)
                continue;

            bestScore = score;
            best = candidate;
        }

        if (!best)
            best = GetWrappedSelectable(selectables, currentSelectable, direction);

        if (!best)
            return false;

        EventSystem.current.SetSelectedGameObject(best.gameObject);
        return true;
    }

    public static bool TrySubmitSelected()
    {
        var eventSystem = EventSystem.current;
        if (!eventSystem ||
            UICursorController.ConsumedVirtualSubmitThisFrame ||
            s_lastSubmitFrame == Time.frameCount)
            return false;

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (!selected)
            return false;

        var selectable = selected.GetComponentInParent<Selectable>();
        if (!IsUsable(selectable))
            return false;

        var eventData = new BaseEventData(eventSystem);
        if (ExecuteEvents.Execute(selected, eventData, ExecuteEvents.submitHandler))
        {
            s_lastSubmitFrame = Time.frameCount;
            return true;
        }

        var button = selected.GetComponentInParent<Button>();
        if (!TryPressButton(button))
            return false;

        s_lastSubmitFrame = Time.frameCount;
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

                if (button.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return button;
            }
        }

        return null;
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
               selectable.interactable &&
               selectable.gameObject.activeInHierarchy;
    }

    class EventSystemNavigationState
    {
        public bool OriginalSendNavigationEvents;
        public int UseCount;
    }
}
