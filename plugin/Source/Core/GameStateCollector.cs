using UnityEngine;

namespace SilksongAgent;

public static class GameStateCollector
{
    private static float episodeStartTime;

    public static void ResetEpisodeTime()
    {
        episodeStartTime = Time.time;
    }

    public static unsafe GameState CollectGameState()
    {
        var state = new GameState();

        if (HeroController.instance != null)
        {
            var player = HeroController.instance;
            var rb = player.GetComponent<Rigidbody2D>();

            state.playerPosX = player.transform.position.x;
            state.playerPosY = player.transform.position.y;
            state.playerVelX = rb != null ? rb.linearVelocity.x : 0f;
            state.playerVelY = rb != null ? rb.linearVelocity.y : 0f;

            state.playerHealth = player.playerData.health;
            state.playerMaxHealth = player.playerData.maxHealth;
            state.playerSilk = player.playerData.silk;
            state.playerGrounded = (byte)(player.cState.onGround ? 1 : 0);
            state.playerCanDash = (byte)(player.CanDash() ? 1 : 0);
            state.playerFacingRight = (byte)(player.cState.facingRight ? 1 : 0);
            state.playerInvincible = (byte)(player.playerData.isInvincible ? 1 : 0);
            state.playerCanAttack = (byte)(player.CanAttack() ? 1 : 0);

            Vector2 playerPos = new Vector2(player.transform.position.x, player.transform.position.y);
            RaycastSensor.PerformRaycast(playerPos, out float[] distances, out RaycastHitType[] hitTypes);

            for (int i = 0; i < Constants.RayCount; i++)
            {
                state.raycastDistances[i] = distances[i] / Constants.MaxRayDistance;
                state.raycastHitTypes[i] = (int)hitTypes[i];
            }

            if (player.animCtrl != null && player.animCtrl.animator != null)
            {
                var animator = player.animCtrl.animator;
                var clip = animator.CurrentClip;
                if (clip != null && clip.frames != null)
                {
                    state.playerAnimationState = (int)PlayerAnimationMapper.GetAnimationState(clip.name);
                    state.playerAnimationProgress = clip.frames.Length > 0
                        ? (float)animator.CurrentFrame / clip.frames.Length
                        : 0f;
                }
                else
                {
                    state.playerAnimationState = (int)PlayerAnimationState.Unknown;
                    state.playerAnimationProgress = 0f;
                }
            }
        }

        if (BossStateManager.CurrentBoss != null)
        {
            var boss = BossStateManager.CurrentBoss;
            var bossRb = BossStateManager.CurrentBossRb;

            state.bossPosX = boss.transform.position.x;
            state.bossPosY = boss.transform.position.y;

            state.bossVelX = bossRb != null ? bossRb.linearVelocity.x : 0f;
            state.bossVelY = bossRb != null ? bossRb.linearVelocity.y : 0f;

            state.bossHealth = boss.hp;
            state.bossMaxHealth = Constants.LaceBossMaxHealth;

            BossStateManager.UpdateBossPhase();
            state.bossPhase = BossStateManager.CurrentPhase;

            state.bossFacingRight = (byte)(boss.transform.localScale.x > 0 ? 1 : 0);

            var bossAnimator = BossStateManager.CurrentBossAnimator;
            if (bossAnimator != null)
            {
                var clip = bossAnimator.CurrentClip;
                if (clip != null && clip.frames != null)
                {
                    state.bossAnimationState = (int)BossStateManager.GetAnimationState(clip.name);
                    state.bossAnimationProgress = clip.frames.Length > 0
                        ? (float)bossAnimator.CurrentFrame / clip.frames.Length
                        : 0f;
                }
                else
                {
                    state.bossAnimationState = (int)BossAnimationState.Unknown;
                    state.bossAnimationProgress = 0f;
                }
            }
            else
            {
                state.bossAnimationState = (int)BossAnimationState.Unknown;
                state.bossAnimationProgress = 0f;
            }
        }
        else
        {
            state.bossAnimationState = (int)BossAnimationState.Idle;
            state.bossAnimationProgress = 0f;
            state.bossFacingRight = 1;
        }

        state.episodeTime = Time.time - episodeStartTime;
        state.terminated = 0;
        state.truncated = 0;

        return state;
    }
}
