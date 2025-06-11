using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    public AmmoData ammoData; // Drag your created AmmoData Scriptable Object here in the Inspector
    public float rotationSpeed = 50f; // Visual effect
    public float bobHeight = 0.1f;    // Visual effect
    public float bobSpeed = 1.5f;     // Visual effect

    private Vector3 startPosition;
    private float bobTimer = 0f;

    void Start()
    {
        startPosition = transform.position;
        // Ensure the pickup has a Collider and is set to Is Trigger
        // It should also have a Rigidbody (can be kinematic) if the player doesn't have one
    }

    void Update()
    {
        // Optional: Add visual rotation and bobbing
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        bobTimer += Time.deltaTime * bobSpeed;
        float newY = startPosition.y + Mathf.Sin(bobTimer) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            WeaponController weaponController = other.GetComponent<WeaponController>();
            if (weaponController != null)
            {
                // Assuming your current weapon is the one that uses this ammo type
                // You might need more sophisticated logic here if you have multiple weapons
                // and different ammo types. For now, we'll just add to the currently equipped weapon's reserve.

                // This assumes your WeaponController has a public method to add ammo
                // We'll add this method in the next step!
                bool pickedUp = weaponController.AddAmmoToCurrentWeapon(ammoData.ammoAmount);

                if (pickedUp)
                {
                    Debug.Log("Picked up " + ammoData.ammoAmount + " " + ammoData.ammoName);
                    Destroy(gameObject); // Remove the pickup from the scene
                }
                else
                {
                    Debug.Log("Could not pick up ammo (maybe weapon not equipped or full?)");
                }
            }
        }
    }

    // Optional: Visual indicator for the trigger area in the editor
    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
