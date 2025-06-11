using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    public HealthData healthData; // Drag your HealthData Scriptable Object here
    public float rotationSpeed = 50f;
    public float bobHeight = 0.1f;
    public float bobSpeed = 1.5f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
        // Ensure this GameObject has a Collider set to Is Trigger
        // And a Rigidbody (can be kinematic) for trigger detection to work reliably
    }

    void Update()
    {
        // Optional visual effects
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        transform.position = startPosition + new Vector3(0f, Mathf.Sin(Time.time * bobSpeed) * bobHeight, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                if (playerHealth.Heal(healthData.healAmount)) // Call the new Heal method
                {
                    // Play pickup sound
                    if (healthData.pickupSound != null)
                    {
                        AudioSource.PlayClipAtPoint(healthData.pickupSound, transform.position);
                    }
                    Debug.Log("Player picked up " + healthData.healthItemName + ". Healed " + healthData.healAmount + " HP.");
                    Destroy(gameObject); // Remove the pickup
                }
                else
                {
                    Debug.Log("Player health is already full, cannot pick up " + healthData.healthItemName);
                }
            }
        }
    }

    // Optional: Draw a gizmo to visualize the pickup area in the editor
    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(col.bounds.center, col.bounds.extents.x); // Or use size.magnitude for more accurate visual
        }
    }
}