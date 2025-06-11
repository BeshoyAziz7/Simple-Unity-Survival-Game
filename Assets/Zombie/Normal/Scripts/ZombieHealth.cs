using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieHealth : MonoBehaviour
{
    //For Spawning
    public event System.Action<GameObject> OnZombieDied; // Event to notify when this zombie dies

    public int maxHealth = 100;
    private int currentHealth;
    public GameObject deathEffect;
    public AudioClip hitSound;
    public AudioClip deathSound;

    private AudioSource audioSource;
    private Animator animator;
    private ZombieAI zombieAI; // Make sure this is assigned if used
    private NavMeshAgent agent;
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
        zombieAI = GetComponent<ZombieAI>();
        agent = GetComponent<NavMeshAgent>();
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
            // Consider if ForceIdleState() is appropriate here or if you want a specific "hit reaction" state in AI
            // Forcing idle might interrupt other behaviors too abruptly.
            // zombieAI.ForceIdleState(); // You might want a dedicated HitReaction in AI
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // If you want a hit animation to play, you'd trigger it here.
            // Since you removed it, ResumeAfterHit will just introduce a small delay.
            StartCoroutine(ResumeAfterHit());
        }
    }

    IEnumerator ResumeAfterHit()
    {
        yield return new WaitForSeconds(0.2f); 

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
        // This is good for logic like decrementing spawner counts.
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
            animator.SetTrigger("Death"); // Make sure "Death" is a valid trigger in your Animator Controller

            if (deathEffect != null)
            {
                Instantiate(deathEffect, transform.position, Quaternion.identity);
            }

            // Destroy the GameObject after the animation's approximate duration
            StartCoroutine(DestroyAfterDelay(3f)); // Adjust 3f to match your death animation length
        }
        else
        {
            // If no animator, still create death effect but destroy faster or immediately
            if (deathEffect != null)
            {
                Instantiate(deathEffect, transform.position, Quaternion.identity);
            }
            // Destroy immediately if there's no animation to wait for
            Destroy(gameObject);
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