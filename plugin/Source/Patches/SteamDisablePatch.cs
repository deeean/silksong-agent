using System;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;

namespace SilksongAgent;

[HarmonyPatch]
public static class SteamDisablePatch
{
    [HarmonyPatch(typeof(SteamOnlineSubsystem), nameof(SteamOnlineSubsystem.IsPackaged))]
    [HarmonyPrefix]
    public static bool IsPackaged_Prefix(ref bool __result)
    {
        Plugin.Logger.LogInfo("Skipping Steam initialization to allow multiple instances");
        __result = false;
        return false;
    }
}
