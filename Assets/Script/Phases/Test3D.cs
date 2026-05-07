using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EyeClosed))]
[RequireComponent(typeof(AutoWalk))]
public class Test3D : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField] private AutoWalk autoWalk;
    [SerializeField] private EyeClosed eyeClosed;

    [Tooltip("The UI Canvas shown before and after training")]
    public GameObject IntroductionCanvas;
    [Header("Intro Canvas child Panel")]
    public GameObject IntroButton;
    public GameObject IntroNextButton;
    [Header("Scene Reference")]
    public GameObject Test3DScene;

    [Header("Door 1")]
    [SerializeField] private OB door1;
    [Header("Door 2")]
    [SerializeField] private OB door2;

#region Unity Lifecycle
    private void Awake()
    {
        if(autoWalk == null) autoWalk = GetComponent<AutoWalk>();
        if(eyeClosed == null) eyeClosed = GetComponent<EyeClosed>();
    }

    private void OnValidate(){
        autoWalk = GetComponent<AutoWalk>();
        eyeClosed = GetComponent<EyeClosed>();
    }

    void OnEnable()
    {
        if (LSLCommunicationManager.Instance != null)
            LSLCommunicationManager.Instance.OnPredictionResult += HandlePredictionLSL;
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

#region Eye Closed Check
    public void StartEyeClosedTest()
    {
        eyeClosed.StartChecking();
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
        Test3DMain();
    }

    public void Test3DMain()
    {
        if(IsBCIMode())
        {
            StartEyeClosedTest();
            
        }
    }
    
#endregion
#region Walk to Door
    public void walk(int code)
    {
        autoWalk.MoveToTarget(code);
    }
#endregion

#region Introduction
    public void ShowIntro()
    {
        if (IntroductionCanvas != null) IntroductionCanvas.SetActive(true);
        if (Test3DScene != null) Test3DScene.SetActive(false);
        StartIntroButtonUI();
    }

     public void StartIntroButtonUI() 
    { 
        if (IntroductionCanvas == null) return;
        IntroButton.SetActive(true);
        IntroNextButton.SetActive(false);


    }


     public void IntroNextButtonUI() 
    {
        if (IntroductionCanvas == null) return;
        IntroductionCanvas.SetActive(true);
        IntroButton.SetActive(false);
        IntroNextButton.SetActive(true);
    }
    
#endregion

#region LSL
public void HandlePredictionLSL(BCIMessage msg)
{
    Debug.Log($"[Test3D] BCI Prediction received: {msg}");

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
        var exp = MainControl.Instance.currentExperiment;
        return exp == MainControl.ExperimentType.BCI;
    }
    #endregion

     public void NextPhase()
    {
        if (MainControl.Instance != null) MainControl.Instance.GoToNextPhase();            
        gameObject.SetActive(false);
    }
}
