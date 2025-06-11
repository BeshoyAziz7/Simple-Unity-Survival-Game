using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectiveType
{
    Collect, // For picking up items
    Reach,   // For reaching a location (not used in this example yet)
    Interact // For interacting with something (not used in this example yet)
}

[CreateAssetMenu(fileName = "New Objective", menuName = "Gameplay/Objective")]
public class Objective : ScriptableObject
{
    public string objectiveID; // Unique ID for this objective (e.g., "FindPistol")
    [TextArea(3, 5)]
    public string description;
    public ObjectiveType type = ObjectiveType.Collect;
    public GameObject objectiveTargetPrefab; // The specific weapon PREFAB that needs to be picked up
                                             // OR a reference to an object in the scene if it's unique and pre-placed.
                                             // For a weapon pickup, this could be the WeaponData ScriptableObject.

    public WeaponData targetWeaponData; // Use this if the objective is to pick up a *type* of weapon

    [HideInInspector] public bool isCompleted = false;
    [HideInInspector] public GameObject objectiveInstanceInScene; // Reference to the actual item in scene if spawned/placed
}