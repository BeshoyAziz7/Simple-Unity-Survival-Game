using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Create this as a Scriptable Object for easy weapon creation
[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/New Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public GameObject weaponPrefab;   // Visual model when equipped
    public int damageAmount = 25;
    public float fireRate = 0.5f;     // Time between shots
    public float range = 50f;
    public int maxAmmo = 30;          // Max ammo in a single magazine
    public int currentAmmo = 30;      // Current ammo in the magazine
    public int reserveAmmo = 30;      // Ammo carried outside the magazine (your "other 30")
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public GameObject muzzleFlashPrefab;
    public GameObject impactEffectPrefab;
}