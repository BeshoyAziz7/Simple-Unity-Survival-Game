using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public class FootstepAudio : MonoBehaviour { 

    public AudioClip[] concreteFootsteps; // Footstep sounds for concrete 
    public AudioClip[] grassFootsteps; // Footstep sounds for grass 
    public AudioClip[] sprintConcreteFootsteps; // Sprint footstep sounds for concrete 
    public AudioClip[] sprintGrassFootsteps; // Sprint footstep sounds for grass 
    public float footstepVolume = 0.5f; // Volume for footsteps 
    private AudioSource audioSource; 
    private bool isSprinting; 
    private bool isZoomed; 
    private bool isMoving; 
    private string currentSurface = "Concrete"; // Default surface


    void Start()
{
    audioSource = gameObject.AddComponent<AudioSource>();
    audioSource.volume = footstepVolume;
    audioSource.playOnAwake = false;
}

public void UpdateMovementState(bool sprinting, bool zoomed, bool moving)
{
    isSprinting = sprinting;
    isZoomed = zoomed;
    isMoving = moving;

    // Detect surface type under player
    RaycastHit hit;
    if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.5f, ~0))
    {
        if (hit.collider.CompareTag("Grass"))
        {
            currentSurface = "Grass";
        }
        else
        {
            currentSurface = "Concrete"; // Default to concrete if no specific tag
        }
    }
}

public void PlayFootstep()
{
    if (!isMoving || isZoomed) return; // No footsteps when idle or zoomed

    AudioClip[] clips = GetFootstepClips();
    if (clips.Length > 0)
    {
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        audioSource.PlayOneShot(clip);
    }
}

private AudioClip[] GetFootstepClips()
{
    if (isSprinting)
    {
        return currentSurface == "Grass" ? sprintGrassFootsteps : sprintConcreteFootsteps;
    }
    return currentSurface == "Grass" ? grassFootsteps : concreteFootsteps;
}



}