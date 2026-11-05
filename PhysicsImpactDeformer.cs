using UnityEngine;

/// <summary>
/// PhysicsImpactDeformer - runtime dents, impact damage, and elastic recovery.
///
/// Attach to a GameObject containing a readable MeshFilter and a Collider.
/// Rigidbody collisions deform the visual mesh; optionally recook a MeshCollider
/// so its collision shape also follows the deformation.
///
/// Demo: in Play mode, aim the Main Camera at this object and middle-click.
/// The demo mouse input requires Legacy Input Manager or Active Input Handling: Both.
///
/// Public API:
///     ApplyImpact(worldPosition, inwardWorldDirection, strength);
///     RepairImmediately();
///
/// Works in Unity 2022.3 LTS and Unity 6 without extra packages.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
public sealed class PhysicsImpactDeformer : MonoBehaviour
{
    [Header("Collision Damage")]
    [SerializeField] private bool deformOnCollisions = true;
    [SerializeField, Min(0f)] private float minimumImpactSpeed = 2f;
    [Tooltip("Dent depth in world units per unit of speed above the threshold.")]
    [SerializeField, Min(0f)] private float collisionStrengthMultiplier = 0.035f;
    [SerializeField, Min(0.001f)] private float maximumImpactStrength = 0.5f;
    [SerializeField, Range(1, 8)] private int contactsPerCollision = 3;

    [Header("Deformation")]
    [Tooltip("World-space radius influenced by each impact.")]
    [SerializeField, Min(0.01f)] private float impactRadius = 0.7f;
    [Tooltip("Maximum distance that any vertex may move from its original position, in world units.")]
    [SerializeField, Min(0.001f)] private float maximumVertexDisplacement = 0.5f;
    [Tooltip("Use a smooth, feathered falloff rather than a sharp-edged dent.")]
    [SerializeField] private bool smoothFalloff = true;

    [Header("Recovery")]
    [Tooltip("When enabled, dents gradually relax towards the original shape.")]
    [SerializeField] private bool enableRecovery = true;
    [Tooltip("Exponential recovery rate per second. Zero means no automatic recovery.")]
    [SerializeField, Min(0f)] private float recoveryRate = 0.75f;

    [Header("Physics Collider (Optional)")]
    [Tooltip("Recooking MeshColliders is expensive. Leave off for most moving props.")]
    [SerializeField] private bool updateMeshCollider = false;
    [SerializeField, Min(0.02f)] private float colliderRefreshInterval = 0.25f;

    [Header("Testing")]
    [Tooltip("Middle-click the object while looking through a Camera tagged MainCamera.")]
    [SerializeField] private bool enableMiddleClickTest = true;
    [SerializeField, Min(0.001f)] private float testImpactStrength = 0.18f;
    [SerializeField, Min(0.1f)] private float testRayDistance = 100f;
    [SerializeField] private bool showLastImpactGizmo = true;

    private MeshFilter meshFilter;
    private Mesh originalMesh;
    private Mesh runtimeMesh;
    private MeshCollider attachedMeshCollider;
    private Mesh originalColliderMesh;
    private Vector3[] originalVertices;
    private Vector3[] currentVertices;

    private bool colliderUsesRuntimeMesh;
    private bool colliderDirty;
    private bool anyVerticesDeformed;
    private float nextColliderRefreshTime;
    private Vector3 lastImpactPosition;
    private bool hasImpactPosition;

    /// <summary>True after the component has created its private runtime mesh.</summary>
    public bool IsReady { get { return runtimeMesh != null; } }

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        originalMesh = meshFilter.sharedMesh;

        if (originalMesh == null)
        {
            Debug.LogError("PhysicsImpactDeformer requires a MeshFilter with a mesh.", this);
            enabled = false;
            return;
        }

        if (!originalMesh.isReadable)
        {
            Debug.LogError(
                "PhysicsImpactDeformer: The mesh is not readable. In its Model " +
                "Import Settings, enable Read/Write and reimport the asset.", this);
            enabled = false;
            return;
        }

        // Instantiate the mesh so other objects and the source asset are untouched.
        runtimeMesh = Instantiate(originalMesh);
        runtimeMesh.name = originalMesh.name + " (Runtime Deformable)";
        runtimeMesh.MarkDynamic();
        originalVertices = runtimeMesh.vertices;
        currentVertices = (Vector3[])originalVertices.Clone();
        meshFilter.sharedMesh = runtimeMesh;

