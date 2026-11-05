using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Creates a fully connected, reproducible 3D maze from Unity primitives.
/// No prefabs, packages, NavMesh, or external assets are required.
///
/// Setup:
/// 1. Add this component to an empty GameObject.
/// 2. Optionally assign a player Transform (for example, SimpleFPSController).
/// 3. Press Play. R = regenerate; H = show/hide the shortest route.
///
/// The generated floor and walls include 3D colliders. The exit invokes a
/// UnityEvent when the assigned player reaches it.
/// </summary>
[DisallowMultipleComponent]
public sealed class ProceduralMazeGenerator : MonoBehaviour
{
    [Header("Maze Layout")]
    [SerializeField, Range(2, 40)] private int mazeWidth = 12;
    [SerializeField, Range(2, 40)] private int mazeDepth = 12;
    [SerializeField, Min(1f)] private float cellSize = 4f;
    [SerializeField, Min(0.5f)] private float wallHeight = 3f;
    [SerializeField, Min(0.05f)] private float wallThickness = 0.25f;
    [SerializeField, Min(0.05f)] private float floorThickness = 0.35f;

    [Header("Deterministic Generation")]
    [SerializeField] private int seed = 12345;
    [SerializeField] private bool randomizeSeedOnGenerate = false;
    [SerializeField] private bool generateOnStart = true;

    [Header("Player and Goal")]
    [SerializeField] private Transform player;
    [Tooltip("Height of the player's origin above the floor when spawned.")]
    [SerializeField, Min(0f)] private float playerSpawnHeight = 1.1f;
    [SerializeField, Range(0.15f, 0.49f)] private float exitRadiusInCells = 0.36f;
    [SerializeField] private UnityEvent onMazeCompleted = new UnityEvent();

    [Header("Debug and Controls")]
    [SerializeField] private bool showSolutionOnStart = false;
    [SerializeField] private bool showOnScreenHints = true;
    [SerializeField] private bool enableKeyboardControls = true;

    [Header("Optional Custom Materials")]
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material floorMaterial;
    [SerializeField] private Material startMaterial;
    [SerializeField] private Material exitMaterial;
    [SerializeField] private Material solutionMaterial;

    // Wall indices: north (+Z), east (+X), south (-Z), west (-X).
    private static readonly int[] Dx = { 0, 1, 0, -1 };
    private static readonly int[] Dz = { 1, 0, -1, 0 };

    private sealed class Cell
    {
        public readonly bool[] Walls = { true, true, true, true };
        public bool Visited;
    }

    private Cell[,] cells;
    private Transform generatedRoot;
    private Transform solutionRoot;
    private Material[] temporaryMaterials;
    private bool mazeCompleted;

    public int CurrentSeed { get { return seed; } }
    public bool IsCompleted { get { return mazeCompleted; } }

    private void Start()
    {
        if (generateOnStart)
            Generate();
    }

    private void OnValidate()
    {
        mazeWidth = Mathf.Clamp(mazeWidth, 2, 40);
        mazeDepth = Mathf.Clamp(mazeDepth, 2, 40);
        cellSize = Mathf.Max(1f, cellSize);
        wallHeight = Mathf.Max(0.5f, wallHeight);
        wallThickness = Mathf.Clamp(wallThickness, 0.05f, cellSize * 0.4f);
        floorThickness = Mathf.Max(0.05f, floorThickness);
        playerSpawnHeight = Mathf.Max(0f, playerSpawnHeight);
    }

    private void Update()
    {
        if (enableKeyboardControls)
        {
            bool regenerate = false;
            bool toggleSolution = false;

#if ENABLE_LEGACY_INPUT_MANAGER
            regenerate |= Input.GetKeyDown(KeyCode.R);
            toggleSolution |= Input.GetKeyDown(KeyCode.H);
#endif
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                regenerate |= UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame;
                toggleSolution |= UnityEngine.InputSystem.Keyboard.current.hKey.wasPressedThisFrame;
            }
#endif
            if (regenerate)
                Generate();
            if (toggleSolution)
                ToggleSolution();
        }

