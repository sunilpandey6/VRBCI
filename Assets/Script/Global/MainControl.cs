using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
public class MainControl : MonoBehaviour
{
    public static MainControl Instance { get; private set; }

    public enum ExperimentType
    {
        EyeTracking,
        Hybrid,
        BCI
    }

    public enum ExperimentPhase
    {
        SettingUI,
        TestUI,
        Demo3D,
        TrainBCI,
        Test3D,
        Completed
    }

    // gaze controller
    public bool isGazeInteractionEnabled { get; private set; } = true;
    
    [Header("Experiment Configuration")]
    public ExperimentType currentExperiment = ExperimentType.EyeTracking;
    public ExperimentPhase currentPhase;

    // Event triggered whenever the phase changes
    public event Action<ExperimentPhase> OnPhaseChanged;

    [Header("Phase Sequences")]
    private readonly List<ExperimentPhase> eyeTrackingSequence = new List<ExperimentPhase>
    {
        ExperimentPhase.SettingUI,
        ExperimentPhase.TestUI,
        ExperimentPhase.Demo3D,
        ExperimentPhase.Test3D
    };

    private readonly List<ExperimentPhase> hybridSequence = new List<ExperimentPhase>
    {
        ExperimentPhase.TestUI,
        ExperimentPhase.Test3D
    };

    private readonly List<ExperimentPhase> bciSequence = new List<ExperimentPhase>
    {
        ExperimentPhase.TrainBCI,
        ExperimentPhase.Test3D
    };

    private List<ExperimentPhase> currentSequence;
    private int currentPhaseIndex = 0;

    private void Awake()
    {
        // Implementing Singleton pattern for easy global access from other UI/Test scripts
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            // Uncomment if MainControl should persist across different Unity Scenes
            // DontDestroyOnLoad(this.gameObject); 
        }
    }

    void Start()
    {
        // Initialize the first experiment type (EyeTracking)
        SetExperimentType(ExperimentType.EyeTracking);
    }

    /// <summary>
    /// Programmatically change the experiment type and initialize its specific flow.
    /// You can call this from a UI button or it will trigger automatically at the end of a block.
    /// </summary>
    public void SetExperimentType(ExperimentType newType)
    {
        currentExperiment = newType;
        // if the current experiment is BCI, disable gaze interaction
        isGazeInteractionEnabled = (newType != ExperimentType.BCI);

        Debug.Log($"--- Starting Experiment Block: {currentExperiment} ---");
        
        if (ExperimentLogger.Instance != null)
        {
            ExperimentLogger.Instance.LogEvent("Experiment_Start", currentExperiment.ToString());
        }

        InitializeExperiment();
    }

    private void InitializeExperiment()
    {
        switch (currentExperiment)
        {
            case ExperimentType.EyeTracking:
                currentSequence = eyeTrackingSequence;
                break;
            case ExperimentType.Hybrid:
                currentSequence = hybridSequence;
                break;
            case ExperimentType.BCI:
                currentSequence = bciSequence;
                break;
        }

        currentPhaseIndex = 0;

        if (currentSequence != null && currentSequence.Count > 0)
        {
            SetPhase(currentSequence[currentPhaseIndex]);
        }
    }

    /// <summary>
    /// Call this method from UI buttons or other scripts to progress to the next phase/scene.
    /// Example: MainControl.Instance.GoToNextPhase();
    /// </summary>
    public void GoToNextPhase()
    {
        currentPhaseIndex++;

        if (currentPhaseIndex < currentSequence.Count)
        {
            // Move to the next phase within the current experiment
            SetPhase(currentSequence[currentPhaseIndex]);
        }
        else
        {
            // The sequence for the current experiment has finished
            Debug.Log($"Experiment {currentExperiment} Completed!");
            
            if (ExperimentLogger.Instance != null)
            {
                ExperimentLogger.Instance.LogEvent("Experiment_Ended", currentExperiment.ToString());
            }

            // Automatically transition to the next Experiment Type
            GoToNextExperiment();
        }
    }

    /// <summary>
    /// Handles the continuous flow: EyeTracking -> Hybrid -> BCI
    /// </summary>
    private void GoToNextExperiment()
    {
        if (currentExperiment == ExperimentType.EyeTracking)
        {
            SetExperimentType(ExperimentType.Hybrid);
        }
        else if (currentExperiment == ExperimentType.Hybrid)
        {
            SetExperimentType(ExperimentType.BCI);
        }
        else if (currentExperiment == ExperimentType.BCI)
        {
            // If BCI is finished, everything is fully done
            Debug.Log("ALL EXPERIMENTS COMPLETED!");
            currentPhase = ExperimentPhase.Completed;
            
            if (ExperimentLogger.Instance != null)
            {
                ExperimentLogger.Instance.LogEvent("All_Experiments_Completed");
            }
        }
    }

    private void SetPhase(ExperimentPhase newPhase)
    {
        currentPhase = newPhase;
        
        Debug.Log($"Transitioning to Phase: {currentPhase}");
        
        if (ExperimentLogger.Instance != null)
        {
            ExperimentLogger.Instance.LogEvent("Phase_Start", currentPhase.ToString());
        }
        
        // Notify listeners (like SceneController) that the phase has changed
        OnPhaseChanged?.Invoke(currentPhase);
    }
}
