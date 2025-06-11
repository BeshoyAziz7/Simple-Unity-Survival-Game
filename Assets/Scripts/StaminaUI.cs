using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    public Slider staminaBar; // Reference to the UI slider for stamina

void Start()
{
    if (staminaBar != null)
    {
        staminaBar.value = 1f; // Initialize to full stamina
    }
}

public void UpdateStamina(float staminaPercentage)
{
    if (staminaBar != null)
    {
        staminaBar.value = staminaPercentage; // Update slider value (0-1 range)
    }
}

}