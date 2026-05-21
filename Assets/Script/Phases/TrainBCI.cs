using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainBCI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The GameObject representing Door 1")]
    public GameObject Door1;
    
    [Tooltip("The GameObject representing Door 2")]
    public GameObject Door2;
    
    [Tooltip("The UI Canvas shown before and after training")]
    public GameObject IntroductionCanvas;
   
    [Header("Intro Buttons")]
    public GameObject IntroButton;
    public GameObject IntroNextButton;

    [Header("Training Parameters")]
    [Tooltip("Number of trials (m times) per door")]
    public int m_times = 5;
    [Header("Active Duration")]
    [Tooltip("Duration in seconds (n sec) to show each door")]
    public float showDuration = 4f;
    [Header("Imagery  Duration")]
    [Tooltip("Duration in seconds for the blank screen before the imagery phase")]
    public float imageryDuration = 4f;
    [Header("Transition Duration")]
    [Tooltip("Duration before the imagery phase starts")]
    public float imageryDelay = 0.3f;



    public void Start()
    {
        ShowIntro();
        
    }
    private void OnEnable()
    {
        PositionCanvasFront();
        ShowIntro();
        if (LSLCommunicationManager.Instance != null)
            LSLCommunicationManager.Instance.OnTrainingEvent += OnTrainingResult;   
    }

    private void OnDisable()
    {
        if (IntroductionCanvas != null) IntroductionCanvas.SetActive(false);
        if (Door1 != null) Door1.SetActive(false);
        if (Door2 != null) Door2.SetActive(false);
        if (LSLCommunicationManager.Instance != null)
            LSLCommunicationManager.Instance.OnTrainingEvent -= OnTrainingResult;
    }



    public void PositionCanvasFront()
    {
        if (GlobalInput.Instance.cam == null) return;

        if (GlobalInput.Instance.cam != null)
        {
            // Position in front of camera once
            transform.position = GlobalInput.Instance.cam.transform.position
                + GlobalInput.Instance.cam.transform.right * GlobalInput.Instance.horizontalOffset
                + GlobalInput.Instance.cam.transform.up * GlobalInput.Instance.verticalOffset
                + GlobalInput.Instance.cam.transform.forward * GlobalInput.Instance.distance;

            // Make UI face the camera once
            transform.rotation = GlobalInput.Instance.cam.transform.rotation;
        }
    }


    /// <summary>
    /// Displays the Introduction Canvas and hides the doors.
    /// Also disables the "Next" button so the user must proceed through the training.
    /// </summary>
    public void ShowIntro()
    {
        if (IntroductionCanvas != null) IntroductionCanvas.SetActive(true);

        if (Door1 != null) Door1.SetActive(false);
        if (Door2 != null) Door2.SetActive(false);

        StartIntroButtonUI();
    }

    /// <summary>
    /// Hides the "Intro-Next-UI" button at the start of the scene.
    /// </summary>
    public void StartIntroButtonUI()
    {
        if (IntroductionCanvas == null) return;
        IntroButton.SetActive(true);
        IntroNextButton.SetActive(false);
    }

    /// <summary>
    /// Shows the "Intro-Next-UI" button at the end of the training routine.
    /// </summary>
    public void IntroNextButtonUI()
    {
        if (IntroductionCanvas == null) return;
        IntroductionCanvas.SetActive(true);
        IntroButton.SetActive(false);
        IntroNextButton.SetActive(true);
    }

    /// <summary>
    /// Proceeds to the next phase of the experiment.
    /// Should be linked to the "Intro-Next-UI" button.
    /// </summary>
    public void NextPhase()
    {
        if (MainControl.Instance != null) MainControl.Instance.GoToNextPhase();
        gameObject.SetActive(false);
    }


    /// <summary>
    /// Function to disable the introduction canvas and begin the training routine.
    /// Should be linked to the "Start" button on the IntroductionCanvas.
    /// </summary>
    public void StartTraining()
    {
        if (IntroductionCanvas != null) IntroductionCanvas.SetActive(false);
        // if (IntroductionCanvas != null) 
        // {
        //     BB[] buttons = IntroductionCanvas.GetComponentsInChildren<BB>(true); 
        //     foreach (var b in buttons) 
        //     {
        //         if (b.gameObject.name == "Intro-UI") 
        //         {
        //             b.gameObject.SetActive(false); 
        //         }
        //     } 
        //     IntroductionCanvas.SetActive(false);
        // }
        StartCoroutine(TrainingRoutine());
    }

