using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    public Slider healthBar; // Reference to the UI slider


    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log("Player health initialized: " + currentHealth);
        UpdateHealthBar(); // Initialize UI

    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("Player took " + damage + " damage! Health: " + currentHealth);
        UpdateHealthBar(); // Refresh UI after damage

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // NEW METHOD TO HEAL PLAYER
    public bool Heal(int amount)
    {
        if (currentHealth < maxHealth)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            Debug.Log("Player healed " + amount + " health. Current Health: " + currentHealth);
            UpdateHealthBar();
            return true; // Successfully healed
        }
        return false; // Already at max health
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = (float)currentHealth / maxHealth; // Convert to 0-1 range
        }
    }


    void Die()
    {
        Debug.Log("Player died!");
        // Add death effects (e.g., respawn, game over screen) later.
        // For now, you might want to disable player input or show a game over UI.
    }
}