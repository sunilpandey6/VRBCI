using UnityEngine;

public class EyeClosedTest : MonoBehaviour
{
    public EyeClosed eyeClosed;

    private void Start()
    {
        if (eyeClosed == null)
        {
            Debug.LogError("EyeClosed reference missing!");
            return;
        }

        // Listen for successful eye close detection
        eyeClosed.OnEyesClosedTriggered.AddListener(OnEyeClosedDetected);

        // Start the actual eye tracking check
        eyeClosed.StartChecking();

        Debug.Log("EyeClosed test started.");
    }

    private void OnEyeClosedDetected()
    {
        Debug.Log("SUCCESS: Eyes were closed long enough!");
    }

    private void OnDestroy()
    {
        if (eyeClosed != null)
        {
            eyeClosed.OnEyesClosedTriggered.RemoveListener(OnEyeClosedDetected);
        }
    }
}