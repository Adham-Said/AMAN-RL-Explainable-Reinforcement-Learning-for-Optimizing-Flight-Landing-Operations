using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class PlaneDTO
{
    public float PlaneID { get; set; }
    public int HighPriority { get; set; }
    
    public PlaneDTO(float planeID, int highPriority)
    {
        PlaneID = planeID;
        HighPriority = highPriority; // 1 for high priority, 0 for low priority
    }
}

[Serializable]
public class Plane
{
    public float PlaneID { get; set; }
    public float ServiceTime { get; set; }
    public int ServerIndex { get; set; }
    public int HighPriority { get; set; }

    public Plane(float planeID, float serviceTime,
        int serverIndex, int highPriority)
    {
        PlaneID = planeID;
        ServiceTime = serviceTime;
        ServerIndex = serverIndex;
        HighPriority = highPriority;
    }

    // Default constructor for serialization
    public Plane()
    {
    }
}

public class Planes
{
    private List<Plane> planes;
    private System.Random random;

    public Planes()
    {
        planes = new List<Plane>();
        random = new System.Random();
    }

    /// <summary>
    /// Initializes a collection of planes with randomized statuses based on provided percentages
    /// </summary>
    /// <param name="numPlanes">Total number of planes to create</param>
    /// <param name="highPriorityPercentage">Percentage of high priority planes (0-100)</param>
    public void InitializePlanes(int numPlanes,
        float highPriorityPercentage,
        float meanServiceTime)
    {
        planes.Clear();
        
        for (int i = 0; i < numPlanes; i++)
        {
            // Generate generic data
            float planeID = i + 1;
            float serviceTime = random.Next((int)meanServiceTime - 5, (int)meanServiceTime + 10);

            // Generate random status based on percentages
            int isHighPriority = random.Next(0, 100) < highPriorityPercentage ? 1 : 0;

            // Create and add plane
            planes.Add(new Plane(
                planeID,
                serviceTime,
                -1, // No server assigned initially
                isHighPriority));
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
    /// Gets an array of simplified plane data for Python integration
    /// </summary>
    /// <returns>Array of PlaneDTO objects containing only PlaneID and HighPriority</returns>
    public PlaneDTO[] GetPlanesForPython()
    {
        return planes.Select(p => new PlaneDTO(p.PlaneID, p.HighPriority)).ToArray();
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
    public List<Plane> GetPlanesByStatus(int highPriority = 0)
    {
        return planes.FindAll(p => 
            (p.HighPriority == highPriority));
    }
}
