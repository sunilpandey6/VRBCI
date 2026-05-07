using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Events;
using LSL; // Requires liblsl-CSharp in the project

#region JSON Message Schema

/// <summary>
/// Remark payload nested inside every BCI message.
/// Fields are optional — unpopulated fields default to their C# zero-values.
/// </summary>
[Serializable]
public class BCIRemark
{
    public float  Detected_Frequency;
    public float  Confidence_Score;
    public bool   SSVEP_Present;
    public string Message;
    // Training fields
    public int    Epochs_Collected;
    public int    Target_Epochs;
    public string Object;
}

/// <summary>
/// Top-level JSON envelope sent by the Python BCI backend over LSL.
/// </summary>
[Serializable]
public class BCIMessage
{
    public int       Code;    // Protocol integer (100, 101, 201 …)
    public string    Event;   // Unity event label forwarded from LSL markers
    public string    Detail;  // Unity detail label forwarded from LSL markers
    public BCIRemark Remark;  // BCI-specific analysis results
}

#endregion

/// <summary>
/// Central manager that receives integer commands from the Python BCI backend
/// over Lab Streaming Layer (LSL) and dispatches them as typed Unity events.
///
/// Attach this to a persistent GameObject in your scene (e.g. "Managers").
/// Subscribe to the public events from any other script — no LSL knowledge needed.
/// </summary>
public class LSLCommunicationManager : MonoBehaviour
{
    #region Singleton

    /// <summary>Global singleton reference — access via LSLCommunicationManager.Instance.</summary>
    public static LSLCommunicationManager Instance { get; private set; }

    #endregion

    #region Inspector Configuration

    [Header("LSL Stream Settings")]
    [Tooltip("Name of the LSL stream sent by the Python backend (must match).")]
    public string streamName = "BCIBackend";

    [Tooltip("LSL stream type tag (must match Python outlet type).")]
    public string streamType = "BCIResult";

    [Tooltip("Seconds to wait for a stream before timing out during resolution.")]
    public float resolveTimeout = 3f;

    [Tooltip("Seconds between automatic reconnect attempts when the stream is lost.")]
    public float reconnectInterval = 5f;

    #endregion

    #region Inspector Debug / Status (read-only)

    [Header("Debug / Status (read-only)")]
    [Tooltip("True while an active LSL inlet is open.")]
    [SerializeField] private bool isConnected = false;

    [Tooltip("The most recent integer code received from Python.")]
    [SerializeField] private int lastReceivedCode = -1;

    [Tooltip("Total messages successfully processed this session.")]
    [SerializeField] private int totalMessagesProcessed = 0;

    [Header("Status")]
    [Tooltip("True if LSL inlet should retry to open after failure.")]
    [SerializeField] private bool shouldRetry = false;

    #endregion

    #region Public Events

    /// <summary>
    /// Fired when the flicker detection state changes.
    /// bool  param : true = FLICKER_DETECTED (100), false = FLICKER_NOT_DETECTED (101).
    /// BCIMessage  : full JSON envelope from Python, used by BB for Event/Detail ownership check.
    ///
    /// Example subscription:
    ///   LSLCommunicationManager.Instance.OnFlickerStateChanged += HandleFlicker;
    ///   void HandleFlicker(BCIMessage msg) { ... }
    /// </summary>
    public event Action<BCIMessage> OnFlickerStateChanged;

    /// <summary>
    /// Fired when a training epoch for an object completes.
    /// int param: 1 = Object 1 complete (201), 2 = Object 2 complete (202).
    /// </summary>
    public event Action<BCIMessage> OnTrainingEvent;

    /// <summary>
    /// Fired when the Python model outputs a prediction result.
    /// int param: 1 = Object 1 predicted (300), 2 = Object 2 predicted (301).
    /// </summary>
    public event Action<BCIMessage> OnPredictionResult;

    // UnityEvent wrappers — assignable from the Inspector without code
    [Header("UnityEvent Wrappers (assignable in Inspector)")]
    public UnityEvent<BCIMessage>  OnFlickerStateChangedUnity;
    public UnityEvent<BCIMessage>   OnTrainingEventUnity;
    public UnityEvent<BCIMessage>   OnPredictionResultUnity;

    #endregion

    #region Protocol Enum

    /// <summary>
    /// Integer codes sent by the Python backend.
    /// Keep in sync with the Python protocol definition.
    /// </summary>
    public enum BCICommand
    {
        FlickerDetected         = 100,
        FlickerNotDetected      = 101,
        TrainObj1ActiveComplete = 201,
        TrainObj2ActiveComplete = 202,
        TrainObj1ImageryComplete= 203,
        TrainObj2ImageryComplete= 204,
        PredictResultActiveObj1 = 300,
        PredictResultActiveObj2 = 301,
        PredictResultImageryObj1= 302,
        PredictResultImageryObj2= 303,
    }

