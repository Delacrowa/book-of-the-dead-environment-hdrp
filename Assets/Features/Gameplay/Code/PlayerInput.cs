
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay {

public enum PlayerInputMapping {
    MouseAndKeyboard,
    PlayStation,
    PlayStationForWindows,
    PlayStationForMac,
    Xbox,
    XboxForWindows,
    XboxForMac,
    Vive
}

public interface IPlayerInputMapping {
    string moveX { get; }
    string moveY { get; }
    string lookX { get; }
    string lookY { get; }
    string run { get; }
    string jump { get; }
}

public class PlayerInputMappingAttribute : Attribute {
    public static IEnumerable<Type> GetTypes() {
        return
            from a in AppDomain.CurrentDomain.GetAssemblies()
            from t in a.GetTypes()
            where t.IsDefined(typeof(PlayerInputMappingAttribute), false)
            select t;
    }

    public int index;

    public PlayerInputMappingAttribute(PlayerInputMapping mapping) {
        index = (int) mapping;
    }
}

namespace PlayerInputMappings {

[PlayerInputMapping(PlayerInputMapping.MouseAndKeyboard)]
class MouseAndKeyboard : IPlayerInputMapping {
    public string moveX { get { return "Horizontal"; }}
    public string moveY { get { return "Vertical"; }}
    public string lookX { get { return "Mouse X"; }}
    public string lookY { get { return "Mouse Y"; }}
    public string run { get { return "Fire3"; }}
    public string jump { get { return "Jump"; }}
}

[PlayerInputMapping(PlayerInputMapping.PlayStation)]
class PlayStation : IPlayerInputMapping {
    public string moveX { get { return "PSLStickX"; }}
    public string moveY { get { return "PSLStickY"; }}
    public string lookX { get { return "PSRStickX"; }}
    public string lookY { get { return "PSRStickY"; }}
    public string run { get { return "PSLTrigger"; }}
    public string jump { get { return "PSCross"; }}
}

[PlayerInputMapping(PlayerInputMapping.PlayStationForWindows)]
class PlayStationForWindows : IPlayerInputMapping {
    public string moveX { get { return "PSWinLStickX"; }}
    public string moveY { get { return "PSWinLStickY"; }}
    public string lookX { get { return "PSWinRStickX"; }}
    public string lookY { get { return "PSWinRStickY"; }}
    public string run { get { return "PSWinLTrigger"; }}
    public string jump { get { return "PSWinCross"; }}
}

[PlayerInputMapping(PlayerInputMapping.PlayStationForMac)]
class PlayStationForMac : IPlayerInputMapping {
    public string moveX { get { return "PSMacLStickX"; }}
    public string moveY { get { return "PSMacLStickY"; }}
    public string lookX { get { return "PSMacRStickX"; }}
    public string lookY { get { return "PSMacRStickY"; }}
    public string run { get { return "PSMacLTrigger"; }}
    public string jump { get { return "PSMacCross"; }}
}

[PlayerInputMapping(PlayerInputMapping.Xbox)]
class Xbox : IPlayerInputMapping {
    public string moveX { get { return "XboxLStickX"; }}
    public string moveY { get { return "XboxLStickY"; }}
    public string lookX { get { return "XboxRStickX"; }}
    public string lookY { get { return "XboxRStickY"; }}
    public string run { get { return "XboxLTrigger"; }}
    public string jump { get { return "XboxButtonA"; }}
}

[PlayerInputMapping(PlayerInputMapping.XboxForWindows)]
class XboxForWindows : IPlayerInputMapping {
    public string moveX { get { return "XboxWinLStickX"; }}
    public string moveY { get { return "XboxWinLStickY"; }}
    public string lookX { get { return "XboxWinRStickX"; }}
    public string lookY { get { return "XboxWinRStickY"; }}
    public string run { get { return "XboxWinLTrigger"; }}
    public string jump { get { return "XboxWinButtonA"; }}
}

[PlayerInputMapping(PlayerInputMapping.XboxForMac)]
class XboxForMac : IPlayerInputMapping {
    public string moveX { get { return "XboxMacLStickX"; }}
    public string moveY { get { return "XboxMacLStickY"; }}
    public string lookX { get { return "XboxMacRStickX"; }}
    public string lookY { get { return "XboxMacRStickY"; }}
    public string run { get { return "XboxMacLTrigger"; }}
    public string jump { get { return "XboxMacButtonA"; }}
}

[PlayerInputMapping(PlayerInputMapping.Vive)]
class Vive : IPlayerInputMapping {
    public string moveX { get { return "ViveLThumbX"; }}
    public string moveY { get { return "ViveLThumbY"; }}
    public string lookX { get { return "ViveRThumbX"; }}
    public string lookY { get { return "ViveRThumbY"; }}
    public string run { get { return "ViveLTrigger"; }}
    public string jump { get { return "ViveLThumb"; }}
}

} // PlayerInputMappings

public static class PlayerInputMappingExtensions {
    public static PlayerInput GetKeyboardInput(this IPlayerInputMapping mapping) {
        return new PlayerInput {
            move = new Vector2(Input.GetAxis(mapping.moveX), Input.GetAxis(mapping.moveY)),
            look = new Vector2(Input.GetAxis(mapping.lookY), Input.GetAxis(mapping.lookX)),
            run = Input.GetButton(mapping.run) ? 1f : 0f,
            jump = Input.GetButtonDown(mapping.jump)
        };
    }

    public static PlayerInput GetControllerInput(this IPlayerInputMapping mapping) {
        return new PlayerInput {
            move = new Vector2(Input.GetAxis(mapping.moveX), Input.GetAxis(mapping.moveY)),
            look = new Vector2(Input.GetAxis(mapping.lookY), Input.GetAxis(mapping.lookX)),
            run = Input.GetAxis(mapping.run),
            jump = Input.GetButtonDown(mapping.jump)
        };
    }
}

public struct PlayerInput {
    static readonly IPlayerInputMapping[] _mappings;

    public static PlayerInputMapping mapping { get; private set; }
    public static bool forceMapping { get; private set; }

    public static bool ignore { get; set; }

    static PlayerInput() {
        _mappings = new IPlayerInputMapping[Enum.GetNames(typeof(PlayerInputMapping)).Length];

        foreach (var type in PlayerInputMappingAttribute.GetTypes()) {
            var mapping = Activator.CreateInstance(type) as IPlayerInputMapping;

            foreach (var attribute in type.GetCustomAttributes(typeof(PlayerInputMappingAttribute), false))
                _mappings[((PlayerInputMappingAttribute) attribute).index] = mapping;
        }
    }

    public static void SelectInputMapping(PlayerInputMapping? @override = null, bool force = false) {
        var selected = mapping;

        if (@override.HasValue)
            selected = @override.Value;
        else if (Application.platform == RuntimePlatform.PS4)
            selected = PlayerInputMapping.PlayStation;
        else if (Application.platform == RuntimePlatform.XboxOne)
            selected = PlayerInputMapping.Xbox;
        else {
            var ignoreCase = StringComparison.OrdinalIgnoreCase;
            selected = PlayerInputMapping.MouseAndKeyboard;

            foreach (var i in Input.GetJoystickNames())
                if (i.StartsWith("openvr", ignoreCase) && i.IndexOf("vive", ignoreCase) >= 0)
                    selected = PlayerInputMapping.Vive;
                else if (i.IndexOf("xbox", ignoreCase) >= 0 ||
                        i.IndexOf("360", ignoreCase) >= 0 || i.IndexOf("gpx", ignoreCase) >= 0) {
                    if (Application.platform == RuntimePlatform.WindowsPlayer ||
                            Application.platform == RuntimePlatform.WindowsEditor)
                        selected = PlayerInputMapping.XboxForWindows;
                    else if (Application.platform == RuntimePlatform.OSXPlayer ||
                            Application.platform == RuntimePlatform.OSXEditor)
                        selected = PlayerInputMapping.XboxForMac;
                } else if (i.IndexOf("sony", ignoreCase) >= 0 || i.IndexOf("wireless", ignoreCase) >= 0) {
                    if (Application.platform == RuntimePlatform.WindowsPlayer ||
                            Application.platform == RuntimePlatform.WindowsEditor)
                        selected = PlayerInputMapping.PlayStationForWindows;
                    else if (Application.platform == RuntimePlatform.OSXPlayer ||
                            Application.platform == RuntimePlatform.OSXEditor)
                        selected = PlayerInputMapping.PlayStationForMac;
                }
        }

        if (mapping != selected) {
            mapping = selected;

            Debug.Log("SetInputMapping: " + selected);
            Input.ResetInputAxes();
        }

        forceMapping = force;
    }

    public static void Update(out PlayerInput input) {
        if (!ignore) {
            if (mapping == PlayerInputMapping.MouseAndKeyboard)
                input = _mappings[(int) mapping].GetKeyboardInput();
            else
                input = _mappings[(int) mapping].GetControllerInput();
        } else
            input = default(PlayerInput);
    }

    public Vector2 look;
    public Vector2 move;
    public float zoom;
    public float run;
    public bool jump;
}

} // Gameplay

