using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerActivationTrigger : MonoBehaviour
{
    [Header("Spawner Reference")]
    [Tooltip("Drag the Zombie Spawner GameObject here that you want to activate.")]
    public ZombieSpawner targetSpawner;

    [Header("Trigger Settings")]
    [Tooltip("Should this trigger only activate the spawner once?")]
    public bool triggerOnce = true;
    [Tooltip("Delay in seconds before activating the spawner after player enters.")]
    public float activationDelay = 0f;

    private bool _hasBeenTriggered = false;

    void Start()
    {
        // Ensure the GameObject has a Collider and it's set to be a trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("SpawnerActivationTrigger on " + gameObject.name + " requires a Collider component.", gameObject);
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("SpawnerActivationTrigger on " + gameObject.name + ": Collider is not set to 'Is Trigger'. It might not work as expected.", gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerOnce && _hasBeenTriggered)
            {
                return; // Already triggered and it's a one-time trigger
            }

            if (targetSpawner != null)
            {
                Debug.Log(gameObject.name + " triggered by Player. Activating spawner: " + targetSpawner.name + " with delay: " + activationDelay);
                targetSpawner.ExternalActivateSpawner(activationDelay);
                _hasBeenTriggered = true;

                if (triggerOnce)
                {
                    // Optional: Disable this trigger's collider after use to prevent re-triggering
                    // GetComponent<Collider>().enabled = false;
                    // Or disable the whole GameObject if it serves no other purpose:
                    // gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning("SpawnerActivationTrigger on " + gameObject.name + " has no Target Spawner assigned.", gameObject);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (targetSpawner != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetSpawner.transform.position);
            Gizmos.DrawWireSphere(transform.position, 0.5f); // Mark the trigger location
        }
    }
}