        CheckForCompletion();
    }

    /// <summary>Rebuilds geometry from a seeded depth-first maze.</summary>
    [ContextMenu("Generate Maze (Play Mode)")]
    public void Generate()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode before generating the maze.", this);
            return;
        }

        ClearGeneratedObjects();
        EnsureMaterials();

        if (randomizeSeedOnGenerate)
            seed = Guid.NewGuid().GetHashCode();

        var random = new System.Random(seed);
        cells = new Cell[mazeWidth, mazeDepth];

        for (int x = 0; x < mazeWidth; x++)
            for (int z = 0; z < mazeDepth; z++)
                cells[x, z] = new Cell();

        CarveConnectedMaze(random);

        generatedRoot = new GameObject("Generated Maze (runtime)").transform;
        generatedRoot.SetParent(transform, false);
        generatedRoot.localPosition = Vector3.zero;
        generatedRoot.localRotation = Quaternion.identity;
        generatedRoot.localScale = Vector3.one;

        BuildGeometry();
        BuildMarkers();
        BuildSolutionPath();
        solutionRoot.gameObject.SetActive(showSolutionOnStart);

        mazeCompleted = false;
        PlacePlayerAtStart();
        Debug.Log("Maze generated with seed " + seed + ".", this);
    }

    private void CarveConnectedMaze(System.Random random)
    {
        // Iterative randomized depth-first search: no recursion or isolated cells.
        var stack = new Stack<Vector2Int>();
        var possibleDirections = new List<int>(4);
        var current = new Vector2Int(0, 0);
        cells[0, 0].Visited = true;
        stack.Push(current);

        while (stack.Count > 0)
        {
            current = stack.Peek();
            possibleDirections.Clear();

            for (int direction = 0; direction < 4; direction++)
            {
                int nextX = current.x + Dx[direction];
                int nextZ = current.y + Dz[direction];

                if (InsideMaze(nextX, nextZ) && !cells[nextX, nextZ].Visited)
                    possibleDirections.Add(direction);
            }

            if (possibleDirections.Count == 0)
            {
                stack.Pop();
                continue;
            }

            int chosen = possibleDirections[random.Next(possibleDirections.Count)];
            int nx = current.x + Dx[chosen];
            int nz = current.y + Dz[chosen];

            cells[current.x, current.y].Walls[chosen] = false;
            cells[nx, nz].Walls[(chosen + 2) % 4] = false;
            cells[nx, nz].Visited = true;
            stack.Push(new Vector2Int(nx, nz));
        }
    }

    private bool InsideMaze(int x, int z)
    {
        return x >= 0 && z >= 0 && x < mazeWidth && z < mazeDepth;
    }

    private void BuildGeometry()
    {
        float width = mazeWidth * cellSize;
        float depth = mazeDepth * cellSize;
        float thickness = Mathf.Min(wallThickness, cellSize * 0.4f);

        CreatePrimitive(
            PrimitiveType.Cube, "Floor", generatedRoot,
            new Vector3(width * 0.5f, -floorThickness * 0.5f, depth * 0.5f),
            new Vector3(width + thickness, floorThickness, depth + thickness),
            floorMaterial != null ? floorMaterial : temporaryMaterials[1], true);

        Material material = wallMaterial != null ? wallMaterial : temporaryMaterials[0];
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int z = 0; z < mazeDepth; z++)
            {
                Cell cell = cells[x, z];
                float cx = (x + 0.5f) * cellSize;
                float cz = (z + 0.5f) * cellSize;

                // Emit each shared wall only once: north and east plus the
                // two outer boundaries on the south and west edges.
                if (cell.Walls[0])
                    CreateWall("North", cx, (z + 1) * cellSize, cellSize + thickness,
                        thickness, material);

                if (cell.Walls[1])
                    CreateWall("East", (x + 1) * cellSize, cz, thickness,
                        cellSize + thickness, material);

                if (z == 0 && cell.Walls[2])
                    CreateWall("South boundary", cx, 0f, cellSize + thickness,
                        thickness, material);

                if (x == 0 && cell.Walls[3])
                    CreateWall("West boundary", 0f, cz, thickness,
                        cellSize + thickness, material);
            }
        }
    }

    private void CreateWall(string label, float x, float z, float sizeX,
        float sizeZ, Material material)
    {
        CreatePrimitive(PrimitiveType.Cube, label, generatedRoot,
            new Vector3(x, wallHeight * 0.5f, z),
            new Vector3(sizeX, wallHeight, sizeZ), material, true);
    }

    private void BuildMarkers()
    {
        float diameter = cellSize * 0.42f;
        var start = CreatePrimitive(
            PrimitiveType.Sphere, "START", generatedRoot,
            CellCentre(0, 0, diameter * 0.4f),
            Vector3.one * diameter,
            startMaterial != null ? startMaterial : temporaryMaterials[2], false);
        start.transform.localScale = Vector3.one * diameter * 0.6f;

        // Decorative goal sphere; completion uses the assigned player's position
        // rather than trigger physics, so no extra component or Rigidbody is needed.
        CreatePrimitive(
            PrimitiveType.Sphere, "EXIT", generatedRoot,
            CellCentre(mazeWidth - 1, mazeDepth - 1, diameter * 0.5f),
            Vector3.one * diameter,
            exitMaterial != null ? exitMaterial : temporaryMaterials[3], false);
    }

    private void BuildSolutionPath()
    {
        solutionRoot = new GameObject("Shortest solution (H to toggle)").transform;
        solutionRoot.SetParent(generatedRoot, false);
        solutionRoot.localPosition = Vector3.zero;
        solutionRoot.localRotation = Quaternion.identity;
        solutionRoot.localScale = Vector3.one;

        List<Vector2Int> path = FindShortestPath();
        Material material = solutionMaterial != null ? solutionMaterial : temporaryMaterials[4];
        float size = Mathf.Min(0.4f, cellSize * 0.14f);

        // Starting and ending markers already identify the path endpoints.
        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2Int cell = path[i];
            CreatePrimitive(PrimitiveType.Sphere, "Breadcrumb " + i,
                solutionRoot, CellCentre(cell.x, cell.y, size * 0.5f),
                Vector3.one * size, material, false);
        }
    }

    private List<Vector2Int> FindShortestPath()
    {
        // Breadth-first search works for mazes containing loops as well.
        var previous = new Vector2Int[mazeWidth, mazeDepth];
        var visited = new bool[mazeWidth, mazeDepth];
        var queue = new Queue<Vector2Int>();

        queue.Enqueue(Vector2Int.zero);
        visited[0, 0] = true;

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            if (cell.x == mazeWidth - 1 && cell.y == mazeDepth - 1)
                break;

            for (int direction = 0; direction < 4; direction++)
            {
                if (cells[cell.x, cell.y].Walls[direction])
                    continue;

                int nx = cell.x + Dx[direction];
                int nz = cell.y + Dz[direction];
                if (!InsideMaze(nx, nz) || visited[nx, nz])
                    continue;

                visited[nx, nz] = true;
                previous[nx, nz] = cell;
                queue.Enqueue(new Vector2Int(nx, nz));
            }
        }

        var result = new List<Vector2Int>();
        if (!visited[mazeWidth - 1, mazeDepth - 1])
            return result;

        Vector2Int current = new Vector2Int(mazeWidth - 1, mazeDepth - 1);
        while (current != Vector2Int.zero)
        {
            result.Add(current);
            current = previous[current.x, current.y];
        }

        result.Add(Vector2Int.zero);
        result.Reverse();
        return result;
    }

    private GameObject CreatePrimitive(PrimitiveType type, string objectName,
        Transform parent, Vector3 localPosition, Vector3 localScale,
        Material material, bool keepCollider)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = objectName;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = localScale;

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;

        if (!keepCollider)
        {
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }
        }

        return obj;
    }

    private Vector3 CellCentre(int x, int z, float height)
    {
        return new Vector3((x + 0.5f) * cellSize, height,
            (z + 0.5f) * cellSize);
    }

    private void PlacePlayerAtStart()
    {
        if (player == null)
            return;

        // CharacterControllers should be disabled during scripted teleports.
        CharacterController controller = player.GetComponent<CharacterController>();
        bool restoreController = controller != null && controller.enabled;
        if (restoreController)
            controller.enabled = false;

        player.SetPositionAndRotation(
            transform.TransformPoint(CellCentre(0, 0, playerSpawnHeight)),
            transform.rotation);

        if (restoreController)
            controller.enabled = true;
    }

    private void CheckForCompletion()
    {
        if (mazeCompleted || player == null || cells == null)
            return;

        Vector3 localPlayerPosition = transform.InverseTransformPoint(player.position);
        Vector3 goal = CellCentre(mazeWidth - 1, mazeDepth - 1, 0f);
        float deltaX = localPlayerPosition.x - goal.x;
        float deltaZ = localPlayerPosition.z - goal.z;
        float radius = cellSize * exitRadiusInCells;

        if (deltaX * deltaX + deltaZ * deltaZ <= radius * radius &&
            Mathf.Abs(localPlayerPosition.y) < wallHeight * 2f)
        {
            mazeCompleted = true;
            Debug.Log("Maze completed! Seed: " + seed, this);
            onMazeCompleted.Invoke();
        }
    }

    public void ToggleSolution()
    {
        if (solutionRoot != null)
            solutionRoot.gameObject.SetActive(!solutionRoot.gameObject.activeSelf);
    }

    public Vector3 GetStartWorldPosition()
    {
        return transform.TransformPoint(CellCentre(0, 0, playerSpawnHeight));
    }

    private void ClearGeneratedObjects()
    {
        if (generatedRoot != null)
        {
            generatedRoot.gameObject.SetActive(false);
            Destroy(generatedRoot.gameObject);
            generatedRoot = null;
            solutionRoot = null;
        }
        cells = null;
    }

    private void EnsureMaterials()
    {
        if (temporaryMaterials != null)
            return;

        // Prefer URP when present; fall back to the built-in render pipeline.
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("HDRP/Lit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("Could not find a runtime shader. Using Unity's default primitive materials.", this);
            temporaryMaterials = new Material[5];
            return;
        }

        temporaryMaterials = new Material[5];
        temporaryMaterials[0] = MakeMaterial(shader, new Color(0.24f, 0.39f, 0.57f));
        temporaryMaterials[1] = MakeMaterial(shader, new Color(0.36f, 0.38f, 0.39f));
        temporaryMaterials[2] = MakeMaterial(shader, new Color(0.12f, 0.9f, 0.55f));
        temporaryMaterials[3] = MakeMaterial(shader, new Color(1f, 0.6f, 0.08f));
        temporaryMaterials[4] = MakeMaterial(shader, new Color(0.95f, 0.83f, 0.22f));
    }

    private static Material MakeMaterial(Shader shader, Color color)
    {
        var material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        return material;
    }

    private void OnDestroy()
    {
        if (temporaryMaterials == null)
            return;

        foreach (Material material in temporaryMaterials)
        {
            if (material != null)
                Destroy(material);
        }
    }

    private void OnGUI()
    {
        if (!showOnScreenHints || !Application.isPlaying || cells == null)
            return;

        GUILayout.BeginArea(new Rect(12, 12, 340, 116), GUI.skin.box);
        GUILayout.Label("PROCEDURAL MAZE  |  Seed: " + seed);
        GUILayout.Label("Size: " + mazeWidth + " x " + mazeDepth);
        GUILayout.Label(mazeCompleted ? "Goal reached!" : "Find the orange exit marker.");
        if (enableKeyboardControls)
            GUILayout.Label("R: Regenerate  |  H: Toggle solution");
        GUILayout.EndArea();
    }
}
