using UnityEngine;
using System.Collections;

public class PlaneVisual : MonoBehaviour
{
    private Vector3 targetPosition;
    private bool isMoving = false;
    private float moveSpeed;
    private System.Action onArrivalComplete;

    // Initial approach movement
    public void StartApproach(float timeToArrive, System.Action onComplete)
    {
        transform.position = new Vector3(0f, 0.3f, -45f);  // Spawn position
        targetPosition = new Vector3(0f, 0.3f, -15f);      // End of arrival strip
        moveSpeed = Vector3.Distance(transform.position, targetPosition) / timeToArrive;
        isMoving = true;
        onArrivalComplete = onComplete;
    }

    // Instant position updates
    public void TeleportToServer(int serverIndex)
    {
        isMoving = false;
        // Assuming servers are spaced 7 units apart starting from x = -14
        float xPos = -14f + (serverIndex * 7f);
        transform.position = new Vector3(xPos, 0.3f, -5f);
    }

    public void TeleportToQueue(int queueIndex, int positionInQueue)
    {
        isMoving = false;
        // Assuming queues are spaced 7 units apart starting from x = -7
        float xPos = -7f + (queueIndex * 7f);
        // Position in queue (6 units spacing: 5 unit cube + 1 unit space)
        float zPos = -15f + (positionInQueue * 6f);
        transform.position = new Vector3(xPos, 0.3f, zPos);
    }

    public void StartDeparture()
    {
        StartCoroutine(DepartureSequence());
    }

    private IEnumerator DepartureSequence()
    {
        Vector3 departurePos = transform.position + Vector3.forward * 10f;
        float departureTime = 1f; // 1 second to depart
        float elapsedTime = 0f;
        Vector3 startPos = transform.position;

        while (elapsedTime < departureTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / departureTime;
            transform.position = Vector3.Lerp(startPos, departurePos, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Update()
    {
        if (isMoving)
        {
            // Move towards target
            transform.position = Vector3.MoveTowards(
                transform.position, 
                targetPosition, 
                moveSpeed * Time.deltaTime
            );

            // Check if arrived
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isMoving = false;
                onArrivalComplete?.Invoke();
            }
        }
    }
} 