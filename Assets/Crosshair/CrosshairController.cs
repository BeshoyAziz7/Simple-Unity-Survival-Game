using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    public Image crosshairImage;    // Assign in inspector or from WeaponController
    public Image dotCrosshairImage; // Assign in inspector - this is the dot crosshair for zoom mode
    
    [Header("Positioning")]
    public Vector2 dotCrosshairOffset = new Vector2(0, 50f); // How much to move the crosshair up (y+)
    
    private float currentSpread = 0f;
    private float maxSpread = 100f;
    private float spreadRecoverySpeed = 5f;
    
    // Initialize
    void Start()
    {
        // Hide both crosshairs at start
        if (crosshairImage != null) 
            crosshairImage.gameObject.SetActive(false);
        
        if (dotCrosshairImage != null)
            dotCrosshairImage.gameObject.SetActive(false);
    }
    
    void Update()
    {
        // Gradually reduce spread over time (recoil recovery)
        if (currentSpread > 0)
        {
            currentSpread -= spreadRecoverySpeed * Time.deltaTime;
            currentSpread = Mathf.Max(0, currentSpread);
            UpdateCrosshairSize();
        }
    }
    
    // Show/hide the regular crosshair
    public void ShowCrosshair(bool show)
    {
        if (crosshairImage != null)
            crosshairImage.gameObject.SetActive(show);
    }
    
    // Show/hide the dot crosshair used for zoomed aiming
    public void ShowDotCrosshair(bool show)
    {
        if (dotCrosshairImage != null)
        {
            dotCrosshairImage.gameObject.SetActive(show);
            
            // Apply vertical offset when showing the crosshair
            if (show)
            {
                dotCrosshairImage.rectTransform.anchoredPosition = dotCrosshairOffset;
            }
        }
    }
    
    // Add recoil spread to the crosshair
    public void AddSpread(float amount)
    {
        currentSpread += amount;
        currentSpread = Mathf.Min(currentSpread, maxSpread);
        UpdateCrosshairSize();
    }
    
    // Update the crosshair size based on current spread
    void UpdateCrosshairSize()
    {
        if (crosshairImage != null)
        {
            // Adjust size based on spread
            float size = 1f + (currentSpread / 50f);
            crosshairImage.rectTransform.localScale = new Vector3(size, size, 1f);
        }
        
        // Dot crosshair doesn't change size, just position for recoil
        if (dotCrosshairImage != null && dotCrosshairImage.gameObject.activeSelf)
        {
            // Add slight random offset for recoil visual while maintaining the base offset
            float recoilOffset = currentSpread / 200f;
            dotCrosshairImage.rectTransform.anchoredPosition = new Vector2(
                dotCrosshairOffset.x + Random.Range(-recoilOffset, recoilOffset),
                dotCrosshairOffset.y + Random.Range(-recoilOffset, recoilOffset)
            );
        }
    }
    
    // Reset crosshair to default state
    public void ResetCrosshair()
    {
        currentSpread = 0f;
        UpdateCrosshairSize();
        
        if (dotCrosshairImage != null)
            dotCrosshairImage.rectTransform.anchoredPosition = dotCrosshairOffset;
    }
    
    // Get the normalized screen point (0-1) where the dot crosshair is positioned
    // This helps align raycasts with the actual visual crosshair
    public Vector2 GetNormalizedCrosshairPosition()
    {
        if (dotCrosshairImage != null && dotCrosshairImage.gameObject.activeSelf)
        {
            // Convert the anchored position to a normalized screen position
            // This is an approximation and might need adjustment based on canvas settings
            Canvas canvas = dotCrosshairImage.canvas;
            if (canvas != null)
            {
                // Get canvas rect
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    // Calculate the crosshair position in normalized screen space
                    Vector2 screenPoint = new Vector2(
                        0.5f + (dotCrosshairImage.rectTransform.anchoredPosition.x / canvasRect.rect.width),
                        0.5f + (dotCrosshairImage.rectTransform.anchoredPosition.y / canvasRect.rect.height)
                    );
                    
                    Debug.Log("Normalized crosshair position: " + screenPoint);
                    return screenPoint;
                }
            }
        }
        
        // Default to center of screen if we can't calculate
        return new Vector2(0.5f, 0.5f);
    }
}