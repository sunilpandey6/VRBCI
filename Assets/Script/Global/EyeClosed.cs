using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using ViveSR.anipal.Eye;

public class EyeClosed : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Threshold below which the eye is considered closed (0.0 to 1.0)")]
    [Range(0f, 1f)]
    public float opennessThreshold = 0.1f;
    
    [Tooltip("Duration in seconds that both eyes must remain closed to trigger the event")]
    public float requiredClosedDuration = 0.5f;

    [Header("Audio Feedback")]
    [Tooltip("AudioSource used to play the sound")]
    public AudioSource audioSource;
    [Tooltip("Sound to play when eyes are closed for the required duration")]
    public AudioClip closedSound;
    public AudioClip predictSound;


    [Header("Status (Read Only)")]
    public bool isChecking = false;
    public bool areEyesClosed = false;
    public float currentClosedTime = 0f;

    [Header("Events")]
    public UnityEvent OnEyesClosedTriggered;

    private bool hasTriggered = false;
    private Coroutine checkCoroutine;

#region Unity Lifecycle
    // private void OnEnable()
    // {   
    // }

    // private void OnDisable()
    // {
    // }
#endregion


    /// <summary>
    /// Starts actively checking if the user's eyes are closed.
    /// Call this function when you want to begin monitoring.
    /// </summary>
    public void StartChecking()
    {
        if (checkCoroutine != null) return; // Already checking
        
        isChecking = true;
        ResetState();
        checkCoroutine = StartCoroutine(CheckEyesClosedRoutine());
        Debug.Log("[EyeClosed] Started checking for closed eyes.");
    }

    /// <summary>
    /// Stops checking for closed eyes.
    /// </summary>
    public void StopChecking()
    {
        if (checkCoroutine != null)
        {
            StopCoroutine(checkCoroutine);
            checkCoroutine = null;
        }
        isChecking = false;
        ResetState();
        Debug.Log("[EyeClosed] Stopped checking for closed eyes.");
    }

    private IEnumerator CheckEyesClosedRoutine()
    {
        while (true)
        {
            // Ensure the SRanipal Eye framework is active and working before polling data
            if (SRanipal_Eye_Framework.Status == SRanipal_Eye_Framework.FrameworkStatus.WORKING)
            {
                float leftOpenness, rightOpenness;
                bool leftValid = SRanipal_Eye_v2.GetEyeOpenness(EyeIndex.LEFT, out leftOpenness);
                bool rightValid = SRanipal_Eye_v2.GetEyeOpenness(EyeIndex.RIGHT, out rightOpenness);

                if (leftValid && rightValid)
                {
                    // Check if both eyes are below the openness threshold
                    if (leftOpenness <= opennessThreshold && rightOpenness <= opennessThreshold)
                    {
                        currentClosedTime += Time.deltaTime;

                        // Trigger the event only once per continuous close duration
                        if (currentClosedTime >= requiredClosedDuration && !hasTriggered)
                        {
                            areEyesClosed = true;
                            hasTriggered = true;
                            
                            // Play the configured sound
                            if (audioSource != null && closedSound != null)
                            {
                                audioSource.PlayOneShot(closedSound);
                                ExperimentLogger.Instance?.LogEvent("Predict Door Imagery", "eye closed","Predict_Start_Imagery");
                                LSL_Logger.Instance?.LogEvent("Predict Door Imagery", "eye closed","Predict_Start_Imagery");
                            }

                            OnEyesClosedTriggered?.Invoke();
                            Debug.Log($"[EyeClosed] Both eyes have been closed for {requiredClosedDuration} seconds. Playing sound.");
                        }
                    }
                    else
                    {
                        // At least one eye is open or above threshold; reset timer
                        ResetState();
                    }
                }
                else
                {
                    // If eye tracking data is lost or invalid, reset the timer to prevent false positives
                    ResetState();
                }
            }

            yield return null; // Wait for the next frame
        }
    }

    public void playPredictSound()
    {
        if (audioSource != null && predictSound != null)
            audioSource.PlayOneShot(predictSound);
    }

    private void ResetState()
    {
        currentClosedTime = 0f;
        areEyesClosed = false;
        hasTriggered = false;
    }
}
