using System;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SilksongAgent;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance;
    public static bool IsReady = false;
    public static int InstanceId = 0;

    internal new static ManualLogSource Logger;

    private Harmony _harmony;

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;

        ParseCommandLineArgs();

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        var stepModeManager = new GameObject("StepModeManager");
        DontDestroyOnLoad(stepModeManager);
        stepModeManager.AddComponent<StepModeManager>();

        var sharedMemoryManager = new GameObject("SharedMemoryManager");
        DontDestroyOnLoad(sharedMemoryManager);
        sharedMemoryManager.AddComponent<SharedMemoryManager>();

        var debugOverlayManager = new GameObject("DebugOverlayManager");
        DontDestroyOnLoad(debugOverlayManager);
        debugOverlayManager.AddComponent<DebugOverlayManager>();

        var bossProjectileManager = new GameObject("BossProjectileManager");
        DontDestroyOnLoad(bossProjectileManager);
        bossProjectileManager.AddComponent<BossProjectileManager>();

        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;
    }
    
    private void ParseCommandLineArgs()
    {
        CommandLineArgs.Parse();
        InstanceId = CommandLineArgs.Id;
        ActionManager.IsAgentControlEnabled = !CommandLineArgs.Manual;
        Logger.LogInfo($"Instance ID: {CommandLineArgs.Id}, Time scale: {CommandLineArgs.TimeScale}, Manual: {CommandLineArgs.Manual}");
    }

    private void Update()
    {
        if (StepModeManager.Instance != null && StepModeManager.Instance.IsEnabled) {
            return;
        }

        Time.timeScale = (IsReady || CommandLineArgs.Manual) ? CommandLineArgs.TimeScale : 100.0f;
    }
}