        attachedMeshCollider = GetComponent<MeshCollider>();
        if (updateMeshCollider && attachedMeshCollider != null)
        {
            if (attachedMeshCollider.sharedMesh == originalMesh)
            {
                originalColliderMesh = attachedMeshCollider.sharedMesh;
                attachedMeshCollider.sharedMesh = runtimeMesh;
                colliderUsesRuntimeMesh = true;
            }
            else
            {
                Debug.LogWarning(
                    "PhysicsImpactDeformer: The MeshCollider uses a different mesh. " +
                    "Its collision shape will not be modified.", this);
            }
        }
    }

    private void Update()
    {
        if (!IsReady)
            return;

#if ENABLE_LEGACY_INPUT_MANAGER
        if (enableMiddleClickTest && Input.GetMouseButtonDown(2))
            TestImpactUnderCursor();
#endif

        if (enableRecovery && recoveryRate > 0f && anyVerticesDeformed)
            RecoverVertices(Time.deltaTime);

        // Throttle expensive physics-mesh recooking, independently of rendering.
        if (colliderDirty && colliderUsesRuntimeMesh &&
            Time.time >= nextColliderRefreshTime)
        {
            attachedMeshCollider.sharedMesh = null;
            attachedMeshCollider.sharedMesh = runtimeMesh;
            nextColliderRefreshTime = Time.time + colliderRefreshInterval;
            colliderDirty = false;
        }
    }

    /// <summary>
    /// Dents the mesh around a world-space point in the given world-space direction.
    /// Pass a vector pointing INTO the surface, for example -RaycastHit.normal.
    /// Strength is the peak movement distance in world units.
    /// Returns true when at least one vertex changed.
    /// </summary>
    public bool ApplyImpact(Vector3 worldPoint, Vector3 inwardWorldDirection, float strength)
    {
        if (!IsReady || strength <= 0f || inwardWorldDirection.sqrMagnitude < 0.000001f)
            return false;

        bool changed = DeformVertices(
            worldPoint,
            inwardWorldDirection.normalized,
            Mathf.Min(strength, maximumImpactStrength));

        if (changed)
            UploadMeshChanges();

        return changed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!deformOnCollisions || !IsReady || collision.contactCount == 0)
            return;

        float excessSpeed = collision.relativeVelocity.magnitude - minimumImpactSpeed;
        if (excessSpeed <= 0f)
            return;

        float strength = Mathf.Min(
            excessSpeed * collisionStrengthMultiplier,
            maximumImpactStrength);

        int count = Mathf.Min(collision.contactCount, contactsPerCollision);
        float strengthPerContact = strength / count;
        bool changed = false;

        for (int i = 0; i < count; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            // In a collision, penetration is approximately opposite the
            // contact surface normal. Impact displacement uses world units.
            changed |= DeformVertices(
                contact.point, -contact.normal, strengthPerContact);
        }

        if (changed)
            UploadMeshChanges();
    }

#if ENABLE_LEGACY_INPUT_MANAGER
    private void TestImpactUnderCursor()
    {
        Camera cameraToUse = Camera.main;
        if (cameraToUse == null)
            return;

        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, testRayDistance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        // Require the hit Collider to belong to this object or a child.
        if (hit.collider.transform != transform &&
            !hit.collider.transform.IsChildOf(transform))
            return;

        ApplyImpact(hit.point, -hit.normal, testImpactStrength);
    }
