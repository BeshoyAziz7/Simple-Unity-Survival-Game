using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;          // Reference to player
    public Vector3 offset = new Vector3(0f, 2f, -5f); // Camera offset (normal view)
    public Vector3 zoomedOffset = new Vector3(0f, 4f, -5f); // Offset when zoomed (much higher Y value)
    public float mouseSensitivity = 100f;
    public float pitchMin = -30f;     // Limit up/down
    public float pitchMax = 60f;

    [Header("Collision Settings")] // New header for collision properties
    public LayerMask collisionLayers; // Layers the camera should collide with (e.g., Environment, Default)
    public float collisionRadius = 0.3f; // Radius of the spherecast to avoid clipping edges
    public float minDistance = 1.0f; // Minimum distance camera can get to target (to prevent too close clip)
    public float collisionOffset = 0.1f; // Small offset from the hit point to prevent Z-fighting

    private WeaponController weaponController;
    private Vector3 currentOffset;
    private float offsetLerpSpeed = 10f;

    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        if (target != null)
        {
            weaponController = target.GetComponent<WeaponController>();
        }

        currentOffset = offset;
        // Initialize yaw and pitch based on current camera rotation relative to target, if needed
        // Otherwise, they'll start from 0 and camera will reset to default rotation on play
        yaw = transform.eulerAngles.y; // Start with current Y rotation
        pitch = transform.eulerAngles.x; // Start with current X rotation
    }

    void LateUpdate() // Use LateUpdate to ensure player and their rotation have already been processed
    {
        if (target == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // Determine the target offset based on zoom state
        Vector3 desiredOffset = (weaponController != null && weaponController.IsZoomed()) ? zoomedOffset : offset;
        currentOffset = Vector3.Lerp(currentOffset, desiredOffset, Time.deltaTime * offsetLerpSpeed);

        // Calculate the raw desired camera position without collision
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredCameraPosition = target.position + rotation * currentOffset;

        // --- Camera Collision Detection ---
        RaycastHit hit;
        // Direction from target to desired camera position
        Vector3 direction = desiredCameraPosition - target.position;
        float distance = direction.magnitude;

        // Perform a SphereCast to check for collisions
        // A SphereCast is better than a Raycast for cameras as it accounts for the camera's "size" (radius)
        if (Physics.SphereCast(target.position, collisionRadius, direction.normalized, out hit, distance, collisionLayers))
        {
            // Collision detected! Adjust camera position to be at the hit point, pulled back slightly
            transform.position = target.position + direction.normalized * (hit.distance - collisionOffset);
            // Ensure camera doesn't go closer than minDistance
            if (Vector3.Distance(transform.position, target.position) < minDistance)
            {
                transform.position = target.position + direction.normalized * minDistance;
            }
        }
        else
        {
            // No collision, camera can go to its desired position
            transform.position = desiredCameraPosition;
        }
        // --- End Camera Collision Detection ---

        // Handle where the camera looks
        if (weaponController != null && weaponController.IsZoomed())
        {
            Vector3 lookTarget = target.position + new Vector3(0, 1.7f, 0); // Look at the head/upper body
            transform.LookAt(lookTarget);
        }
        else
        {
            transform.LookAt(target);
        }
    }
}