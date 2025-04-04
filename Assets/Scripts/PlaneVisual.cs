using UnityEngine;
using System;

/// <summary>
/// Controls the visual representation and movement of planes in the airport simulation.
/// </summary>
public class PlaneVisual : MonoBehaviour
{
    #region Configuration Constants
    
    /// <summary>Movement speed of the plane in units per second</summary>
    private const float SPEED = 15f;
    
    /// <summary>Fixed height at which all planes operate</summary>
    private const float PLANE_HEIGHT = 0.3f;
    
    /// <summary>Distance between queued planes</summary>
    private const float QUEUE_SPACING = 5f;
    
    /// <summary>Initial spawn distance from runway start</summary>
    private const float RUNWAY_LENGTH = 45f;

    /// <summary>Distance threshold to consider movement complete</summary>
    private const float ARRIVAL_THRESHOLD = 0.01f;

    #endregion

    #region State Variables
    
    /// <summary>Target position for current movement</summary>
    private Vector3 targetPosition;
    
    /// <summary>Callback to invoke when movement completes</summary>
    private Action onMovementComplete;
    private bool isMoving = false;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Initializes the plane's physics and collision components.
    /// Called when the plane GameObject is created.
    /// </summary>
    private void Awake()
    {
        InitializePhysics();
        InitializeCollider();
    }

    /// <summary>
    /// Updates the plane's position based on its movement.
    /// </summary>
    private void Update()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, SPEED * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) <= ARRIVAL_THRESHOLD)
            {
                isMoving = false;
                onMovementComplete?.Invoke();
            }
        }
    }

    /// <summary>
    /// Draws debug visualization for the plane's collision bounds.
    /// Only visible in the Unity Editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 boxSize = new Vector3(4f, 0.3f, 4f);
        Gizmos.DrawWireCube(transform.position, boxSize);
    }

    #endregion

    #region Public Movement Interface

    /// <summary>
    /// Initiates the plane's approach sequence from the start of the runway.
    /// </summary>
    /// <param name="time">Current simulation time</param>
    /// <param name="onComplete">Callback to invoke when approach is complete</param>
    public void StartApproach(float time, Action onComplete)
    {
        Vector3 startPos = new Vector3(0, PLANE_HEIGHT, -RUNWAY_LENGTH);
        Vector3 endPos = new Vector3(0, PLANE_HEIGHT, 0);
        
        transform.position = startPos;
        MoveToPosition(endPos, onComplete);
    }

    /// <summary>
    /// Instantly moves the plane to a server position.
    /// </summary>
    /// <param name="serverIndex">Index of the target server</param>
    public void TeleportToServer(int serverIndex)
    {
        Vector3 serverPos = AirportSimulation.Instance.GetServerPosition(serverIndex);
        transform.position = new Vector3(serverPos.x, PLANE_HEIGHT, serverPos.z);
    }

    /// <summary>
    /// Instantly moves the plane to a position in the queue.
    /// </summary>
    /// <param name="queueIndex">Index of the queue line</param>
    /// <param name="positionInQueue">Position within the queue (0 is front)</param>
    public void TeleportToQueue(int queueIndex, int positionInQueue)
    {
        Vector3 queueStart = AirportSimulation.Instance.GetQueuePosition(queueIndex);
        transform.position = new Vector3(
            queueStart.x,
            PLANE_HEIGHT,
            queueStart.z + (positionInQueue * -QUEUE_SPACING)
        );
    }

    /// <summary>
    /// Initiates the plane's departure sequence.
    /// </summary>
    public void StartDeparture()
    {
        Vector3 departurePos = new Vector3(
            transform.position.x,
            PLANE_HEIGHT,
            transform.position.z + RUNWAY_LENGTH
        );

        MoveToPosition(departurePos, () => Destroy(gameObject));
    }

    #endregion

    #region Private Movement Implementation

    /// <summary>
    /// Initializes the plane's physics properties.
    /// </summary>
    private void InitializePhysics()
    {
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    /// <summary>
    /// Initializes the plane's collision detection.
    /// </summary>
    private void InitializeCollider()
    {
        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
    }

    /// <summary>
    /// Initiates movement to a target position.
    /// </summary>
    /// <param name="endPos">Target position</param>
    /// <param name="onComplete">Optional callback on completion</param>
    private void MoveToPosition(Vector3 endPos, Action onComplete = null)
    {
        targetPosition = new Vector3(endPos.x, PLANE_HEIGHT, endPos.z);
        onMovementComplete = onComplete;
        isMoving = true;
    }

    #endregion
} 