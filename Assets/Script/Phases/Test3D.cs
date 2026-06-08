using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(EyeClosed))]
public class Test3D : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField] public EyeClosed eyeClosed;

    [Header("Experiment Mode")]
    public MainControl.ExperimentType currentMode;

    [Header("Intro Panel")]
    [Tooltip("The UI Canvas shown before and after training")]
    public GameObject IntroductionCanvas;
    [SerializeField] private UpdateUIPos introPos;
    [Header("Intro Canvas child Panel")]
    
    public GameObject IntroButton;
    [Header("Instruction Heading Text UI")]
    public TMP_Text IntroButton_headingText;
    [Header("Instruction Text UI")]
    public TMP_Text IntroButton_instructionText;

    [Header("Instruction Heading Texts")]
    [TextArea] public string eyeTrackingHeading;
    [TextArea] public string hybridHeading;
    [TextArea] public string bciHeading;

    [Header("Instruction Main Texts")]
    [TextArea] public string eyeTrackingInstruction;
    [TextArea] public string hybridInstruction;
    [TextArea] public string bciInstruction;

    public GameObject IntroNextButton;
    [Header("Instruction UI")]
    public TMP_Text IntroNextButton_headingText;
    [Header("Instruction UI")]
    public TMP_Text IntroNextButton_instructionText;

    [Header("Instruction Heading Texts")]
    [TextArea] public string eyeTrackingHeadingFinal;
    [TextArea] public string hybridHeadingFinal;
    [TextArea] public string bciHeadingFinal;

    [Header("Instruction Main Texts")]
    [TextArea] public string eyeTrackingInstructionFinal;
    [TextArea] public string hybridInstructionFinal;
    [TextArea] public string bciInstructionFinal;

    [Header("Answer UI")]
    public GameObject IntroAnswerUI;

    [Header("Scene Reference")]
    public GameObject Test3DScene;

    [Header("Door 1")]
    [SerializeField] private OB door1;
    [Header("Door 2")]
    [SerializeField] private OB door2;


    [Header("Spawn Point")]
    [SerializeField] private Transform spawnPoint;

    [Header("Player / Camera Rig")]
    [SerializeField] private Transform playerRig;

    #region Unity Lifecycle
    private void Awake()
    {
        if(eyeClosed == null) eyeClosed = GetComponent<EyeClosed>();
    }

    private void OnValidate(){
        eyeClosed = GetComponent<EyeClosed>();
    }

    void OnEnable()
    {
        if (LSLCommunicationManager.Instance != null)
            LSLCommunicationManager.Instance.OnPredictionResult += HandlePredictionLSL;
        currentMode = MainControl.Instance.currentExperiment;
        //show ui for test 3d
        ShowIntro();
        
    }

    void OnDisable()
    {
        if (IntroductionCanvas != null) IntroductionCanvas.SetActive(false);
        if (Test3DScene != null) Test3DScene.SetActive(false);
        if (LSLCommunicationManager.Instance != null)
            LSLCommunicationManager.Instance.OnPredictionResult -= HandlePredictionLSL;
    }
    #endregion

    #region Introduction
    public void ShowIntro()
    {
        if (IntroductionCanvas != null) 
        { 
            IntroductionCanvas.SetActive(true); 
            if (introPos != null)
            {
                introPos.PositionCanvasFront();
            }
        }
        if (Test3DScene != null) Test3DScene.SetActive(false);
        StartIntroButtonUI();
    }

    public void StartIntroButtonUI()
    {
        if (IntroductionCanvas == null) return;
        IntroButton.SetActive(true);
        IntroAnswerUI.SetActive(false);
        IntroNextButton.SetActive(false);
        SetMessageIntroButtonUI();


    }

    public void SetMessageIntroButtonUI()
    {
        if (IntroButton_headingText == null) return;
        if (IntroButton_instructionText == null) return;

        if (currentMode == MainControl.ExperimentType.EyeTracking)
        {
            IntroButton_headingText.text = eyeTrackingHeading;
            IntroButton_instructionText.text = eyeTrackingInstruction;
        }
        else if (currentMode == MainControl.ExperimentType.Hybrid)
        {
            IntroButton_headingText.text = hybridHeading;
            IntroButton_instructionText.text = hybridInstruction;
        }

        else if (currentMode == MainControl.ExperimentType.Hybrid)
        {
            IntroButton_headingText.text = bciHeading;
            IntroButton_instructionText.text = bciInstruction;
        }
    }
    // Final UI show after the experiment
    public void IntroNextButtonUI()
    {
        if (IntroductionCanvas == null) return;
        if (Test3DScene != null) Test3DScene.SetActive(false);
        IntroductionCanvas.SetActive(true);
        if (introPos != null)
        {
            introPos.PositionCanvasFront();
        }

        IntroButton.SetActive(false);
        IntroAnswerUI.SetActive(false);
        IntroNextButton.SetActive(true);
        SetMessageIntroFinalUI();

    }

    public void SetMessageIntroFinalUI()
    {
        if (IntroNextButton_headingText == null) return;
        if (IntroNextButton_instructionText == null) return;

        if (currentMode == MainControl.ExperimentType.EyeTracking)
        {
            IntroNextButton_headingText.text = eyeTrackingHeadingFinal;
            IntroNextButton_instructionText.text = eyeTrackingInstructionFinal;
        }
        else if (currentMode == MainControl.ExperimentType.Hybrid)
        {
            IntroNextButton_headingText.text = hybridHeadingFinal;
            IntroNextButton_instructionText.text = hybridInstructionFinal;
        }

        else if (currentMode == MainControl.ExperimentType.Hybrid)
        {
            IntroNextButton_headingText.text = bciHeadingFinal;
            IntroNextButton_instructionText.text = bciInstructionFinal;
        }

    }
    #endregion
    #region Answer UI

    public void SetAnsUI()
    {
        if (IntroductionCanvas == null) return;
        if (Test3DScene != null) Test3DScene.SetActive(false);
        IntroductionCanvas.SetActive(true);
        if (introPos != null)
        {
            introPos.PositionCanvasFront();
        }

        IntroButton.SetActive(false);
        IntroAnswerUI.SetActive(true);
        IntroNextButton.SetActive(false);
    }

    #endregion

    #region Eye Closed Check
    public void StartEyeClosedTest()
    {
        eyeClosed.StartChecking();
    }

    // create function to store the experiment mode and then the test results
    public void ProcessAnswer()
    {
        //create log
        ExperimentLogger.Instance?.LogEvent("Answer", "Answer_Phase", gameObject.name);
        LSL_Logger.Instance?.LogEvent("Answer", "Answer_Phase", gameObject.name);
        
        IntroNextButtonUI();
        
    }
    #endregion

