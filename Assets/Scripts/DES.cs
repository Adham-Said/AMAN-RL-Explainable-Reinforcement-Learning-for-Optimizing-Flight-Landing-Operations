using UnityEngine;
using System.Collections.Generic;

public class DES : MonoBehaviour
{
    protected float clock = 0f;
    protected float maxClock = 0f;
    protected float averages = 0f;
    protected List<SimEvent> events;
    protected RandomGenerator random;
    
    [SerializeField] protected float timeScale = 1f; // 1 real minute = 1 simulation minute
    protected bool isRunning = false;

    public float Clock => clock;
    public bool IsRunning => isRunning;

    protected virtual void Awake()
    {
        Debug.Log("DES: Initializing simulation...");
        events = new List<SimEvent>();
        random = gameObject.AddComponent<RandomGenerator>();
        random.Initialize();
        
        // Add initial start event
        AddEvent(new SimEvent("start", 0f));
        Debug.Log("DES: Added start event");
        
        // Start the simulation
        isRunning = true;
        Debug.Log("DES: Starting simulation...");
    }

    protected virtual void Update()
    {
        if (!isRunning) return;

        // First convert real seconds to simulation minutes, then apply time scale
        float deltaMinutes = (Time.deltaTime / 60f);  // Convert real seconds to minutes
        clock += deltaMinutes * timeScale;  // Apply time scale to get simulation minutes

        // Process all events that should have occurred by now
        while (events.Count > 0 && events[0].Time <= clock)
        {
            ProcessNextEvent();
        }

        // Debug time progression
        if (Time.frameCount % 60 == 0)  // Log every 60 frames
        {
            Debug.Log($"Real time: {Time.realtimeSinceStartup:F2}s, Sim time: {clock:F2}m, Scale: {timeScale}x");
        }
    }

    protected void AddEvent(SimEvent newEvent)
    {
        events.Add(newEvent);
        events.Sort((a, b) => a.CompareTo(b));
    }

    protected virtual void ProcessNextEvent()
    {
        if (events.Count == 0) return;

        SimEvent currentEvent = events[0];
        events.RemoveAt(0);
        HandleEvent(currentEvent);
    }

    protected virtual void HandleEvent(SimEvent e)
    {
        Debug.Log($"DES: Event processed at time {clock:F2} minutes: {e.GetAttributeValue<string>("Type")}");
    }

    public virtual void Report()
    {
        isRunning = false;
        Debug.Log($"Simulation ended at time: {clock:F2} minutes");
    }

    public void PauseSimulation()
    {
        isRunning = false;
    }

    public void ResumeSimulation()
    {
        isRunning = true;
    }
} 