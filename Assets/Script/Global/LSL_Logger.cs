using UnityEngine;
using System;
using LSL; // Requires LSL4Unity / liblsl-unity plugin in the project

/// </summary>
public class LSL_Logger : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static LSL_Logger Instance { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Tooltip("Name of the LSL outlet stream (must match what the Python receiver listens for).")]
    public string streamName = "Unity_Markers";

    [Tooltip("LSL stream type tag.")]
    public string streamType = "Markers";

    [Tooltip("LSL stream channel count.")]
    public string streamId = "unity_experiment_01";

    // ─── Private ──────────────────────────────────────────────────────────────
    private StreamOutlet outlet;
    private string[] sample = new string[1]; // reused buffer to avoid GC pressure

    // ─── Unity Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        // ── Singleton guard ──
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ── Create LSL outlet ──
        // channel_count = 1  (one string channel)
        // channel_format = cf_string
        // source_id is a unique identifier so Python can find this stream reliably
        string sourceId = $"UnityMarkers_{SystemInfo.deviceUniqueIdentifier}";
        StreamInfo info = new StreamInfo(
            name: streamName,
            type: streamType,
            channel_count: 1,
            nominal_srate: LSL.LSL.IRREGULAR_RATE,
            channel_format: channel_format_t.cf_string,
            source_id: $"{streamId}_{sourceId}"
        );

        outlet = new StreamOutlet(info);

        Debug.Log($"[LSL_Logger] Outlet created — stream: '{streamName}', type: '{streamType}', source_id: '{sourceId}'");
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Sends an LSL marker with the same structure as ExperimentLogger.LogEvent.
    ///
    /// Marker string format:
    ///   "yyyy-MM-dd HH:mm:ss.fff,Experiment,Phase,Event,Detail,Action"
    ///
    /// Parameters mirror ExperimentLogger.LogEvent exactly so callers can
    /// invoke both loggers with identical arguments.
    /// </summary>
    /// <param name="eventName">Event label  — e.g. "Dwell_Complete"</param>
    /// <param name="detail">    Detail label — e.g. "Button1"</param>
    /// <param name="action">    Action label — e.g. "Classify_Dwelling"</param>
    public void LogEvent(string eventName, string detail = "", string action = "")
    {
        if (outlet == null) return;

        // 1. Timestamp (matches ExperimentLogger format)
        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        // 2 & 3. Experiment and Phase pulled from MainControl (same as ExperimentLogger)
        string experiment = MainControl.Instance != null
            ? MainControl.Instance.currentExperiment.ToString()
            : "None";
        string phase = MainControl.Instance != null
            ? MainControl.Instance.currentPhase.ToString()
            : "None";

        // Sanitize commas (same logic as ExperimentLogger — CSV-safe)
        detail    = detail.Replace(",", ";");
        action    = action.Replace(",", ";");
        eventName = eventName.Replace(",", ";");

        // Build the marker string (identical columns to the CSV header)
        // Header: Time,Experiment,Phase,Event,Detail,Action
        sample[0] = $"{time},{experiment},{phase},{eventName},{detail},{action}";

        // Push to LSL
        outlet.push_sample(sample,LSL.LSL.local_clock());

        Debug.Log($"[LSL_Logger] Marker sent: {sample[0]}");
    }

    // ─── Cleanup ─────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (outlet != null)
        {
            outlet.Close();
            outlet = null;
        }
    }
}
