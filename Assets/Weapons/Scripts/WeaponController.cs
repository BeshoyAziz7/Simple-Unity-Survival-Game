using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Make sure this is included for TextMeshPro support
using UnityEngine.UI; // For Image components

public class WeaponController : MonoBehaviour
{

    public Transform weaponHoldPoint;  // Empty GameObject child of player where weapon is held
    public Camera playerCamera;        // Reference to the main camera

    private WeaponData currentWeapon;
    private GameObject currentWeaponObject;
    private float nextFireTime = 0f;
    private AudioSource audioSource;
    private bool isReloading = false;
    private ThirdPersonMovement movementController;
    private CrosshairController crosshairController; // Reference to crosshair controller

    // UI elements to reference
    public TextMeshProUGUI ammoText;   // Drag your TextMeshPro component here in the inspector
    public Image crosshairImage;       // Reference to the regular crosshair image
    public Image dotCrosshairImage;    // Reference to the dot crosshair image for zoomed aiming

    [Header("Weapon Display Settings")]
    public float weaponRecoil = 1.5f;  // Amount of recoil when firing

    [Header("Zoom Settings")]
    public float zoomedFOV = 40f;      // Camera FOV when zoomed in
    public float normalFOV = 60f;      // Normal camera FOV
    public float zoomSpeed = 10f;      // How fast the camera zooms in/out
    private bool isZoomed = false;     // Is the player currently in zoom mode
    private float currentFOV;          // Current FOV for smooth transitions

    // Original weapon position for smooth transitions
    private Vector3 originalWeaponPosition;
    private Vector3 zoomedWeaponPosition = new Vector3(0.1f, -0.05f, 0.2f); // Adjust this to position weapon when zoomed

    [Header("Drop Weapon Settings")] // New header for drop settings
    public KeyCode dropWeaponKey = KeyCode.P; // Assign the drop key in the Inspector
    public GameObject weaponPickupPrefab; // Drag your WeaponPickup prefab here (the one with WeaponPickup.cs)
    public Vector3 droppedWeaponRotation = new Vector3(90f, 0f, 0f); // New: Rotation for the dropped weapon

    [Header("Effects")] // New Header
    public GameObject bloodSplatterEffectPrefab; // Drag your BloodSplatterEffect prefab here

    [Header("When splitting Blood")] // New Header
    public LayerMask groundLayerMask; // Assign only ground layers in Inspector


    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Get reference to movement controller
        movementController = GetComponent<ThirdPersonMovement>();

        // Get reference to crosshair controller
        crosshairController = GetComponent<CrosshairController>();
        if (crosshairController == null)
        {
            crosshairController = gameObject.AddComponent<CrosshairController>();
            crosshairController.crosshairImage = crosshairImage;
            crosshairController.dotCrosshairImage = dotCrosshairImage;
        }
        else
        {
            // Make sure the crosshair controller has references
            if (crosshairController.crosshairImage == null)
                crosshairController.crosshairImage = crosshairImage;
            if (crosshairController.dotCrosshairImage == null)
                crosshairController.dotCrosshairImage = dotCrosshairImage;
        }

        // Default to main camera if not assigned
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Store original FOV
        if (playerCamera != null)
        {
            normalFOV = playerCamera.fieldOfView;
            currentFOV = normalFOV;
        }

        // Hide UI elements until weapon is equipped
        if (ammoText != null) ammoText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Handle dropping the weapon
        if (currentWeapon != null && Input.GetKeyDown(dropWeaponKey))
        {
            DropWeapon();
            return; // Exit Update to avoid other actions immediately after dropping
        }

        if (currentWeapon == null) return; // If no weapon is equipped, stop here

        // Handle zooming with right click
        HandleZoom();

        // Handle shooting - now only when zoomed
        if (Input.GetMouseButton(0) && isZoomed && Time.time >= nextFireTime && currentWeapon.currentAmmo > 0 && !isReloading)
        {
            Shoot();
        }

