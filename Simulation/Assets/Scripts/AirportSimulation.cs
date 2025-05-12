using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class AirportSimulation : MonoBehaviour
{
    public static AirportSimulation Instance { get; private set; }

    // Constants for visualization
    public const float PlaneFlyHeight = 200f;
    public const float PlaneLandingHeight = 4f;
    public const float PlaneFlySpeed = 100f;
    public const float PlaneLandingSpeed = 80f;
    public const float PlaneTaxiSpeed = 50f;
    public const float PlaneSize = 1f;
    public const float PlaneSpeed = 9f;
    public const float ArrivalPathLength = 45f;
    public const float PlaneHeight = 0.5f;

    [Header("Simulation Parameters")]
    [Tooltip("When checked, uses basic simulation mode. When unchecked, uses agent-based mode.")]
    [SerializeField] private bool useBasicMode = true;
    [SerializeField] public int NumberOfWaypoints = 10;
    [SerializeField] public int NumberOfServers = 10;
    [SerializeField] private float MeanServiceTime = 5f;
    [SerializeField] private int TotalPlanes = 20;

    [Header("Visualization")]
    [SerializeField] private GameObject AirlinerPrefab;

    [Header("Agent Settings")]
    [SerializeField] private bool useAgentMode = true;
    private AgentController agentController;
    private List<Plane> pendingPlanes = new List<Plane>();
    private bool isProcessingLanding = false;

    [Header("Simulation State")]
    public bool IsInitialized = false;
    public float waitTime = 10f; // Time to wait between plane arrivals
    public int CurrentPlaneIndex = 0;
    private float nextArrivalTime;
    private int[] ServerStatus; // 0 = idle, 1 = booked, 2 = serving
    private Planes planesManager;
    private bool IsArrivalClear = true;
    private bool IsDepartureClear = true;

    public Vector3[] Waypoints;
    public Vector3[] ServerPositions;
    private float Clock = 0f;
    private Dictionary<Plane, PlaneVisual> PlaneVisuals = new Dictionary<Plane, PlaneVisual>();
    private Report simulationReport;
    private Dictionary<Plane, float> planeArrivalTimes = new Dictionary<Plane, float>();
    private Dictionary<Plane, float> planeServiceStartTimes = new Dictionary<Plane, float>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize ServerStatus array with default size
        ServerStatus = new int[NumberOfServers];
        
        InitializeWaypoints();
        InitializeServerPositions();
    }

    private void OnDestroy()
    {
        if (agentController != null)
        {
            agentController.OnDecisionMade -= HandleAgentDecision;
        }
    }

    private void Start()
    {
        simulationReport = new Report(NumberOfServers);
        // Initialize the planes manager with planes and various status percentages
        planesManager = new Planes();
        planesManager.InitializePlanes(
            numPlanes: TotalPlanes,
            highPriorityPercentage: 10f,
            meanServiceTime: MeanServiceTime
        );

        Debug.Log($"Initialized {planesManager.GetPlaneCount()} planes");
        
        if (!IsInitialized)
        {
            Debug.LogError("Simulation parameters not set! Call SetParameters() first.");
            return;
        }

        Debug.Log($"Starting simulation in {(useBasicMode ? "Basic" : "Agent-Based")} mode...");
        
        if (useBasicMode)
        {
            StartSimulation();
        }
        else
        {
            agentController = gameObject.AddComponent<AgentController>();
            agentController.OnDecisionMade += HandleAgentDecision;
            StartSimulation();
        }
    }

    public void SetParameters(int totalArrivals, float meanServiceTime)
    {
        this.MeanServiceTime = meanServiceTime;
        this.TotalPlanes = totalArrivals;
        IsInitialized = true;
        Debug.Log($"Parameters set: Mean Service Time={meanServiceTime}, Number of Servers={NumberOfServers}, TotalPlanes={totalArrivals}");
    }

    private void InitializeWaypoints()
    {
        Waypoints = new Vector3[NumberOfWaypoints];
        for (int i = 0; i < NumberOfWaypoints; i++)
        {
            GameObject waypoint = GameObject.Find($"Waypoint {i}");
            if (waypoint != null)
            {
                Waypoints[i] = waypoint.transform.position;
                Debug.Log($"Waypoint {i} initialized at position {Waypoints[i]}");
            }
            else
            {
                Debug.LogError($"Could not find Waypoint {i} in the scene!");
            }
        }
        Debug.Log($"Initialized {Waypoints.Length} waypoints");
    }

    private void InitializeServerPositions()
    {
        ServerPositions = new Vector3[NumberOfServers];
        for (int i = 0; i < NumberOfServers; i++)
        {
            GameObject server = GameObject.Find($"Server {i}");
            if (server != null)
            {
                ServerPositions[i] = server.transform.position;
                Debug.Log($"Server {i} initialized at position {ServerPositions[i]}");
            }
            else
            {
                Debug.LogError($"Could not find server {i} in the scene!");
            }
        }
        Debug.Log($"Initialized {ServerPositions.Length} servers");
    }

    private PlaneVisual CreatePlaneVisual(Plane plane)
    {
        // Create the plane at the spawn point
        GameObject planeObj = Instantiate(AirlinerPrefab, Waypoints[0], Quaternion.identity);
        PlaneVisual visual = planeObj.AddComponent<PlaneVisual>();
        planeObj.name = $"Plane {plane.PlaneID}";
        Debug.Log($"Created plane {planeObj.name} at {Waypoints[0]}");
        return visual;
    }

    public void StartSimulation()
    {
        Debug.Log("Simulation started. Waiting 10 seconds before starting arrivals...");
        nextArrivalTime = Time.time + 10f; // Set the time for first arrival to 10 seconds from now
    }

    private void Update()
    {
        // Check if it's time for the next arrival
        if (Time.time >= nextArrivalTime)
        {
            CheckNextArrival();
            // Set next arrival time to a very large number to prevent multiple checks
            nextArrivalTime = float.MaxValue;
        }
    }

    private void HandleAgentDecision(int planeIndex)
    {
        if (planeIndex >= 0 && planeIndex < pendingPlanes.Count)
        {
            Plane selectedPlane = pendingPlanes[planeIndex];
            if (!selectedPlane.IsProcessed)
            {
                pendingPlanes.RemoveAt(planeIndex);
                StartCoroutine(ExecuteLandingSequence(selectedPlane));
            }
        }
        isProcessingLanding = false;
        
        // Process next plane if any
        if (useAgentMode && pendingPlanes.Count > 0)
        {
            ProcessNextPlaneWithAgent();
        }
    }

    private void ProcessNextPlaneWithAgent()
    {
        if (isProcessingLanding || pendingPlanes.Count == 0) return;
        
        isProcessingLanding = true;
        agentController.RequestDecision(pendingPlanes.ToArray());
    }

    private void CheckNextArrival()
    {
        Debug.Log($"Checking next arrival. CurrentPlaneIndex={CurrentPlaneIndex}, planesManager.GetPlaneCount()={planesManager.GetPlaneCount()}, IsArrivalClear={IsArrivalClear}, FindIdleServer()={FindIdleServer()}");
        
        if (CurrentPlaneIndex >= planesManager.GetPlaneCount())
        {
            Debug.Log("All planes have arrived");
            return;
        }

        // Get the next plane
        var allPlanes = planesManager.GetAllPlanes();
        Plane plane = allPlanes[CurrentPlaneIndex];
        
        if (useAgentMode)
        {
            // In agent mode, add to pending planes and let agent decide
            pendingPlanes.Add(plane);
            plane.IsProcessed = true;
            plane.ArrivalTime = Time.time;
            planeArrivalTimes[plane] = Time.time;
            CurrentPlaneIndex++;
            
            // Process the plane with agent
            ProcessNextPlaneWithAgent();
        }
        else
        {
            // In basic mode, handle immediately if conditions are met
            if (IsArrivalClear && FindIdleServer() != -1)
            {
                HandleArrival();
            }
            else
            {
                Debug.Log("Conditions not met for next arrival. Waiting...");
            }
        }
    }

    private void HandleArrival()
    {
        // Check if we can land a plane (runway clear and server available)
        if (!IsArrivalClear)
        {
            Debug.Log("Arrival path is busy, waiting...");
            return;
        }

        int serverIndex = FindIdleServer();
        if (serverIndex == -1)
        {
            Debug.Log("No available servers, waiting...");
            return;
        }

        // Get the next plane based on simulation mode
        Plane plane = null;
        var allPlanes = planesManager.GetAllPlanes();
        
        if (useBasicMode)
        {
            Debug.Log("Searching for next important plane to land");
            // In Basic mode, prioritize high-priority planes first
            plane = allPlanes.FirstOrDefault(p => p.HighPriority == 1 && !p.IsProcessed);
            
            // If no high-priority planes, get the next unprocessed plane
            if (plane == null)
            {
                Debug.Log("No high-priority planes found, searching for next unprocessed plane");
                plane = allPlanes.FirstOrDefault(p => !p.IsProcessed);
            }
            else
            {
                Debug.Log($"Found high-priority plane {plane.PlaneID}");
            }
        }
        else
        {
            // Agent-Based mode (to be implemented)
            // For now, just get the next unprocessed plane in order
            if (CurrentPlaneIndex < allPlanes.Count)
            {
                plane = allPlanes[CurrentPlaneIndex];
            }
        }

        if (plane == null)
        {
            Debug.Log("No more planes to process");
            CheckSimulationCompletion();
            return;
        }
        
        // Mark the plane as processed
        plane.IsProcessed = true;
        
        // Record arrival time for reporting
        plane.ArrivalTime = Time.time;
        planeArrivalTimes[plane] = Time.time;

        Debug.Log($"Handling arrival of plane {plane.PlaneID} at time {Clock}");
        
        // Mark the plane as being serviced
        plane.ServerIndex = serverIndex;
        
        // Create visual representation at arrival position
        PlaneVisual visual = CreatePlaneVisual(plane);
        PlaneVisuals[plane] = visual;
        
        // Mark arrival runway as busy and server as busy
        IsArrivalClear = false;
        ServerStatus[serverIndex] = 1;
        
        Debug.Log($"Plane {plane.PlaneID} is landing and will be serviced at server {serverIndex}");
        
        // Start landing sequence
        StartCoroutine(ExecuteLandingSequence(plane, visual, serverIndex));
        ServerStatus[serverIndex] = 1;
        
        // Move to next plane for next arrival
        CurrentPlaneIndex++;
    }

    private IEnumerator ExecuteLandingSequence(Plane plane, PlaneVisual visual = null, int serverIndex = -1){
        // If serverIndex not provided, find an available one
        if (serverIndex == -1)
        {
            serverIndex = FindIdleServer();
            if (serverIndex == -1)
            {
                Debug.LogError("No available servers for landing sequence");
                yield break;
            }
        }
        
        if (ServerPositions == null || ServerPositions.Length == 0)
        {
            Debug.LogError("No server positions available");
            yield break;
        }
        
        if (serverIndex < 0 || serverIndex >= ServerPositions.Length)
        {
            Debug.LogError($"Invalid server index: {serverIndex}");
            yield break;
        }
        
        // Mark the server as booked
        ServerStatus[serverIndex] = 1;
        plane.ServerIndex = serverIndex;
        
        // Create visual if not provided
        if (visual == null)
        {
            visual = CreatePlaneVisual(plane);
            if (visual == null) 
            {
                ServerStatus[serverIndex] = 0; // Free the server
                yield break;
            }
            PlaneVisuals[plane] = visual;
        }
        
        // move to waypoint 0
        yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[0], Waypoints[1], PlaneFlySpeed));
        
        // move to waypoint 1 (Approach)
        yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[1], Waypoints[2], PlaneFlySpeed));

        // move to waypoint 2 (Runway)
        yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[2], Waypoints[3], PlaneLandingSpeed));
        
        // Taxi to server position
        Vector3 serverPosition = ServerPositions[serverIndex];
        if(serverIndex<5){
            StartCoroutine(ExecuteTaxiFrom0to4ServersIn(plane, visual, serverIndex));
        }else{
            StartCoroutine(ExecuteTaxiFrom5to9ServersIn(plane, visual, serverIndex));
        }

        Debug.Log($"Plane {plane.PlaneID} has landed at server {serverIndex}");

        IsArrivalClear = true;
        ServerStatus[serverIndex] = 2;
        
        CheckSimulationCompletion();
    }

    private IEnumerator ExecuteTaxiFrom0to4ServersIn(Plane plane, PlaneVisual visual, int serverIndex){
        
        // take first turn
        yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[3], Waypoints[4], PlaneTaxiSpeed));
        yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[4], Waypoints[5], PlaneTaxiSpeed));
        yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[5], Waypoints[6], PlaneTaxiSpeed));

        switch (serverIndex)
        {
            case 0:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[6], Waypoints[7], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[7], Waypoints[8], PlaneTaxiSpeed));
                break;
            case 1:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[6], Waypoints[9], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[9], Waypoints[10], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[10], Waypoints[11], PlaneTaxiSpeed));
                break;
            case 2:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[6], Waypoints[12], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[12], Waypoints[13], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[13], Waypoints[14], PlaneTaxiSpeed));
                break;
            case 3:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[6], Waypoints[15], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[15], Waypoints[16], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[16], Waypoints[17], PlaneTaxiSpeed));
                break;
            case 4:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[6], Waypoints[18], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[18], Waypoints[19], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[19], Waypoints[20], PlaneTaxiSpeed));
                break;
            default:
                break;
        }

        float serviceStartTime = Time.time;
        planeServiceStartTimes[plane] = serviceStartTime;
        
        float serviceTime = plane.ServiceTime;
        Debug.Log($"Servicing plane {plane.PlaneID} for {serviceTime} seconds at server {serverIndex}");
        
        yield return new WaitForSeconds(serviceTime);
        while (!IsDepartureWayClear())
        {
            yield return new WaitForSeconds(1f);
        }
        
        // Start taxi out sequence after service is complete and way is clear
        StartCoroutine(ExecuteTaxiFrom0to4ServersOut(plane, visual, serverIndex));
    }

    private IEnumerator ExecuteTaxiFrom0to4ServersOut(Plane plane, PlaneVisual visual, int serverIndex){
        IsDepartureClear = false;
        ServerStatus[serverIndex] = 0;
        
        // Trigger new arrival check as soon as we start leaving the server
        CheckNextArrival();

        switch (serverIndex)
        {
            case 0:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[8], Waypoints[7], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[7], Waypoints[6], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[6], Waypoints[21], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[41], Waypoints[42], PlaneTaxiSpeed));
                break;
            case 1:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[11], Waypoints[10], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[10], Waypoints[9], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[9], Waypoints[21], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[21], Waypoints[41], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[41], Waypoints[42], PlaneTaxiSpeed));
                break;
            case 2:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[14], Waypoints[13], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[13], Waypoints[12], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[12], Waypoints[21], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[21], Waypoints[41], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[41], Waypoints[42], PlaneTaxiSpeed));
                break;
            case 3:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[17], Waypoints[16], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[16], Waypoints[15], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[15], Waypoints[21], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[21], Waypoints[41], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[41], Waypoints[42], PlaneTaxiSpeed));
                break;
            case 4:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[20], Waypoints[19], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[19], Waypoints[18], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[18], Waypoints[21], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[21], Waypoints[41], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[41], Waypoints[42], PlaneTaxiSpeed));
                break;
            default:
                break;
        }

        // Update server status
        IsDepartureClear = true;
        Debug.Log($"Plane {plane.PlaneID} has departed from server {serverIndex}");
        
        // Record service completion
        float serviceEndTime = Time.time;
        simulationReport.RecordPlaneServed(plane, planeServiceStartTimes[plane], serviceEndTime);
        simulationReport.RecordServerUsage(serverIndex, serviceEndTime - planeServiceStartTimes[plane]);

        // Start departure sequence and wait for it to complete
        yield return StartCoroutine(ExecuteDepartureSequence(plane, visual, serverIndex));

        // Clean up after departure sequence is complete
        planeArrivalTimes.Remove(plane);
        planeServiceStartTimes.Remove(plane);
        PlaneVisuals.Remove(plane);
        
        // Destroy the visual after a short delay
        if (visual != null && visual.gameObject != null)
        {
            Destroy(visual.gameObject);
        }
        
        CheckSimulationCompletion();
    }

    private IEnumerator ExecuteTaxiFrom5to9ServersIn(Plane plane, PlaneVisual visual, int serverIndex){
        // take Second turn
        yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[3], Waypoints[22], PlaneTaxiSpeed));
        yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[22], Waypoints[23], PlaneTaxiSpeed));
        yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[23], Waypoints[24], PlaneTaxiSpeed));

        switch (serverIndex)
        {
            case 5:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[24], Waypoints[25], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[25], Waypoints[26], PlaneTaxiSpeed));
                break;
            case 6:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[24], Waypoints[27], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[27], Waypoints[28], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[28], Waypoints[29], PlaneTaxiSpeed));
                break;
            case 7:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[24], Waypoints[30], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[30], Waypoints[31], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[31], Waypoints[32], PlaneTaxiSpeed));
                break;
            case 8:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[24], Waypoints[33], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[33], Waypoints[34], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[34], Waypoints[35], PlaneTaxiSpeed));
                break;
            case 9:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[24], Waypoints[36], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[36], Waypoints[37], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[37], Waypoints[38], PlaneTaxiSpeed));
                break;
            default:
                break;
        }

        float serviceStartTime = Time.time;
        planeServiceStartTimes[plane] = serviceStartTime;
        
        float serviceTime = plane.ServiceTime;
        Debug.Log($"Servicing plane {plane.PlaneID} for {serviceTime} seconds at server {serverIndex}");
        
        yield return new WaitForSeconds(serviceTime);
        while (!IsDepartureWayClear())
        {
            yield return new WaitForSeconds(1f);
        }
        
        // Start taxi out sequence after service is complete and way is clear
        StartCoroutine(ExecuteTaxiFrom5to9ServersOut(plane, visual, serverIndex));
    }

    private IEnumerator ExecuteTaxiFrom5to9ServersOut(Plane plane, PlaneVisual visual, int serverIndex){
        IsDepartureClear = false;
        ServerStatus[serverIndex] = 0;
        
        // Trigger new arrival check as soon as we start leaving the server
        CheckNextArrival();

        switch (serverIndex)
        {
            case 5:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[26], Waypoints[25], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[25], Waypoints[24], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[24], Waypoints[39], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[39], Waypoints[40], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[40], Waypoints[41], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[41], Waypoints[42], PlaneTaxiSpeed));
                break;
            case 6:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[29], Waypoints[28], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[28], Waypoints[27], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[27], Waypoints[39], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[39], Waypoints[40], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[40], Waypoints[41], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[41], Waypoints[42], PlaneTaxiSpeed));
                break;
            case 7:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[32], Waypoints[31], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[31], Waypoints[30], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[30], Waypoints[39], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[39], Waypoints[40], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[40], Waypoints[41], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[41], Waypoints[42], PlaneTaxiSpeed));
                break;
            case 8:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[35], Waypoints[34], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[34], Waypoints[33], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[33], Waypoints[39], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[39], Waypoints[40], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[40], Waypoints[41], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[41], Waypoints[42], PlaneTaxiSpeed));
                break;
            case 9:
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[38], Waypoints[37], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[37], Waypoints[36], PlaneTaxiSpeed, true));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[36], Waypoints[39], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[39], Waypoints[40], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[40], Waypoints[41], PlaneTaxiSpeed));
                yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[41], Waypoints[42], PlaneTaxiSpeed));
                break;
            default:
                break;
        }

        // Update server status
        IsDepartureClear = true;
        Debug.Log($"Plane {plane.PlaneID} has departed from server {serverIndex}");
        
        // Record service completion
        float serviceEndTime = Time.time;
        simulationReport.RecordPlaneServed(plane, planeServiceStartTimes[plane], serviceEndTime);
        simulationReport.RecordServerUsage(serverIndex, serviceEndTime - planeServiceStartTimes[plane]);

        // Start departure sequence and wait for it to complete
        yield return StartCoroutine(ExecuteDepartureSequence(plane, visual, serverIndex));

        // Clean up after departure sequence is complete
        planeArrivalTimes.Remove(plane);
        planeServiceStartTimes.Remove(plane);
        PlaneVisuals.Remove(plane);
        
        // Destroy the visual after a short delay
        if (visual != null && visual.gameObject != null)
        {
            Destroy(visual.gameObject);
        }
        
        CheckSimulationCompletion();
    }

    private IEnumerator ExecuteDepartureSequence(Plane plane, PlaneVisual visual, int serverIndex){
        // move to waypoint 0
        yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[42], Waypoints[43], PlaneTaxiSpeed));
        
        // move to waypoint 1 (Approach)
        yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[43], Waypoints[44], PlaneLandingSpeed));

        // move to waypoint 2 (Runway)
        yield return StartCoroutine(visual.MoveToWaypoint(Waypoints[44], Waypoints[45], PlaneFlySpeed));

        IsDepartureClear = true;
        
        Debug.Log($"Plane {plane.PlaneID} has departed from server {serverIndex}");

        // Clean up
        planeArrivalTimes.Remove(plane);
        planeServiceStartTimes.Remove(plane);
        PlaneVisuals.Remove(plane);
        
        // Destroy the plane object after a short delay
        if (visual != null && visual.gameObject != null)
        {
            Destroy(visual.gameObject);
        }

        CheckNextArrival();
        CheckSimulationCompletion();
    }

    private int FindIdleServer()
    {
        for (int i = 0; i < ServerStatus.Length; i++)
        {
            if (ServerStatus[i] == 0)
                return i;
        }
        return -1;
    }

    private bool IsArrivalWayClear()
    {
        return IsArrivalClear;
    }

    private bool IsDepartureWayClear()
    {
        return IsDepartureClear;
    }

    private void CheckSimulationCompletion()
    {
        if (CurrentPlaneIndex >= planesManager.GetPlaneCount() && PlaneVisuals.Count == 0)
        {
            bool allServersIdle = true;
            foreach (var status in ServerStatus)
            {
                if (status != 0)
                {
                    allServersIdle = false;
                    break;
                }
            }
            
            if (allServersIdle)
            {
                Debug.Log("Simulation complete: All planes have been processed and all servers are idle. Generating report...");
                GenerateReport();
            }
        }
    }

    // Checks if all planes have completed their arrivals and ends the simulation, then calls for the report
    private void GenerateReport()
    {
        if (simulationReport != null)
        {
            simulationReport.FinalizeReport();
            simulationReport.LogReport();
        }
        else
        {
            Debug.LogError("Simulation report is not initialized!");
        }
    }

}

   