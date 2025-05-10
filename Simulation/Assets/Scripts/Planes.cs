using System;
using System.Collections.Generic;
using UnityEngine;

public class Planes
{
    private List<Plane> planes;
    private Random random;

    public Planes()
    {
        planes = new List<Plane>();
        random = new Random();
    }

    /// <summary>
    /// Initializes a collection of planes with randomized statuses based on provided percentages
    /// </summary>
    /// <param name="numPlanes">Total number of planes to create</param>
    /// <param name="lowFuelPercentage">Percentage of planes with low fuel (0-100)</param>
    /// <param name="inTransitPercentage">Percentage of planes in transit (0-100)</param>
    /// <param name="highPriorityPercentage">Percentage of high priority planes (0-100)</param>
    /// <param name="emergencyPercentage">Percentage of emergency planes (0-100)</param>
    public void InitializePlanes(int numPlanes,
     float lowFuelPercentage, float inTransitPercentage, 
     float highPriorityPercentage, float emergencyPercentage,
     float meanServiceTime)
    {
        planes.Clear();
        
        for (int i = 0; i < numPlanes; i++)
        {
            // Generate generic data
            float planeID = i + 1;
            float serviceTime = random.Next((int)meanServiceTime - 5, (int)meanServiceTime + 10);

            // Generate random status based on percentages
            bool isLowFuel = random.Next(0, 100) < lowFuelPercentage;
            bool isInTransit = random.Next(0, 100) < inTransitPercentage;
            bool isHighPriority = random.Next(0, 100) < highPriorityPercentage;
            bool isEmergency = random.Next(0, 100) < emergencyPercentage;

            // Create and add plane
            planes.Add(new Plane(
                planeID,
                serviceTime,
                -1, // No server assigned initially
                -1, // No hold assigned initially
                isLowFuel,
                isInTransit,
                isHighPriority,
                isEmergency));
        }
    }

    /// <summary>
    /// Adds a new plane to the collection
    /// </summary>
    public void AddPlane(Plane plane)
    {
        planes.Add(plane);
    }

    /// <summary>
    /// Removes a plane from the collection by ID
    /// </summary>
    public bool RemovePlane(float planeID)
    {
        var plane = planes.Find(p => p.PlaneID == planeID);
        if (plane != null)
        {
            planes.Remove(plane);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Updates an existing plane's data
    /// </summary>
    public bool UpdatePlane(float planeID, Plane updatedPlane)
    {
        var index = planes.FindIndex(p => p.PlaneID == planeID);
        if (index != -1)
        {
            planes[index] = updatedPlane;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a plane by ID
    /// </summary>
    public Plane GetPlane(float planeID)
    {
        return planes.Find(p => p.PlaneID == planeID);
    }

    /// <summary>
    /// Gets all planes in the collection
    /// </summary>
    public List<Plane> GetAllPlanes()
    {
        return new List<Plane>(planes);
    }

    /// <summary>
    /// Gets the number of planes in the collection
    /// </summary>
    public int GetPlaneCount()
    {
        return planes.Count;
    }

    /// <summary>
    /// Gets planes with specific status
    /// </summary>
    public List<Plane> GetPlanesByStatus(bool lowFuel = false, bool inTransit = false, 
        bool highPriority = false, bool emergency = false)
    {
        return planes.FindAll(p => 
            (p.FuelLow == lowFuel) && 
            (p.InTransit == inTransit) && 
            (p.HighPriority == highPriority) && 
            (p.Emergency == emergency));
    }
}
