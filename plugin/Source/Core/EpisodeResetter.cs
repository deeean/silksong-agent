using System.Collections;
using System.Linq;
using System.Reflection;
using GlobalEnums;
using HutongGames.PlayMaker;
using UnityEngine;

namespace SilksongAgent;

public static class EpisodeResetter
{
    // Initial state storage
    private static Vector3 _heroSpawnPosition;
    private static Vector3 _bossSpawnPosition;
    private static int _bossInitialHp;
    private static bool _initialStateCaptured = false;

    public static bool IsInitialStateCaptured => _initialStateCaptured;

    // Reflection fields for private HeroController members
    private static FieldInfo _hazardRespawnRoutineField;
    private static FieldInfo _hazardInvulnRoutineField;
    private static FieldInfo _takeDamageCoroutineField;
    private static FieldInfo _doingHazardRespawnField;

    static EpisodeResetter()
    {
        var heroType = typeof(HeroController);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;

        _hazardRespawnRoutineField = heroType.GetField("hazardRespawnRoutine", flags);
        _hazardInvulnRoutineField = heroType.GetField("hazardInvulnRoutine", flags);
        _takeDamageCoroutineField = heroType.GetField("takeDamageCoroutine", flags);
        _doingHazardRespawnField = heroType.GetField("doingHazardRespawn", flags);
    }

    public static void CaptureInitialState()
    {
        if (HeroController.instance != null)
        {
            _heroSpawnPosition = new Vector3(Constants.HeroSpawnX, Constants.HeroSpawnY, 0f);
        }

        if (BossStateManager.CurrentBoss != null)
        {
            _bossSpawnPosition = BossStateManager.CurrentBoss.transform.position;
            _bossInitialHp = Constants.LaceBossMaxHealth;
        }
        else
        {
            _bossSpawnPosition = new Vector3(Constants.BossSpawnX, Constants.BossSpawnY, 0f);
            _bossInitialHp = Constants.LaceBossMaxHealth;
        }

        _initialStateCaptured = true;
    }

    public static IEnumerator SoftResetEpisode()
    {
        Plugin.IsReady = false;

        if (!CommandLineArgs.Manual)
        {
            ActionManager.IsAgentControlEnabled = true;
        }

        var hero = HeroController.instance;
        if (hero == null)
        {
            Plugin.Logger.LogWarning("SoftReset: HeroController is null, falling back to hard reset");
            yield return ResetEpisode();
            yield break;
        }

        if (!_initialStateCaptured)
        {
            Plugin.Logger.LogWarning("SoftReset: Initial state not captured, falling back to hard reset");
            yield return ResetEpisode();
            yield break;
        }

        // ===== Phase 1: Block all damage immediately =====
        hero.damageMode = DamageMode.NO_DAMAGE;
        hero.playerData.isInvincible = true;

        // ===== Phase 2: Stop all hazard-related coroutines =====
        StopHeroCoroutines(hero);

        // ===== Phase 3: Reset hero state =====
        ResetHeroState(hero);

        // ===== Phase 4: Reset boss state =====
        ResetBossState();

        // ===== Phase 5: Clear projectiles =====
        if (BossProjectileManager.Instance != null)
        {
            BossProjectileManager.Instance.ClearProjectileCache();
        }
        ClearActiveProjectiles();

        // ===== Phase 6: Wait a few frames for safety =====
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // ===== Phase 7: Restore damage mode =====
        hero.damageMode = DamageMode.FULL_DAMAGE;
        hero.playerData.isInvincible = false;
        hero.cState.invulnerable = false;

        // ===== Phase 8: Final setup =====
        BossStateManager.ResetBossPhase();

        Plugin.IsReady = true;
        GameStateCollector.ResetEpisodeTime();
        ActionManager.ResetInputs();

        StepModeManager.Instance.EnableStepMode();

        SharedMemoryManager.Instance.WriteGameState();
        SharedMemoryManager.Instance.WriteState(StateType.Reset);
    }

    private static void StopHeroCoroutines(HeroController hero)
    {
        // Stop specific hazard-related coroutines
        var hazardRespawnRoutine = _hazardRespawnRoutineField?.GetValue(hero) as Coroutine;
        if (hazardRespawnRoutine != null)
        {
            hero.StopCoroutine(hazardRespawnRoutine);
            _hazardRespawnRoutineField.SetValue(hero, null);
        }

        var hazardInvulnRoutine = _hazardInvulnRoutineField?.GetValue(hero) as Coroutine;
        if (hazardInvulnRoutine != null)
        {
            hero.StopCoroutine(hazardInvulnRoutine);
            _hazardInvulnRoutineField.SetValue(hero, null);
        }

        var takeDamageCoroutine = _takeDamageCoroutineField?.GetValue(hero) as Coroutine;
        if (takeDamageCoroutine != null)
        {
            hero.StopCoroutine(takeDamageCoroutine);
            _takeDamageCoroutineField.SetValue(hero, null);
        }

        // Reset doingHazardRespawn flag
        _doingHazardRespawnField?.SetValue(hero, false);
    }

