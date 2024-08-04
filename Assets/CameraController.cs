using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // The target the camera will follow
    public float smoothSpeed = 0.125f; // The smooth speed for following the target
    public Vector3 offset; // Offset from the target

    public float mouseSensitivity = 100f; // Mouse sensitivity for looking around
    private float xRotation = 0f;
    private float yRotation = 0f;

    public bool isFollowing = true; // Toggle to enable/disable camera follow

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Lock the cursor to the screen
    }

    void FixedUpdate()
    {
        if (isFollowing)
        {
            FollowTarget();
        }

        ControlCamera();
    }

    void FollowTarget()
    {
        // Calculate the desired position based on the target's position and rotation
        Vector3 desiredPosition = target.position + target.rotation * offset;
        // Smoothly interpolate to the desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Calculate the desired rotation based on the target's forward vector
        Quaternion desiredRotation = Quaternion.LookRotation(target.forward);
        // Smoothly interpolate to the desired rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, smoothSpeed);
    }

    void ControlCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply mouse rotation relative to the target's forward direction
        Quaternion rotationOffset = Quaternion.Euler(xRotation, yRotation, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation * rotationOffset, smoothSpeed);
    }

    public void EnableFollow()
    {
        isFollowing = true;
    }

    public void DisableFollow()
    {
        isFollowing = false;
    }
}
