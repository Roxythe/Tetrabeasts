using System;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public enum TetrabeastsControlProfile
{
    PlatformDefault = 0,
    KeyboardMouse = 1,
    XboxController = 2,
    PlayStationController = 3,
    HandheldController = 4
}

public enum TetrabeastsControlAction
{
    MoveLeft,
    MoveRight,
    SoftDrop,
    RotateClockwise,
    RotateCounterClockwise,
    HardDrop,
    Special,
    Pause,
    MenuNavigate,
    MenuSubmit,
    MenuCancel,
    ToggleControls
}

public readonly struct TetrabeastsControlBindingRow
{
    public readonly TetrabeastsControlAction Action;
    public readonly string ActionLabel;
    public readonly string BindingLabel;

    public TetrabeastsControlBindingRow(TetrabeastsControlAction action, string actionLabel, string bindingLabel)
    {
        Action = action;
        ActionLabel = actionLabel;
        BindingLabel = bindingLabel;
    }
}

public readonly struct TetrabeastsControlBinding : IEquatable<TetrabeastsControlBinding>
{
    public readonly string Path;
    public readonly string Label;

    public TetrabeastsControlBinding(string path, string label)
    {
        Path = path ?? string.Empty;
        Label = label ?? string.Empty;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(Path);

    public bool Equals(TetrabeastsControlBinding other)
    {
        return string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object obj)
    {
        return obj is TetrabeastsControlBinding other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Path ?? string.Empty);
    }
}

public static class TetrabeastsControls
{
    const string KeyControlProfile = "Settings_ControlProfile";
    const string KeyControlBindingsPrefix = "Settings_ControlBindings_";
    const string ControllerGlyphSpriteAssetName = "ControllerGlyphs";
    const int ControllerGlyphSizePercent = 170;
    const int CompactControllerGlyphSizePercent = 125;
    const int TutorialContinueIndicatorGlyphSizePercent = 175;
    const float StickPressedThreshold = 0.5f;
    const float DefaultMenuSubmitSuppressSeconds = 0.22f;

    static readonly TetrabeastsControlAction[] DisplayActions =
    {
        TetrabeastsControlAction.MoveLeft,
        TetrabeastsControlAction.MoveRight,
        TetrabeastsControlAction.SoftDrop,
        TetrabeastsControlAction.RotateClockwise,
        TetrabeastsControlAction.RotateCounterClockwise,
        TetrabeastsControlAction.HardDrop,
        TetrabeastsControlAction.Special,
        TetrabeastsControlAction.Pause,
        TetrabeastsControlAction.ToggleControls,
        TetrabeastsControlAction.MenuSubmit,
        TetrabeastsControlAction.MenuCancel
    };

    static readonly TetrabeastsControlProfile[] ProfileOptions =
    {
        TetrabeastsControlProfile.PlatformDefault,
        TetrabeastsControlProfile.KeyboardMouse,
        TetrabeastsControlProfile.XboxController,
        TetrabeastsControlProfile.PlayStationController,
        TetrabeastsControlProfile.HandheldController
    };

    static readonly Dictionary<TetrabeastsControlAction, HoldRepeatState> RepeatStates = new();
    static readonly Dictionary<TetrabeastsControlAction, int> PressConsumedFrames = new();
    static readonly Dictionary<TetrabeastsControlProfile, BindingProfileState> BindingProfiles = new();
    static float suppressMenuSubmitUntilTime = -1f;
    static int suppressMenuSubmitUntilFrame = -1000;
#if ENABLE_LEGACY_INPUT_MANAGER
    static readonly HashSet<string> MissingLegacyAxes = new();
#endif
    static bool hasLastInputProfile;
    static TetrabeastsControlProfile lastInputProfile = TetrabeastsControlProfile.KeyboardMouse;
#if ENABLE_INPUT_SYSTEM
    static bool inputSystemDeviceHooked;
    static Gamepad lastInputGamepad;
    static int lastInputGamepadDeviceId = -1;
#endif

    public static event Action<TetrabeastsControlProfile> ProfileChanged;
    public static event Action<TetrabeastsControlProfile> BindingsChanged;
    public static event Action<TetrabeastsControlProfile> PlatformDefaultProfileChanged;

#if ENABLE_INPUT_SYSTEM
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void BootstrapInputSystemDeviceTracking()
    {
        if (inputSystemDeviceHooked)
            return;

        InputSystem.onDeviceChange -= HandleInputSystemDeviceChange;
        InputSystem.onDeviceChange += HandleInputSystemDeviceChange;
        inputSystemDeviceHooked = true;
    }
#endif

    public static IReadOnlyList<TetrabeastsControlProfile> SelectableProfiles => ProfileOptions;
    public static IReadOnlyList<TetrabeastsControlAction> RebindableActions => DisplayActions;

    public static TetrabeastsControlProfile SavedProfile
    {
        get
        {
            int saved = PlayerPrefs.GetInt(KeyControlProfile, (int)TetrabeastsControlProfile.PlatformDefault);
            return IsValidProfileValue(saved)
                ? (TetrabeastsControlProfile)saved
                : TetrabeastsControlProfile.PlatformDefault;
        }
    }

    public static TetrabeastsControlProfile EffectiveProfile => ResolveProfile(SavedProfile);

    public static TetrabeastsControlProfile ActiveInputProfile
    {
        get
        {
            RefreshActiveInputProfile();
            return hasLastInputProfile ? lastInputProfile : EffectiveProfile;
        }
    }

    public static TetrabeastsControlProfile ControlPromptProfile
    {
        get
        {
            return ActiveInputProfile;
        }
    }

    public static void SaveProfile(TetrabeastsControlProfile profile)
    {
        if (!IsValidProfileValue((int)profile))
            profile = TetrabeastsControlProfile.PlatformDefault;

        if (SavedProfile == profile)
            return;

        PlayerPrefs.SetInt(KeyControlProfile, (int)profile);
        PlayerPrefsSaveScheduler.QueueSave();
        ProfileChanged?.Invoke(profile);
    }

    public static void ResetToPlatformDefault()
    {
        SaveProfile(TetrabeastsControlProfile.PlatformDefault);
    }

    public static void RefreshActiveInputProfile()
    {
        if (TryDetectActiveInputProfile(out var profile))
            SetLastInputProfile(profile);
    }

#if ENABLE_INPUT_SYSTEM
    public static bool TrySetActiveInputProfileFromDevice(InputDevice device)
    {
        if (!(device is Gamepad gamepad) || !IsUsableGamepad(gamepad))
            return false;

        SetLastInputGamepadAndProfile(gamepad);
        return true;
    }

    public static bool TryGetActiveGamepad(out Gamepad gamepad)
    {
        if (TryGetUsedGamepadThisFrame(out var usedGamepad))
        {
            SetLastInputGamepadAndProfile(usedGamepad);
            gamepad = usedGamepad;
            return true;
        }

        if (IsUsableGamepad(lastInputGamepad))
        {
            gamepad = lastInputGamepad;
            return true;
        }

        if (lastInputGamepadDeviceId >= 0 &&
            TryGetGamepadByDeviceId(lastInputGamepadDeviceId, out var matchingGamepad))
        {
            SetLastInputGamepad(matchingGamepad);
            gamepad = matchingGamepad;
            return true;
        }

        if (TryGetFallbackGamepad(out var fallbackGamepad))
        {
            SetLastInputGamepad(fallbackGamepad);
            if (!hasLastInputProfile)
                SetLastInputProfile(GetBestProfileForGamepad(fallbackGamepad));

            gamepad = fallbackGamepad;
            return true;
        }

        gamepad = null;
        return false;
    }
#endif

    public static bool WasMenuNavigationPressedThisFrame()
    {
        return WasPressed(TetrabeastsControlAction.MenuNavigate);
    }

    public static void SuppressMenuSubmit(float seconds = DefaultMenuSubmitSuppressSeconds)
    {
        suppressMenuSubmitUntilTime = Mathf.Max(
            suppressMenuSubmitUntilTime,
            Time.unscaledTime + Mathf.Max(0f, seconds));
        suppressMenuSubmitUntilFrame = Mathf.Max(suppressMenuSubmitUntilFrame, Time.frameCount + 1);
    }

    static bool IsMenuSubmitSuppressed()
    {
        return Time.frameCount <= suppressMenuSubmitUntilFrame ||
               Time.unscaledTime < suppressMenuSubmitUntilTime;
    }

    public static bool IsMenuNavigationHeld()
    {
        return IsHeld(TetrabeastsControlAction.MenuNavigate);
    }

    public static Vector2 GetMenuNavigationDirection()
    {
        Vector2 direction = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        direction += GetMenuNavigationDirectionInputSystem();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        direction += GetMenuNavigationDirectionLegacy();
#endif

        return ClampMenuNavigationDirection(direction);
    }

    public static Vector2 GetMenuScrollDirection()
    {
        Vector2 direction = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        direction += GetMenuScrollDirectionInputSystem();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        direction += GetMenuScrollDirectionLegacy();
#endif

        return ClampMenuNavigationDirection(direction);
    }

    public static bool WasButtonNavigationPressedThisFrame()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        pressed |= WasButtonNavigationPressedInputSystem();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= WasButtonNavigationPressedLegacy();
#endif

        return pressed;
    }

    public static bool IsButtonNavigationHeld()
    {
        bool held = false;

#if ENABLE_INPUT_SYSTEM
        held |= IsButtonNavigationHeldInputSystem();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        held |= IsButtonNavigationHeldLegacy();
#endif

        return held;
    }

    public static Vector2 GetButtonNavigationDirection()
    {
        Vector2 direction = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        direction += GetButtonNavigationDirectionInputSystem();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        direction += GetButtonNavigationDirectionLegacy();
#endif

        return ClampMenuNavigationDirection(direction);
    }

    public static bool WasAnyMenuInputPressedThisFrame()
    {
        return WasPressed(TetrabeastsControlAction.MenuNavigate) ||
               WasPressed(TetrabeastsControlAction.MenuSubmit) ||
               WasPressed(TetrabeastsControlAction.MenuCancel);
    }

    public static TetrabeastsControlProfile GetEditableProfile(TetrabeastsControlProfile profile)
    {
        return ResolveProfile(profile);
    }

    public static bool HasCustomBindings(TetrabeastsControlProfile profile)
    {
        var state = GetBindingProfileState(GetEditableProfile(profile));
        return state.HasCustom;
    }

    public static bool HasUnsavedBindingChanges(TetrabeastsControlProfile profile)
    {
        var state = GetBindingProfileState(GetEditableProfile(profile));
        return state.Dirty;
    }

    public static void SaveBindings(TetrabeastsControlProfile profile)
    {
        TetrabeastsControlProfile editableProfile = GetEditableProfile(profile);
        var state = GetBindingProfileState(editableProfile);

        if (!state.HasCustom)
        {
            PlayerPrefs.DeleteKey(GetBindingsPrefsKey(editableProfile));
            state.Dirty = false;
            PlayerPrefsSaveScheduler.QueueSave();
            return;
        }

        SaveBindingProfileState(editableProfile, state);
    }

    static void SaveBindingProfileState(TetrabeastsControlProfile profile, BindingProfileState state)
    {
        var data = new BindingProfileSaveData();
        foreach (var action in DisplayActions)
        {
            var actionData = new BindingActionSaveData { action = action.ToString() };
            if (state.Bindings.TryGetValue(action, out var bindings))
            {
                for (int i = 0; i < bindings.Count; i++)
                {
                    var binding = bindings[i];
                    actionData.bindings.Add(new BindingSaveData { path = binding.Path, label = binding.Label });
                }
            }

            data.actions.Add(actionData);
        }

        PlayerPrefs.SetString(GetBindingsPrefsKey(profile), JsonUtility.ToJson(data));
        PlayerPrefsSaveScheduler.QueueSave();
        state.Dirty = false;
    }

    public static void ResetBindingsToDefault(TetrabeastsControlProfile profile)
    {
        TetrabeastsControlProfile editableProfile = GetEditableProfile(profile);
        PlayerPrefs.DeleteKey(GetBindingsPrefsKey(editableProfile));
        BindingProfiles[editableProfile] = CreateDefaultBindingState(editableProfile, hasCustom: false);
        PlayerPrefsSaveScheduler.QueueSave();
        BindingsChanged?.Invoke(editableProfile);
    }

    public static void SetActionBinding(TetrabeastsControlProfile profile, TetrabeastsControlAction action, TetrabeastsControlBinding binding)
    {
        TetrabeastsControlProfile editableProfile = GetEditableProfile(profile);
        var state = GetBindingProfileState(editableProfile);
        EnsureCustomBindingState(editableProfile, state);

        if (!binding.IsValid)
            state.Bindings[action] = new List<TetrabeastsControlBinding>();
        else
            state.Bindings[action] = new List<TetrabeastsControlBinding> { binding };

        state.Dirty = true;
        BindingsChanged?.Invoke(editableProfile);
    }

    public static void ClearActionBinding(TetrabeastsControlProfile profile, TetrabeastsControlAction action)
    {
        TetrabeastsControlProfile editableProfile = GetEditableProfile(profile);
        var state = GetBindingProfileState(editableProfile);
        EnsureCustomBindingState(editableProfile, state);
        state.Bindings[action] = new List<TetrabeastsControlBinding>();
        state.Dirty = true;
        BindingsChanged?.Invoke(editableProfile);
    }

    public static bool TryFindActionUsingBinding(
        TetrabeastsControlProfile profile,
        TetrabeastsControlBinding binding,
        TetrabeastsControlAction excludingAction,
        out TetrabeastsControlAction conflictAction)
    {
        conflictAction = default;
        if (!binding.IsValid)
            return false;

        var state = GetBindingProfileState(GetEditableProfile(profile));
        bool conflictIsMenuAction = IsMenuBindingAction(excludingAction);
        foreach (var action in DisplayActions)
        {
            if (action == excludingAction || IsMenuBindingAction(action) != conflictIsMenuAction)
                continue;

            if (!state.Bindings.TryGetValue(action, out var bindings))
                continue;

            for (int i = 0; i < bindings.Count; i++)
            {
                if (!bindings[i].Equals(binding))
                    continue;

                conflictAction = action;
                return true;
            }
        }

        return false;
    }

    public static bool HasUnboundActions(TetrabeastsControlProfile profile)
    {
        return GetUnboundActions(profile).Count > 0;
    }

    public static List<TetrabeastsControlAction> GetUnboundActions(TetrabeastsControlProfile profile)
    {
        var unbound = new List<TetrabeastsControlAction>();
        var state = GetBindingProfileState(GetEditableProfile(profile));
        foreach (var action in DisplayActions)
        {
            if (!state.Bindings.TryGetValue(action, out var bindings) || bindings.Count == 0)
                unbound.Add(action);
        }

        return unbound;
    }

    public static bool TryReadRebindInput(
        TetrabeastsControlProfile profile,
        TetrabeastsControlAction action,
        out TetrabeastsControlBinding binding,
        out bool cancelled)
    {
        binding = default;
        cancelled = false;

        TetrabeastsControlProfile editableProfile = GetEditableProfile(profile);

#if ENABLE_INPUT_SYSTEM
        if (WasRebindCancelPressed(editableProfile, action))
        {
            cancelled = true;
            return true;
        }

        if (editableProfile == TetrabeastsControlProfile.KeyboardMouse)
            return TryReadKeyboardMouseRebind(out binding);

        return TryReadGamepadRebind(editableProfile, out binding);
#else
        return false;
#endif
    }

    public static bool AnyRebindInputHeld(TetrabeastsControlProfile profile)
    {
#if ENABLE_INPUT_SYSTEM
        TetrabeastsControlProfile editableProfile = GetEditableProfile(profile);
        if (editableProfile == TetrabeastsControlProfile.KeyboardMouse)
            return AnyKeyboardMouseRebindInputHeld();

        return AnyGamepadRebindInputHeld();
#else
        return false;
#endif
    }

    public static TetrabeastsControlProfile ResolveProfile(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.PlatformDefault
            ? GetPlatformDefaultProfile()
            : profile;
    }

    public static TetrabeastsControlProfile GetPlatformDefaultProfile()
    {
        RefreshActiveInputProfile();

        if (SteamInputService.TryGetDetectedControlProfile(out var steamProfile) &&
            IsControllerProfile(steamProfile) &&
            (!hasLastInputProfile || ShouldConnectedGamepadOverrideLastProfile(steamProfile)))
        {
            SetLastInputProfile(steamProfile);
        }

        if (hasLastInputProfile)
            return lastInputProfile;

        if (SteamInputService.TryGetDetectedControlProfile(out steamProfile))
            return steamProfile;

        return GetHardwarePlatformDefaultProfile();
    }

    static TetrabeastsControlProfile GetHardwarePlatformDefaultProfile()
    {
        string platform = Application.platform.ToString();

        if (platform.IndexOf("PlayStation", StringComparison.OrdinalIgnoreCase) >= 0 ||
            platform.IndexOf("PS4", StringComparison.OrdinalIgnoreCase) >= 0 ||
            platform.IndexOf("PS5", StringComparison.OrdinalIgnoreCase) >= 0)
            return TetrabeastsControlProfile.PlayStationController;

        if (platform.IndexOf("Xbox", StringComparison.OrdinalIgnoreCase) >= 0 ||
            platform.IndexOf("GameCore", StringComparison.OrdinalIgnoreCase) >= 0)
            return TetrabeastsControlProfile.XboxController;

        if (platform.IndexOf("Switch", StringComparison.OrdinalIgnoreCase) >= 0 ||
            LooksLikeHandheldPc())
            return TetrabeastsControlProfile.HandheldController;

        return TetrabeastsControlProfile.KeyboardMouse;
    }

    static bool TryDetectActiveInputProfile(out TetrabeastsControlProfile profile)
    {
        profile = default;

#if ENABLE_INPUT_SYSTEM
        if (WasKeyboardMouseUsedThisFrame())
        {
            profile = TetrabeastsControlProfile.KeyboardMouse;
            return true;
        }

        if (TryGetUsedGamepadThisFrame(out var gamepad))
        {
            SetLastInputGamepad(gamepad);
            profile = GetBestProfileForGamepad(gamepad);
            return true;
        }
#endif

        if (!hasLastInputProfile && SteamInputService.TryGetDetectedControlProfile(out var detectedSteamProfile))
        {
            profile = detectedSteamProfile;
            return true;
        }

        return false;
    }

    static void SetLastInputProfile(TetrabeastsControlProfile profile)
    {
        if (profile == TetrabeastsControlProfile.PlatformDefault)
            profile = GetHardwarePlatformDefaultProfile();

        if (hasLastInputProfile && lastInputProfile == profile)
            return;

        hasLastInputProfile = true;
        lastInputProfile = profile;
        PlatformDefaultProfileChanged?.Invoke(profile);

        if (SavedProfile == TetrabeastsControlProfile.PlatformDefault)
            ProfileChanged?.Invoke(TetrabeastsControlProfile.PlatformDefault);
    }

