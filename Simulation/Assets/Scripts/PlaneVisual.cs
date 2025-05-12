using UnityEngine;
using DG.Tweening;
using System.Collections;
using System;

public class PlaneVisual : MonoBehaviour
{
    private const float ARRIVAL_THRESHOLD = 0.01f;

    private void Awake()
    {
        InitializePhysics();
        InitializeCollider();
    }

    private void InitializePhysics()
    {
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    private void InitializeCollider()
    {
        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
    }

    /// <summary>
    /// Spawns a plane at the specified waypoint with the given plane ID
    /// </summary>
    /// <param name="planeID">The ID of the plane</param>
    /// <param name="waypointIndex">The index of the waypoint to spawn at</param>
    /// <param name="airlinePrefab">The prefab to instantiate</param>
    public static PlaneVisual SpawnPlane(float planeID, int waypointIndex, GameObject airlinePrefab, Vector3[] waypoints)
    {
        if (waypointIndex < 0 || waypointIndex >= waypoints.Length)
        {
            Debug.LogError("Invalid waypoint index: " + waypointIndex);
            return null;
        }

        // Instantiate the plane prefab
        GameObject planeObj = Instantiate(airlinePrefab);
        PlaneVisual planeVisual = planeObj.GetComponent<PlaneVisual>();

        if (planeVisual == null)
        {
            Debug.LogError("Plane prefab does not have PlaneVisual component");
            Destroy(planeObj);
            return null;
        }

        // Set the plane's position
        Vector3 waypoint = waypoints[waypointIndex];
        planeObj.transform.position = new Vector3(waypoint.x, AirportSimulation.PlaneFlyHeight, waypoint.z);

        // Set the plane's ID
        planeVisual.GetComponent<Plane>().PlaneID = planeID;

        return planeVisual;
    }

    /// <summary>
    /// Performs a 90-degree turn around a waypoint
    /// </summary>
    /// <param name="waypoint">The waypoint to turn around</param>
    /// <param name="height">The height to maintain during the turn</param>
    /// <param name="speed">The speed of the turn</param>
    /// <param name="clockwise">Whether to turn clockwise or counter-clockwise</param>
    /// <param name="onComplete">Callback when the turn is complete</param>
    public void TurnAroundWaypoint(Vector3 waypoint, float height, float speed, bool clockwise, TweenCallback onComplete = null)
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + (waypoint - startPosition).normalized * 2f;
        
        // Calculate the center point for the 90-degree turn
        Vector3 center = (startPosition + endPosition) / 2f;
        center.y = height;
        
        startPosition.y = height;
        endPosition.y = height;

        DOTween.To(
            () => 0f,
            x => {
                float angle = x * (clockwise ? 1f : -1f) * Mathf.PI / 2f;
                transform.position = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 1f;
            },
            1f,
            2f / speed
        ).OnComplete(onComplete ?? (() => { }));
    }

    /// <summary>
    /// Moves the plane from one waypoint to another with linear speed transition
    /// </summary>
    /// <param name="startWaypoint">The starting waypoint</param>
    /// <param name="endWaypoint">The ending waypoint</param>
    /// <param name="startHeight">Starting height</param>
    /// <param name="endHeight">Ending height</param>
    /// <param name="startSpeed">Starting speed</param>
    /// <param name="endSpeed">Ending speed</param>
    /// <param name="onComplete">Callback when the movement is complete</param>
    public void MoveBetweenWaypoints(Vector3 startWaypoint, Vector3 endWaypoint, 
        float startHeight, float endHeight, 
        float startSpeed, float endSpeed, 
        TweenCallback onComplete = null)
    {
        Vector3 startPos = new Vector3(startWaypoint.x, startHeight, startWaypoint.z);
        Vector3 endPos = new Vector3(endWaypoint.x, endHeight, endWaypoint.z);
        
        float distance = Vector3.Distance(startPos, endPos);
        float avgSpeed = (startSpeed + endSpeed) / 2f;
        float duration = distance / avgSpeed;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOMove(endPos, duration)
            .SetEase(Ease.Linear));
        
        seq.OnComplete(onComplete ?? (() => { }));
        
        seq.Play();
    }

    /// <summary>
    /// Moves the plane to the specified position at the given height and speed
    /// </summary>
    /// <param name="targetPosition">The target position to move to (X and Z coordinates)</param>
    /// <param name="height">The height to maintain during movement</param>
    /// <param name="speed">The speed of movement</param>
    /// <param name="onComplete">Callback when the movement is complete</param>
    public void MoveTo(Vector3 targetPosition, float height, float speed, TweenCallback onComplete = null)
    {
        // Set the target position with the specified height
        Vector3 targetPos = new Vector3(targetPosition.x, height, targetPosition.z);
        
        // Calculate duration based on distance and speed
        float distance = Vector3.Distance(transform.position, targetPos);
        float duration = distance / speed;

        // Create and play the movement tween
        transform.DOMove(targetPos, duration)
            .SetEase(Ease.Linear)
            .OnComplete(onComplete ?? (() => { }));
    }

    /// <summary>
    /// Instantly moves the plane to the specified position and height
    /// </summary>
    /// <param name="position">The target position (X and Z coordinates)</param>
    /// <param name="height">The height to set the plane at</param>
    public void Teleport(Vector3 position, float height)
    {
        StartCoroutine(TeleportAfterDelay(position, height));
    }

    private IEnumerator TeleportAfterDelay(Vector3 position, float height)
    {
        // Wait for 2 seconds before teleporting
        yield return new WaitForSeconds(2f);
        transform.position = new Vector3(position.x, height, position.z);
    }
    
    /// <summary>
    /// Moves the plane from start waypoint to end waypoint at a constant speed with proper orientation
    /// </summary>
    /// <param name="start">Starting position (X, Y, Z coordinates)</param>
    /// <param name="end">Target position (X, Y, Z coordinates)</param>
    /// <param name="speed">Movement speed in units per second</param>
    /// <returns>IEnumerator for coroutine</returns>
    public IEnumerator MoveToWaypoint(Vector3 start, Vector3 end, float speed, bool backwards = false)
    {
        // Set initial position at start point
        transform.position = start;
        
        // Calculate direction to the end point
        Vector3 direction = (end - start).normalized;
        
        // Only rotate if we have a valid direction (not zero length)
        if (direction != Vector3.zero)
        {
            // Calculate the target rotation to face the end point
            Quaternion targetRotation = Quaternion.LookRotation(-direction);
            
            // Apply the rotation immediately
            transform.rotation = targetRotation;
            
            // Optional: Smooth rotation (uncomment if you want smooth rotation)
            // float rotationSpeed = 2f;
            // while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            // {
            //     transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            //     yield return null;
            // }
        }
        
        // Calculate the distance to move
        float distance = Vector3.Distance(start, end);
        float duration = distance / speed;
        float elapsedTime = 0f;
        
        // Move the plane smoothly between positions
        while (elapsedTime < duration)
        {
            // Calculate the interpolation factor (0 to 1)
            float t = elapsedTime / duration;
            
            // Move the plane from start to end
            transform.position = Vector3.Lerp(start, end, t);
            
            // If moving backwards, rotate 180 degrees to show reverse orientation
            if (backwards && direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
            
            // Update the elapsed time
            elapsedTime += Time.deltaTime;
            
            yield return null;
        }
        
        // Ensure we reach exactly the end position
        transform.position = end;
        
        // Reset rotation after movement if needed
        if (backwards && direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}