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
    public const float PlaneFlySpeed = 60f;
    public const float PlaneLandingSpeed = 50f;
    public const float PlaneTaxiSpeed = 50f;
    public const float PlaneSize = 1f;
    public const float PlaneSpeed = 9f;
    public const float ArrivalPathLength = 45f;
    public const float PlaneHeight = 0.5f;

    public enum SimulationMode
    {
        Basic,
        AgentBased
    }

    [Header("Simulation Parameters")]
    [SerializeField] private SimulationMode simulationMode = SimulationMode.Basic;
    [SerializeField] public int NumberOfWaypoints = 10;
    [SerializeField] public int NumberOfServers = 10;
    [SerializeField] private float MeanServiceTime = 7f;
    [SerializeField] private int TotalPlanes = 20;

    [Header("Visualization")]
    [SerializeField] private GameObject AirlinerPrefab;

    [Header("Simulation State")]
    public bool IsInitialized = false;
    public int CurrentPlaneIndex = 0;
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

        Debug.Log("Starting simulation setup...");
        StartSimulation();
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

    private PlaneVisual CreatePlaneVisual()
    {
        GameObject planeObj = Instantiate(AirlinerPrefab);
        PlaneVisual visual = planeObj.AddComponent<PlaneVisual>();
        Debug.Log($"Created plane visual at position {planeObj.transform.position}");
        return visual;
    }

    public void StartSimulation()
    {
        Debug.Log("Simulation started.");
        CheckNextArrival();
    }

    private void CheckNextArrival()
    {
        Debug.Log($"Checking next arrival. CurrentPlaneIndex={CurrentPlaneIndex}, planesManager.GetPlaneCount()={planesManager.GetPlaneCount()}, IsArrivalClear={IsArrivalClear}, FindIdleServer()={FindIdleServer()}");
        // Only try to handle next arrival if we have planes left and conditions are met
        if (CurrentPlaneIndex < planesManager.GetPlaneCount() && IsArrivalClear && FindIdleServer() != -1)
        {
            HandleArrival();
        }
        else
        {
            Debug.Log("Conditions not met for next arrival. Waiting...");
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
        
        if (simulationMode == SimulationMode.Basic)
        {
            Debug.Log("Searching for next important plane to land");
            // In Basic mode, prioritize high-priority planes first
            plane = allPlanes.FirstOrDefault(p => p.HighPriority == 1 && !p.IsProcessed);
            Debug.Log($"Found {plane} plane");

            // If no high-priority planes, get the next unprocessed plane
            if (plane == null)
            {
                Debug.Log("No high-priority planes found, searching for next unprocessed plane");
                plane = allPlanes.FirstOrDefault(p => !p.IsProcessed);
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
        PlaneVisual visual = CreatePlaneVisual();
        PlaneVisuals[plane] = visual;
        
        // Mark arrival runway as busy and server as busy
        IsArrivalClear = false;
        ServerStatus[serverIndex] = 1;
        
        Debug.Log($"Plane {plane.PlaneID} is landing and will be serviced at server {serverIndex}");
        
        // Start landing sequence
        StartCoroutine(ExecuteLandingSequence(plane, visual, serverIndex));
        ServerStatus[serverIndex] = 2;
        
        // Move to next plane for next arrival
        CurrentPlaneIndex++;
    }

    private IEnumerator ExecuteLandingSequence(Plane plane, PlaneVisual visual, int serverIndex)
    {
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
        
        // Calculate positions relative to the runway
        Vector3 runwayStart = new Vector3(0, 0, 100f);  // Start of runway
        Vector3 runwayEnd = new Vector3(0, 0, 0);       // End of runway
        
        // Teleport to approach position (above runway start)
        visual.Teleport(runwayStart, PlaneFlyHeight);
        yield return new WaitForSeconds(0.1f);
        
        // Teleport to landing position (above runway end)
        visual.Teleport(runwayEnd, PlaneLandingHeight);
        yield return new WaitForSeconds(0.1f);
        
        // Teleport to server position
        Vector3 serverPosition = ServerPositions[serverIndex];
        visual.Teleport(serverPosition, PlaneLandingHeight);
        
        // Update server status
        ServerStatus[serverIndex] = 2;
        IsArrivalClear = true;
        
        Debug.Log($"Plane {plane.PlaneID} has landed at server {serverIndex}");
        
        // Start service and schedule departure
        StartCoroutine(ExecuteTeleportServiceAndDeparture(plane, visual, serverIndex));
    }

    private IEnumerator ExecuteTeleportServiceAndDeparture(Plane plane, PlaneVisual visual, int serverIndex)
    {
        float serviceStartTime = Time.time;
        planeServiceStartTimes[plane] = serviceStartTime;
        
        float serviceTime = plane.ServiceTime;
        Debug.Log($"Servicing plane {plane.PlaneID} for {serviceTime} seconds at server {serverIndex}");
        
        yield return new WaitForSeconds(serviceTime);
        yield return StartCoroutine(ExecuteTeleportDepartureSequence(plane, visual, serverIndex));
    }

    private IEnumerator ExecuteTeleportDepartureSequence(Plane plane, PlaneVisual visual, int serverIndex)
    {
        while (!IsDepartureWayClear())
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        IsDepartureClear = false;
        
        // Calculate runway positions for departure
        Vector3 runwayStart = new Vector3(0, 0, 0);     // Start of runway
        Vector3 runwayEnd = new Vector3(0, 0, -100f);    // End of runway
        
        // Teleport to runway start
        visual.Teleport(runwayStart, PlaneLandingHeight);
        yield return new WaitForSeconds(0.1f);
        
        // Take off from runway
        visual.Teleport(runwayEnd, PlaneFlyHeight);
        
        // Update server status
        ServerStatus[serverIndex] = 0;
        Debug.Log($"Plane {plane.PlaneID} has departed from server {serverIndex}");
        
        // Record service completion
        float serviceEndTime = Time.time;
        simulationReport.RecordPlaneServed(plane, planeServiceStartTimes[plane], serviceEndTime);
        simulationReport.RecordServerUsage(serverIndex, serviceEndTime - planeServiceStartTimes[plane]);
        
        // Clean up
        planeArrivalTimes.Remove(plane);
        planeServiceStartTimes.Remove(plane);
        PlaneVisuals.Remove(plane);
        
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

   