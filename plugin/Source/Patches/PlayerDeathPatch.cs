using HarmonyLib;
using UnityEngine;

namespace SilksongAgent;

[HarmonyPatch]
public class PlayerDeathPatch
{
    [HarmonyPatch(typeof(HeroController), nameof(HeroController.Die))]
    [HarmonyPrefix]
    private static bool HeroController_Die()
    {
        return false;
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.PlayerDead))]
    [HarmonyPrefix]
    private static bool GameManager_PlayerDead()
    {
        return false;
    }
}
