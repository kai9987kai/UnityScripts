// Aim Controller script - REVAMPED! Trying to make aiming and trajectory work perfectly together.
// My goal: Click and HOLD to aim (show trajectory), release to FIRE.
// Also trying to make it look like a human (me!) wrote it.
// Date: [Current Date, e.g., May 8, 2025] - Updated by [Your Name/Alias]
using UnityEngine;

// This script should be the ONLY launcher controller active on the GameObject.
// Make sure LauncherController script is disabled or removed!
public class AimController_EvenMore_Human : MonoBehaviour
{
    // --- Things I need to link in the Inspector ---
    [Header("Where Things Happen")]
    [Tooltip("This is the empty GameObject where the ball actually starts. Its forward direction IS the aiming direction! MUST BE SET!")]
    public Transform launch_Point_Transform;
    [Tooltip("The Ball Prefab from my Project Assets folder. It really needs a Rigidbody! MUST BE SET!")]
    public GameObject ball_Prefab_To_Make;

    [Header("How Aiming Feels")]
    [Tooltip("Smallest force if I just tap.")]
    public float g_MinimumLaunchForce = 5f;
    [Tooltip("Biggest force if I hold down for a while.")]
    public float g_MaximumLaunchForce = 20f;
    [Tooltip("How fast the power bar (force) fills up when holding (oomph per second).")]
    public float force_ChargeSpeed_PerSec = 5f;
    [Tooltip("How fast it rotates with A/D/W/S keys (degrees/sec).")]
    public float keyboard_RotationDegreesPerSec = 90f;

    [Header("Sound Effects")]
    [Tooltip("Sound to play when firing.")]
    public AudioClip sfx_FireClip;
    [Tooltip("The AudioSource component that plays the sound.")]
    public AudioSource audio_Source_Component;
    [Tooltip("Make the pitch slightly random? (Sounds less boring).")]
    public bool should_Randomize_Pitch = true;

    // --- My Internal State Variables ---
    // These tell me (and other scripts like TrajectoryLine) what's going on.
    [HideInInspector] public bool player_Is_Currently_Aiming = false; // TrajectoryLine uses this! Is the player HOLDING the button?
    [HideInInspector] public float power_Of_Current_Shot = 0f; // How much force have I charged up?
    [HideInInspector] public Vector3 current_Calculated_AimDirection = Vector3.forward; // Which way am I aiming right now?

    // This little helper flag stops the ball firing instantly if the player clicks super fast.
    private bool just_Started_Aiming_This_Frame = false;

    // Awake runs once when the script loads. Good for checks.
    void Awake()
    {
        Debug.Log($"AimController '{gameObject.name}': Awake()... setting initial power.", this);
        power_Of_Current_Shot = g_MinimumLaunchForce; // Start at minimum power.

        // Check my important links!
        if (launch_Point_Transform == null)
            Debug.LogError($"AIM CONTROLLER AWAKE PROBLEM for '{gameObject.name}': 'launch_Point_Transform' is NOT ASSIGNED in the Inspector! Aiming/Firing/Trajectory will be broken!", this);
        if (ball_Prefab_To_Make == null)
            Debug.LogError($"AIM CONTROLLER AWAKE PROBLEM for '{gameObject.name}': 'ball_Prefab_To_Make' is NOT ASSIGNED in the Inspector! Cannot shoot anything!", this);
    }

    // Update runs every frame. This is the main loop.
    void Update()
    {
        // Reset this timing flag each frame.
        just_Started_Aiming_This_Frame = false;

        // Do these jobs every frame:
        MyJob_Handle_Aiming_Rotation(); // Update where the launcher is pointing.
        MyJob_Handle_Firing_Input();    // Check if player pressed/released fire button.
        MyJob_Handle_Power_Charging();  // Increase power if aiming.
        MyJob_Update_Aim_Direction_Variable(); // Keep the public direction variable fresh.

        // Optional Debug Log (uncomment to see status frequently)
        // if (Time.frameCount % 60 == 0) // About once per second
        //    Debug.Log($"AimController Status: Aiming={player_Is_Currently_Aiming}, Power={power_Of_Current_Shot:F1}, Dir={current_Calculated_AimDirection}", this);
    }

