using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Demo3D : MonoBehaviour
{
    #region Variables
    [Header("Demo 3D UI Reference")]
    [SerializeField] private GameObject IntroPanel;
    [SerializeField] private GameObject Demo3DCanvas;

    [Header("Instruction UI")]
    public TMP_Text headingText;
    [Header("Instruction UI")]
    public TMP_Text instructionText;

    [Header("Instruction Heading Texts")]
    [TextArea] public string HeadingIntro;

    [Header("Instruction Main Texts")]
    [TextArea] public string InstructionTextDemo;

    [Header("Door Reference")]
    [SerializeField] private GameObject Door1;
    [SerializeField] private GameObject Door2;
    #endregion

    #region Unity Lifecycle
    private void OnEnable()
    {
        PositionCanvasFront();
        ShowIntroPanel();
    }

    private void OnDisable()
    {
        if (IntroPanel != null) IntroPanel.SetActive(false);
        if (Demo3DCanvas != null) Demo3DCanvas.SetActive(false);
        if (Door1 != null) Door1.SetActive(false);
        if (Door2 != null) Door2.SetActive(false);
    }

    public void ShowIntroPanel()
    {
        if (IntroPanel != null) IntroPanel.SetActive(true);
        if (Demo3DCanvas != null) Demo3DCanvas.SetActive(false);
        if (Door1 != null) Door1.SetActive(false);
        if (Door2 != null) Door2.SetActive(false);

        UpdateHeadingText();
        UpdateInstructionText();
    }

    private void UpdateHeadingText()
    {
        if (headingText == null) return;
        headingText.text = HeadingIntro;
    }

    private void UpdateInstructionText()
    {
        if (instructionText == null) return;
        instructionText.text = InstructionTextDemo;
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
    #endregion

    #region Door Switching
    public void ShowDemo3DPanel()
    {
        if (IntroPanel != null) IntroPanel.SetActive(false);
        if (Demo3DCanvas != null) Demo3DCanvas.SetActive(true);
        Door1Active();
    }

    public void Door1Active()
    {
        if (Door1 != null) Door1.SetActive(true);
        if (Door2 != null) Door2.SetActive(false);
    }
    
    public void Door2Active()
    {
        if (Door1 != null) Door1.SetActive(false);
        if (Door2 != null) Door2.SetActive(true);
    }
    #endregion

    #region Next Phase
    public void NextPhase()
    {
        if (MainControl.Instance != null) MainControl.Instance.GoToNextPhase();
        gameObject.SetActive(false);
    }
    #endregion
}
