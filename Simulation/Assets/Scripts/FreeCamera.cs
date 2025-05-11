using UnityEngine;

public class FreeCamera : MonoBehaviour
{
    public float moveSpeed = 25f;     // Movement speed
    public float rotationSpeed = 100f; // Rotation speed

    private void Update()
    {
        // Handle camera movement
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Move the camera
        Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.Self);

        // Rotate the camera only when left mouse button is held down
        if (Input.GetMouseButton(0)) // 0 is the left mouse button
        {
            transform.Rotate(Vector3.up, mouseX * rotationSpeed * Time.deltaTime, Space.World);
            transform.Rotate(Vector3.left, mouseY * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}