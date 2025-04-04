using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class AirportSimulation : DES
{
    // Singleton pattern
    public static AirportSimulation Instance { get; private set; }

    // Constants for visualization
    private const float PLANE_SIZE = 5f;
    private const float PLANE_SPEED = 9f; // 45 units in 5 seconds
    private const float ARRIVAL_PATH_LENGTH = 45f;
    private const float PLANE_HEIGHT = 0.5f; // Assuming a default plane height

    [Header("Simulation Parameters")]
    [SerializeField] private int numberOfServers = 5;  // Number of gates
    [SerializeField] private int numberOfQueues = 3;   // Number of waiting areas
    [SerializeField] private float meanServiceTime = 3f;  // Mean service time for planes
    [SerializeField] private float meanArrivalTime = 1f;  // Mean time between arrivals
    [SerializeField] private int totalArrivals = 10;      // Total number of planes to simulate
    [SerializeField] private float simulationTimeScale = 1f;  // Simulation speed multiplier

    [Header("Visualization")]
    [SerializeField] private Material planeMaterial; // Optional: for distinguishing planes

    private bool[] serverStatus;  // true = busy, false = idle
    private List<Queue<Plane>> queues;  // List of queues for planes
    private int totalPlanes = 0;
    private int delayedPlanes = 0;
    private float totalDelayTime = 0f;
    private float[] serverUtilization;  // Track utilization for each server
    private float[] serverBusyTime;     // Track busy time for each server
    private List<float> queueLengths = new List<float>();  // Track queue lengths over time
    private bool isInitialized = false;

    private Vector3[] gatePositions;  // Will store positions of each gate
    private Vector3[] queueStartPositions; // Will store positions of each queue
    private float startRealTime;  // To track actual running time

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
            queueContents[i] = new List<Plane>();
        }
        
        isInitialized = true;
        Debug.Log($"Parameters set: Total Arrivals={totalArrivals}, Mean Service Time={meanServiceTime}, Mean Arrival Time={meanArrivalTime}, Time Scale={timeScale}x");
    }

    public void SetTimeScale(float newTimeScale)
    {
        simulationTimeScale = newTimeScale;
        timeScale = simulationTimeScale;
        Debug.Log($"Time scale changed to {newTimeScale}x");
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance == null)
        {
            Instance = this;
            timeScale = simulationTimeScale;
            random = new RandomGenerator();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeGatePositions();
        InitializeQueuePositions();
        
        // Initialize queue contents tracking
        for (int i = 0; i < numberOfQueues; i++)
        {
            queueContents[i] = new List<Plane>();
        }
    }

    private void Start()
    {
        Debug.Log("Starting simulation setup...");
        // Auto-initialize parameters
        SetParameters(totalArrivals, meanServiceTime, meanArrivalTime, simulationTimeScale);
        
        // Auto-start simulation
        StartSimulation();
        Debug.Log($"Simulation started with parameters: Arrivals={totalArrivals}, ServiceTime={meanServiceTime}, ArrivalTime={meanArrivalTime}, TimeScale={simulationTimeScale}");
    }

    private void InitializeGatePositions()
    {
        gatePositions = new Vector3[numberOfServers];
        
        for (int i = 0; i < numberOfServers; i++)
        {
            GameObject server = GameObject.Find($"Server {i + 1}");
            if (server != null)
            {
                // Ensure server doesn't fall
                Rigidbody rb = server.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = server.AddComponent<Rigidbody>();
                }
                rb.isKinematic = true;
                rb.useGravity = false;
                
                // Add trigger collider
                BoxCollider collider = server.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = server.AddComponent<BoxCollider>();
                }
                collider.isTrigger = true;
                
                gatePositions[i] = server.transform.position;
                Debug.Log($"Server {i + 1} initialized at position {gatePositions[i]}");
            }
            else
            {
                Debug.LogError($"Could not find Server {i + 1} in the scene!");
            }
        }
    }

    private void InitializeQueuePositions()
    {
        queueStartPositions = new Vector3[numberOfQueues];
        
        for (int i = 0; i < numberOfQueues; i++)
        {
            GameObject queue = GameObject.Find($"Queue {i + 1}");
            if (queue != null)
            {
                // Ensure queue doesn't fall
                Rigidbody rb = queue.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = queue.AddComponent<Rigidbody>();
                }
                rb.isKinematic = true;
                rb.useGravity = false;
                
                // Add trigger collider
                BoxCollider collider = queue.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = queue.AddComponent<BoxCollider>();
                }
                collider.isTrigger = true;
                
                queueStartPositions[i] = queue.transform.position;
                Debug.Log($"Queue {i + 1} initialized at position {queueStartPositions[i]}");
            }
            else
            {
                Debug.LogError($"Could not find Queue {i + 1} in the scene!");
            }
        }
    }

    public Vector3 GetServerPosition(int serverIndex)
    {
        if (serverIndex >= 0 && serverIndex < gatePositions.Length)
            return gatePositions[serverIndex];
        return Vector3.zero;
    }

    public Vector3 GetQueuePosition(int queueIndex)
    {
        if (queueIndex >= 0 && queueIndex < queueStartPositions.Length)
            return queueStartPositions[queueIndex];
        return Vector3.zero;
    }

    public void StartSimulation()
    {
        if (!isInitialized)
        {
            Debug.LogError("Simulation not initialized! Call SetParameters first.");
            return;
        }

        Debug.Log("Creating first arrival event...");
        startRealTime = Time.realtimeSinceStartup;
        
        // Schedule first arrival
        SimEvent startEvent = new SimEvent("start", 0f);
        startEvent.AddAttribute("Type", "start");
        Schedule(startEvent);
    }

    protected override void HandleEvent(SimEvent e)
    {
        string eventType = e.GetAttributeValue<string>("Type");
        Debug.Log($"Handling event of type: {eventType} at time {clock}");
        
        switch (eventType)
        {
            case "start":
                Debug.Log("Processing start event");
                ScheduleNextArrival();
                break;
                
            case "arrival":
                Debug.Log("Processing arrival event");
                HandleArrival(e);
                break;
                
            case "departure":
                Debug.Log("Processing departure event");
                HandleDeparture(e);
                break;
                
            case "end":
                Debug.Log("Processing end event");
                Report();
                break;
        }
    }

    private void HandleArrival(SimEvent e)
    {
        totalPlanes++;
        Plane plane = e.GetAttributeValue<Plane>("Plane");
        Debug.Log($"Handling arrival of plane {totalPlanes} at time {clock}");
        
        // Create visual representation at arrival position
        PlaneVisual visual = CreatePlaneVisual();
        planeVisuals[plane] = visual;
        
        // Start the plane at arrival position
        visual.StartApproach(ARRIVAL_PATH_LENGTH / PLANE_SPEED, () => {
            // After arrival animation, assign to server or queue
            int serverIndex = FindIdleServer();
            if (serverIndex != -1)
            {
                Debug.Log($"Found idle server {serverIndex} for plane {totalPlanes}");
                StartService(plane, serverIndex);
            }
            else
            {
                Debug.Log($"No idle server found for plane {totalPlanes}, adding to queue");
                AddToQueue(plane);
            }
        });
        
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

    private void AddToQueue(Plane plane)
    {
        // Find shortest queue
        int shortestQueueIndex = 0;
        int shortestLength = queues[0].Count;
        
        for (int i = 1; i < queues.Count; i++)
        {
            if (queues[i].Count < shortestLength)
            {
                shortestLength = queues[i].Count;
                shortestQueueIndex = i;
            }
        }
        
        // Add to shortest queue
        queues[shortestQueueIndex].Enqueue(plane);
        if (planeVisuals.ContainsKey(plane))
        {
            planeVisuals[plane].TeleportToQueue(shortestQueueIndex, queues[shortestQueueIndex].Count - 1);
        }
    }

    private PlaneVisual CreatePlaneVisual()
    {
        GameObject planeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        planeObj.transform.localScale = new Vector3(PLANE_SIZE, PLANE_SIZE, PLANE_SIZE);
        planeObj.name = $"Plane_{totalPlanes}";
        
        // Remove existing components from primitive
        DestroyImmediate(planeObj.GetComponent<BoxCollider>());
        
        if (planeMaterial != null)
            planeObj.GetComponent<Renderer>().material = planeMaterial;
            
        PlaneVisual visual = planeObj.AddComponent<PlaneVisual>();
        return visual;
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
        
        // Delay server release
        StartCoroutine(DelayServerRelease(serverIndex));
        
        // Delay before trying to get the next plane
        StartCoroutine(DelayNextPlaneService(serverIndex));
        
        // Update queue visuals after a plane departs
        UpdateQueueVisuals();
    }

    /// <summary>
    /// Called by PlaneVisual when a plane has completed its departure animation
    /// </summary>
    public void OnPlaneDepartureComplete()
    {
        Debug.Log("[AirportSimulation] Plane departure animation complete");
        CheckSimulationCompletion();
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
        if (planeVisuals.ContainsKey(plane))
        {
            planeVisuals[plane].TeleportToServer(serverIndex);
        }
        
        // Schedule departure after service time
        SimEvent departureEvent = new SimEvent("departure", clock + serviceTime);
        departureEvent.AddAttribute("ServerIndex", serverIndex);
        departureEvent.AddAttribute("Plane", plane);
        Schedule(departureEvent);
        
        Debug.Log($"Plane started service at server {serverIndex}, service time: {serviceTime:F2}");
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

    private void RecordQueueLengths()
    {
        float totalLength = queues.Sum(q => q.Count);
        queueLengths.Add(totalLength);
    }

    private void ScheduleNextArrival()
    {
        float nextArrivalTime = clock + random.Exponential(meanArrivalTime);
        Debug.Log($"Scheduling next arrival at time: {nextArrivalTime}");
        
        SimEvent arrivalEvent = new SimEvent("arrival", nextArrivalTime);
        Plane newPlane = new Plane(clock);
        arrivalEvent.AddAttribute("Plane", newPlane);
        Schedule(arrivalEvent);
    }

    private void CheckSimulationCompletion()
    {
        // Check if we've processed all planes and all servers are idle
        bool allServersIdle = !serverStatus.Any(status => status);
        bool allQueuesEmpty = !queues.Any(q => q.Count > 0);
        bool allPlanesArrived = totalPlanes >= totalArrivals;
        
        Debug.Log($"[AirportSimulation] Checking completion - Servers Idle: {allServersIdle}, Queues Empty: {allQueuesEmpty}, All Planes Arrived: {allPlanesArrived}");

        if (allPlanesArrived && allServersIdle && allQueuesEmpty)
        {
            Debug.Log("[AirportSimulation] All planes processed. Ending simulation...");
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
                    
                    // Adjust orientation and position of the first plane in the queue to align more towards the servers
                    if (j == 0)
                    {
                        planeVisuals[queueList[j]].transform.rotation = Quaternion.Euler(0, 0, 0);
                        planeVisuals[queueList[j]].transform.position = new Vector3(
                            queueStartPositions[i].x,
                            PLANE_HEIGHT,
                            queueStartPositions[i].z + 5f // Adjust closer to the server
                        );
                    }
                }
            }
        }
    }

    private IEnumerator DelayServerRelease(int serverIndex)
    {
        yield return new WaitForSeconds(3f); // Increase delay to 3 seconds
        serverStatus[serverIndex] = false;
    }

    private IEnumerator DelayNextPlaneService(int serverIndex)
    {
        yield return new WaitForSeconds(2f); // Delay for 2 seconds
        
        // Try to get next plane from queues
        if (TryGetNextPlane(out Plane nextPlane))
        {
            StartService(nextPlane, serverIndex);
        }
    }
} 