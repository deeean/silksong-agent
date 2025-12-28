using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace SilksongAgent;

public static class BossStateManager
{
    public static HealthManager CurrentBoss { get; private set; }
    public static Rigidbody2D CurrentBossRb { get; private set; }
    public static PlayMakerFSM CurrentBossFsm { get; private set; }
    public static tk2dSpriteAnimator CurrentBossAnimator { get; private set; }

    private static int currentBossPhase = 0;
    private static string lastTrackedFsmState = "";

    private static readonly Dictionary<string, BossAnimationState> AnimationMap = new()
    {
        { "Idle", BossAnimationState.Idle },
        { "Combo Slash", BossAnimationState.ComboSlash },
        { "Antic", BossAnimationState.Antic },
        { "Rising Slash", BossAnimationState.RisingSlash },
        { "Charge Antic", BossAnimationState.ChargeAntic },
        { "RapidSlash Charge", BossAnimationState.RapidSlashCharge },
        { "TurnToIdle", BossAnimationState.Unknown },
        { "Counter Stance", BossAnimationState.CounterStance },
        { "Engarde", BossAnimationState.Unknown },
        { "Evade", BossAnimationState.Evade },
        { "Forward Hop", BossAnimationState.ForwardHop },
        { "NPC Idle Right", BossAnimationState.Unknown },
        { "NPC Idle Turn Left", BossAnimationState.Unknown },
        { "NPC Idle Left", BossAnimationState.Unknown },
        { "NPC Idle Turn Right", BossAnimationState.Unknown },
        { "Possession", BossAnimationState.Unknown },
        { "Stun", BossAnimationState.Stun },
        { "Charge", BossAnimationState.Charge },
        { "Charge Recover", BossAnimationState.ChargeRecover },
        { "Downstab Antic", BossAnimationState.DownstabAntic },
        { "Downstab", BossAnimationState.Downstab },
        { "Downstab End", BossAnimationState.DownstabEnd },
        { "Counter Antic", BossAnimationState.CounterAntic },
        { "Counter End", BossAnimationState.CounterEnd },
        { "Counter Hit", BossAnimationState.CounterHit },
        { "RapidSlash End", BossAnimationState.RapidSlashEnd },
        { "RapidSlash Loop", BossAnimationState.RapidSlashLoop },
        { "RapidSlash Effect", BossAnimationState.RapidSlashEffect },
        { "Jump Antic", BossAnimationState.JumpAntic },
        { "Conduct", BossAnimationState.Conduct },
        { "CrossSlash Antic", BossAnimationState.CrossSlashAntic },
        { "Conduct End", BossAnimationState.ConductEnd },
        { "Stun Air", BossAnimationState.StunAir },
        { "Stun Recover", BossAnimationState.StunRecover },
        { "Jump AnticQ", BossAnimationState.JumpAnticQ },
        { "Jump Away", BossAnimationState.JumpAway },
        { "Eye Flash", BossAnimationState.Unknown },
        { "MultiHit Slash", BossAnimationState.MultiHitSlash },
        { "Pose Lean", BossAnimationState.Unknown },
        { "Pose Upright", BossAnimationState.Unknown },
        { "Pose Swish", BossAnimationState.Unknown },
        { "Dash Burst", BossAnimationState.DashBurst },
        { "AirDash Burst", BossAnimationState.AirDashBurst },
        { "Stun Hit", BossAnimationState.StunHit },
        { "Trap Stun", BossAnimationState.TrapStun },
        { "Pose Hornet Defeated", BossAnimationState.Unknown },
        { "NPC Sit", BossAnimationState.Unknown },
        { "Swish Block", BossAnimationState.SwishBlock },
        { "ConductToIdle", BossAnimationState.Unknown },
        { "NPC Sit Antic", BossAnimationState.Unknown },
        { "NPC SitLook", BossAnimationState.Unknown },
        { "SitToIdle", BossAnimationState.Unknown },
        { "Combo Slash Q", BossAnimationState.ComboSlashQ },
        { "Downstab Antic Q", BossAnimationState.DownstabAnticQ },
        { "Bomb Slash Antic", BossAnimationState.BombSlashAntic },
        { "Bomb Slash", BossAnimationState.BombSlash },
        { "Fall", BossAnimationState.Unknown },
        { "Land", BossAnimationState.Unknown },
        { "Death 1", BossAnimationState.Unknown },
        { "Death 2", BossAnimationState.Unknown },
        { "Lie", BossAnimationState.Unknown },
        { "LieToWake", BossAnimationState.Unknown },
        { "Combo Slash Triple", BossAnimationState.ComboSlashTriple },
        { "P2 Shift Old", BossAnimationState.P2ShiftOld },
        { "ChargeMulti Antic", BossAnimationState.ChargeMultiAntic },
        { "ChargeMulti", BossAnimationState.ChargeMulti },
        { "ChargeMulti Recover", BossAnimationState.ChargeMultiRecover },
        { "Rising Slash Multi", BossAnimationState.RisingSlashMulti },
        { "Roar", BossAnimationState.Unknown },
        { "Death Stagger", BossAnimationState.Unknown },
        { "Laugh", BossAnimationState.Unknown },
        { "Tele In", BossAnimationState.TeleIn },
        { "Death Air", BossAnimationState.Unknown },
        { "Death Land Stun", BossAnimationState.Unknown },
        { "Tele Out", BossAnimationState.TeleOut },
        { "Wall Bounce", BossAnimationState.WallBounce },
        { "Charge Crossup", BossAnimationState.ChargeCrossup },
        { "Quick Slash", BossAnimationState.QuickSlash },
        { "RapidSlashAir TeleIn", BossAnimationState.RapidSlashAirTeleIn },
        { "RapidSlashAir", BossAnimationState.RapidSlashAir },
        { "Sing", BossAnimationState.Unknown },
        { "Sing End", BossAnimationState.Unknown },
        { "RapidSlashAir End", BossAnimationState.RapidSlashAirEnd },
        { "Tele Out Fast", BossAnimationState.TeleOutFast },
        { "Counter Antic Fast", BossAnimationState.CounterAnticFast },
        { "MultiHit Slash Air", BossAnimationState.MultiHitSlashAir },
        { "Multihit AirEnd", BossAnimationState.MultihitAirEnd },
        { "P2 Shift", BossAnimationState.P2Shift },
        { "Mid Battle Roar", BossAnimationState.Unknown },
        { "Counter Flash", BossAnimationState.CounterFlash },
        { "RapidSlashAir End Q", BossAnimationState.RapidSlashAirEndQ },
        { "Swish Block Long", BossAnimationState.SwishBlockLong },
        { "Combo Strike 1", BossAnimationState.ComboStrike1 },
        { "Combo Strike 2", BossAnimationState.ComboStrike2 },
        { "Charge Strike", BossAnimationState.ChargeStrike },
        { "Downstab Strike", BossAnimationState.DownstabStrike },
        { "Downstab Followup", BossAnimationState.DownstabFollowup },
        { "Forward Hop Intro", BossAnimationState.ForwardHopIntro },
        { "Combo Slash LongAntic", BossAnimationState.ComboSlashLongAntic },
        { "Forward Hop Slow", BossAnimationState.ForwardHopSlow },
        { "Lava Damage", BossAnimationState.Unknown },
        { "Tele In Fast", BossAnimationState.TeleInFast },
    };

