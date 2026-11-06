# UnityScripts

**A collection of reusable Unity C# gameplay scripts, physics experiments, and projectile-game systems by [Kai Piper](https://github.com/kai9987kai).**

[![Unity](https://img.shields.io/badge/Engine-Unity-222222?logo=unity)](https://unity.com/)
[![C%23](https://img.shields.io/badge/Language-C%23-512BD4?logo=sharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![GitHub repository](https://img.shields.io/badge/Source-GitHub-181717?logo=github)](https://github.com/kai9987kai/UnityScripts)

> **Project status:** A library of individual scripts and work-in-progress examples, **not** a complete, plug-and-play Unity project. Some scripts need integration fixes; read [Known issues](#known-issues-and-limitations) before importing the entire repository.

## Overview

UnityScripts contains example components for first-person movement, Rigidbody interactions, ball movement, trigger-based teleportation, procedural maze generation, runtime object placement, impact-driven mesh deformation, and a more substantial physics-based projectile-target game. The scripts are designed as learning resources and starting points for Unity prototypes, coursework experiments, and small games.

**What's included**

- **First-person movement:** walking, sprinting, mouse look, gravity, and jumping via a `CharacterController`.
- **Gravity gun:** raycast pickup, physics-based holding, distance adjustment, rotation, and charged throws.
- **Rigidbody ball movement:** simple WASD force controls and a Shift modifier.
- **Portal-style teleporting:** a trigger-based ball teleportation experiment.
- **Procedural mazes:** seeded, fully connected 3D mazes generated at runtime, with colliders, path preview, and completion callbacks.
- **Runtime construction:** grid snapping, ghost previews, rotation, obstruction checks, placement/removal, and undo/redo.
- **Impact deformation:** collision dents and scripted impacts on readable meshes, with optional elastic recovery and collider updates.
- **Time and weather:** configurable day/night cycle, overcast lighting, fog, optional wind and precipitation, and storm lightning.
- **Stealth NPC AI:** NavMesh patrols, sight cones, suspicion accumulation, scripted noise events, pursuit, and searching the last known position.
- **Prefab pooling:** bounded reusable GameObject pools with prewarming, timed returns, lifecycle callbacks and diagnostics.
- **Projectile game:** two alternative launcher controllers, charge-to-fire aiming, trajectory visualization, target/obstacle spawning, a shot counter, and win/lose state management.

## Repository contents

```text
UnityScripts/
├── AdvancedGravityGun.cs
├── Balltoredportal.cs
├── DynamicWorldEnvironment.cs
├── PhysicsImpactDeformer.cs
├── PlayerController.cs
├── ProceduralMazeGenerator_Unity.cs
├── RuntimeBuildingSystem.cs
├── RuntimeObjectPool.cs
├── StealthGuardAI.cs
├── WASD-withshift-ballmovement.cs
├── projectileballgame/
│   ├── Aim.cs
│   ├── BallController.cs
│   ├── GameManager.cs
│   ├── LauncherController.cs
│   └── Trajectory.cs
├── CODE_OF_CONDUCT.md
├── LICENSE
├── README.md
└── SECURITY.md
```

### Script reference

| Script | Component class | Purpose |
| --- | --- | --- |
| [`AdvancedGravityGun.cs`](AdvancedGravityGun.cs) | `AdvancedGravityGun` | Grab, hold, rotate, and throw non-kinematic Rigidbodies using a physics controller. |
| [`DynamicWorldEnvironment.cs`](DynamicWorldEnvironment.cs) | `DynamicWorldEnvironment` | A day/night clock and five smoothly blended weather presets, with optional ambient/fog, WindZone, rain/snow and lightning effects. |
| [`StealthGuardAI.cs`](StealthGuardAI.cs) | `StealthGuardAI` | NavMesh-based finite-state patrol, vision, noise-driven investigation, chase, and last-known-position search. |
| [`RuntimeObjectPool.cs`](RuntimeObjectPool.cs) | `RuntimeObjectPool` | Named, bounded prefab pools with prewarming, expansion, timed return, reset callbacks, and active/available statistics. |
| [`PlayerController.cs`](PlayerController.cs) | `SimpleFPSController` | CharacterController-based first-person movement, sprinting, jumping, and mouse look. |
| [`ProceduralMazeGenerator_Unity.cs`](ProceduralMazeGenerator_Unity.cs) | `ProceduralMazeGenerator` | Generate deterministic, connected mazes with solid walls/floors, optional shortest-path visualization, and a completion event. |
| [`RuntimeBuildingSystem.cs`](RuntimeBuildingSystem.cs) | `RuntimeBuildingSystem` | Place and remove pieces at runtime with previews, snapping, rotation, obstruction checks, and undo/redo. |
| [`PhysicsImpactDeformer.cs`](PhysicsImpactDeformer.cs) | `PhysicsImpactDeformer` | Deform readable meshes from physics contacts or direct impacts, with optional recovery and MeshCollider refresh. |
| [`WASD-withshift-ballmovement.cs`](WASD-withshift-ballmovement.cs) | `Ballmovement` | Basic Rigidbody force movement using WASD, with experimental Shift behaviour. |
| [`Balltoredportal.cs`](Balltoredportal.cs) | `TeleportBall` | Teleport a ball to an assigned destination when it enters a trigger; requires a code fix (see below). |
| [`projectileballgame/Aim.cs`](projectileballgame/Aim.cs) | `AimController_EvenMore_Human` | Charge and release projectile shots; aim using mouse or keyboard. |
| [`projectileballgame/BallController.cs`](projectileballgame/BallController.cs) | `BallController` | Ball lifetime, collisions, targets, obstacles, kill zones, and death notifications. |
| [`projectileballgame/GameManager.cs`](projectileballgame/GameManager.cs) | `GameManager` | Shot accounting, spawn distribution, target counts, level restart, and game-over screens. |
| [`projectileballgame/LauncherController.cs`](projectileballgame/LauncherController.cs) | `LauncherController` | An alternative, power/elevation-based projectile launcher with TMP displays. |
| [`projectileballgame/Trajectory.cs`](projectileballgame/Trajectory.cs) | `TrajectoryLine` | Render a ballistic preview with a `LineRenderer` while using the aim controller. |

## Requirements

- A Unity Editor installation with 3D physics support. **Unity 2022.3 LTS or Unity 6** is a reasonable starting point, but no specific Unity version has been verified against the complete collection.
- C# scripting enabled (standard in Unity).
- For scripts that use `UnityEngine.Input`, enable **Input Manager (Old)** or set **Active Input Handling** to **Both** (`Edit > Project Settings > Player > Other Settings`). In particular, `RuntimeBuildingSystem` and the older controllers require legacy input. `ProceduralMazeGenerator` supports legacy input and, when the Input System package is installed/enabled, its `R`/`H` keyboard shortcuts also support the new Input System. The optional middle-click demo in `PhysicsImpactDeformer` requires legacy input; collision-driven deformation and its public API do not.
- **TextMeshPro** for `projectileballgame/GameManager.cs` and `LauncherController.cs` (`using TMPro;`). Install it through Package Manager if necessary and import its essential resources when prompted.
- An appropriately configured Unity scene for most examples: cameras, collision geometry, components, prefabs, Inspector references, and tags are **not** supplied in this repository. The maze generator creates its own geometry; the building system can create three demonstration pieces if no prefabs are assigned.
- For `PhysicsImpactDeformer`, a mesh with **Read/Write Enabled** (on imported model assets), sufficient vertex density for visible dents, and a Collider for collision-driven use.
- For `DynamicWorldEnvironment`, a directional Light is needed for visible sunlight; moonlight, lightning, WindZone, and rain/snow ParticleSystems are optional. The script does not generate particle prefabs, cloud geometry, or a skybox.
- For `StealthGuardAI`, a guard GameObject with a **NavMeshAgent** standing on a **baked NavMesh**, plus an assigned target and optional patrol point Transforms. Depending on Unity version/project setup, install **AI Navigation** via Package Manager for NavMesh baking. The component uses `UnityEngine.AI`; it needs no separate player controller.
- For `RuntimeObjectPool`, configure at least one named **Prefab** in the component's **Pool Definitions** list. No other packages or special Unity input settings are required.

## Installation

1. Download the repository using **Code > Download ZIP**, or clone it:

   ```bash
   git clone https://github.com/kai9987kai/UnityScripts.git
   ```

2. Open your existing **3D Unity project**.
3. Copy the script or script group you want into a folder such as `Assets/Scripts/`. For the projectile game, keep all related files together initially.
4. **Rename scripts with mismatched filenames before attaching them to GameObjects.** Unity MonoBehaviour components should have a script filename matching their class name:

   | Repository filename | Rename in Unity to |
   | --- | --- |
   | `Balltoredportal.cs` | `TeleportBall.cs` |
   | `PlayerController.cs` | `SimpleFPSController.cs` |
   | `WASD-withshift-ballmovement.cs` | `Ballmovement.cs` |
   | `projectileballgame/Aim.cs` | `AimController_EvenMore_Human.cs` |
   | `projectileballgame/Trajectory.cs` | `TrajectoryLine.cs` |
   | `ProceduralMazeGenerator_Unity.cs` | `ProceduralMazeGenerator.cs` |

5. Let Unity compile the scripts. If you imported the whole repository, address the [`Aim.cs` private-field compile error](#known-issues-and-limitations) first.
6. Add the desired component(s) to suitable GameObjects and assign the Inspector references. Follow the setups below.

> **Tip:** Import only the components you intend to use. A compile error anywhere under `Assets/` can prevent unrelated scripts from running.

## Quick-start examples

### 1. First-person player (`SimpleFPSController`)

1. Create a player GameObject (for example, a Capsule) and add **CharacterController**.
2. Add `SimpleFPSController` (after renaming `PlayerController.cs`).
3. Add a Camera as a **child of the player** and assign its tag **MainCamera**.
4. Place the player above a floor with a Collider.
5. Adjust `walkSpeed`, `sprintMultiplier`, `jumpForce`, `mouseSensitivity`, and related values in the Inspector.
6. Press **Play** and click the Game view to focus input.

| Input | Behaviour |
| --- | --- |
| `W` / `A` / `S` / `D` | Move relative to player orientation |
| Mouse | Look around |
| `Left Shift` | Sprint |
| `Space` | Jump when grounded |

This script uses `CharacterController.Move`, not Rigidbody movement. It expects both a valid `CharacterController` and an active camera tagged `MainCamera`; a missing main camera can cause a runtime null-reference error.

### 2. Gravity gun (`AdvancedGravityGun`)

1. Add `AdvancedGravityGun` **to the player's Camera GameObject** (the script requires a `Camera` component).
2. Place one or more 3D objects in the scene with **Colliders** and **non-kinematic Rigidbodies**.
3. Use the Inspector to configure `pickupRange`, `maximumObjectMass`, `pickupLayers`, hold distance, spring/damping, and throw power.
4. Enter Play mode and aim the camera at a physics object.

| Input | Behaviour |
| --- | --- |
| `E` | Pick up / drop a Rigidbody |
| Mouse wheel | Increase / decrease holding distance |
| `Q` / `R` | Rotate held object |
| Hold left mouse button | Charge a throw |
| Release left mouse button | Throw the object |

The component uses a raycast for pickup and proportional-derivative (PD) force/torque control in `FixedUpdate`. It temporarily disables gravity for the held object and restores previous Rigidbody settings on release. Pickup ignores triggers and respects the configured layer mask and mass limit. It supports Unity 6's `Rigidbody.linearVelocity` with a conditional fallback to `Rigidbody.velocity` for older versions.

### 3. WASD ball experiment (`Ballmovement`)

1. Create a Sphere with a **Rigidbody** and a Collider.
2. Attach `Ballmovement` (rename `WASD-withshift-ballmovement.cs`).
3. Drag the Sphere's Rigidbody into the script's `rb` Inspector field and set `speed`.
4. Play and experiment with `W`, `A`, `S`, `D`, and `Left Shift`.

**Caution:** The current implementation applies forces in `Update`, and Shift applies additional **forward** force even while moving sideways or backwards. This is experimental behaviour, not a conventional all-direction sprint controller; for consistent Rigidbody physics, move force application to `FixedUpdate` and revise the Shift logic.

### 4. Teleportation experiment (`TeleportBall`)

The intended setup is a portal/trigger Collider with **Is Trigger** enabled, plus a separate destination Transform assigned to `redSphere`. Balls should be tagged `Ball` and have a Rigidbody and Collider.

**Important:** `TeleportBall` currently stores the Rigidbody from the GameObject hosting the portal script, rather than from the entering ball. Attaching it to a trigger portal without a Rigidbody on that same GameObject causes a null-reference error when teleporting. Update the teleport method to obtain the entering ball's Rigidbody (for example, via `ball.GetComponent<Rigidbody>()`) before use. The extra `OnTriggerEnter(BoxCollider)` overload is unnecessary; Unity invokes the standard `OnTriggerEnter(Collider)` message.

### 5. Procedural 3D maze (`ProceduralMazeGenerator`)

[Source](ProceduralMazeGenerator_Unity.cs) — a self-contained maze generator using an **iterative randomized depth-first search**. It creates a connected maze from a seed, generates floor/wall geometry with Colliders, and computes a shortest route with breadth-first search.

1. Copy `ProceduralMazeGenerator_Unity.cs` into `Assets/Scripts/` and **rename it to `ProceduralMazeGenerator.cs`**.
2. Add `ProceduralMazeGenerator` to an empty GameObject.
3. Optionally assign a player Transform (for example, the object with `SimpleFPSController`) to **Player**. If assigned, the generator moves it to the start on generation and checks whether it reaches the exit.
4. Set `mazeWidth`, `mazeDepth`, `cellSize`, and `seed` in the Inspector. The size ranges from **2×2 to 40×40 cells**.
5. Press **Play**. With `generateOnStart` enabled, the maze appears automatically. The orange marker denotes the exit.

| Input | Behaviour |
| --- | --- |
| `R` | Regenerate the maze (same layout if the seed remains fixed) |
| `H` | Toggle visualization of the shortest route |

Turn on `randomizeSeedOnGenerate` for new layouts each time. The `onMazeCompleted` UnityEvent is invoked when the **assigned player** reaches the exit; if Player is unassigned, only the maze/markers and route preview operate. You can also invoke `Generate()`, `ToggleSolution()`, or `GetStartWorldPosition()` from other scripts. The generator creates **runtime primitives**, not serialized scene assets or a NavMesh; larger mazes can create many GameObjects and Colliders.

### 6. Runtime construction (`RuntimeBuildingSystem`)

[Source](RuntimeBuildingSystem.cs) — an in-game placement prototype with a ghost preview, optional grid snapping, rotation steps, overlap checks, and undo/redo.

1. Add `RuntimeBuildingSystem` to an empty GameObject.
2. Assign **Build Camera**, or ensure your active camera is tagged **MainCamera**.
3. Leave `placeablePrefabs` empty to use the three automatically created example pieces: **wall, platform, pillar**. Alternatively, supply your own prefab array (including renderers, and preferably Colliders for placed-object selection and obstruction checks).
4. Use a floor with a Collider in the `placementSurfaces` layer mask, or keep `allowFallbackGroundPlane` enabled for placement on a virtual horizontal plane.
5. Press **Play**. The builder enters build mode by default and displays an on-screen status panel.

| Input | Behaviour |
| --- | --- |
| `B` | Toggle build mode |
| `1`–`9` / mouse wheel | Choose an available building piece |
| `Q` / `E` | Rotate the preview by `rotationStep` |
| `G` | Toggle grid snapping |
| Left mouse button | Place the selected piece when the preview is valid |
| Right mouse button | Remove the first hit placed piece |
| `Ctrl+Z` / `Ctrl+Y` | Undo / redo a placement or removal |

The preview uses the camera centre when the cursor is locked, otherwise the mouse position. Configure `gridSize`, `maxBuildDistance`, `placementSurfaces`, `obstructionLayers`, and `checkForObstructions` in the Inspector. An exposed API includes `SetBuildMode(bool)`, `Undo()`, `Redo()`, and `ClearAllBuildings()`. This is an **in-memory runtime prototype**: it does not save structures across Play sessions, and it uses the **legacy Input Manager**.

### 7. Physics impact deformation (`PhysicsImpactDeformer`)

[Source](PhysicsImpactDeformer.cs) — visually deform a mesh at collision contact points or from scripted impact positions, and optionally allow dents to recover over time.

1. Create a 3D mesh object with a **MeshFilter**, MeshRenderer, and non-trigger Collider. For imported models, enable **Read/Write** in the Model Import Settings; ensure the mesh has enough vertices for visibly localized deformation.
2. Attach `PhysicsImpactDeformer` to the same GameObject as the MeshFilter.
3. Set `minimumImpactSpeed`, `impactRadius`, `maximumVertexDisplacement`, and `collisionStrengthMultiplier` to control denting.
4. With `deformOnCollisions` enabled, strike the object using another physics object configured to generate collision callbacks (typically a moving Rigidbody). Contacts above the speed threshold cause dents.
5. For a manual test, tag a camera **MainCamera**, enter Play mode, and **middle-click** the deformable object's collider. This optional test needs the **legacy Input Manager**.
6. Enable `enableRecovery` for gradual return to the original mesh, or invoke `RepairImmediately()` to restore the shape immediately.

The public API also exposes `ApplyImpact(worldPosition, inwardWorldDirection, strength)`, which returns whether any vertices changed. For a surface raycast hit, the inward direction is normally `-hit.normal`. Strength is a **world-space displacement**, not an impulse magnitude.

By default, the script deforms **only the visual mesh**. `updateMeshCollider` can optionally synchronize a matching MeshCollider at a throttled interval, but collider recooking is expensive and Unity has restrictions on non-convex MeshColliders attached to dynamic Rigidbodies. Use this feature cautiously on static/environment geometry. The source asset is not modified: the component clones the mesh at runtime.

### 8. Dynamic day/night and weather (`DynamicWorldEnvironment`)

[Source](DynamicWorldEnvironment.cs) — a standalone example with a 24-hour clock and five transitionable weather presets: **Clear, Overcast, Rain, Storm, Snow**. It controls directional lights, ambient lighting, fog, wind and optional precipitation emission.

1. Create an empty GameObject named **World Environment** and attach `DynamicWorldEnvironment.cs`.
2. Add a **Directional Light** to your scene and assign it as **Sun** (or set it as the scene's Render Settings sun). Optionally assign a second directional Light as **Moon**.
3. Choose **Starting Hour**, **Full Day Minutes**, **Starting Weather**, and **Transition Seconds**. The default is one 24-hour game day every **20 real minutes**.
4. Press **Play**. The sun moves throughout the day, with simplified sunrise at **06:00**, solar noon at **12:00**, and sunset at **18:00**.
5. For precipitation, create and configure rain and/or snow **ParticleSystems**, position them in your scene (or follow the player using your own component), and assign them to **Rain Particles** or **Snow Particles**. The script changes particle emission and starts/stops those systems; it does **not** create the particle assets.
6. Optionally assign a **WindZone** and an extra **Lightning Light**. Storms then animate wind and produce short light flashes at random intervals.

| Keyboard control | Effect |
| --- | --- |
| `F6` | Pause/resume the clock |
| `F7` | Cycle through weather presets |
| `F8` | Advance one simulated hour |

The keyboard demonstration uses the **legacy Input Manager**. In Input System-only projects, call the public methods instead:

```csharp
using UnityEngine;

public class WeatherDemo : MonoBehaviour
{
    [SerializeField] private DynamicWorldEnvironment environment;

    public void StartStorm()
    {
        environment.SetWeather(DynamicWorldEnvironment.WeatherType.Storm);
        environment.SetTime(22f);
    }

    public void StopTime()
    {
        environment.PauseClock(true);
    }
}
```

The component exposes `TimeOfDayHours`, `Sunlight01`, `IsNight`, `TargetWeather`, `RainIntensity01`, and `SnowIntensity01`, plus the `WeatherChanged` C# event. A weather change event fires when a preset is **requested**, while the actual visual blending occurs over `transitionSeconds`.

**Limitations:** This is a lightweight environmental prototype, not a physically based weather simulator. It does **not** generate clouds, thunder audio, particle assets, skybox transitions, or weather-dependent gameplay physics. Fog and ambient effects use Unity's `RenderSettings`; URP/HDRP volume-based workflows may require adaptation. When the component is disabled, its prior global fog/ambient values are restored by default; Light and WindZone values are not restored.

### 9. Stealth guard AI (`StealthGuardAI`)

[Source](StealthGuardAI.cs) — a single-component NPC controller with a five-state finite-state machine and configurable sensory thresholds. It uses Unity's `NavMeshAgent` to move rather than directly adjusting transforms.

| State | Behaviour |
| --- | --- |
| `Patrol` | Walk sequentially between assigned waypoint Transforms |
| `Investigate` | Move toward a reported noise or partially observed target |
| `Chase` | Pursue the target's most recently observed position |
| `Search` | Check randomly sampled, NavMesh-reachable positions around the last known location |
| `Return` | Resume the patrol route after searching |

**Setup**

1. In a Unity 3D scene, create walkable terrain or a floor and **bake a NavMesh**. In projects that use the AI Navigation package, add/configure a `NavMeshSurface` and bake it. Verify the guard starts **on** a navigable surface.
2. Create an enemy GameObject (a Capsule is sufficient), add a **NavMeshAgent**, then attach `StealthGuardAI.cs`.
3. Assign the player's **Transform** to the guard's **Target** field. Give the player a Collider if you want a raycast to confirm that the player is the first hit; the script also allows unobstructed targets without Colliders.
4. Create several empty GameObjects around the environment and put their Transforms in **Patrol Points**. Leave the array empty to keep the guard stationary until it sees/hears something.
5. Optionally assign an **Eyes** Transform at head height; otherwise the script offsets the guard's origin using **Eye Height**.
6. Configure **Sight Range**, **Field Of View**, **Vision Layers**, **Hearing Radius**, **Detection Threshold**, patrol/chase speeds and search durations.
7. Press **Play**. Use the **Scene** view to see the yellow vision boundary, cyan hearing-radius sphere, and the magenta last-known-position marker when applicable.

**Trigger noise from gameplay**

Add the following call to a footstep, thrown-object, or weapon script; loudness `1f` means its normal hearing radius, and `2f` doubles it:

```csharp
// Called when a noise should alert nearby guards.
StealthGuardAI.BroadcastNoise(transform.position, 1f);
```

Or notify a specific guard:

```csharp
guard.HearNoise(impactPoint, 1.5f);
```

Other scripts can call `SetTarget(Transform)`, inspect `CurrentState`, `Suspicion01`, `CanSeeTarget` and `LastKnownPosition`, or subscribe to the `StateChanged` C# event. No player movement or noise is generated automatically.

**Sensor and performance notes:** Sight uses a view angle, maximum distance and a line-of-sight raycast at a configurable interval (`senseInterval`, default **0.15 s**). A blocker must be included in `visionLayers` to obstruct sight. Noise is a distance-based event and **does not model acoustic occlusion**. Guards are registered in a static list for broadcasts, so sending very frequent noise events to many guards may require optimization. The search system samples positions against the baked NavMesh; it does not build or rebake navigation geometry at runtime.

### 10. Runtime prefab pooling (`RuntimeObjectPool`)

[Source](RuntimeObjectPool.cs) — a reusable pool manager for **bullets, projectiles, particle effects, enemies, pickups, and other short-lived objects**. Reusing deactivated GameObjects generally avoids repeated Instantiate/Destroy calls and can reduce allocation spikes.

**Setup**

1. Create a scene GameObject called **Pool Manager**, then attach `RuntimeObjectPool.cs`.
2. Add at least one item to **Pool Definitions**. Give it a unique ID (for example, `Projectile`), assign a prefab, and set **Prewarm**, **Maximum Instances**, and **Expand When Exhausted**.
3. Set the prefab's normal active state to **enabled**. The manager creates its initial instances under an inactive staging parent, then stores them inactive until they are rented.
4. Reference the manager from gameplay scripts via a serialized `RuntimeObjectPool` field. Spawn using the matching pool ID and return objects rather than calling `Destroy()`.
5. Optionally enable **Show Stats Overlay** to inspect active, available, and total instance counts during Play mode.

**Spawning a projectile**

```csharp
using UnityEngine;

public sealed class PooledProjectileLauncher : MonoBehaviour
{
    [SerializeField] private RuntimeObjectPool pool;
    [SerializeField] private Transform muzzle;
    [SerializeField] private float projectileSpeed = 20f;

    public void Fire()
    {
        if (pool == null || muzzle == null) return;

        // Returns automatically after 4 seconds unless returned sooner.
        GameObject projectile = pool.Spawn(
            "Projectile", muzzle.position, muzzle.rotation, null, 4f);
        if (projectile == null) return; // Pool exhausted or misconfigured.

        Rigidbody body = projectile.GetComponent<Rigidbody>();
        if (body != null)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = muzzle.forward * projectileSpeed;
#else
            body.velocity = muzzle.forward * projectileSpeed;
#endif
        }
    }
}
```

The launcher code is a documentation example, **not an additional file**. Put it in `PooledProjectileLauncher.cs` if you want to try it. Configure the pool with an active `Projectile` prefab containing a Collider and Rigidbody; the optional four-second lifetime is measured using scaled `Time.time`.

**Public API**

| Method / property | Use |
| --- | --- |
| `Spawn(id, position, rotation, parent, lifetimeSeconds)` | Rent and activate a pooled GameObject; returns `null` if unavailable |
| `Return(instance)` | Deactivate and recycle a rented object |
| `ReturnAll(id)` | Return all active instances from one pool |
| `TryGetStats(id, out active, out available, out total)` | Read pool usage counters |
| `Initialize()` | Initialize pool definitions (also called automatically in `Awake`) |
| `PoolCount` | Number of configured, valid pools |

For a projectile that hits something before the lifetime expires, call `pool.Return(gameObject)` from its collision logic. The manager will cancel its pending timed return.

**Resetting pooled behaviours**

Because reuse does **not** create a fresh GameObject or run `Awake` again, a pooled component should explicitly reset variables, physics, timers, and visual effects as necessary. Implement `RuntimeObjectPool.IPoolCallbacks` on any attached MonoBehaviour to receive `OnPoolSpawned()` and `OnPoolReturned()` notifications. The pool caches these interfaces when it creates an instance. Components added dynamically afterward are not automatically included in that cache.

**Limits and caveats:** Pool limits are per ID. When `expandWhenExhausted` is false (or maximum capacity is reached), `Spawn` returns `null` instead of silently stealing an active object. Avoid directly calling `Destroy` on pooled instances; use `Return`. The manager can reclaim capacity from externally destroyed active instances during periodic cleanup. Auto-return and cleanup depend on the manager remaining enabled, and rented instances are destroyed with their owning manager. Do not use object pooling as a drop-in replacement for projectile scripts that rely on `OnDestroy` unless their lifecycle has been adapted.

### Combining components

These components are independent demos, not a unified input framework. Some default keys overlap: `R` regenerates the maze but rotates a held gravity-gun object; `Q`/`E` control building rotation while `Q` also rotates gravity-gun objects and `E` is gravity-gun pickup. Disable/remap conflicting controls or only enable the relevant component while testing. Both the maze and building system use on-screen IMGUI panels in the top-left, which may overlap if enabled together.

## Projectile ball game

The [`projectileballgame/`](projectileballgame/) folder is a multi-component prototype in which players launch balls towards spawned targets. The `GameManager` tracks blue targets, grants extra shots after green-target hits, manages ball lifecycle, and displays win/lose UI. The scripts are **not fully integrated in their current state**; see the issues below before attempting a full scene build.

### Core scene setup

1. Create one `GameManager` GameObject and attach `GameManager.cs`.
2. Create a **Ball prefab** with a Sphere Collider, Rigidbody, `BallController`, and `Ball` tag. Assign the prefab and a scene `ball_Starting_Spawn_Point` Transform to `GameManager`.
3. Create target prefabs with non-trigger Colliders. Use the exact tags **`BlueTarget`** and **`GreenTarget`**. Create obstacles tagged **`Obstacle`**. The script also recognises **`Ground`** on collisions and **`KillZone`** on trigger entry.
4. Create several empty GameObjects as spawn anchors and assign them to `where_To_Spawn_Items_Anchors`. Configure spawn radius, spacing checks, target counts, and obstacle counts. Give the scene enough clear space for `Physics.CheckSphere` to find free spawn positions.
5. Make a Canvas with two TextMeshProUGUI labels (shots left, blue targets left) and optional Win/Lose panels. Assign the UI references in `GameManager`.
6. Choose **one** of the launcher approaches described below. **Do not run both launcher components on the same launcher.**

### Launcher A — `LauncherController` (pre-spawned ball)

`GameManager` creates a ball at the starting spawn point and makes its Rigidbody kinematic. `LauncherController` locates that ball by its `Ball` tag, makes it dynamic, and applies an impulse. This is the closer match to `GameManager`'s current pre-spawn architecture.

Attach `LauncherController` to a launcher GameObject, set `actual_Launch_Point_Transform`, and optionally link its TextMeshProUGUI fields. Ensure the GameManager's ball spawn position and the launch point are aligned.

| Input | Behaviour |
| --- | --- |
| `A` / `D` | Rotate launcher left / right (yaw) |
| `W` / `S` | Increase / decrease launch power |
| `Q` / `E` | Increase / decrease elevation |
| Left mouse button or `Space` | Fire |

`LauncherController` uses `final_Launch_Force_Multiplier` to scale its impulse. It needs a `GameManager` in the scene.

### Launcher B — `AimController_EvenMore_Human` + `TrajectoryLine`

This alternative launcher charges a shot while the player holds **left mouse button or Space**, and fires upon release. Keyboard `A`/`D` adjust yaw; `W`/`S` adjust pitch. Holding **right mouse button** attempts to aim towards a raycast hit on objects in the **`Ground` layer**.

- Attach `AimController_EvenMore_Human` to a launcher GameObject; assign `launch_Point_Transform` and a Rigidbody-equipped `ball_Prefab_To_Make`.
- Add a **LineRenderer** and `TrajectoryLine` to an object; assign `theAimController` to the launcher component.
- Configure line resolution and time step. The line appears only while aiming and shows a gravity-based ballistic estimate.
- Optional: link an `AudioSource` and firing `AudioClip` on the aim controller.

**Integration warning:** The aim controller **instantiates a new ball**, whereas the current `GameManager` **already pre-spawns one**. These designs must be reconciled for a reliable game loop. The aim controller also has a compilation error described below. The trajectory preview treats impulse strength as initial speed; the displayed path can differ from the ball's actual flight when its Rigidbody mass is not 1, or when drag/collisions affect it.

### Gameplay rules and tags

| Tag | Expected interaction |
| --- | --- |
| `Ball` | Projectile prefab or active ball; needs a Rigidbody for launcher operation |
| `BlueTarget` | Colliding ball destroys target; `GameManager` decrements remaining blue targets |
| `GreenTarget` | Colliding ball destroys target and ends the ball's life; `GameManager` awards bonus shots (default: 3) |
| `Obstacle` | Colliding ball dies while obstacle remains |
| `Ground` | Colliding ball can continue; layer name `Ground` is also used by mouse aiming (tags and layers are different) |
| `KillZone` | Trigger destroys ball after its initial kill-zone grace period |

The game starts with **10 shots** by default. It attempts to spawn **12 blue targets**, **4 green targets**, and **15 obstacles**, subject to available space and anchor configuration. Destroy all blue targets to win; use all shots while blue targets remain to lose. The level pauses when the game ends. `GameManager` provides public methods for restart and quit, which can also be connected to UI Buttons.

## Known issues and limitations

This repository is suitable for experimentation, but not all scripts are production-ready:

1. **`Aim.cs` currently fails to compile when `GameManager.cs` is present.** It accesses `GameManager`'s private fields `is_Ball_Currently_Active_Internal`, `is_Game_Over_Internal`, and `current_Shots_Left_Internal` in a diagnostic `Debug.Log` expression. Remove those private-field references from that log or expose appropriate public read-only properties. The existing public `Query_IsPlayerAllowedToFireAShot()` method already provides the required permission check.
2. **Six script filenames do not match their MonoBehaviour class names.** Use the rename table in [Installation](#installation) before attaching those scripts in Unity.
3. **The two projectile launchers use incompatible spawning assumptions.** `LauncherController` acts on a ball pre-created by `GameManager`, while `AimController_EvenMore_Human` instantiates a new ball. Use one launcher approach and adapt GameManager's ball lifecycle accordingly.
4. **`TeleportBall` references the wrong Rigidbody for typical portal setups.** It should operate on the incoming ball's Rigidbody; otherwise, runtime errors may occur.
5. **Trajectory prediction is approximate.** `TrajectoryLine` uses a simple ballistic formula and currently assumes launch force corresponds to initial velocity; mass, collisions, aerodynamic drag, and other forces are not simulated in the preview.
6. **Ball movement applies physics forces in `Update` rather than `FixedUpdate`.** Movement therefore depends on the rendering loop, and the Shift behaviour does not match a conventional sprint modifier.
7. **Ball lifetime starts as soon as the ball prefab is created.** The GameManager's waiting/kinematic ball can time out before being fired. The controller should ideally start its lifetime when launched.
8. **End-screen keyboard handling is limited.** `GameManager.Update()` returns early when the game is over, so its `R` restart and `Escape` quit shortcuts are inactive at that stage. Connect end-screen UI Buttons to `MyAction_RestartLevel()` / `MyAction_QuitGame()` or adjust the input ordering.
9. **This repository does not contain a Unity project, test suite, sample scene, or package manifest.** Functionality depends on configuring your own scene; compatibility and behaviour have not been verified end-to-end.
10. **Procedural mazes exist only during Play mode.** They are built as runtime primitive GameObjects, and large dimensions (up to 40×40) can cause heavy GameObject, rendering, and physics overhead. No NavMesh or baked scene is generated.
11. **Runtime building does not persist placed pieces.** Undo/redo history and placements are session-only. It requires legacy input; use appropriate layer masks, Colliders, and a camera for reliable placement and removal.
12. **Mesh deformation has geometry and physics constraints.** The source mesh must be readable, low-resolution meshes show coarse dents, and visual deformation does not alter collision geometry unless MeshCollider refresh is explicitly enabled. Frequent recooking is performance-intensive.
13. **Demo controls may conflict.** The maze, building system, gravity gun, and projectile examples reuse some keys. See [Combining components](#combining-components).
14. **Weather is not a complete atmosphere simulation.** `DynamicWorldEnvironment` requires supplied particle systems for visible precipitation, and URP/HDRP projects may need pipeline-specific overrides for fog and sky.
15. **Stealth guard AI requires precomputed navigation.** `StealthGuardAI` cannot navigate without a valid baked NavMesh. Hearing only responds to explicit calls to `HearNoise` or `BroadcastNoise`; sight requires a target reference and sensible physics layer masks. It does not implement attack, animation, cover tactics, or networking.
16. **Pooled objects require an explicit reset protocol.** `RuntimeObjectPool` reuses existing objects; per-instance velocity, animations, particles, collision state, or custom script state may persist between rentals unless your components reset themselves. Existing scripts that destroy bullets instead of returning them need integration changes.

Issues and fixes are welcome via [GitHub Issues](https://github.com/kai9987kai/UnityScripts/issues).

## Troubleshooting

| Symptom | What to check |
| --- | --- |
| **Can't add script component** | Rename the `.cs` file to match the class exactly; check the Console for compile errors. |
| **`CS0122` or inaccessible member error** | In `Aim.cs`, remove references to the three private `GameManager` fields or add safe read-only accessors. |
| **`TMPro` namespace missing** | Install/import TextMeshPro through Package Manager. |
| **Keyboard/mouse doesn't respond** | Set Active Input Handling to **Input Manager (Old)** or **Both**, then focus the Game view. |
| **Ball/target isn't detected** | Check exact tag spelling, colliders, Rigidbody state, and trigger-versus-solid Collider configuration. |
| **No trajectory line** | Add a `LineRenderer`, assign `theAimController` and launch point, then hold the aim/fire button. |
| **Objects do not spawn** | Assign spawn anchors/prefabs and increase usable space or adjust overlap radius and spawn attempts. |
| **Gravity gun won't grab an object** | Ensure it has a non-kinematic Rigidbody, appropriate mass, a Collider, and is in the pickup layer/range. |
| **Teleport causes an exception** | Fix `TeleportBall` so it uses the ball's Rigidbody, not the portal's. |
| **Maze script can't be attached** | Rename `ProceduralMazeGenerator_Unity.cs` to `ProceduralMazeGenerator.cs`, then check Unity's Console. |
| **Maze completion never fires** | Assign a **Player** Transform and ensure it reaches the orange exit marker; the callback is position-based, not a trigger. |
| **Build preview won't place** | Check **Build Camera** / MainCamera, the raycast range, a valid placement surface or fallback plane, obstruction masks, and the preview colour. |
| **Runtime builder input fails** | Enable **Input Manager (Old)** or **Both**; `RuntimeBuildingSystem` uses `UnityEngine.Input`. |
| **Deformer stays flat or disables itself** | Check **Read/Write Enabled** on an imported mesh, enough mesh subdivisions, collision speed above the threshold, and a non-trigger Collider. |
| **Collision shape doesn't match a dent** | Visual changes are separate from physics by default. Add a matching MeshCollider and enable `updateMeshCollider` only if its performance/physics trade-offs are acceptable. |
| **Weather changes but there is no rain or snow** | Assign real rain/snow ParticleSystems; the component controls emission but does not create particles or materials. |
| **Day/night lighting doesn't change** | Assign a Directional Light as Sun (or set RenderSettings.sun), and check the active render pipeline's volume/environment lighting configuration. |
| **Stealth guard doesn't move** | Bake a NavMesh, place the guard on it, ensure its NavMeshAgent is enabled, and assign valid patrol point Transforms. |
| **Stealth guard sees through walls** | Include obstacle layers in `visionLayers`; vision raycasts ignore objects excluded from that mask. |
| **Stealth guard never hears footsteps** | Explicitly call `StealthGuardAI.BroadcastNoise(...)` or `HearNoise(...)` from the footstep/impact event; this is not automatic physics audio detection. |
| **Object pool Spawn returns null** | Check the exact pool ID, assigned prefab, expansion policy and maximum instance count. Check Console warnings for skipped definitions. |
| **Pooled object reappears with old velocity/effects** | Reset state in a component implementing `RuntimeObjectPool.IPoolCallbacks` when it is spawned or returned. |
| **Pooled instance never returns** | Call `Return(instance)` or pass a positive `lifetimeSeconds` to `Spawn`. Timed return uses scaled game time and runs only while the manager is enabled. |

## Contributing

Contributions are welcome, particularly compatibility fixes, improved input handling, physics refinements, sample scenes, and documentation. Open an [issue](https://github.com/kai9987kai/UnityScripts/issues) to discuss changes, or fork the repository and submit a pull request. Keep each component's filename consistent with its class, describe relevant scene setup, and test changes in Unity where possible.

Please follow the repository's [Code of Conduct](CODE_OF_CONDUCT.md). For security concerns, see [SECURITY.md](SECURITY.md).

## License

Distributed under the **MIT License**. See [LICENSE](LICENSE) for the full terms.

Copyright © 2026 Kai Piper.

---

**Created by [Kai Piper](https://github.com/kai9987kai)** · [Repository](https://github.com/kai9987kai/UnityScripts) · [Report an issue](https://github.com/kai9987kai/UnityScripts/issues)
