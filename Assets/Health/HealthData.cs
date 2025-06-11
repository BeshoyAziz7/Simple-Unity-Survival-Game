using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Health Pickup", menuName = "Inventory/Health Pickup")]
public class HealthData : ScriptableObject
{
    public string healthItemName = "First Aid Kit";
    public int healAmount = 25; // How much health this pickup restores
    public AudioClip pickupSound; // Sound played when health is picked up
}