using System;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
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

public static class TetrabeastsControls
{
    const string KeyControlProfile = "Settings_ControlProfile";

    static readonly TetrabeastsControlProfile[] ProfileOptions =
    {
        TetrabeastsControlProfile.PlatformDefault,
        TetrabeastsControlProfile.KeyboardMouse,
        TetrabeastsControlProfile.XboxController,
        TetrabeastsControlProfile.PlayStationController,
        TetrabeastsControlProfile.HandheldController
    };

    public static event Action<TetrabeastsControlProfile> ProfileChanged;

    public static IReadOnlyList<TetrabeastsControlProfile> SelectableProfiles => ProfileOptions;

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
        ProfileChanged?.Invoke(profile);
    }

    public static void ResetToPlatformDefault()
    {
        SaveProfile(TetrabeastsControlProfile.PlatformDefault);
    }

    public static TetrabeastsControlProfile ResolveProfile(TetrabeastsControlProfile profile)
    {
        return profile == TetrabeastsControlProfile.PlatformDefault
            ? GetPlatformDefaultProfile()
            : profile;
    }

    public static TetrabeastsControlProfile GetPlatformDefaultProfile()
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
        var actions = new[]
        {
            TetrabeastsControlAction.MoveLeft,
            TetrabeastsControlAction.MoveRight,
            TetrabeastsControlAction.SoftDrop,
            TetrabeastsControlAction.RotateClockwise,
            TetrabeastsControlAction.RotateCounterClockwise,
            TetrabeastsControlAction.HardDrop,
            TetrabeastsControlAction.Special,
            TetrabeastsControlAction.Pause,
            TetrabeastsControlAction.MenuNavigate,
            TetrabeastsControlAction.MenuSubmit,
            TetrabeastsControlAction.MenuCancel
        };

        var rows = new TetrabeastsControlBindingRow[actions.Length];
        for (int i = 0; i < actions.Length; i++)
            rows[i] = new TetrabeastsControlBindingRow(
                actions[i],
                GetActionLabel(actions[i]),
                GetBindingLabel(actions[i], effectiveProfile));

        return rows;
    }

    public static string GetBindingLabel(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        TetrabeastsControlProfile effectiveProfile = ResolveProfile(profile);

        if (effectiveProfile == TetrabeastsControlProfile.KeyboardMouse)
            return GetKeyboardMouseBindingLabel(action);

        return GetControllerBindingLabel(action, effectiveProfile);
    }

    public static bool WasPressed(TetrabeastsControlAction action)
    {
        return WasPressed(action, EffectiveProfile);
    }

    public static bool WasPressed(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        TetrabeastsControlProfile effectiveProfile = ResolveProfile(profile);

        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        pressed |= WasPressedInputSystem(action, effectiveProfile);
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= WasPressedLegacy(action, effectiveProfile);
#endif

        return pressed;
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

    static string GetKeyboardMouseBindingLabel(TetrabeastsControlAction action)
    {
        return action switch
        {
            TetrabeastsControlAction.MoveLeft => "A / Left Arrow",
            TetrabeastsControlAction.MoveRight => "D / Right Arrow",
            TetrabeastsControlAction.SoftDrop => "S / Down Arrow",
            TetrabeastsControlAction.RotateClockwise => "E / Up Arrow",
            TetrabeastsControlAction.RotateCounterClockwise => "Q / Z",
            TetrabeastsControlAction.HardDrop => "Space",
            TetrabeastsControlAction.Special => "R",
            TetrabeastsControlAction.Pause => "Escape",
            TetrabeastsControlAction.MenuNavigate => "WASD / Arrow Keys / Tab",
            TetrabeastsControlAction.MenuSubmit => "Enter / Space",
            TetrabeastsControlAction.MenuCancel => "Escape",
            _ => string.Empty
        };
    }

    static string GetControllerBindingLabel(TetrabeastsControlAction action, TetrabeastsControlProfile profile)
    {
        bool playStation = profile == TetrabeastsControlProfile.PlayStationController;

        string south = playStation ? "Cross" : "A";
        string east = playStation ? "Circle" : "B";
        string west = playStation ? "Square" : "X";
        string north = playStation ? "Triangle" : "Y";
        string leftShoulder = playStation ? "L1" : "LB";
        string rightShoulder = playStation ? "R1" : "RB";
        string menu = playStation ? "Options" : "Menu";

        return action switch
        {
            TetrabeastsControlAction.MoveLeft => "Left Stick Left / D-Pad Left",
            TetrabeastsControlAction.MoveRight => "Left Stick Right / D-Pad Right",
            TetrabeastsControlAction.SoftDrop => "Left Stick Down / D-Pad Down",
            TetrabeastsControlAction.RotateClockwise => $"{east} / {rightShoulder}",
            TetrabeastsControlAction.RotateCounterClockwise => $"{west} / {leftShoulder}",
            TetrabeastsControlAction.HardDrop => south,
            TetrabeastsControlAction.Special => north,
            TetrabeastsControlAction.Pause => menu,
            TetrabeastsControlAction.MenuNavigate => "Left Stick / D-Pad",
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
            TetrabeastsControlAction.MenuSubmit => gamepad.buttonSouth.wasPressedThisFrame,
            TetrabeastsControlAction.MenuCancel => gamepad.buttonEast.wasPressedThisFrame,
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
            TetrabeastsControlAction.MenuSubmit => Input.GetKeyDown(KeyCode.JoystickButton0),
            TetrabeastsControlAction.MenuCancel => Input.GetKeyDown(KeyCode.JoystickButton1),
            _ => false
        };
    }
#endif

    static bool IsMenuCancel(TetrabeastsControlAction action)
    {
        return action == TetrabeastsControlAction.MenuCancel;
    }
}