#if ENABLE_INPUT_SYSTEM
    static void SetLastInputGamepad(Gamepad gamepad)
    {
        lastInputGamepad = gamepad;
        lastInputGamepadDeviceId = gamepad != null ? gamepad.deviceId : -1;
    }

    static void SetLastInputGamepadAndProfile(Gamepad gamepad)
    {
        if (gamepad == null)
            return;

        SetLastInputGamepad(gamepad);
        SetLastInputProfile(GetBestProfileForGamepad(gamepad));
    }

    static void HandleInputSystemDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (!(device is Gamepad gamepad))
            return;

        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Reconnected:
            case InputDeviceChange.Enabled:
            case InputDeviceChange.ConfigurationChanged:
            case InputDeviceChange.UsageChanged:
                SetLastInputGamepadAndProfile(gamepad);
                break;

            case InputDeviceChange.Disconnected:
            case InputDeviceChange.Removed:
                if (lastInputGamepad == gamepad || lastInputGamepadDeviceId == gamepad.deviceId)
                    SetLastInputGamepad(null);

                if (TryGetFallbackGamepad(out var fallbackGamepad))
                    SetLastInputGamepadAndProfile(fallbackGamepad);
                break;
        }
    }

    static bool TryGetUsedGamepadThisFrame(out Gamepad usedGamepad)
    {
        usedGamepad = null;
        Gamepad fallback = null;

        foreach (var gamepad in Gamepad.all)
        {
            if (!IsUsableGamepad(gamepad) || !WasGamepadUsedThisFrame(gamepad))
                continue;

            var profile = GetBestProfileForGamepad(gamepad);
            if (profile == TetrabeastsControlProfile.PlayStationController ||
                profile == TetrabeastsControlProfile.HandheldController)
            {
                usedGamepad = gamepad;
                return true;
            }

            if (fallback == null)
                fallback = gamepad;
        }

        if (fallback != null)
        {
            usedGamepad = fallback;
            return true;
        }

        return false;
    }

    static bool TryGetGamepadByDeviceId(int deviceId, out Gamepad gamepad)
    {
        foreach (var connected in Gamepad.all)
        {
            if (IsUsableGamepad(connected) && connected.deviceId == deviceId)
            {
                gamepad = connected;
                return true;
            }
        }

        gamepad = null;
        return false;
    }

    static bool TryGetFallbackGamepad(out Gamepad gamepad)
    {
        if (IsUsableGamepad(Gamepad.current))
        {
            gamepad = Gamepad.current;
            return true;
        }

        foreach (var connected in Gamepad.all)
        {
            if (IsUsableGamepad(connected))
            {
                gamepad = connected;
                return true;
            }
        }

        gamepad = null;
        return false;
    }

    static bool IsUsableGamepad(Gamepad gamepad)
    {
        return gamepad != null && gamepad.added && gamepad.enabled;
    }

    static bool ShouldConnectedGamepadOverrideLastProfile(TetrabeastsControlProfile connectedProfile)
    {
        if (!hasLastInputProfile || connectedProfile == lastInputProfile)
            return false;

        if (lastInputProfile == TetrabeastsControlProfile.KeyboardMouse)
            return false;

        return connectedProfile == TetrabeastsControlProfile.PlayStationController ||
            connectedProfile == TetrabeastsControlProfile.HandheldController;
    }

    static bool ShouldPreferDetectedControllerProfile(
        TetrabeastsControlProfile inputSystemProfile,
        TetrabeastsControlProfile detectedProfile)
    {
        if (!IsControllerProfile(detectedProfile) || detectedProfile == inputSystemProfile)
            return false;

        return inputSystemProfile == TetrabeastsControlProfile.XboxController ||
            detectedProfile == TetrabeastsControlProfile.PlayStationController ||
            detectedProfile == TetrabeastsControlProfile.HandheldController;
    }

    static bool WasKeyboardMouseUsedThisFrame()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            return true;

        var mouse = Mouse.current;
        if (mouse == null)
            return false;

        return mouse.leftButton.wasPressedThisFrame ||
               mouse.rightButton.wasPressedThisFrame ||
               mouse.middleButton.wasPressedThisFrame ||
               mouse.forwardButton.wasPressedThisFrame ||
               mouse.backButton.wasPressedThisFrame ||
               mouse.delta.ReadValue().sqrMagnitude > 0.01f ||
               mouse.scroll.ReadValue().sqrMagnitude > 0.01f;
    }

    static bool WasGamepadUsedThisFrame(Gamepad gamepad)
    {
        if (!IsUsableGamepad(gamepad))
            return false;

        try
        {
            return gamepad.buttonSouth.wasPressedThisFrame ||
                   gamepad.buttonEast.wasPressedThisFrame ||
                   gamepad.buttonWest.wasPressedThisFrame ||
                   gamepad.buttonNorth.wasPressedThisFrame ||
                   gamepad.leftShoulder.wasPressedThisFrame ||
                   gamepad.rightShoulder.wasPressedThisFrame ||
                   gamepad.leftTrigger.wasPressedThisFrame ||
                   gamepad.rightTrigger.wasPressedThisFrame ||
                   gamepad.startButton.wasPressedThisFrame ||
                   gamepad.selectButton.wasPressedThisFrame ||
                   gamepad.leftStickButton.wasPressedThisFrame ||
                   gamepad.rightStickButton.wasPressedThisFrame ||
                   gamepad.dpad.left.wasPressedThisFrame ||
                   gamepad.dpad.right.wasPressedThisFrame ||
                   gamepad.dpad.down.wasPressedThisFrame ||
                   gamepad.dpad.up.wasPressedThisFrame ||
                   gamepad.leftStick.ReadValue().magnitude >= StickPressedThreshold ||
                   gamepad.rightStick.ReadValue().magnitude >= StickPressedThreshold;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    static TetrabeastsControlProfile GetProfileForGamepad(Gamepad gamepad)
    {
        if (gamepad == null)
            return TetrabeastsControlProfile.XboxController;

        string description = GetGamepadDescription(gamepad);

        if (LooksLikePlayStationGamepad(description))
            return TetrabeastsControlProfile.PlayStationController;

        if (ContainsIgnoreCase(description, "steam deck") ||
            ContainsIgnoreCase(description, "steamcontroller") ||
            ContainsIgnoreCase(description, "steam controller") ||
            ContainsIgnoreCase(description, "switch") ||
            ContainsIgnoreCase(description, "joy-con") ||
            ContainsIgnoreCase(description, "joycon") ||
            LooksLikeHandheldPc())
            return TetrabeastsControlProfile.HandheldController;

        return TetrabeastsControlProfile.XboxController;
    }

    static TetrabeastsControlProfile GetBestProfileForGamepad(Gamepad gamepad)
    {
        var profile = GetProfileForGamepad(gamepad);
        string description = GetGamepadDescription(gamepad);
        if (SteamInputService.TryGetDetectedControlProfile(out var steamProfile) &&
            IsControllerProfile(steamProfile) &&
            !ShouldTrustInputSystemProfile(description, profile) &&
            ShouldPreferDetectedControllerProfile(profile, steamProfile))
            profile = steamProfile;

        return profile;
    }

    static string GetGamepadDescription(Gamepad gamepad)
    {
        if (gamepad == null)
            return string.Empty;

        return $"{gamepad.GetType().FullName} {gamepad.name} {gamepad.displayName} {gamepad.layout} {gamepad.variants} " +
            $"{gamepad.description.product} {gamepad.description.manufacturer} " +
            $"{gamepad.description.interfaceName} {gamepad.description.capabilities} {gamepad.description}";
    }

    static bool ShouldTrustInputSystemProfile(string description, TetrabeastsControlProfile profile)
    {
        return profile switch
        {
            TetrabeastsControlProfile.PlayStationController => LooksLikePlayStationGamepad(description),
            TetrabeastsControlProfile.XboxController => LooksLikeXboxGamepad(description),
            TetrabeastsControlProfile.HandheldController => LooksLikeHandheldGamepad(description),
            _ => false
        };
    }

    static bool LooksLikePlayStationGamepad(string description)
    {
        if (ContainsIgnoreCase(description, "dualshock") ||
            ContainsIgnoreCase(description, "dualshock4") ||
            ContainsIgnoreCase(description, "dual shock") ||
            ContainsIgnoreCase(description, "dualsense") ||
            ContainsIgnoreCase(description, "dualsensegamepad") ||
            ContainsIgnoreCase(description, "dual sense") ||
            ContainsIgnoreCase(description, "playstation") ||
            ContainsIgnoreCase(description, "playstation controller") ||
            ContainsIgnoreCase(description, "sony") ||
            ContainsIgnoreCase(description, "sony interactive") ||
            ContainsIgnoreCase(description, "scei") ||
            ContainsIgnoreCase(description, "ps4") ||
            ContainsIgnoreCase(description, "ps5") ||
            ContainsIgnoreCase(description, "vid_054c") ||
            ContainsIgnoreCase(description, "wireless controller"))
            return true;

        if (ContainsIgnoreCase(description, "054c") ||
            ContainsIgnoreCase(description, "0x054c"))
            return true;

        return (ContainsIgnoreCase(description, "vendorid") ||
                ContainsIgnoreCase(description, "vendor id") ||
                ContainsIgnoreCase(description, "vendor")) &&
            ContainsIgnoreCase(description, "1356");
    }

    static bool LooksLikeXboxGamepad(string description)
    {
        if (ContainsIgnoreCase(description, "xbox") ||
            ContainsIgnoreCase(description, "xinput") ||
            ContainsIgnoreCase(description, "x-input") ||
            ContainsIgnoreCase(description, "microsoft") ||
            ContainsIgnoreCase(description, "vid_045e"))
            return true;

        return (ContainsIgnoreCase(description, "vendorid") ||
                ContainsIgnoreCase(description, "vendor id") ||
                ContainsIgnoreCase(description, "vendor")) &&
            ContainsIgnoreCase(description, "1118");
    }

    static bool LooksLikeHandheldGamepad(string description)
    {
        return ContainsIgnoreCase(description, "steam deck") ||
               ContainsIgnoreCase(description, "steamcontroller") ||
               ContainsIgnoreCase(description, "steam controller") ||
               ContainsIgnoreCase(description, "switch") ||
               ContainsIgnoreCase(description, "joy-con") ||
               ContainsIgnoreCase(description, "joycon") ||
               LooksLikeHandheldPc();
    }
#endif

    static bool IsControllerProfile(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.XboxController ||
               profile == TetrabeastsControlProfile.PlayStationController ||
               profile == TetrabeastsControlProfile.HandheldController;
    }

    public static string GetProfileLabel(TetrabeastsControlProfile profile)
    {
        return profile switch
        {
            TetrabeastsControlProfile.PlatformDefault => $"Platform Default ({GetProfileLabel(GetPlatformDefaultProfile())})",
            TetrabeastsControlProfile.KeyboardMouse => "Keyboard / Mouse",
            TetrabeastsControlProfile.XboxController => "Xbox Controller",
            TetrabeastsControlProfile.PlayStationController => "PlayStation Controller",
            TetrabeastsControlProfile.HandheldController => "Handheld Controller",
            _ => "Unknown"
        };
    }

    public static string GetActionLabel(TetrabeastsControlAction action)
    {
        return action switch
        {
            TetrabeastsControlAction.MoveLeft => "Move Left",
            TetrabeastsControlAction.MoveRight => "Move Right",
            TetrabeastsControlAction.SoftDrop => "Soft Drop",
            TetrabeastsControlAction.RotateClockwise => "Rotate Clockwise",
            TetrabeastsControlAction.RotateCounterClockwise => "Rotate Counterclockwise",
            TetrabeastsControlAction.HardDrop => "Hard Drop",
            TetrabeastsControlAction.Special => "Special",
            TetrabeastsControlAction.Pause => "Pause",
            TetrabeastsControlAction.ToggleControls => "Toggle Controls",
            TetrabeastsControlAction.MenuNavigate => "Menu Navigate",
            TetrabeastsControlAction.MenuSubmit => "Menu Confirm",
            TetrabeastsControlAction.MenuCancel => "Menu Back",
            _ => action.ToString()
        };
    }

    public static TetrabeastsControlBindingRow[] GetBindingRows(TetrabeastsControlProfile profile)
    {
        TetrabeastsControlProfile effectiveProfile = ResolveProfile(profile);

        var rows = new TetrabeastsControlBindingRow[DisplayActions.Length];
        for (int i = 0; i < DisplayActions.Length; i++)
            rows[i] = new TetrabeastsControlBindingRow(
                DisplayActions[i],
                $" {GetActionLabel(DisplayActions[i])}",
                GetPresetBindingLabel(DisplayActions[i], effectiveProfile));

        return rows;
    }

    public static string GetBindingLabel(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        return GetBindingLabel(action, profile, ControllerGlyphSizePercent);
    }

    public static string GetCompactBindingLabel(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        return GetBindingLabel(action, profile, CompactControllerGlyphSizePercent);
    }

    static string GetBindingLabel(TetrabeastsControlAction action, TetrabeastsControlProfile profile, int controllerGlyphSizePercent)
    {
        TetrabeastsControlProfile effectiveProfile = ResolveProfile(profile);

        if (TryGetCustomBindingLabel(action, effectiveProfile, controllerGlyphSizePercent, out string customLabel))
            return customLabel;

        if (effectiveProfile == TetrabeastsControlProfile.KeyboardMouse)
            return GetKeyboardMouseBindingLabel(action);

        string steamBindingLabel = SteamInputService.GetBindingLabel(action);
        if (!string.IsNullOrWhiteSpace(steamBindingLabel))
        {
            string firstLabel = FirstBindingLabel(steamBindingLabel);
            return TryGetControllerGlyphFromLabel(firstLabel, effectiveProfile, controllerGlyphSizePercent, out string glyphLabel)
                ? glyphLabel
                : firstLabel;
        }

        return GetControllerBindingLabel(action, effectiveProfile, controllerGlyphSizePercent);
    }

    public static string GetPresetBindingLabel(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        TetrabeastsControlProfile effectiveProfile = ResolveProfile(profile);

        if (TryGetCustomBindingLabel(action, effectiveProfile, ControllerGlyphSizePercent, out string customLabel))
            return customLabel;

        return effectiveProfile == TetrabeastsControlProfile.KeyboardMouse
            ? GetKeyboardMouseBindingLabel(action)
            : GetControllerBindingLabel(action, effectiveProfile);
    }

    public static string GetTutorialContinueBindingLabel()
    {
        return GetTutorialContinueBindingLabel(ControlPromptProfile);
    }

    public static string GetTutorialContinueIndicatorLabel()
    {
        TetrabeastsControlProfile profile = ControlPromptProfile;
        TetrabeastsControlProfile effectiveProfile = ResolveProfile(profile);
        if (effectiveProfile == TetrabeastsControlProfile.KeyboardMouse)
            return "[F]";

        string label = GetBindingLabel(TetrabeastsControlAction.MenuSubmit, effectiveProfile, TutorialContinueIndicatorGlyphSizePercent);
        return string.IsNullOrWhiteSpace(label) ? FormatBracketedTutorialLabel(ControllerSouthLabel(effectiveProfile)) : label;
    }

    static string GetTutorialContinueBindingLabel(TetrabeastsControlProfile profile)
    {
        if (profile == TetrabeastsControlProfile.KeyboardMouse)
            return "F";

        string label = GetTutorialActionBindingLabel(TetrabeastsControlAction.MenuSubmit, profile);
        return string.IsNullOrWhiteSpace(label) ? ControllerSouthLabel(profile) : label;
    }

    public static string ResolveControlPromptTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        string resolved = text;

        TetrabeastsControlProfile profile = ControlPromptProfile;
        if (profile != TetrabeastsControlProfile.KeyboardMouse)
        {
            resolved = ProtectControllerTutorialPhrases(resolved);
            resolved = ReplaceGameplayControlBracketTokens(resolved, profile);
            resolved = RestoreControllerTutorialPhrases(resolved, profile);
            resolved = ReplaceControllerHelpControlTextTokens(resolved, profile);
        }

        resolved = ReplaceTutorialContinuePromptSuffix(resolved, GetTutorialContinueBindingLabel(profile));

        return resolved;
    }

    static string ProtectControllerTutorialPhrases(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        text = text.Replace("by pressing [A]", "__TB_TUTORIAL_MOVE_LEFT__");
        text = text.Replace("by pressing [D]", "__TB_TUTORIAL_MOVE_RIGHT__");
        text = text.Replace("by pressing [S]", "__TB_TUTORIAL_SOFT_DROP__");
        text = text.Replace("by pressing [Q]", "__TB_TUTORIAL_ROTATE_CCW__");
        text = text.Replace("by pressing [Z]", "__TB_TUTORIAL_ROTATE_CCW__");
        text = text.Replace("by pressing [E]", "__TB_TUTORIAL_ROTATE_CW__");
        text = text.Replace("by pressing [R]", "__TB_TUTORIAL_SPECIAL__");
        text = text.Replace("by pressing [Space]", "__TB_TUTORIAL_HARD_DROP__");

        return text;
    }

    static string RestoreControllerTutorialPhrases(string text, TetrabeastsControlProfile profile)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        text = text.Replace("__TB_TUTORIAL_MOVE_LEFT__", GetTutorialPressingPhrase(TetrabeastsControlAction.MoveLeft, profile));
        text = text.Replace("__TB_TUTORIAL_MOVE_RIGHT__", GetTutorialPressingPhrase(TetrabeastsControlAction.MoveRight, profile));
        text = text.Replace("__TB_TUTORIAL_SOFT_DROP__", GetTutorialPressingPhrase(TetrabeastsControlAction.SoftDrop, profile));
        text = text.Replace("__TB_TUTORIAL_ROTATE_CCW__", GetTutorialPressingPhrase(TetrabeastsControlAction.RotateCounterClockwise, profile));
        text = text.Replace("__TB_TUTORIAL_ROTATE_CW__", GetTutorialPressingPhrase(TetrabeastsControlAction.RotateClockwise, profile));
        text = text.Replace("__TB_TUTORIAL_SPECIAL__", GetTutorialPressingPhrase(TetrabeastsControlAction.Special, profile));
        text = text.Replace("__TB_TUTORIAL_HARD_DROP__", GetTutorialPressingPhrase(TetrabeastsControlAction.HardDrop, profile));

        return text;
    }

    static string ReplaceGameplayControlBracketTokens(string text, TetrabeastsControlProfile profile)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        text = ReplaceBracketedControlToken(text, "A", GetTutorialTokenReplacement(TetrabeastsControlAction.MoveLeft, profile));
        text = ReplaceBracketedControlToken(text, "D", GetTutorialTokenReplacement(TetrabeastsControlAction.MoveRight, profile));
        text = ReplaceBracketedControlToken(text, "S", GetTutorialTokenReplacement(TetrabeastsControlAction.SoftDrop, profile));
        text = ReplaceBracketedControlToken(text, "Q", GetTutorialTokenReplacement(TetrabeastsControlAction.RotateCounterClockwise, profile));
        text = ReplaceBracketedControlToken(text, "Z", GetTutorialTokenReplacement(TetrabeastsControlAction.RotateCounterClockwise, profile));
        text = ReplaceBracketedControlToken(text, "E", GetTutorialTokenReplacement(TetrabeastsControlAction.RotateClockwise, profile));
        text = ReplaceBracketedControlToken(text, "R", GetTutorialTokenReplacement(TetrabeastsControlAction.Special, profile));
        text = ReplaceBracketedControlToken(text, "Space", GetTutorialTokenReplacement(TetrabeastsControlAction.HardDrop, profile));
        text = ReplaceBracketedControlToken(text, "Space Bar", GetTutorialTokenReplacement(TetrabeastsControlAction.HardDrop, profile));
        text = ReplaceBracketedControlToken(text, "F", GetTutorialTokenReplacement(TetrabeastsControlAction.MenuSubmit, profile));
        text = ReplaceBracketedControlToken(text, "Barra de Espa\u00e7o", GetTutorialTokenReplacement(TetrabeastsControlAction.HardDrop, profile));
        text = ReplaceBracketedControlToken(text, "barra espaciadora", GetTutorialTokenReplacement(TetrabeastsControlAction.HardDrop, profile));
        text = ReplaceBracketedControlToken(text, "Barre d'espace", GetTutorialTokenReplacement(TetrabeastsControlAction.HardDrop, profile));
        text = ReplaceBracketedControlToken(text, "Leertaste", GetTutorialTokenReplacement(TetrabeastsControlAction.HardDrop, profile));
        text = ReplaceBracketedControlToken(text, "\u041f\u0440\u043e\u0431\u0435\u043b", GetTutorialTokenReplacement(TetrabeastsControlAction.HardDrop, profile));
        text = ReplaceBracketedControlToken(text, "\u7a7a\u683c\u952e", GetTutorialTokenReplacement(TetrabeastsControlAction.HardDrop, profile));
        text = ReplaceBracketedControlToken(text, "Esc", GetTutorialTokenReplacement(TetrabeastsControlAction.Pause, profile));
        text = ReplaceBracketedControlToken(text, "Escape", GetTutorialTokenReplacement(TetrabeastsControlAction.Pause, profile));
        text = ReplaceBracketedControlToken(text, "\u00c9chap", GetTutorialTokenReplacement(TetrabeastsControlAction.Pause, profile));

        return text;
    }

    static string ReplaceBracketedControlToken(string text, string token, string replacement)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(replacement))
            return text;

        return text.Replace($"[{token}]", replacement);
    }

    static string ReplaceControllerHelpControlTextTokens(string text, TetrabeastsControlProfile profile)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        text = ReplaceControlLinePrefix(text, "A", TetrabeastsControlAction.MoveLeft, profile);
        text = ReplaceControlLinePrefix(text, "D", TetrabeastsControlAction.MoveRight, profile);
        text = ReplaceControlLinePrefix(text, "S", TetrabeastsControlAction.SoftDrop, profile);
        text = ReplaceControlLinePrefix(text, "Q", TetrabeastsControlAction.RotateCounterClockwise, profile);
        text = ReplaceControlLinePrefix(text, "Z", TetrabeastsControlAction.RotateCounterClockwise, profile);
        text = ReplaceControlLinePrefix(text, "E", TetrabeastsControlAction.RotateClockwise, profile);
        text = ReplaceControlLinePrefix(text, "R", TetrabeastsControlAction.Special, profile);
        text = ReplaceControlLinePrefix(text, "Escape", TetrabeastsControlAction.Pause, profile);

        string hardDropLabel = FormatBracketedTutorialLabel(GetTutorialActionBindingLabel(TetrabeastsControlAction.HardDrop, profile));
        string pauseLabel = FormatBracketedTutorialLabel(GetTutorialActionBindingLabel(TetrabeastsControlAction.Pause, profile));

        text = ReplaceOrdinalIgnoreCase(text, "Pressing spacebar", $"Pressing {hardDropLabel}");
        text = ReplaceOrdinalIgnoreCase(text, "Pressing space bar", $"Pressing {hardDropLabel}");
        text = ReplaceOrdinalIgnoreCase(text, "Presseing escape", $"Pressing {pauseLabel}");
        text = ReplaceOrdinalIgnoreCase(text, "Pressing escape", $"Pressing {pauseLabel}");
        text = ReplaceOrdinalIgnoreCase(text, "pressing escape", $"pressing {pauseLabel}");

        return text;
    }

    static string ReplaceControlLinePrefix(string text, string token, TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(token))
            return text;

        string replacement = FormatBracketedTutorialLabel(GetTutorialActionBindingLabel(action, profile));
        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        bool changed = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            int leadingLength = 0;
            while (leadingLength < line.Length && char.IsWhiteSpace(line[leadingLength]))
                leadingLength++;

            string trimmed = line.Substring(leadingLength);
            string prefix = $"{token} -";
            if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            lines[i] = line.Substring(0, leadingLength) + replacement + trimmed.Substring(token.Length);
            changed = true;
        }

        return changed ? string.Join("\n", lines) : text;
    }

    static string ReplaceOrdinalIgnoreCase(string text, string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(oldValue))
            return text;

        int startIndex = 0;
        while (startIndex < text.Length)
        {
            int index = text.IndexOf(oldValue, startIndex, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                break;

            text = text.Substring(0, index) + newValue + text.Substring(index + oldValue.Length);
            startIndex = index + newValue.Length;
        }

        return text;
    }

    static string ReplaceTutorialContinuePromptSuffix(string text, string continueLabel)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(continueLabel))
            return text;

        if (TryReplaceFinalParentheticalToken(text, '(', ')', continueLabel, out string replaced) ||
            TryReplaceFinalParentheticalToken(text, '\uff08', '\uff09', continueLabel, out replaced))
            return replaced;

        return text;
    }

    static bool TryReplaceFinalParentheticalToken(string text, char open, char close, string continueLabel, out string replaced)
    {
        replaced = text;
        int end = text.Length - 1;
        while (end >= 0 && char.IsWhiteSpace(text[end]))
            end--;

        if (end < 0 || text[end] != close)
            return false;

        int start = text.LastIndexOf(open, end);
        if (start < 0)
            return false;

        string suffix = text.Substring(start, end - start + 1);
        if (!suffix.Contains("[F]"))
            return false;

        suffix = suffix.Replace("[F]", FormatBracketedTutorialLabel(continueLabel));
        replaced = text.Substring(0, start) + suffix + text.Substring(end + 1);
        return true;
    }

    static string GetTutorialPressingPhrase(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        if (TryGetMovementTutorialPhrase(action, profile, out string phrase))
            return phrase;

        return $"by pressing {FormatBracketedTutorialLabel(GetTutorialActionBindingLabel(action, profile))}";
    }

    static bool TryGetMovementTutorialPhrase(TetrabeastsControlAction action, TetrabeastsControlProfile profile, out string phrase)
    {
        phrase = string.Empty;

        if (!TryGetMovementTutorialParts(action, out string dpadLabel, out string stickDirection, out _))
            return false;

        bool hasDpad = HasTutorialBindingPath(action, profile, GetDpadPathForMovementAction(action));
        bool hasStick = HasTutorialBindingPath(action, profile, GetStickPathForMovementAction(action));

        if (hasDpad && hasStick)
        {
            string dpadDisplay = GetTutorialPathLabel(GetDpadPathForMovementAction(action), profile, dpadLabel);
            string stickDisplay = GetTutorialPathLabel("<Gamepad>/leftStick", profile, "Left Joystick");
            string stickPhrase = stickDirection == "down"
                ? $"tilting the {FormatBracketedTutorialLabel(stickDisplay)} {stickDirection}"
                : $"tilting the {FormatBracketedTutorialLabel(stickDisplay)} to the {stickDirection}";
            phrase = $"by pressing the {FormatBracketedTutorialLabel(dpadDisplay)} button or {stickPhrase}";
            return true;
        }

        if (hasDpad)
        {
            string dpadDisplay = GetTutorialPathLabel(GetDpadPathForMovementAction(action), profile, dpadLabel);
            phrase = $"by pressing the {FormatBracketedTutorialLabel(dpadDisplay)} button";
            return true;
        }

        if (hasStick)
        {
            string stickDisplay = GetTutorialPathLabel("<Gamepad>/leftStick", profile, "Left Joystick");
            phrase = stickDirection == "down"
                ? $"by tilting the {FormatBracketedTutorialLabel(stickDisplay)} down"
                : $"by tilting the {FormatBracketedTutorialLabel(stickDisplay)} to the {stickDirection}";
            return true;
        }

        return false;
    }

    static string GetTutorialTokenReplacement(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        if (TryGetMovementTutorialTokenReplacement(action, profile, out string replacement))
            return replacement;

        return FormatBracketedTutorialLabel(GetTutorialActionBindingLabel(action, profile));
    }

    static bool TryGetMovementTutorialTokenReplacement(TetrabeastsControlAction action, TetrabeastsControlProfile profile, out string replacement)
    {
        replacement = string.Empty;

        if (!TryGetMovementTutorialParts(action, out string dpadLabel, out _, out string stickLabel))
            return false;

        bool hasDpad = HasTutorialBindingPath(action, profile, GetDpadPathForMovementAction(action));
        bool hasStick = HasTutorialBindingPath(action, profile, GetStickPathForMovementAction(action));

        if (hasDpad && hasStick)
        {
            string dpadDisplay = GetTutorialPathLabel(GetDpadPathForMovementAction(action), profile, dpadLabel);
            string stickDisplay = GetTutorialPathLabel(GetStickPathForMovementAction(action), profile, stickLabel);
            replacement = $"{FormatBracketedTutorialLabel(dpadDisplay)} or {FormatBracketedTutorialLabel(stickDisplay)}";
            return true;
        }

        if (hasDpad)
        {
            string dpadDisplay = GetTutorialPathLabel(GetDpadPathForMovementAction(action), profile, dpadLabel);
            replacement = FormatBracketedTutorialLabel(dpadDisplay);
            return true;
        }

        if (hasStick)
        {
            string stickDisplay = GetTutorialPathLabel(GetStickPathForMovementAction(action), profile, stickLabel);
            replacement = FormatBracketedTutorialLabel(stickDisplay);
            return true;
        }

        return false;
    }

    static bool TryGetMovementTutorialParts(
        TetrabeastsControlAction action,
        out string dpadLabel,
        out string stickDirection,
        out string stickLabel)
    {
        switch (action)
        {
            case TetrabeastsControlAction.MoveLeft:
                dpadLabel = "Left Directional Pad";
                stickDirection = "left";
                stickLabel = "Left Joystick Left";
                return true;

            case TetrabeastsControlAction.MoveRight:
                dpadLabel = "Right Directional Pad";
                stickDirection = "right";
                stickLabel = "Left Joystick Right";
                return true;

            case TetrabeastsControlAction.SoftDrop:
                dpadLabel = "Down Directional Pad";
                stickDirection = "down";
                stickLabel = "Left Joystick Down";
                return true;

            default:
                dpadLabel = string.Empty;
                stickDirection = string.Empty;
                stickLabel = string.Empty;
                return false;
        }
    }

    static bool HasTutorialBindingPath(TetrabeastsControlAction action, TetrabeastsControlProfile profile, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !TryGetActionBindings(action, profile, out var bindings))
            return false;

        for (int i = 0; i < bindings.Count; i++)
        {
            if (string.Equals(bindings[i].Path, path, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static string GetDpadPathForMovementAction(TetrabeastsControlAction action)
    {
        return action switch
        {
            TetrabeastsControlAction.MoveLeft => "<Gamepad>/dpad/left",
            TetrabeastsControlAction.MoveRight => "<Gamepad>/dpad/right",
            TetrabeastsControlAction.SoftDrop => "<Gamepad>/dpad/down",
            _ => string.Empty
        };
    }

    static string GetStickPathForMovementAction(TetrabeastsControlAction action)
    {
        return action switch
        {
            TetrabeastsControlAction.MoveLeft => "<Gamepad>/leftStick/left",
            TetrabeastsControlAction.MoveRight => "<Gamepad>/leftStick/right",
            TetrabeastsControlAction.SoftDrop => "<Gamepad>/leftStick/down",
            _ => string.Empty
        };
    }

    static string GetTutorialActionBindingLabel(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        TetrabeastsControlProfile effectiveProfile = ResolveProfile(profile);
        if (effectiveProfile == TetrabeastsControlProfile.KeyboardMouse)
            return GetKeyboardMouseBindingLabel(action);

        if (TryGetActionBindings(action, effectiveProfile, out var bindings))
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (!binding.IsValid)
                    continue;

                if (TryGetTutorialBindingLabel(binding.Path, effectiveProfile, out string label))
                    return label;

                if (!string.IsNullOrWhiteSpace(binding.Label))
                    return binding.Label;

                return binding.Path;
            }
        }

        return GetControllerBindingLabel(action, effectiveProfile);
    }

    static bool TryGetActionBindings(
        TetrabeastsControlAction action,
        TetrabeastsControlProfile profile,
        out List<TetrabeastsControlBinding> bindings)
    {
        TetrabeastsControlProfile effectiveProfile = ResolveProfile(profile);
        var state = GetBindingProfileState(effectiveProfile);
        if (state.Bindings != null &&
            state.Bindings.TryGetValue(action, out bindings) &&
            bindings != null &&
            bindings.Count > 0)
            return true;

        bindings = null;
        return false;
    }

    static bool TryGetTutorialBindingLabel(string path, TetrabeastsControlProfile profile, out string label)
    {
        if (TryGetControllerBindingDisplayLabel(path, profile, out label))
            return true;

        if (TryGetControllerButtonLabel(path, profile, out label))
            return true;

        switch (path)
        {
            case "<Gamepad>/dpad/left":
                label = "Left Directional Pad";
                return true;
            case "<Gamepad>/dpad/right":
                label = "Right Directional Pad";
                return true;
            case "<Gamepad>/dpad/down":
                label = "Down Directional Pad";
                return true;
            case "<Gamepad>/dpad/up":
                label = "Up Directional Pad";
                return true;
            case "<Gamepad>/leftStick/left":
                label = "Left Joystick Left";
                return true;
            case "<Gamepad>/leftStick/right":
                label = "Left Joystick Right";
                return true;
            case "<Gamepad>/leftStick/down":
                label = "Left Joystick Down";
                return true;
            case "<Gamepad>/leftStick/up":
                label = "Left Joystick Up";
                return true;
            case "<Gamepad>/leftStick":
                label = "Left Joystick";
                return true;
            case "<Gamepad>/dpad":
                label = "Directional Pad";
                return true;
            default:
                label = string.Empty;
                return false;
        }
    }

    static string GetTutorialPathLabel(string path, TetrabeastsControlProfile profile, string fallback)
    {
        return TryGetTutorialBindingLabel(path, profile, out string label) && !string.IsNullOrWhiteSpace(label)
            ? label
            : fallback;
    }

    static string FormatBracketedTutorialLabel(string label)
    {
        return $"[{(string.IsNullOrWhiteSpace(label) ? "Unbound" : label)}]";
    }

    public static bool WasPressed(TetrabeastsControlAction action)
    {
        return WasPressed(action, ActiveInputProfile);
    }

    public static bool WasPressed(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        if (action == TetrabeastsControlAction.MenuSubmit && IsMenuSubmitSuppressed())
            return false;

        if (IsPressedConsumedThisFrame(action))
            return false;

        TetrabeastsControlProfile effectiveProfile = ResolveProfile(profile);

        if (TryWasPressedCustom(action, effectiveProfile, out bool customPressed))
        {
            ConsumePressedIfNeeded(action, customPressed);
            return customPressed;
        }

        bool pressed = false;

        pressed |= SteamInputService.WasPressed(action);

#if ENABLE_INPUT_SYSTEM
        pressed |= WasPressedInputSystem(action, effectiveProfile);
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= WasPressedLegacy(action, effectiveProfile);
#endif

        ConsumePressedIfNeeded(action, pressed);
        return pressed;
    }

    public static bool PeekWasPressed(TetrabeastsControlAction action)
    {
        return PeekWasPressed(action, ActiveInputProfile);
    }

    public static bool PeekWasPressed(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        if (action == TetrabeastsControlAction.MenuSubmit && IsMenuSubmitSuppressed())
            return false;

        TetrabeastsControlProfile effectiveProfile = ResolveProfile(profile);

        if (TryWasPressedCustom(action, effectiveProfile, out bool customPressed))
            return customPressed;

        bool pressed = false;

        pressed |= SteamInputService.WasPressed(action);

#if ENABLE_INPUT_SYSTEM
        pressed |= WasPressedInputSystem(action, effectiveProfile);
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= WasPressedLegacy(action, effectiveProfile);
#endif

        return pressed;
    }

    public static bool WasPressedOrRepeated(TetrabeastsControlAction action, float initialDelaySeconds, float repeatIntervalSeconds)
    {
        return WasPressedOrRepeated(action, ActiveInputProfile, initialDelaySeconds, repeatIntervalSeconds);
    }

    public static bool WasPressedOrRepeated(
        TetrabeastsControlAction action,
        TetrabeastsControlProfile profile,
        float initialDelaySeconds,
        float repeatIntervalSeconds)
    {
        if (!IsRepeatableGameplayAction(action))
            return WasPressed(action, profile);

        TetrabeastsControlProfile effectiveProfile = ResolveProfile(profile);
        bool pressed = WasPressed(action, effectiveProfile);
        bool held = IsHeld(action, effectiveProfile);

        if (!RepeatStates.TryGetValue(action, out var state))
            state = default;

        float now = Time.unscaledTime;
        initialDelaySeconds = Mathf.Max(0.01f, initialDelaySeconds);
        repeatIntervalSeconds = Mathf.Max(0.01f, repeatIntervalSeconds);

        if (pressed && !state.IsHeld)
        {
            state.IsHeld = true;
            state.NextRepeatTime = now + initialDelaySeconds;
            RepeatStates[action] = state;
            return true;
        }

        if (!held)
        {
            if (state.IsHeld)
                RepeatStates[action] = default;

            return false;
        }

        if (!state.IsHeld)
        {
            state.IsHeld = true;
            state.NextRepeatTime = now + initialDelaySeconds;
            RepeatStates[action] = state;
            return false;
        }

        if (now < state.NextRepeatTime)
        {
            RepeatStates[action] = state;
            return false;
        }

        state.NextRepeatTime = now + repeatIntervalSeconds;
        RepeatStates[action] = state;
        return true;
    }

    public static bool TryGetPreferredRotationAction(out TetrabeastsControlAction action)
    {
        action = default;

#if ENABLE_INPUT_SYSTEM
        if (TryGetPreferredRotationActionInputSystem(out action))
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (TryGetPreferredRotationActionLegacy(ActiveInputProfile, out action))
            return true;
#endif

        return false;
    }

    public static bool IsHeld(TetrabeastsControlAction action)
    {
        return IsHeld(action, ActiveInputProfile);
    }

    public static bool IsHeld(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        TetrabeastsControlProfile effectiveProfile = ResolveProfile(profile);
        if (TryIsHeldCustom(action, effectiveProfile, out bool customHeld))
            return customHeld;

        bool held = false;

        held |= SteamInputService.IsHeld(action);

#if ENABLE_INPUT_SYSTEM
        held |= IsHeldInputSystem(action, effectiveProfile);
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        held |= IsHeldLegacy(action, effectiveProfile);
#endif

        return held;
    }

    static bool LooksLikeHandheldPc()
    {
        string deviceDescription = $"{SystemInfo.deviceModel} {SystemInfo.deviceName} {SystemInfo.operatingSystem}";

        return ContainsIgnoreCase(deviceDescription, "steam deck") ||
               ContainsIgnoreCase(deviceDescription, "rog ally") ||
               ContainsIgnoreCase(deviceDescription, "legion go") ||
               ContainsIgnoreCase(deviceDescription, "ayaneo") ||
               ContainsIgnoreCase(deviceDescription, "gpd win");
    }

    static bool ContainsIgnoreCase(string value, string needle)
    {
        return !string.IsNullOrEmpty(value) &&
               value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool IsValidProfileValue(int value)
    {
        return Enum.IsDefined(typeof(TetrabeastsControlProfile), value);
    }

    static Vector2 ClampMenuNavigationDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 1f)
            return direction;

        return direction.normalized;
    }

    static bool IsRepeatableGameplayAction(TetrabeastsControlAction action)
    {
        return action == TetrabeastsControlAction.MoveLeft ||
               action == TetrabeastsControlAction.MoveRight ||
               action == TetrabeastsControlAction.SoftDrop ||
               action == TetrabeastsControlAction.RotateClockwise ||
               action == TetrabeastsControlAction.RotateCounterClockwise;
    }

    static bool IsMenuBindingAction(TetrabeastsControlAction action)
    {
        return action == TetrabeastsControlAction.MenuNavigate ||
               action == TetrabeastsControlAction.MenuSubmit ||
               action == TetrabeastsControlAction.MenuCancel;
    }

    static string GetBindingsPrefsKey(TetrabeastsControlProfile profile)
    {
        return $"{KeyControlBindingsPrefix}{profile}";
    }

    static BindingProfileState GetBindingProfileState(TetrabeastsControlProfile profile)
    {
        profile = ResolveProfile(profile);
        if (BindingProfiles.TryGetValue(profile, out var state))
            return state;

        state = LoadBindingProfileState(profile);
        BindingProfiles[profile] = state;
        return state;
    }

    static BindingProfileState LoadBindingProfileState(TetrabeastsControlProfile profile)
    {
        string json = PlayerPrefs.GetString(GetBindingsPrefsKey(profile), string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return CreateDefaultBindingState(profile, hasCustom: false);

        try
        {
            var saveData = JsonUtility.FromJson<BindingProfileSaveData>(json);
            if (saveData == null || saveData.actions == null)
                return CreateDefaultBindingState(profile, hasCustom: false);

            var state = CreateDefaultBindingState(profile, hasCustom: true);
            for (int i = 0; i < DisplayActions.Length; i++)
                state.Bindings[DisplayActions[i]] = new List<TetrabeastsControlBinding>();

            var loadedActions = new HashSet<TetrabeastsControlAction>();
            for (int i = 0; i < saveData.actions.Count; i++)
            {
                var actionData = saveData.actions[i];
                if (actionData == null || !Enum.TryParse(actionData.action, out TetrabeastsControlAction action))
                    continue;

                loadedActions.Add(action);
                var bindings = new List<TetrabeastsControlBinding>();
                if (actionData.bindings != null)
                {
                    for (int j = 0; j < actionData.bindings.Count; j++)
                    {
                        var bindingData = actionData.bindings[j];
                        if (bindingData == null || string.IsNullOrWhiteSpace(bindingData.path))
                            continue;

                        bindings.Add(new TetrabeastsControlBinding(bindingData.path, bindingData.label));
                    }
                }

                state.Bindings[action] = bindings;
            }

            bool changed = AddMissingDefaultBindings(profile, state, loadedActions);
            if (NormalizeLoadedBindingState(profile, state))
                changed = true;

            if (changed)
                SaveBindingProfileState(profile, state);

            return state;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"TetrabeastsControls: Failed to load saved bindings for {profile}: {ex.Message}");
            return CreateDefaultBindingState(profile, hasCustom: false);
        }
    }

    static BindingProfileState CreateDefaultBindingState(TetrabeastsControlProfile profile, bool hasCustom)
    {
        return new BindingProfileState
        {
            HasCustom = hasCustom,
            Dirty = false,
            Bindings = BuildDefaultBindings(profile)
        };
    }

    static Dictionary<TetrabeastsControlAction, List<TetrabeastsControlBinding>> BuildDefaultBindings(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.KeyboardMouse
            ? BuildKeyboardMouseDefaultBindings()
            : BuildControllerDefaultBindings(profile);
    }

    static bool AddMissingDefaultBindings(
        TetrabeastsControlProfile profile,
        BindingProfileState state,
        HashSet<TetrabeastsControlAction> loadedActions)
    {
        if (state?.Bindings == null)
            return false;

        bool changed = false;
        var defaults = BuildDefaultBindings(profile);
        foreach (var action in DisplayActions)
        {
            if (loadedActions != null && loadedActions.Contains(action))
                continue;

            state.Bindings[action] = defaults.TryGetValue(action, out var bindings)
                ? new List<TetrabeastsControlBinding>(bindings)
                : new List<TetrabeastsControlBinding>();
            changed = true;
        }

        return changed;
    }

    static Dictionary<TetrabeastsControlAction, List<TetrabeastsControlBinding>> BuildKeyboardMouseDefaultBindings()
    {
        return new Dictionary<TetrabeastsControlAction, List<TetrabeastsControlBinding>>
        {
            [TetrabeastsControlAction.MoveLeft] = Bindings(Binding("<Keyboard>/a", "A"), Binding("<Keyboard>/leftArrow", "Left Arrow")),
            [TetrabeastsControlAction.MoveRight] = Bindings(Binding("<Keyboard>/d", "D"), Binding("<Keyboard>/rightArrow", "Right Arrow")),
            [TetrabeastsControlAction.SoftDrop] = Bindings(Binding("<Keyboard>/s", "S"), Binding("<Keyboard>/downArrow", "Down Arrow")),
            [TetrabeastsControlAction.RotateClockwise] = Bindings(Binding("<Keyboard>/e", "E"), Binding("<Keyboard>/upArrow", "Up Arrow")),
            [TetrabeastsControlAction.RotateCounterClockwise] = Bindings(Binding("<Keyboard>/q", "Q"), Binding("<Keyboard>/z", "Z")),
            [TetrabeastsControlAction.HardDrop] = Bindings(Binding("<Keyboard>/space", "Space")),
            [TetrabeastsControlAction.Special] = Bindings(Binding("<Keyboard>/r", "R")),
            [TetrabeastsControlAction.Pause] = Bindings(Binding("<Keyboard>/escape", "Escape")),
            [TetrabeastsControlAction.ToggleControls] = Bindings(Binding("<Keyboard>/c", "C")),
            [TetrabeastsControlAction.MenuNavigate] = Bindings(
                Binding("<Keyboard>/w", "W"),
                Binding("<Keyboard>/a", "A"),
                Binding("<Keyboard>/s", "S"),
                Binding("<Keyboard>/d", "D"),
                Binding("<Keyboard>/upArrow", "Up Arrow"),
                Binding("<Keyboard>/leftArrow", "Left Arrow"),
                Binding("<Keyboard>/downArrow", "Down Arrow"),
                Binding("<Keyboard>/rightArrow", "Right Arrow"),
                Binding("<Keyboard>/tab", "Tab")),
            [TetrabeastsControlAction.MenuSubmit] = Bindings(Binding("<Keyboard>/enter", "Enter"), Binding("<Keyboard>/space", "Space")),
            [TetrabeastsControlAction.MenuCancel] = Bindings(Binding("<Keyboard>/escape", "Escape"))
        };
    }

    static Dictionary<TetrabeastsControlAction, List<TetrabeastsControlBinding>> BuildControllerDefaultBindings(TetrabeastsControlProfile profile)
    {
        return new Dictionary<TetrabeastsControlAction, List<TetrabeastsControlBinding>>
        {
            [TetrabeastsControlAction.MoveLeft] = Bindings(Binding("<Gamepad>/leftStick/left", "Left Stick Left"), Binding("<Gamepad>/dpad/left", "D-Pad Left")),
            [TetrabeastsControlAction.MoveRight] = Bindings(Binding("<Gamepad>/leftStick/right", "Left Stick Right"), Binding("<Gamepad>/dpad/right", "D-Pad Right")),
            [TetrabeastsControlAction.SoftDrop] = Bindings(Binding("<Gamepad>/leftStick/down", "Left Stick Down"), Binding("<Gamepad>/dpad/down", "D-Pad Down")),
            [TetrabeastsControlAction.RotateClockwise] = Bindings(Binding("<Gamepad>/buttonEast", ControllerEastLabel(profile)), Binding("<Gamepad>/rightShoulder", ControllerRightShoulderLabel(profile))),
            [TetrabeastsControlAction.RotateCounterClockwise] = Bindings(Binding("<Gamepad>/buttonWest", ControllerWestLabel(profile)), Binding("<Gamepad>/leftShoulder", ControllerLeftShoulderLabel(profile))),
            [TetrabeastsControlAction.HardDrop] = Bindings(Binding("<Gamepad>/buttonSouth", ControllerSouthLabel(profile))),
            [TetrabeastsControlAction.Special] = Bindings(Binding("<Gamepad>/buttonNorth", ControllerNorthLabel(profile))),
            [TetrabeastsControlAction.Pause] = Bindings(Binding("<Gamepad>/start", ControllerMenuLabel(profile))),
            [TetrabeastsControlAction.ToggleControls] = Bindings(Binding("<Gamepad>/select", ControllerBackLabel(profile))),
            [TetrabeastsControlAction.MenuNavigate] = Bindings(Binding("<Gamepad>/leftStick", "Left Stick"), Binding("<Gamepad>/dpad", "D-Pad")),
            [TetrabeastsControlAction.MenuSubmit] = Bindings(Binding("<Gamepad>/buttonSouth", ControllerSouthLabel(profile))),
            [TetrabeastsControlAction.MenuCancel] = Bindings(Binding("<Gamepad>/buttonEast", ControllerEastLabel(profile)))
        };
    }

    static TetrabeastsControlBinding Binding(string path, string label)
    {
        return new TetrabeastsControlBinding(path, label);
    }

    static List<TetrabeastsControlBinding> Bindings(params TetrabeastsControlBinding[] bindings)
    {
        return new List<TetrabeastsControlBinding>(bindings);
    }

    static bool NormalizeLoadedBindingState(TetrabeastsControlProfile profile, BindingProfileState state)
    {
        if (state == null || !state.HasCustom || profile == TetrabeastsControlProfile.KeyboardMouse)
            return false;

        bool changed = false;

        if (profile == TetrabeastsControlProfile.PlayStationController)
        {
            bool legacyHardDropUsesWest =
                HasSingleBindingPath(state, TetrabeastsControlAction.HardDrop, "<Gamepad>/buttonWest");

            if (legacyHardDropUsesWest)
            {
                state.Bindings[TetrabeastsControlAction.HardDrop] =
                    Bindings(Binding("<Gamepad>/buttonSouth", ControllerSouthLabel(profile)));
                changed = true;
            }

            bool legacyMenuSubmitUsesWest =
                HasSingleBindingPath(state, TetrabeastsControlAction.MenuSubmit, "<Gamepad>/buttonWest");

            if (legacyMenuSubmitUsesWest)
            {
                state.Bindings[TetrabeastsControlAction.MenuSubmit] =
                    Bindings(Binding("<Gamepad>/buttonSouth", ControllerSouthLabel(profile)));
                changed = true;
            }
        }

        foreach (var pair in state.Bindings)
        {
            var bindings = pair.Value;
            if (bindings == null)
                continue;

            for (int i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (!TryGetControllerButtonLabel(binding.Path, profile, out string label) ||
                    string.Equals(binding.Label, label, StringComparison.Ordinal))
                    continue;

                bindings[i] = new TetrabeastsControlBinding(binding.Path, label);
                changed = true;
            }
        }

        return changed;
    }

    static bool HasSingleBindingPath(BindingProfileState state, TetrabeastsControlAction action, string path)
    {
        return state.Bindings.TryGetValue(action, out var bindings) &&
               bindings != null &&
               bindings.Count == 1 &&
               string.Equals(bindings[0].Path, path, StringComparison.OrdinalIgnoreCase);
    }

    static bool TryGetControllerButtonLabel(string path, TetrabeastsControlProfile profile, out string label)
    {
        label = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        switch (path)
        {
            case "<Gamepad>/buttonSouth":
                label = ControllerSouthLabel(profile);
                return true;
            case "<Gamepad>/buttonEast":
                label = ControllerEastLabel(profile);
                return true;
            case "<Gamepad>/buttonWest":
                label = ControllerWestLabel(profile);
                return true;
            case "<Gamepad>/buttonNorth":
                label = ControllerNorthLabel(profile);
                return true;
            case "<Gamepad>/leftShoulder":
                label = ControllerLeftShoulderLabel(profile);
                return true;
            case "<Gamepad>/rightShoulder":
                label = ControllerRightShoulderLabel(profile);
                return true;
            case "<Gamepad>/start":
                label = ControllerMenuLabel(profile);
                return true;
            case "<Gamepad>/select":
                label = ControllerBackLabel(profile);
                return true;
            default:
                return false;
        }
    }

    static bool TryGetControllerBindingDisplayLabel(string path, TetrabeastsControlProfile profile, out string label)
    {
        return TryGetControllerBindingDisplayLabel(path, profile, ControllerGlyphSizePercent, out label);
    }

    static bool TryGetControllerBindingDisplayLabel(string path, TetrabeastsControlProfile profile, int controllerGlyphSizePercent, out string label)
    {
        if (TryGetControllerGlyphForPath(path, profile, out string glyphName))
        {
            label = FormatControllerGlyphTag(glyphName, controllerGlyphSizePercent);
            return true;
        }

        if (TryGetControllerButtonLabel(path, profile, out label))
            return true;

        switch (path)
        {
            case "<Gamepad>/dpad/left":
                label = "D-Pad Left";
                return true;
            case "<Gamepad>/dpad/right":
                label = "D-Pad Right";
                return true;
            case "<Gamepad>/dpad/down":
                label = "D-Pad Down";
                return true;
            case "<Gamepad>/dpad/up":
                label = "D-Pad Up";
                return true;
            case "<Gamepad>/dpad":
                label = "D-Pad";
                return true;
            case "<Gamepad>/leftStick/left":
                label = "Left Stick Left";
                return true;
            case "<Gamepad>/leftStick/right":
                label = "Left Stick Right";
                return true;
            case "<Gamepad>/leftStick/down":
                label = "Left Stick Down";
                return true;
            case "<Gamepad>/leftStick/up":
                label = "Left Stick Up";
                return true;
            case "<Gamepad>/leftStick":
                label = "Left Stick";
                return true;
            default:
                label = string.Empty;
                return false;
        }
    }

    static bool TryGetControllerGlyphForPath(string path, TetrabeastsControlProfile profile, out string glyphName)
    {
        glyphName = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string prefix = GetControllerGlyphPrefix(profile);
        switch (path)
        {
            case "<Gamepad>/buttonSouth":
                glyphName = profile == TetrabeastsControlProfile.PlayStationController ? "PS_Cross" : $"{prefix}_A";
                return true;
            case "<Gamepad>/buttonEast":
                glyphName = profile == TetrabeastsControlProfile.PlayStationController ? "PS_Circle" : $"{prefix}_B";
                return true;
            case "<Gamepad>/buttonWest":
                glyphName = profile == TetrabeastsControlProfile.PlayStationController ? "PS_Square" : $"{prefix}_X";
                return true;
            case "<Gamepad>/buttonNorth":
                glyphName = profile == TetrabeastsControlProfile.PlayStationController ? "PS_Triangle" : $"{prefix}_Y";
                return true;
            case "<Gamepad>/leftShoulder":
                glyphName = profile == TetrabeastsControlProfile.PlayStationController ? "PS_L1" : $"{prefix}_L1";
                return true;
            case "<Gamepad>/rightShoulder":
                glyphName = profile == TetrabeastsControlProfile.PlayStationController ? "PS_R1" : $"{prefix}_R1";
                return true;
            case "<Gamepad>/start":
                glyphName = profile == TetrabeastsControlProfile.PlayStationController
                    ? "PS_Options"
                    : profile == TetrabeastsControlProfile.HandheldController ? "Deck_Menu" : "Xbox_Start";
                return true;
            case "<Gamepad>/select":
                glyphName = profile == TetrabeastsControlProfile.PlayStationController
                    ? "PS_Share"
                    : profile == TetrabeastsControlProfile.HandheldController ? "Deck_View" : "Xbox_Back";
                return true;
            case "<Gamepad>/leftTrigger":
                glyphName = "LT";
                return true;
            case "<Gamepad>/rightTrigger":
                glyphName = "RT";
                return true;
            case "<Gamepad>/leftStickPress":
                glyphName = "L3";
                return true;
            case "<Gamepad>/rightStickPress":
                glyphName = "R3";
                return true;
            case "<Gamepad>/dpad":
                glyphName = "DPad";
                return true;
            case "<Gamepad>/dpad/left":
                glyphName = "DPad_Left";
                return true;
            case "<Gamepad>/dpad/right":
                glyphName = "DPad_Right";
                return true;
            case "<Gamepad>/dpad/down":
                glyphName = "DPad_Down";
                return true;
            case "<Gamepad>/dpad/up":
                glyphName = "DPad_Up";
                return true;
            case "<Gamepad>/leftStick":
                glyphName = "LeftStick";
                return true;
            case "<Gamepad>/leftStick/left":
                glyphName = "LeftStick_Left";
                return true;
            case "<Gamepad>/leftStick/right":
                glyphName = "LeftStick_Right";
                return true;
            case "<Gamepad>/leftStick/down":
                glyphName = "LeftStick_Down";
                return true;
            case "<Gamepad>/leftStick/up":
                glyphName = "LeftStick_Up";
                return true;
            default:
                return false;
        }
    }

    static bool TryGetControllerGlyphFromLabel(string label, TetrabeastsControlProfile profile, out string glyphLabel)
    {
        return TryGetControllerGlyphFromLabel(label, profile, ControllerGlyphSizePercent, out glyphLabel);
    }

    static bool TryGetControllerGlyphFromLabel(string label, TetrabeastsControlProfile profile, int controllerGlyphSizePercent, out string glyphLabel)
    {
        glyphLabel = string.Empty;
        if (string.IsNullOrWhiteSpace(label))
            return false;

        string normalized = NormalizeControllerLabel(label);
        string prefix = GetControllerGlyphPrefix(profile);

        string glyphName = normalized switch
        {
            "A" => profile == TetrabeastsControlProfile.PlayStationController ? "PS_Cross" : $"{prefix}_A",
            "B" => profile == TetrabeastsControlProfile.PlayStationController ? "PS_Circle" : $"{prefix}_B",
            "X" => profile == TetrabeastsControlProfile.PlayStationController ? "PS_Cross" : $"{prefix}_X",
            "Y" => profile == TetrabeastsControlProfile.PlayStationController ? "PS_Triangle" : $"{prefix}_Y",
            "CROSS" => "PS_Cross",
            "CIRCLE" => "PS_Circle",
            "SQUARE" => "PS_Square",
            "TRIANGLE" => "PS_Triangle",
            "LT" => "LT",
            "RT" => "RT",
            "L3" => "L3",
            "R3" => "R3",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(glyphName))
        {
            if (normalized == "LB" || normalized == "L1")
                glyphName = profile == TetrabeastsControlProfile.PlayStationController ? "PS_L1" : $"{prefix}_L1";
            else if (normalized == "RB" || normalized == "R1")
                glyphName = profile == TetrabeastsControlProfile.PlayStationController ? "PS_R1" : $"{prefix}_R1";
            else if (normalized == "START" || normalized == "MENU" || normalized == "OPTIONS")
                glyphName = profile == TetrabeastsControlProfile.PlayStationController
                    ? "PS_Options"
                    : profile == TetrabeastsControlProfile.HandheldController ? "Deck_Menu" : "Xbox_Start";
            else if (normalized == "BACK" || normalized == "VIEW" || normalized == "SHARE")
                glyphName = profile == TetrabeastsControlProfile.PlayStationController
                    ? "PS_Share"
                    : profile == TetrabeastsControlProfile.HandheldController ? "Deck_View" : "Xbox_Back";
            else if (normalized == "DPAD" || normalized == "DIRECTIONALPAD")
                glyphName = "DPad";
            else if (normalized == "DPADLEFT" || normalized == "LEFTDIRECTIONALPAD")
                glyphName = "DPad_Left";
            else if (normalized == "DPADRIGHT" || normalized == "RIGHTDIRECTIONALPAD")
                glyphName = "DPad_Right";
            else if (normalized == "DPADDOWN" || normalized == "DOWNDIRECTIONALPAD")
                glyphName = "DPad_Down";
            else if (normalized == "DPADUP" || normalized == "UPDIRECTIONALPAD")
                glyphName = "DPad_Up";
            else if (normalized == "LEFTSTICK" || normalized == "LEFTJOYSTICK")
                glyphName = "LeftStick";
            else if (normalized == "LEFTSTICKLEFT" || normalized == "LEFTJOYSTICKLEFT")
                glyphName = "LeftStick_Left";
            else if (normalized == "LEFTSTICKRIGHT" || normalized == "LEFTJOYSTICKRIGHT")
                glyphName = "LeftStick_Right";
            else if (normalized == "LEFTSTICKDOWN" || normalized == "LEFTJOYSTICKDOWN")
                glyphName = "LeftStick_Down";
            else if (normalized == "LEFTSTICKUP" || normalized == "LEFTJOYSTICKUP")
                glyphName = "LeftStick_Up";
        }

        if (string.IsNullOrEmpty(glyphName))
            return false;

        glyphLabel = FormatControllerGlyphTag(glyphName, controllerGlyphSizePercent);
        return true;
    }

    static string NormalizeControllerLabel(string label)
    {
        var normalized = label.Trim().ToUpperInvariant();
        normalized = normalized.Replace("-", string.Empty);
        normalized = normalized.Replace("_", string.Empty);
        normalized = normalized.Replace(" ", string.Empty);
        normalized = normalized.Replace("/", string.Empty);
        return normalized;
    }

    static string GetControllerGlyphPrefix(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.HandheldController ? "Deck" : "Xbox";
    }

    static string FormatControllerGlyphTag(string glyphName)
    {
        return FormatControllerGlyphTag(glyphName, ControllerGlyphSizePercent);
    }

    static string FormatControllerGlyphTag(string glyphName, int controllerGlyphSizePercent)
    {
        controllerGlyphSizePercent = Mathf.Clamp(controllerGlyphSizePercent, 50, 250);
        return $"<size={controllerGlyphSizePercent}%><sprite=\"{ControllerGlyphSpriteAssetName}\" name=\"{glyphName}\"></size>";
    }

    static void EnsureCustomBindingState(TetrabeastsControlProfile profile, BindingProfileState state)
    {
        if (!state.HasCustom)
        {
            state.HasCustom = true;
            state.Bindings = BuildDefaultBindings(profile);
        }

        Dictionary<TetrabeastsControlAction, List<TetrabeastsControlBinding>> defaults = null;
        foreach (var action in DisplayActions)
        {
            if (!state.Bindings.ContainsKey(action))
            {
                defaults ??= BuildDefaultBindings(profile);
                state.Bindings[action] = defaults.TryGetValue(action, out var bindings)
                    ? new List<TetrabeastsControlBinding>(bindings)
                    : new List<TetrabeastsControlBinding>();
            }
        }
    }

    static bool TryGetCustomBindingLabel(
        TetrabeastsControlAction action,
        TetrabeastsControlProfile profile,
        int controllerGlyphSizePercent,
        out string label)
    {
        label = string.Empty;
        var state = GetBindingProfileState(profile);
        if (!state.HasCustom)
            return false;

        if (!state.Bindings.TryGetValue(action, out var bindings) || bindings.Count == 0)
        {
            label = "Unbound";
            return true;
        }

        label = FormatBindingLabels(bindings, profile, controllerGlyphSizePercent);
        return true;
    }

    static string FormatBindingLabels(
        List<TetrabeastsControlBinding> bindings,
        TetrabeastsControlProfile profile,
        int controllerGlyphSizePercent)
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            var binding = bindings[i];
            if (!binding.IsValid)
                continue;

            if (profile != TetrabeastsControlProfile.KeyboardMouse &&
                (TryGetControllerBindingDisplayLabel(binding.Path, profile, controllerGlyphSizePercent, out string pathLabel) ||
                 TryGetControllerGlyphFromLabel(binding.Label, profile, controllerGlyphSizePercent, out pathLabel)))
                return pathLabel;

            return string.IsNullOrWhiteSpace(binding.Label) ? binding.Path : binding.Label;
        }

        return "Unbound";
    }

    static string FirstBindingLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return string.Empty;

        string[] separators = { " / ", "/", ",", "\n" };
        string[] parts = label.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0].Trim() : label.Trim();
    }

    static bool TryWasPressedCustom(TetrabeastsControlAction action, TetrabeastsControlProfile profile, out bool pressed)
    {
        pressed = false;
        var state = GetBindingProfileState(profile);
        if (!state.HasCustom)
            return false;

#if ENABLE_INPUT_SYSTEM
        if (!state.Bindings.TryGetValue(action, out var bindings))
            return true;

        for (int i = 0; i < bindings.Count; i++)
        {
            if (WasBindingPressedThisFrame(bindings[i]))
            {
                pressed = true;
                return true;
            }
        }
#endif

        return true;
    }

    static bool TryIsHeldCustom(TetrabeastsControlAction action, TetrabeastsControlProfile profile, out bool held)
    {
        held = false;
        var state = GetBindingProfileState(profile);
        if (!state.HasCustom)
            return false;

#if ENABLE_INPUT_SYSTEM
        if (!state.Bindings.TryGetValue(action, out var bindings))
            return true;

        for (int i = 0; i < bindings.Count; i++)
        {
            if (IsBindingHeld(bindings[i]))
            {
                held = true;
                return true;
            }
        }
#endif

        return true;
    }