private IEnumerator TrainingRoutine()
{
    GenerateSequence();

    for (int i = 0; i < trialSequence.Count; i++)
    {
        string trial = trialSequence[i];
        Debug.Log($"[TrainBCI] Trial {i + 1}/{trialSequence.Count}: {trial}");

        if (trial == "Door1")
        {
            if (Door1 != null)
            {
                // ---------------- ACTIVE PHASE ----------------
                ExperimentLogger.Instance.LogEvent("Training_Active_Door1_Start", Door1.name, "TAD1S");
                LSL_Logger.Instance?.LogEvent("Training_Active_Door1_Start", Door1.name, "TAD1S");

                Door1.SetActive(true);
                yield return new WaitForSeconds(showDuration);

                ExperimentLogger.Instance.LogEvent("Training_Active_Door1_End", Door1.name, "TAD1E");
                LSL_Logger.Instance?.LogEvent("Training_Active_Door1_End", Door1.name, "TAD1E");

                Door1.SetActive(false);
            }

            // ---------------- TRANSITION GAP ----------------
            yield return new WaitForSeconds(imageryDelay);

            // ---------------- IMAGERY PHASE ----------------
            ExperimentLogger.Instance?.LogEvent("Training_Imagery_Door1_Start", Door1.name, "TID1S");
            LSL_Logger.Instance?.LogEvent("Training_Imagery_Door1_Start", Door1.name, "TID1S");

            yield return new WaitForSeconds(imageryDuration);

            ExperimentLogger.Instance?.LogEvent("Training_Imagery_Door1_End", Door1.name, "TID1E");
            LSL_Logger.Instance?.LogEvent("Training_Imagery_Door1_End", Door1.name, "TID1E");
        }
        else if (trial == "Door2")
        {
            if (Door2 != null)
            {
                // ---------------- ACTIVE PHASE ----------------
                ExperimentLogger.Instance.LogEvent("Active_Training_Door2_Start", Door2.name, "TAD2S");
                LSL_Logger.Instance?.LogEvent("Active_Training_Door2_Start", Door2.name, "TAD2S");

                Door2.SetActive(true);
                yield return new WaitForSeconds(showDuration);

                ExperimentLogger.Instance.LogEvent("Active_Training_Door2_End", Door2.name, "TAD2E");
                LSL_Logger.Instance?.LogEvent("Active_Training_Door2_End", Door2.name, "TAD2E");

                Door2.SetActive(false);
            }

            // ---------------- TRANSITION GAP ----------------
            yield return new WaitForSeconds(imageryDelay);

            // ---------------- IMAGERY PHASE ----------------
            ExperimentLogger.Instance?.LogEvent("Image_Training_Door2_Start", Door2.name, "TID2S");
            LSL_Logger.Instance?.LogEvent("Image_Training_Door2_Start", Door2.name, "TID2S");

            yield return new WaitForSeconds(imageryDuration);

            ExperimentLogger.Instance?.LogEvent("Image_Training_Door2_End", Door2.name, "TID2E");
            LSL_Logger.Instance?.LogEvent("Image_Training_Door2_End", Door2.name, "TID2E");
        }
    }

    ExperimentLogger.Instance?.LogEvent("Train_End", "Training Complete", "Train_End");
    LSL_Logger.Instance?.LogEvent("Train_End", "Training Complete", "Train_End");

    if (IntroductionCanvas != null) IntroductionCanvas.SetActive(true);
    IntroNextButtonUI();
}

    private List<string> trialSequence = new List<string>();

    private void GenerateSequence()
    {
        trialSequence.Clear();
        for (int i = 0; i < m_times; i++)
        {
            trialSequence.Add("Door1");
            trialSequence.Add("Door2");
        }
        FisherYatesShuffle(trialSequence);
        
        Debug.Log($"[TrainBCI] Generated {trialSequence.Count} trials:");
        for (int i = 0; i < trialSequence.Count; i++)
            Debug.Log($"[TrainBCI]  [{i:D2}] {trialSequence[i]}");
    }

    private static void FisherYatesShuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1); // inclusive upper bound
            (list[i], list[j]) = (list[j], list[i]);
        }
    }




    #region LSL Training Complete
    private void OnTrainingResult(BCIMessage msg)
{
    var cmd = (LSLCommunicationManager.BCICommand)msg.Code;

    switch (cmd)
    {
        case LSLCommunicationManager.BCICommand.TrainObj1ActiveComplete:
            ExperimentLogger.Instance.LogEvent("[TrainBCI] Training complete for Active Door 1");
            HandleDoorTrainingComplete(1, msg);
            break;

        case LSLCommunicationManager.BCICommand.TrainObj2ActiveComplete:
            ExperimentLogger.Instance.LogEvent("[TrainBCI] Training complete for Active Door 2");
            HandleDoorTrainingComplete(2, msg);
            break;

        case LSLCommunicationManager.BCICommand.TrainObj1ImageryComplete:
            ExperimentLogger.Instance.LogEvent("[TrainBCI] Training complete for Imagery Door 1");
            HandleDoorTrainingComplete(1, msg);
            break;

        case LSLCommunicationManager.BCICommand.TrainObj2ImageryComplete:
            ExperimentLogger.Instance.LogEvent("[TrainBCI] Training complete for Imagery Door 2");
            HandleDoorTrainingComplete(2, msg);
            break;

        default:
            // Ignore anything else
            return;
    }

}
    private void HandleDoorTrainingComplete(int doorNumber, BCIMessage msg)
    {
        ExperimentLogger.Instance.LogEvent(msg.Event, msg.Detail, "Door:"+ doorNumber + ":"+msg.Remark.Message);
        LSL_Logger.Instance?.LogEvent(msg.Event, msg.Detail, "Door:"+ doorNumber + ":"+msg.Remark.Message);
    }
    #endregion
}
