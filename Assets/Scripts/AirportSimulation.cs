using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AirportSimulation : DES
{
    [SerializeField] private int numberOfServers = 5;  // Number of gates
    [SerializeField] private int numberOfQueues = 3;   // Number of waiting areas
    [SerializeField] private float meanServiceTime = 3f;  // Mean service time for planes
    [SerializeField] private float meanArrivalTime = 1f;  // Mean time between arrivals
    [SerializeField] private int totalArrivals = 10;      // Total number of planes to simulate

    private bool[] serverStatus;  // true = busy, false = idle
    private List<Queue<Plane>> queues;  // List of queues for planes
    private int totalPlanes = 0;
    private int delayedPlanes = 0;
    private float totalDelayTime = 0f;
    private float[] serverUtilization;  // Track utilization for each server
    private float[] serverBusyTime;     // Track busy time for each server
    private List<float> queueLengths = new List<float>();  // Track queue lengths over time
    private bool isInitialized = false;

    // Add these fields for visualization preparation
    private Vector3[] gatePositions;  // Will store positions of each gate
    private Vector3 queueStartPosition = new Vector3(-50, 0, 0);  // Example position for queue start
    private float gateSpacing = 20f;  // Space between gates

    private float startRealTime;  // To track actual running time

    // Add these fields at the top of the AirportSimulation class
    [Header("Visualization")]
    [SerializeField] private GameObject planePrefab;
    private Dictionary<Plane, PlaneVisual> planeVisuals = new Dictionary<Plane, PlaneVisual>();
    private Dictionary<int, List<Plane>> queueContents = new Dictionary<int, List<Plane>>();

    public void SetParameters(int totalArrivals, float meanServiceTime, float meanArrivalTime, float timeScale)
    {
        this.totalArrivals = totalArrivals;
        this.meanServiceTime = meanServiceTime;
        this.meanArrivalTime = meanArrivalTime;
        base.timeScale = timeScale;
        
        // Initialize arrays and queues
        serverStatus = new bool[numberOfServers];
        serverUtilization = new float[numberOfServers];
        serverBusyTime = new float[numberOfServers];
        queues = new List<Queue<Plane>>();
        
        // Initialize queues
        for (int i = 0; i < numberOfQueues; i++)
        {
            queues.Add(new Queue<Plane>());
        }
        
        isInitialized = true;
        Debug.Log($"Parameters set: Total Arrivals={totalArrivals}, Mean Service Time={meanServiceTime}, Mean Arrival Time={meanArrivalTime}, Time Scale={timeScale}x");
    }

    public void SetTimeScale(float newTimeScale)
    {
        timeScale = newTimeScale;
        Debug.Log($"Time scale changed to {newTimeScale}x");
    }

    protected override void Awake()
    {
        base.Awake();
        InitializeGatePositions();
        
        // Initialize queue contents tracking
        for (int i = 0; i < numberOfQueues; i++)
        {
            queueContents[i] = new List<Plane>();
        }
    }

    private void InitializeGatePositions()
    {
        gatePositions = new Vector3[numberOfServers];
        for (int i = 0; i < numberOfServers; i++)
        {
            gatePositions[i] = new Vector3(i * gateSpacing, 0, 0);
        }
    }

    public void StartSimulation()
    {
        if (!isInitialized)
        {
            Debug.LogError("Simulation not initialized! Call SetParameters first.");
            return;
        }

        startRealTime = Time.realtimeSinceStartup;  // Record start time
        Debug.Log("Starting Airport Simulation...");
        base.Awake(); // Now initialize the base DES system
    }

    protected override void HandleEvent(SimEvent e)
    {
        string eventType = e.GetAttributeValue<string>("Type");
        
        switch (eventType)
        {
            case "start":
                ScheduleNextArrival();
                break;
                
            case "arrival":
                HandleArrival(e);
                break;
                
            case "departure":
                HandleDeparture(e);
                break;
                
            case "end":
                Report();
                break;
        }
    }

    private void HandleArrival(SimEvent e)
    {
        totalPlanes++;
        Plane plane = e.GetAttributeValue<Plane>("Plane");
        
        // Try to find an idle server
        int serverIndex = FindIdleServer();
        
        if (serverIndex != -1)
        {
            // Server is available, move plane to server
            planeVisuals[plane].TeleportToServer(serverIndex);
            StartService(plane, serverIndex);
        }
        else
        {
            // All servers busy, add to shortest queue
            AddToQueue(plane);
        }
        
        // Schedule next arrival if we haven't reached total arrivals
        if (totalPlanes < totalArrivals)
        {
            ScheduleNextArrival();
        }
        else if (totalPlanes == totalArrivals)
        {
            Debug.Log($"All {totalArrivals} planes have arrived. Waiting for remaining services to complete...");
            CheckSimulationCompletion();
        }
    }

    private void HandleDeparture(SimEvent e)
    {
        int serverIndex = e.GetAttributeValue<int>("ServerIndex");
        Plane departingPlane = e.GetAttributeValue<Plane>("Plane");
        
        // Start departure sequence
        if (planeVisuals.ContainsKey(departingPlane))
        {
            planeVisuals[departingPlane].StartDeparture();
            planeVisuals.Remove(departingPlane);
        }
        
        serverStatus[serverIndex] = false;
        
        // Try to get next plane from queues
        if (TryGetNextPlane(out Plane nextPlane))
        {
            // Update queue visuals
            UpdateQueueVisuals();
            StartService(nextPlane, serverIndex);
        }
    }

    private bool TryGetNextPlane(out Plane plane)
    {
        plane = null;
        
        // If no queues have planes, return false
        if (!queues.Any(q => q.Count > 0))
            return false;

        // Find the queue with the most planes
        var longestQueue = queues.OrderByDescending(q => q.Count).First();
        if (longestQueue.Count > 0)
        {
            plane = longestQueue.Dequeue();
            return true;
        }

        return false;
    }

    private void StartService(Plane plane, int serverIndex)
    {
        serverStatus[serverIndex] = true;
        float serviceTime = random.Exponential(meanServiceTime);
        serverBusyTime[serverIndex] += serviceTime;
        
        // Move plane to server position
        planeVisuals[plane].TeleportToServer(serverIndex);
        
        SimEvent departureEvent = new SimEvent("departure", clock + serviceTime);
        departureEvent.AddAttribute("ServerIndex", serverIndex);
        departureEvent.AddAttribute("Plane", plane);
        AddEvent(departureEvent);
    }

    private int FindIdleServer()
    {
        for (int i = 0; i < numberOfServers; i++)
        {
            if (!serverStatus[i])
                return i;
        }
        return -1;
    }

    private void AddToQueue(Plane plane)
    {
        // Find shortest queue
        var shortestQueue = queues.OrderBy(q => q.Count).First();
        int queueIndex = queues.IndexOf(shortestQueue);
        
        // Add to queue contents tracking
        queueContents[queueIndex].Add(plane);
        
        // Update visual position
        planeVisuals[plane].TeleportToQueue(queueIndex, shortestQueue.Count - 1);
        
        shortestQueue.Enqueue(plane);
        delayedPlanes++;
        totalDelayTime += clock - plane.ArrivalTime;
        
        RecordQueueLengths();
    }

    private void RecordQueueLengths()
    {
        float totalLength = queues.Sum(q => q.Count);
        queueLengths.Add(totalLength);
    }

    private void ScheduleNextArrival()
    {
        float nextArrivalTime = clock + random.Exponential(meanArrivalTime);
        SimEvent arrivalEvent = new SimEvent("arrival", nextArrivalTime);
        Plane newPlane = new Plane(clock);
        arrivalEvent.AddAttribute("Plane", newPlane);
        AddEvent(arrivalEvent);
        
        // Spawn visual representation
        SpawnPlaneVisual(newPlane);
    }

    private void CheckSimulationCompletion()
    {
        // Check if we've processed all planes and all servers are idle
        if (totalPlanes >= totalArrivals && !serverStatus.Any(status => status) && !queues.Any(q => q.Count > 0))
        {
            Debug.Log("All planes processed. Ending simulation...");
            Report();
            isRunning = false;
        }
    }

    public override void Report()
    {
        isRunning = false;  // Ensure simulation stops
        float realTimeDuration = Time.realtimeSinceStartup - startRealTime;
        float expectedSimTime = realTimeDuration * timeScale / 60f;  // Convert to expected simulation minutes
        
        // Calculate average queue length
        float avgQueueLength = queueLengths.Count > 0 ? queueLengths.Average() : 0;
        
        // Calculate server utilization
        for (int i = 0; i < numberOfServers; i++)
        {
            serverUtilization[i] = serverBusyTime[i] / clock;
        }
        
        string report = "\n=== AIRPORT SIMULATION FINAL REPORT ===\n\n";
        
        report += $"1. Time Statistics:\n";
        report += $"   - Simulation Time: {clock:F2} minutes\n";
        report += $"   - Real Time Duration: {realTimeDuration:F2} seconds ({realTimeDuration/60:F2} minutes)\n";
        report += $"   - Time Scale: {timeScale:F1}x (1 real second = {timeScale/60:F2} sim minutes)\n";
        report += $"   - Expected Sim Time: {expectedSimTime:F2} minutes\n";
        report += $"   - Average Processing Rate: {(totalPlanes / (clock/60)):F1} planes/hour\n\n";
        
        report += $"2. Delay Statistics:\n";
        report += $"   - Total Planes: {totalPlanes}\n";
        report += $"   - Delayed Planes: {delayedPlanes}\n";
        report += $"   - Percentage Delayed: {((float)delayedPlanes / totalPlanes * 100):F1}%\n\n";
        
        report += $"3. Delay Time:\n";
        report += $"   - Total Delay Time: {totalDelayTime:F2} minutes\n";
        report += $"   - Average Delay Time: {(delayedPlanes > 0 ? totalDelayTime / delayedPlanes : 0):F2} minutes\n\n";
        
        report += $"4. Server (Gate) Utilization:\n";
        for (int i = 0; i < numberOfServers; i++)
        {
            report += $"   - Gate {i + 1}: {serverUtilization[i]:P2}\n";
        }
        float avgUtilization = serverUtilization.Average();
        report += $"   - Average Gate Utilization: {avgUtilization:P2}\n\n";
        
        report += $"5. Queue Statistics:\n";
        report += $"   - Average Queue Length: {avgQueueLength:F2} planes\n";
        report += $"   - Maximum Queue Length: {(queueLengths.Count > 0 ? queueLengths.Max() : 0):F2} planes\n";
        
        report += "\n=====================================\n";
        
        Debug.Log(report);
    }

    // Add this method to get plane positions for visualization
    public List<(Vector3 position, int gateNumber)> GetAllPlanePositions()
    {
        var positions = new List<(Vector3 position, int gateNumber)>();
        
        // Add planes in gates
        for (int i = 0; i < numberOfServers; i++)
        {
            if (serverStatus[i])
            {
                positions.Add((gatePositions[i], i + 1));
            }
        }
        
        // Add planes in queues
        foreach (var queue in queues)
        {
            foreach (var plane in queue)
            {
                positions.Add((plane.Position, plane.AssignedGate));
            }
        }
        
        return positions;
    }

    private void SpawnPlaneVisual(Plane plane)
    {
        GameObject planeObj = Instantiate(planePrefab);
        PlaneVisual visual = planeObj.AddComponent<PlaneVisual>();
        planeVisuals[plane] = visual;
        
        // Start approach movement
        visual.StartApproach(meanArrivalTime, () => HandlePlaneArrival(plane));
    }

    private void HandlePlaneArrival(Plane plane)
    {
        // Try to find an idle server
        int serverIndex = FindIdleServer();
        
        if (serverIndex != -1)
        {
            // Server is available, move plane to server
            planeVisuals[plane].TeleportToServer(serverIndex);
            StartService(plane, serverIndex);
        }
        else
        {
            // All servers busy, add to shortest queue
            AddToQueue(plane);
        }
    }

    private void UpdateQueueVisuals()
    {
        // Update positions for all planes in all queues
        for (int i = 0; i < numberOfQueues; i++)
        {
            var queueList = queueContents[i];
            for (int j = 0; j < queueList.Count; j++)
            {
                if (planeVisuals.ContainsKey(queueList[j]))
                {
                    planeVisuals[queueList[j]].TeleportToQueue(i, j);
                }
            }
        }
    }
}

public class Plane
{
    public float ArrivalTime { get; private set; }
    public Vector3 Position { get; set; }  // For visual representation
    public int AssignedGate { get; set; } = -1;  // -1 means no gate assigned
    
    public Plane(float arrivalTime)
    {
        ArrivalTime = arrivalTime;
        Position = Vector3.zero;  // Will be set when visualizing
    }
} 