# UnityScripts

**A collection of reusable Unity C# gameplay scripts, physics experiments, and projectile-game systems by [Kai Piper](https://github.com/kai9987kai).**

[![Unity](https://img.shields.io/badge/Engine-Unity-222222?logo=unity)](https://unity.com/)
[![C%23](https://img.shields.io/badge/Language-C%23-512BD4?logo=sharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![GitHub repository](https://img.shields.io/badge/Source-GitHub-181717?logo=github)](https://github.com/kai9987kai/UnityScripts)

> **Project status:** A library of individual scripts and work-in-progress examples, **not** a complete, plug-and-play Unity project. Some scripts need integration fixes; read [Known issues](#known-issues-and-limitations) before importing the entire repository.

## Overview

UnityScripts contains example components for first-person movement, Rigidbody interactions, ball movement, trigger-based teleportation, and a more substantial physics-based projectile-target game. The scripts are designed as learning resources and starting points for Unity prototypes, coursework experiments, and small games.

**What's included**

- **First-person movement:** walking, sprinting, mouse look, gravity, and jumping via a `CharacterController`.
- **Gravity gun:** raycast pickup, physics-based holding, distance adjustment, rotation, and charged throws.
- **Rigidbody ball movement:** simple WASD force controls and a Shift modifier.
- **Portal-style teleporting:** a trigger-based ball teleportation experiment.
- **Projectile game:** two alternative launcher controllers, charge-to-fire aiming, trajectory visualization, target/obstacle spawning, a shot counter, and win/lose state management.

## Repository contents

```text
UnityScripts/
├── AdvancedGravityGun.cs
├── Balltoredportal.cs
├── PlayerController.cs
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
| [`PlayerController.cs`](PlayerController.cs) | `SimpleFPSController` | CharacterController-based first-person movement, sprinting, jumping, and mouse look. |
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
- The **legacy Input Manager** enabled, or **Active Input Handling** set to **Both** (`Edit > Project Settings > Player > Other Settings`) because the scripts use `UnityEngine.Input` (`GetAxis`, `GetKey`, `GetMouseButton`, etc.). Input System-only projects need adaptation.
- **TextMeshPro** for `projectileballgame/GameManager.cs` and `LauncherController.cs` (`using TMPro;`). Install it through Package Manager if necessary and import its essential resources when prompted.
- An appropriately configured Unity scene: cameras, collision geometry, components, prefabs, Inspector references, and tags are **not** supplied in this repository.

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
2. **Five script filenames do not match their MonoBehaviour class names.** Use the rename table in [Installation](#installation) before attaching those scripts in Unity.
3. **The two projectile launchers use incompatible spawning assumptions.** `LauncherController` acts on a ball pre-created by `GameManager`, while `AimController_EvenMore_Human` instantiates a new ball. Use one launcher approach and adapt GameManager's ball lifecycle accordingly.
4. **`TeleportBall` references the wrong Rigidbody for typical portal setups.** It should operate on the incoming ball's Rigidbody; otherwise, runtime errors may occur.
5. **Trajectory prediction is approximate.** `TrajectoryLine` uses a simple ballistic formula and currently assumes launch force corresponds to initial velocity; mass, collisions, aerodynamic drag, and other forces are not simulated in the preview.
6. **Ball movement applies physics forces in `Update` rather than `FixedUpdate`.** Movement therefore depends on the rendering loop, and the Shift behaviour does not match a conventional sprint modifier.
7. **Ball lifetime starts as soon as the ball prefab is created.** The GameManager's waiting/kinematic ball can time out before being fired. The controller should ideally start its lifetime when launched.
8. **End-screen keyboard handling is limited.** `GameManager.Update()` returns early when the game is over, so its `R` restart and `Escape` quit shortcuts are inactive at that stage. Connect end-screen UI Buttons to `MyAction_RestartLevel()` / `MyAction_QuitGame()` or adjust the input ordering.
9. **This repository does not contain a Unity project, test suite, sample scene, or package manifest.** Functionality depends on configuring your own scene; compatibility and behaviour have not been verified end-to-end.

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

## Contributing

Contributions are welcome, particularly compatibility fixes, improved input handling, physics refinements, sample scenes, and documentation. Open an [issue](https://github.com/kai9987kai/UnityScripts/issues) to discuss changes, or fork the repository and submit a pull request. Keep each component's filename consistent with its class, describe relevant scene setup, and test changes in Unity where possible.

Please follow the repository's [Code of Conduct](CODE_OF_CONDUCT.md). For security concerns, see [SECURITY.md](SECURITY.md).

## License

Distributed under the **MIT License**. See [LICENSE](LICENSE) for the full terms.

Copyright © 2026 Kai Piper.

---

**Created by [Kai Piper](https://github.com/kai9987kai)** · [Repository](https://github.com/kai9987kai/UnityScripts) · [Report an issue](https://github.com/kai9987kai/UnityScripts/issues)
