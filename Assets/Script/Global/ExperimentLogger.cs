using UnityEngine;
using System.IO;
using System;

public class ExperimentLogger : MonoBehaviour
{
    public static ExperimentLogger Instance { get; private set; }

    private string filePath;
    private StreamWriter writer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Setup CSV file path in the Assets folder so it's easy to find during dev
        string timestamp = DateTime.Now.ToString("MMdd_HHmmss");
        filePath = Path.Combine(Application.dataPath, $"ExperimentLog_{timestamp}.csv");

        // Write CSV header
        writer = new StreamWriter(filePath, true);
        writer.WriteLine("Time,Experiment,Phase,Event,Detail,Action");
        writer.Flush();
        
        Debug.Log($"Logging to: {filePath}");
    }

    /// <summary>
    /// Logs an event to the CSV. Automatically fetches current Experiment and Phase.
    /// Event: Example "Dwell_Complete"
    /// Detail: Example "Button1"
    /// Action: Example "Classify_Dwelling"
    /// </summary>
    public void LogEvent(string eventName, string detail = "", string action = "")
    {
        if (writer == null) return;

        // 1. Time
        string time = DateTime.Now.ToString("HH:mm:ss.fff");
        
        // 2 & 3. Experiment and Phase pulled directly from MainControl
        string experiment = MainControl.Instance != null ? MainControl.Instance.currentExperiment.ToString() : "None";
        string phase = MainControl.Instance != null ? MainControl.Instance.currentPhase.ToString() : "None";

        // Clean any commas from strings since it's a CSV format
        detail = detail.Replace(",", ";");
        action = action.Replace(",", ";");
        eventName = eventName.Replace(",", ";");

        // Format and Write
        string line = $"{time},{experiment},{phase},{eventName},{detail},{action}";
        writer.WriteLine(line);
        writer.Flush();
    }

    private void OnDestroy()
    {
        if (writer != null)
        {
            writer.Close();
            writer = null;
        }
    }
}