#if ENABLE_INPUT_SYSTEM
    static bool WasBindingPressedThisFrame(TetrabeastsControlBinding binding)
    {
        try
        {
            var control = FindBindingControl(binding);
            if (control is ButtonControl button)
                return button.wasPressedThisFrame;

            if (control is StickControl stick)
                return stick.ReadValue().magnitude >= StickPressedThreshold;

            if (control is DpadControl dpad)
                return dpad.ReadValue().magnitude >= StickPressedThreshold;

            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    static bool IsBindingHeld(TetrabeastsControlBinding binding)
    {
        try
        {
            var control = FindBindingControl(binding);
            if (control is ButtonControl button)
                return button.isPressed;

            if (control is StickControl stick)
                return stick.ReadValue().magnitude >= StickPressedThreshold;

            if (control is DpadControl dpad)
                return dpad.ReadValue().magnitude >= StickPressedThreshold;

            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    static InputControl FindBindingControl(TetrabeastsControlBinding binding)
    {
        string path = binding.Path;
        if (TryGetGamepadRelativePath(path, out string relativePath) &&
            TryGetActiveGamepad(out var gamepad))
            return InputSystem.FindControl($"{gamepad.path}/{relativePath}");

        return InputSystem.FindControl(path);
    }

    static bool TryGetGamepadRelativePath(string path, out string relativePath)
    {
        const string Prefix = "<Gamepad>/";
        relativePath = string.Empty;

        if (string.IsNullOrWhiteSpace(path) ||
            !path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        relativePath = path.Substring(Prefix.Length);
        return !string.IsNullOrWhiteSpace(relativePath);
    }

    static bool WasRebindCancelPressed(TetrabeastsControlProfile profile, TetrabeastsControlAction action)
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.backspaceKey.wasPressedThisFrame)
                return true;

            if (profile != TetrabeastsControlProfile.KeyboardMouse && keyboard.escapeKey.wasPressedThisFrame)
                return true;
        }

        var mouse = Mouse.current;
        return mouse != null && mouse.rightButton.wasPressedThisFrame;
    }

    static bool TryReadKeyboardMouseRebind(out TetrabeastsControlBinding binding)
    {
        binding = default;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            foreach (var key in keyboard.allKeys)
            {
                if (key == null || !key.wasPressedThisFrame)
                    continue;

                binding = new TetrabeastsControlBinding($"<Keyboard>/{key.name}", FormatKeyLabel(key));
                return true;
            }
        }

        var mouse = Mouse.current;
        if (mouse == null)
            return false;

        if (mouse.leftButton.wasPressedThisFrame)
            return SetCapturedBinding("<Mouse>/leftButton", "Mouse Left", out binding);

        if (mouse.middleButton.wasPressedThisFrame)
            return SetCapturedBinding("<Mouse>/middleButton", "Mouse Middle", out binding);

        if (mouse.forwardButton.wasPressedThisFrame)
            return SetCapturedBinding("<Mouse>/forwardButton", "Mouse Forward", out binding);

        if (mouse.backButton.wasPressedThisFrame)
            return SetCapturedBinding("<Mouse>/backButton", "Mouse Back", out binding);

        return false;
    }

    static bool TryReadGamepadRebind(TetrabeastsControlProfile profile, out TetrabeastsControlBinding binding)
    {
        binding = default;

        foreach (var gamepad in Gamepad.all)
        {
            if (!IsUsableGamepad(gamepad))
                continue;

            if (TryReadGamepadRebind(gamepad, profile, out binding))
            {
                SetLastInputGamepadAndProfile(gamepad);
                return true;
            }
        }

        return false;
    }

    static bool TryReadGamepadRebind(Gamepad gamepad, TetrabeastsControlProfile profile, out TetrabeastsControlBinding binding)
    {
        binding = default;
        if (!IsUsableGamepad(gamepad))
            return false;

        try
        {
            if (TryCaptureGamepadButton(gamepad.buttonSouth, "<Gamepad>/buttonSouth", ControllerSouthLabel(profile), out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.buttonEast, "<Gamepad>/buttonEast", ControllerEastLabel(profile), out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.buttonWest, "<Gamepad>/buttonWest", ControllerWestLabel(profile), out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.buttonNorth, "<Gamepad>/buttonNorth", ControllerNorthLabel(profile), out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.leftShoulder, "<Gamepad>/leftShoulder", ControllerLeftShoulderLabel(profile), out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.rightShoulder, "<Gamepad>/rightShoulder", ControllerRightShoulderLabel(profile), out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.leftTrigger, "<Gamepad>/leftTrigger", "LT", out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.rightTrigger, "<Gamepad>/rightTrigger", "RT", out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.startButton, "<Gamepad>/start", ControllerMenuLabel(profile), out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.selectButton, "<Gamepad>/select", ControllerBackLabel(profile), out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.leftStickButton, "<Gamepad>/leftStickPress", "Left Stick Press", out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.rightStickButton, "<Gamepad>/rightStickPress", "Right Stick Press", out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.dpad.left, "<Gamepad>/dpad/left", "D-Pad Left", out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.dpad.right, "<Gamepad>/dpad/right", "D-Pad Right", out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.dpad.down, "<Gamepad>/dpad/down", "D-Pad Down", out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.dpad.up, "<Gamepad>/dpad/up", "D-Pad Up", out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.leftStick.left, "<Gamepad>/leftStick/left", "Left Stick Left", out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.leftStick.right, "<Gamepad>/leftStick/right", "Left Stick Right", out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.leftStick.down, "<Gamepad>/leftStick/down", "Left Stick Down", out binding)) return true;
            if (TryCaptureGamepadButton(gamepad.leftStick.up, "<Gamepad>/leftStick/up", "Left Stick Up", out binding)) return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        return false;
    }

    static bool TryCaptureGamepadButton(ButtonControl button, string path, string label, out TetrabeastsControlBinding binding)
    {
        binding = default;
        if (button == null || !button.wasPressedThisFrame)
            return false;

        binding = new TetrabeastsControlBinding(path, label);
        return true;
    }

    static bool SetCapturedBinding(string path, string label, out TetrabeastsControlBinding binding)
    {
        binding = new TetrabeastsControlBinding(path, label);
        return true;
    }

    static bool AnyKeyboardMouseRebindInputHeld()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            foreach (var key in keyboard.allKeys)
            {
                if (key != null && key.isPressed)
                    return true;
            }
        }

        var mouse = Mouse.current;
        return mouse != null &&
               (mouse.leftButton.isPressed ||
                mouse.rightButton.isPressed ||
                mouse.middleButton.isPressed ||
                mouse.forwardButton.isPressed ||
                mouse.backButton.isPressed);
    }

    static bool AnyGamepadRebindInputHeld()
    {
        foreach (var gamepad in Gamepad.all)
        {
            if (!IsUsableGamepad(gamepad))
                continue;

            try
            {
                if (gamepad.buttonSouth.isPressed ||
                    gamepad.buttonEast.isPressed ||
                    gamepad.buttonWest.isPressed ||
                    gamepad.buttonNorth.isPressed ||
                    gamepad.leftShoulder.isPressed ||
                    gamepad.rightShoulder.isPressed ||
                    gamepad.leftTrigger.isPressed ||
                    gamepad.rightTrigger.isPressed ||
                    gamepad.startButton.isPressed ||
                    gamepad.selectButton.isPressed ||
                    gamepad.leftStickButton.isPressed ||
                    gamepad.rightStickButton.isPressed ||
                    gamepad.dpad.left.isPressed ||
                    gamepad.dpad.right.isPressed ||
                    gamepad.dpad.down.isPressed ||
                    gamepad.dpad.up.isPressed ||
                    gamepad.leftStick.ReadValue().magnitude >= StickPressedThreshold)
                    return true;
            }
            catch (InvalidOperationException)
            {
            }
        }

        return false;
    }

    static string FormatKeyLabel(KeyControl key)
    {
        if (key == null)
            return string.Empty;

        string label = key.displayName;
        return string.IsNullOrWhiteSpace(label) ? key.name : label;
    }
