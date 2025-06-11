using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For UI Image

public class DayNightCycle : MonoBehaviour
{
public float dayDurationInSeconds = 120f;
public Light sunLight;
public float currentTime = 0.5f; // Start at midday (brightest)

[Header("Light Intensity Settings")]
public float maxDayIntensity = 1.5f; // Brightest at noon
public float minNightIntensity = 0.1f; // Darkest at midnight
public float duskDawnIntensity = 0.5f; // Intensity during sunrise/sunset

[Header("UI Settings")]
public Image notificationImage; // Reference to UI Image component
public Sprite nightSprite; // PNG sprite for "Night Falls!"
public Sprite daySprite; // PNG sprite for "Day Breaks!"
public float fadeDuration = 1f; // Duration of fade-in/fade-out
public float displayDuration = 3f; // How long the image stays on screen
private bool wasNightLastFrame = false; // Track previous night state

void Start()
{
// Ensure notification image is initially invisible
if (notificationImage != null)
{
notificationImage.color = new Color(1f, 1f, 1f, 0f); // Fully transparent
}
}

void Update()
{
// Increment time (0 = midnight, 0.5 = noon, 1 = next midnight)
currentTime += Time.deltaTime / dayDurationInSeconds;
if (currentTime >= 1f) currentTime = 0f;

// Rotate the sun (for visual day/night)
sunLight.transform.rotation = Quaternion.Euler(
Mathf.Lerp(-90f, 270f, currentTime),
170f,
0f
);

// Adjust light intensity dynamically
UpdateLightIntensity();

// Check for night/day transition
bool isNight = IsNightTime();
if (isNight != wasNightLastFrame)
{
StartCoroutine(ShowNotification(isNight));
}
wasNightLastFrame = isNight;
}

void UpdateLightIntensity()
{
// Calculate intensity based on time of day
float intensity;
if (currentTime < 0.25f) // Night (midnight to sunrise)
{
intensity = Mathf.Lerp(minNightIntensity, duskDawnIntensity, currentTime * 4f);
}
else if (currentTime < 0.5f) // Morning (sunrise to noon)
{
intensity = Mathf.Lerp(duskDawnIntensity, maxDayIntensity, (currentTime - 0.25f) * 4f);
}
else if (currentTime < 0.75f) // Evening (noon to sunset)
{
intensity = Mathf.Lerp(maxDayIntensity, duskDawnIntensity, (currentTime - 0.5f) * 4f);
}
else // Night (sunset to midnight)
{
intensity = Mathf.Lerp(duskDawnIntensity, minNightIntensity, (currentTime - 0.75f) * 4f);
}

sunLight.intensity = intensity;
}

public bool IsNightTime()
{
return currentTime < 0.25f || currentTime > 0.75f;
}

private IEnumerator ShowNotification(bool isNight)
{
if (notificationImage == null || nightSprite == null || daySprite == null) yield break;

// Set the appropriate sprite
notificationImage.sprite = isNight ? nightSprite : daySprite;

// Fade in
float elapsed = 0f;
while (elapsed < fadeDuration)
{
elapsed += Time.deltaTime;
float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
notificationImage.color = new Color(1f, 1f, 1f, alpha);
yield return null;
}
notificationImage.color = new Color(1f, 1f, 1f, 1f); // Fully opaque

// Hold display
yield return new WaitForSeconds(displayDuration);

// Fade out
elapsed = 0f;
while (elapsed < fadeDuration)
{
elapsed += Time.deltaTime;
float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
notificationImage.color = new Color(1f, 1f, 1f, alpha);
yield return null;
}
notificationImage.color = new Color(1f, 1f, 1f, 0f); // Fully transparent
}
}
