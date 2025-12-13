using System.Collections;
using GlobalEnums;
using UnityEngine;

namespace SilksongAgent;

public static class EpisodeResetter
{
    public static IEnumerator ResetEpisode()
    {
        Plugin.IsReady = false;

        if (!CommandLineArgs.Manual)
        {
            ActionManager.IsAgentControlEnabled = true;
        }

        BossStateManager.ResetBoss();

        var consecutiveDoingHazardRespawnFrames = 0;
        while (true)
        {
            if (!HeroController.instance.doingHazardRespawn)
            {
                consecutiveDoingHazardRespawnFrames++;
            }
            else
            {
                consecutiveDoingHazardRespawnFrames = 0;
            }

            if (consecutiveDoingHazardRespawnFrames > Constants.ConsecutiveFrameThreshold)
            {
                break;
            }

            yield return new WaitForEndOfFrame();
        }

        HeroController.instance.playerData.encounteredLaceTower = true;
        HeroController.instance.playerData.defeatedLaceTower = false;
        HeroController.instance.playerData.laceTowerDoorOpened = false;
        HeroController.instance.playerData.health = HeroController.instance.playerData.maxHealth;
        HeroController.instance.playerData.silk = HeroController.instance.playerData.silkRegenMax;

        var sceneInfo = new GameManager.SceneLoadInfo
        {
            SceneName = Constants.BossTowerScene,
            EntryGateName = Constants.BossTowerEntryGate,
            HeroLeaveDirection = GatePosition.unknown,
            EntryDelay = 0f,
            Visualization = GameManager.SceneLoadVisualizations.Default,
            AlwaysUnloadUnusedAssets = true,
        };

        GameManager.instance.BeginSceneTransition(sceneInfo);

        var consecutiveAcceptingInputFrames = 0;
        while (true)
        {
            if (HeroController.instance.acceptingInput)
            {
                consecutiveAcceptingInputFrames++;
            }
            else
            {
                consecutiveAcceptingInputFrames = 0;
            }

            if (consecutiveAcceptingInputFrames > Constants.ConsecutiveFrameThreshold)
            {
                Plugin.Logger.LogInfo("Reset complete - player accepting input");
                break;
            }

            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForEndOfFrame();

        BossStateManager.FindBoss();

        if (BossProjectileManager.Instance != null)
        {
            BossProjectileManager.Instance.ClearProjectileCache();
        }

        BossStateManager.ResetBossPhase();

        Plugin.IsReady = true;
        GameStateCollector.ResetEpisodeTime();
        ActionManager.ResetInputs();

        StepModeManager.Instance.EnableStepMode();

        SharedMemoryManager.Instance.WriteGameState();
        SharedMemoryManager.Instance.WriteState(StateType.Reset);
    }
}