#endif

    static string ControllerSouthLabel(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.PlayStationController ? "X" : "A";
    }

    static string ControllerEastLabel(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.PlayStationController ? "Circle" : "B";
    }

    static string ControllerWestLabel(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.PlayStationController ? "Square" : "X";
    }

    static string ControllerNorthLabel(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.PlayStationController ? "Triangle" : "Y";
    }

    static string ControllerLeftShoulderLabel(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.PlayStationController ? "L1" : "LB";
    }

    static string ControllerRightShoulderLabel(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.PlayStationController ? "R1" : "RB";
    }

    static string ControllerMenuLabel(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.PlayStationController ? "Options" : "Start";
    }

    static string ControllerBackLabel(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.PlayStationController ? "Share" : "Back";
    }

    static string GetKeyboardMouseBindingLabel(TetrabeastsControlAction action)
    {
        return action switch
        {
            TetrabeastsControlAction.MoveLeft => "A",
            TetrabeastsControlAction.MoveRight => "D",
            TetrabeastsControlAction.SoftDrop => "S",
            TetrabeastsControlAction.RotateClockwise => "E",
            TetrabeastsControlAction.RotateCounterClockwise => "Q",
            TetrabeastsControlAction.HardDrop => "Space",
            TetrabeastsControlAction.Special => "R",
            TetrabeastsControlAction.Pause => "Escape",
            TetrabeastsControlAction.ToggleControls => "C",
            TetrabeastsControlAction.MenuNavigate => "WASD",
            TetrabeastsControlAction.MenuSubmit => "Enter",
            TetrabeastsControlAction.MenuCancel => "Escape",
            _ => string.Empty
        };
    }

    static string GetControllerBindingLabel(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        return GetControllerBindingLabel(action, profile, ControllerGlyphSizePercent);
    }

    static string GetControllerBindingLabel(
        TetrabeastsControlAction action,
        TetrabeastsControlProfile profile,
        int controllerGlyphSizePercent)
    {
        if (TryGetDefaultControllerBindingPath(action, out string path) &&
            TryGetControllerBindingDisplayLabel(path, profile, controllerGlyphSizePercent, out string glyphLabel))
            return glyphLabel;

        string south = ControllerSouthLabel(profile);
        string east = ControllerEastLabel(profile);
        string west = ControllerWestLabel(profile);
        string north = ControllerNorthLabel(profile);
        string menu = ControllerMenuLabel(profile);
        string back = ControllerBackLabel(profile);

        return action switch
        {
            TetrabeastsControlAction.MoveLeft => "Left Stick Left",
            TetrabeastsControlAction.MoveRight => "Left Stick Right",
            TetrabeastsControlAction.SoftDrop => "Left Stick Down",
            TetrabeastsControlAction.RotateClockwise => east,
            TetrabeastsControlAction.RotateCounterClockwise => west,
            TetrabeastsControlAction.HardDrop => south,
            TetrabeastsControlAction.Special => north,
            TetrabeastsControlAction.Pause => menu,
            TetrabeastsControlAction.ToggleControls => back,
            TetrabeastsControlAction.MenuNavigate => "Left Stick",
            TetrabeastsControlAction.MenuSubmit => south,
            TetrabeastsControlAction.MenuCancel => east,
            _ => string.Empty
        };
    }

    static bool TryGetDefaultControllerBindingPath(TetrabeastsControlAction action, out string path)
    {
        path = action switch
        {
            TetrabeastsControlAction.MoveLeft => "<Gamepad>/leftStick/left",
            TetrabeastsControlAction.MoveRight => "<Gamepad>/leftStick/right",
            TetrabeastsControlAction.SoftDrop => "<Gamepad>/leftStick/down",
            TetrabeastsControlAction.RotateClockwise => "<Gamepad>/buttonEast",
            TetrabeastsControlAction.RotateCounterClockwise => "<Gamepad>/buttonWest",
            TetrabeastsControlAction.HardDrop => "<Gamepad>/buttonSouth",
            TetrabeastsControlAction.Special => "<Gamepad>/buttonNorth",
            TetrabeastsControlAction.Pause => "<Gamepad>/start",
            TetrabeastsControlAction.ToggleControls => "<Gamepad>/select",
            TetrabeastsControlAction.MenuNavigate => "<Gamepad>/leftStick",
            TetrabeastsControlAction.MenuSubmit => "<Gamepad>/buttonSouth",
            TetrabeastsControlAction.MenuCancel => "<Gamepad>/buttonEast",
            _ => string.Empty
        };

        return !string.IsNullOrWhiteSpace(path);
    }

