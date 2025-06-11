using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // Required for NavMesh.SamplePosition

public class ZombieSpawner : MonoBehaviour
{
    [Header("Zombie Prefabs")]
    public GameObject[] zombiePrefabs;

    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public float spawnRadius = 10f;
    public int maxZombiesAtOnce = 5;
    public int totalZombiesToSpawnThisSession = 20; // Set to -1 for infinite if externally managed
    public float spawnInterval = 3f;
    public float initialDelay = 1f; // Used if not externally triggered immediately

    [Header("Activation Settings")]
    public bool spawnOnStart = false; // Set to false if using an external trigger as primary
    public bool activateOnPlayerProximity = false; // Can coexist or be overridden by external trigger
    public float playerProximityRadius = 25f;
    public bool deactivateWhenPlayerLeaves = false;
    public bool respawnIfPlayerReEnters = false; // For proximity activation

    // Public flag to indicate if this spawner waits for an external trigger
    [Tooltip("If true, this spawner will ONLY activate when ExternalActivateSpawner() is called.")]
    public bool waitForExternalTrigger = false;


    private List<GameObject> _activeZombies = new List<GameObject>();
    private int _zombiesSpawnedThisSessionCount = 0;
    private bool _isSpawnerActive = false;
    private Transform _playerTransform;
    private Coroutine _spawnLoopCoroutine;
    private bool _initialActivationDone = false; // Tracks if spawner has been activated at least once

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError("ZombieSpawner: Player not found! Proximity activation will not work correctly.");
            if (activateOnPlayerProximity && !waitForExternalTrigger)
            {
                 // If we need proximity but have no player, and aren't waiting for external, log error and don't proceed.
                Debug.LogError("ZombieSpawner " + gameObject.name + " requires player for proximity but player not found, and not waiting for external trigger.");
                return;
            }
        }

        if (waitForExternalTrigger)
        {
            // If waiting for external trigger, ensure other auto-activations are effectively off
            spawnOnStart = false;
            activateOnPlayerProximity = false;
            _isSpawnerActive = false; // Ensure it's not active initially
            return; // Do nothing further, wait for ExternalActivateSpawner()
        }

        if (spawnOnStart)
        {
            ExternalActivateSpawner(initialDelay); // Can use ExternalActivateSpawner for consistency
        }
    }

    void Update()
    {
        if (waitForExternalTrigger || _initialActivationDone && !respawnIfPlayerReEnters) // If externally triggered and not respawnable, or already done its one-time proximity activation
        {
            // If it was externally triggered, or its proximity trigger was a one-shot,
            // and it's not set to respawn on re-entry, its Update logic for proximity is done.
            // However, if it CAN deactivate when player leaves, that logic still needs to run.
            if (_isSpawnerActive && deactivateWhenPlayerLeaves && activateOnPlayerProximity && _playerTransform != null)
            {
                 float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
                 if (distanceToPlayer > playerProximityRadius)
                 {
                    StopSpawning();
                    // Respawn logic is handled by re-entering proximity if respawnIfPlayerReEnters is true
                 }
            }
            return;
        }


        if (activateOnPlayerProximity && _playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            if (!_isSpawnerActive && distanceToPlayer <= playerProximityRadius)
            {
                 // Only activate via proximity if it hasn't been activated yet OR if it's set to respawn
                if (!_initialActivationDone || respawnIfPlayerReEnters)
                {
                    if(respawnIfPlayerReEnters && _initialActivationDone) // Reset for respawn
                    {
                        _zombiesSpawnedThisSessionCount = 0;
                    }
                    ExternalActivateSpawner(initialDelay); // Use the public activation method
                }
            }
            else if (_isSpawnerActive && deactivateWhenPlayerLeaves && distanceToPlayer > playerProximityRadius)
            {
                StopSpawning();
                // If respawnIfPlayerReEnters is true, initialActivationDone remains true,
                // but the reset of _zombiesSpawnedThisSessionCount above handles the respawn.
            }
        }
         _activeZombies.RemoveAll(item => item == null);
    }

    // Public method to be called by the external trigger
    public void ExternalActivateSpawner(float delay = 0f)
    {
        if (_isSpawnerActive && _initialActivationDone && !respawnIfPlayerReEnters) return; // Already active and not meant to restart via this specific call if one-time

        Debug.Log("Zombie Spawner Externally Activated: " + gameObject.name);
        _isSpawnerActive = true;
        _initialActivationDone = true; // Mark that an activation attempt has been made

        if (_spawnLoopCoroutine != null)
        {
            StopCoroutine(_spawnLoopCoroutine);
        }
        _spawnLoopCoroutine = StartCoroutine(SpawnLoop(delay));
    }


    void StartSpawningInternal(float delay) // Renamed to avoid confusion
    {
        if (_isSpawnerActive && _initialActivationDone && !respawnIfPlayerReEnters) return;

        _isSpawnerActive = true;
        _initialActivationDone = true;
        if (_spawnLoopCoroutine != null)
        {
            StopCoroutine(_spawnLoopCoroutine);
        }
        _spawnLoopCoroutine = StartCoroutine(SpawnLoop(delay));
    }

    void StopSpawning()
    {
        if (!_isSpawnerActive) return;

        Debug.Log("Zombie Spawner Deactivated: " + gameObject.name);
        _isSpawnerActive = false;
        if (_spawnLoopCoroutine != null)
        {
            StopCoroutine(_spawnLoopCoroutine);
            _spawnLoopCoroutine = null;
        }
    }

    IEnumerator SpawnLoop(float startDelay)
    {
        yield return new WaitForSeconds(startDelay);

        while (_isSpawnerActive && (totalZombiesToSpawnThisSession < 0 || _zombiesSpawnedThisSessionCount < totalZombiesToSpawnThisSession))
        {
            if (_activeZombies.Count < maxZombiesAtOnce)
            {
                AttemptSpawnZombie();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
        Debug.Log("Zombie Spawner " + gameObject.name + " has finished its session.");
        _isSpawnerActive = false;
    }

    void AttemptSpawnZombie()
    {
        if (zombiePrefabs == null || zombiePrefabs.Length == 0)
        {
            Debug.LogWarning("ZombieSpawner: No zombie prefabs assigned!");
            return;
        }

        GameObject prefabToSpawn = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];
        Vector3 spawnPositionAttempt;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            spawnPositionAttempt = randomSpawnPoint.position;
        }
        else
        {
            Vector3 randomDirection = Random.insideUnitSphere * spawnRadius;
            randomDirection.y = 0;
            spawnPositionAttempt = transform.position + randomDirection;
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPositionAttempt, out hit, 5.0f, NavMesh.AllAreas))
        {
            GameObject newZombie = Instantiate(prefabToSpawn, hit.position, Quaternion.identity);
            _activeZombies.Add(newZombie);
            _zombiesSpawnedThisSessionCount++;

            ZombieHealth zh = newZombie.GetComponent<ZombieHealth>();
            if (zh != null) zh.OnZombieDied += HandleZombieDeath;

            ZombieHealth_Runner zhr = newZombie.GetComponent<ZombieHealth_Runner>();
            if (zhr != null) zhr.OnZombieDied += HandleZombieDeath;

            ZombieHealth_Tank zht = newZombie.GetComponent<ZombieHealth_Tank>();
            if (zht != null) zht.OnZombieDied += HandleZombieDeath;
        }
        else
        {
            Debug.LogWarning("ZombieSpawner: Could not find valid NavMesh position near " + spawnPositionAttempt + " for spawner " + gameObject.name);
        }
    }

    void HandleZombieDeath(GameObject deadZombieInstance)
    {
        if (_activeZombies.Contains(deadZombieInstance))
        {
            _activeZombies.Remove(deadZombieInstance);

            ZombieHealth zh = deadZombieInstance.GetComponent<ZombieHealth>();
            if (zh != null) zh.OnZombieDied -= HandleZombieDeath;

            ZombieHealth_Runner zhr = deadZombieInstance.GetComponent<ZombieHealth_Runner>();
            if (zhr != null) zhr.OnZombieDied -= HandleZombieDeath;

            ZombieHealth_Tank zht = deadZombieInstance.GetComponent<ZombieHealth_Tank>();
            if (zht != null) zht.OnZombieDied -= HandleZombieDeath;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
        else
        {
            foreach (Transform point in spawnPoints)
            {
                if (point != null) Gizmos.DrawWireSphere(point.position, 1f);
            }
        }

        if (activateOnPlayerProximity && !waitForExternalTrigger) // Only draw proximity if it's enabled and not overridden
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, playerProximityRadius);
        }
    }
}