#region  Test 3D Main
//if BCI mode, start the eye closed test and wait for user input
// if eye closed detect, then assign predict start log
// wait for LSL manager to get the prediction 
//after confirmation then manual trigger door for dwell + Flicker
// then move to door
// Final UI Close Experiment.
    public void StartTest3D()
    {
        
        if (IntroductionCanvas != null) IntroductionCanvas.SetActive(false);
        if (Test3DScene != null) Test3DScene.SetActive(true);
        MoveToPoint();
        Test3DMain();
    }

    public void Test3DMain()
    {
        if(IsBCIMode())
        {
            // StartEyeClosedTest();
            ExperimentLogger.Instance?.LogEvent("Predict_Start", "Prediction_Phase", "Predict_Active_Start");
            LSL_Logger.Instance?.LogEvent("Predict_Start", "Prediction_Phase", "Predict_Active_Start");
            
        }
    }

    private void MoveToPoint()
    {
        if (playerRig != null && spawnPoint != null)
        {
            playerRig.position = spawnPoint.position;
            playerRig.rotation = spawnPoint.rotation;
        }
    }

    #endregion


#region LSL
public void HandlePredictionLSL(BCIMessage msg)
{
    Debug.Log($"[Test3D] BCI Prediction received: {msg}");

    if (door1 != null && door1.doorCode != OB.DoorCode.None && msg.Code == (int)door1.doorCode)
    {
        door1.TriggerInteraction();
        eyeClosed.playPredictSound();
    }
    else if (door2 != null && door2.doorCode != OB.DoorCode.None && msg.Code == (int)door2.doorCode)
    {
        door2.TriggerInteraction();
        eyeClosed.playPredictSound();

    }

    ExperimentLogger.Instance?.LogEvent("Predict_End", "Prediction_Phase", "Predict_End");
    LSL_Logger.Instance?.LogEvent("Predict_End", "Prediction_Phase", "Predict_End");
}
#endregion

 #region Helpers

    /// <summary>
    /// Returns true when the current experiment mode requires LSL confirmation
    /// before executing an action (Hybrid or BCI).
    /// </summary>
    private bool IsBCIMode()
    {
        if (MainControl.Instance == null) return false;
        return currentMode == MainControl.ExperimentType.BCI;
    }
    #endregion

     public void NextPhase()
    {
        gameObject.SetActive(false);
        if (MainControl.Instance != null) MainControl.Instance.GoToNextPhase();            
       
    }
}
