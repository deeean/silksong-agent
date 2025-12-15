using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace SilksongAgent;

public static class EpisodeResetter
{
    private static Vector3 _heroSpawnPosition;
    private static Vector3 _bossSpawnPosition;
    private static int _bossInitialHp;
    private static bool _initialStateCaptured = false;

    public static bool IsInitialStateCaptured => _initialStateCaptured;

    private static FieldInfo _hazardRespawnRoutineField;
    private static FieldInfo _hazardInvulnRoutineField;
    private static FieldInfo _takeDamageCoroutineField;
    private static FieldInfo _doingHazardRespawnField;
    private static FieldInfo _recoilRoutineField;
    private static FieldInfo _tilemapTestCoroutineField;
    private static FieldInfo _frostedFadeOutRoutineField;
    private static FieldInfo _cocoonFloatRoutineField;
    private static FieldInfo _attackTimeField;
    private static FieldInfo _dashTimerField;
    private static FieldInfo _dashCooldownTimerField;
    private static FieldInfo _nailChargeTimerField;
    private static FieldInfo _preventCastByDialogueEndTimerField;
    private static FieldInfo _airDashedField;
    private static FieldInfo _bounceTimerField;
    private static FieldInfo _recoilTimerField;
    private static FieldInfo _shadowDashTimerField;
    private static FieldInfo _jumpQueuingField;
    private static FieldInfo _doubleJumpQueuingField;
    private static FieldInfo _attackQueuingField;
    private static FieldInfo _dashQueuingField;
    private static FieldInfo _jumpReleaseQueuingField;
    private static FieldInfo _harpoonQueuingField;
    private static FieldInfo _toolThrowQueueingField;
    private static FieldInfo _wallSlidingLField;
    private static FieldInfo _wallSlidingRField;
    private static FieldInfo _doubleJumpedField;
    private static FieldInfo _wallJumpedLField;
    private static FieldInfo _wallJumpedRField;
    private static FieldInfo _hardLandedField;
    private static FieldInfo _didAirHangField;

    private static FieldInfo _attackCooldownField;
    private static FieldInfo _attackDurationField;
    private static FieldInfo _recoilStepsLeftField;
    private static FieldInfo _recoilVelocityField;
    private static FieldInfo _jumpStepsField;
    private static FieldInfo _jumpedStepsField;
    private static FieldInfo _doubleJumpStepsField;
    private static FieldInfo _wallLockStepsField;
    private static FieldInfo _wallJumpChainStepsLeftField;
    private static FieldInfo _currentWalljumpSpeedField;
    private static FieldInfo _walljumpSpeedDecelField;
    private static FieldInfo _wallUnstickStepsField;
    private static FieldInfo _landingBufferStepsField;
    private static FieldInfo _dashQueueStepsField;
    private static FieldInfo _jumpQueueStepsField;
    private static FieldInfo _doubleJumpQueueStepsField;
    private static FieldInfo _jumpReleaseQueueStepsField;
    private static FieldInfo _attackQueueStepsField;
    private static FieldInfo _harpoonQueueStepsField;
    private static FieldInfo _toolThrowQueueStepsField;
    private static FieldInfo _hardLandingTimerField;
    private static FieldInfo _dashLandingTimerField;
    private static FieldInfo _lookDelayTimerField;
    private static FieldInfo _wallslideClipTimerField;
    private static FieldInfo _hardLandFailSafeTimerField;
    private static FieldInfo _hazardDeathTimerField;
    private static FieldInfo _floatingBufferTimerField;
    private static FieldInfo _wallStickTimerField;
    private static FieldInfo _wallClingCooldownTimerField;
    private static FieldInfo _ledgeBufferStepsField;
    private static FieldInfo _headBumpStepsField;
    private static FieldInfo _softLandTimeField;
    private static FieldInfo _canSoftLandField;
    private static FieldInfo _fallRumbleField;
    private static FieldInfo _fallCheckFlaggedField;
    private static FieldInfo _wallSlashingField;
    private static FieldInfo _evadingDidClashField;
    private static FieldInfo _currentGravityField;
    private static FieldInfo _prevGravityScaleField;

    private static FieldInfo _startWithWallslideField;
    private static FieldInfo _startWithJumpField;
    private static FieldInfo _startWithAnyJumpField;
    private static FieldInfo _startWithTinyJumpField;
    private static FieldInfo _startWithShuttlecockField;
    private static FieldInfo _startWithFullJumpField;
    private static FieldInfo _startWithFlipJumpField;
    private static FieldInfo _startWithBackflipJumpField;
    private static FieldInfo _startWithBrollyField;
    private static FieldInfo _startWithDoubleJumpField;
    private static FieldInfo _startWithWallsprintLaunchField;
    private static FieldInfo _startWithDashField;
    private static FieldInfo _dashCurrentFacingField;
    private static FieldInfo _startWithDownSpikeBounceField;
    private static FieldInfo _startWithDownSpikeBounceSlightlyShortField;
    private static FieldInfo _startWithDownSpikeBounceShortField;
    private static FieldInfo _startWithDownSpikeEndField;
    private static FieldInfo _startWithHarpoonBounceField;
    private static FieldInfo _startWithWitchSprintBounceField;
    private static FieldInfo _startWithBalloonBounceField;
    private static FieldInfo _startWithUpdraftExitField;
    private static FieldInfo _startWithScrambleLeapField;
    private static FieldInfo _startWithRecoilBackField;
    private static FieldInfo _startWithRecoilBackLongField;
    private static FieldInfo _startWithWhipPullRecoilField;
    private static FieldInfo _startWithAttackField;
    private static FieldInfo _startWithToolThrowField;
    private static FieldInfo _startWithWallJumpField;

    private static FieldInfo _evasionByHitRemainingField;
    private static FieldInfo _rapidBulletTimerField;
    private static FieldInfo _rapidBulletCountField;
    private static FieldInfo _rapidBombTimerField;
    private static FieldInfo _rapidBombCountField;
    private static FieldInfo _rapidStormTimerField;
    private static FieldInfo _rapidStormCountField;
    private static FieldInfo _hasTakenDamageField;
    private static FieldInfo _invincibleField;
    private static FieldInfo _invincibleFromDirectionField;
    private static FieldInfo _directionOfLastAttackField;
    private static FieldInfo _notifiedBattleSceneField;

    private static FieldInfo _recoilStateField;
    private static FieldInfo _recoilTimeRemainingField;
    private static FieldInfo _recoilSpeedField;
    private static FieldInfo _isRecoilSweepingField;
    private static FieldInfo _previousRecoilAngleField;

