using UnityEngine;

public class SimulationReport
{
    public void GenerateReport(AirportSimulation simulation)
    {
        Debug.Log("=== AIRPORT SIMULATION REPORT ===");

        // Print simulation parameters
        Debug.Log($"Simulation Parameters:");
        Debug.Log($"- Number of Waypoints: {simulation.NumberOfWaypoints}");
        Debug.Log($"- Number of Servers: {simulation.NumberOfServers}");
        Debug.Log($"- Number of Holds: {simulation.NumberOfHolds}");
        Debug.Log($"- Mean Service Time: {simulation.MeanServiceTime}");
        Debug.Log($"- Mean Arrival Time: {simulation.MeanArrivalTime}");
        Debug.Log($"- Total Arrivals: {simulation.TotalArrivals}");

        // Print simulation results
        Debug.Log($"Total Planes: {simulation.TotalPlanes}");
        Debug.Log($"Delayed Planes: {simulation.DelayedPlanes}");
        Debug.Log($"Total Delay Time: {simulation.TotalDelayTime:F2} minutes");
        Debug.Log($"Average Delay Time: {(simulation.DelayedPlanes > 0 ? simulation.TotalDelayTime / simulation.DelayedPlanes : 0):F2} minutes");

        // Print server utilization
        Debug.Log("Server Utilization:");
        for (int i = 0; i < simulation.NumberOfServers; i++)
        {
            Debug.Log($"- Server {i + 1}: {simulation.ServerUtilization[i]:P2}");
        }

        Debug.Log("=================================");
    }
}
