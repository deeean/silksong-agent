using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SilksongAgent;

public enum StateType
{
    None = 0,
    Ready = 1,
    Step = 2,
    Reset = 3
}

public enum CommandType
{
    None = 0,
    Step = 1,
    Reset = 2
}

public enum BossAttackState
{
    Idle = 0,
    Hop = 1,
    Pose = 2,
    ComboSlashAntic = 3,
    ComboSlashAttack = 4,
    CounterAntic = 5,
    CounterStance = 6,
    CounterAttack = 7,
    RapidSlashAntic = 8,
    RapidSlashAttack = 9,
    JSlashAntic = 10,
    JSlashAttack = 11,
    DownstabAntic = 12,
    DownstabAttack = 13,
    ChargeAntic = 14,
    ChargeAttack = 15,
    CrossSlashAntic = 16,
    CrossSlashAttack = 17,
    Evade = 18,
    Stun = 19,
    Teleport = 20,
    PhaseTransition = 21,
    QuickSlashAttack = 22,
    SlashEnd = 23,
    Fall = 24,
    MultihitSlash = 25,
    Multihitting = 26,
    SteamDamage = 27,
    Unknown = 28
}

public enum PlayerAnimationState
{
    Idle = 0,
    Airborne = 1,
    Land = 2,
    IdleToRun = 3,
    RunToIdle = 4,
    Turn = 5,
    WoundDoubleStrike = 6,
    Stun = 7,
    Recoil = 8,
    Dash = 9,
    Sprint = 10,
    DashToIdle = 11,
    SlashAlt = 12,
    SlashLandRunAlt = 13,
    DashAttackAntic = 14,
    SlashLand = 15,
    DashToRun = 16,
    UpSlash = 17,
    Slash = 18,
    DownSpikeAntic = 19,
    DownSpike = 20,
    DownspikeRecovery = 21,
    DownSpikeBounce2 = 22,
    DownSpikeBounce1 = 23,
    RecoilTwirl = 24,
    LandToRun = 25,
    SkidEnd1 = 26,
    HarpoonAntic = 27,
    HarpoonThrow = 28,
    HarpoonDash = 29,
    HarpoonCatch = 30,
    SilkChargeEnd = 31,
    AirDash = 32,
    SprintAir = 33,
    SprintAirLoop = 34,
    NeedleThrowAnticG = 35,
    NeedleThrowThrowing = 36,
    NeedleThrowCatch = 37,
    DoubleJump = 38,
    Walljump = 39,
    WallSlide = 40,
    DashAttack = 41,
    DashAttackRecover = 42,
    SlashToRun = 43,
    Run = 44,
    SkidEnd2 = 45,
    SprintAirShort = 46,
    MantleCling = 47,
    MantleVault = 48,
    SlashLandRun = 49,
    Wound = 50,
    HazardRespawn = 51,
    MantleLand = 52,
    MantleLandToRun = 53,
    SprintTurn = 54,
    NeedleThrowAnticA = 55,
    UmbrellaInflateAntic = 56,
    UmbrellaInflate = 57,
    UmbrellaFloat = 58,
    DownspikeRecoveryLand = 59,
    DashDown = 60,
    ShuttlecockAntic = 61,
    Shuttlecock = 62,
    SprintBackflip = 63,
    UmbrellaDeflate = 64,
    DashDownLand = 65,
    UmbrellaTurn = 66,
    MantleCancelToJump = 67,
    IdleHurt = 68,
    LookDown = 69,
    LookDownEnd = 70,
    LookUp = 71,
    LookUpEnd = 72,
    BindChargeGround = 73,
    BindBurstGround = 74,
    BindChargeAir = 75,
    BindBurstAir = 76,
    Fall = 77,
    WallCling = 78,
    WalljumpAntic = 79,
    HardLand = 80,
    Walk = 81,
    Unknown = 82
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CommandData
{
    public int commandType;
    public byte left;
    public byte right;
    public byte up;
    public byte down;
    public byte jump;
    public byte attack;
    public byte dash;
    public byte clawline;
    public byte skill;
    public byte heal;
    public int commandReady;
}


[StructLayout(LayoutKind.Sequential, Pack = 4)]
public unsafe struct GameState
{
    public float playerPosX;
    public float playerPosY;
    public float playerVelX;
    public float playerVelY;
    public float bossPosX;
    public float bossPosY;
    public float bossVelX;
    public float bossVelY;
    public float episodeTime;