    private static FieldInfo _preventClashTinkField;
    private static FieldInfo _damageAllowedTimeField;
    private static FieldInfo _nailClashRoutineField;
    private static FieldInfo _cancelAttackField;

    private static FieldInfo _singleFlashRoutineField;
    private static FieldInfo _flashChangedField;
    private static FieldInfo _geoFlashField;
    private static FieldInfo _geoTimerField;

    private static FieldInfo _invulnerablePulseFlashRoutineField;

    private static FieldInfo _alertRangeUnalertTimerField;
    private static FieldInfo _alertRangeIsHeroInRangeField;
    private static FieldInfo _alertRangeHaveLineOfSightField;
    private static FieldInfo _alertRangeHasHeroField;

    private static FieldInfo _enemyDeathEffectsDidFireField;
    private static FieldInfo _enemyDeathEffectsIsBlackThreadedField;

    private static FieldInfo _randomEventLastEventIndexField;
    private static FieldInfo _arrayGetRandomLastIndexField;
    private static FieldInfo _playRandomSoundLastIndexField;
    private static FieldInfo _randomIntLastIndexField;

    static EpisodeResetter()
    {
        var heroType = typeof(HeroController);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;

        _hazardRespawnRoutineField = heroType.GetField("hazardRespawnRoutine", flags);
        _hazardInvulnRoutineField = heroType.GetField("hazardInvulnRoutine", flags);
        _takeDamageCoroutineField = heroType.GetField("takeDamageCoroutine", flags);
        _doingHazardRespawnField = heroType.GetField("doingHazardRespawn", flags);
        _recoilRoutineField = heroType.GetField("recoilRoutine", flags);
        _tilemapTestCoroutineField = heroType.GetField("tilemapTestCoroutine", flags);
        _frostedFadeOutRoutineField = heroType.GetField("frostedFadeOutRoutine", flags);
        _cocoonFloatRoutineField = heroType.GetField("cocoonFloatRoutine", flags);
        _attackTimeField = heroType.GetField("attack_time", flags);
        _dashTimerField = heroType.GetField("dash_timer", flags);
        _dashCooldownTimerField = heroType.GetField("dashCooldownTimer", flags);
        _nailChargeTimerField = heroType.GetField("nailChargeTimer", flags);
        _preventCastByDialogueEndTimerField = heroType.GetField("preventCastByDialogueEndTimer", flags);
        _airDashedField = heroType.GetField("airDashed", flags);
        _bounceTimerField = heroType.GetField("bounceTimer", flags);
        _recoilTimerField = heroType.GetField("recoilTimer", flags);
        _shadowDashTimerField = heroType.GetField("shadowDashTimer", flags);
        _jumpQueuingField = heroType.GetField("jumpQueuing", flags);
        _doubleJumpQueuingField = heroType.GetField("doubleJumpQueuing", flags);
        _attackQueuingField = heroType.GetField("attackQueuing", flags);
        _dashQueuingField = heroType.GetField("dashQueuing", flags);
        _jumpReleaseQueuingField = heroType.GetField("jumpReleaseQueuing", flags);
        _harpoonQueuingField = heroType.GetField("harpoonQueuing", flags);
        _toolThrowQueueingField = heroType.GetField("toolThrowQueueing", flags);
        _wallSlidingLField = heroType.GetField("wallSlidingL", flags);
        _wallSlidingRField = heroType.GetField("wallSlidingR", flags);
        _doubleJumpedField = heroType.GetField("doubleJumped", flags);
        _wallJumpedLField = heroType.GetField("wallJumpedL", flags);
        _wallJumpedRField = heroType.GetField("wallJumpedR", flags);
        _hardLandedField = heroType.GetField("hardLanded", flags);
        _didAirHangField = heroType.GetField("didAirHang", flags);

        _attackCooldownField = heroType.GetField("attack_cooldown", flags);
        _attackDurationField = heroType.GetField("attackDuration", flags);
        _recoilStepsLeftField = heroType.GetField("recoilStepsLeft", flags);
        _recoilVelocityField = heroType.GetField("recoilVelocity", flags);
        _jumpStepsField = heroType.GetField("jump_steps", flags);
        _jumpedStepsField = heroType.GetField("jumped_steps", flags);
        _doubleJumpStepsField = heroType.GetField("doubleJump_steps", flags);
        _wallLockStepsField = heroType.GetField("wallLockSteps", flags);
        _wallJumpChainStepsLeftField = heroType.GetField("wallJumpChainStepsLeft", flags);
        _currentWalljumpSpeedField = heroType.GetField("currentWalljumpSpeed", flags);
        _walljumpSpeedDecelField = heroType.GetField("walljumpSpeedDecel", flags);
        _wallUnstickStepsField = heroType.GetField("wallUnstickSteps", flags);
        _landingBufferStepsField = heroType.GetField("landingBufferSteps", flags);
        _dashQueueStepsField = heroType.GetField("dashQueueSteps", flags);
        _jumpQueueStepsField = heroType.GetField("jumpQueueSteps", flags);
        _doubleJumpQueueStepsField = heroType.GetField("doubleJumpQueueSteps", flags);
        _jumpReleaseQueueStepsField = heroType.GetField("jumpReleaseQueueSteps", flags);
        _attackQueueStepsField = heroType.GetField("attackQueueSteps", flags);
        _harpoonQueueStepsField = heroType.GetField("harpoonQueueSteps", flags);
        _toolThrowQueueStepsField = heroType.GetField("toolThrowQueueSteps", flags);
        _hardLandingTimerField = heroType.GetField("hardLandingTimer", flags);
        _dashLandingTimerField = heroType.GetField("dashLandingTimer", flags);
        _lookDelayTimerField = heroType.GetField("lookDelayTimer", flags);
        _wallslideClipTimerField = heroType.GetField("wallslideClipTimer", flags);
        _hardLandFailSafeTimerField = heroType.GetField("hardLandFailSafeTimer", flags);
        _hazardDeathTimerField = heroType.GetField("hazardDeathTimer", flags);
        _floatingBufferTimerField = heroType.GetField("floatingBufferTimer", flags);
        _wallStickTimerField = heroType.GetField("wallStickTimer", flags);
        _wallClingCooldownTimerField = heroType.GetField("wallClingCooldownTimer", flags);
        _ledgeBufferStepsField = heroType.GetField("ledgeBufferSteps", flags);
        _headBumpStepsField = heroType.GetField("headBumpSteps", flags);
        _softLandTimeField = heroType.GetField("softLandTime", flags);
        _canSoftLandField = heroType.GetField("canSoftLand", flags);
        _fallRumbleField = heroType.GetField("fallRumble", flags);
        _fallCheckFlaggedField = heroType.GetField("fallCheckFlagged", flags);
        _wallSlashingField = heroType.GetField("wallSlashing", flags);
        _evadingDidClashField = heroType.GetField("evadingDidClash", flags);
        _currentGravityField = heroType.GetField("currentGravity", flags);
        _prevGravityScaleField = heroType.GetField("prevGravityScale", flags);

        _startWithWallslideField = heroType.GetField("startWithWallslide", flags);
        _startWithJumpField = heroType.GetField("startWithJump", flags);
        _startWithAnyJumpField = heroType.GetField("startWithAnyJump", flags);
        _startWithTinyJumpField = heroType.GetField("startWithTinyJump", flags);
        _startWithShuttlecockField = heroType.GetField("startWithShuttlecock", flags);
        _startWithFullJumpField = heroType.GetField("startWithFullJump", flags);
        _startWithFlipJumpField = heroType.GetField("startWithFlipJump", flags);
        _startWithBackflipJumpField = heroType.GetField("startWithBackflipJump", flags);
        _startWithBrollyField = heroType.GetField("startWithBrolly", flags);
        _startWithDoubleJumpField = heroType.GetField("startWithDoubleJump", flags);
        _startWithWallsprintLaunchField = heroType.GetField("startWithWallsprintLaunch", flags);
        _startWithDashField = heroType.GetField("startWithDash", flags);
        _dashCurrentFacingField = heroType.GetField("dashCurrentFacing", flags);
        _startWithDownSpikeBounceField = heroType.GetField("startWithDownSpikeBounce", flags);
        _startWithDownSpikeBounceSlightlyShortField = heroType.GetField("startWithDownSpikeBounceSlightlyShort", flags);
        _startWithDownSpikeBounceShortField = heroType.GetField("startWithDownSpikeBounceShort", flags);
        _startWithDownSpikeEndField = heroType.GetField("startWithDownSpikeEnd", flags);
        _startWithHarpoonBounceField = heroType.GetField("startWithHarpoonBounce", flags);
        _startWithWitchSprintBounceField = heroType.GetField("startWithWitchSprintBounce", flags);
        _startWithBalloonBounceField = heroType.GetField("startWithBalloonBounce", flags);
        _startWithUpdraftExitField = heroType.GetField("startWithUpdraftExit", flags);
        _startWithScrambleLeapField = heroType.GetField("startWithScrambleLeap", flags);
        _startWithRecoilBackField = heroType.GetField("startWithRecoilBack", flags);
        _startWithRecoilBackLongField = heroType.GetField("startWithRecoilBackLong", flags);
        _startWithWhipPullRecoilField = heroType.GetField("startWithWhipPullRecoil", flags);
        _startWithAttackField = heroType.GetField("startWithAttack", flags);
        _startWithToolThrowField = heroType.GetField("startWithToolThrow", flags);
        _startWithWallJumpField = heroType.GetField("startWithWallJump", flags);

        var healthManagerType = typeof(HealthManager);
        _evasionByHitRemainingField = healthManagerType.GetField("evasionByHitRemaining", flags);
        _rapidBulletTimerField = healthManagerType.GetField("rapidBulletTimer", flags);
        _rapidBulletCountField = healthManagerType.GetField("rapidBulletCount", flags);
        _rapidBombTimerField = healthManagerType.GetField("rapidBombTimer", flags);
        _rapidBombCountField = healthManagerType.GetField("rapidBombCount", flags);
        _rapidStormTimerField = healthManagerType.GetField("rapidStormTimer", flags);
        _rapidStormCountField = healthManagerType.GetField("rapidStormCount", flags);
        _hasTakenDamageField = healthManagerType.GetField("hasTakenDamage", flags);
        _invincibleField = healthManagerType.GetField("invincible", flags);
        _invincibleFromDirectionField = healthManagerType.GetField("invincibleFromDirection", flags);
        _directionOfLastAttackField = healthManagerType.GetField("directionOfLastAttack", flags);
        _notifiedBattleSceneField = healthManagerType.GetField("notifiedBattleScene", flags);

        var recoilType = typeof(Recoil);
        _recoilStateField = recoilType.GetField("state", flags);
        _recoilTimeRemainingField = recoilType.GetField("recoilTimeRemaining", flags);
        _recoilSpeedField = recoilType.GetField("recoilSpeed", flags);
        _isRecoilSweepingField = recoilType.GetField("isRecoilSweeping", flags);
        _previousRecoilAngleField = recoilType.GetField("previousRecoilAngle", flags);

        var damageHeroType = typeof(DamageHero);
        _preventClashTinkField = damageHeroType.GetField("preventClashTink", flags);
        _damageAllowedTimeField = damageHeroType.GetField("damageAllowedTime", flags);
        _nailClashRoutineField = damageHeroType.GetField("nailClashRoutine", flags);
        _cancelAttackField = damageHeroType.GetField("cancelAttack", flags);

        var spriteFlashType = typeof(SpriteFlash);
        _singleFlashRoutineField = spriteFlashType.GetField("singleFlashRoutine", flags);
        _flashChangedField = spriteFlashType.GetField("flashChanged", flags);
        _geoFlashField = spriteFlashType.GetField("geoFlash", flags);
        _geoTimerField = spriteFlashType.GetField("geoTimer", flags);

        var invulnerablePulseType = typeof(InvulnerablePulse);
        _invulnerablePulseFlashRoutineField = invulnerablePulseType.GetField("flashRoutine", flags);

        var alertRangeType = typeof(AlertRange);
        _alertRangeUnalertTimerField = alertRangeType.GetField("unalertTimer", flags);
        _alertRangeIsHeroInRangeField = alertRangeType.GetField("isHeroInRange", flags);
        _alertRangeHaveLineOfSightField = alertRangeType.GetField("haveLineOfSight", flags);
        _alertRangeHasHeroField = alertRangeType.GetField("hasHero", flags);

        var enemyDeathEffectsType = typeof(EnemyDeathEffects);
        _enemyDeathEffectsDidFireField = enemyDeathEffectsType.GetField("didFire", flags);
        _enemyDeathEffectsIsBlackThreadedField = enemyDeathEffectsType.GetField("isBlackThreaded", flags);

        var randomEventType = typeof(RandomEvent);
        _randomEventLastEventIndexField = randomEventType.GetField("lastEventIndex", flags);

        var arrayGetRandomType = typeof(ArrayGetRandom);
        _arrayGetRandomLastIndexField = arrayGetRandomType.GetField("lastIndex", flags);

        var playRandomSoundType = typeof(PlayRandomSound);
        _playRandomSoundLastIndexField = playRandomSoundType.GetField("lastIndex", flags);

        var randomIntType = typeof(RandomInt);
        _randomIntLastIndexField = randomIntType.GetField("lastIndex", flags);
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

        hero.damageMode = DamageMode.NO_DAMAGE;
        hero.playerData.isInvincible = true;

        StopHeroCoroutines(hero);

        ResetHeroState(hero);

        ResetBossState();

        if (BossProjectileManager.Instance != null)
        {
            BossProjectileManager.Instance.ClearProjectileCache();
        }
        ClearActiveProjectiles();

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        hero.damageMode = DamageMode.FULL_DAMAGE;
        hero.playerData.isInvincible = false;
        hero.cState.invulnerable = false;

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

        var recoilRoutine = _recoilRoutineField?.GetValue(hero) as Coroutine;
        if (recoilRoutine != null)
        {
            hero.StopCoroutine(recoilRoutine);
            _recoilRoutineField.SetValue(hero, null);
        }

        var tilemapTestCoroutine = _tilemapTestCoroutineField?.GetValue(hero) as Coroutine;
        if (tilemapTestCoroutine != null)
        {
            hero.StopCoroutine(tilemapTestCoroutine);
            _tilemapTestCoroutineField.SetValue(hero, null);
        }

        var frostedFadeOutRoutine = _frostedFadeOutRoutineField?.GetValue(hero) as Coroutine;
        if (frostedFadeOutRoutine != null)
        {
            hero.StopCoroutine(frostedFadeOutRoutine);
            _frostedFadeOutRoutineField.SetValue(hero, null);
        }

        var cocoonFloatRoutine = _cocoonFloatRoutineField?.GetValue(hero) as Coroutine;
        if (cocoonFloatRoutine != null)
        {
            hero.StopCoroutine(cocoonFloatRoutine);
            _cocoonFloatRoutineField.SetValue(hero, null);
        }

        _doingHazardRespawnField?.SetValue(hero, false);
    }

