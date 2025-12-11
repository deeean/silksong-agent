using HarmonyLib;
using System.Collections;
using System;
using GlobalEnums;

namespace SilksongAgent.Patches;

[HarmonyPatch]
public class FreezeMomentPatch
{
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.FreezeMoment), typeof(int))]
    [HarmonyPrefix]
    private static bool GameManager_FreezeMoment_Int(int type)
    {
        return false;
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.FreezeMoment), typeof(FreezeMomentTypes), typeof(Action))]
    [HarmonyPrefix]
    private static bool GameManager_FreezeMoment_Types(FreezeMomentTypes type, Action onFinish)
    {
        onFinish?.Invoke();
        return false;
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.FreezeMoment),
        typeof(float), typeof(float), typeof(float), typeof(float), typeof(Action))]
    [HarmonyPrefix]
    private static bool GameManager_FreezeMoment_Coroutine(
        float rampDownTime, float waitTime, float rampUpTime, float targetSpeed, Action onFinish,
        ref IEnumerator __result)
    {
        __result = EmptyCoroutine(onFinish);
        return false;
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.FreezeMomentGC))]
    [HarmonyPrefix]
    private static bool GameManager_FreezeMomentGC(
        float rampDownTime, float waitTime, float rampUpTime, float targetSpeed,
        ref IEnumerator __result)
    {
        __result = EmptyCoroutine(null);
        return false;
    }

    private static IEnumerator EmptyCoroutine(Action onFinish)
    {
        onFinish?.Invoke();
        yield break;
    }
}
