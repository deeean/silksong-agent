using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System;

namespace SilksongAgent;

public class ActionManager
{
    public static bool IsLeftPressed { get; set; }
    public static bool IsRightPressed { get; set; }
    public static bool IsUpPressed { get; set; }
    public static bool IsDownPressed { get; set; }
    public static bool IsJumpPressed { get; set; }
    public static bool IsAttackPressed { get; set; }
    public static bool IsDashPressed { get; set; }
    public static bool IsClawlinePressed { get; set; }
    public static bool IsSkillPressed { get; set; }
    public static bool IsHealPressed { get; set; }

    public static bool IsAgentControlEnabled { get; set; } = false;

    public static void ResetInputs()
    {
        IsLeftPressed = false;
        IsRightPressed = false;
        IsUpPressed = false;
        IsDownPressed = false;
        IsJumpPressed = false;
        IsAttackPressed = false;
        IsDashPressed = false;
        IsClawlinePressed = false;
        IsSkillPressed = false;
        IsHealPressed = false;
    }
}

[HarmonyPatch(typeof(ButtonControl), nameof(ButtonControl.isPressed), MethodType.Getter)]
public static class ButtonControlPatch
{
    public static bool Prefix(ButtonControl __instance, ref bool __result)
    {
        if (!ActionManager.IsAgentControlEnabled)
            return true;

        string keyName = __instance.name;

        switch (keyName)
        {
            case "leftArrow":
                __result = ActionManager.IsLeftPressed;
                return false;
            case "rightArrow":
                __result = ActionManager.IsRightPressed;
                return false;
            case "upArrow":
                __result = ActionManager.IsUpPressed;
                return false;
            case "downArrow":
                __result = ActionManager.IsDownPressed;
                return false;
            case "z":
                __result = ActionManager.IsJumpPressed;
                return false;
            case "x":
                __result = ActionManager.IsAttackPressed;
                return false;
            case "c":
                __result = ActionManager.IsDashPressed;
                return false;
            case "s":
                __result = ActionManager.IsClawlinePressed;
                return false;
            case "f":
                __result = ActionManager.IsSkillPressed;
                return false;
            case "a":
                __result = ActionManager.IsHealPressed;
                return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(Input), "GetKeyDown", typeof(KeyCode))]
public static class GetKeyDownPatch
{
    public static bool Prefix(KeyCode key, ref bool __result)
    {
        if (!ActionManager.IsAgentControlEnabled)
            return true;

        return true;
    }
}

[HarmonyPatch(typeof(Input), "GetKey", typeof(KeyCode))]
public static class GetKeyPatch
{
    public static bool Prefix(KeyCode key, ref bool __result)
    {
        if (!ActionManager.IsAgentControlEnabled)
            return true;

        switch (key)
        {
            case KeyCode.LeftArrow:
                __result = ActionManager.IsLeftPressed;
                return false;
            case KeyCode.RightArrow:
                __result = ActionManager.IsRightPressed;
                return false;
            case KeyCode.UpArrow:
                __result = ActionManager.IsUpPressed;
                return false;
            case KeyCode.DownArrow:
                __result = ActionManager.IsDownPressed;
                return false;
            case KeyCode.Z:
                __result = ActionManager.IsJumpPressed;
                return false;
            case KeyCode.X:
                __result = ActionManager.IsAttackPressed;
                return false;
            case KeyCode.C:
                __result = ActionManager.IsDashPressed;
                return false;
            case KeyCode.S:
                __result = ActionManager.IsClawlinePressed;
                return false;
            case KeyCode.F:
                __result = ActionManager.IsSkillPressed;
                return false;
            case KeyCode.A:
                __result = ActionManager.IsHealPressed;
                return false;
        }

        return true;
    }
}
