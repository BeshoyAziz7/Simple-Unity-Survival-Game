using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbientSoundManager : MonoBehaviour
{

    public AudioClip windHowlClip; // Assign wind howling sound in Inspector 
    private AudioSource audioSource; 
    private float targetVolume = 0.3f; // Subtle volume for ambient effect 
    private float fadeInSpeed = 2f; // Speed of initial fade-in

void Start()
{
    // Initialize AudioSource
    audioSource = gameObject.AddComponent<AudioSource>();
    audioSource.clip = windHowlClip;
    audioSource.loop = true; // Loop the wind sound
    audioSource.playOnAwake = false;
    audioSource.volume = 0f;

    // Start playing the sound immediately
    audioSource.Play();
    Debug.Log("Started playing background wind howl sound");
}

void Update()
{
    // Fade in to target volume at start
    if (audioSource.volume < targetVolume)
    {
        audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume, fadeInSpeed * Time.deltaTime);
    }
}

}