using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Assignable GameObject for the camera to follow
    public Vector3 offset; // Offset of the camera from the target
    public float smoothSpeed = 0.125f; // Smoothness factor for the camera's movement
    public float rotationSpeed = 5f; // Sensitivity of the mouse movement for rotation
    public float verticalRotationSpeed = 2f; // Sensitivity of the mouse for vertical movement
    public float minVerticalAngle = -40f; // Clamping values for vertical camera rotation (min angle)
    public float maxVerticalAngle = 80f; // Clamping values for vertical camera rotation (max angle)
    public float minCameraHeight = 1.0f; // Minimum height for the camera to prevent clipping below ground

    private float currentRotationAngle = 0f; // Store the current rotation angle around the target
    private float currentVerticalAngle = 0f; // Store the current vertical rotation angle

    void Start()
    {
        // Add a SphereCollider to the camera
        SphereCollider cameraCollider = gameObject.AddComponent<SphereCollider>();

        // Add a Rigidbody to the camera and set it to kinematic
        Rigidbody cameraRigidbody = gameObject.AddComponent<Rigidbody>();
        cameraRigidbody.isKinematic = true;
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Mouse input for horizontal movement (left-right rotation)
            float horizontalInput = Input.GetAxis("Mouse X");

            // Mouse input for vertical movement (up-down rotation)
            float verticalInput = Input.GetAxis("Mouse Y");

            // Update the current rotation angles based on mouse movement
            currentRotationAngle += horizontalInput * rotationSpeed; // Horizontal rotation (around Y-axis)
            currentVerticalAngle -= verticalInput * verticalRotationSpeed; // Vertical rotation (around X-axis)

            // Clamp the vertical rotation to avoid over-rotation
            currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);

            // Calculate the rotation around the target
            Quaternion rotation = Quaternion.Euler(currentVerticalAngle, currentRotationAngle, 0f);

            // Apply the rotation to the offset position
            Vector3 rotatedOffset = rotation * offset;

            // Calculate the desired position
            Vector3 desiredPosition = target.position + rotatedOffset;

            // Clamp the y position to ensure the camera doesn't go below the ground
            desiredPosition.y = Mathf.Max(desiredPosition.y, minCameraHeight);

            // Smoothly interpolate to the desired position
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

            // Update camera position
            transform.position = smoothedPosition;

            // Optional: Make the camera look at the target
            transform.LookAt(target);
        }
    }
}
