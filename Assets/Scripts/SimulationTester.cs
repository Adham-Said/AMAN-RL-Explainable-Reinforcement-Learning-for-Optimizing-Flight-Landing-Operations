using UnityEngine;

public class SimulationTester : MonoBehaviour
{
    private AirportSimulation airportSimulation;
    [SerializeField] private int totalArrivals = 100;
    [SerializeField] private float meanServiceTime = 30f;
    [SerializeField] private float meanArrivalTime = 10f;
    [SerializeField] private float timeScale = 1f;  // 1 = real time, 2 = double speed, etc.

    void Start()
    {
        RunSimulation();
    }

    public void RunSimulation()
    {
        // Clean up any existing simulation
        if (airportSimulation != null)
        {
            Destroy(airportSimulation.gameObject);
        }

        // Create new simulation
        GameObject simObject = new GameObject("AirportSimulation");
        airportSimulation = simObject.AddComponent<AirportSimulation>();
        
        // Set parameters and start
        airportSimulation.SetParameters(
            totalArrivals: totalArrivals,
            meanServiceTime: meanServiceTime,
            meanArrivalTime: meanArrivalTime,
            timeScale: timeScale
        );
        
        // Start the simulation
        airportSimulation.StartSimulation();
    }

    // Optional: Add a method to manually restart the simulation
    public void RestartSimulation()
    {
        RunSimulation();
    }

    // Add method to change time scale during runtime
    public void SetTimeScale(float newTimeScale)
    {
        timeScale = newTimeScale;
        if (airportSimulation != null)
        {
            airportSimulation.SetTimeScale(newTimeScale);
        }
    }
} 