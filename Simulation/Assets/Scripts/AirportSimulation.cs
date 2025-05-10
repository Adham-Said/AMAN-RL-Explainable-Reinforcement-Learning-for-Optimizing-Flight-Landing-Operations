using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class AirportSimulation : DES
{
    public static AirportSimulation Instance { get; private set; }

    // Constants for visualization
    private const float PlaneFlyHeight = 200f;
    private const float PlaneLandingHeight = 4f;

    private const float PlaneFlySpeed = 60f;
    private const float PlaneLandingSpeed = 50f;
    private const float PlaneTaxiSpeed = 50f;

    private const float PlaneSize = 1f;
    private const float PlaneSpeed = 9f;
    private const float ArrivalPathLength = 45f;
    private const float PlaneHeight = 0.5f;

    [Header("Simulation Parameters")]
    [SerializeField] public int NumberOfWaypoints = 10;
    [SerializeField] public int NumberOfServers = 10;
    [SerializeField] public int NumberOfHolds = 4;
    [SerializeField] public float MeanServiceTime = 3f;
    [SerializeField] public float MeanArrivalTime = 1f;
    [SerializeField] public int TotalArrivals = 10;

    [Header("Visualization")]
    [SerializeField] private GameObject AirlinerPrefab;

    private int[] ServerStatus; //0= idle, 1= booked, 2= serving
    private int[] HoldStatus; //0= idle, 1= booked, 2= serving
    private float[] HoldBusyTime;
    public int TotalPlanes = 0;
    public int DelayedPlanes = 0;
    public int DivertedPlanes = 0;
    public float TotalDelayTime = 0f;
    public float[] ServerUtilization;
    public float[] ServerBusyTime;
    private bool IsInitialized = false;
    private bool IsArrivalClear = true;
    private bool IsDepartureClear = true;

    public Vector3[] Waypoints;
    public Vector3[] ServerPositions;
    public Vector3[] HoldPositions;
    private float Clock = 0f;

    private Dictionary<Plane, PlaneVisual> PlaneVisuals = new Dictionary<Plane, PlaneVisual>();

    public Vector3 GetWaypointsPosition(int index)
    {
        return (index >= 0 && index < Waypoints.Length) ? Waypoints[index] : Vector3.zero;
    }

    public Vector3 GetServerPosition(int index)
    {
        return (index >= 0 && index < ServerPositions.Length) ? ServerPositions[index] : Vector3.zero;
    }

    public Vector3 GetHoldPosition(int index)
    {
        return (index >= 0 && index < HoldPositions.Length) ? HoldPositions[index] : Vector3.zero;
    }

    public void SetParameters(int totalArrivals, float meanServiceTime, float meanArrivalTime, float timeScale = 1f)
    {
        this.TotalArrivals = totalArrivals;
        this.MeanServiceTime = meanServiceTime;
        this.MeanArrivalTime = meanArrivalTime;

        ServerStatus = new int[NumberOfServers];
        ServerUtilization = new float[NumberOfServers];
        ServerBusyTime = new float[NumberOfServers];

        HoldStatus = new int[NumberOfHolds];
        HoldBusyTime = new float[NumberOfHolds];

        IsInitialized = true;
        Debug.Log($"Parameters set: Total Arrivals={totalArrivals}, Mean Service Time={meanServiceTime}, Mean Arrival Time={meanArrivalTime}");
    }

    protected override void Awake()
    {
        base.Awake();
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
        InitializeHoldPositions();
    }

    private void Start()
    {
        Debug.Log("Starting simulation setup...");
        SetParameters(TotalArrivals, MeanServiceTime, MeanArrivalTime);
        StartSimulation();
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

    private void InitializeHoldPositions()
    {
        HoldPositions = new Vector3[NumberOfHolds];
        for (int i = 0; i < NumberOfHolds; i++)
        {
            GameObject hold = GameObject.Find($"Hold {i}");
            if (hold != null)
            {
                HoldPositions[i] = hold.transform.position;
                Debug.Log($"Hold {i} initialized at position {HoldPositions[i]}");
            }
            else
            {
                Debug.LogError($"Could not find hold {i} in the scene!");
            }
        }
    }

    public void StartSimulation()
    {
        Debug.Log("Simulation started.");
        ScheduleNextArrival();
    }

    protected override void HandleEvent(SimEvent e)
    {
        string eventType = e.GetAttributeValue<string>("Type");
        Debug.Log($"Handling event of type: {eventType} at time {Clock}");

        switch (eventType)
        {
            case "start":
                ScheduleNextArrival();
                break;
            case "arrival":
                HandleArrival(e);
                break;
            case "Hold":
                HandleHold(e);
                break;
            case "departure":
                HandleDeparture(e);
                break;
            case "end":
                Report();
                break;
        }
    }

    private void ScheduleNextArrival()
    {
        Debug.Log("Scheduling next arrival...");
        float nextArrivalTime = Clock + random.Exponential(MeanArrivalTime);
        Debug.Log($"Scheduling next arrival at time: {nextArrivalTime}");
        
        SimEvent arrivalEvent = new SimEvent("arrival", nextArrivalTime);
        Plane newPlane = new Plane(Clock);
        arrivalEvent.AddAttribute("Plane", newPlane);
        Schedule(arrivalEvent);
    }

    private void HandleArrival(SimEvent e)
    {
        TotalPlanes++;
        Plane plane = e.GetAttributeValue<Plane>("Plane");
        Debug.Log($"Handling arrival of plane {TotalPlanes} at time {Clock}");
        
        // Create visual representation at arrival position
        PlaneVisual visual = CreatePlaneVisual();
        PlaneVisuals[plane] = visual;
        
        // Checks if their are idle server and the airstrip is clear
        int serverIndex = FindIdleServer();
        if (serverIndex == -1) 
        {
            Debug.Log($"No idle server found for plane {TotalPlanes}, adding plane {TotalPlanes} to hold");
            AddToHold(plane);
            if (!IsArrivalWayClear())
            {
                Debug.Log($"There is already other plane landing, adding plane {TotalPlanes} to hold");
                AddToHold(plane);
            }   
        }

        IsArrivalClear = false;
        Debug.Log($"Arrival runway is busy landing plane {TotalPlanes}");

        ServerStatus[serverIndex] = 1;
        Debug.Log($"Booking server {serverIndex} for plane {TotalPlanes}");

        Debug.Log($"Plane {TotalPlanes} is landing");
        // Start the plane at arrival position
        
        // Approach
        visual.MoveBetweenWaypoints(Waypoints[0], Waypoints[1], PlaneFlyHeight, PlaneFlyHeight, PlaneFlySpeed, PlaneFlySpeed);

        // Descend
        visual.MoveBetweenWaypoints(Waypoints[1], Waypoints[2], PlaneFlyHeight, PlaneLandingHeight, PlaneFlySpeed, PlaneLandingSpeed);

        // Land
        visual.MoveBetweenWaypoints(Waypoints[2], Waypoints[3], PlaneLandingHeight, PlaneLandingHeight, PlaneLandingSpeed, PlaneTaxiSpeed);
        
        IsArrivalClear = true;

        Debug.Log($"Plane {TotalPlanes} is done landing, arrival runway is clear, heading to server {serverIndex}");

        if (serverIndex < 6){
            //WIP
        }
        else
        {
            //WIP
        }

        // Plane reached the server
        ServerStatus[serverIndex] = 2;
        
        // Schedule next arrival if we haven't reached total arrivals
        if (TotalPlanes < TotalArrivals)
        {
            Debug.Log($"Plane {TotalPlanes} is at server {serverIndex}, scheduling next arrival");
            ScheduleNextArrival();
        }
        else
        {
            Debug.Log($"Plane {TotalPlanes} is at server {serverIndex}, simulation is complete");
            CheckSimulationCompletion();
        }
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

    private int FindIdleHold()
    {
        for (int i = 0; i < HoldStatus.Length; i++)
        {
            if (HoldStatus[i] == 0)
                return i;
        }
        return -1;
    }

    private bool IsArrivalWayClear(){
        return IsArrivalClear;
    }

    private bool IsDepartureWayClear(){
        return IsDepartureClear;
    }



    private void StartService(Plane plane, int serverIndex)
    {
        ServerStatus[serverIndex] = 2;
        Debug.Log($"Plane {plane} assigned to server {serverIndex}.");
    }

    private void StartHold(Plane plane, int holdIndex)
    {
        HoldStatus[holdIndex] = 2;
        Debug.Log($"Plane {plane} assigned to hold {holdIndex}.");
    }

    private PlaneVisual CreatePlaneVisual()
    {
        GameObject planeObj = Instantiate(AirlinerPrefab);
        PlaneVisual visual = planeObj.AddComponent<PlaneVisual>();
        return visual;
    }

    private void AddToHold(Plane plane)
    {
        int holdIndex = FindIdleHold();
        if (holdIndex != -1)
        {
            Debug.Log($"Adding plane {plane} to hold {holdIndex}");
            HoldStatus[holdIndex] = 1;

            // PlaneVisual visual = PlaneVisuals[plane];
            // visual.ApproachHold(
            //     HoldPositions, // pass the HoldPositions array
            //     holdIndex, // pass the holdIndex
            //     PlaneFlyHeight, // approach height
            //     PlaneFlySpeed // approach speed
            // );

            SimEvent releaseEvent = new SimEvent("releaseHold", Clock);
            releaseEvent.AddAttribute("HoldIndex", holdIndex);
            releaseEvent.AddAttribute("Plane", plane);
            Schedule(releaseEvent);
        }
        else
        {
            Debug.LogError($"No available hold for plane {plane}. Plane is diverted.");
            DivertedPlanes++;
        }
    }

    private void HandleHold(SimEvent e)
    {
        int holdIndex = e.GetAttributeValue<int>("HoldIndex");
        Plane plane = e.GetAttributeValue<Plane>("Plane");

        int serverIndex = FindIdleServer();
        if (serverIndex != -1)
        {
            Debug.Log($"Releasing plane {plane} from hold {holdIndex} to server {serverIndex}");
            HoldStatus[holdIndex] = 0;
            StartService(plane, serverIndex);
        }
        else
        {
            Debug.Log($"No idle server available for plane {plane} in hold {holdIndex}. Rescheduling release.");
            SimEvent rescheduleEvent = new SimEvent("releaseHold", Clock + 1f);
            rescheduleEvent.AddAttribute("HoldIndex", holdIndex);
            rescheduleEvent.AddAttribute("Plane", plane);
            Schedule(rescheduleEvent);
        }
    }


    private void HandleDeparture(SimEvent e)
    {
        Debug.Log("Departure logic not implemented yet.");
    }

    // Checks if all planes have completed their arrivals and ends the simulation, then calls for the report
    private void CheckSimulationCompletion()
    {
        if (TotalPlanes == TotalArrivals)
        {
            Debug.Log("Simulation complete: All planes have arrived. Generating report...");
            Report();
        }
        else
        {
            Debug.Log($"Simulation not complete: {TotalPlanes} of {TotalArrivals} planes have arrived.");
        }
    }

    public override void Report()
    {
        Debug.Log("Simulation report generated.");
        // Add logic to generate and print the simulation report
    }
}