    public static int CurrentPhase => currentBossPhase;

    public static void FindBoss()
    {
        var healthManagers = Object.FindObjectsByType<HealthManager>(FindObjectsSortMode.None);
        CurrentBoss = healthManagers
            .Where(hm => hm != null && hm.hp > 0)
            .Where(hm => hm.name.Contains("Boss") ||
                         hm.name.Contains("Lace") ||
                         hm.hp >= 100)
            .OrderByDescending(hm => hm.hp)
            .FirstOrDefault();

        if (CurrentBoss != null)
        {
            CurrentBossRb = CurrentBoss.GetComponent<Rigidbody2D>();
            CurrentBossFsm = CurrentBoss.GetComponent<PlayMakerFSM>();
            CurrentBossAnimator = CurrentBoss.GetComponent<tk2dSpriteAnimator>();
        }
        else
        {
            CurrentBossRb = null;
            CurrentBossFsm = null;
            CurrentBossAnimator = null;
            Plugin.Logger.LogWarning("No boss found in scene");
        }
    }

    public static void ResetBoss()
    {
        if (CurrentBoss != null)
        {
            Plugin.Logger.LogInfo($"[Reset] Clearing previous boss reference: {CurrentBoss.name}");
        }
        CurrentBoss = null;
        CurrentBossRb = null;
        CurrentBossFsm = null;
        CurrentBossAnimator = null;
    }

