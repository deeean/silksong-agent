using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace SilksongAgent.Patches;

internal static class SkipIntroPatch
{
    [HarmonyPatch(typeof(StartManager))]
    [HarmonyPatch("Start")]
    internal static class StartManager_Start_Patch
    {
        internal static void Postfix(StartManager __instance)
        {
            __instance.startManagerAnimator.speed = 9999f;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == Constants.MenuTitleScene)
        {
            GameManager.instance.StartCoroutine(ContinueGame());
        }
    }

    private static IEnumerator ContinueGame()
    {
        yield return new WaitForEndOfFrame();
        UIManager.instance.UIContinueGame(1);
    }

    private static IEnumerator WaitForSceneReady()
    {
        var consecutiveBenchFrames = 0;
        while (true)
        {
            if (HeroController.instance.playerData.atBench)
            {
                consecutiveBenchFrames++;
            }
            else
            {
                consecutiveBenchFrames = 0;
            }

            if (consecutiveBenchFrames > Constants.ConsecutiveFrameThreshold)
            {
                HeroController.instance.playerData.atBench = false;
                HeroController.instance.RegainControl();
                HeroController.instance.AcceptInput();
                HeroController.instance.StartAnimationControlToIdle();
                Plugin.Logger.LogInfo("Player taken off bench");
                break;
            }
            
            yield return new WaitForEndOfFrame();
        }
        
        var consecutiveOnGroundFrames = 0;

        while (true)
        {
            if (HeroController.instance.cState.onGround)
            {
                consecutiveOnGroundFrames++;
            }
            else
            {
                consecutiveOnGroundFrames = 0;
            }

            if (consecutiveOnGroundFrames > Constants.ConsecutiveFrameThreshold)
            {
                Plugin.Logger.LogInfo("Player is on ground, scene ready");
                break;
            }
            
            yield return new WaitForEndOfFrame();
        }

        SharedMemoryManager.Instance.WriteState(StateType.Ready);
    }

    [HarmonyPatch(typeof(GameManager))]
    [HarmonyPatch("FinishedEnteringScene")]
    internal static class GameManager_FinishedEnteringScene_Patch
    {
        internal static void Postfix()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != Constants.BossTowerScene && scene.name != Constants.MenuTitleScene)
            {
                GameManager.instance.StartCoroutine(WaitForSceneReady());
            }
        }
    }

}