#endif

    private bool DeformVertices(Vector3 worldPoint, Vector3 worldDirection, float strength)
    {
        bool changed = false;
        float radiusSquared = impactRadius * impactRadius;
        Vector3 normalizedDirection = worldDirection.normalized;

        for (int i = 0; i < currentVertices.Length; i++)
        {
            Vector3 worldVertex = transform.TransformPoint(currentVertices[i]);
            float sqrDistance = (worldVertex - worldPoint).sqrMagnitude;
            if (sqrDistance >= radiusSquared)
                continue;

            float proximity = 1f - Mathf.Sqrt(sqrDistance) / impactRadius;
            float weight = smoothFalloff
                ? proximity * proximity * (3f - 2f * proximity)
                : proximity;

            // Convert WORLD displacement to local, correctly handling scaling.
            Vector3 desiredWorldMove = normalizedDirection * (strength * weight);
            Vector3 candidate = currentVertices[i] +
                transform.InverseTransformVector(desiredWorldMove);

            // Clamp total permanent displacement in world space, so repeated
            // impacts cannot push vertices arbitrarily far from the source.
            Vector3 totalWorldDisplacement = transform.TransformVector(
                candidate - originalVertices[i]);
            if (totalWorldDisplacement.sqrMagnitude >
                maximumVertexDisplacement * maximumVertexDisplacement)
            {
                totalWorldDisplacement = Vector3.ClampMagnitude(
                    totalWorldDisplacement, maximumVertexDisplacement);
                candidate = originalVertices[i] +
                    transform.InverseTransformVector(totalWorldDisplacement);
            }

            if ((candidate - currentVertices[i]).sqrMagnitude > 0.0000000001f)
            {
                currentVertices[i] = candidate;
                changed = true;
            }
        }

        if (changed)
        {
            anyVerticesDeformed = true;
            lastImpactPosition = worldPoint;
            hasImpactPosition = true;
        }

        return changed;
    }

    private void RecoverVertices(float deltaTime)
    {
        // Frame-rate independent exponential decay; approaches the original
        // position smoothly instead of moving a fixed number per frame.
        float blend = 1f - Mathf.Exp(-recoveryRate * deltaTime);
        bool changed = false;
        bool stillDeformed = false;

        for (int i = 0; i < currentVertices.Length; i++)
        {
            Vector3 difference = originalVertices[i] - currentVertices[i];
            if (difference.sqrMagnitude < 0.00000001f)
            {
                // Snap small residual errors to avoid updates forever.
                if (difference.sqrMagnitude > 0f)
                {
                    currentVertices[i] = originalVertices[i];
                    changed = true;
                }
                continue;
            }

            currentVertices[i] = Vector3.Lerp(
                currentVertices[i], originalVertices[i], blend);
            changed = true;
            stillDeformed = true;
        }

        anyVerticesDeformed = stillDeformed;
        if (changed)
            UploadMeshChanges();
    }

    private void UploadMeshChanges()
    {
        runtimeMesh.vertices = currentVertices;
        runtimeMesh.RecalculateNormals();
        runtimeMesh.RecalculateBounds();

        if (colliderUsesRuntimeMesh)
            colliderDirty = true;
    }

    /// <summary>Restores the original visual shape and schedules collider refresh.</summary>
    [ContextMenu("Repair Mesh Immediately")]
    public void RepairImmediately()
    {
        if (!IsReady)
            return;

        System.Array.Copy(originalVertices, currentVertices, originalVertices.Length);
        anyVerticesDeformed = false;
        UploadMeshChanges();
    }

    private void OnValidate()
    {
        impactRadius = Mathf.Max(0.01f, impactRadius);
        maximumVertexDisplacement = Mathf.Max(0.001f, maximumVertexDisplacement);
        maximumImpactStrength = Mathf.Max(0.001f, maximumImpactStrength);
        minimumImpactSpeed = Mathf.Max(0f, minimumImpactSpeed);
        collisionStrengthMultiplier = Mathf.Max(0f, collisionStrengthMultiplier);
        recoveryRate = Mathf.Max(0f, recoveryRate);
        colliderRefreshInterval = Mathf.Max(0.02f, colliderRefreshInterval);
        contactsPerCollision = Mathf.Clamp(contactsPerCollision, 1, 8);
        testImpactStrength = Mathf.Max(0.001f, testImpactStrength);
        testRayDistance = Mathf.Max(0.1f, testRayDistance);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showLastImpactGizmo || !hasImpactPosition)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(lastImpactPosition, impactRadius);
    }

    private void OnDestroy()
    {
        if (colliderUsesRuntimeMesh && attachedMeshCollider != null)
            attachedMeshCollider.sharedMesh = originalColliderMesh;

        if (meshFilter != null && meshFilter.sharedMesh == runtimeMesh)
            meshFilter.sharedMesh = originalMesh;

        if (runtimeMesh != null)
            Destroy(runtimeMesh);
    }
}