    private static void ResetHeroState(HeroController hero)
    {
        // Reset position
        hero.transform.position = _heroSpawnPosition;

        // Reset velocity
        var rb = hero.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Reset state flags
        hero.cState.hazardDeath = false;
        hero.cState.dead = false;
        hero.cState.recoiling = false;
        hero.cState.invulnerable = false;
        hero.cState.hazardRespawning = false;
        hero.cState.transitioning = false;
        hero.cState.onGround = true;
        hero.cState.falling = false;
        hero.cState.jumping = false;
        hero.cState.dashing = false;
        hero.cState.attacking = false;

        // Reset hero_state to idle (critical for accepting input)
        hero.hero_state = ActorStates.idle;

        // Enable input accepting
        hero.acceptingInput = true;

        // Reset health and silk (MaxHealth() also updates UI properly)
        hero.MaxHealth();
        hero.playerData.silk = hero.playerData.silkRegenMax;

        // Reset player data flags
        hero.playerData.encounteredLaceTower = true;
        hero.playerData.defeatedLaceTower = false;
        hero.playerData.laceTowerDoorOpened = false;

        // Face right
        if (!hero.cState.facingRight)
        {
            hero.FaceRight();
        }

        // Reset all Player FSMs to Idle state
        ResetHeroFsms(hero);
    }

    private static void ResetHeroFsms(HeroController hero)
    {
        var allFsms = hero.GetComponents<PlayMakerFSM>();
        foreach (var fsm in allFsms)
        {
            try
            {
                // Most FSMs should go to "Idle" state
                if (fsm.FsmStates.Any(s => s.Name == "Idle"))
                {
                    fsm.SetState("Idle");
                }
            }
            catch (System.Exception) { }
        }
    }

    private static void ResetBossState()
    {
        if (BossStateManager.CurrentBoss != null)
        {
            var boss = BossStateManager.CurrentBoss;

            // Reset HP
            boss.hp = _bossInitialHp;

            // Reset position
            boss.transform.position = _bossSpawnPosition;

            // Reset velocity
            if (BossStateManager.CurrentBossRb != null)
            {
                BossStateManager.CurrentBossRb.linearVelocity = Vector2.zero;
                BossStateManager.CurrentBossRb.angularVelocity = 0f;
            }

            // Reset ALL FSMs on boss (including child objects)
            ResetAllBossFsms(boss);

            // Reset Stun Control FSM and stun damage accumulation
            ResetBossStunState(boss);
        }
        else
        {
            BossStateManager.FindBoss();
        }
    }

    private static void ResetAllBossFsms(HealthManager boss)
    {
        // Get ALL FSMs including children
        var allFsms = boss.GetComponentsInChildren<PlayMakerFSM>(true);

        foreach (var fsm in allFsms)
        {
            try
            {
                var fsmName = fsm.FsmName;

                // Reset phase-related FSM variables
                ResetPhaseFsmVariables(fsm);

                // Set appropriate state based on FSM name
                if (fsmName == "Control" || fsmName == "")
                {
                    // Main control FSM - reset to initial state
                    if (fsm.FsmStates.Any(s => s.Name == "Refight Engarde"))
                    {
                        fsm.SetState("Refight Engarde");
                    }
                    else if (fsm.FsmStates.Any(s => s.Name == "Idle"))
                    {
                        fsm.SetState("Idle");
                    }
                }
                else if (fsmName.Contains("Projectile") || fsmName.Contains("Spawn") || fsmName.Contains("Summon"))
                {
                    // Projectile-related FSMs - disable or reset to idle
                    if (fsm.FsmStates.Any(s => s.Name == "Idle"))
                    {
                        fsm.SetState("Idle");
                    }
                    else if (fsm.FsmStates.Any(s => s.Name == "Init"))
                    {
                        fsm.SetState("Init");
                    }
                }
                else if (fsmName != "Stun Control" && fsmName != "Stun")
                {
                    // Other FSMs - try to reset to Idle
                    if (fsm.FsmStates.Any(s => s.Name == "Idle"))
                    {
                        fsm.SetState("Idle");
                    }
                }
            }
            catch (System.Exception) { }
        }
    }

