using UnityEngine;
using System.Collections.Generic;
using System.Collections;

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

    [Header("Simulation Parameters")]
    [SerializeField] public int NumberOfWaypoints = 10;
    [SerializeField] public int NumberOfServers = 10;
    [SerializeField] private float MeanServiceTime = 30f;
    [SerializeField] private int TotalPlanes = 20;

    [Header("Visualization")]
    [SerializeField] private GameObject AirlinerPrefab;

    [Header("Simulation State")]
    public bool IsInitialized = false;
    public int CurrentPlaneIndex = 0;
    private int[] ServerStatus; // 0 = idle, 1 = busy, 2 = blocked
    private Planes planesManager;
    private bool IsArrivalClear = true;
    private bool IsDepartureClear = true;

    public Vector3[] Waypoints;
    public Vector3[] ServerPositions;
    private float Clock = 0f;
    private Dictionary<Plane, PlaneVisual> PlaneVisuals = new Dictionary<Plane, PlaneVisual>();

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

        InitializeWaypoints();
        InitializeServerPositions();
    }

    private void Start()
    {
        // Initialize the planes manager with planes and various status percentages
        planesManager = new Planes();
        planesManager.InitializePlanes(
            numPlanes: TotalPlanes,
            highPriorityPercentage: 20f,
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
        ServerStatus = new int[NumberOfServers];
        IsInitialized = true;
        Debug.Log($"Parameters set: Mean Service Time={meanServiceTime}, Number of Servers={NumberOfServers}");
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
    }

    private PlaneVisual CreatePlaneVisual()
    {
        GameObject planeObj = Instantiate(AirlinerPrefab);
        PlaneVisual visual = planeObj.AddComponent<PlaneVisual>();
        return visual;
    }

    public void StartSimulation()
    {
        Debug.Log("Simulation started.");
        CheckAndHandleNextArrival();
    }

    private void CheckAndHandleNextArrival()
    {
        // Only try to handle next arrival if we have planes left and conditions are met
        if (CurrentPlaneIndex < planesManager.GetPlaneCount() && IsArrivalClear && FindIdleServer() != -1)
        {
            HandleArrival();
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

        // Get the next plane
        var allPlanes = planesManager.GetAllPlanes();
        if (CurrentPlaneIndex >= allPlanes.Count)
        {
            Debug.Log("No more planes to process");
            CheckSimulationCompletion();
            return;
        }

        Plane plane = allPlanes[CurrentPlaneIndex];
        if (plane == null)
        {
            Debug.LogError($"Failed to get plane at index {CurrentPlaneIndex}");
            return;
        }

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
        
        // Move to next plane for next arrival
        CurrentPlaneIndex++;
        
        // Check if there are more planes to process
        if (CurrentPlaneIndex < allPlanes.Count)
        {
            Debug.Log("Waiting for next available slot to land next plane");
        }
        else
        {
            Debug.Log("No more planes to arrive");
        }
    }

    private IEnumerator ExecuteLandingSequence(Plane plane, PlaneVisual visual, int serverIndex)
    {
        // Approach
        visual.MoveBetweenWaypoints(Waypoints[0], Waypoints[1], PlaneFlyHeight, PlaneFlyHeight, PlaneFlySpeed, PlaneFlySpeed);
        yield return new WaitForSeconds(1.5f);
        
        // Descend
        visual.MoveBetweenWaypoints(Waypoints[1], Waypoints[2], PlaneFlyHeight, PlaneLandingHeight, PlaneFlySpeed, PlaneLandingSpeed);
        yield return new WaitForSeconds(1.5f);
        
        // Land
        visual.MoveBetweenWaypoints(Waypoints[2], Waypoints[3], PlaneLandingHeight, PlaneLandingHeight, PlaneLandingSpeed, PlaneTaxiSpeed);
        yield return new WaitForSeconds(2f);
        
        // Move to server position
        Vector3 serverPosition = ServerPositions[serverIndex];
        visual.MoveTo(serverPosition, PlaneLandingHeight, PlaneTaxiSpeed);
        
        // Mark server as in service
        ServerStatus[serverIndex] = 2;
        
        // Clear arrival runway
        IsArrivalClear = true;
        
        Debug.Log($"Plane {plane.PlaneID} has arrived at server {serverIndex}");
        
        // Check if we can land another plane now that the runway is clear
        CheckAndHandleNextArrival();
        
        // Start service and schedule departure
        StartCoroutine(ExecuteServiceAndDeparture(plane, visual, serverIndex));
    }

    private IEnumerator ExecuteServiceAndDeparture(Plane plane, PlaneVisual visual, int serverIndex)
    {
        // Service the plane (wait for service time)
        float serviceTime = plane.ServiceTime;
        Debug.Log($"Servicing plane {plane.PlaneID} for {serviceTime} seconds at server {serverIndex}");
        yield return new WaitForSeconds(serviceTime);
        
        // Handle departure
        yield return StartCoroutine(ExecuteDepartureSequence(plane, visual, serverIndex));
    }

    private IEnumerator ExecuteDepartureSequence(Plane plane, PlaneVisual visual, int serverIndex)
    {
        // Wait for departure way to be clear
        while (!IsDepartureWayClear())
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        // Mark departure way as busy
        IsDepartureClear = false;
        
        // Taxi to departure position
        visual.MoveTo(Waypoints[3], PlaneLandingHeight, PlaneTaxiSpeed);
        yield return new WaitForSeconds(1f);
        
        // Take off
        visual.MoveBetweenWaypoints(Waypoints[3], Waypoints[4], PlaneLandingHeight, PlaneFlyHeight, PlaneTaxiSpeed, PlaneFlySpeed);
        
        // Clear the server
        ServerStatus[serverIndex] = 0;
        
        // Mark departure way as clear
        IsDepartureClear = true;
        
        Debug.Log($"Plane {plane.PlaneID} has departed from server {serverIndex}");
        
        // Remove the plane visual after it's out of sight
        yield return new WaitForSeconds(2f);
        if (visual != null && visual.gameObject != null)
        {
            Destroy(visual.gameObject);
        }
        
        // Remove from tracking
        PlaneVisuals.Remove(plane);
        
        // Check if we can land another plane now that a server is free
        CheckAndHandleNextArrival();
        
        // Check if simulation is complete
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

    private void StartService(Plane plane, int serverIndex)
    {
        ServerStatus[serverIndex] = 2;
        Debug.Log($"Plane {plane.PlaneID} assigned to server {serverIndex}.");
    }

    private void HandleDeparture()
    {
        Debug.Log("Departure logic not implemented yet.");
    }

    // Checks if all planes have completed their arrivals and ends the simulation, then calls for the report
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
                Report();
            }
        }
    }
        
    public void Report()
    {
        Debug.Log("Simulation report generated.");
        // Add logic to generate and print the simulation report
    }

}

   