    // My job for handling how the player rotates the launcher.
    private void MyJob_Handle_Aiming_Rotation()
    {
        // --- Mouse Aiming (Hold Right Mouse Button) ---
        if (Input.GetMouseButton(1))
        {
            Camera mainCam = Camera.main; // Find the main camera
            if (mainCam != null)
            {
                Ray camRay = mainCam.ScreenPointToRay(Input.mousePosition);
                int groundMask = LayerMask.GetMask("Ground"); // Only hit ground layer
                if (Physics.Raycast(camRay, out RaycastHit hitInfo, 100f, groundMask))
                {
                    // Look at the hit point on the ground, but keep my own height
                    Vector3 lookTarget = new Vector3(hitInfo.point.x, transform.position.y, hitInfo.point.z);
                    transform.LookAt(lookTarget);
                }
            }
            // else { Debug.LogWarning("Mouse aiming needs a Camera tagged 'MainCamera'."); } // Log only if needed
        }

        // --- Keyboard Rotation (A/D for Yaw, W/S for Pitch) ---
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = -Input.GetAxis("Vertical"); // Negative so W aims up

        if (Mathf.Abs(horizontalInput) > 0.01f || Mathf.Abs(verticalInput) > 0.01f)
        {
            float pitch = verticalInput * keyboard_RotationDegreesPerSec * Time.deltaTime;
            float yaw = horizontalInput * keyboard_RotationDegreesPerSec * Time.deltaTime;
            transform.Rotate(pitch, yaw, 0f, Space.Self); // Rotate relative to self
        }
    }

    // My job for checking the fire button (LMB or Space) to start/stop aiming.
    private void MyJob_Handle_Firing_Input()
    {
        bool canFireAccordingToGameManager = true; // Assume yes initially
        if (GameManager.game_Instance != null)
        {
            canFireAccordingToGameManager = GameManager.game_Instance.Query_IsPlayerAllowedToFireAShot();
        }

        // Did player PRESS fire button this frame?
        bool firePressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        // Did player RELEASE fire button this frame?
        bool fireReleased = Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space);

