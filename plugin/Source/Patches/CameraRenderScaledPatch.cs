using System.Reflection;
using HarmonyLib;
using TeamCherry.SharedUtils;
using UnityEngine;

namespace SilksongAgent;

[HarmonyPatch]
public static class CameraRenderScaledPatch
{
    private static bool _minimalRendering = true;
    private static FieldInfo _forceFullResolutionV2Field;

    private const int MinWidth = 16;
    private const int MinHeight = 16;

    public static bool MinimalRendering
    {
        get => _minimalRendering;
        set
        {
            _minimalRendering = value;
            if (_minimalRendering)
            {
                CameraRenderScaled.Resolution = new ScreenRes(MinWidth, MinHeight);
            }
            else
            {
                CameraRenderScaled.Resolution = new ScreenRes(0, 0);
            }
        }
    }

    [HarmonyPatch(typeof(CameraRenderScaled), "OnPreCull")]
    [HarmonyPrefix]
    public static void OnPreCull_Prefix(CameraRenderScaled __instance)
    {
        if (!CommandLineArgs.NoFx) return;

        if (_minimalRendering)
        {
            CameraRenderScaled.Resolution = new ScreenRes(MinWidth, MinHeight);
            __instance.ForceFullResolution = false;

            _forceFullResolutionV2Field ??= typeof(CameraRenderScaled).GetField(
                "forceFullResolutionV2",
                BindingFlags.NonPublic | BindingFlags.Static);
            _forceFullResolutionV2Field?.SetValue(null, false);
        }
    }
}
