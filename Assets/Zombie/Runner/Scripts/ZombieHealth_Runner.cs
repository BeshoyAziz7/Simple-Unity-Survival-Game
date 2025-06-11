using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieHealth_Runner : MonoBehaviour
{
    //For spawning
    public event System.Action<GameObject> OnZombieDied; // Event to notify when this zombie dies

    public int maxHealth = 50;  // Low health
    private int currentHealth;
    public GameObject deathEffect;
    public AudioClip hitSound;
    public AudioClip deathSound;
    public float dodgeChance = 0.3f; // 30% chance to dodge attacks

    private AudioSource audioSource;
    private Animator animator;
    private ZombieAI_Runner zombieAI; // Make sure this is assigned if used
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
        zombieAI = GetComponent<ZombieAI_Runner>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        // Chance to dodge the attack completely
        if (Random.value < dodgeChance && !isDead) // Check !isDead again in case of simultaneous hits
        {
            StartCoroutine(DodgeAnimation());
            return;
        }

        currentHealth -= damage;

        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        if (zombieAI != null)
        {
            zombieAI.ForceStaggerState(); // Runner specific reaction
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

    IEnumerator DodgeAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Dodge"); // Ensure "Dodge" trigger exists
        }

        if (agent != null && agent.isActiveAndEnabled)
        {
            Vector3 dodgeDirection = Random.value > 0.5f ? transform.right : -transform.right;
            agent.Move(dodgeDirection * 2f); // Consider NavMeshAgent.Warp for immediate teleport if Move is too slow
            yield return new WaitForSeconds(0.5f); // Dodge animation/action duration
        }
    }

    IEnumerator ResumeAfterHit()
    {
        yield return new WaitForSeconds(0.2f); // Quick recovery for runner

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
            // Runners might have a quicker death/disappearance
            StartCoroutine(DestroyAfterDelay(1.5f)); // Adjust 1.5f to match runner death animation
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