        // --- Logic to START Aiming ---
        if (firePressed && !player_Is_Currently_Aiming)
        {
            // Check if the game rules allow starting a shot right now
            if (!canFireAccordingToGameManager)
            {
                Debug.Log("AimController: Tried START aiming, but GameManager says NO (Query_IsPlayerAllowedToFireAShot is false). Reason: Ball active?=" + GameManager.game_Instance?.is_Ball_Currently_Active_Internal + ", Game Over?=" + GameManager.game_Instance?.is_Game_Over_Internal + ", Shots Left=" + GameManager.game_Instance?.current_Shots_Left_Internal, this);
                return; // Don't start aiming
            }

            // Start aiming!
            player_Is_Currently_Aiming = true;      // Set the important flag!
            just_Started_Aiming_This_Frame = true;  // Set the timing flag!
            power_Of_Current_Shot = g_MinimumLaunchForce; // Reset power
            Debug.Log("AimController: ---> AIMING STARTED (Hold Detected) <--- Flag 'player_Is_Currently_Aiming' = TRUE. Trajectory should appear.", this);
        }
        // --- Logic to STOP Aiming and FIRE ---
        // Only fire if button released AND we were aiming AND it wasn't the *same frame* we started aiming.
        else if (fireReleased && player_Is_Currently_Aiming && !just_Started_Aiming_This_Frame)
        {
            Debug.Log("AimController: ---> FIRING BALL (Release Detected) <--- Calling the launch function.", this);
            MyActualJob_LaunchTheBall(); // Launch the ball!
            player_Is_Currently_Aiming = false; // Stop aiming AFTER the launch attempt.
            Debug.Log("AimController: ---> AIMING STOPPED (Fired) <--- Flag 'player_Is_Currently_Aiming' = FALSE.", this);
        }
        // --- Logic to CANCEL Aiming (if player releases instantly) ---
        // If player released on the SAME frame they pressed...
        else if (fireReleased && player_Is_Currently_Aiming && just_Started_Aiming_This_Frame)
        {
            // They tapped too quickly, don't fire, just cancel aiming.
            player_Is_Currently_Aiming = false;
            Debug.Log("AimController: ---> AIMING CANCELLED (Instant Click/Release) <--- Flag 'player_Is_Currently_Aiming' = FALSE. No ball fired.", this);
        }
    }

    // My job to increase power if the player is holding the aim button.
    private void MyJob_Handle_Power_Charging()
    {
        // Only charge if aiming, and NOT on the very first frame they started aiming.
        if (player_Is_Currently_Aiming && !just_Started_Aiming_This_Frame)
        {
            power_Of_Current_Shot += force_ChargeSpeed_PerSec * Time.deltaTime;
            // Clamp the power so it doesn't go over the max!
            power_Of_Current_Shot = Mathf.Min(power_Of_Current_Shot, g_MaximumLaunchForce);
            // Optional Debug:
            // if (Time.frameCount % 15 == 0) Debug.Log($"AimController: Charging... Power = {power_Of_Current_Shot:F1}");
        }
    }

    // My job to keep the public 'current_Calculated_AimDirection' variable updated.
    private void MyJob_Update_Aim_Direction_Variable()
    {
        if (launch_Point_Transform != null)
        {
            current_Calculated_AimDirection = launch_Point_Transform.forward.normalized;
        }
        else
        {
            // Use default, Awake should have warned if null.
            current_Calculated_AimDirection = Vector3.forward;
        }
    }

    // My function that actually makes the ball appear and pushes it!
    private void MyActualJob_LaunchTheBall()
    {
        // Final check: Still allowed to fire according to game rules?
        if (GameManager.game_Instance != null && !GameManager.game_Instance.Query_IsPlayerAllowedToFireAShot())
        {
            // This might happen if the game state changed between the input check and now.
            Debug.LogWarning("AimController Launch Aborted: GameManager says cannot fire right now (maybe ball became active unexpectedly?).", this);
            return;
        }

        // Do I have everything I need?
        if (launch_Point_Transform == null || ball_Prefab_To_Make == null)
        {
            Debug.LogError("AimController Launch Failed: Missing Launch Point or Ball Prefab link in Inspector!", this);
            return;
        }

        Debug.Log($"AimController: LAUNCHING! Power={power_Of_Current_Shot:F2}, Direction={current_Calculated_AimDirection}", this);

        // Make the ball!
        GameObject newBall = Instantiate(ball_Prefab_To_Make, launch_Point_Transform.position, launch_Point_Transform.rotation);

        // Get its physics component (Rigidbody).
        if (newBall.TryGetComponent<Rigidbody>(out Rigidbody ballRb))
        {
            // Make sure physics affects it.
            ballRb.isKinematic = false;
            // Give it the push!
            ballRb.AddForce(current_Calculated_AimDirection * power_Of_Current_Shot, ForceMode.Impulse);

            // Tell the GameManager a shot was successfully fired!
            if (GameManager.game_Instance != null)
            {
                GameManager.game_Instance.Event_AShotWasJustFiredByPlayer();
            }
        }
        else
        {
            // Uh oh, the prefab is missing the physics part!
            Debug.LogError($"AimController Launch PROBLEM: Ball Prefab '{ball_Prefab_To_Make.name}' is missing a Rigidbody Component!", newBall);
        }

        // Play the sound
        if (audio_Source_Component != null && sfx_FireClip != null)
        {
            audio_Source_Component.pitch = should_Randomize_Pitch ? Random.Range(0.95f, 1.05f) : 1f;
            audio_Source_Component.PlayOneShot(sfx_FireClip);
        }
    }
} // End of AimController_EvenMore_Human class - Make sure this is the last line!