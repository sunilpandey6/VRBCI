using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalInput : MonoBehaviour
{
    public static GlobalInput Instance { get; private set; }
    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Header("Dwell Settings")]
    public float dwellTime = 2.5f;
    [Header("Flicker Settings")]
    public float flickerDuration = 2f;
    public float flickerHz = 15f;
    public int refreshRate = 90;

    [Header("Border Colors")]
    public Color idleColor = new Color32(255, 255, 255, 255); // white
    public Color midColor = new Color32(255, 165, 0, 255); // orange
    public Color activeColor = new Color32(0, 255, 0, 255); // green

    [Header("Flicker Colors")]
    public Color flickerOn = new Color32(0, 0, 0, 0); // black

    [Header("UI Positioning")]
    public Camera cam;
    public float distance = 2f;
    public float horizontalOffset = 0f;
    public float verticalOffset = 0f;


}