    public static void ResetBossPhase()
    {
        currentBossPhase = 0;
        lastTrackedFsmState = "";
    }

    public static void UpdateBossPhase()
    {
        if (CurrentBossFsm == null)
            return;

        string stateName = CurrentBossFsm.ActiveStateName;
        if (string.IsNullOrEmpty(stateName) || stateName == lastTrackedFsmState)
            return;

        lastTrackedFsmState = stateName;

        if (currentBossPhase < 1)
        {
            if (stateName.StartsWith("P2 Shift"))
            {
                currentBossPhase = 1;
            }
        }

        if (currentBossPhase < 2)
        {
            if (stateName.StartsWith("P3 Roar"))
            {
                currentBossPhase = 2;
            }
        }
    }

    public static BossAnimationState GetAnimationState(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
            return BossAnimationState.Unknown;

        return AnimationMap.TryGetValue(clipName, out var state) ? state : BossAnimationState.Unknown;
    }
}

public enum BossAnimationState
{
    Idle = 0,
    ComboSlash = 1,
    Antic = 2,
    RisingSlash = 3,
    ChargeAntic = 4,
    RapidSlashCharge = 5,
    CounterStance = 6,
    Evade = 7,
    ForwardHop = 8,
    Stun = 9,
    Charge = 10,
    ChargeRecover = 11,
    DownstabAntic = 12,
    Downstab = 13,
    DownstabEnd = 14,
    CounterAntic = 15,
    CounterEnd = 16,
    CounterHit = 17,
    RapidSlashEnd = 18,
    RapidSlashLoop = 19,
    RapidSlashEffect = 20,
    JumpAntic = 21,
    Conduct = 22,
    CrossSlashAntic = 23,
    ConductEnd = 24,
    StunAir = 25,
    StunRecover = 26,
    JumpAnticQ = 27,
    JumpAway = 28,
    MultiHitSlash = 29,
    DashBurst = 30,
    AirDashBurst = 31,
    StunHit = 32,
    TrapStun = 33,
    SwishBlock = 34,
    ComboSlashQ = 35,
    DownstabAnticQ = 36,
    BombSlashAntic = 37,
    BombSlash = 38,
    ComboSlashTriple = 39,
    P2ShiftOld = 40,
    ChargeMultiAntic = 41,
    ChargeMulti = 42,
    ChargeMultiRecover = 43,
    RisingSlashMulti = 44,
    TeleIn = 45,
    TeleOut = 46,
    WallBounce = 47,
    ChargeCrossup = 48,
    QuickSlash = 49,
    RapidSlashAirTeleIn = 50,
    RapidSlashAir = 51,
    RapidSlashAirEnd = 52,
    TeleOutFast = 53,
    CounterAnticFast = 54,
    MultiHitSlashAir = 55,
    MultihitAirEnd = 56,
    P2Shift = 57,
    CounterFlash = 58,
    RapidSlashAirEndQ = 59,
    SwishBlockLong = 60,
    ComboStrike1 = 61,
    ComboStrike2 = 62,
    ChargeStrike = 63,
    DownstabStrike = 64,
    DownstabFollowup = 65,
    ForwardHopIntro = 66,
    ComboSlashLongAntic = 67,
    ForwardHopSlow = 68,
    TeleInFast = 69,
    Unknown = 70
}
