using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MLtest : MonoBehaviour
{
    #region Inspector Fields

    [Header("Door References")]
    [Tooltip("GameObject for Door 1 (must have OB component attached)")]
    public GameObject Door1;

    [Tooltip("GameObject for Door 2 (must have OB component attached)")]
    public GameObject Door2;

    [Header("Sequence Parameters")]
    [Tooltip("Number of Door 1 trials")]
    public int door1Count = 10;

    [Tooltip("Number of Door 2 trials")]
    public int door2Count = 10;

    [Tooltip("Number of Door 1 Flicker trials")]
    public int door1FlickerCount = 10;

    [Header("Timing (seconds)")]
    [Tooltip("Blank gap BEFORE the stimulus appears")]
    public float preTrialDelay  = 2f;

    [Tooltip("Duration the stimulus is shown")]
    public float showDuration   = 4f;

    [Tooltip("Blank gap for imagery")]
    public float imageryDuration = 4f;

    [Tooltip("Rest period between trials (after post-trial gap)")]
    public float restPeriod = 2f;


    public GameObject uiElement;
    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Private State

    /// <summary>The shuffled trial list generated in Start().</summary>
    private List<string> trialSequence = new List<string>();

    /// <summary>Cached OB component on Door1 (provides access to the Flicker).</summary>
    private OB ob1;

    /// <summary>Cached OB component on Door2 (provides access to the Flicker).</summary>
    private OB ob2;

    /// <summary>Cached Flicker component on Door1.</summary>
    private Flicker flicker1;

    /// <summary>Cached Flicker component on Door2.</summary>
    private Flicker flicker2;

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        // Cache components — warn early if references are missing
        if (Door1 != null)
        {
            ob1     = Door1.GetComponent<OB>();
            flicker1 = Door1.GetComponent<Flicker>();
        }
        else Debug.LogWarning("[MLtest] Door1 is not assigned!");

        if (Door2 != null)
        {
            ob2     = Door2.GetComponent<OB>();
            flicker2 = Door2.GetComponent<Flicker>();
        }
        else Debug.LogWarning("[MLtest] Door2 is not assigned!");
    }

    private void Start()
    {
        // Hide both doors at startup
        SetDoorVisible(Door1, false);
        SetDoorVisible(Door2, false);

        // Build and shuffle the trial sequence
        GenerateSequence();
    }

    private void OnDisable()
    {
        // Clean up: hide doors and stop any running coroutines
        StopAllCoroutines();
        SetDoorVisible(Door1, false);
        SetDoorVisible(Door2, false);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Sequence Generation

    /// <summary>
    /// Populates <see cref="trialSequence"/> with the three trial types,
    /// shuffles it using Fisher-Yates, and logs every entry to the console.
    /// </summary>
    private void GenerateSequence()
    {
        trialSequence.Clear();

        // Define trial types and counts from the inspector
        var trialTypes = new (string label, int count)[]
        {
            ("Door1",        door1Count),
            ("Door2",        door2Count),
            ("Door1Flicker", door1FlickerCount),
        };

        // Fill the list
        foreach (var (label, count) in trialTypes)
            for (int i = 0; i < count; i++)
                trialSequence.Add(label);

        // Fisher-Yates in-place shuffle
        FisherYatesShuffle(trialSequence);

        // Print the entire sequence to the console
        Debug.Log($"[MLtest] Generated {trialSequence.Count} trials:");
        for (int i = 0; i < trialSequence.Count; i++)
            Debug.Log($"[MLtest]  [{i:D2}] {trialSequence[i]}");
    }

    /// <summary>
    /// Performs an in-place Fisher-Yates shuffle on any <see cref="List{T}"/>.
    /// </summary>
    private static void FisherYatesShuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1); // inclusive upper bound
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Public Entry Point

    /// <summary>
    /// Call this (e.g. from a UI button) to start playing through all trials.
    /// </summary>
    public void StartMLTest()
    {
        SetDoorVisible(uiElement, false);
        StartCoroutine(RunTrialSequence());
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Trial Coroutine

    /// <summary>
    /// Iterates through every entry in <see cref="trialSequence"/> and
    /// runs the timed display logic for each trial.
    /// </summary>
    private IEnumerator RunTrialSequence()
    {
        Debug.Log("[MLtest] Trial sequence started.");
        ExperimentLogger.Instance?.LogEvent("MLtest_Start", "MLtest", "Trial_Sequence_Start");
        LSL_Logger.Instance?.LogEvent("MLtest_Start", "MLtest", "Trial_Sequence_Start");

        for (int i = 0; i < trialSequence.Count; i++)
        {
            string trial = trialSequence[i];
            Debug.Log($"[MLtest] Trial {i + 1}/{trialSequence.Count}: {trial}");

            yield return StartCoroutine(RunSingleTrial(i, trial));
        }

        Debug.Log("[MLtest] All trials complete.");
        ExperimentLogger.Instance?.LogEvent("MLtest_End", "MLtest", "Trial_Sequence_End");
        LSL_Logger.Instance?.LogEvent("MLtest_End", "MLtest", "Trial_Sequence_End");
    }

    /// <summary>
    /// Executes one trial:
    ///  1. Pre-trial blank gap
    ///  2. Show the correct door (and start flicker if needed)
    ///  3. Post-stimulus blank gap
    /// </summary>
    private IEnumerator RunSingleTrial(int index, string trialType)
    {
        // ── 1. Pre-trial blank (both doors hidden) ───────────────────────────
        SetDoorVisible(Door1, false);
        SetDoorVisible(Door2, false);
        yield return new WaitForSeconds(preTrialDelay);

        // ── 2. Determine which door and whether to flicker ───────────────────
        bool useDoor1   = trialType == "Door1"        || trialType == "Door1Flicker";
        bool useFlicker = trialType == "Door1Flicker";

        GameObject activeDoor  = useDoor1 ? Door1  : Door2;
        Flicker    activeFlick = useDoor1 ? flicker1 : flicker2;
        string     doorName    = activeDoor != null ? activeDoor.name : (useDoor1 ? "Door1" : "Door2");

        // Build event name roots to match TrainBCI.cs convention:
        //   Door1 active  → Training_Active_Door1_Start / End
        //   Door2 active  → Active_Training_Door2_Start / End
        //   Door1 imagery → Training_Imagery_Door1_Start / End
        //   Door2 imagery → Image_Training_Door2_Start / End
        string activeStart  = useDoor1 ? "Training_Active_Door1_Start"  : "Active_Training_Door2_Start";
        string activeEnd    = useDoor1 ? "Training_Active_Door1_End"    : "Active_Training_Door2_End";
        string imageryStart = useDoor1 ? "Training_Imagery_Door1_Start" : " ";
        string imageryEnd   = useDoor1 ? "Training_Imagery_Door1_End"   : " ";

        // ── Log active-phase onset ───────────────────────────────────────────
        ExperimentLogger.Instance?.LogEvent(activeStart, doorName, activeStart);
        LSL_Logger.Instance?.LogEvent(activeStart, doorName, activeStart);

        // ── Show the door ────────────────────────────────────────────────────
        SetDoorVisible(activeDoor, true);

        // ── Optionally start flicker ─────────────────────────────────────────
        if (useFlicker && activeFlick != null)
        {
            activeFlick.StartFlicker(showDuration);
            Debug.Log($"[MLtest] Flicker started on {doorName}.");
        }

        // ── Hold for show duration ───────────────────────────────────────────
        yield return new WaitForSeconds(showDuration);

        // ── Hide door ────────────────────────────────────────────────────────
        SetDoorVisible(activeDoor, false);

        // ── Log active-phase offset ──────────────────────────────────────────
        ExperimentLogger.Instance?.LogEvent(activeEnd, doorName, activeEnd);
        LSL_Logger.Instance?.LogEvent(activeEnd, doorName, activeEnd);

        // ── 3. Post-trial blank — imagery / rest window ──────────────────────
        ExperimentLogger.Instance?.LogEvent(imageryStart, doorName, imageryStart);
        LSL_Logger.Instance?.LogEvent(imageryStart, doorName, imageryStart);

        yield return new WaitForSeconds(imageryDuration);

        ExperimentLogger.Instance?.LogEvent(imageryEnd, doorName, imageryEnd);
        LSL_Logger.Instance?.LogEvent(imageryEnd, doorName, imageryEnd);

        // ── 4. Rest period (between trials) ──────────────────────────────────
        yield return new WaitForSeconds(restPeriod);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Helpers

    /// <summary>
    /// Activates or deactivates a door GameObject safely.
    /// </summary>
    private static void SetDoorVisible(GameObject door, bool visible)
    {
        if (door != null) door.SetActive(visible);
    }

    #endregion
}
