// This is a different kind of launcher script.
// Date: [Current Date, e.g., May 8, 2025]
using UnityEngine;
using TMPro;
using System.Collections;

public class LauncherController : MonoBehaviour
{
    [Header("Important Object Links")]
    public Transform actual_Launch_Point_Transform;
    public TextMeshProUGUI ui_Elevation_Display_Text;
    public TextMeshProUGUI ui_Angle_Display_Text;
    public TextMeshProUGUI ui_Power_Display_Text;

    [Header("Launcher Control Settings")]
    public float minimum_Possible_Power = 0f;
    public float maximum_Possible_Power = 10f;
    public float power_Change_Sensitivity = 5f;
    public float minimum_Elevation_Angle = 0f;
    public float maximum_Elevation_Angle = 89f;
    public float elevation_Change_Sensitivity = 30f;
    public float horizontal_Rotation_Sensitivity = 50f;
    public float final_Launch_Force_Multiplier = 20f;

    private float current_Internal_Power_Value;
    private float current_Launcher_Elevation_Angle;
    private float current_Launcher_Horizontal_Angle_Yaw;
    private bool can_Actually_Fire_Shot_Now = false;

    void Start()
    {
        current_Internal_Power_Value = minimum_Possible_Power;
        current_Launcher_Elevation_Angle = 0f;
        current_Launcher_Horizontal_Angle_Yaw = transform.eulerAngles.y;

        ApplyLauncherRotationSettings();
        RefreshLauncherUI();
        StartCoroutine(Routine_EnableFiring_After_Short_Delay(0.2f));
    }

    IEnumerator Routine_EnableFiring_After_Short_Delay(float delay_In_Seconds)
    {
        yield return new WaitForSeconds(delay_In_Seconds);
        can_Actually_Fire_Shot_Now = true;
    }

    void Update()
    {
        if (GameManager.game_Instance == null) return;

        // Use the NEW GameManager method name
        if (GameManager.game_Instance.Query_IsPlayerAllowedToFireAShot() && can_Actually_Fire_Shot_Now)
        {
            ProcessPlayerInputForLauncher();
            ApplyLauncherRotationSettings();
            RefreshLauncherUI();

            if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space))
            {
                Action_FireTheBall();
            }
        }
        // Optional: Update UI even if cannot fire, to show current aiming state
        // else if (!GameManager.game_Instance.Query_IsPlayerAllowedToFireAShot()) {
        //    ProcessPlayerInputForLauncher(); // Allow aiming
        //    ApplyLauncherRotationSettings(); // Apply visual rotation
        //    RefreshLauncherUI(); // Update UI text
        // }
    }

    void ProcessPlayerInputForLauncher()
    {
        current_Internal_Power_Value += Input.GetAxis("Vertical") * power_Change_Sensitivity * Time.deltaTime;
        current_Internal_Power_Value = Mathf.Clamp(current_Internal_Power_Value, minimum_Possible_Power, maximum_Possible_Power);

        current_Launcher_Horizontal_Angle_Yaw += Input.GetAxis("Horizontal") * horizontal_Rotation_Sensitivity * Time.deltaTime;

        float pitch_Input_Value = 0f;
        if (Input.GetKey(KeyCode.Q)) pitch_Input_Value = 1f;
        else if (Input.GetKey(KeyCode.E)) pitch_Input_Value = -1f; // else if makes Q/E mutually exclusive for this frame
        current_Launcher_Elevation_Angle += pitch_Input_Value * elevation_Change_Sensitivity * Time.deltaTime;
        current_Launcher_Elevation_Angle = Mathf.Clamp(current_Launcher_Elevation_Angle, minimum_Elevation_Angle, maximum_Elevation_Angle);
    }

    void ApplyLauncherRotationSettings()
    {
        transform.rotation = Quaternion.Euler(0f, current_Launcher_Horizontal_Angle_Yaw, 0f);
        if (actual_Launch_Point_Transform != null)
        {
            actual_Launch_Point_Transform.localRotation = Quaternion.Euler(current_Launcher_Elevation_Angle, 0f, 0f);
        }
    }

    void RefreshLauncherUI()
    {
        if (ui_Elevation_Display_Text != null) ui_Elevation_Display_Text.text = $"Elev: {current_Launcher_Elevation_Angle:F1}°";
        if (ui_Angle_Display_Text != null) ui_Angle_Display_Text.text = $"Yaw: {Mathf.Repeat(current_Launcher_Horizontal_Angle_Yaw, 360):F1}°";
        if (ui_Power_Display_Text != null) ui_Power_Display_Text.text = $"Power: {current_Internal_Power_Value:F1}";
    }

    void Action_FireTheBall()
    {
        // Use the NEW GameManager method name
        if (GameManager.game_Instance == null || !GameManager.game_Instance.Query_IsPlayerAllowedToFireAShot()) return;

        GameObject ball_Object_To_Launch = GameObject.FindGameObjectWithTag("Ball");
        if (ball_Object_To_Launch == null)
        {
            Debug.LogError("LAUNCHER FIRE FAILED: Could not find any GameObject with the tag 'Ball'!");
            return;
        }

        if (ball_Object_To_Launch.TryGetComponent<Rigidbody>(out Rigidbody ball_Rigidbody_Component))
        {
            ball_Rigidbody_Component.isKinematic = false;
            ball_Rigidbody_Component.useGravity = true;
            ball_Object_To_Launch.transform.rotation = actual_Launch_Point_Transform.rotation;
            ball_Rigidbody_Component.AddForce(actual_Launch_Point_Transform.forward * current_Internal_Power_Value * final_Launch_Force_Multiplier, ForceMode.Impulse);

            // Use the NEW GameManager method name
            GameManager.game_Instance.Event_AShotWasJustFiredByPlayer();
        }
        else
        {
            Debug.LogError($"LAUNCHER FIRE FAILED: Ball '{ball_Object_To_Launch.name}' missing Rigidbody!", ball_Object_To_Launch);
            Destroy(ball_Object_To_Launch);
            if (GameManager.game_Instance != null)
            {
                GameManager.game_Instance.BallDied("Launch Error - No Rigidbody on found ball");
            }
        }
    }
}