    private static void ResetPhaseFsmVariables(PlayMakerFSM fsm)
    {
        try
        {
            // Phase tracking variables (critical for projectile spawning)
            var didP2Shift = fsm.FsmVariables.FindFsmBool("Did P2 Shift");
            if (didP2Shift != null) didP2Shift.Value = false;

            var didP3Shift = fsm.FsmVariables.FindFsmBool("Did P3 Shift");
            if (didP3Shift != null) didP3Shift.Value = false;

            var phaseVar = fsm.FsmVariables.FindFsmInt("Phase");
            if (phaseVar != null) phaseVar.Value = 1;

            // Counter variables
            var ctCharge = fsm.FsmVariables.FindFsmInt("Ct Charge");
            if (ctCharge != null) ctCharge.Value = 0;

            var ctCombo = fsm.FsmVariables.FindFsmInt("Ct Combo");
            if (ctCombo != null) ctCombo.Value = 0;

            var ctCrossSlash = fsm.FsmVariables.FindFsmInt("Ct CrossSlash");
            if (ctCrossSlash != null) ctCrossSlash.Value = 0;

            var ctEvade = fsm.FsmVariables.FindFsmInt("Ct Evade");
            if (ctEvade != null) ctEvade.Value = 0;

            var evadeAttempts = fsm.FsmVariables.FindFsmInt("Evade Attempts");
            if (evadeAttempts != null) evadeAttempts.Value = 0;

            var hops = fsm.FsmVariables.FindFsmInt("Hops");
            if (hops != null) hops.Value = 0;

            var chargesPerformed = fsm.FsmVariables.FindFsmInt("Charges Performed");
            if (chargesPerformed != null) chargesPerformed.Value = 0;

            var ctBombSlash = fsm.FsmVariables.FindFsmInt("Ct Bomb Slash");
            if (ctBombSlash != null) ctBombSlash.Value = 0;

            // State flags
            var counterReady = fsm.FsmVariables.FindFsmBool("Counter Ready");
            if (counterReady != null) counterReady.Value = false;

            var willCounter = fsm.FsmVariables.FindFsmBool("Will Counter");
            if (willCounter != null) willCounter.Value = false;

            var willCrossSlash = fsm.FsmVariables.FindFsmBool("Will CrossSlash");
            if (willCrossSlash != null) willCrossSlash.Value = false;

            var crossSlashingHero = fsm.FsmVariables.FindFsmBool("CrossSlashing Hero");
            if (crossSlashingHero != null) crossSlashingHero.Value = false;

            var canEvade = fsm.FsmVariables.FindFsmBool("Can Evade");
            if (canEvade != null) canEvade.Value = false;

            var canBombSlash = fsm.FsmVariables.FindFsmBool("Can Bomb Slash");
            if (canBombSlash != null) canBombSlash.Value = false;

            var doPose = fsm.FsmVariables.FindFsmBool("Do Pose");
            if (doPose != null) doPose.Value = false;

            var hornetDead = fsm.FsmVariables.FindFsmBool("Hornet Dead");
            if (hornetDead != null) hornetDead.Value = false;

            // Position reset
            var selfX = fsm.FsmVariables.FindFsmFloat("Self X");
            if (selfX != null) selfX.Value = Constants.BossSpawnX;

            // Reset string variables
            var nextEvent = fsm.FsmVariables.FindFsmString("Next Event");
            if (nextEvent != null) nextEvent.Value = "";

            var anim = fsm.FsmVariables.FindFsmString("Anim");
            if (anim != null) anim.Value = "";

            var crossSlashAnim = fsm.FsmVariables.FindFsmString("Cross Slash Anim");
            if (crossSlashAnim != null) crossSlashAnim.Value = "";
        }
        catch (System.Exception) { }
    }

    private static void ResetBossStunState(HealthManager boss)
    {
        // Find Stun Control FSM on boss
        var allFsms = boss.GetComponents<PlayMakerFSM>();
        foreach (var fsm in allFsms)
        {
            if (fsm.FsmName == "Stun Control" || fsm.FsmName == "Stun")
            {
                try
                {
                    // Reset all stun-related float variables
                    var hitsTotal = fsm.FsmVariables.FindFsmFloat("Hits Total");
                    if (hitsTotal != null) hitsTotal.Value = 0f;

                    var comboCounter = fsm.FsmVariables.FindFsmFloat("Combo Counter");
                    if (comboCounter != null) comboCounter.Value = 0f;

                    var stunDamage = fsm.FsmVariables.FindFsmFloat("Stun Damage");
                    if (stunDamage != null) stunDamage.Value = 0f;

                    // Reset FSM to Idle state
                    if (fsm.FsmStates.Any(s => s.Name == "Idle"))
                    {
                        fsm.SetState("Idle");
                    }
                }
                catch (System.Exception) { }
            }
        }
    }

    private static void ClearActiveProjectiles()
    {
        // Find and deactivate all active projectiles
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj == null || !obj.activeInHierarchy) continue;

            if (obj.name.Contains("lace_circle_slash") ||
                obj.name == "Cross Slash" ||
                obj.name.Contains("projectile") ||
                obj.name.Contains("Projectile"))
            {
                obj.SetActive(false);
            }
        }
    }

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

        // Capture initial state for future soft resets
        CaptureInitialState();

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