    private static void ResetHeroState(HeroController hero)
    {
        hero.transform.position = _heroSpawnPosition;

        var rb = hero.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = hero.DEFAULT_GRAVITY;
        }

        hero.cState.hazardDeath = false;
        hero.cState.dead = false;
        hero.cState.recoiling = false;
        hero.cState.recoilingLeft = false;
        hero.cState.recoilingRight = false;
        hero.cState.recoilingDrill = false;
        hero.cState.recoilFrozen = false;
        hero.cState.invulnerable = false;
        hero.cState.hazardRespawning = false;
        hero.cState.transitioning = false;
        hero.cState.onGround = true;
        hero.cState.falling = false;
        hero.cState.jumping = false;
        hero.cState.doubleJumping = false;
        hero.cState.dashing = false;
        hero.cState.backDashing = false;
        hero.cState.attacking = false;
        hero.cState.preventDash = false;
        hero.cState.dashCooldown = false;
        hero.cState.nailCharging = false;
        hero.cState.parrying = false;
        hero.cState.wallSliding = false;
        hero.cState.wallClinging = false;
        hero.cState.bouncing = false;
        hero.cState.shroomBouncing = false;
        hero.cState.downSpiking = false;
        hero.cState.downSpikeAntic = false;
        hero.cState.downSpikeBouncing = false;
        hero.cState.downSpikeRecovery = false;
        hero.cState.casting = false;
        hero.cState.castRecoiling = false;
        hero.cState.evading = false;
        hero.cState.floating = false;
        hero.cState.shuttleCock = false;
        hero.cState.lookingUp = false;
        hero.cState.lookingUpAnim = false;
        hero.cState.lookingDown = false;
        hero.cState.lookingDownAnim = false;
        hero.cState.touchingWall = false;
        hero.cState.isSprinting = false;
        hero.cState.isBackSprinting = false;
        hero.cState.isBackScuttling = false;