    #endregion

    #region Private Fields

    // LSL objects — only accessed from the polling coroutine (effectively single-threaded
    // in Unity since coroutines run on the main thread each frame)
    private StreamInlet  _inlet;
    private StreamInfo[] _resolvedStreams;

    // Thread-safe queue: the polling coroutine enqueues; Update() dequeues & dispatches
    // Queue carries fully parsed BCIMessage objects so Update() never touches raw JSON.
    private readonly ConcurrentQueue<BCIMessage> _messageQueue = new ConcurrentQueue<BCIMessage>();

    // Reusable string buffer — Python sends JSON strings, not bare integers
    private readonly string[] _sampleBuffer = new string[1];

    // Reconnect state
    private bool _isAttemptingReconnect = false;
    private Coroutine _pollingCoroutine;
    private Coroutine _reconnectCoroutine;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // ── Singleton guard ──────────────────────────────────────────────────
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[LSLCommMgr] Duplicate instance detected — destroying extra.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scene loads
    }

    private void Start()
    {
        // Kick off the initial stream resolution attempt
        _pollingCoroutine = StartCoroutine(ResolveAndPoll());
    }

    /// <summary>
    /// Drains the concurrent queue and dispatches events on the Unity Main Thread.
    /// Deliberately kept lightweight — no blocking calls here.
    /// </summary>
    private void Update()
    {
        // Process up to all queued messages per frame (typically 0 or 1)
        while (_messageQueue.TryDequeue(out BCIMessage msg))
        {
            lastReceivedCode = msg.Code;
            totalMessagesProcessed++;
            DispatchCommand(msg);
        }
    }

    private void OnDestroy()
    {
        CloseInlet();
    }

    private void OnApplicationQuit()
    {
        CloseInlet();
    }

    #endregion

    #region Stream Resolution & Polling Coroutine

    /// <summary>
    /// Resolves the LSL stream, opens an inlet, then polls every frame.
    /// Uses <c>yield return null</c> to stay on the Main Thread and never block.
    /// </summary>
    private IEnumerator ResolveAndPoll()
    {
        Debug.Log($"[LSLCommMgr] Resolving stream '{streamName}' (type: '{streamType}')…");

        // ── Resolve ─────────────────────────────────────────────────────────
        _resolvedStreams = null;
        bool resolved = false;

        try
        {
            // LSL.LSL.resolve_stream is blocking but typically fast (<1 s).
            // We call it here in Start's coroutine so the rest of the app is
            // already running. For true non-blocking resolve, run this on a
            // background thread and push the result back via the queue.
            _resolvedStreams = LSL.LSL.resolve_stream("name", streamName, 1, resolveTimeout);
            resolved = _resolvedStreams != null && _resolvedStreams.Length > 0;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LSLCommMgr] Stream resolution failed: {ex.Message}");
        }

        if (!resolved)
        {
            Debug.LogWarning($"[LSLCommMgr] No stream found — scheduling reconnect in {reconnectInterval}s.");
            isConnected = false;
            yield return ScheduleReconnect();
            yield break;
        }

        // ── Open Inlet ──────────────────────────────────────────────────────
        try
        {
            _inlet = new StreamInlet(_resolvedStreams[0]);
            _inlet.open_stream(); // throws on failure
            isConnected = true;
            Debug.Log($"[LSLCommMgr] ✓ Connected to LSL stream '{streamName}'.");

            // Log the connection event so it appears in the experiment CSV + LSL outlet
            ExperimentLogger.Instance?.LogEvent("LSL_Connected", streamName, "BCI_Stream_Open");
            LSL_Logger.Instance?.LogEvent("LSL_Connected", streamName, "BCI_Stream_Open");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LSLCommMgr] Failed to open inlet: {ex.Message}");
            isConnected = false;
            shouldRetry = true;
        }
        if (shouldRetry)
        {
            yield return ScheduleReconnect();
            yield break;
        }

        // ── Polling Loop ─────────────────────────────────────────────────────
        // yield return null surrenders control each frame — non-blocking.
        while (_inlet != null && isConnected)
        {
            yield return null; // Wait one frame before pulling

            try
            {
                // pull_sample with timeout=0 returns immediately if no data
                double timestamp = _inlet.pull_sample(_sampleBuffer, 0.0f);

                if (timestamp > 0.0 && !string.IsNullOrEmpty(_sampleBuffer[0]))
                {
                    // Parse the JSON envelope sent by the Python backend
                    BCIMessage msg = TryParseMessage(_sampleBuffer[0]);
                    if (msg != null)
                        _messageQueue.Enqueue(msg);
                }
            }
            catch (ObjectDisposedException)
            {
                // Inlet was closed externally — exit gracefully
                Debug.LogWarning("[LSLCommMgr] Inlet disposed mid-poll — stopping poll loop.");
                break;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LSLCommMgr] poll_sample error: {ex.Message} — scheduling reconnect.");
                break;
            }
        }

        // If we exited the loop without being destroyed, the stream was lost
        if (this != null && isActiveAndEnabled)
        {
            isConnected = false;
            Debug.LogWarning("[LSLCommMgr] Stream lost — scheduling reconnect.");
            ExperimentLogger.Instance?.LogEvent("LSL_Disconnected", streamName, "BCI_Stream_Lost");
            yield return ScheduleReconnect();
        }
    }

    #endregion

    #region Reconnect Logic

    /// <summary>
    /// Waits <see cref="reconnectInterval"/> seconds then restarts the poll coroutine.
    /// Ensures only one reconnect attempt is in-flight at a time.
    /// </summary>
    private IEnumerator ScheduleReconnect()
    {
        if (_isAttemptingReconnect) yield break;
        _isAttemptingReconnect = true;

        CloseInlet(); // Tidy up any stale inlet

        Debug.Log($"[LSLCommMgr] Reconnecting in {reconnectInterval}s…");
        yield return new WaitForSeconds(reconnectInterval);

        _isAttemptingReconnect = false;

        if (this != null && isActiveAndEnabled)
        {
            _pollingCoroutine = StartCoroutine(ResolveAndPoll());
        }
    }

    #endregion

    #region Command Dispatch

    /// <summary>
    /// Routes a parsed <see cref="BCIMessage"/> to the correct event.
    /// All logic executes on the Unity Main Thread — no blocking calls.
    /// </summary>
    private void DispatchCommand(BCIMessage msg)
    {
        int code = msg.Code;

        // ── Special case: Python connection handshake ─────────────────────
        // Python calls send_test_message() on startup to verify the LSL link.
        // We surface this as a prominent green log so it is unmissable in the
        // Unity Console, then return early — no game-state change needed.
        if (code == (int)BCICommand.FlickerDetected &&
            string.Equals(msg.Event, "Test_Connection", StringComparison.OrdinalIgnoreCase))
        {
            string connMsg = msg.Remark?.Message ?? "(no message)";
            float  freq    = msg.Remark?.Detected_Frequency ?? 0f;
            float  conf    = msg.Remark?.Confidence_Score   ?? 0f;

            Debug.Log(
                $"<color=green><b>[LSLCommMgr] ✅ Python BCI backend connection established!</b></color>\n" +
                $"  Detail      : {msg.Detail}\n" +
                $"  Frequency   : {freq} Hz\n" +
                $"  Confidence  : {conf:P0}\n" +
                $"  Message     : {connMsg}"
            );

            ExperimentLogger.Instance?.LogEvent("Python_Connected", msg.Detail, connMsg);
            LSL_Logger.Instance?.LogEvent("Python_Connected", msg.Detail, connMsg);
            return; // Handshake only — do NOT fire gameplay events
        }

        // ── Experiment-mode gate ──────────────────────────────────────────
        // Flicker events are only meaningful in Hybrid and BCI modes.
        // In EyeTracking mode BB executes immediately after flicker — LSL
        // responses must not arrive and re-trigger the action a second time.
        var exp = MainControl.Instance?.currentExperiment;
        bool isBCIOrHybrid = exp == MainControl.ExperimentType.BCI ||
                             exp == MainControl.ExperimentType.Hybrid;

        if ((code == (int)BCICommand.FlickerDetected ||
             code == (int)BCICommand.FlickerNotDetected) && !isBCIOrHybrid)
        {
            Debug.Log($"[LSLCommMgr] Flicker code {code} ignored — experiment mode is {exp}.");
            return;
        }

        // ── Standard dispatch ─────────────────────────────────────────────
        Debug.Log($"[LSLCommMgr] Received code: {code} ({(BCICommand)code})");

        // Log to CSV and LSL outlet — mirroring the experiment logging pattern
        ExperimentLogger.Instance?.LogEvent("BCI_Command_Received", code.ToString(), ((BCICommand)code).ToString());
        LSL_Logger.Instance?.LogEvent("BCI_Command_Received", code.ToString(), ((BCICommand)code).ToString());

        switch (code)
        {
            // ── Flicker Detection ─────────────────────────────────────────
            // Full BCIMessage is forwarded so subscribers (BB) can verify
            // Event/Detail ownership before acting on the result.
            case (int)BCICommand.FlickerDetected:
                OnFlickerStateChanged?.Invoke(msg);
                OnFlickerStateChangedUnity?.Invoke(msg);  // Inspector wiring: bool only
                break;

            case (int)BCICommand.FlickerNotDetected:
                OnFlickerStateChanged?.Invoke(msg);
                OnFlickerStateChangedUnity?.Invoke(msg); // Inspector wiring: bool only
                break;

            // ── Training Complete ─────────────────────────────────────────
            case (int)BCICommand.TrainObj1ActiveComplete:
                OnTrainingEvent?.Invoke(msg);
                OnTrainingEventUnity?.Invoke(msg);
                break;

            case (int)BCICommand.TrainObj2ActiveComplete:
                OnTrainingEvent?.Invoke(msg);
                OnTrainingEventUnity?.Invoke(msg);
                break;
            case (int)BCICommand.TrainObj1ImageryComplete:
                OnTrainingEvent?.Invoke(msg);
                OnTrainingEventUnity?.Invoke(msg);
                break;
            case (int)BCICommand.TrainObj2ImageryComplete:
                OnTrainingEvent?.Invoke(msg);
                OnTrainingEventUnity?.Invoke(msg);
                break;

            // ── Prediction Result ─────────────────────────────────────────
            case (int)BCICommand.PredictResultImageryObj1:
                OnPredictionResult?.Invoke(msg);
                OnPredictionResultUnity?.Invoke(msg);
                break;

            case (int)BCICommand.PredictResultImageryObj2:
                OnPredictionResult?.Invoke(msg);
                OnPredictionResultUnity?.Invoke(msg);
                break;

            case (int)BCICommand.PredictResultActiveObj1:
                OnPredictionResult?.Invoke(msg);
                OnPredictionResultUnity?.Invoke(msg);
                break;
            case (int)BCICommand.PredictResultActiveObj2:
                OnPredictionResult?.Invoke(msg);
                OnPredictionResultUnity?.Invoke(msg);
                break;

            // ── Unknown Code ──────────────────────────────────────────────
            default:
                Debug.LogWarning($"[LSLCommMgr] Unknown BCI command code: {code} — ignoring.");
                break;
        }
    }

    /// <summary>
    /// Deserialises a raw JSON string from the LSL sample buffer into a
    /// <see cref="BCIMessage"/>. Returns <c>null</c> on any parse failure so
    /// the caller can skip malformed packets gracefully.
    /// </summary>
    private static BCIMessage TryParseMessage(string raw)
    {
        try
        {
            BCIMessage msg = JsonUtility.FromJson<BCIMessage>(raw);
            if (msg == null)
            {
                Debug.LogWarning($"[LSLCommMgr] JSON deserialized to null. Raw: {raw}");
                return null;
            }
            return msg;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LSLCommMgr] Failed to parse JSON: {ex.Message} | Raw: {raw}");
            return null;
        }
    }

    #endregion

    #region Helpers

    /// <summary>Safely closes and nullifies the LSL inlet.</summary>
    private void CloseInlet()
    {
        if (_inlet == null) return;

        try
        {
            _inlet.close_stream();
            _inlet = null;
            isConnected = false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LSLCommMgr] Error closing inlet: {ex.Message}");
            _inlet = null;
            isConnected = false;
        }
    }

    #endregion

    #region Public Utilities

    /// <summary>
    /// Manually inject a BCI command integer (for in-Editor testing without a
    /// live Python backend). Call from a test script or the Unity Inspector button.
    /// </summary>
    /// <param name="code">Integer code from the BCICommand protocol.</param>
    /// <param name="code">Integer code from the BCICommand protocol.</param>
    /// <param name="unityEvent">Optional event label (use "Test_Connection" to trigger the handshake log).</param>
    public void SimulateCommand(int code, string unityEvent = "")
    {
        BCIMessage msg = new BCIMessage
        {
            Code   = code,
            Event  = unityEvent,
            Detail = "Simulated",
            Remark = new BCIRemark { Message = "Injected from SimulateCommand()" }
        };
        Debug.Log($"[LSLCommMgr] SIMULATED command: {code} event='{unityEvent}'");
        _messageQueue.Enqueue(msg);
    }

    /// <summary>Returns true if the LSL inlet is currently open and healthy.</summary>
    public bool IsConnected => isConnected;

    /// <summary>Returns the last integer code that was successfully dequeued.</summary>
    public int LastReceivedCode => lastReceivedCode;
    #endregion
}
