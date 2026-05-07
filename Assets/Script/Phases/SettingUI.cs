using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SettingUI : MonoBehaviour
{  
    [Header("UI Panels")]
    public GameObject IntroPanel;
    public GameObject DwellPanel;
    public GameObject FlickerPanel;
    
    [Header("Dwell Setting References")]
    public TMP_Text dwellTimeText;

    [Header("Flicker Setting References")]
    public TMP_Text flickerHzText;
    public TMP_Text flickerTimeText;

    #region Unity Lifecycle
    private void Start()
    {
        PositionCanvasFront();
        IntroPanelOn();
        StartIntroButton();
        UpdateAllValues();
    }

    private void OnDisable()
    {
        if (IntroPanel != null) IntroPanel.SetActive(false);
        if (DwellPanel != null) DwellPanel.SetActive(false);
        if (FlickerPanel != null) FlickerPanel.SetActive(false);
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

    #region Panel Switching
    public void IntroPanelOn()
    {
        if (IntroPanel != null) IntroPanel.SetActive(true);
        if (DwellPanel != null) DwellPanel.SetActive(false);
        if (FlickerPanel != null) FlickerPanel.SetActive(false);
    }
    
    public void DwellPanelOn()
    {
        if (DwellPanel != null) DwellPanel.SetActive(true);
        if (FlickerPanel != null) FlickerPanel.SetActive(false);
        if (IntroPanel != null) IntroPanel.SetActive(false);
    }

    public void FlickerPanelOn()
    {
        if (DwellPanel != null) DwellPanel.SetActive(false);
        if (FlickerPanel != null) FlickerPanel.SetActive(true);
        if (IntroPanel != null) IntroPanel.SetActive(false);
    }

    public void NextPhase()
    {
        if (MainControl.Instance != null) MainControl.Instance.GoToNextPhase();
        gameObject.SetActive(false);
    }
    #endregion

    #region Intro Actions
    public void StartIntroButton() 
    { 
        BB[] buttons = IntroPanel.GetComponentsInChildren<BB>(true); 
        foreach (var b in buttons) 
        {
            if (b.gameObject.name == "Intro-Next-UI") b.gameObject.SetActive(false); 
        } 
    }

    public void IntroNextButton() 
    {
         BB[] buttons = IntroPanel.GetComponentsInChildren<BB>(true); 
         foreach (var b in buttons) 
         {
            if (b.gameObject.name == "Intro-Next-UI") b.gameObject.SetActive(true); 
         } 
    }
    #endregion
    
    #region Value Updates
    public void UpdateAllValues()
    {
        UpdateDwellValue();
        UpdateFlickerValues();
    }

    private void UpdateDwellValue()
    {
        if (dwellTimeText != null)
            dwellTimeText.text = GlobalInput.Instance.dwellTime.ToString("0.0") + " Sec";
    }

    private void UpdateFlickerValues()
    {
        if (flickerTimeText != null)
            flickerTimeText.text = GlobalInput.Instance.flickerDuration.ToString("0.0") + " Sec";
        if (flickerHzText != null)
            flickerHzText.text = GlobalInput.Instance.flickerHz.ToString("0.0") + " Hz";
    }
    #endregion

    #region Dwell Settings Controls
    public void addDwellTIme()
    {
        if (GlobalInput.Instance.dwellTime < 5f)
        {
            GlobalInput.Instance.dwellTime += 0.5f;
            UpdateDwellValue();
        }
    }
    public void subDwellTIme()
    {
        if (GlobalInput.Instance.dwellTime > 1.0f)
        {
            GlobalInput.Instance.dwellTime -= 0.5f;
            UpdateDwellValue();
        }
    }
    #endregion

    #region Flicker Settings Controls
    public void addhz()
    {
        if (GlobalInput.Instance.flickerHz < 30f)
        {
            GlobalInput.Instance.flickerHz += 1.0f;
            UpdateFlickerValues();
        }
    }
    public void subhz()
    {
        if (GlobalInput.Instance.flickerHz > 12f)
        {
            GlobalInput.Instance.flickerHz -= 1.0f;
            UpdateFlickerValues();
        }
    }

    public void addTime()
    {
        if (GlobalInput.Instance.flickerDuration < 5f)
        {
            GlobalInput.Instance.flickerDuration += 0.5f;
            UpdateFlickerValues();
        }
    }

    public void subTime()
    {
        if (GlobalInput.Instance.flickerDuration > 1.5f)
        {
            GlobalInput.Instance.flickerDuration -= 0.5f;
            UpdateFlickerValues();
        }
    }
    #endregion
}