#if ENABLE_INPUT_SYSTEM
    static bool TryGetPreferredRotationActionInputSystem(out TetrabeastsControlAction action)
    {
        action = default;

        bool clockwise = false;
        bool counterClockwise = false;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            clockwise |= keyboard.eKey.wasPressedThisFrame ||
                keyboard.upArrowKey.wasPressedThisFrame ||
                keyboard.eKey.isPressed ||
                keyboard.upArrowKey.isPressed;
            counterClockwise |= keyboard.qKey.wasPressedThisFrame ||
                keyboard.zKey.wasPressedThisFrame ||
                keyboard.qKey.isPressed ||
                keyboard.zKey.isPressed;
        }

        if (TryGetActiveGamepad(out var gamepad))
        {
            try
            {
                clockwise |= gamepad.buttonEast.wasPressedThisFrame ||
                    gamepad.rightShoulder.wasPressedThisFrame ||
                    gamepad.buttonEast.isPressed ||
                    gamepad.rightShoulder.isPressed;
                counterClockwise |= gamepad.buttonWest.wasPressedThisFrame ||
                    gamepad.leftShoulder.wasPressedThisFrame ||
                    gamepad.buttonWest.isPressed ||
                    gamepad.leftShoulder.isPressed;
            }
            catch (InvalidOperationException)
            {
            }
        }

        if (clockwise == counterClockwise)
            return false;

        action = clockwise
            ? TetrabeastsControlAction.RotateClockwise
            : TetrabeastsControlAction.RotateCounterClockwise;
        return true;
    }

    static bool WasPressedInputSystem(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        if (IsMenuCancel(action) && WasKeyboardEscapePressed())
            return true;

        if (action == TetrabeastsControlAction.Pause && WasKeyboardEscapePressed())
            return true;

        if (IsMenuAction(action))
            return WasKeyboardActionPressed(action) || WasGamepadActionPressed(action);

        if (profile == TetrabeastsControlProfile.KeyboardMouse)
            return WasKeyboardActionPressed(action);

        return WasGamepadActionPressed(action);
    }

    static bool WasKeyboardActionPressed(TetrabeastsControlAction action)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        return action switch
        {
            TetrabeastsControlAction.MoveLeft => keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame,
            TetrabeastsControlAction.MoveRight => keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame,
            TetrabeastsControlAction.SoftDrop => keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame,
            TetrabeastsControlAction.RotateClockwise => keyboard.eKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame,
            TetrabeastsControlAction.RotateCounterClockwise => keyboard.qKey.wasPressedThisFrame || keyboard.zKey.wasPressedThisFrame,
            TetrabeastsControlAction.HardDrop => keyboard.spaceKey.wasPressedThisFrame,
            TetrabeastsControlAction.Special => keyboard.rKey.wasPressedThisFrame,
            TetrabeastsControlAction.Pause => keyboard.escapeKey.wasPressedThisFrame,
            TetrabeastsControlAction.ToggleControls => keyboard.cKey.wasPressedThisFrame,
            TetrabeastsControlAction.MenuNavigate =>
                keyboard.wKey.wasPressedThisFrame ||
                keyboard.aKey.wasPressedThisFrame ||
                keyboard.sKey.wasPressedThisFrame ||
                keyboard.dKey.wasPressedThisFrame ||
                keyboard.upArrowKey.wasPressedThisFrame ||
                keyboard.leftArrowKey.wasPressedThisFrame ||
                keyboard.downArrowKey.wasPressedThisFrame ||
                keyboard.rightArrowKey.wasPressedThisFrame ||
                keyboard.tabKey.wasPressedThisFrame,
            TetrabeastsControlAction.MenuSubmit => keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame,
            TetrabeastsControlAction.MenuCancel => keyboard.escapeKey.wasPressedThisFrame,
            _ => false
        };
    }

    static bool WasGamepadActionPressed(TetrabeastsControlAction action)
    {
        if (!TryGetActiveGamepad(out var gamepad))
            return false;

        try
        {
            return action switch
            {
                TetrabeastsControlAction.MoveLeft => gamepad.dpad.left.wasPressedThisFrame || gamepad.leftStick.left.wasPressedThisFrame,
                TetrabeastsControlAction.MoveRight => gamepad.dpad.right.wasPressedThisFrame || gamepad.leftStick.right.wasPressedThisFrame,
                TetrabeastsControlAction.SoftDrop => gamepad.dpad.down.wasPressedThisFrame || gamepad.leftStick.down.wasPressedThisFrame,
                TetrabeastsControlAction.RotateClockwise => gamepad.buttonEast.wasPressedThisFrame || gamepad.rightShoulder.wasPressedThisFrame,
                TetrabeastsControlAction.RotateCounterClockwise => gamepad.buttonWest.wasPressedThisFrame || gamepad.leftShoulder.wasPressedThisFrame,
                TetrabeastsControlAction.HardDrop => gamepad.buttonSouth.wasPressedThisFrame,
                TetrabeastsControlAction.Special => gamepad.buttonNorth.wasPressedThisFrame,
                TetrabeastsControlAction.Pause => gamepad.startButton.wasPressedThisFrame,
                TetrabeastsControlAction.ToggleControls => gamepad.selectButton.wasPressedThisFrame,
                TetrabeastsControlAction.MenuNavigate =>
                    gamepad.dpad.left.wasPressedThisFrame ||
                    gamepad.dpad.right.wasPressedThisFrame ||
                    gamepad.dpad.down.wasPressedThisFrame ||
                    gamepad.dpad.up.wasPressedThisFrame ||
                    gamepad.leftStick.left.wasPressedThisFrame ||
                    gamepad.leftStick.right.wasPressedThisFrame ||
                    gamepad.leftStick.down.wasPressedThisFrame ||
                    gamepad.leftStick.up.wasPressedThisFrame,
                TetrabeastsControlAction.MenuSubmit => gamepad.buttonSouth.wasPressedThisFrame,
                TetrabeastsControlAction.MenuCancel => gamepad.buttonEast.wasPressedThisFrame,
                _ => false
            };
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    static bool IsHeldInputSystem(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        if (IsMenuAction(action))
            return IsKeyboardActionHeld(action) || IsGamepadActionHeld(action);

        if (profile == TetrabeastsControlProfile.KeyboardMouse)
            return IsKeyboardActionHeld(action);

        return IsGamepadActionHeld(action);
    }

    static Vector2 GetMenuNavigationDirectionInputSystem()
    {
        Vector2 direction = Vector2.zero;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                direction.x -= 1f;

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                direction.x += 1f;

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                direction.y -= 1f;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                direction.y += 1f;
        }

        if (TryGetActiveGamepad(out var gamepad))
        {
            try
            {
                direction += gamepad.leftStick.ReadValue();
                direction += gamepad.dpad.ReadValue();
            }
            catch (InvalidOperationException)
            {
            }
        }

        return ClampMenuNavigationDirection(direction);
    }

    static Vector2 GetMenuScrollDirectionInputSystem()
    {
        if (!TryGetActiveGamepad(out var gamepad))
            return Vector2.zero;

        try
        {
            return gamepad.rightStick.ReadValue();
        }
        catch (InvalidOperationException)
        {
            return Vector2.zero;
        }
    }

    static bool WasButtonNavigationPressedInputSystem()
    {
        var keyboard = Keyboard.current;
        bool keyboardPressed = keyboard != null &&
            (keyboard.wKey.wasPressedThisFrame ||
             keyboard.aKey.wasPressedThisFrame ||
             keyboard.sKey.wasPressedThisFrame ||
             keyboard.dKey.wasPressedThisFrame ||
             keyboard.upArrowKey.wasPressedThisFrame ||
             keyboard.leftArrowKey.wasPressedThisFrame ||
             keyboard.downArrowKey.wasPressedThisFrame ||
             keyboard.rightArrowKey.wasPressedThisFrame ||
             keyboard.tabKey.wasPressedThisFrame);

        bool gamepadPressed = false;
        if (TryGetActiveGamepad(out var gamepad))
        {
            try
            {
                gamepadPressed =
                    gamepad.dpad.left.wasPressedThisFrame ||
                    gamepad.dpad.right.wasPressedThisFrame ||
                    gamepad.dpad.down.wasPressedThisFrame ||
                    gamepad.dpad.up.wasPressedThisFrame;
            }
            catch (InvalidOperationException)
            {
                gamepadPressed = false;
            }
        }

        return keyboardPressed || gamepadPressed;
    }

    static bool IsButtonNavigationHeldInputSystem()
    {
        var keyboard = Keyboard.current;
        bool keyboardHeld = keyboard != null &&
            (keyboard.wKey.isPressed ||
             keyboard.aKey.isPressed ||
             keyboard.sKey.isPressed ||
             keyboard.dKey.isPressed ||
             keyboard.upArrowKey.isPressed ||
             keyboard.leftArrowKey.isPressed ||
             keyboard.downArrowKey.isPressed ||
             keyboard.rightArrowKey.isPressed ||
             keyboard.tabKey.isPressed);

        bool gamepadHeld = false;
        if (TryGetActiveGamepad(out var gamepad))
        {
            try
            {
                gamepadHeld =
                    gamepad.dpad.left.isPressed ||
                    gamepad.dpad.right.isPressed ||
                    gamepad.dpad.down.isPressed ||
                    gamepad.dpad.up.isPressed;
            }
            catch (InvalidOperationException)
            {
                gamepadHeld = false;
            }
        }

        return keyboardHeld || gamepadHeld;
    }

    static Vector2 GetButtonNavigationDirectionInputSystem()
    {
        Vector2 direction = Vector2.zero;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                direction.x -= 1f;

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                direction.x += 1f;

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                direction.y -= 1f;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                direction.y += 1f;
        }

        if (TryGetActiveGamepad(out var gamepad))
        {
            try
            {
                direction += gamepad.dpad.ReadValue() + gamepad.leftStick.ReadValue();
            }
            catch (InvalidOperationException)
            {
            }
        }

        return ClampMenuNavigationDirection(direction);
    }

    static bool IsKeyboardActionHeld(TetrabeastsControlAction action)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        return action switch
        {
            TetrabeastsControlAction.MoveLeft => keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed,
            TetrabeastsControlAction.MoveRight => keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed,
            TetrabeastsControlAction.SoftDrop => keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed,
            TetrabeastsControlAction.RotateClockwise => keyboard.eKey.isPressed || keyboard.upArrowKey.isPressed,
            TetrabeastsControlAction.RotateCounterClockwise => keyboard.qKey.isPressed || keyboard.zKey.isPressed,
            TetrabeastsControlAction.HardDrop => keyboard.spaceKey.isPressed,
            TetrabeastsControlAction.Special => keyboard.rKey.isPressed,
            TetrabeastsControlAction.Pause => keyboard.escapeKey.isPressed,
            TetrabeastsControlAction.ToggleControls => keyboard.cKey.isPressed,
            TetrabeastsControlAction.MenuNavigate =>
                keyboard.wKey.isPressed ||
                keyboard.aKey.isPressed ||
                keyboard.sKey.isPressed ||
                keyboard.dKey.isPressed ||
                keyboard.upArrowKey.isPressed ||
                keyboard.leftArrowKey.isPressed ||
                keyboard.downArrowKey.isPressed ||
                keyboard.rightArrowKey.isPressed ||
                keyboard.tabKey.isPressed,
            TetrabeastsControlAction.MenuSubmit => keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed || keyboard.spaceKey.isPressed,
            TetrabeastsControlAction.MenuCancel => keyboard.escapeKey.isPressed,
            _ => false
        };
    }

    static bool IsGamepadActionHeld(TetrabeastsControlAction action)
    {
        if (!TryGetActiveGamepad(out var gamepad))
            return false;

        try
        {
            Vector2 leftStick = gamepad.leftStick.ReadValue();

            return action switch
            {
                TetrabeastsControlAction.MoveLeft => gamepad.dpad.left.isPressed || leftStick.x <= -StickPressedThreshold,
                TetrabeastsControlAction.MoveRight => gamepad.dpad.right.isPressed || leftStick.x >= StickPressedThreshold,
                TetrabeastsControlAction.SoftDrop => gamepad.dpad.down.isPressed || leftStick.y <= -StickPressedThreshold,
                TetrabeastsControlAction.RotateClockwise => gamepad.buttonEast.isPressed || gamepad.rightShoulder.isPressed,
                TetrabeastsControlAction.RotateCounterClockwise => gamepad.buttonWest.isPressed || gamepad.leftShoulder.isPressed,
                TetrabeastsControlAction.HardDrop => gamepad.buttonSouth.isPressed,
                TetrabeastsControlAction.Special => gamepad.buttonNorth.isPressed,
                TetrabeastsControlAction.Pause => gamepad.startButton.isPressed,
                TetrabeastsControlAction.ToggleControls => gamepad.selectButton.isPressed,
                TetrabeastsControlAction.MenuNavigate =>
                    gamepad.dpad.left.isPressed ||
                    gamepad.dpad.right.isPressed ||
                    gamepad.dpad.down.isPressed ||
                    gamepad.dpad.up.isPressed ||
                    Mathf.Abs(leftStick.x) >= StickPressedThreshold ||
                    Mathf.Abs(leftStick.y) >= StickPressedThreshold,
                TetrabeastsControlAction.MenuSubmit => gamepad.buttonSouth.isPressed,
                TetrabeastsControlAction.MenuCancel => gamepad.buttonEast.isPressed,
                _ => false
            };
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    static bool WasKeyboardEscapePressed()
    {
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
    }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
    static bool TryGetPreferredRotationActionLegacy(TetrabeastsControlProfile profile, out TetrabeastsControlAction action)
    {
        action = default;

        KeyCode rotateClockwiseButton = GetLegacyControllerButtonKey(profile, TetrabeastsControlAction.RotateClockwise);
        KeyCode rotateCounterClockwiseButton = GetLegacyControllerButtonKey(profile, TetrabeastsControlAction.RotateCounterClockwise);

        bool clockwise =
            Input.GetKeyDown(KeyCode.E) ||
            Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetKeyDown(rotateClockwiseButton) ||
            Input.GetKeyDown(KeyCode.JoystickButton5) ||
            Input.GetKey(KeyCode.E) ||
            Input.GetKey(KeyCode.UpArrow) ||
            Input.GetKey(rotateClockwiseButton) ||
            Input.GetKey(KeyCode.JoystickButton5);

        bool counterClockwise =
            Input.GetKeyDown(KeyCode.Q) ||
            Input.GetKeyDown(KeyCode.Z) ||
            Input.GetKeyDown(rotateCounterClockwiseButton) ||
            Input.GetKeyDown(KeyCode.JoystickButton4) ||
            Input.GetKey(KeyCode.Q) ||
            Input.GetKey(KeyCode.Z) ||
            Input.GetKey(rotateCounterClockwiseButton) ||
            Input.GetKey(KeyCode.JoystickButton4);

        if (clockwise == counterClockwise)
            return false;

        action = clockwise
            ? TetrabeastsControlAction.RotateClockwise
            : TetrabeastsControlAction.RotateCounterClockwise;
        return true;
    }

    static bool WasPressedLegacy(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        if (IsMenuCancel(action) && Input.GetKeyDown(KeyCode.Escape))
            return true;

        if (action == TetrabeastsControlAction.Pause && Input.GetKeyDown(KeyCode.Escape))
            return true;

        if (IsMenuAction(action))
            return WasKeyboardActionPressedLegacy(action) || WasGamepadActionPressedLegacy(action, profile);

        if (profile == TetrabeastsControlProfile.KeyboardMouse)
            return WasKeyboardActionPressedLegacy(action);

        return WasGamepadActionPressedLegacy(action, profile);
    }

    static bool WasKeyboardActionPressedLegacy(TetrabeastsControlAction action)
    {
        return action switch
        {
            TetrabeastsControlAction.MoveLeft => Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow),
            TetrabeastsControlAction.MoveRight => Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow),
            TetrabeastsControlAction.SoftDrop => Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow),
            TetrabeastsControlAction.RotateClockwise => Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.UpArrow),
            TetrabeastsControlAction.RotateCounterClockwise => Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Z),
            TetrabeastsControlAction.HardDrop => Input.GetKeyDown(KeyCode.Space),
            TetrabeastsControlAction.Special => Input.GetKeyDown(KeyCode.R),
            TetrabeastsControlAction.Pause => Input.GetKeyDown(KeyCode.Escape),
            TetrabeastsControlAction.ToggleControls => Input.GetKeyDown(KeyCode.C),
            TetrabeastsControlAction.MenuNavigate =>
                Input.GetKeyDown(KeyCode.W) ||
                Input.GetKeyDown(KeyCode.A) ||
                Input.GetKeyDown(KeyCode.S) ||
                Input.GetKeyDown(KeyCode.D) ||
                Input.GetKeyDown(KeyCode.UpArrow) ||
                Input.GetKeyDown(KeyCode.LeftArrow) ||
                Input.GetKeyDown(KeyCode.DownArrow) ||
                Input.GetKeyDown(KeyCode.RightArrow) ||
                Input.GetKeyDown(KeyCode.Tab),
            TetrabeastsControlAction.MenuSubmit => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space),
            TetrabeastsControlAction.MenuCancel => Input.GetKeyDown(KeyCode.Escape),
            _ => false
        };
    }

    static bool WasGamepadActionPressedLegacy(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        if (IsLegacySingleButtonAction(action))
            return Input.GetKeyDown(GetLegacyControllerButtonKey(profile, action));

        return action switch
        {
            TetrabeastsControlAction.RotateClockwise => Input.GetKeyDown(GetLegacyControllerButtonKey(profile, action)) || Input.GetKeyDown(KeyCode.JoystickButton5),
            TetrabeastsControlAction.RotateCounterClockwise => Input.GetKeyDown(GetLegacyControllerButtonKey(profile, action)) || Input.GetKeyDown(KeyCode.JoystickButton4),
            TetrabeastsControlAction.MenuNavigate => Mathf.Abs(Input.GetAxisRaw("Horizontal")) >= StickPressedThreshold || Mathf.Abs(Input.GetAxisRaw("Vertical")) >= StickPressedThreshold,
            _ => false
        };
    }

    static bool IsHeldLegacy(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        if (IsMenuAction(action))
            return IsKeyboardActionHeldLegacy(action) || IsGamepadActionHeldLegacy(action, profile);

        if (profile == TetrabeastsControlProfile.KeyboardMouse)
            return IsKeyboardActionHeldLegacy(action);

        return IsGamepadActionHeldLegacy(action, profile);
    }

    static Vector2 GetMenuNavigationDirectionLegacy()
    {
        Vector2 direction = Vector2.zero;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            direction.x -= 1f;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            direction.x += 1f;

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            direction.y -= 1f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            direction.y += 1f;

        direction.x += Input.GetAxisRaw("Horizontal");
        direction.y += Input.GetAxisRaw("Vertical");

        return ClampMenuNavigationDirection(direction);
    }

    static Vector2 GetMenuScrollDirectionLegacy()
    {
        return ClampMenuNavigationDirection(new Vector2(
            ReadBestLegacyAxis("RightStickHorizontal", "Right Stick X", "JoystickRightStickHorizontal"),
            ReadBestLegacyAxis("RightStickVertical", "Right Stick Y", "JoystickRightStickVertical")));
    }

    static float ReadBestLegacyAxis(params string[] axisNames)
    {
        float best = 0f;
        if (axisNames == null)
            return best;

        for (int i = 0; i < axisNames.Length; i++)
        {
            if (!TryReadLegacyAxisRaw(axisNames[i], out float value))
                continue;

            if (Mathf.Abs(value) > Mathf.Abs(best))
                best = value;
        }

        return best;
    }

    static bool TryReadLegacyAxisRaw(string axisName, out float value)
    {
        value = 0f;
        if (string.IsNullOrWhiteSpace(axisName) || MissingLegacyAxes.Contains(axisName))
            return false;

        try
        {
            value = Input.GetAxisRaw(axisName);
            return true;
        }
        catch (ArgumentException)
        {
            MissingLegacyAxes.Add(axisName);
            return false;
        }
    }

    static bool WasButtonNavigationPressedLegacy()
    {
        return Input.GetKeyDown(KeyCode.W) ||
               Input.GetKeyDown(KeyCode.A) ||
               Input.GetKeyDown(KeyCode.S) ||
               Input.GetKeyDown(KeyCode.D) ||
               Input.GetKeyDown(KeyCode.UpArrow) ||
               Input.GetKeyDown(KeyCode.LeftArrow) ||
               Input.GetKeyDown(KeyCode.DownArrow) ||
               Input.GetKeyDown(KeyCode.RightArrow) ||
               Input.GetKeyDown(KeyCode.Tab);
    }

    static bool IsButtonNavigationHeldLegacy()
    {
        return Input.GetKey(KeyCode.W) ||
               Input.GetKey(KeyCode.A) ||
               Input.GetKey(KeyCode.S) ||
               Input.GetKey(KeyCode.D) ||
               Input.GetKey(KeyCode.UpArrow) ||
               Input.GetKey(KeyCode.LeftArrow) ||
               Input.GetKey(KeyCode.DownArrow) ||
               Input.GetKey(KeyCode.RightArrow) ||
               Input.GetKey(KeyCode.Tab);
    }

    static Vector2 GetButtonNavigationDirectionLegacy()
    {
        Vector2 direction = Vector2.zero;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            direction.x -= 1f;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            direction.x += 1f;

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            direction.y -= 1f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            direction.y += 1f;

        return ClampMenuNavigationDirection(direction);
    }

    static bool IsKeyboardActionHeldLegacy(TetrabeastsControlAction action)
    {
        return action switch
        {
            TetrabeastsControlAction.MoveLeft => Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow),
            TetrabeastsControlAction.MoveRight => Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow),
            TetrabeastsControlAction.SoftDrop => Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
            TetrabeastsControlAction.RotateClockwise => Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.UpArrow),
            TetrabeastsControlAction.RotateCounterClockwise => Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.Z),
            TetrabeastsControlAction.HardDrop => Input.GetKey(KeyCode.Space),
            TetrabeastsControlAction.Special => Input.GetKey(KeyCode.R),
            TetrabeastsControlAction.Pause => Input.GetKey(KeyCode.Escape),
            TetrabeastsControlAction.ToggleControls => Input.GetKey(KeyCode.C),
            TetrabeastsControlAction.MenuNavigate =>
                Input.GetKey(KeyCode.W) ||
                Input.GetKey(KeyCode.A) ||
                Input.GetKey(KeyCode.S) ||
                Input.GetKey(KeyCode.D) ||
                Input.GetKey(KeyCode.UpArrow) ||
                Input.GetKey(KeyCode.LeftArrow) ||
                Input.GetKey(KeyCode.DownArrow) ||
                Input.GetKey(KeyCode.RightArrow) ||
                Input.GetKey(KeyCode.Tab),
            TetrabeastsControlAction.MenuSubmit => Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter) || Input.GetKey(KeyCode.Space),
            TetrabeastsControlAction.MenuCancel => Input.GetKey(KeyCode.Escape),
            _ => false
        };
    }

    static bool IsGamepadActionHeldLegacy(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (IsLegacySingleButtonAction(action))
            return Input.GetKey(GetLegacyControllerButtonKey(profile, action));

        return action switch
        {
            TetrabeastsControlAction.MoveLeft => horizontal <= -StickPressedThreshold,
            TetrabeastsControlAction.MoveRight => horizontal >= StickPressedThreshold,
            TetrabeastsControlAction.SoftDrop => vertical <= -StickPressedThreshold,
            TetrabeastsControlAction.RotateClockwise => Input.GetKey(GetLegacyControllerButtonKey(profile, action)) || Input.GetKey(KeyCode.JoystickButton5),
            TetrabeastsControlAction.RotateCounterClockwise => Input.GetKey(GetLegacyControllerButtonKey(profile, action)) || Input.GetKey(KeyCode.JoystickButton4),
            TetrabeastsControlAction.MenuNavigate => Mathf.Abs(horizontal) >= StickPressedThreshold || Mathf.Abs(vertical) >= StickPressedThreshold,
            _ => false
        };
    }

    static bool IsLegacySingleButtonAction(TetrabeastsControlAction action)
    {
        return action == TetrabeastsControlAction.HardDrop ||
               action == TetrabeastsControlAction.Special ||
               action == TetrabeastsControlAction.Pause ||
               action == TetrabeastsControlAction.ToggleControls ||
               action == TetrabeastsControlAction.MenuSubmit ||
               action == TetrabeastsControlAction.MenuCancel;
    }

    static KeyCode GetLegacyControllerButtonKey(TetrabeastsControlProfile profile, TetrabeastsControlAction action)
    {
        bool playStation = profile == TetrabeastsControlProfile.PlayStationController;

        return action switch
        {
            TetrabeastsControlAction.RotateClockwise => playStation ? KeyCode.JoystickButton2 : KeyCode.JoystickButton1,
            TetrabeastsControlAction.RotateCounterClockwise => playStation ? KeyCode.JoystickButton0 : KeyCode.JoystickButton2,
            TetrabeastsControlAction.HardDrop => playStation ? KeyCode.JoystickButton1 : KeyCode.JoystickButton0,
            TetrabeastsControlAction.Special => KeyCode.JoystickButton3,
            TetrabeastsControlAction.Pause => playStation ? KeyCode.JoystickButton9 : KeyCode.JoystickButton7,
            TetrabeastsControlAction.ToggleControls => playStation ? KeyCode.JoystickButton8 : KeyCode.JoystickButton6,
            TetrabeastsControlAction.MenuSubmit => playStation ? KeyCode.JoystickButton1 : KeyCode.JoystickButton0,
            TetrabeastsControlAction.MenuCancel => playStation ? KeyCode.JoystickButton2 : KeyCode.JoystickButton1,
            _ => KeyCode.None
        };
    }
