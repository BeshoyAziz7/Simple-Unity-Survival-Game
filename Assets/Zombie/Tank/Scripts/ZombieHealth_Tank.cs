using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieHealth_Tank : MonoBehaviour
{
    //For spawning
    public event System.Action<GameObject> OnZombieDied; // Event to notify when this zombie dies

    public int maxHealth = 200;  // Higher health for tank
    private int currentHealth;
    public GameObject deathEffect;
    public AudioClip hitSound;
    public AudioClip deathSound;
    public float staggerResistance = 0.5f; // Takes less stagger time

    private AudioSource audioSource;
    private Animator animator;
    private ZombieAI_Tank zombieAI; // Make sure this is assigned if used
    private UnityEngine.AI.NavMeshAgent agent;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        animator = GetComponent<Animator>();
        zombieAI = GetComponent<ZombieAI_Tank>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        if (zombieAI != null)
        {
            // Tank might have a different reaction or resist stagger
            // For now, using ForceIdleState as per your original script.
            // You might want a specific "TankHitReaction" in its AI.
            // zombieAI.ForceIdleState();
        }


        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(ResumeAfterHit());
        }
    }

    IEnumerator ResumeAfterHit()
    {
        // Tanks might have a different stagger duration or resist it
        yield return new WaitForSeconds(0.2f * staggerResistance); // Using your staggerResistance

        if (isDead) yield break;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
        }

        if (zombieAI != null && zombieAI.enabled)
        {
            zombieAI.ReevaluateStateAfterHit();
        }
    }

    void Die()
    {
        if (isDead) return; // Prevent Die() from being called multiple times
        isDead = true;

        // Notify systems that this zombie is now considered dead
        if (OnZombieDied != null)
        {
            OnZombieDied.Invoke(gameObject);
        }

        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // Disable AI and other components immediately
        if (zombieAI != null)
        {
            zombieAI.enabled = false;
        }

        Collider zombieCollider = GetComponent<Collider>();
        if (zombieCollider != null)
        {
            zombieCollider.enabled = false;
        }

        if (agent != null)
        {
            agent.enabled = false; // Stop NavMeshAgent
        }

        // Handle death effects and animation
        if (animator != null)
        {
            animator.SetTrigger("Death"); // Make sure "Death" is a valid trigger

            if (deathEffect != null)
            {
                Instantiate(deathEffect, transform.position, Quaternion.identity);
            }

            // Destroy the GameObject after the animation's approximate duration
            // Tanks might have a longer death animation
            StartCoroutine(DestroyAfterDelay(4.0f)); // Adjust 4.0f to match tank death animation
        }
        else
        {
            // If no animator, still create death effect but destroy faster
            if (deathEffect != null)
            {
                Instantiate(deathEffect, transform.position, Quaternion.identity);
            }
            Destroy(gameObject); // Destroy immediately if no animation
        }
    }

    IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    public bool IsDead()
    {
        return isDead;
    }
}