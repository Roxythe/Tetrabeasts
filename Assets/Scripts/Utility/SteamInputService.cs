using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
using Steamworks;
#endif

[DisallowMultipleComponent]
public sealed class SteamInputService : MonoBehaviour
{
    const string ManifestFileName = "steam_input_manifest.vdf";
    const string GameplayActionSetName = "Gameplay";
    const string MoveAnalogActionName = "Move";
    const string MenuNavigateAnalogActionName = "MenuNavigate";
    const float AnalogPressedThreshold = 0.5f;

    static SteamInputService _instance;

    public static event Action ControllerDisconnected;

    [SerializeField] bool logSteamInputStatus = true;

    string lastStatus = "Steam Input is not initialized.";
    float nextInitializeAttemptTime;

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
    readonly InputHandle_t[] controllerHandles = new InputHandle_t[Constants.STEAM_INPUT_MAX_COUNT];
    readonly EInputActionOrigin[] originBuffer = new EInputActionOrigin[Constants.STEAM_INPUT_MAX_ORIGINS];
    readonly Dictionary<TetrabeastsControlAction, InputDigitalActionHandle_t> digitalHandles = new();
    readonly Dictionary<TetrabeastsControlAction, bool> currentHeld = new();
    readonly Dictionary<TetrabeastsControlAction, bool> previousHeld = new();
    readonly List<string> labelParts = new();
    readonly List<string> glyphPaths = new();

    InputActionSetHandle_t gameplayActionSet;
    InputAnalogActionHandle_t moveAnalogAction;
    InputAnalogActionHandle_t menuNavigateAnalogAction;
    InputHandle_t activeController;
    int connectedControllerCount;
    int lastFrameUpdated = -1;
    int lastDisconnectNotificationFrame = -1;
    bool steamInputInitialized;
    bool handlesResolved;

    Callback<SteamInputDeviceConnected_t> deviceConnectedCallback;
    Callback<SteamInputDeviceDisconnected_t> deviceDisconnectedCallback;
#endif

    public static SteamInputService Instance => _instance;
    public string LastStatus => lastStatus;

    public static bool IsAvailable
    {
        get
        {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
            return Application.isPlaying && Ensure() && _instance.steamInputInitialized;
#else
            return false;
#endif
        }
    }

    public static bool HasConnectedController
    {
        get
        {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
            if (!Application.isPlaying)
                return false;

            var service = Ensure();
            if (!service || !service.steamInputInitialized)
                return false;

            service.UpdateFrameStateIfNeeded();
            return service.connectedControllerCount > 0;
#else
            return false;
#endif
        }
    }