        hero.cState.whipLashing = false;
        hero.cState.isTriggerEventsPaused = false;
        hero.cState.inConveyorZone = false;
        hero.cState.onConveyor = false;
        hero.cState.onConveyorV = false;
        hero.cState.isTouchingSlopeLeft = false;
        hero.cState.isTouchingSlopeRight = false;
        hero.cState.isToolThrowing = false;
        hero.cState.altAttack = false;
        hero.cState.upAttacking = false;
        hero.cState.downAttacking = false;
        hero.cState.mantleRecovery = false;
        hero.cState.swimming = false;
        hero.cState.parryAttack = false;
        hero.cState.wallJumping = false;
        hero.cState.willHardLand = false;
        hero.cState.inUpdraft = false;
        hero.cState.fakeHurt = false;
        hero.cState.downSpikeBouncingShort = false;

        hero.controlReqlinquished = false;

        _attackTimeField?.SetValue(hero, 0f);
        _dashTimerField?.SetValue(hero, 0f);
        _dashCooldownTimerField?.SetValue(hero, 0f);
        _nailChargeTimerField?.SetValue(hero, 0f);
        _preventCastByDialogueEndTimerField?.SetValue(hero, 0f);
        _airDashedField?.SetValue(hero, false);
        _bounceTimerField?.SetValue(hero, 0f);
        _recoilTimerField?.SetValue(hero, 0f);
        _shadowDashTimerField?.SetValue(hero, 0f);
        hero.parryInvulnTimer = 0f;

        _jumpQueuingField?.SetValue(hero, false);
        _doubleJumpQueuingField?.SetValue(hero, false);
        _attackQueuingField?.SetValue(hero, false);
        _dashQueuingField?.SetValue(hero, false);
        _jumpReleaseQueuingField?.SetValue(hero, false);
        _harpoonQueuingField?.SetValue(hero, false);
        _toolThrowQueueingField?.SetValue(hero, false);

        _jumpQueueStepsField?.SetValue(hero, 0);
        _doubleJumpQueueStepsField?.SetValue(hero, 0);
        _jumpReleaseQueueStepsField?.SetValue(hero, 0);
        _attackQueueStepsField?.SetValue(hero, 0);
        _harpoonQueueStepsField?.SetValue(hero, 0);
        _toolThrowQueueStepsField?.SetValue(hero, 0);
        _dashQueueStepsField?.SetValue(hero, 0);

        _wallSlidingLField?.SetValue(hero, false);
        _wallSlidingRField?.SetValue(hero, false);
        _wallSlashingField?.SetValue(hero, false);
        _wallLockStepsField?.SetValue(hero, 0);
        _wallJumpChainStepsLeftField?.SetValue(hero, 0);
        _currentWalljumpSpeedField?.SetValue(hero, 0f);
        _walljumpSpeedDecelField?.SetValue(hero, 0f);
        _wallUnstickStepsField?.SetValue(hero, 0);
        _wallslideClipTimerField?.SetValue(hero, 0f);
        _wallStickTimerField?.SetValue(hero, 0f);
        _wallClingCooldownTimerField?.SetValue(hero, 0f);
        hero.wallLocked = false;
        hero.touchingWallL = false;
        hero.touchingWallR = false;

