// This script is for drawing the line that shows where the ball might go.
// It needs a LineRenderer component on the same object.
// Date: [Current Date, e.g., May 8, 2025]
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryLine : MonoBehaviour
{
    [Tooltip("You need to drag your AimController script (the one on your launcher) in here. VERY IMPORTANT!")]
    public AimController_EvenMore_Human theAimController; // My AimController buddy

    [Header("Line Curve Look Settings")]
    [Tooltip("How many little points or segments to use for drawing the line. More points = smoother line. Less points = better performance maybe?")]
    public int line_Resolution = 30;
    [Tooltip("How much game time (in seconds) between each point on the line. Affects how 'long' the trajectory prediction is.")]
    public float time_Step_Between_Points = 0.1f;

    private LineRenderer myLineRendererComponent; // This is the LineRenderer I'm controlling.
    private Vector3 current_Gravity_Value; // To store the game's gravity, so I don't ask Physics all the time.

    // Awake is called by Unity when this script is first loading up.
    void Awake()
    {
        Debug.Log($"TrajectoryLine '{gameObject.name}': Awake is running!", this);
        myLineRendererComponent = GetComponent<LineRenderer>();
        current_Gravity_Value = Physics.gravity;

        if (myLineRendererComponent == null)
        {
            Debug.LogError($"TRAJECTORY LINE AWAKE ERROR: Oh no! I ({gameObject.name}) am MISSING a LineRenderer component! I can't draw anything without it. Please add a LineRenderer to me in the Inspector.", this);
            enabled = false; // Disable this script if no LineRenderer, it's useless.
            return;
        }
        else
        {
            Debug.Log($"TrajectoryLine '{gameObject.name}': Found my LineRenderer component! Setting its positionCount to {line_Resolution} and useWorldSpace to true.", this);
            myLineRendererComponent.positionCount = line_Resolution;
            myLineRendererComponent.useWorldSpace = true; // So the line draws in world space, not just relative to me.
            myLineRendererComponent.enabled = false; // Start with the line hidden.
        }

        if (theAimController == null)
        {
            Debug.LogError($"TRAJECTORY LINE AWAKE ERROR: My 'theAimController' variable is NOT SET (it's NULL) in the Inspector for '{gameObject.name}'! I need this to know where to draw the line from. Please drag your AimController object here.", this);
            // Don't disable the script here, it might get assigned later, but Update will check.
        }
        else
        {
            Debug.Log($"TrajectoryLine '{gameObject.name}': My 'theAimController' is linked to '{theAimController.gameObject.name}'. Good!", this);
        }
        Debug.Log($"TrajectoryLine '{gameObject.name}': Awake finished. Gravity is {current_Gravity_Value}.", this);
    }

    // Update is called by Unity every single frame. This is where I'll draw the line if needed.
    void Update()
    {
        // First, the most important checks!
        if (myLineRendererComponent == null)
        {
            // This should have been caught in Awake, but just in case.
            // Debug.LogError("TrajectoryLine Update: My LineRenderer is null! Can't do anything.", this);
            return; // Stop if no line renderer.
        }

        if (theAimController == null)
        {
            // Debug.LogWarning("TrajectoryLine Update: My 'theAimController' is still null. I can't draw the line. Hiding it.", this);
            myLineRendererComponent.enabled = false;
            return; // Stop if no aim controller.
        }

        // Now, ask the AimController if the player is currently aiming.
        bool isPlayerAimingRightNow = theAimController.player_Is_Currently_Aiming;

        if (isPlayerAimingRightNow)
        {
            // The player IS aiming! So, I should try to draw the line.
            // First, make sure the AimController has its launch point set up!
            if (theAimController.launch_Point_Transform == null)
            {
                Debug.LogError($"TRAJECTORY LINE ERROR: Player is aiming, but 'theAimController.launch_Point_Transform' is NULL! I don't know where the line should start from. Hiding line.", this);
                myLineRendererComponent.enabled = false;
                return;
            }

            // If we got here, we should be good to draw! Make the line visible.
            if (!myLineRendererComponent.enabled)
            {
                myLineRendererComponent.enabled = true;
                Debug.Log("TrajectoryLine: Player is AIMING, enabling LineRenderer.", this);
            }

            // Get all the info I need from the AimController.
            Vector3 start_Position_For_Line = theAimController.launch_Point_Transform.position;
            Vector3 aim_Direction_From_Controller = theAimController.current_Calculated_AimDirection;
            float current_Shot_Speed_From_Controller = theAimController.power_Of_Current_Shot;

            // A little check for silly values that would break the math.
            if (current_Shot_Speed_From_Controller <= 0.01f)
            {
                // If power is basically zero, trajectory is just a dot or falls straight down,
                // might as well hide it or handle it specially. For now, let's just log and draw it.
                // Debug.LogWarning($"TrajectoryLine: Shot speed is very low ({current_Shot_Speed_From_Controller}). Trajectory might look weird.", this);
            }
            if (time_Step_Between_Points <= 0f)
            {
                Debug.LogError("TrajectoryLine: 'time_Step_Between_Points' is zero or negative! This will break the calculation. Setting to a small default (0.05) for this frame to avoid errors.", this);
                time_Step_Between_Points = 0.05f; // Prevent division by zero or infinite loops if logic depended on it.
            }


            // Make sure my LineRenderer still has the right number of points.
            if (myLineRendererComponent.positionCount != line_Resolution)
            {
                // This should usually only happen if line_Resolution changes while the game is running.
                Debug.LogWarning($"TrajectoryLine: My line_Resolution ({line_Resolution}) doesn't match LineRenderer's positionCount ({myLineRendererComponent.positionCount}). Fixing it.", this);
                myLineRendererComponent.positionCount = line_Resolution;
            }

            // Now, loop through each point of my line and calculate its position.
            // Debug.Log($"Trajectory drawing: StartPos={start_Position_For_Line}, Dir={aim_Direction_From_Controller}, Speed={current_Shot_Speed_From_Controller}, TimeStep={time_Step_Between_Points}, Gravity={current_Gravity_Value.y}", this);
            for (int i = 0; i < line_Resolution; i++)
            {
                float time_At_This_Point = (float)i * time_Step_Between_Points; // 't' in the physics formula.

                // The physics formula for where a projectile will be:
                // position = initial_position + (initial_velocity * time) + (0.5 * gravity * time * time)
                // Here, initial_velocity is (aim_Direction_From_Controller * current_Shot_Speed_From_Controller).
                Vector3 part1_initial_velocity_effect = aim_Direction_From_Controller * current_Shot_Speed_From_Controller * time_At_This_Point;
                Vector3 part2_gravity_effect = 0.5f * current_Gravity_Value * time_At_This_Point * time_At_This_Point;
                Vector3 calculated_Point_Position_In_World = start_Position_For_Line + part1_initial_velocity_effect + part2_gravity_effect;

                // Sanity check for NaN or Infinity, which can happen with bad math/inputs
                if (float.IsNaN(calculated_Point_Position_In_World.x) || float.IsInfinity(calculated_Point_Position_In_World.x))
                {
                    Debug.LogError($"TrajectoryLine: Calculated point {i} is NaN or Infinity! Inputs: start={start_Position_For_Line}, dir={aim_Direction_From_Controller}, speed={current_Shot_Speed_From_Controller}, t={time_At_This_Point}. Using start position as fallback for this point.", this);
                    myLineRendererComponent.SetPosition(i, start_Position_For_Line); // Fallback to avoid breaking LineRenderer
                    continue; // Skip to next point
                }

                myLineRendererComponent.SetPosition(i, calculated_Point_Position_In_World);
                // If you want to see EVERY point being calculated (VERY SPAMMY):
                // Debug.Log($"Trajectory point {i}: t={time_At_This_Point:F2}, pos={calculated_Point_Position_In_World}", this);
            }
        }
        else
        {
            // If the player is NOT aiming, make sure my line is hidden.
            if (myLineRendererComponent.enabled)
            {
                myLineRendererComponent.enabled = false;
                // Debug.Log("TrajectoryLine: Player is NOT aiming, disabling LineRenderer.", this);
            }
        }
    }
}