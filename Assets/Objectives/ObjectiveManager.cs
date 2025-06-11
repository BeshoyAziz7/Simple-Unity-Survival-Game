using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // For TextMeshPro UI

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [Header("Objective Settings")]
    public List<Objective> objectivesList;
    public Objective currentObjective;

    [Header("UI Elements")]
    public TextMeshProUGUI objectiveDisplayText;
    public GameObject compassUIRoot;
    public RectTransform compassNeedle;

    [Header("Player and Camera")]
    public Transform playerTransform; // Assign the player's transform
    public Camera mainCamera; // Assign the main camera (or player camera)

    private int _currentObjectiveIndex = -1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else Debug.LogError("ObjectiveManager: Player Transform not assigned and not found by tag!");
        }
        if (mainCamera == null)
        {
             mainCamera = Camera.main;
             if(mainCamera == null && playerTransform != null) mainCamera = playerTransform.GetComponentInChildren<Camera>();
             if(mainCamera == null) Debug.LogError("ObjectiveManager: Main Camera not assigned and not found!");
        }


        if (compassUIRoot != null) compassUIRoot.SetActive(false);
        if (objectiveDisplayText != null) objectiveDisplayText.text = "";

        SetNextObjective();
    }

    void Update()
    {
        if (currentObjective != null && !currentObjective.isCompleted && compassUIRoot != null && compassUIRoot.activeSelf)
        {
            UpdateCompass();
        }
    }

    public void SetNextObjective()
    {
        _currentObjectiveIndex++;
        if (objectivesList != null && _currentObjectiveIndex < objectivesList.Count)
        {
            currentObjective = objectivesList[_currentObjectiveIndex];
            currentObjective.isCompleted = false;
            UpdateObjectiveDisplay();
            if (compassUIRoot != null) compassUIRoot.SetActive(true);
            LocateObjectiveInstanceInScene();
        }
        else
        {
            currentObjective = null;
            if (objectiveDisplayText != null) objectiveDisplayText.text = "All objectives completed!";
            if (compassUIRoot != null) compassUIRoot.SetActive(false);
            Debug.Log("All objectives finished.");
        }
    }

    void UpdateObjectiveDisplay()
    {
        if (objectiveDisplayText != null && currentObjective != null)
        {
            objectiveDisplayText.text = "Objective: " + currentObjective.description;
        }
        else if (objectiveDisplayText != null)
        {
            objectiveDisplayText.text = "";
        }
    }

    public void ObjectiveCompleted(Objective completedObjective)
    {
        if (currentObjective == completedObjective)
        {
            currentObjective.isCompleted = true;
            Debug.Log("Objective Completed: " + currentObjective.description);
            SetNextObjective();
        }
    }

    void LocateObjectiveInstanceInScene()
    {
        if (currentObjective == null || currentObjective.targetWeaponData == null)
        {
             if(currentObjective != null) currentObjective.objectiveInstanceInScene = null;
             if (compassUIRoot != null) compassUIRoot.SetActive(false);
             return;
        }

        WeaponPickup[] allPickups = FindObjectsOfType<WeaponPickup>();
        currentObjective.objectiveInstanceInScene = null;
        foreach (WeaponPickup pickup in allPickups)
        {
            if (pickup.weaponData == currentObjective.targetWeaponData)
            {
                currentObjective.objectiveInstanceInScene = pickup.gameObject;
                Debug.Log("Objective target instance found: " + pickup.gameObject.name);
                if (compassUIRoot != null) compassUIRoot.SetActive(true);
                return;
            }
        }
        Debug.LogWarning("ObjectiveManager: Could not find an instance of " + currentObjective.targetWeaponData.weaponName + " in the scene for the current objective.");
        if (compassUIRoot != null) compassUIRoot.SetActive(false);
    }


    void UpdateCompass()
    {
        // Ensure all necessary components and references are available
        if (playerTransform == null || compassNeedle == null || currentObjective == null || currentObjective.objectiveInstanceInScene == null || mainCamera == null)
        {
            if (compassUIRoot != null && compassUIRoot.activeSelf) compassUIRoot.SetActive(false);
            return;
        }
        // Ensure compass is visible if it needs to be
        if (!compassUIRoot.activeSelf) compassUIRoot.SetActive(true);


        Vector3 targetPosition = currentObjective.objectiveInstanceInScene.transform.position;
        Vector3 directionToTarget = targetPosition - playerTransform.position;

        // Project the camera's forward and the direction to target onto the XZ plane
        // This makes the compass ignore vertical differences and player's pitch.
        Vector3 cameraForwardXZ = mainCamera.transform.forward;
        cameraForwardXZ.y = 0; //
        cameraForwardXZ.Normalize(); //

        Vector3 targetDirXZ = directionToTarget;
        targetDirXZ.y = 0; //
        targetDirXZ.Normalize(); //

        // If the target is directly above/below or very close, avoid division by zero or NaN
        if (targetDirXZ.sqrMagnitude < 0.001f) // Effectively zero on XZ plane
        {
            compassNeedle.localEulerAngles = Vector3.zero; // Point straight up (or default orientation)
            return;
        }

        // Calculate the signed angle between the camera's forward (XZ) and the target direction (XZ)
        // This angle tells us how much to rotate the needle from "straight up" to point at the target.
        float angle = Vector3.SignedAngle(cameraForwardXZ, targetDirXZ, Vector3.up); //

        // Apply the rotation to the compass needle.
        // We use -angle because UI rotations often work clockwise for positive Z angles,
        // whereas SignedAngle gives counter-clockwise for positive values.
        compassNeedle.localEulerAngles = new Vector3(0, 0, -angle); //
    }
}