        _doubleJumpedField?.SetValue(hero, false);
        _wallJumpedLField?.SetValue(hero, false);
        _wallJumpedRField?.SetValue(hero, false);
        _jumpStepsField?.SetValue(hero, 0);
        _jumpedStepsField?.SetValue(hero, 0);
        _doubleJumpStepsField?.SetValue(hero, 0);

        _attackCooldownField?.SetValue(hero, 0f);
        _attackDurationField?.SetValue(hero, 0f);

        _recoilStepsLeftField?.SetValue(hero, 0);
        _recoilVelocityField?.SetValue(hero, 0f);

        _hardLandedField?.SetValue(hero, false);
        _hardLandingTimerField?.SetValue(hero, 0f);
        _dashLandingTimerField?.SetValue(hero, 0f);
        _hardLandFailSafeTimerField?.SetValue(hero, 0f);
        _landingBufferStepsField?.SetValue(hero, 0);
        _ledgeBufferStepsField?.SetValue(hero, 0);
        _softLandTimeField?.SetValue(hero, 0);
        _canSoftLandField?.SetValue(hero, false);
        _fallRumbleField?.SetValue(hero, false);
        _fallCheckFlaggedField?.SetValue(hero, false);

        _lookDelayTimerField?.SetValue(hero, 0f);
        _hazardDeathTimerField?.SetValue(hero, 0f);
        _floatingBufferTimerField?.SetValue(hero, 0f);
        _headBumpStepsField?.SetValue(hero, 0);

        _didAirHangField?.SetValue(hero, false);
        _evadingDidClashField?.SetValue(hero, false);
        hero.dashingDown = false;

        _currentGravityField?.SetValue(hero, hero.DEFAULT_GRAVITY);
        _prevGravityScaleField?.SetValue(hero, hero.DEFAULT_GRAVITY);

        _startWithWallslideField?.SetValue(hero, false);
        _startWithJumpField?.SetValue(hero, false);
        _startWithAnyJumpField?.SetValue(hero, false);
        _startWithTinyJumpField?.SetValue(hero, false);
        _startWithShuttlecockField?.SetValue(hero, false);
        _startWithFullJumpField?.SetValue(hero, false);
        _startWithFlipJumpField?.SetValue(hero, false);
        _startWithBackflipJumpField?.SetValue(hero, false);
        _startWithBrollyField?.SetValue(hero, false);
        _startWithDoubleJumpField?.SetValue(hero, false);
        _startWithWallsprintLaunchField?.SetValue(hero, false);
        _startWithDashField?.SetValue(hero, false);
        _dashCurrentFacingField?.SetValue(hero, false);
        _startWithDownSpikeBounceField?.SetValue(hero, false);
        _startWithDownSpikeBounceSlightlyShortField?.SetValue(hero, false);
        _startWithDownSpikeBounceShortField?.SetValue(hero, false);
        _startWithDownSpikeEndField?.SetValue(hero, false);
        _startWithHarpoonBounceField?.SetValue(hero, false);
        _startWithWitchSprintBounceField?.SetValue(hero, false);
        _startWithBalloonBounceField?.SetValue(hero, false);
        _startWithUpdraftExitField?.SetValue(hero, false);
        _startWithScrambleLeapField?.SetValue(hero, false);
        _startWithRecoilBackField?.SetValue(hero, false);
        _startWithRecoilBackLongField?.SetValue(hero, false);
        _startWithWhipPullRecoilField?.SetValue(hero, false);
        _startWithAttackField?.SetValue(hero, false);
        _startWithToolThrowField?.SetValue(hero, false);
        _startWithWallJumpField?.SetValue(hero, false);

        hero.hero_state = ActorStates.idle;

        hero.acceptingInput = true;

        hero.MaxHealth();
        hero.playerData.silk = hero.playerData.silkRegenMax;

        hero.playerData.encounteredLaceTower = true;
        hero.playerData.defeatedLaceTower = false;
        hero.playerData.laceTowerDoorOpened = false;

        if (!hero.cState.facingRight)
        {
            hero.FaceRight();
        }