    public static bool CanShowBindingPanel => IsAvailable && HasConnectedController;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Application.isPlaying)
            Ensure();
    }

    public static SteamInputService Ensure()
    {
        if (!Application.isPlaying)
            return _instance;

        if (_instance)
            return _instance;

        var existing = FindFirstObjectByType<SteamInputService>(FindObjectsInactive.Include);
        if (existing)
        {
            _instance = existing;
            if (!_instance.gameObject.activeSelf)
                _instance.gameObject.SetActive(true);

            return _instance;
        }

        var go = new GameObject("SteamInputService");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<SteamInputService>();
        return _instance;
    }

    public static bool TryGetDetectedControlProfile(out TetrabeastsControlProfile profile)
    {
        profile = TetrabeastsControlProfile.KeyboardMouse;

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (!Application.isPlaying)
            return false;

        var service = Ensure();
        if (!service || !service.steamInputInitialized)
            return false;

        service.UpdateFrameStateIfNeeded();
        if (service.connectedControllerCount <= 0)
            return false;

        profile = service.GetDetectedControlProfileInternal();
        return true;
#else
        return false;
#endif
    }

    public static bool WasPressed(TetrabeastsControlAction action)
    {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (!Application.isPlaying)
            return false;

        var service = Ensure();
        if (!service || !service.steamInputInitialized)
            return false;

        service.UpdateFrameStateIfNeeded();
        return service.GetWasPressed(action);
#else
        return false;
#endif
    }

    public static bool IsHeld(TetrabeastsControlAction action)
    {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (!Application.isPlaying)
            return false;

        var service = Ensure();
        if (!service || !service.steamInputInitialized)
            return false;

        service.UpdateFrameStateIfNeeded();
        return service.GetIsHeld(action);
#else
        return false;
#endif
    }

    public static string GetBindingLabel(TetrabeastsControlAction action)
    {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (!Application.isPlaying)
            return string.Empty;

        var service = Ensure();
        if (!service || !service.steamInputInitialized)
            return string.Empty;

        service.UpdateFrameStateIfNeeded();
        return service.GetBindingLabelInternal(action);
#else
        return string.Empty;
#endif
    }

    public static string[] GetBindingGlyphPaths(TetrabeastsControlAction action)
    {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (!Application.isPlaying)
            return Array.Empty<string>();

        var service = Ensure();
        if (!service || !service.steamInputInitialized)
            return Array.Empty<string>();

        service.UpdateFrameStateIfNeeded();
        return service.GetBindingGlyphPathsInternal(action);
#else
        return Array.Empty<string>();
#endif
    }

    public static bool ShowBindingPanel()
    {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (!Application.isPlaying)
            return false;

        var service = Ensure();
        if (!service || !service.steamInputInitialized)
            return false;

        service.UpdateFrameStateIfNeeded();
        if (service.connectedControllerCount <= 0 || IsZero(service.activeController))
            return false;

        try
        {
            return SteamInput.ShowBindingPanel(service.activeController);
        }
        catch (Exception ex)
        {
            service.lastStatus = $"Steam Input binding panel failed: {ex.Message}";
            if (service.logSteamInputStatus)
                Debug.LogWarning(service.lastStatus);

            return false;
        }
#else
        return false;
#endif
    }

    void Awake()
    {
        if (_instance && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeSteamInputIfAvailable();
    }

    void Update()
    {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (!steamInputInitialized)
        {
            TryInitializeAgain();
            return;
        }

        UpdateFrameStateIfNeeded();
#endif
    }

    void OnApplicationQuit()
    {
        ShutdownSteamInput();
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            ShutdownSteamInput();
            _instance = null;
        }
    }

    void ShutdownSteamInput()
    {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (!steamInputInitialized)
            return;

        try
        {
            SteamInput.Shutdown();
        }
        catch (Exception)
        {
            // Steam API shutdown order is not guaranteed during application quit.
        }

        steamInputInitialized = false;
#endif
    }

    void TryInitializeAgain()
    {
        if (!Application.isPlaying || Time.unscaledTime < nextInitializeAttemptTime)
            return;

        nextInitializeAttemptTime = Time.unscaledTime + 5f;
        InitializeSteamInputIfAvailable();
    }

    void InitializeSteamInputIfAvailable()
    {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (steamInputInitialized)
            return;

        try
        {
            var steam = SteamPlatformService.Ensure();
            if (!steam || !steam.IsAvailable)
            {
                lastStatus = steam ? steam.LastStatus : "Steam API did not initialize.";
                return;
            }

            string manifestPath = Path.Combine(Application.streamingAssetsPath, ManifestFileName);
            if (File.Exists(manifestPath))
            {
                bool manifestSet = SteamInput.SetInputActionManifestFilePath(manifestPath);
                if (!manifestSet)
                    Debug.LogWarning($"Steam Input action manifest was not accepted: {manifestPath}");
            }
            else if (logSteamInputStatus)
            {
                Debug.LogWarning($"Steam Input action manifest is missing: {manifestPath}");
            }

            steamInputInitialized = SteamInput.Init(true);
            if (!steamInputInitialized)
            {
                lastStatus = "Steam Input did not initialize.";
                if (logSteamInputStatus)
                    Debug.LogWarning(lastStatus);

                return;
            }

            ResolveActionHandles();
            deviceConnectedCallback = Callback<SteamInputDeviceConnected_t>.Create(OnSteamInputDeviceConnected);
            deviceDisconnectedCallback = Callback<SteamInputDeviceDisconnected_t>.Create(OnSteamInputDeviceDisconnected);
            SteamInput.EnableDeviceCallbacks();
            SteamInput.RunFrame();
            RefreshConnectedControllers(notifyDisconnect: false);

            lastStatus = connectedControllerCount > 0
                ? $"Steam Input initialized with {connectedControllerCount} controller(s)."
                : "Steam Input initialized; no Steam Input controller is connected yet.";

            if (logSteamInputStatus)
                Debug.Log(lastStatus);
        }
        catch (Exception ex)
        {
            steamInputInitialized = false;
            lastStatus = $"Steam Input unavailable: {ex.Message}";
            if (logSteamInputStatus)
                Debug.LogWarning(lastStatus);
        }
#else
        lastStatus = "Steamworks.NET is not enabled for this build.";
#endif
    }

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
    void ResolveActionHandles()
    {
        gameplayActionSet = SteamInput.GetActionSetHandle(GameplayActionSetName);
        moveAnalogAction = SteamInput.GetAnalogActionHandle(MoveAnalogActionName);
        menuNavigateAnalogAction = SteamInput.GetAnalogActionHandle(MenuNavigateAnalogActionName);

        digitalHandles.Clear();
        AddDigitalHandle(TetrabeastsControlAction.MoveLeft, "MoveLeft");
        AddDigitalHandle(TetrabeastsControlAction.MoveRight, "MoveRight");
        AddDigitalHandle(TetrabeastsControlAction.SoftDrop, "SoftDrop");
        AddDigitalHandle(TetrabeastsControlAction.RotateClockwise, "RotateClockwise");
        AddDigitalHandle(TetrabeastsControlAction.RotateCounterClockwise, "RotateCounterClockwise");
        AddDigitalHandle(TetrabeastsControlAction.HardDrop, "HardDrop");
        AddDigitalHandle(TetrabeastsControlAction.Special, "Special");
        AddDigitalHandle(TetrabeastsControlAction.Pause, "Pause");
        AddDigitalHandle(TetrabeastsControlAction.MenuSubmit, "MenuSubmit");
        AddDigitalHandle(TetrabeastsControlAction.MenuCancel, "MenuCancel");

        handlesResolved = !IsZero(gameplayActionSet);
    }

    void AddDigitalHandle(TetrabeastsControlAction action, string actionName)
    {
        var handle = SteamInput.GetDigitalActionHandle(actionName);
        if (!IsZero(handle))
            digitalHandles[action] = handle;
    }

    void UpdateFrameStateIfNeeded()
    {
        if (!steamInputInitialized || lastFrameUpdated == Time.frameCount)
            return;

        lastFrameUpdated = Time.frameCount;

        foreach (var action in GetAllActions())
            previousHeld[action] = currentHeld.TryGetValue(action, out bool held) && held;

        ClearCurrentHeld();

        try
        {
            SteamInput.RunFrame();
            RefreshConnectedControllers(notifyDisconnect: true);
            ActivateGameplayActionSet();
            UpdateHeldStates();
        }
        catch (Exception ex)
        {
            lastStatus = $"Steam Input update failed: {ex.Message}";
            if (logSteamInputStatus)
                Debug.LogWarning(lastStatus);
        }
    }

    void RefreshConnectedControllers(bool notifyDisconnect)
    {
        int previousCount = connectedControllerCount;
        InputHandle_t previousActive = activeController;

        Array.Clear(controllerHandles, 0, controllerHandles.Length);
        connectedControllerCount = Mathf.Clamp(SteamInput.GetConnectedControllers(controllerHandles), 0, controllerHandles.Length);
        activeController = GetFirstControllerHandle();

        if (!notifyDisconnect || previousCount <= 0)
            return;

        if (connectedControllerCount == 0 || (!IsZero(previousActive) && !ContainsController(previousActive)))
            NotifyControllerDisconnected();
    }

    InputHandle_t GetFirstControllerHandle()
    {
        for (int i = 0; i < connectedControllerCount; i++)
        {
            if (!IsZero(controllerHandles[i]))
                return controllerHandles[i];
        }

        return default;
    }

    bool ContainsController(InputHandle_t handle)
    {
        if (IsZero(handle))
            return false;

        for (int i = 0; i < connectedControllerCount; i++)
        {
            if (controllerHandles[i] == handle)
                return true;
        }

        return false;
    }

    void ActivateGameplayActionSet()
    {
        if (!handlesResolved || IsZero(gameplayActionSet))
            return;

        for (int i = 0; i < connectedControllerCount; i++)
        {
            if (!IsZero(controllerHandles[i]))
                SteamInput.ActivateActionSet(controllerHandles[i], gameplayActionSet);
        }
    }

    void UpdateHeldStates()
    {
        if (connectedControllerCount <= 0 || IsZero(activeController))
            return;

        foreach (var kvp in digitalHandles)
        {
            var data = SteamInput.GetDigitalActionData(activeController, kvp.Value);
            if (data.bActive != 0 && data.bState != 0)
                currentHeld[kvp.Key] = true;
        }

        if (!IsZero(moveAnalogAction))
        {
            var move = SteamInput.GetAnalogActionData(activeController, moveAnalogAction);
            if (move.bActive != 0)
            {
                if (move.x <= -AnalogPressedThreshold)
                    currentHeld[TetrabeastsControlAction.MoveLeft] = true;

                if (move.x >= AnalogPressedThreshold)
                    currentHeld[TetrabeastsControlAction.MoveRight] = true;

                if (move.y <= -AnalogPressedThreshold)
                    currentHeld[TetrabeastsControlAction.SoftDrop] = true;

                if (Mathf.Abs(move.x) >= AnalogPressedThreshold || Mathf.Abs(move.y) >= AnalogPressedThreshold)
                    currentHeld[TetrabeastsControlAction.MenuNavigate] = true;
            }
        }

        if (!IsZero(menuNavigateAnalogAction))
        {
            var navigate = SteamInput.GetAnalogActionData(activeController, menuNavigateAnalogAction);
            if (navigate.bActive != 0 &&
                (Mathf.Abs(navigate.x) >= AnalogPressedThreshold || Mathf.Abs(navigate.y) >= AnalogPressedThreshold))
            {
                currentHeld[TetrabeastsControlAction.MenuNavigate] = true;
            }
        }
    }

    bool GetWasPressed(TetrabeastsControlAction action)
    {
        bool held = currentHeld.TryGetValue(action, out bool isHeldNow) && isHeldNow;
        bool wasHeld = previousHeld.TryGetValue(action, out bool wasHeldBefore) && wasHeldBefore;
        return held && !wasHeld;
    }

    bool GetIsHeld(TetrabeastsControlAction action)
    {
        return currentHeld.TryGetValue(action, out bool held) && held;
    }

    string GetBindingLabelInternal(TetrabeastsControlAction action)
    {
        if (connectedControllerCount <= 0 || IsZero(activeController) || IsZero(gameplayActionSet))
            return string.Empty;

        labelParts.Clear();

        if (IsMovementAction(action))
            AppendAnalogOriginLabels(moveAnalogAction);

        if (action == TetrabeastsControlAction.MenuNavigate)
        {
            AppendAnalogOriginLabels(menuNavigateAnalogAction);
            if (labelParts.Count == 0)
                AppendAnalogOriginLabels(moveAnalogAction);
        }
        else if (digitalHandles.TryGetValue(action, out var digitalHandle))
        {
            AppendDigitalOriginLabels(digitalHandle);
        }

        return labelParts.Count > 0 ? string.Join(" / ", labelParts) : string.Empty;
    }

    string[] GetBindingGlyphPathsInternal(TetrabeastsControlAction action)
    {
        if (connectedControllerCount <= 0 || IsZero(activeController) || IsZero(gameplayActionSet))
            return Array.Empty<string>();

        glyphPaths.Clear();

        if (IsMovementAction(action))
            AppendAnalogOriginGlyphs(moveAnalogAction);

        if (action == TetrabeastsControlAction.MenuNavigate)
        {
            AppendAnalogOriginGlyphs(menuNavigateAnalogAction);
            if (glyphPaths.Count == 0)
                AppendAnalogOriginGlyphs(moveAnalogAction);
        }
        else if (digitalHandles.TryGetValue(action, out var digitalHandle))
        {
            AppendDigitalOriginGlyphs(digitalHandle);
        }

        return glyphPaths.Count > 0 ? glyphPaths.ToArray() : Array.Empty<string>();
    }

    void AppendDigitalOriginLabels(InputDigitalActionHandle_t actionHandle)
    {
        int count = SteamInput.GetDigitalActionOrigins(activeController, gameplayActionSet, actionHandle, originBuffer);
        AppendOriginLabels(count);
    }

    void AppendAnalogOriginLabels(InputAnalogActionHandle_t actionHandle)
    {
        if (IsZero(actionHandle))
            return;

        int count = SteamInput.GetAnalogActionOrigins(activeController, gameplayActionSet, actionHandle, originBuffer);
        AppendOriginLabels(count);
    }

    void AppendDigitalOriginGlyphs(InputDigitalActionHandle_t actionHandle)
    {
        int count = SteamInput.GetDigitalActionOrigins(activeController, gameplayActionSet, actionHandle, originBuffer);
        AppendOriginGlyphs(count);
    }

    void AppendAnalogOriginGlyphs(InputAnalogActionHandle_t actionHandle)
    {
        if (IsZero(actionHandle))
            return;

        int count = SteamInput.GetAnalogActionOrigins(activeController, gameplayActionSet, actionHandle, originBuffer);
        AppendOriginGlyphs(count);
    }

    void AppendOriginLabels(int count)
    {
        int safeCount = Mathf.Clamp(count, 0, originBuffer.Length);
        for (int i = 0; i < safeCount; i++)
        {
            var origin = originBuffer[i];
            if (origin == EInputActionOrigin.k_EInputActionOrigin_None)
                continue;

            string label = GetCompactOriginLabel(origin);
            if (string.IsNullOrWhiteSpace(label))
                label = SteamInput.GetStringForActionOrigin(origin);

            AddDistinct(labelParts, label);
        }
    }

    void AppendOriginGlyphs(int count)
    {
        int safeCount = Mathf.Clamp(count, 0, originBuffer.Length);
        for (int i = 0; i < safeCount; i++)
        {
            var origin = originBuffer[i];
            if (origin == EInputActionOrigin.k_EInputActionOrigin_None)
                continue;

            string path = SteamInput.GetGlyphPNGForActionOrigin(
                origin,
                ESteamInputGlyphSize.k_ESteamInputGlyphSize_Small,
                0);

            AddDistinct(glyphPaths, path);
        }
    }

    TetrabeastsControlProfile GetDetectedControlProfileInternal()
    {
        if (IsZero(activeController))
            return TetrabeastsControlProfile.KeyboardMouse;

        var inputType = SteamInput.GetInputTypeForHandle(activeController);
        return inputType switch
        {
            ESteamInputType.k_ESteamInputType_PS3Controller => TetrabeastsControlProfile.PlayStationController,
            ESteamInputType.k_ESteamInputType_PS4Controller => TetrabeastsControlProfile.PlayStationController,
            ESteamInputType.k_ESteamInputType_PS5Controller => TetrabeastsControlProfile.PlayStationController,
            ESteamInputType.k_ESteamInputType_SteamDeckController => TetrabeastsControlProfile.HandheldController,
            ESteamInputType.k_ESteamInputType_SwitchJoyConPair => TetrabeastsControlProfile.HandheldController,
            ESteamInputType.k_ESteamInputType_SwitchJoyConSingle => TetrabeastsControlProfile.HandheldController,
            ESteamInputType.k_ESteamInputType_SwitchProController => TetrabeastsControlProfile.HandheldController,
            ESteamInputType.k_ESteamInputType_XBox360Controller => TetrabeastsControlProfile.XboxController,
            ESteamInputType.k_ESteamInputType_XBoxOneController => TetrabeastsControlProfile.XboxController,
            ESteamInputType.k_ESteamInputType_GenericGamepad => TetrabeastsControlProfile.XboxController,
            ESteamInputType.k_ESteamInputType_SteamController => TetrabeastsControlProfile.HandheldController,
            _ => TetrabeastsControlProfile.XboxController
        };
    }

    void OnSteamInputDeviceConnected(SteamInputDeviceConnected_t callback)
    {
        RefreshConnectedControllers(notifyDisconnect: false);
    }

    void OnSteamInputDeviceDisconnected(SteamInputDeviceDisconnected_t callback)
    {
        if (activeController == callback.m_ulDisconnectedDeviceHandle || connectedControllerCount <= 1)
            NotifyControllerDisconnected();

        RefreshConnectedControllers(notifyDisconnect: false);
    }

    void NotifyControllerDisconnected()
    {
        if (lastDisconnectNotificationFrame == Time.frameCount)
            return;

        lastDisconnectNotificationFrame = Time.frameCount;
        ControllerDisconnected?.Invoke();
    }

    void ClearCurrentHeld()
    {
        foreach (var action in GetAllActions())
            currentHeld[action] = false;
    }

    static IEnumerable<TetrabeastsControlAction> GetAllActions()
    {
        yield return TetrabeastsControlAction.MoveLeft;
        yield return TetrabeastsControlAction.MoveRight;
        yield return TetrabeastsControlAction.SoftDrop;
        yield return TetrabeastsControlAction.RotateClockwise;
        yield return TetrabeastsControlAction.RotateCounterClockwise;
        yield return TetrabeastsControlAction.HardDrop;
        yield return TetrabeastsControlAction.Special;
        yield return TetrabeastsControlAction.Pause;
        yield return TetrabeastsControlAction.MenuNavigate;
        yield return TetrabeastsControlAction.MenuSubmit;
        yield return TetrabeastsControlAction.MenuCancel;
    }

    static bool IsMovementAction(TetrabeastsControlAction action)
    {
        return action == TetrabeastsControlAction.MoveLeft ||
               action == TetrabeastsControlAction.MoveRight ||
               action == TetrabeastsControlAction.SoftDrop;
    }

    static void AddDistinct(List<string> values, string value)
    {
        if (string.IsNullOrWhiteSpace(value) || values.Contains(value))
            return;

        values.Add(value);
    }

    static string GetCompactOriginLabel(EInputActionOrigin origin)
    {
        string name = origin.ToString();

        if (name.Contains("_PS4_X") || name.Contains("_PS5_X"))
            return "\u2715";

        if (name.Contains("_PS4_Circle") || name.Contains("_PS5_Circle"))
            return "\u25CB";

        if (name.Contains("_PS4_Square") || name.Contains("_PS5_Square"))
            return "\u25A1";

        if (name.Contains("_PS4_Triangle") || name.Contains("_PS5_Triangle"))
            return "\u25B3";

        return string.Empty;
    }

    static bool IsZero(InputHandle_t handle)
    {
        return handle.m_InputHandle == 0;
    }

    static bool IsZero(InputActionSetHandle_t handle)
    {
        return (ulong)handle == 0;
    }

    static bool IsZero(InputDigitalActionHandle_t handle)
    {
        return (ulong)handle == 0;
    }

    static bool IsZero(InputAnalogActionHandle_t handle)
    {
        return (ulong)handle == 0;
    }
#endif
}
