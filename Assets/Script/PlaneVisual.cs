using UnityEngine;
using System;
using DG.Tweening;

public class PlaneVisual : MonoBehaviour
{
    private const float ARRIVAL_THRESHOLD = 0.01f;

    private void Awake()
    {
        InitializePhysics();
        InitializeCollider();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 boxSize = new Vector3(4f, 0.3f, 4f);
        Gizmos.DrawWireCube(transform.position, boxSize);
    }

    private Action onMovementComplete;

    public void LandingAnimation(Vector3[] waypoints, float flyHeight, float landingHeight, float flySpeed, float landingSpeed, float taxiSpeed, Action onComplete)
    {
        // Step 1: Spawn - Place plane at waypoint 0 with flySpeed
        transform.position = new Vector3(waypoints[0].x, flyHeight, waypoints[0].z);
        
        // Step 2: Approach - Move from waypoint 0 to waypoint 1 at constant flySpeed
        MoveToPositionDOTween(
            new Vector3(waypoints[1].x, flyHeight, waypoints[1].z),
            flySpeed,
            () => {
                // Step 3: Descend - Move from waypoint 1 to waypoint 2, transition from flySpeed to landingSpeed
                MoveToPositionWithSpeedTransitionDOTween(
                    new Vector3(waypoints[2].x, landingHeight, waypoints[2].z),
                    flySpeed,
                    landingSpeed,
                    () => {
                        // Step 4: Landing - Move from waypoint 2 to waypoint 3, transition from landingSpeed to taxiSpeed
                        MoveToPositionWithSpeedTransitionDOTween(
                            new Vector3(waypoints[3].x, landingHeight, waypoints[3].z),
                            landingSpeed,
                            taxiSpeed,
                            onComplete
                        );
                    }
                );
            }
        );
    }

    public void Approach1to5Servers(Vector3[] waypoints, float landingHeight, float taxiSpeed, Action onComplete)
    {
        // Step 5: Curved taxi from waypoint 3 to 5 with waypoint 4 as control point
        RotateAroundPointArc(
            new Vector3(waypoints[3].x, landingHeight, waypoints[3].z),
            new Vector3(waypoints[4].x, landingHeight, waypoints[4].z),
            new Vector3(waypoints[5].x, landingHeight, waypoints[5].z),
            taxiSpeed,
            false,
            () => {
                // Step 6: Curved taxi from waypoint 5 to 7 with waypoint 6 as control point
                RotateAroundPointArc(
                    new Vector3(waypoints[5].x, landingHeight, waypoints[5].z),
                    new Vector3(waypoints[6].x, landingHeight, waypoints[6].z),
                    new Vector3(waypoints[7].x, landingHeight, waypoints[7].z),
                    taxiSpeed,
                    false,
                    () => {
                        // Step 7: Curved taxi from waypoint 7 to 9 with waypoint 8 as control point
                        RotateAroundPointArc(
                            new Vector3(waypoints[7].x, landingHeight, waypoints[7].z),
                            new Vector3(waypoints[8].x, landingHeight, waypoints[8].z),
                            new Vector3(waypoints[9].x, landingHeight, waypoints[9].z),
                            taxiSpeed,
                            true,
                            onComplete
                        );
                    }
                );
            }
        );
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

    private void MoveToPositionDOTween(Vector3 endPos, float speed, Action onComplete = null)
    {
        float distance = Vector3.Distance(transform.position, endPos);
        float duration = distance / speed;
        transform.DOMove(endPos, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void MoveToPositionWithSpeedTransitionDOTween(Vector3 endPos, float startSpeed, float endSpeed, Action onComplete = null)
    {
        float distance = Vector3.Distance(transform.position, endPos);
        // For a linear speed transition, approximate average speed
        float avgSpeed = (startSpeed + endSpeed) / 2f;
        float duration = distance / avgSpeed;
        // Optionally, use a custom ease for speed transition
        transform.DOMove(endPos, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void RotateAroundPointArc(Vector3 start, Vector3 center, Vector3 end, float speed, bool clockwise, Action onComplete = null)
    {
        // Calculate radius and angles
        Vector3 startOffset = start - center;
        Vector3 endOffset = end - center;
        float radius = startOffset.magnitude;
        float startAngle = Mathf.Atan2(startOffset.z, startOffset.x);
        float endAngle = Mathf.Atan2(endOffset.z, endOffset.x);

        // Ensure the arc goes in the correct direction
        float angleDiff = endAngle - startAngle;
        if (clockwise && angleDiff > 0)
            endAngle -= 2 * Mathf.PI;
        else if (!clockwise && angleDiff < 0)
            endAngle += 2 * Mathf.PI;

        float arcLength = Mathf.Abs(endAngle - startAngle) * radius;
        float duration = arcLength / speed;

        transform.position = start;

        DOTween.To(
            () => startAngle,
            a => {
                Vector3 pos = center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
                transform.position = pos;
                // Calculate the next position a tiny step ahead along the arc
                float step = 0.01f * (clockwise ? 1f : -1f);
                float nextA = a + step;
                Vector3 nextPos = center + new Vector3(Mathf.Cos(nextA), 0, Mathf.Sin(nextA)) * radius;
                Vector3 forward = (nextPos - pos).normalized;
                transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            },
            endAngle,
            duration
        ).SetEase(Ease.Linear)
        .OnComplete(() => onComplete?.Invoke());
    }
}