        // Handle reloading
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentWeapon.currentAmmo < currentWeapon.maxAmmo)
        {
            // Ensure there's reserve ammo to reload
            if (currentWeapon.reserveAmmo > 0)
            {
                StartCoroutine(Reload());
            }
            else
            {
                Debug.Log("No reserve ammo to reload!");
            }
        }

        // Update UI
        UpdateAmmoDisplay();

        // Handle FOV transitions
        UpdateFOV();
    }

    // Handle zoom functionality
    void HandleZoom()
    {
        if (Input.GetMouseButtonDown(1)) // Right mouse button pressed
        {
            isZoomed = true;
            crosshairController.ShowCrosshair(false);
            crosshairController.ShowDotCrosshair(true);

            // If we have a movement controller, reduce speed while zoomed
            if (movementController != null)
            {
                movementController.SetZoomSpeed();
            }
        }
        else if (Input.GetMouseButtonUp(1)) // Right mouse button released
        {
            isZoomed = false;
            crosshairController.ShowCrosshair(false); // Keep both off when not zoomed
            crosshairController.ShowDotCrosshair(false);

            // Reset movement speed
            if (movementController != null)
            {
                movementController.ResetSpeed();
            }
        }
    }

    // Smoothly transition FOV
    void UpdateFOV()
    {
        if (playerCamera != null)
        {
            float targetFOV = isZoomed ? zoomedFOV : normalFOV;
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * zoomSpeed);
            playerCamera.fieldOfView = currentFOV;

        }
    }

    //For adding Ammo
    public bool AddAmmoToCurrentWeapon(int amount)
    {
        if (currentWeapon != null)
        {
            // You might want to cap reserveAmmo at a certain maximum here
            // For now, it just adds.
            currentWeapon.reserveAmmo += amount;
            UpdateAmmoDisplay(); // Refresh the UI
            Debug.Log("Added " + amount + " to reserve. Total reserve: " + currentWeapon.reserveAmmo);
            return true; // Ammo was added
        }
        return false; // No weapon equipped
    }


    // Update the ammo display
    void UpdateAmmoDisplay()
    {
        if (ammoText != null && currentWeapon != null)
        {
            // Display current ammo in magazine and total reserve ammo
            ammoText.text = currentWeapon.currentAmmo + " / " + currentWeapon.reserveAmmo;
        }
    }

    public void EquipWeapon(WeaponData weaponData)
    {
        // Remove current weapon if one exists
        if (currentWeaponObject != null)
        {
            Destroy(currentWeaponObject);
        }

        // Set new weapon
        currentWeapon = weaponData;

        // Create weapon model
        if (weaponData.weaponPrefab != null)
        {
            currentWeaponObject = Instantiate(weaponData.weaponPrefab, weaponHoldPoint);
            currentWeaponObject.transform.localPosition = Vector3.zero;
            currentWeaponObject.transform.localRotation = Quaternion.identity;

            // Store original position for zoom transitions
            originalWeaponPosition = currentWeaponObject.transform.localPosition;
        }

        // Update the movement controller to switch animations
        if (movementController != null)
        {
            movementController.SetWeaponEquipped(true);
        }

        // Show UI elements
        if (ammoText != null)
        {
            ammoText.gameObject.SetActive(true);
            UpdateAmmoDisplay(); // Update immediately
        }

        // Reset crosshair state - don't show any crosshair until zoomed
        if (crosshairController != null)
        {
            crosshairController.ShowCrosshair(false);
            crosshairController.ShowDotCrosshair(false);
        }

        Debug.Log("Equipped " + weaponData.weaponName);
    }

    public void UnequipWeapon()
    {
        if (currentWeaponObject != null)
        {
            Destroy(currentWeaponObject);
            currentWeapon = null;

            // Update movement controller
            if (movementController != null)
            {
                movementController.SetWeaponEquipped(false);
            }

            // Hide UI elements
            if (ammoText != null) ammoText.gameObject.SetActive(false);

            // Hide crosshairs
            if (crosshairController != null)
            {
                crosshairController.ShowCrosshair(false);
                crosshairController.ShowDotCrosshair(false);
            }

            // Reset zoom state
            isZoomed = false;
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = normalFOV;
            }

            Debug.Log("Weapon unequipped");
        }
    }

    void Shoot()
    {
        nextFireTime = Time.time + currentWeapon.fireRate;
        currentWeapon.currentAmmo--;

        // Update UI immediately after changing ammo
        UpdateAmmoDisplay();

        // Apply recoil to crosshair
        if (crosshairController != null)
        {
            crosshairController.AddSpread(weaponRecoil);
        }

        // Play sound
        if (currentWeapon.shootSound != null)
        {
            audioSource.PlayOneShot(currentWeapon.shootSound);
        }

        // Show muzzle flash
        if (currentWeapon.muzzleFlashPrefab != null)
        {
            // Find muzzle point - assumes there's a transform named "MuzzlePoint" on the weapon
            Transform muzzlePoint = currentWeaponObject.transform.Find("MuzzlePoint");
            if (muzzlePoint != null)
            {
                GameObject muzzleFlash = Instantiate(currentWeapon.muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
                Destroy(muzzleFlash, 0.05f); // Quick flash
            }
        }

        // Get the actual position of the crosshair on screen
        Vector2 crosshairScreenPoint = new Vector2(0.5f, 0.5f); // Default center
        if (crosshairController != null)
        {
            crosshairScreenPoint = crosshairController.GetNormalizedCrosshairPosition();
        }

        // Raycast for hit detection from adjusted position (aligned with dot crosshair)
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(crosshairScreenPoint.x, crosshairScreenPoint.y, 0));
        RaycastHit hit;

        // Draw a debug ray to visualize where we're shooting
        Debug.DrawRay(ray.origin, ray.direction * currentWeapon.range, Color.red, 1f);

        if (Physics.Raycast(ray, out hit, currentWeapon.range))
        {
            Debug.Log("Hit: " + hit.transform.name);

            // Handle hitting zombies
            ZombieHealth zombieHealth = hit.transform.GetComponent<ZombieHealth>();
if (zombieHealth != null)
{
    zombieHealth.TakeDamage(currentWeapon.damageAmount);
    
    // Instantiate blood splatter effect on the ground
    if (bloodSplatterEffectPrefab != null)
    {
        // Cast a ray downward from the hit point to find the ground
        RaycastHit groundHit;
        
        // Cast ray downward from the zombie hit point to find the ground beneath
        if (Physics.Raycast(hit.point, Vector3.down, out groundHit, 20f, groundLayerMask))
        {
            // Place blood splatter on the ground
            Vector3 bloodPosition = groundHit.point;
            
            // Add small offset to prevent z-fighting with ground
            bloodPosition += Vector3.up * 0.01f;
            
            // Rotate to align with ground surface
            Quaternion bloodRotation = Quaternion.LookRotation(Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            
            GameObject bloodSplatter = Instantiate(bloodSplatterEffectPrefab, bloodPosition, bloodRotation);
            Destroy(bloodSplatter, 5f); // Keep blood visible longer
        }
        else
        {
            Debug.Log("No ground found below zombie for blood splatter");
        }
    }
}
            ZombieHealth_Tank tankHealth = hit.transform.GetComponent<ZombieHealth_Tank>();
            if (tankHealth != null)
            {
                tankHealth.TakeDamage(currentWeapon.damageAmount);
                if (bloodSplatterEffectPrefab != null)
                {
        // Cast a ray downward from the hit point to find the ground
        RaycastHit groundHit;

        // Cast ray downward from the zombie hit point to find the ground beneath
        if (Physics.Raycast(hit.point, Vector3.down, out groundHit, 20f, groundLayerMask))
        {
            // Place blood splatter on the ground
            Vector3 bloodPosition = groundHit.point;
            
            // Add small offset to prevent z-fighting with ground
            bloodPosition += Vector3.up * 0.01f;
            
            // Rotate to align with ground surface
            Quaternion bloodRotation = Quaternion.LookRotation(Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            
            GameObject bloodSplatter = Instantiate(bloodSplatterEffectPrefab, bloodPosition, bloodRotation);
            Destroy(bloodSplatter, 5f); // Keep blood visible longer
        }
        else
        {
            Debug.Log("No ground found below zombie for blood splatter");
        }
                }
            }

            ZombieHealth_Runner runnerHealth = hit.transform.GetComponent<ZombieHealth_Runner>();
            if (runnerHealth != null)
            {
                runnerHealth.TakeDamage(currentWeapon.damageAmount);
                if (bloodSplatterEffectPrefab != null)
                {
        // Cast a ray downward from the hit point to find the ground
        RaycastHit groundHit;
        
        // Cast ray downward from the zombie hit point to find the ground beneath
        if (Physics.Raycast(hit.point, Vector3.down, out groundHit, 20f, groundLayerMask))
        {
            // Place blood splatter on the ground
            Vector3 bloodPosition = groundHit.point;
            
            // Add small offset to prevent z-fighting with ground
            bloodPosition += Vector3.up * 0.01f;
            
            // Rotate to align with ground surface
            Quaternion bloodRotation = Quaternion.LookRotation(Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            
            GameObject bloodSplatter = Instantiate(bloodSplatterEffectPrefab, bloodPosition, bloodRotation);
            Destroy(bloodSplatter, 5f); // Keep blood visible longer
        }
        else
        {
            Debug.Log("No ground found below zombie for blood splatter");
        }
                }
            }


            // Show impact effect
            if (currentWeapon.impactEffectPrefab != null)
            {
                GameObject impactEffect = Instantiate(currentWeapon.impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impactEffect, 2f); // Cleanup after 2 seconds
            }
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Reloading...");

        // Play reload sound
        if (currentWeapon.reloadSound != null)
        {
            audioSource.PlayOneShot(currentWeapon.reloadSound);
        }

        // Wait for reload time (could be weapon specific)
        yield return new WaitForSeconds(2f);

        // Calculate ammo needed to fill the magazine
        int ammoNeeded = currentWeapon.maxAmmo - currentWeapon.currentAmmo;

        // Determine how much ammo to take from reserve
        int ammoToTakeFromReserve = Mathf.Min(ammoNeeded, currentWeapon.reserveAmmo);

        // Refill current ammo from reserve
        currentWeapon.currentAmmo += ammoToTakeFromReserve;
        currentWeapon.reserveAmmo -= ammoToTakeFromReserve;

        // Update UI immediately after reload completes
        UpdateAmmoDisplay();

        isReloading = false;
        Debug.Log("Reload complete");
    }

    // New method to handle dropping the weapon
        void DropWeapon()
    {
        if (currentWeapon == null || weaponPickupPrefab == null)
        {
            Debug.LogWarning("Cannot drop weapon: No weapon equipped or Weapon Pickup Prefab not assigned.");
            return;
        }

        // 1. Create a new WeaponPickup instance in the world
        // Position it slightly in front of the player or at their feet
        Vector3 dropPosition = transform.position + transform.forward * 0.5f + Vector3.up * 0.1f; // Adjust as needed

        // Instantiate with the desired rotation
        GameObject droppedWeapon = Instantiate(weaponPickupPrefab, dropPosition, Quaternion.Euler(droppedWeaponRotation)); // MODIFIED LINE

        // 2. Transfer the WeaponData to the new WeaponPickup
        WeaponPickup pickupScript = droppedWeapon.GetComponent<WeaponPickup>();
        if (pickupScript != null)
        {
            // Create a new WeaponData instance for the pickup to ensure it's a unique copy
            // and doesn't share reference with the original ScriptableObject asset.
            WeaponData newWeaponData = ScriptableObject.CreateInstance<WeaponData>();
            newWeaponData.weaponName = currentWeapon.weaponName;
            newWeaponData.weaponPrefab = currentWeapon.weaponPrefab;
            newWeaponData.damageAmount = currentWeapon.damageAmount;
            newWeaponData.fireRate = currentWeapon.fireRate;
            newWeaponData.range = currentWeapon.range;
            newWeaponData.maxAmmo = currentWeapon.maxAmmo;
            newWeaponData.currentAmmo = currentWeapon.currentAmmo; // Keep current ammo in mag
            newWeaponData.reserveAmmo = currentWeapon.reserveAmmo; // Keep reserve ammo

            newWeaponData.shootSound = currentWeapon.shootSound;
            newWeaponData.reloadSound = currentWeapon.reloadSound;
            newWeaponData.muzzleFlashPrefab = currentWeapon.muzzleFlashPrefab;
            newWeaponData.impactEffectPrefab = currentWeapon.impactEffectPrefab;

            pickupScript.weaponData = newWeaponData; // Assign the copied data to the pickup
        }
        else
        {
            Debug.LogError("WeaponPickup prefab does not have a WeaponPickup script!");
        }

        // 3. Unequip the weapon from the player
        UnequipWeapon();

        Debug.Log("Dropped " + currentWeapon.weaponName);
    }


    // Check if player has weapon equipped
    public bool HasWeaponEquipped()
    {
        return currentWeapon != null;
    }

    // Check if player is currently in zoom mode
    public bool IsZoomed()
    {
        return isZoomed;
    }
}