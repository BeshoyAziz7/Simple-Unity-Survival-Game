using System.Collections;
using System.Collections.Generic;
using UnityEngine;


    [CreateAssetMenu(fileName = "New Ammo", menuName = "Inventory/New Ammo")]
    public class AmmoData : ScriptableObject
    {
    public string ammoName = "Pistol Ammo"; // Name of the ammo type
    public int ammoAmount = 30;             // How much ammo this pickup gives
    // You could add an enum here later if you have multiple weapon types
    // public WeaponType weaponType; // e.g., Pistol, Shotgun, Rifle
    }