using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A reusable, physics-based gravity gun for first-person Unity games.
/// Attach this script to the player's Camera.
///
/// Controls (legacy Input Manager / Active Input Handling: Both):
/// E             Pick up or release an object
/// Mouse wheel   Adjust the held object's distance
/// Hold LMB      Charge a throw
/// Release LMB   Throw the held object
/// Q / R         Rotate the held object left / right
///
/// Works with dynamic Rigidbodies; does not require any other scripts.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class AdvancedGravityGun : MonoBehaviour
{
    [Header("Picking Up Objects")]
    [SerializeField, Min(0.5f)] private float pickupRange = 8f;
    [SerializeField, Min(0.01f)] private float maximumObjectMass = 50f;
    [SerializeField] private LayerMask pickupLayers = ~0;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;

    [Header("Holding Distance")]
    [SerializeField, Min(0.2f)] private float minimumHoldDistance = 1.5f;
    [SerializeField, Min(0.3f)] private float maximumHoldDistance = 6f;
    [SerializeField, Min(0.01f)] private float scrollSensitivity = 2f;
    [SerializeField, Min(1f)] private float releaseIfFartherThan = 14f;

    [Header("Position Physics")]
    [Tooltip("Acceleration per metre of position error.")]
    [SerializeField, Min(0f)] private float positionSpring = 55f;
    [Tooltip("Velocity damping prevents the object oscillating around its target.")]
    [SerializeField, Min(0f)] private float positionDamping = 12f;
    [SerializeField, Min(0.1f)] private float maximumLinearAcceleration = 110f;

    [Header("Rotation Physics")]
    [SerializeField, Min(0f)] private float rotationSpring = 32f;
    [SerializeField, Min(0f)] private float rotationDamping = 9f;
    [SerializeField, Min(0.1f)] private float maximumAngularAcceleration = 90f;
    [SerializeField, Min(0f)] private float rotationDegreesPerSecond = 100f;
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
    [SerializeField] private KeyCode rotateRightKey = KeyCode.R;

    [Header("Charged Throw")]
    [SerializeField, Min(0f)] private float minimumThrowSpeed = 7f;
    [SerializeField, Min(0f)] private float additionalThrowSpeed = 17f;
    [SerializeField, Min(0.1f)] private float fullChargeSeconds = 1.2f;

    [Header("Presentation")]
    [SerializeField] private bool showOnScreenHints = true;
    [SerializeField] private bool drawDebugHoldLine = false;

    private Camera playerCamera;
    private Rigidbody heldBody;
    private float holdDistance;
    private float chargeSeconds;
    private bool chargingThrow;
    private bool previousUseGravity;
    private RigidbodyInterpolation previousInterpolation;
    private Quaternion cameraRelativeRotation;

    private readonly List<ColliderPair> ignoredCollisionPairs =
        new List<ColliderPair>();

    private struct ColliderPair
    {
        public Collider ObjectCollider;
        public Collider PlayerCollider;

        public ColliderPair(Collider objectCollider, Collider playerCollider)
        {
            ObjectCollider = objectCollider;
            PlayerCollider = playerCollider;
        }
    }

    private void Awake()
    {
        playerCamera = GetComponent<Camera>();
        ValidateSettings();
    }

    private void OnValidate()
    {
        ValidateSettings();
    }

    private void ValidateSettings()
    {
        pickupRange = Mathf.Max(0.5f, pickupRange);
        maximumObjectMass = Mathf.Max(0.01f, maximumObjectMass);
        minimumHoldDistance = Mathf.Max(0.2f, minimumHoldDistance);
        maximumHoldDistance = Mathf.Max(minimumHoldDistance, maximumHoldDistance);
        releaseIfFartherThan = Mathf.Max(maximumHoldDistance + 1f, releaseIfFartherThan);
        fullChargeSeconds = Mathf.Max(0.1f, fullChargeSeconds);
        scrollSensitivity = Mathf.Max(0.01f, scrollSensitivity);
        maximumLinearAcceleration = Mathf.Max(0.1f, maximumLinearAcceleration);
        maximumAngularAcceleration = Mathf.Max(0.1f, maximumAngularAcceleration);
    }

    private void Update()
    {
        if (playerCamera == null)
            return;

        // If an external script destroys the object while held, clean up.
        if (heldBody == null && ignoredCollisionPairs.Count > 0)
        {
            RestoreCollisions();
            chargingThrow = false;
            chargeSeconds = 0f;
        }

        if (Input.GetKeyDown(pickupKey))
        {
            if (heldBody == null)
                TryPickUp();
            else
                ReleaseHeldObject();
        }

        if (heldBody == null)
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            holdDistance = Mathf.Clamp(
                holdDistance + scroll * scrollSensitivity,
                minimumHoldDistance,
                maximumHoldDistance);
        }

        float rotationInput = 0f;
        if (Input.GetKey(rotateLeftKey)) rotationInput -= 1f;
        if (Input.GetKey(rotateRightKey)) rotationInput += 1f;

        if (rotationInput != 0f)
        {
            cameraRelativeRotation = Quaternion.AngleAxis(
                rotationInput * rotationDegreesPerSecond * Time.deltaTime,
                Vector3.up) * cameraRelativeRotation;
        }

        if (Input.GetMouseButtonDown(0))
        {
            chargingThrow = true;
            chargeSeconds = 0f;
        }

        if (chargingThrow && Input.GetMouseButton(0))
            chargeSeconds = Mathf.Min(fullChargeSeconds, chargeSeconds + Time.deltaTime);

        if (chargingThrow && Input.GetMouseButtonUp(0))
            ThrowHeldObject();
    }

    private void FixedUpdate()
    {
        if (heldBody == null || playerCamera == null)
            return;

        if (!heldBody.gameObject.activeInHierarchy || heldBody.isKinematic)
        {
            ReleaseHeldObject();
            return;
        }

        Vector3 cameraPosition = playerCamera.transform.position;
        if (Vector3.Distance(heldBody.position, cameraPosition) > releaseIfFartherThan)
        {
            ReleaseHeldObject();
            return;
        }

        Vector3 targetPosition = cameraPosition +
            playerCamera.transform.forward * holdDistance;

        // PD control: acceleration = spring * positionError - damping * velocity.
        // Acceleration mode treats light and heavy objects consistently.
        Vector3 acceleration =
            (targetPosition - heldBody.position) * positionSpring -
            GetBodyVelocity(heldBody) * positionDamping;

        acceleration = Vector3.ClampMagnitude(
            acceleration, maximumLinearAcceleration);

        heldBody.AddForce(acceleration, ForceMode.Acceleration);

        // A quaternion error provides smooth orientation control without
        // directly writing transform.rotation and bypassing physics.
        Quaternion targetRotation = playerCamera.transform.rotation *
            cameraRelativeRotation;
        Quaternion delta = targetRotation * Quaternion.Inverse(heldBody.rotation);
        delta.ToAngleAxis(out float angleDegrees, out Vector3 rotationAxis);

        if (angleDegrees > 180f)
            angleDegrees -= 360f;

        Vector3 angularAcceleration = -heldBody.angularVelocity * rotationDamping;
        if (rotationAxis.sqrMagnitude > 0.000001f &&
            Mathf.Abs(angleDegrees) > 0.01f)
        {
            angularAcceleration += rotationAxis.normalized *
                (angleDegrees * Mathf.Deg2Rad * rotationSpring);
        }

        heldBody.AddTorque(
            Vector3.ClampMagnitude(angularAcceleration,
                maximumAngularAcceleration),
            ForceMode.Acceleration);

        if (drawDebugHoldLine)
            Debug.DrawLine(cameraPosition, targetPosition, Color.cyan, Time.fixedDeltaTime);
    }

    private void TryPickUp()
    {
        // Only the nearest hit can be selected; walls block objects behind them.
        if (!Physics.Raycast(playerCamera.transform.position,
                playerCamera.transform.forward,
                out RaycastHit hit,
                pickupRange,
                pickupLayers,
                QueryTriggerInteraction.Ignore))
            return;

        Rigidbody candidate = hit.rigidbody;
        if (candidate == null || candidate.isKinematic ||
            candidate.mass > maximumObjectMass ||
            playerCamera.transform.IsChildOf(candidate.transform))
            return;

        heldBody = candidate;
        previousUseGravity = candidate.useGravity;
        previousInterpolation = candidate.interpolation;

        candidate.useGravity = false;
        candidate.interpolation = RigidbodyInterpolation.Interpolate;
        candidate.WakeUp();

        holdDistance = Mathf.Clamp(hit.distance,
            minimumHoldDistance, maximumHoldDistance);
        cameraRelativeRotation = Quaternion.Inverse(playerCamera.transform.rotation) *
            candidate.rotation;

        chargingThrow = false;
        chargeSeconds = 0f;
        IgnorePlayerCollisions(candidate);
    }

    private void IgnorePlayerCollisions(Rigidbody body)
    {
        // GetComponentsInParent includes a CharacterController if present.
        Collider[] playerColliders = playerCamera.GetComponentsInParent<Collider>();
        Collider[] objectColliders = body.GetComponentsInChildren<Collider>();

        foreach (Collider objectCollider in objectColliders)
        {
            if (objectCollider == null || !objectCollider.enabled)
                continue;

            foreach (Collider playerCollider in playerColliders)
            {
                if (playerCollider == null || !playerCollider.enabled ||
                    objectCollider == playerCollider ||
                    Physics.GetIgnoreCollision(objectCollider, playerCollider))
                    continue;

                Physics.IgnoreCollision(objectCollider, playerCollider, true);
                ignoredCollisionPairs.Add(new ColliderPair(
                    objectCollider, playerCollider));
            }
        }
    }

    private void RestoreCollisions()
    {
        // Only restore pairs that this component itself changed.
        foreach (ColliderPair pair in ignoredCollisionPairs)
        {
            if (pair.ObjectCollider != null && pair.PlayerCollider != null &&
                pair.ObjectCollider.enabled && pair.PlayerCollider.enabled &&
                pair.ObjectCollider.gameObject.activeInHierarchy &&
                pair.PlayerCollider.gameObject.activeInHierarchy)
            {
                Physics.IgnoreCollision(
                    pair.ObjectCollider, pair.PlayerCollider, false);
            }
        }
        ignoredCollisionPairs.Clear();
    }

    private Rigidbody ReleaseHeldObject()
    {
        Rigidbody released = heldBody;

        if (released != null)
        {
            released.useGravity = previousUseGravity;
            released.interpolation = previousInterpolation;
        }

        heldBody = null;
        chargingThrow = false;
        chargeSeconds = 0f;
        RestoreCollisions();
        return released;
    }

    private void ThrowHeldObject()
    {
        float charge = Mathf.Clamp01(chargeSeconds / fullChargeSeconds);
        float speed = minimumThrowSpeed + additionalThrowSpeed * charge;
        Rigidbody released = ReleaseHeldObject();

        if (released != null)
        {
            // VelocityChange uses throw speed instead of varying with mass.
            released.AddForce(
                playerCamera.transform.forward * speed,
                ForceMode.VelocityChange);
        }
    }

    private void OnDisable()
    {
        ReleaseHeldObject();
    }

    private void OnGUI()
    {
        if (!showOnScreenHints || !Application.isPlaying)
            return;

        float centreX = Screen.width * 0.5f;
        float centreY = Screen.height * 0.5f;
        GUI.Label(new Rect(centreX - 5f, centreY - 10f, 24f, 24f), "+");

        if (heldBody == null)
        {
            GUI.Box(new Rect(14f, 14f, 290f, 38f),
                "GRAVITY GUN  |  E: pick up");
        }
        else
        {
            float charge = chargingThrow ?
                Mathf.Clamp01(chargeSeconds / fullChargeSeconds) : 0f;

            GUI.Box(new Rect(14f, 14f, 360f, 86f),
                "GRAVITY GUN  |  " + heldBody.name +
                "\nE: drop   Wheel: distance   Q/R: rotate" +
                "\nHold / release Left Mouse: throw");

            GUI.Box(new Rect(25f, 104f, 220f, 18f),
                "Throw charge: " + Mathf.RoundToInt(charge * 100f) + "%");
        }
    }

    private static Vector3 GetBodyVelocity(Rigidbody body)
    {
#if UNITY_6000_0_OR_NEWER
        return body.linearVelocity;
#else
        return body.velocity;
#endif
    }
}
