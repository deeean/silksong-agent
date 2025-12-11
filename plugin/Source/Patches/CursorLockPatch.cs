using HarmonyLib;
using UnityEngine;

namespace SilksongAgent.Patches;

[HarmonyPatch(typeof(Cursor))]
[HarmonyPatch("lockState", MethodType.Setter)]
public class CursorLockPatch
{
    static bool Prefix(ref CursorLockMode value)
    {
        value = CursorLockMode.None;
        Cursor.visible = true;
        return true;
    }
}
