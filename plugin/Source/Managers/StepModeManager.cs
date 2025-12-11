using System;
using System.Collections;
using UnityEngine;

namespace SilksongAgent;

public class StepModeManager : MonoBehaviour
{
    public static StepModeManager Instance { get; private set; }

    private bool isEnabled = false;
    private bool isSteppingFrame = false;

    public bool IsEnabled => isEnabled;
    public bool IsSteppingFrame => isSteppingFrame;

    private void Awake()
    {
        Instance = this;
    }
    
    public void EnableStepMode()
    {
        isEnabled = true;
        Time.timeScale = 0f;
    }
    
    public void DisableStepMode()
    {
        isEnabled = false;
        Time.timeScale = CommandLineArgs.TimeScale;
    }

    public IEnumerator Step()
    {
        isSteppingFrame = true;
        Time.timeScale = CommandLineArgs.TimeScale;

        for (int i = 0; i < Constants.FramesPerStep; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        Time.timeScale = 0f;
        SharedMemoryManager.Instance.WriteGameState();
        SharedMemoryManager.Instance.WriteState(StateType.Step);
        isSteppingFrame = false;
    }
}