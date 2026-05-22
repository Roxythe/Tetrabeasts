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
    MenuCancel
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
    const float StickPressedThreshold = 0.5f;

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
    static readonly Dictionary<TetrabeastsControlProfile, BindingProfileState> BindingProfiles = new();
    static bool hasLastInputProfile;
    static TetrabeastsControlProfile lastInputProfile = TetrabeastsControlProfile.KeyboardMouse;

    public static event Action<TetrabeastsControlProfile> ProfileChanged;
    public static event Action<TetrabeastsControlProfile> BindingsChanged;
    public static event Action<TetrabeastsControlProfile> PlatformDefaultProfileChanged;

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

    public static void SaveProfile(TetrabeastsControlProfile profile)
    {
        if (!IsValidProfileValue((int)profile))
            profile = TetrabeastsControlProfile.PlatformDefault;

        if (SavedProfile == profile)
            return;

        PlayerPrefs.SetInt(KeyControlProfile, (int)profile);
        PlayerPrefs.Save();
        SteamCloudSaveService.QueueUpload();
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

    public static bool WasMenuNavigationPressedThisFrame()
    {
        return WasPressed(TetrabeastsControlAction.MenuNavigate);
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
            PlayerPrefs.Save();
            SteamCloudSaveService.QueueUpload();
            return;
        }

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

        PlayerPrefs.SetString(GetBindingsPrefsKey(editableProfile), JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        SteamCloudSaveService.QueueUpload();
        state.Dirty = false;
    }

    public static void ResetBindingsToDefault(TetrabeastsControlProfile profile)
    {
        TetrabeastsControlProfile editableProfile = GetEditableProfile(profile);
        PlayerPrefs.DeleteKey(GetBindingsPrefsKey(editableProfile));
        BindingProfiles[editableProfile] = CreateDefaultBindingState(editableProfile, hasCustom: false);
        PlayerPrefs.Save();
        SteamCloudSaveService.QueueUpload();
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

        if (hasLastInputProfile)
            return lastInputProfile;

        if (SteamInputService.TryGetDetectedControlProfile(out var steamProfile))
            return steamProfile;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
            return GetProfileForGamepad(Gamepad.current);
#endif

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

        var gamepad = Gamepad.current;
        if (WasGamepadUsedThisFrame(gamepad))
        {
            profile = GetProfileForGamepad(gamepad);
            return true;
        }
#endif

        if (!hasLastInputProfile && SteamInputService.TryGetDetectedControlProfile(out var steamProfile))
        {
            profile = steamProfile;
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
        if (gamepad == null)
            return false;

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

    static TetrabeastsControlProfile GetProfileForGamepad(Gamepad gamepad)
    {
        if (gamepad == null)
            return TetrabeastsControlProfile.XboxController;

        string description = $"{gamepad.GetType().Name} {gamepad.name} {gamepad.displayName} " +
            $"{gamepad.description.product} {gamepad.description.manufacturer} {gamepad.description.interfaceName}";

        if (ContainsIgnoreCase(description, "dualshock") ||
            ContainsIgnoreCase(description, "dualsense") ||
            ContainsIgnoreCase(description, "playstation") ||
            ContainsIgnoreCase(description, "ps4") ||
            ContainsIgnoreCase(description, "ps5") ||
            ContainsIgnoreCase(description, "wireless controller"))
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
#endif

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
                GetBindingLabel(DisplayActions[i], effectiveProfile));

        return rows;
    }

    public static string GetBindingLabel(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        TetrabeastsControlProfile effectiveProfile = ResolveProfile(profile);

        if (TryGetCustomBindingLabel(action, effectiveProfile, out string customLabel))
            return customLabel;

        if (effectiveProfile == TetrabeastsControlProfile.KeyboardMouse)
            return GetKeyboardMouseBindingLabel(action);

        string steamBindingLabel = SteamInputService.GetBindingLabel(action);
        if (!string.IsNullOrWhiteSpace(steamBindingLabel))
            return FirstBindingLabel(steamBindingLabel);

        return GetControllerBindingLabel(action, effectiveProfile);
    }

    public static bool WasPressed(TetrabeastsControlAction action)
    {
        return WasPressed(action, EffectiveProfile);
    }

    public static bool WasPressed(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
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
        return WasPressedOrRepeated(action, EffectiveProfile, initialDelaySeconds, repeatIntervalSeconds);
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

        if (pressed)
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

    public static bool IsHeld(TetrabeastsControlAction action)
    {
        return IsHeld(action, EffectiveProfile);
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

            for (int i = 0; i < saveData.actions.Count; i++)
            {
                var actionData = saveData.actions[i];
                if (actionData == null || !Enum.TryParse(actionData.action, out TetrabeastsControlAction action))
                    continue;

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

    static void EnsureCustomBindingState(TetrabeastsControlProfile profile, BindingProfileState state)
    {
        if (!state.HasCustom)
        {
            state.HasCustom = true;
            state.Bindings = BuildDefaultBindings(profile);
        }

        foreach (var action in DisplayActions)
        {
            if (!state.Bindings.ContainsKey(action))
                state.Bindings[action] = new List<TetrabeastsControlBinding>();
        }
    }

    static bool TryGetCustomBindingLabel(TetrabeastsControlAction action, TetrabeastsControlProfile profile, out string label)
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

        label = FormatBindingLabels(bindings);
        return true;
    }

    static string FormatBindingLabels(List<TetrabeastsControlBinding> bindings)
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            var binding = bindings[i];
            if (!binding.IsValid)
                continue;

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
        var control = InputSystem.FindControl(binding.Path);
        if (control is ButtonControl button)
            return button.wasPressedThisFrame;

        if (control is StickControl stick)
            return stick.ReadValue().magnitude >= StickPressedThreshold;

        if (control is DpadControl dpad)
            return dpad.ReadValue().magnitude >= StickPressedThreshold;

        return false;
    }

    static bool IsBindingHeld(TetrabeastsControlBinding binding)
    {
        var control = InputSystem.FindControl(binding.Path);
        if (control is ButtonControl button)
            return button.isPressed;

        if (control is StickControl stick)
            return stick.ReadValue().magnitude >= StickPressedThreshold;

        if (control is DpadControl dpad)
            return dpad.ReadValue().magnitude >= StickPressedThreshold;

        return false;
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
        var gamepad = Gamepad.current;
        if (gamepad == null)
            return false;

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
        var gamepad = Gamepad.current;
        return gamepad != null &&
               (gamepad.buttonSouth.isPressed ||
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
                gamepad.leftStick.ReadValue().magnitude >= StickPressedThreshold);
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
        return profile == TetrabeastsControlProfile.PlayStationController ? "\u2715" : "A";
    }

    static string ControllerEastLabel(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.PlayStationController ? "\u25CB" : "B";
    }

    static string ControllerWestLabel(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.PlayStationController ? "\u25A1" : "X";
    }

    static string ControllerNorthLabel(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.PlayStationController ? "\u25B3" : "Y";
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
        return profile == TetrabeastsControlProfile.PlayStationController ? "Options" : "Menu";
    }

    static string ControllerBackLabel(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.PlayStationController ? "Share" : "View";
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
            TetrabeastsControlAction.MenuNavigate => "WASD",
            TetrabeastsControlAction.MenuSubmit => "Enter",
            TetrabeastsControlAction.MenuCancel => "Escape",
            _ => string.Empty
        };
    }

    static string GetControllerBindingLabel(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        bool playStation = profile == TetrabeastsControlProfile.PlayStationController;

        string south = playStation ? "\u2715" : "A";
        string east = playStation ? "\u25CB" : "B";
        string west = playStation ? "\u25A1" : "X";
        string north = playStation ? "\u25B3" : "Y";
        string menu = playStation ? "Options" : "Menu";

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
            TetrabeastsControlAction.MenuNavigate => "Left Stick",
            TetrabeastsControlAction.MenuSubmit => south,
            TetrabeastsControlAction.MenuCancel => east,
            _ => string.Empty
        };
    }

#if ENABLE_INPUT_SYSTEM
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
        var gamepad = Gamepad.current;
        if (gamepad == null)
            return false;

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

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            direction += gamepad.leftStick.ReadValue();
            direction += gamepad.dpad.ReadValue();
        }

        return ClampMenuNavigationDirection(direction);
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

        var gamepad = Gamepad.current;
        bool gamepadPressed = gamepad != null &&
            (gamepad.dpad.left.wasPressedThisFrame ||
             gamepad.dpad.right.wasPressedThisFrame ||
             gamepad.dpad.down.wasPressedThisFrame ||
             gamepad.dpad.up.wasPressedThisFrame);

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

        var gamepad = Gamepad.current;
        bool gamepadHeld = gamepad != null &&
            (gamepad.dpad.left.isPressed ||
             gamepad.dpad.right.isPressed ||
             gamepad.dpad.down.isPressed ||
             gamepad.dpad.up.isPressed);

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

        var gamepad = Gamepad.current;
        if (gamepad != null)
            direction += gamepad.dpad.ReadValue();

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
        var gamepad = Gamepad.current;
        if (gamepad == null)
            return false;

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

    static bool WasKeyboardEscapePressed()
    {
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
    }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
    static bool WasPressedLegacy(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        if (IsMenuCancel(action) && Input.GetKeyDown(KeyCode.Escape))
            return true;

        if (action == TetrabeastsControlAction.Pause && Input.GetKeyDown(KeyCode.Escape))
            return true;

        if (IsMenuAction(action))
            return WasKeyboardActionPressedLegacy(action) || WasGamepadActionPressedLegacy(action);

        if (profile == TetrabeastsControlProfile.KeyboardMouse)
            return WasKeyboardActionPressedLegacy(action);

        return WasGamepadActionPressedLegacy(action);
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

    static bool WasGamepadActionPressedLegacy(TetrabeastsControlAction action)
    {
        return action switch
        {
            TetrabeastsControlAction.RotateClockwise => Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.JoystickButton5),
            TetrabeastsControlAction.RotateCounterClockwise => Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.JoystickButton4),
            TetrabeastsControlAction.HardDrop => Input.GetKeyDown(KeyCode.JoystickButton0),
            TetrabeastsControlAction.Special => Input.GetKeyDown(KeyCode.JoystickButton3),
            TetrabeastsControlAction.Pause => Input.GetKeyDown(KeyCode.JoystickButton7),
            TetrabeastsControlAction.MenuNavigate => Mathf.Abs(Input.GetAxisRaw("Horizontal")) >= StickPressedThreshold || Mathf.Abs(Input.GetAxisRaw("Vertical")) >= StickPressedThreshold,
            TetrabeastsControlAction.MenuSubmit => Input.GetKeyDown(KeyCode.JoystickButton0),
            TetrabeastsControlAction.MenuCancel => Input.GetKeyDown(KeyCode.JoystickButton1),
            _ => false
        };
    }

    static bool IsHeldLegacy(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        if (IsMenuAction(action))
            return IsKeyboardActionHeldLegacy(action) || IsGamepadActionHeldLegacy(action);

        if (profile == TetrabeastsControlProfile.KeyboardMouse)
            return IsKeyboardActionHeldLegacy(action);

        return IsGamepadActionHeldLegacy(action);
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

    static bool IsGamepadActionHeldLegacy(TetrabeastsControlAction action)
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        return action switch
        {
            TetrabeastsControlAction.MoveLeft => horizontal <= -StickPressedThreshold,
            TetrabeastsControlAction.MoveRight => horizontal >= StickPressedThreshold,
            TetrabeastsControlAction.SoftDrop => vertical <= -StickPressedThreshold,
            TetrabeastsControlAction.RotateClockwise => Input.GetKey(KeyCode.JoystickButton1) || Input.GetKey(KeyCode.JoystickButton5),
            TetrabeastsControlAction.RotateCounterClockwise => Input.GetKey(KeyCode.JoystickButton2) || Input.GetKey(KeyCode.JoystickButton4),
            TetrabeastsControlAction.HardDrop => Input.GetKey(KeyCode.JoystickButton0),
            TetrabeastsControlAction.Special => Input.GetKey(KeyCode.JoystickButton3),
            TetrabeastsControlAction.Pause => Input.GetKey(KeyCode.JoystickButton7),
            TetrabeastsControlAction.MenuNavigate => Mathf.Abs(horizontal) >= StickPressedThreshold || Mathf.Abs(vertical) >= StickPressedThreshold,
            TetrabeastsControlAction.MenuSubmit => Input.GetKey(KeyCode.JoystickButton0),
            TetrabeastsControlAction.MenuCancel => Input.GetKey(KeyCode.JoystickButton1),
            _ => false
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
