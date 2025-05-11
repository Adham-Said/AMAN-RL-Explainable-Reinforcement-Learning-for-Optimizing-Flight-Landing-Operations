using System;
using System.Collections.Generic;
using UnityEngine;

public class Report
{
    private float simulationStartTime;
    private float simulationEndTime;
    private int totalPlanesProcessed;
    private int highPriorityPlanes;
    private int lowPriorityPlanes;
    private float totalServiceTime;
    private float[] serverBusyTime;
    private int[] serverUsageCount;
    private int numberOfServers;
    private float totalWaitingTime;
    private float maxWaitingTime;
    private int planesWithWaitTime;

    public Report(int numberOfServers)
    {
        this.numberOfServers = numberOfServers;
        serverBusyTime = new float[numberOfServers];
        serverUsageCount = new int[numberOfServers];
        Reset();
    }

    public void Reset()
    {
        simulationStartTime = Time.time;
        simulationEndTime = 0f;
        totalPlanesProcessed = 0;
        highPriorityPlanes = 0;
        lowPriorityPlanes = 0;
        totalServiceTime = 0f;
        totalWaitingTime = 0f;
        maxWaitingTime = 0f;
        planesWithWaitTime = 0;
        
        for (int i = 0; i < numberOfServers; i++)
        {
            serverBusyTime[i] = 0f;
            serverUsageCount[i] = 0;
        }
    }

    public void RecordPlaneServed(Plane plane, float serviceStartTime, float serviceEndTime)
    {
        totalPlanesProcessed++;
        
        if (plane.HighPriority == 1)
            highPriorityPlanes++;
        else
            lowPriorityPlanes++;

        float serviceTime = serviceEndTime - serviceStartTime;
        totalServiceTime += serviceTime;
        
        // Record waiting time if available
        if (serviceStartTime > plane.ArrivalTime)
        {
            float waitTime = serviceStartTime - plane.ArrivalTime;
            totalWaitingTime += waitTime;
            if (waitTime > maxWaitingTime)
                maxWaitingTime = waitTime;
            planesWithWaitTime++;
        }
    }

    public void RecordServerUsage(int serverIndex, float busyTime)
    {
        if (serverIndex >= 0 && serverIndex < numberOfServers)
        {
            serverBusyTime[serverIndex] += busyTime;
            serverUsageCount[serverIndex]++;
        }
    }

    public void FinalizeReport()
    {
        simulationEndTime = Time.time;
    }

    public string GenerateReport()
    {
        float totalSimulationTime = simulationEndTime - simulationStartTime;
        float avgServiceTime = totalPlanesProcessed > 0 ? totalServiceTime / totalPlanesProcessed : 0f;
        float avgWaitingTime = planesWithWaitTime > 0 ? totalWaitingTime / planesWithWaitTime : 0f;
        
        string report = "=== Airport Simulation Report ===\n";
        report += $"Simulation Time: {totalSimulationTime:F2} seconds\n";
        report += $"Total Planes Processed: {totalPlanesProcessed}\n";
        report += $"  - High Priority: {highPriorityPlanes}\n";
        report += $"  - Low Priority: {lowPriorityPlanes}\n";
        report += $"Average Service Time: {avgServiceTime:F2} seconds\n";
        report += $"Average Waiting Time: {avgWaitingTime:F2} seconds\n";
        report += $"Maximum Waiting Time: {maxWaitingTime:F2} seconds\n";
        
        // Server utilization
        report += "\nServer Utilization:\n";
        for (int i = 0; i < numberOfServers; i++)
        {
            float utilization = totalSimulationTime > 0 ? (serverBusyTime[i] / totalSimulationTime) * 100 : 0;
            report += $"Server {i + 1}: {utilization:F1}% busy ({serverUsageCount[i]} planes served)\n";
        }
        
        // Additional metrics
        float avgPlanesPerMinute = totalSimulationTime > 0 ? (totalPlanesProcessed / totalSimulationTime) * 60 : 0;
        report += $"\nAverage Throughput: {avgPlanesPerMinute:F2} planes per minute\n";
        
        return report;
    }

    public void LogReport()
    {
        string report = GenerateReport();
        Debug.Log(report);
    }
}
