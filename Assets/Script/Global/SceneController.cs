using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneController : MonoBehaviour
{
    [Header("Phase GameObjects (Assign in Inspector)")]
    [Tooltip("The parent GameObject for the Setting UI")]
    public GameObject settingUIObject;
    
    [Tooltip("The parent GameObject for the Test UI")]
    public GameObject testUIObject;
    
    [Tooltip("The parent GameObject for the Demo 3D Environment")]
    public GameObject demo3DObject;
    
    [Tooltip("The parent GameObject for the Train BCI Environment")]
    public GameObject trainBCIObject;
    
    [Tooltip("The parent GameObject for the Test 3D Environment")]
    public GameObject test3DObject;

    private void Start()
    {
        // 1. Listen to MainControl
        if (MainControl.Instance != null)
        {
            MainControl.Instance.OnPhaseChanged += UpdateSceneForPhase;
            
            // Force an initial update to sync with the current phase on startup
            UpdateSceneForPhase(MainControl.Instance.currentPhase);
        }
        else
        {
            Debug.LogError("SceneController couldn't find MainControl.Instance! Ensure MainControl exists in the scene.");
        }
    }

    private void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks when this object is destroyed or scene changes
        if (MainControl.Instance != null)
        {
            MainControl.Instance.OnPhaseChanged -= UpdateSceneForPhase;
        }
    }

    private void UpdateSceneForPhase(MainControl.ExperimentPhase newPhase)
    {
        // 2. Turn OFF everything
        TurnOffAll();

        // 3. Turn ON correct objects
        switch (newPhase)
        {
            case MainControl.ExperimentPhase.SettingUI:
                if (settingUIObject != null) settingUIObject.SetActive(true);
                break;
            case MainControl.ExperimentPhase.TestUI:
                if (testUIObject != null) testUIObject.SetActive(true);
                break;
            case MainControl.ExperimentPhase.Demo3D:
                if (demo3DObject != null) demo3DObject.SetActive(true);
                break;
            case MainControl.ExperimentPhase.TrainBCI:
                if (trainBCIObject != null) trainBCIObject.SetActive(true);
                break;
            case MainControl.ExperimentPhase.Test3D:
                if (test3DObject != null) test3DObject.SetActive(true);
                break;
            case MainControl.ExperimentPhase.Completed:
                // Optional: Show a "Thank you" UI or an End Screen
                Debug.Log("SceneController: All experiments completed.");
                break;
        }
    }

    private void TurnOffAll()
    {
        if (settingUIObject != null) settingUIObject.SetActive(false);
        if (testUIObject != null) testUIObject.SetActive(false);
        if (demo3DObject != null) demo3DObject.SetActive(false);
        if (trainBCIObject != null) trainBCIObject.SetActive(false);
        if (test3DObject != null) test3DObject.SetActive(false);
    }
}
