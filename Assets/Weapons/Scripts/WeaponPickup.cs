using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Add this if you're using new Input System

public class WeaponPickup : MonoBehaviour
{
    // public string objectiveIDToComplete; // Alternative: use an ID to match
    [Header("Objective Settings")]
    public bool isObjectiveItem = false;

    public WeaponData weaponData;   // Scriptable object with weapon stats
    public float rotationSpeed = 50f;  // How fast the weapon rotates for visual effect
    public float bobHeight = 0.2f;     // How much the weapon bobs up and down
    public float bobSpeed = 2f;        // Speed of bobbing motion
    public KeyCode pickupKey = KeyCode.E; // Traditional input key

    private Vector3 startPosition;
    private float bobTimer = 0f;
    private bool playerInRange = false;
    private GameObject currentPlayer;

    // Debug variables
    public bool showDebugMessages = true;

    void Start()
    {
        startPosition = transform.position;
        if (showDebugMessages)
            Debug.Log("WeaponPickup initialized: " + weaponData.weaponName);
    }

    void Update()
    {

        // Check for pickup input
        if (playerInRange && currentPlayer != null)
        {
            // Support for old input system
            if (Input.GetKeyDown(pickupKey))
            {
                AttemptPickup();
            }

            // Support for new input system
            // Note: Only one of these methods will be used depending on your setup
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                AttemptPickup();
            }
        }
    }

private void AttemptPickup()
{
    if (showDebugMessages)
        Debug.Log("Pickup key pressed while player in range");

    WeaponController weaponController = currentPlayer.GetComponent<WeaponController>();
    if (weaponController != null)
    {
        if (showDebugMessages)
            Debug.Log("WeaponController found, equipping " + weaponData.weaponName);

        weaponController.EquipWeapon(weaponData);

        // --- OBJECTIVE COMPLETION CHECK ---
        if (isObjectiveItem && ObjectiveManager.Instance != null &&
            ObjectiveManager.Instance.currentObjective != null &&
            ObjectiveManager.Instance.currentObjective.targetWeaponData == this.weaponData && // Check if this weapon matches current objective's target
            ObjectiveManager.Instance.currentObjective.objectiveInstanceInScene == this.gameObject) // And it's THE instance
        {
            ObjectiveManager.Instance.ObjectiveCompleted(ObjectiveManager.Instance.currentObjective);
        }
        // --- END OBJECTIVE COMPLETION CHECK ---

        Destroy(gameObject);  // Remove the pickup from the scene
    }
    else
    {
        if (showDebugMessages)
            Debug.LogError("Player is missing WeaponController component!");
    }
}


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (showDebugMessages)
                Debug.Log("Player entered pickup trigger");

            playerInRange = true;
            currentPlayer = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (showDebugMessages)
                Debug.Log("Player exited pickup trigger");

            playerInRange = false;
            currentPlayer = null;
        }
    }

    // Old method that might be causing issues - combining with the above approach
    private void OnTriggerStay(Collider other)
    {
        // We handle this in Update now, but keeping this for backwards compatibility
        if (other.CompareTag("Player") && Input.GetKeyDown(pickupKey))
        {
            if (showDebugMessages)
                Debug.Log("Pickup triggered through OnTriggerStay");

            WeaponController weaponController = other.GetComponent<WeaponController>();
            if (weaponController != null)
            {
                weaponController.EquipWeapon(weaponData);
                Destroy(gameObject);
            }
        }
    }

    // Visual indicator to see the pickup trigger area
    private void OnDrawGizmos()
    {
        // Draw the trigger area
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}