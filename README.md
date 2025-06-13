# Zombie Survival Game (Unity) Using Free Assets

This is a simple third-person zombie survival game developed in Unity. The project features basic player movement, a day/night cycle, dynamic zombie spawning, and a rudimentary objective system. (Will be improved in the future).

## Features

* **Third-Person Character Controller**: Basic player movement, sprinting with stamina, and camera control.
* **Weapon System**: Pick up, equip, fire, reload, and drop weapons. Features different weapon types via Scriptable Objects.
* **Zombie AI**:
    * Three types of zombies: Standard, Runner, and Tank, each with distinct health, speed, and attack behaviors.
    * Zombies can patrol, chase the player (with vision cones and memory), and attack.
    * Day/Night cycle influences zombie behavior (e.g., increased speed/aggression at night).
* **Dynamic Spawning**: Zombies are spawned by `ZombieSpawner` objects, which can be activated by proximity or external triggers.
* **Day/Night Cycle**: Visual and gameplay changes based on the time of day, including sun rotation and light intensity. Notifications for day/night transitions.
* **Pickup System**: Players can pick up weapons, ammo, and health kits from the environment.
* **Objective System**: A simple objective manager guides the player through tasks, currently focused on weapon collection. Features a compass UI to direct the player to objectives.
* **UI Elements**: Displays for player health, weapon ammo, and stamina.
* **Sound Effects**: Footstep sounds, weapon sounds, and ambient wind sounds.

## Project Structure

The project is organized into various C# scripts, each managing a specific aspect of the game.

### Core Systems

* `DayNightCycle.cs`: Manages the time of day, sun rotation, light intensity, and day/night notifications.
* `AmbientSoundManager.cs`: Handles continuous background ambient sounds, like wind.
* `Objective.cs`: A Scriptable Object defining a single game objective (e.g., collect a specific weapon).
* `ObjectiveManager.cs`: Manages the progression of objectives, updates UI, and directs the player with a compass.

### Player Related Scripts

* `ThirdPersonMovement.cs`: Controls player character movement, sprinting, stamina, and animation states. Also plays footstep sounds via `FootstepAudio`.
* `PlayerHealth.cs`: Manages the player's health, handles taking damage, healing, and updates the health bar UI.
* `WeaponController.cs`: Manages the player's equipped weapon, handles firing, reloading, zooming, and dropping weapons. It also interacts with `CrosshairController` for aim visuals.
* `FootstepAudio.cs`: Plays different footstep sounds based on the player's movement state (walking/sprinting) and the ground surface (concrete/grass).
* `CrosshairController.cs`: Controls the visual feedback of the crosshair, including spread from recoil and different crosshairs for zoomed aiming.

### Item & Pickup Scripts

* `WeaponData.cs`: A Scriptable Object defining properties for different weapons (damage, fire rate, ammo, etc.).
* `WeaponPickup.cs`: Handles the logic for picking up weapons from the game world. Interacts with the `WeaponController` and `ObjectiveManager`.
* `AmmoData.cs`: A Scriptable Object defining properties for different ammo types.
* `AmmoPickup.cs`: Manages the collection of ammo pickups, adding ammo to the player's current weapon.
* `HealthData.cs`: A Scriptable Object defining properties for health pickups (heal amount, sound).
* `HealthPickup.cs`: Handles the logic for picking up health items, healing the player.

### Enemy (Zombie) Scripts

* `ZombieAI.cs`: Base AI for standard zombies, defining patrol, chase, and attack states. Adapts vision and speed based on day/night cycle.
* `ZombieAI_Runner.cs`: AI for 'Runner' zombies, extending from base AI with faster movement, wider vision, and a dodge mechanic.
* `ZombieAI_Tank.cs`: AI for 'Tank' zombies, extending from base AI with higher health, more damage, and stagger resistance.
* `ZombieHealth.cs`: Base health management for standard zombies, handles taking damage, death effects, and notifies spawners upon death.
* `ZombieHealth_Runner.cs`: Health management for 'Runner' zombies, including a dodge chance and specific death animation delay.
* `ZombieHealth_Tank.cs`: Health management for 'Tank' zombies, with increased health, stagger resistance, and specific death animation delay.
* `ZombieSpawner.cs`: Spawns zombies within a defined radius or at specific points, managing the number of active zombies and total spawns. Can be activated on start, proximity, or by external triggers.
* `SpawnerActivationTrigger.cs`: A trigger volume that activates a `ZombieSpawner` when the player enters it, with options for one-time activation and delay.

### Camera Script

* `FollowCamera.cs`: A third-person camera script that follows the player, handles mouse look, and adjusts offset based on zoom state. Includes basic collision detection to prevent clipping through obstacles.

### UI Scripts

* `StaminaUI.cs`: Updates a UI slider to display the player's current stamina.

## How to Play

