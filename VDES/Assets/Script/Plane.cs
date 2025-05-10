using UnityEngine;

public class Plane
{
    public float ArrivalTime { get; private set; }
    public float ServiceStartTime { get; set; }
    public float ServiceEndTime { get; set; }
    public float DepartureTime { get; set; }
    public Vector3 Position { get; set; }  // For visual representation
    public int AssignedGate { get; set; } = -1;  // -1 means no gate assigned

    public Plane(float arrivalTime)
    {
        ArrivalTime = arrivalTime;
        ServiceStartTime = -1f;
        ServiceEndTime = -1f;
        DepartureTime = -1f;
        Position = Vector3.zero;  // Initial position
        AssignedGate = -1;  // No gate assigned initially
    }
} 