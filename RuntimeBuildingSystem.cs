using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A modular runtime building system for Unity 2022.3 LTS and Unity 6.
/// No packages, UI prefabs, or scene setup required for the three example pieces.
///
/// B: toggle build mode    1-9 or mouse wheel: choose piece
/// Q/E: rotate             G: toggle grid snapping
/// Left click: place       Right click: remove a placed piece
/// Ctrl+Z: undo            Ctrl+Y: redo
///
/// Uses Unity's legacy Input API. Enable "Input Manager (Old)" or "Both"
/// under Project Settings > Player > Active Input Handling.
/// </summary>
[DisallowMultipleComponent]
public sealed class RuntimeBuildingSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera buildCamera;
    [Tooltip("Leave empty to create three demonstration pieces automatically.")]
    [SerializeField] private GameObject[] placeablePrefabs;
    [SerializeField] private Transform placedObjectsParent;

    [Header("Building")]
    [SerializeField] private bool beginInBuildMode = true;
    [SerializeField, Min(1f)] private float maxBuildDistance = 35f;
    [SerializeField] private LayerMask placementSurfaces = ~0;
    [SerializeField] private LayerMask obstructionLayers = ~0;
    [SerializeField] private bool allowFallbackGroundPlane = true;
    [SerializeField] private float fallbackGroundY = 0f;
    [SerializeField] private bool checkForObstructions = true;
    [SerializeField, Range(0f, 0.15f)] private float collisionInset = 0.035f;

    [Header("Snapping")]
    [SerializeField] private bool gridSnap = true;
    [SerializeField, Min(0.1f)] private float gridSize = 1f;
    [SerializeField, Range(1f, 180f)] private float rotationStep = 90f;
    [SerializeField] private float verticalOffset = 0f;

    [Header("Presentation")]
    [SerializeField] private bool showInstructions = true;
    [SerializeField] private Color validPreviewColour = new Color(0.15f, 0.95f, 0.45f, 0.8f);
    [SerializeField] private Color blockedPreviewColour = new Color(1f, 0.25f, 0.22f, 0.8f);
    [SerializeField, Range(1, 200)] private int maxUndoActions = 60;

    private sealed class PieceRecord
    {
        public int PrefabIndex;
        public Vector3 Position;
        public Quaternion Rotation;
        public GameObject Instance;
    }

    private struct BuildAction
    {
        public bool WasPlacement;
        public PieceRecord Piece;

        public BuildAction(bool wasPlacement, PieceRecord piece)
        {
            WasPlacement = wasPlacement;
            Piece = piece;
        }
    }

    private readonly Dictionary<GameObject, PieceRecord> placed =
        new Dictionary<GameObject, PieceRecord>();
    private readonly List<BuildAction> history = new List<BuildAction>();
    private readonly List<GameObject> exampleTemplates = new List<GameObject>();

    private int historyPosition;
    private int selectedIndex;
    private float rotationY;
    private bool buildMode;
    private bool canPlace;
    private GameObject preview;
    private Renderer[] previewRenderers;
    private MaterialPropertyBlock colourBlock;
    private Ray currentRay;
    private bool hasRay;
    private Collider surfaceCollider;

    public bool IsBuildMode { get { return buildMode; } }
    public bool CanPlace { get { return canPlace; } }
    public int SelectedPrefabIndex { get { return selectedIndex; } }
    public int PlacedObjectCount { get { return placed.Count; } }

    private void Awake()
    {
        buildCamera = buildCamera != null ? buildCamera : Camera.main;
        if (placeablePrefabs == null || !HasUsablePrefab())
            CreateExamplePrefabs();

        if (placedObjectsParent == null)
        {
            GameObject container = new GameObject("Runtime Placed Objects");
            placedObjectsParent = container.transform;
        }

        colourBlock = new MaterialPropertyBlock();
        buildMode = beginInBuildMode;
        if (buildMode)
            RebuildPreview();
    }

    private void OnValidate()
    {
        maxBuildDistance = Mathf.Max(1f, maxBuildDistance);
        gridSize = Mathf.Max(0.1f, gridSize);
        rotationStep = Mathf.Clamp(rotationStep, 1f, 180f);
        maxUndoActions = Mathf.Clamp(maxUndoActions, 1, 200);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            SetBuildMode(!buildMode);

        if (!buildMode)
            return;

        if (buildCamera == null)
            buildCamera = Camera.main;
        if (buildCamera == null)
            return;

        bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (control && Input.GetKeyDown(KeyCode.Z))
        {
            Undo();
            RefreshPreview();
            return;
        }
        if (control && Input.GetKeyDown(KeyCode.Y))
        {
            Redo();
            RefreshPreview();
            return;
        }

        if (Input.GetKeyDown(KeyCode.G))
            gridSnap = !gridSnap;
        if (Input.GetKeyDown(KeyCode.Q))
            rotationY -= rotationStep;
        if (Input.GetKeyDown(KeyCode.E))
            rotationY += rotationStep;

        int count = placeablePrefabs.Length;
        for (int i = 0; i < Mathf.Min(9, count); i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                SelectPrefab(i);
        }

        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (wheel > 0.001f)
            SelectPrefab((selectedIndex + 1) % count);
        else if (wheel < -0.001f)
            SelectPrefab((selectedIndex + count - 1) % count);

        RefreshPreview();

        if (Input.GetMouseButtonDown(0) && canPlace)
            PlaceSelected();
        if (Input.GetMouseButtonDown(1))
            TryRemoveUnderCursor();
    }

    public void SetBuildMode(bool enabled)
    {
        buildMode = enabled;
        canPlace = false;

        if (enabled && preview == null)
            RebuildPreview();
        if (preview != null)
            preview.SetActive(enabled);
    }

    public void SelectPrefab(int index)
    {
        if (placeablePrefabs == null || placeablePrefabs.Length == 0)
            return;
        index = (index % placeablePrefabs.Length + placeablePrefabs.Length)
                % placeablePrefabs.Length;
        if (placeablePrefabs[index] == null || selectedIndex == index)
            return;

        selectedIndex = index;
        RebuildPreview();
    }

    private bool HasUsablePrefab()
    {
        foreach (GameObject item in placeablePrefabs)
            if (item != null)
                return true;
        return false;
    }

    private void CreateExamplePrefabs()
    {
        GameObject wall = MakeExample("Wall (2 x 2 x 0.25)",
            new Vector3(2f, 2f, 0.25f), new Color(0.65f, 0.73f, 0.84f));
        GameObject floor = MakeExample("Platform (2 x 0.2 x 2)",
            new Vector3(2f, 0.2f, 2f), new Color(0.47f, 0.62f, 0.78f));
        GameObject pillar = MakeExample("Pillar (0.5 x 2 x 0.5)",
            new Vector3(0.5f, 2f, 0.5f), new Color(0.85f, 0.66f, 0.39f));
        placeablePrefabs = new[] { wall, floor, pillar };
    }

    private GameObject MakeExample(string objectName, Vector3 scale, Color colour)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = objectName + " [template]";
        item.transform.SetParent(transform, false);
        item.transform.localScale = scale;
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null && renderer.material.HasProperty("_Color"))
            renderer.material.color = colour;
        item.SetActive(false);
        exampleTemplates.Add(item);
        return item;
    }

    private void RebuildPreview()
    {
        if (preview != null)
            Destroy(preview);
        preview = null;
        previewRenderers = null;
        canPlace = false;

        if (placeablePrefabs == null || placeablePrefabs.Length == 0)
            return;
        if (placeablePrefabs[selectedIndex] == null)
            return;

        preview = Instantiate(placeablePrefabs[selectedIndex]);
        preview.name = "BUILD PREVIEW (not placed)";

        // A preview must not collide, be raycast-targetable, or run game logic.
        foreach (Transform child in preview.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = 2; // Unity's built-in Ignore Raycast layer.
        foreach (Collider collider in preview.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (Rigidbody body in preview.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.detectCollisions = false;
        }
        foreach (MonoBehaviour script in preview.GetComponentsInChildren<MonoBehaviour>(true))
            script.enabled = false;
        foreach (AudioSource audio in preview.GetComponentsInChildren<AudioSource>(true))
            audio.enabled = false;
        foreach (ParticleSystem particle in preview.GetComponentsInChildren<ParticleSystem>(true))
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        previewRenderers = preview.GetComponentsInChildren<Renderer>(true);
        preview.SetActive(false);
    }

    private void RefreshPreview()
    {
        canPlace = false;
        if (preview == null || buildCamera == null)
            return;

        currentRay = Cursor.lockState == CursorLockMode.Locked
            ? buildCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : buildCamera.ScreenPointToRay(Input.mousePosition);
        hasRay = true;

        Vector3 location;
        surfaceCollider = null;

        if (Physics.Raycast(currentRay, out RaycastHit hit,
                maxBuildDistance, placementSurfaces, QueryTriggerInteraction.Ignore))
        {
            location = hit.point;
            surfaceCollider = hit.collider;
        }
        else if (allowFallbackGroundPlane)
        {
            Plane floor = new Plane(Vector3.up,
                new Vector3(0f, fallbackGroundY, 0f));
            if (!floor.Raycast(currentRay, out float distance) ||
                distance < 0f || distance > maxBuildDistance)
            {
                preview.SetActive(false);
                return;
            }
            location = currentRay.GetPoint(distance);
        }
        else
        {
            preview.SetActive(false);
            return;
        }

        if (gridSnap)
        {
            location.x = Mathf.Round(location.x / gridSize) * gridSize;
            location.z = Mathf.Round(location.z / gridSize) * gridSize;
        }

        preview.SetActive(true);
        preview.transform.SetPositionAndRotation(location,
            Quaternion.Euler(0f, rotationY, 0f));

        if (!TryGetVisualBounds(previewRenderers, out Bounds bounds))
        {
            preview.SetActive(false);
            return;
        }

        // Prefabs with a centred pivot rest on top of the placement surface.
        Vector3 adjusted = preview.transform.position;
        adjusted.y += (location.y + verticalOffset) - bounds.min.y;
        preview.transform.position = adjusted;

        TryGetVisualBounds(previewRenderers, out bounds);
        canPlace = !checkForObstructions || IsSpaceFree(bounds, surfaceCollider);
        ColourPreview(canPlace ? validPreviewColour : blockedPreviewColour);
    }

    private static bool TryGetVisualBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;
        if (renderers == null)
            return false;
        foreach (Renderer item in renderers)
        {
            if (item == null || !item.enabled || !item.gameObject.activeInHierarchy)
                continue;
            if (!found)
            {
                bounds = item.bounds;
                found = true;
            }
            else
                bounds.Encapsulate(item.bounds);
        }
        return found;
    }

    private bool IsSpaceFree(Bounds bounds, Collider supportingSurface)
    {
        Vector3 extents = bounds.extents;
        extents.x = Mathf.Max(0.01f, extents.x - collisionInset);
        extents.y = Mathf.Max(0.01f, extents.y - collisionInset);
        extents.z = Mathf.Max(0.01f, extents.z - collisionInset);

        Collider[] overlaps = Physics.OverlapBox(bounds.center, extents,
            Quaternion.identity, obstructionLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider collider in overlaps)
        {
            if (collider == null || !collider.enabled)
                continue;
            // The floor (or top of a placed piece) is allowed to support a new item.
            if (supportingSurface != null &&
                (collider == supportingSurface ||
                 collider.transform.IsChildOf(supportingSurface.transform)))
                continue;
            if (preview != null && collider.transform.IsChildOf(preview.transform))
                continue;
            return false;
        }
        return true;
    }

    private void ColourPreview(Color colour)
    {
        if (previewRenderers == null)
            return;
        foreach (Renderer item in previewRenderers)
        {
            if (item == null)
                continue;
            item.GetPropertyBlock(colourBlock);
            colourBlock.SetColor("_Color", colour);
            colourBlock.SetColor("_BaseColor", colour);
            item.SetPropertyBlock(colourBlock);
            colourBlock.Clear();
        }
    }

    private void PlaceSelected()
    {
        if (preview == null || !canPlace)
            return;
        PieceRecord piece = new PieceRecord
        {
            PrefabIndex = selectedIndex,
            Position = preview.transform.position,
            Rotation = preview.transform.rotation
        };
        Spawn(piece);
        RecordAction(new BuildAction(true, piece));
        Physics.SyncTransforms();
        RefreshPreview();
    }

    private void TryRemoveUnderCursor()
    {
        if (!hasRay)
            return;

        RaycastHit[] hits = Physics.RaycastAll(currentRay, maxBuildDistance,
            ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            PieceRecord piece = FindPlacedPiece(hit.collider.transform);
            if (piece != null)
            {
                Despawn(piece);
                RecordAction(new BuildAction(false, piece));
            }
            // Stop at first solid obstruction: don't delete through walls.
            break;
        }
    }

    private PieceRecord FindPlacedPiece(Transform hit)
    {
        while (hit != null)
        {
            if (placed.TryGetValue(hit.gameObject, out PieceRecord item))
                return item;
            hit = hit.parent;
        }
        return null;
    }

    private void Spawn(PieceRecord piece)
    {
        GameObject prefab = placeablePrefabs[piece.PrefabIndex];
        if (prefab == null)
            return;
        piece.Instance = Instantiate(prefab, piece.Position,
            piece.Rotation, placedObjectsParent);
        piece.Instance.name = prefab.name.Replace(" [template]", "") + " [placed]";
        piece.Instance.SetActive(true);
        placed[piece.Instance] = piece;
    }

    private void Despawn(PieceRecord piece)
    {
        if (piece.Instance == null)
            return;
        placed.Remove(piece.Instance);
        piece.Instance.SetActive(false);
        Destroy(piece.Instance);
        piece.Instance = null;
    }

    private void RecordAction(BuildAction action)
    {
        if (historyPosition < history.Count)
            history.RemoveRange(historyPosition, history.Count - historyPosition);
        history.Add(action);
        historyPosition++;
        if (history.Count > maxUndoActions)
        {
            history.RemoveAt(0);
            historyPosition--;
        }
    }

    public void Undo()
    {
        if (historyPosition == 0)
            return;
        BuildAction action = history[--historyPosition];
        if (action.WasPlacement)
            Despawn(action.Piece);
        else
            Spawn(action.Piece);
        Physics.SyncTransforms();
    }

    public void Redo()
    {
        if (historyPosition >= history.Count)
            return;
        BuildAction action = history[historyPosition++];
        if (action.WasPlacement)
            Spawn(action.Piece);
        else
            Despawn(action.Piece);
        Physics.SyncTransforms();
    }

    public void ClearAllBuildings()
    {
        List<PieceRecord> all = new List<PieceRecord>(placed.Values);
        foreach (PieceRecord item in all)
            Despawn(item);
        history.Clear();
        historyPosition = 0;
        Physics.SyncTransforms();
    }

    private void OnGUI()
    {
        if (!showInstructions || !buildMode ||
            placeablePrefabs == null || placeablePrefabs.Length == 0)
            return;

        GUI.Box(new Rect(12, 12, 370, 118), "RUNTIME BUILD MODE");
        string label = placeablePrefabs[selectedIndex] != null
            ? placeablePrefabs[selectedIndex].name.Replace(" [template]", "")
            : "Missing prefab";
        GUI.Label(new Rect(24, 36, 345, 22), "Selected: " + label);
        GUI.Label(new Rect(24, 57, 345, 22),
            "LMB place | RMB remove | Q/E rotate | G snap");
        GUI.Label(new Rect(24, 78, 345, 22),
            "1-9 / wheel switch | Ctrl+Z/Y undo/redo | B exit");
        GUI.Label(new Rect(24, 99, 345, 22),
            "Snap: " + (gridSnap ? "ON" : "OFF") + " | " +
            (canPlace ? "Placeable" : "No valid placement") +
            " | Objects: " + placed.Count);
    }

    private void OnDestroy()
    {
        foreach (GameObject item in exampleTemplates)
            if (item != null)
                Destroy(item);
    }
}
