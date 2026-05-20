using System.Collections;
using UnityEngine;
using ViveSR.anipal.Eye;

public class BlinkDetection : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Threshold below which the eye is considered closed (0.0 to 1.0)")]
    [Range(0f, 1f)]
    public float opennessThreshold = 0.1f;

    [Header("Status (Read Only)")]
    public bool isChecking = false;
    public bool lastEyesClosedState = false;

    private Coroutine blinkCoroutine;

    private void OnEnable()
    {
        StartChecking();
    }

    private void OnDisable()
    {
        StopChecking();
    }

    public void StartChecking()
    {
        if (blinkCoroutine != null) return;
        
        isChecking = true;
        lastEyesClosedState = false;
        blinkCoroutine = StartCoroutine(BlinkCheckRoutine());
        Debug.Log("[BlinkDetection] Started monitoring for eye blinks.");
    }

    public void StopChecking()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        isChecking = false;
        lastEyesClosedState = false;
        Debug.Log("[BlinkDetection] Stopped monitoring for eye blinks.");
    }

    private IEnumerator BlinkCheckRoutine()
    {
        while (true)
        {
            if (SRanipal_Eye_Framework.Status == SRanipal_Eye_Framework.FrameworkStatus.WORKING)
            {
                float leftOpenness, rightOpenness;
                bool leftValid = SRanipal_Eye_v2.GetEyeOpenness(EyeIndex.LEFT, out leftOpenness);
                bool rightValid = SRanipal_Eye_v2.GetEyeOpenness(EyeIndex.RIGHT, out rightOpenness);

                if (leftValid && rightValid)
                {
                    bool currentlyClosed = (leftOpenness <= opennessThreshold && rightOpenness <= opennessThreshold);
                    if (currentlyClosed)
                    {
                        if (!lastEyesClosedState)
                        {
                            lastEyesClosedState = true;
                            LSL_Logger.Instance?.LogEvent("Eye_Closed", "eye_tracking", "Eye_Closed");
                            ExperimentLogger.Instance?.LogEvent("Eye_Closed", "eye_tracking", "Eye_Closed");
                            Debug.Log("[BlinkDetection] Eye_Closed logged.");
                        }
                    }
                    else
                    {
                        if (lastEyesClosedState)
                        {
                            lastEyesClosedState = false;
                            LSL_Logger.Instance?.LogEvent("Eye_Opened", "eye_tracking", "Eye_Opened");
                            ExperimentLogger.Instance?.LogEvent("Eye_Opened", "eye_tracking", "Eye_Opened");
                            Debug.Log("[BlinkDetection] Eye_Opened logged.");
                        }
                    }
                }
                else
                {
                    if (lastEyesClosedState)
                    {
                        lastEyesClosedState = false;
                        LSL_Logger.Instance?.LogEvent("Eye_Opened", "eye_tracking", "Eye_Opened");
                        ExperimentLogger.Instance?.LogEvent("Eye_Opened", "eye_tracking", "Eye_Opened");
                    }
                }
            }
            yield return null;
        }
    }
}
