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

    private static int currentBossPhase = 0;
    private static string lastTrackedFsmState = "";
    private static readonly HashSet<string> observedBossStates = new HashSet<string>();

    private static readonly Dictionary<string, BossAttackState> ExactStateMap = new Dictionary<string, BossAttackState>
    {
        { "Idle", BossAttackState.Idle },
        { "Land", BossAttackState.Idle },
        { "Wallcling", BossAttackState.Idle },
        { "ComboSlash 1", BossAttackState.ComboSlashAntic },
        { "Counter Antic", BossAttackState.CounterAntic },
        { "Counter Stance", BossAttackState.CounterStance },
        { "RapidSlashAir Antic", BossAttackState.RapidSlashAntic },
        { "RapidSlash Charge", BossAttackState.RapidSlashAntic },
        { "J Slash M Antic", BossAttackState.JSlashAntic },
        { "Downstab Antic", BossAttackState.DownstabAntic },
        { "Charge Antic", BossAttackState.ChargeAntic },
        { "CrossSlash Antic", BossAttackState.CrossSlashAntic },
        { "Crossup Antic", BossAttackState.CrossSlashAntic },
        { "Slash Slam", BossAttackState.CrossSlashAttack },
        { "Bounce Back", BossAttackState.Teleport },
        { "Slash End", BossAttackState.SlashEnd },
        { "Fall", BossAttackState.Fall },
        { "Steam Damage", BossAttackState.SteamDamage },
    };

    private static readonly (string prefix, BossAttackState state)[] PrefixStateMap =
    {
        ("Hop", BossAttackState.Hop),
        ("Pose", BossAttackState.Pose),
        ("Refight", BossAttackState.Pose),
        ("ComboSlash", BossAttackState.ComboSlashAttack),
        ("Combo Strike", BossAttackState.ComboSlashAttack),
        ("Quick Slash", BossAttackState.QuickSlashAttack),
        ("Counter", BossAttackState.CounterAttack),
        ("RapidSlash", BossAttackState.RapidSlashAttack),
        ("J Slash", BossAttackState.JSlashAttack),
        ("Downstab", BossAttackState.DownstabAttack),
        ("Dstab", BossAttackState.DownstabAttack),
        ("Charge", BossAttackState.ChargeAttack),
        ("CrossSlash", BossAttackState.CrossSlashAttack),
        ("Evade", BossAttackState.Evade),
        ("Stun", BossAttackState.Stun),
        ("Tele", BossAttackState.Teleport),
        ("P2 Shift", BossAttackState.PhaseTransition),
        ("P3 Roar", BossAttackState.PhaseTransition),
        ("Multihit Slash", BossAttackState.MultihitSlash),
        ("Multihitting", BossAttackState.Multihitting),
    };

    public static int CurrentPhase => currentBossPhase;

    public static IReadOnlyCollection<string> GetObservedBossStates() => observedBossStates;

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
        }
        else
        {
            CurrentBossRb = null;
            CurrentBossFsm = null;
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

    public static BossAttackState GetBossAttackState()
    {
        if (CurrentBossFsm == null)
            return BossAttackState.Idle;

        string stateName = CurrentBossFsm.ActiveStateName;
        if (string.IsNullOrEmpty(stateName))
            return BossAttackState.Idle;

        stateName = stateName.Trim();
        observedBossStates.Add(stateName);

        if (ExactStateMap.TryGetValue(stateName, out var exactState))
            return exactState;

        foreach (var (prefix, state) in PrefixStateMap)
        {
            if (stateName.StartsWith(prefix))
                return state;
        }

        Plugin.Logger.LogWarning($"[BossStateManager] Unmapped boss state: '{stateName}'");
        return BossAttackState.Unknown;
    }
}