1.  **Clone the Repository**: Clone this GitHub repository to your local machine.
2.  **Open in Unity**: Open the project in Unity (Unity 2022.3.x LTS or later recommended).
3.  **Navigate to Scene**: Open the primary game scene (e.g., `Assets/Scenes/GameScene.unity`).
4.  **Run**: Press the Play button in the Unity Editor.

### Controls

* **W, A, S, D**: Move Character
* **Shift**: Sprint (consumes stamina)
* **Right-Click (Hold)**: Zoom Aim / Aim Down Sights
* **Left-Click**: Fire Weapon (only when zoomed)
* **R**: Reload Weapon
* **E**: Pick up Weapon/Item (when in range)
* **P**: Drop Current Weapon
* **Mouse Movement**: Control Camera

## Setup and Configuration (for Developers)

### Prefabs

Ensure the following prefabs are correctly assigned in their respective script fields in the Inspector:

* **Player Prefab**: Should have `ThirdPersonMovement`, `WeaponController`, `PlayerHealth`, `FootstepAudio`, `StaminaUI`, and `CrosshairController` attached.
    * `WeaponController`: Requires `WeaponData` Scriptable Objects, `muzzleFlashPrefab`, `impactEffectPrefab`, `bloodSplatterEffectPrefab`, and `weaponPickupPrefab` to be assigned. UI TextMeshPro and Image components also need to be linked.
    * `FootstepAudio`: Requires various `AudioClip` arrays for footstep sounds.
* **Zombie Spawners**:
    * `ZombieSpawner`: Needs `zombiePrefabs` (your zombie character prefabs) and optionally `spawnPoints`.
    * `SpawnerActivationTrigger`: Needs a `targetSpawner` (a `ZombieSpawner` in the scene).
* **Pickup Items**:
    * `WeaponPickup`: Requires a `WeaponData` Scriptable Object.
    * `AmmoPickup`: Requires an `AmmoData` Scriptable Object.
    * `HealthPickup`: Requires a `HealthData` Scriptable Object.
* **UI Canvas**:
    * Ensure your UI Canvas has `TextMeshProUGUI` for ammo display, `Image` for crosshairs, `Slider` for health and stamina, and `ObjectiveManager` UI elements (objective text, compass).
* **DayNightCycle**:
    * Needs a `Light` component assigned as `sunLight`.
    * `notificationImage`, `nightSprite`, `daySprite` need to be set up for day/night transitions.
* **FollowCamera**: `target` should be the Player character. `collisionLayers` should be configured to include environmental layers.

### Layers and Tags

* **Player**: Ensure your player GameObject has the "Player" tag.
* **Ground**: Ensure ground objects have a layer included in `groundMask` for `ThirdPersonMovement` and `groundLayerMask` for `WeaponController` (for blood splatters).
* **Zombies**: Ensure zombie prefabs have relevant tags for damage detection.
* **Vision Obstacles**: Objects that should block zombie line-of-sight should be on a layer specified in `visionObstacleMask` in the `ZombieAI` scripts.
* **Camera Collisions**: Environmental objects the camera should collide with should be on layers specified in `FollowCamera.cs`'s `collisionLayers`.

### NavMesh

* Ensure your environment has a **NavMesh** baked for zombie navigation. Without it, zombies will not be able to move.

## Known Issues / Future Improvements

* **Player Health/Death**: Currently, player death is just a debug message. A proper game over screen or respawn system is needed.
* **Zombie AI**: Can be further refined for more complex behaviors (e.g., sound attraction, breaking doors, varying attack patterns).
* **Animations**: Ensure all character and weapon animations are correctly set up and transition smoothly.
* **Objective Variety**: Expand objective types beyond just item collection.
* **Inventory System**: Implement a more robust inventory for managing multiple weapons and items.
* **Performance**: Optimize performance for larger numbers of zombies.
* **Sound Management**: Implement a more centralized sound manager for overall volume control and mixing.
* **UI/UX**: Further polish UI elements and feedback.


## 📞 Let's Connect! 📞

I'm always open to new opportunities, collaborations, or just a friendly chat! Feel free to reach out to me:

* **Location**: Toms River, NJ, USA 📍
* **Email**: beshoyaziz707@gmail.com 📧
* **Phone**: +1 (848) 333-9667 📱
* **LinkedIn**: [https://www.linkedin.com/in/beshoy-aziz-183450279/](https://www.linkedin.com/in/beshoy-aziz-183450279/) 💼
* **GitHub**: [https://github.com/BeshoyAziz7](https://github.com/BeshoyAziz7) 🐙
* **Instagram**: [https://www.instagram.com/beshoo_a8?igsh=MTR4eXd1aTVvaGRqMQ==](https://www.instagram.com/beshoo_a8?igsh=MTR4eXd1aTVvaGRqMQ==) 📸




---