    public int playerHealth;
    public int playerMaxHealth;
    public int playerSilk;
    public int bossHealth;
    public int bossMaxHealth;
    public int bossPhase;
    public int bossAttackState;

    public byte playerGrounded;
    public byte playerCanDash;
    public byte playerFacingRight;
    public byte playerInvincible;
    public byte bossFacingRight;
    public byte terminated;
    public byte truncated;
    public byte playerCanAttack;

    public byte playerAttacking;
    public byte playerDashing;
    public byte playerJumping;
    public byte playerFalling;
    public byte playerFocusing;
    public byte playerCasting;
    public byte playerRecoiling;
    public byte playerWallSliding;

    public fixed float raycastDistances[32];
    public fixed int raycastHitTypes[32];

    public int playerAnimationState;
    public float playerAnimationProgress;
    public float playerAnimationTotalFrames;
}

public class SharedMemoryManager : MonoBehaviour
{
    public static SharedMemoryManager Instance;

    private const string MemoryNameBase = "silksong_shared_memory";
    private const int MemorySize = 4096;
    private const int StateOffset = 0;
    private const int GameStateOffset = 4;
    private const int CommandOffset = 1024;

    private MemoryMappedFile memoryMappedFile;
    private MemoryMappedViewAccessor accessor;
    private CommandData commandData;

    private string GetMemoryName()
    {
        if (Plugin.InstanceId == 0)
            return MemoryNameBase;
        return $"{MemoryNameBase}_{Plugin.InstanceId}";
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        var memoryName = GetMemoryName();
        memoryMappedFile = MemoryMappedFile.CreateOrOpen(memoryName, MemorySize);
        accessor = memoryMappedFile.CreateViewAccessor();
    }

    public void WriteState(StateType state)
    {
        try
        {
            accessor.Write(StateOffset, (int)state);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Error writing state: {e.Message}");
        }
    }

    public void WriteGameState()
    {
        try
        {
            BossProjectileManager.Instance?.RefreshProjectileCache();

            GameState gameState = GameStateCollector.CollectGameState();

            accessor.Write(GameStateOffset, ref gameState);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Error writing game state: {e.Message}");
        }
    }

    private void ReadCommand()
    {
        try
        {
            accessor.Read(CommandOffset, out commandData);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Error reading command: {e.Message}");
        }
    }

    private void ProcessCommand()
    {
        if (commandData.commandReady != 1)
        {
            return;
        }

        if (GameManager.instance == null || HeroController.instance == null)
        {
            return;
        }

        ActionManager.IsLeftPressed = commandData.left != 0;
        ActionManager.IsRightPressed = commandData.right != 0;
        ActionManager.IsUpPressed = commandData.up != 0;
        ActionManager.IsDownPressed = commandData.down != 0;
        ActionManager.IsJumpPressed = commandData.jump != 0;
        ActionManager.IsAttackPressed = commandData.attack != 0;
        ActionManager.IsDashPressed = commandData.dash != 0;
        ActionManager.IsClawlinePressed = commandData.clawline != 0;
        ActionManager.IsSkillPressed = commandData.skill != 0;
        ActionManager.IsHealPressed = commandData.heal != 0;

        var commandType = (CommandType)commandData.commandType;
        switch (commandType)
        {
            case CommandType.Step:
                GameManager.instance.StartCoroutine(StepModeManager.Instance.Step());
                break;
            case CommandType.Reset:
                StepModeManager.Instance.DisableStepMode();
                GameManager.instance.StartCoroutine(EpisodeResetter.ResetEpisode());
                break;
            case CommandType.None:
            default:
                break;
        }

        try
        {
            accessor.Write(CommandOffset + Marshal.OffsetOf<CommandData>("commandReady").ToInt32(), 0);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Error resetting commandReady: {e.Message}");
        }
    }

    private void Update()
    {
        if (CommandLineArgs.Manual)
            return;

        ReadCommand();
        ProcessCommand();
    }
}
