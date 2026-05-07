using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BB : MonoBehaviour
{
    [Header("Button Type")]
    [SerializeField] private Button button;
    public Att attribute;
    public enum Att
    {
        None,
        Normal,
        DwellDemo,
        FlickerDemo
    }

    // WaitingForLSL: flicker has finished; we are waiting for the Python
    // backend to confirm or reject the detection before executing the action.
    enum State
    {
        Idle,
        Hovering,
        Dwelling,
        Flickering,
        WaitingForLSL,
    }

    public enum ActionType
    {
        None,
        ButtonAction,
        TestUI
    }
    [SerializeField] private State currentState = State.Idle;

    [Header("Button as header")]
    [SerializeField] private Image outlineImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private Image buttonImage;

    [SerializeField] private RectTransform outlineRect;
    [SerializeField] private RectTransform borderRect;
    [SerializeField] private RectTransform buttonRect;

    private Material runtimeMaterial;
    private Material runtimeMaterialFlicker;

    [Header("Internal Value")]
    public bool isHovering = false;
    [SerializeField] private bool hasTriggered = false;
    [SerializeField] private float dwellTimer = 0f;

    // Ownership: which button is actively waiting for an LSL response.
    // Only one button can be waiting at a time across the whole scene.
    public static BB activeButton  = null;
    public static BB waitingButton = null;

    // Time-based flicker anchor
    private float flickerStartTime = -1f;

    [Header("Outline and Border Settings")]
    [SerializeField] private float outlineSize = 10f;
    [SerializeField] private float borderSize = 3f;

    [Header("Button Action")]
    [SerializeField] private ActionType selectedAction;

    // Unique identifier for this button — set in the Inspector.
    // Python echoes this back in BCIMessage.Detail so we can verify ownership.
    [Header("BCI Identity")]
    [Tooltip("Unique button ID sent as the LSL marker Detail. Must match what Python echoes back.")]
    [SerializeField] private string buttonId;

    // Last event and detail strings sent to LSL — used for response validation.
    private string lastEvent;
    private string lastDetail;

    // Retry limiting — prevents infinite flicker loops on repeated non-detection
    private int retryCount = 0;
    private const int maxRetries = 3;

    [Header("UI Control reference")]
    public string value;
    public bool isDelete;
    public bool isNext;
    public TestUI testUI;

    #region Unity Lifecycle
    void Awake()
    {
        if (!button)
            button = GetComponent<Button>();

        if (outlineImage)
            outlineRect.sizeDelta = buttonRect.sizeDelta + new Vector2(outlineSize * 2, outlineSize * 2);

        if (borderImage)
            borderRect.sizeDelta = buttonRect.sizeDelta + new Vector2(borderSize * 2, borderSize * 2);
        
        buttonId = gameObject.name;
    }

    void OnEnable()
    {
        if (outlineImage)
        {
            runtimeMaterial = new Material(outlineImage.material);
            outlineImage.material = runtimeMaterial;
            ApplyGlobalColors();
            outlineImage.gameObject.SetActive(false);
        }

        if (buttonImage)
        {
            runtimeMaterialFlicker = new Material(buttonImage.material);
            buttonImage.material = runtimeMaterialFlicker;
            ApplyFlickerColors();
        }

        // Reset runtime state
        dwellTimer       = 0f;
        hasTriggered     = false;
        flickerStartTime = -1f;

        // Subscribe to the LSL flicker event — unsubscribed in OnDisable
        if (LSLCommunicationManager.Instance != null)
            LSLCommunicationManager.Instance.OnFlickerStateChanged += HandleFlickerLSL;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent null-ref after scene unload
        if (LSLCommunicationManager.Instance != null)
            LSLCommunicationManager.Instance.OnFlickerStateChanged -= HandleFlickerLSL;

        runtimeMaterial         = null;
        runtimeMaterialFlicker  = null;

        if (activeButton  == this) activeButton  = null;
        if (waitingButton == this) waitingButton = null;

        StopAllCoroutines();
    }

    void ApplyGlobalColors()
    {
        runtimeMaterial.SetColor("_IdleColor",   GlobalInput.Instance.idleColor);
        runtimeMaterial.SetColor("_MidColor",    GlobalInput.Instance.midColor);
        runtimeMaterial.SetColor("_ActiveColor", GlobalInput.Instance.activeColor);
    }

    void ApplyFlickerColors()
    {
        runtimeMaterialFlicker.SetColor("_IdleColor",    GlobalInput.Instance.idleColor);
        runtimeMaterialFlicker.SetColor("_FlickerColor", GlobalInput.Instance.flickerOn);
    }

    public void Update()
    {
        switch (attribute)
        {
            case Att.None:
                HandleNone();
                break;
            case Att.Normal:
                if (isHovering && currentState != State.Flickering) ChangeColor();
                break;
            case Att.DwellDemo:
                if (isHovering && currentState != State.Flickering) HandleDwell();
                break;
            case Att.FlickerDemo:
                if (isHovering) HandleFlickerDemo();
                break;
        }

        if (currentState == State.Flickering)
            UpdateFlicker();
    }

    #endregion

    #region State Control

    void HandleNone()
    {
        if (outlineImage && !outlineImage.gameObject.activeSelf)
        {
            outlineImage.gameObject.SetActive(true);
            outlineImage.color = Color.yellow;
        }
    }

    void HandleDwell()
    {
        if (outlineImage && !outlineImage.gameObject.activeSelf) outlineImage.gameObject.SetActive(true);

        currentState = State.Dwelling;

        dwellTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(dwellTimer / GlobalInput.Instance.dwellTime);
        runtimeMaterial.SetFloat("_Progress", progress);

        if (progress >= 1f && currentState != State.Flickering && !hasTriggered)
        {
            hasTriggered = true;
            Execution(selectedAction);
        }
    }

    void HandleFlickerDemo()
    {
        if (isHovering)
        {
            currentState = State.Flickering;
            flickerStartTime = -1f;
        }
        else
        {
            currentState = State.Idle;
            flickerStartTime = -1f;
            if (runtimeMaterialFlicker != null)
                runtimeMaterialFlicker.SetFloat("_FlickerState", 0f);
        }
    }

    #endregion

    #region Dwell Functions

    public void ChangeColor()
    {
        if (outlineImage && !outlineImage.gameObject.activeSelf)
            outlineImage.gameObject.SetActive(true);

        ExperimentLogger.Instance?.LogEvent("Dwell_Start", $"Button: {gameObject.name}", "Dwell_Started");
        LSL_Logger.Instance?.LogEvent("Dwell_Start", $"Button: {gameObject.name}", "Dwell_Started");

        if (outlineImage)
        {
            dwellTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(dwellTimer / GlobalInput.Instance.dwellTime);
            runtimeMaterial.SetFloat("_Progress", progress);

            if (dwellTimer >= GlobalInput.Instance.dwellTime && !hasTriggered)
            {
                hasTriggered = true;
                ResetColor();
                StartCoroutine(FlickerAndExecute());
            }
        }
    }

    private IEnumerator FlickerAndExecute()
    {
        ExperimentLogger.Instance?.LogEvent("Dwell_Complete", $"Button: {gameObject.name}", "Dwelling_Completed");
        LSL_Logger.Instance?.LogEvent("Dwell_Complete", $"Button: {gameObject.name}", "Dwelling_Completed");

        currentState     = State.Flickering;
        hasTriggered     = true;
        flickerStartTime = -1f;

        // Store the event/detail pair so HandleFlickerLSL can validate the echo
        lastEvent  = "Flicker_Start";
        lastDetail = buttonId;

        ExperimentLogger.Instance?.LogEvent(lastEvent, lastDetail, "Flicker_Start");
        LSL_Logger.Instance?.LogEvent(lastEvent, lastDetail, "Flicker_Start");

        yield return new WaitForSeconds(GlobalInput.Instance.flickerDuration);

        // Stop the visual flicker
        flickerStartTime = -1f;
        if (runtimeMaterialFlicker != null)
            runtimeMaterialFlicker.SetFloat("_FlickerState", 0f);

        ExperimentLogger.Instance?.LogEvent("Flicker_End", $"Button: {gameObject.name}", "Flicker_End");
        LSL_Logger.Instance?.LogEvent(lastEvent, lastDetail, "Flicker_End");

        // ── Route by experiment mode ─────────────────────────────────────────
        if (!IsBCIMode())
        {
            // EyeTracking: execute immediately, no LSL wait
            currentState = State.Idle;
            Execution(selectedAction);
            yield break;
        }

        // Hybrid / BCI: park here and wait for HandleFlickerLSL to fire
        currentState  = State.WaitingForLSL;
        waitingButton = this;
        Debug.Log($"[BB] {buttonId} is WaitingForLSL.");
    }

    public void ResetColor()
    {
        dwellTimer = 0f;
        if (outlineImage)
        {
            runtimeMaterial.SetFloat("_Progress", 0f);
            outlineImage.gameObject.SetActive(false);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region LSL Response Handler

    /// <summary>
    /// Called by LSLCommunicationManager whenever a Flicker code (100/101) arrives.
    /// Validates 4 conditions before acting:
    ///   1. Ownership  — this button is the one waiting
    ///   2. State      — we are still in WaitingForLSL
    ///   3. Event match — Python echoed back the same event label
    ///   4. Detail match — Python echoed back our buttonId
    /// </summary>
    private void HandleFlickerLSL(BCIMessage msg)
    {
        // 1. Ownership: only the button that sent the flicker should respond
        if (waitingButton != this) return;

        // 2. State guard: if we somehow left WaitingForLSL, ignore
        if (currentState != State.WaitingForLSL) return;

        // 3. Event match: Python must echo back "Flicker_Start"
        if (msg.Event != lastEvent) return;

        // 4. Detail match: Python must echo back our specific buttonId
        if (msg.Detail != lastDetail) return;

        if (msg.Code == (int)LSLCommunicationManager.BCICommand.FlickerDetected)
        {
            Debug.Log($"[BB] Valid LSL response for '{buttonId}': detected = Detected");
            retryCount    = 0;
            waitingButton = null;
            currentState  = State.Idle;
            Execution(selectedAction);
        }
        else
        {
            Debug.Log($"[BB] Invalid LSL response for '{buttonId}': detected = Not Detected");
            currentState  = State.Idle;
            StartCoroutine(RetryFlicker());
        }
    }

    /// <summary>
    /// Brief pause, re-runs the flicker window, then re-enters WaitingForLSL.
    /// Cancelled immediately if the user looked away (isHovering == false) or if
    /// maxRetries has been exceeded — whichever comes first.
    /// </summary>
    private IEnumerator RetryFlicker()
    {
        retryCount++;

        // ── Max-retry guard ───────────────────────────────────────────────────
        if (retryCount > maxRetries)
        {
            Debug.Log($"[BB] Max retries reached for '{buttonId}', cancelling flicker.");
            ExperimentLogger.Instance?.LogEvent("Retry_Cancelled", $"Button: {gameObject.name}", "Max_Retries_Reached");
            LSL_Logger.Instance?.LogEvent("Retry_Cancelled", $"Button: {gameObject.name}", "Max_Retries_Reached");
            CancelRetry();
            yield break;
        }

        // Short gap so the EEG epoch window is clean
        yield return new WaitForSeconds(0.3f);

        // ── Hover guard — user may have looked away during the gap ─────────────
        if (!isHovering)
        {
            Debug.Log($"[BB] User no longer hovering '{buttonId}' — cancelling retry.");
            ExperimentLogger.Instance?.LogEvent("Retry_Cancelled", $"Button: {gameObject.name}", "Gaze_Lost");
            LSL_Logger.Instance?.LogEvent("Retry_Cancelled", $"Button: {gameObject.name}", "Gaze_Lost");
            CancelRetry();
            yield break;
        }

        Debug.Log($"[BB] Retry {retryCount}/{maxRetries} for '{buttonId}'.");
        ExperimentLogger.Instance?.LogEvent("Flicker_Retry", $"Button: {gameObject.name}", $"Retry: {retryCount} / {maxRetries}");
        LSL_Logger.Instance?.LogEvent("Flicker_Retry", $"Button: {gameObject.name}", $"Retry: {retryCount} / {maxRetries}");

        currentState     = State.Flickering;
        flickerStartTime = -1f;

        LSL_Logger.Instance?.LogEvent(lastEvent, lastDetail, "Flicker_Start");
        ExperimentLogger.Instance?.LogEvent(lastEvent, lastDetail, "Flicker_Start");

        yield return new WaitForSeconds(GlobalInput.Instance.flickerDuration);

        // Stop flicker visual
        flickerStartTime = -1f;
        if (runtimeMaterialFlicker != null)
            runtimeMaterialFlicker.SetFloat("_FlickerState", 0f);

        LSL_Logger.Instance?.LogEvent(lastEvent, lastDetail, "Flicker_End");
        ExperimentLogger.Instance?.LogEvent("Flicker_End", $"Button: {gameObject.name}", "Flicker_End");

        // Back to waiting — HandleFlickerLSL will fire again when Python responds
        currentState  = State.WaitingForLSL;
        waitingButton = this;
        Debug.Log($"[BB] '{buttonId}' re-entered WaitingForLSL after retry {retryCount}/{maxRetries}.");
    }

    /// <summary>
    /// Shared cleanup for cancelled retries (max exceeded or gaze lost).
    /// </summary>
    private void CancelRetry()
    {
        currentState = State.Idle;
        retryCount   = 0;
        if (waitingButton == this) waitingButton = null;
        flickerStartTime = -1f;
        if (runtimeMaterialFlicker != null)
            runtimeMaterialFlicker.SetFloat("_FlickerState", 0f);
        ResetColor();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Action

    public void Execution(ActionType action)
    {
        ExperimentLogger.Instance?.LogEvent("Action_Executed",
            $"Button: {gameObject.name}, Action: {action}", "Execution_Proceeding");
        LSL_Logger.Instance?.LogEvent("Action_Executed",
            $"Button: {gameObject.name}, Action: {action}", "Execution_Proceeding");

        switch (action)
        {
            case ActionType.None:
                break;
            case ActionType.ButtonAction:
                button?.onClick.Invoke();
                break;
            case ActionType.TestUI:
                TestUIControl();
                break;
        }
    }

    #endregion

    #region Flicker Functions

    void UpdateFlicker()
    {
        if (currentState != State.Flickering || runtimeMaterialFlicker == null) return;

        // Anchor the phase to when flickering started so every activation is consistent
        if (flickerStartTime < 0f)
            flickerStartTime = Time.unscaledTime;

        float elapsed = Time.unscaledTime - flickerStartTime;
        float phase = (elapsed * GlobalInput.Instance.flickerHz) % 1.0f;
        bool isOn = phase < 0.5f;  // 50% duty cycle, self-correcting every frame

        runtimeMaterialFlicker.SetFloat("_FlickerState", isOn ? 1f : 0f);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Helpers

    /// <summary>
    /// Returns true when the current experiment mode requires LSL confirmation
    /// before executing a button action (Hybrid or BCI).
    /// </summary>
    private bool IsBCIMode()
    {
        if (MainControl.Instance == null) return false;
        var exp = MainControl.Instance.currentExperiment;
        return exp == MainControl.ExperimentType.BCI ||
               exp == MainControl.ExperimentType.Hybrid;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region TestUI Update

    public void TestUIControl()
    {
        if (isNext)
            NextPhase();
        else if (isDelete)
            DeleteValue();
        else
            AddValue();
    }

    private void AddValue()
    {
        if (testUI != null) testUI.AddDigit(value);
    }

    private void DeleteValue()
    {
        if (testUI != null) testUI.RemoveDigitLast();
    }

    private void NextPhase()
    {
        if (testUI != null) testUI.NextPhase();
    }

    #endregion

    #region Hover Events

    public void OnHoverEnter()
    {
        if (activeButton != null && activeButton != this) return;
        activeButton = this;

        isHovering = true;
        ExperimentLogger.Instance?.LogEvent("Hover_Enter", $"Button: {gameObject.name}", "Hovering");
        LSL_Logger.Instance?.LogEvent("Hover_Enter", $"Button: {gameObject.name}", "Hovering");

        if (currentState == State.Idle)
            currentState = State.Hovering;
    }

    public void OnHoverExit()
    {
        if (activeButton != this) return;

        isHovering   = false;
        retryCount   = 0;
        currentState = State.Idle;
        dwellTimer   = 0f;
        hasTriggered = false;
        flickerStartTime = -1f;

        ExperimentLogger.Instance?.LogEvent("Hover_Exit", $"Button: {gameObject.name}", "Hover_Exit");
        LSL_Logger.Instance?.LogEvent("Hover_Exit", $"Button: {gameObject.name}", "Hover_Exit");

        ResetColor();

        if (runtimeMaterialFlicker != null)
            runtimeMaterialFlicker.SetFloat("_FlickerState", 0f);

        activeButton = null;
    }

    #endregion
}