#endif

    static bool IsMenuCancel(TetrabeastsControlAction action)
    {
        return action == TetrabeastsControlAction.MenuCancel;
    }

    static bool IsMenuAction(TetrabeastsControlAction action)
    {
        return action == TetrabeastsControlAction.MenuNavigate ||
               action == TetrabeastsControlAction.MenuSubmit ||
               action == TetrabeastsControlAction.MenuCancel;
    }

    static bool IsPressedConsumedThisFrame(TetrabeastsControlAction action)
    {
        return ShouldConsumePressedAction(action) &&
               PressConsumedFrames.TryGetValue(action, out int frame) &&
               frame == Time.frameCount;
    }

    static void ConsumePressedIfNeeded(TetrabeastsControlAction action, bool pressed)
    {
        if (pressed && ShouldConsumePressedAction(action))
            PressConsumedFrames[action] = Time.frameCount;
    }

    static bool ShouldConsumePressedAction(TetrabeastsControlAction action)
    {
        return !IsMenuAction(action);
    }

    struct HoldRepeatState
    {
        public bool IsHeld;
        public float NextRepeatTime;
    }

    class BindingProfileState
    {
        public bool HasCustom;
        public bool Dirty;
        public Dictionary<TetrabeastsControlAction, List<TetrabeastsControlBinding>> Bindings = new();
    }

    [Serializable]
    class BindingProfileSaveData
    {
        public List<BindingActionSaveData> actions = new();
    }

    [Serializable]
    class BindingActionSaveData
    {
        public string action;
        public List<BindingSaveData> bindings = new();
    }

    [Serializable]
    class BindingSaveData
    {
        public string path;
        public string label;
    }
}
