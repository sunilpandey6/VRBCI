using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TestUI : MonoBehaviour
{
    [Header("UI Value")]
    public TMP_Text value;
    
    [Header("UI Panels")]
    public GameObject Introduction;
    public GameObject testCanvas;

    [Header("Test panel Next button")]
    public GameObject nextButton;

    [Header("Input Value")]
    private string currentText = "";


    [Header("Experiment Mode")]
    public MainControl.ExperimentType currentMode;

    [Header("Instruction UI")]
    public TMP_Text headingText;
    [Header("Instruction UI")]
    public TMP_Text instructionText;

    [Header("Instruction Heading Texts")]
    [TextArea] public string eyeTrackingHeading;
    [TextArea] public string hybridHeading;

    [Header("Instruction Main Texts")]
    [TextArea] public string eyeTrackingInstruction;
    [TextArea] public string hybridInstruction;

#region Unity Lifecycle
    private void OnEnable()
    {
        PositionCanvasFront();
        currentMode = MainControl.Instance.currentExperiment;
        DisplayInitialPanels();
    }

    private void OnDisable()
    {
        if (Introduction != null) Introduction.SetActive(false);
        if (testCanvas != null) testCanvas.SetActive(false);
    }
#endregion

#region UI Positioning
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
#endregion

#region Intro Panel
    void DisplayInitialPanels()
    {
        if (Introduction != null) Introduction.SetActive(true);
        if (testCanvas != null) testCanvas.SetActive(false);
        UpdateHeadingText();
        UpdateInstructionText();
    }

    private void UpdateHeadingText()
    {
        if (headingText == null) return;

        if (currentMode == MainControl.ExperimentType.EyeTracking)
            headingText.text = eyeTrackingHeading;
        else if (currentMode == MainControl.ExperimentType.Hybrid)
            headingText.text = hybridHeading;
    }
    private void UpdateInstructionText()
    {
        if (instructionText == null) return;

        if (currentMode == MainControl.ExperimentType.EyeTracking)
            instructionText.text = eyeTrackingInstruction;
        else if (currentMode == MainControl.ExperimentType.Hybrid)
            instructionText.text = hybridInstruction;
    }
#endregion

#region Panel Switching
    public void StartTestCanvas()
    {
        if (Introduction != null) Introduction.SetActive(false);
        if (testCanvas != null) testCanvas.SetActive(true);
        UpdateDisplay();
    }
#endregion

#region UI Test Canvas

    void UpdateDisplay()
    {
        if (value != null)
            value.text = currentText;
            
        if (nextButton != null)
            nextButton.SetActive(!string.IsNullOrEmpty(currentText));
    }
    
    public void AddDigit(string digit)
    {
        currentText += digit;
        UpdateDisplay();
    }

    public void RemoveDigitLast()
    {
        if (currentText.Length > 0)
        {
            currentText = currentText.Substring(0, currentText.Length - 1);
            UpdateDisplay();
        }
    }

    public void NextPhase()
    {
        if (MainControl.Instance != null) MainControl.Instance.GoToNextPhase();
        gameObject.SetActive(false);
    }
#endregion
}