        ResetHeroFsms(hero);
    }

    private static void ResetHeroFsms(HeroController hero)
    {
        var allFsms = hero.GetComponents<PlayMakerFSM>();
        foreach (var fsm in allFsms)
        {
            try
            {
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

            boss.hp = _bossInitialHp;

            boss.transform.position = _bossSpawnPosition;

            boss.transform.localScale = new Vector3(-1f, 1f, 1f);

            if (BossStateManager.CurrentBossRb != null)
            {
                BossStateManager.CurrentBossRb.linearVelocity = Vector2.zero;
                BossStateManager.CurrentBossRb.angularVelocity = 0f;
            }

            ResetHealthManagerTimers(boss);

            ResetAllBossFsms(boss);

            ResetBossStunState(boss);

            ResetBossRecoil(boss);

            ResetBossDamageHero(boss);

            ResetBossSpriteFlash(boss);

            ResetBossInvulnerablePulse(boss);

            ResetBossAlertRange(boss);

            ResetBossDeathEffects(boss);
        }
        else
        {
            BossStateManager.FindBoss();
        }
    }

    private static void ResetBossSpriteFlash(HealthManager boss)
    {
        var spriteFlashes = boss.GetComponentsInChildren<SpriteFlash>(true);
        foreach (var spriteFlash in spriteFlashes)
        {
            var flashRoutine = _singleFlashRoutineField?.GetValue(spriteFlash) as Coroutine;
            if (flashRoutine != null)
            {
                spriteFlash.StopCoroutine(flashRoutine);
                _singleFlashRoutineField.SetValue(spriteFlash, null);
            }

            _flashChangedField?.SetValue(spriteFlash, false);
            _geoFlashField?.SetValue(spriteFlash, false);
            _geoTimerField?.SetValue(spriteFlash, 0f);

            spriteFlash.CancelFlash();
        }
    }

    private static void ResetBossInvulnerablePulse(HealthManager boss)
    {
        var invulnerablePulses = boss.GetComponentsInChildren<InvulnerablePulse>(true);
        foreach (var pulse in invulnerablePulses)
        {
            pulse.StopInvulnerablePulse();
        }
    }

    private static void ResetBossAlertRange(HealthManager boss)
    {
        var alertRanges = boss.GetComponentsInChildren<AlertRange>(true);
        foreach (var alertRange in alertRanges)
        {
            _alertRangeUnalertTimerField?.SetValue(alertRange, 0f);
            _alertRangeIsHeroInRangeField?.SetValue(alertRange, false);
            _alertRangeHaveLineOfSightField?.SetValue(alertRange, false);
            _alertRangeHasHeroField?.SetValue(alertRange, false);
        }
    }

    private static void ResetBossDeathEffects(HealthManager boss)
    {
        var deathEffects = boss.GetComponentsInChildren<EnemyDeathEffects>(true);
        foreach (var deathEffect in deathEffects)
        {
            _enemyDeathEffectsDidFireField?.SetValue(deathEffect, false);
            _enemyDeathEffectsIsBlackThreadedField?.SetValue(deathEffect, false);
        }
    }

    private static void ResetBossDamageHero(HealthManager boss)
    {
        var damageHeroes = boss.GetComponentsInChildren<DamageHero>(true);
        foreach (var damageHero in damageHeroes)
        {
            var nailClashRoutine = _nailClashRoutineField?.GetValue(damageHero) as Coroutine;
            if (nailClashRoutine != null)
            {
                damageHero.StopCoroutine(nailClashRoutine);
                _nailClashRoutineField.SetValue(damageHero, null);
            }

            _preventClashTinkField?.SetValue(damageHero, false);
            _damageAllowedTimeField?.SetValue(damageHero, 0.0);
            _cancelAttackField?.SetValue(damageHero, false);
        }
    }

    private static void ResetBossRecoil(HealthManager boss)
    {
        var recoil = boss.GetComponent<Recoil>();
        if (recoil != null)
        {
            _recoilStateField?.SetValue(recoil, 0);
            _recoilTimeRemainingField?.SetValue(recoil, 0f);
            _recoilSpeedField?.SetValue(recoil, 0f);
            _isRecoilSweepingField?.SetValue(recoil, false);
            _previousRecoilAngleField?.SetValue(recoil, 0f);
        }
    }

    private static void ResetHealthManagerTimers(HealthManager boss)
    {
        _evasionByHitRemainingField?.SetValue(boss, -1f);
        _rapidBulletTimerField?.SetValue(boss, 0f);
        _rapidBulletCountField?.SetValue(boss, 0);
        _rapidBombTimerField?.SetValue(boss, 0f);
        _rapidBombCountField?.SetValue(boss, 0);
        _rapidStormTimerField?.SetValue(boss, 0f);
        _rapidStormCountField?.SetValue(boss, 0);
        _hasTakenDamageField?.SetValue(boss, false);
        _directionOfLastAttackField?.SetValue(boss, 0);
        _notifiedBattleSceneField?.SetValue(boss, false);

        boss.tinkTimer = 0f;
    }

    private static void ResetAllBossFsms(HealthManager boss)
    {
        var allFsms = boss.GetComponentsInChildren<PlayMakerFSM>(true);

        foreach (var fsm in allFsms)
        {
            try
            {
                var fsmName = fsm.FsmName;
                var objName = fsm.gameObject.name;

                if (fsmName == "Control" && objName.Contains("Lace Boss"))
                {
                    ResetPhaseFsmVariables(fsm);

                    if (fsm.FsmStates.Any(s => s.Name == "Refight Engarde"))
                    {
                        fsm.SetState("Refight Engarde");
                    }
                }
                else if (fsmName == "Multicircle")
                {
                    SetFsmFloat(fsm, "X Scale", 0f);
                    SetFsmFloat(fsm, "Next Y Pos", 0f);
                    SetFsmFloat(fsm, "Slash Distance", 3f);
                    if (fsm.FsmStates.Any(s => s.Name == "Idle"))
                    {
                        fsm.SetState("Idle");
                    }
                }
                else if (fsmName == "Circle Slash Catch")
                {
                    SetFsmBool(fsm, "Listen For MultiHit", false);
                }
                else if (fsmName == "FSM" && (objName.Contains("Hit") || objName.Contains("Slash")))
                {
                    SetFsmBool(fsm, "Black Threaded", false);
                    SetFsmBool(fsm, "Did Bind Bell Hit", false);
                    SetFsmBool(fsm, "Hazard Hit", false);
                    SetFsmBool(fsm, "Hero Parrying", false);
                    SetFsmBool(fsm, "Ignore Parrying", false);
                    SetFsmBool(fsm, "z2 Steam Hazard", false);
                    SetFsmBool(fsm, "z3 Force Black Threaded", false);
                    SetFsmString(fsm, "Hero Event", "");
                    SetFsmString(fsm, "Hero Event Hazard", "");

                    if (objName.Contains("Combo Slash"))
                    {
                        SetFsmString(fsm, "Hero Event", "MULTI DOUBLE STRIKE");
                        SetFsmString(fsm, "Hero Event Hazard", "MULTI WOUND HAZARD");
                        if (fsm.FsmStates.Any(s => s.Name == "Idle"))
                        {
                            fsm.SetState("Idle");
                        }
                    }
                }
                else if (fsmName == "Multihitter")
                {
                    SetFsmBool(fsm, "Bind Bell Hit", false);
                    SetFsmBool(fsm, "Parrying", false);
                    if (fsm.FsmStates.Any(s => s.Name == "Idle"))
                    {
                        fsm.SetState("Idle");
                    }
                }
                else if (fsmName == "Control" && objName.Contains("Corpse"))
                {
                    SetFsmFloat(fsm, "Scale X", 0f);
                    SetFsmFloat(fsm, "Pos X", 0f);
                    SetFsmFloat(fsm, "Constrain X Min", 40.5f);
                    SetFsmFloat(fsm, "Constrain X Max", 67.5f);
                }
                else if (fsmName == "Control" && objName.Contains("NPC"))
                {
                    SetFsmBool(fsm, "Hero Is Hurt", false);
                }
                else if (fsmName.Contains("Projectile") || fsmName.Contains("Spawn") || fsmName.Contains("Summon"))
                {
                    if (fsm.FsmStates.Any(s => s.Name == "Idle"))
                    {
                        fsm.SetState("Idle");
                    }
                    else if (fsm.FsmStates.Any(s => s.Name == "Init"))
                    {
                        fsm.SetState("Init");
                    }
                }

                ResetRandomEventActions(fsm);
            }
            catch (System.Exception) { }
        }
    }

    private static void ResetRandomEventActions(PlayMakerFSM fsm)
    {
        foreach (var state in fsm.FsmStates)
        {
            foreach (var action in state.Actions)
            {
                if (action is RandomEvent randomEvent && _randomEventLastEventIndexField != null)
                {
                    _randomEventLastEventIndexField.SetValue(randomEvent, -1);
                }
                else if (action is ArrayGetRandom arrayGetRandom && _arrayGetRandomLastIndexField != null)
                {
                    _arrayGetRandomLastIndexField.SetValue(arrayGetRandom, -1);
                }
                else if (action is PlayRandomSound playRandomSound && _playRandomSoundLastIndexField != null)
                {
                    _playRandomSoundLastIndexField.SetValue(playRandomSound, -1);
                }
                else if (action is RandomInt randomInt && _randomIntLastIndexField != null)
                {
                    _randomIntLastIndexField.SetValue(randomInt, -1);
                }
            }
        }
    }

    private static void ResetPhaseFsmVariables(PlayMakerFSM fsm)
    {
        try
        {
            SetFsmBool(fsm, "Above Wallcling Min", false);
            SetFsmBool(fsm, "Can Bomb Slash", false);
            SetFsmBool(fsm, "Counter Range", false);
            SetFsmBool(fsm, "Counter Ready", false);
            SetFsmBool(fsm, "CrossSlashing Hero", false);
            SetFsmBool(fsm, "Did P2 Shift", false);
            SetFsmBool(fsm, "Did P3 Shift", false);
            SetFsmBool(fsm, "Do Pose", false);
            SetFsmBool(fsm, "Evade Bomb", false);
            SetFsmBool(fsm, "Facing Right", false);
            SetFsmBool(fsm, "Floor Ahead", true);
            SetFsmBool(fsm, "Hero Is R", false);
            SetFsmBool(fsm, "Hero on Wall", false);
            SetFsmBool(fsm, "Hornet Dead", false);
            SetFsmBool(fsm, "Not Above Hero", false);
            SetFsmBool(fsm, "Right Side", false);
            SetFsmBool(fsm, "Wall Ahead", false);
            SetFsmBool(fsm, "Will Counter", false);
            SetFsmBool(fsm, "Will CrossSlash", false);
            SetFsmBool(fsm, "In Evade Range", false);
            SetFsmBool(fsm, "Can Evade", false);
            SetFsmBool(fsm, "Evade Floor Safe", false);
            SetFsmBool(fsm, "Evade Flipper", false);

            SetFsmInt(fsm, "Ct Charge", 0);
            SetFsmInt(fsm, "Ct Combo", 1);
            SetFsmInt(fsm, "Ct CrossSlash", 0);
            SetFsmInt(fsm, "Ct Evade", 0);
            SetFsmInt(fsm, "Ct J Slash", 0);
            SetFsmInt(fsm, "Evade Attempts", 0);
            SetFsmInt(fsm, "Hops", 2);
            SetFsmInt(fsm, "Ms Charge", 1);
            SetFsmInt(fsm, "Ms Combo", 0);
            SetFsmInt(fsm, "Ms Evade", 0);
            SetFsmInt(fsm, "Ms J Slash", 1);
            SetFsmInt(fsm, "Ct Bomb Slash", 0);
            SetFsmInt(fsm, "Ms Bomb Slash", 0);
            SetFsmInt(fsm, "Phase", 1);
            SetFsmInt(fsm, "P2 HP", 600);
            SetFsmInt(fsm, "P3 HP", 320);
            SetFsmInt(fsm, "Rage Slashes", 2);
            SetFsmInt(fsm, "Charges Performed", 0);

            SetFsmFloat(fsm, "Angle", 0f);
            SetFsmFloat(fsm, "Angle Max", 0f);
            SetFsmFloat(fsm, "Angle Min", 0f);
            SetFsmFloat(fsm, "Anim Start Time", 0f);
            SetFsmFloat(fsm, "Bomb Max X", 66.5f);
            SetFsmFloat(fsm, "Bomb Max Y", 106f);
            SetFsmFloat(fsm, "Bomb Min X", 40.5f);
            SetFsmFloat(fsm, "Bomb Min Y", 101f);
            SetFsmFloat(fsm, "Bomb X", 0f);
            SetFsmFloat(fsm, "Bomb Y", 0f);
            SetFsmFloat(fsm, "Centre X", 54f);
            SetFsmFloat(fsm, "Charge Time", 0f);
            SetFsmFloat(fsm, "Combo Slash Speed", 30f);
            SetFsmFloat(fsm, "Counter Pause", 0f);
            SetFsmFloat(fsm, "CrossSlash Antic Time", 0f);
            SetFsmFloat(fsm, "Distance", 9.923786f);
            SetFsmFloat(fsm, "Double Strike Pause", 0.2f);
            SetFsmFloat(fsm, "Gravity", 2f);
            SetFsmFloat(fsm, "Hero X", 0f);
            SetFsmFloat(fsm, "Idle Time", 0.65f);
            SetFsmFloat(fsm, "Land Y", 100.25f);
            SetFsmFloat(fsm, "Self X", 0f);
            SetFsmFloat(fsm, "Stun Timer", 0f);
            SetFsmFloat(fsm, "Target Distance", 6f);
            SetFsmFloat(fsm, "Tele Offset", 0f);
            SetFsmFloat(fsm, "Tele Out Floor", 95f);
            SetFsmFloat(fsm, "Tele X", 0f);
            SetFsmFloat(fsm, "Velocity Y", 0f);
            SetFsmFloat(fsm, "Wallcling Min Y", 99f);
            SetFsmFloat(fsm, "X Scale", 0f);
            SetFsmFloat(fsm, "Arena Plat Bot Y", 99.5f);
            SetFsmFloat(fsm, "Fall Check Speed", -6f);

            SetFsmString(fsm, "Cross Slash Anim", "");
            SetFsmString(fsm, "Next Event", "COMBO");
            SetFsmString(fsm, "Anim", "");
        }
        catch (System.Exception) { }
    }

    private static void SetFsmBool(PlayMakerFSM fsm, string name, bool value)
    {
        var v = fsm.FsmVariables.FindFsmBool(name);
        if (v != null) v.Value = value;
    }

    private static void SetFsmInt(PlayMakerFSM fsm, string name, int value)
    {
        var v = fsm.FsmVariables.FindFsmInt(name);
        if (v != null) v.Value = value;
    }

    private static void SetFsmFloat(PlayMakerFSM fsm, string name, float value)
    {
        var v = fsm.FsmVariables.FindFsmFloat(name);
        if (v != null) v.Value = value;
    }

    private static void SetFsmString(PlayMakerFSM fsm, string name, string value)
    {
        var v = fsm.FsmVariables.FindFsmString(name);
        if (v != null) v.Value = value;
    }

    private static void ResetBossStunState(HealthManager boss)
    {
        var allFsms = boss.GetComponents<PlayMakerFSM>();
        foreach (var fsm in allFsms)
        {
            if (fsm.FsmName == "Stun Control" || fsm.FsmName == "Stun")
            {
                try
                {
                    SetFsmBool(fsm, "Daze Effect Active", false);
                    SetFsmBool(fsm, "Abyss Attacking", false);

                    SetFsmInt(fsm, "Stun Combo", 13);
                    SetFsmInt(fsm, "Stun Hit Max", 15);

                    SetFsmFloat(fsm, "Combo Time", 1f);
                    SetFsmFloat(fsm, "Daze X Scale", 0f);
                    SetFsmFloat(fsm, "Daze Y Scale", 0f);
                    SetFsmFloat(fsm, "Hits Total", 0f);
                    SetFsmFloat(fsm, "Combo Counter", 0f);
                    SetFsmFloat(fsm, "Stun Damage", 0f);
                    SetFsmFloat(fsm, "Epsilon", 0.01f);

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

        CaptureInitialState();

        if (BossProjectileManager.Instance != null)
        {
            BossProjectileManager.Instance.ClearProjectileCache();
        }

        BossStateManager.ResetBossPhase();

        LogBossInitialState();

        Plugin.IsReady = true;
        GameStateCollector.ResetEpisodeTime();
        ActionManager.ResetInputs();

        StepModeManager.Instance.EnableStepMode();

        SharedMemoryManager.Instance.WriteGameState();
        SharedMemoryManager.Instance.WriteState(StateType.Reset);
    }

    private static void LogBossInitialState()
    {
        var boss = BossStateManager.CurrentBoss;
        if (boss == null)
        {
            Plugin.Logger.LogWarning("[HardReset State] Boss is null!");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("\n========== HARD RESET INITIAL STATE ==========");

        sb.AppendLine("\n--- Transform & Rigidbody ---");
        sb.AppendLine($"Position: {boss.transform.position}");
        sb.AppendLine($"Rotation: {boss.transform.rotation.eulerAngles}");
        sb.AppendLine($"LocalScale: {boss.transform.localScale}");

        var rb = BossStateManager.CurrentBossRb;
        if (rb != null)
        {
            sb.AppendLine($"Velocity: {rb.linearVelocity}");
            sb.AppendLine($"AngularVelocity: {rb.angularVelocity}");
        }

        sb.AppendLine("\n--- HealthManager Public ---");
        sb.AppendLine($"hp: {boss.hp}");
        sb.AppendLine($"isDead: {boss.isDead}");
        sb.AppendLine($"tinkTimer: {boss.tinkTimer}");
        sb.AppendLine($"IsInvincible: {boss.IsInvincible}");
        sb.AppendLine($"InvincibleFromDirection: {boss.InvincibleFromDirection}");

        sb.AppendLine("\n--- HealthManager Private (Reflection) ---");
        LogFieldValue(sb, boss, _evasionByHitRemainingField, "evasionByHitRemaining");
        LogFieldValue(sb, boss, _rapidBulletTimerField, "rapidBulletTimer");
        LogFieldValue(sb, boss, _rapidBulletCountField, "rapidBulletCount");
        LogFieldValue(sb, boss, _rapidBombTimerField, "rapidBombTimer");
        LogFieldValue(sb, boss, _rapidBombCountField, "rapidBombCount");
        LogFieldValue(sb, boss, _rapidStormTimerField, "rapidStormTimer");
        LogFieldValue(sb, boss, _rapidStormCountField, "rapidStormCount");
        LogFieldValue(sb, boss, _hasTakenDamageField, "hasTakenDamage");
        LogFieldValue(sb, boss, _invincibleField, "invincible");
        LogFieldValue(sb, boss, _invincibleFromDirectionField, "invincibleFromDirection");

        sb.AppendLine("\n--- Boss FSMs ---");
        var allFsms = boss.GetComponentsInChildren<PlayMakerFSM>(true);
        foreach (var fsm in allFsms)
        {
            sb.AppendLine($"\n[FSM: {fsm.FsmName}] on {fsm.gameObject.name}");
            sb.AppendLine($"  ActiveState: {fsm.ActiveStateName}");

            LogFsmVariables(sb, fsm);

            LogRandomEventActions(sb, fsm);
        }

        sb.AppendLine("\n--- Stun Control FSM ---");
        var stunFsms = boss.GetComponents<PlayMakerFSM>().Where(f => f.FsmName == "Stun Control" || f.FsmName == "Stun");
        foreach (var fsm in stunFsms)
        {
            sb.AppendLine($"[{fsm.FsmName}] ActiveState: {fsm.ActiveStateName}");
            var hitsTotal = fsm.FsmVariables.FindFsmFloat("Hits Total");
            var comboCounter = fsm.FsmVariables.FindFsmFloat("Combo Counter");
            var stunDamage = fsm.FsmVariables.FindFsmFloat("Stun Damage");
            sb.AppendLine($"  Hits Total: {hitsTotal?.Value}");
            sb.AppendLine($"  Combo Counter: {comboCounter?.Value}");
            sb.AppendLine($"  Stun Damage: {stunDamage?.Value}");
        }

        sb.AppendLine("\n========== END HARD RESET STATE ==========\n");

        Plugin.Logger.LogInfo(sb.ToString());
    }

    private static void LogFieldValue(StringBuilder sb, object obj, FieldInfo field, string name)
    {
        if (field != null)
        {
            var value = field.GetValue(obj);
            sb.AppendLine($"{name}: {value}");
        }
        else
        {
            sb.AppendLine($"{name}: [field not found]");
        }
    }

    private static void LogFsmVariables(StringBuilder sb, PlayMakerFSM fsm)
    {
        var vars = fsm.FsmVariables;

        foreach (var v in vars.BoolVariables)
        {
            sb.AppendLine($"  Bool '{v.Name}': {v.Value}");
        }

        foreach (var v in vars.IntVariables)
        {
            sb.AppendLine($"  Int '{v.Name}': {v.Value}");
        }

        foreach (var v in vars.FloatVariables)
        {
            sb.AppendLine($"  Float '{v.Name}': {v.Value}");
        }

        foreach (var v in vars.StringVariables)
        {
            sb.AppendLine($"  String '{v.Name}': '{v.Value}'");
        }
    }

    private static void LogRandomEventActions(StringBuilder sb, PlayMakerFSM fsm)
    {
        if (_randomEventLastEventIndexField == null) return;

        foreach (var state in fsm.FsmStates)
        {
            foreach (var action in state.Actions)
            {
                if (action is RandomEvent randomEvent)
                {
                    var lastIndex = _randomEventLastEventIndexField.GetValue(randomEvent);
                    sb.AppendLine($"  [State '{state.Name}'] RandomEvent.lastEventIndex: {lastIndex}");
                }
            }
        }
    }
}
