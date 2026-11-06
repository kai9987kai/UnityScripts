using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reusable, bounded prefab pools for projectiles, VFX, enemies and pickups.
/// Attach to a single scene GameObject; configure named pools in the Inspector.
/// Use Spawn(), Return(), ReturnAll(), or the optional timed auto-return.
/// No external packages or singleton are required.
/// </summary>
[DisallowMultipleComponent]
public sealed class RuntimeObjectPool : MonoBehaviour
{
    [Serializable]
    public sealed class PoolDefinition
    {
        [Tooltip("Unique identifier, e.g. Bullet or ImpactFX.")]
        public string id = "Example";
        public GameObject prefab;
        [Min(0)] public int prewarm = 8;
        [Min(1)] public int maximumInstances = 64;
        [Tooltip("Allow creating more instances when the prewarmed objects are busy.")]
        public bool expandWhenExhausted = true;
    }

    /// <summary>
    /// Optional callback interface for behaviours on a pooled prefab.
    /// Implement it in any attached MonoBehaviour to reset state on reuse.
    /// </summary>
    public interface IPoolCallbacks
    {
        void OnPoolSpawned();
        void OnPoolReturned();
    }

    private sealed class Bucket
    {
        public PoolDefinition Definition;
        public Transform Storage;
        public int Created;
        public readonly Queue<GameObject> Available = new Queue<GameObject>();
        public readonly HashSet<GameObject> InUse = new HashSet<GameObject>();
    }

    [Header("Pool Definitions")]
    [SerializeField] private List<PoolDefinition> poolDefinitions =
        new List<PoolDefinition>();

    [Header("Diagnostics")]
    [SerializeField] private bool logMissingPools = true;
    [SerializeField] private bool showStatsOverlay = false;

    private readonly Dictionary<string, Bucket> buckets =
        new Dictionary<string, Bucket>(StringComparer.Ordinal);
    private readonly Dictionary<GameObject, Bucket> owners =
        new Dictionary<GameObject, Bucket>();
    private readonly Dictionary<GameObject, IPoolCallbacks[]> callbacks =
        new Dictionary<GameObject, IPoolCallbacks[]>();
    private readonly Dictionary<GameObject, float> expiry =
        new Dictionary<GameObject, float>();

    private readonly List<GameObject> expired = new List<GameObject>();
    private readonly List<GameObject> scratch = new List<GameObject>();
    private Transform staging;
    private bool ready;

    public int PoolCount { get { return buckets.Count; } }

    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// Build the named pools and prewarm their prefabs. Called automatically
    /// in Awake and also on first Spawn when invoked before Awake.
    /// </summary>
    public void Initialize()
    {
        if (ready)
            return;

        ready = true;
        GameObject stagingObject = new GameObject("Pool Staging (inactive)");
        stagingObject.SetActive(false);
        stagingObject.transform.SetParent(transform, false);
        staging = stagingObject.transform;

        foreach (PoolDefinition definition in poolDefinitions)
        {
            if (definition == null || definition.prefab == null ||
                string.IsNullOrWhiteSpace(definition.id))
            {
                Debug.LogWarning("RuntimeObjectPool: skipped an incomplete pool definition.", this);
                continue;
            }

            string id = definition.id.Trim();
            if (buckets.ContainsKey(id))
            {
                Debug.LogWarning("RuntimeObjectPool: duplicate pool ID '" + id + "'.", this);
                continue;
            }

            Bucket bucket = new Bucket();
            bucket.Definition = definition;
            GameObject storageObject = new GameObject("Pool: " + id);
            storageObject.transform.SetParent(transform, false);
            bucket.Storage = storageObject.transform;
            buckets.Add(id, bucket);

            int limit = Mathf.Max(1, definition.maximumInstances);
            int prewarm = Mathf.Clamp(definition.prewarm, 0, limit);
            for (int i = 0; i < prewarm; i++)
                CreateInactive(bucket);
        }
    }

    /// <summary>
    /// Rent an instance from a named pool. Returns null if the ID is unknown
    /// or all allowed instances are in use.
    /// lifetimeSeconds > 0 automatically returns it using scaled Time.time.
    /// The returned GameObject is active and positioned in world space.
    /// </summary>
    public GameObject Spawn(
        string id,
        Vector3 worldPosition,
        Quaternion worldRotation,
        Transform parent = null,
        float lifetimeSeconds = 0f)
    {
        if (!ready)
            Initialize();

        Bucket bucket;
        if (string.IsNullOrWhiteSpace(id) ||
            !buckets.TryGetValue(id.Trim(), out bucket))
        {
            if (logMissingPools)
                Debug.LogWarning("RuntimeObjectPool: unknown pool '" + id + "'.", this);
            return null;
        }

        GameObject instance = null;
        while (bucket.Available.Count > 0)
        {
            instance = bucket.Available.Dequeue();
            if (instance != null)
                break;

            // A caller destroyed an inactive object outside this pool.
            bucket.Created = Mathf.Max(0, bucket.Created - 1);
        }

        if (instance == null)
        {
            if (!bucket.Definition.expandWhenExhausted ||
                bucket.Created >= Mathf.Max(1, bucket.Definition.maximumInstances))
                return null;

            instance = CreateInactive(bucket);
            if (instance == null)
                return null;

            // CreateInactive enqueues it, so remove that entry before renting.
            instance = bucket.Available.Dequeue();
        }

        instance.transform.SetParent(parent, false);
        instance.transform.SetPositionAndRotation(worldPosition, worldRotation);

        bucket.InUse.Add(instance);
        if (lifetimeSeconds > 0f && !float.IsNaN(lifetimeSeconds) &&
            !float.IsInfinity(lifetimeSeconds))
            expiry[instance] = Time.time + lifetimeSeconds;
        else
            expiry.Remove(instance);

        instance.SetActive(true);
        InvokeCallbacks(instance, true);
        return instance;
    }

    /// <summary>Return an active instance previously spawned by this manager.</summary>
    public bool Return(GameObject instance)
    {
        if (instance == null)
            return false;

        Bucket bucket;
        if (!owners.TryGetValue(instance, out bucket) ||
            !bucket.InUse.Remove(instance))
            return false;

        expiry.Remove(instance);
        InvokeCallbacks(instance, false);
        if (instance == null)
            return true; // A callback destroyed the object.

        instance.SetActive(false);
        instance.transform.SetParent(bucket.Storage, false);
        bucket.Available.Enqueue(instance);
        return true;
    }

    /// <summary>Return all active instances of one pool. Returns how many were returned.</summary>
    public int ReturnAll(string id)
    {
        if (!ready) Initialize();

        Bucket bucket;
        if (string.IsNullOrWhiteSpace(id) ||
            !buckets.TryGetValue(id.Trim(), out bucket))
            return 0;

        scratch.Clear();
        foreach (GameObject instance in bucket.InUse)
            scratch.Add(instance);

        int count = 0;
        foreach (GameObject instance in scratch)
            if (Return(instance))
                count++;

        scratch.Clear();
        return count;
    }

    /// <summary>Inspect a pool without allocating collections.</summary>
    public bool TryGetStats(string id, out int active, out int available, out int total)
    {
        active = available = total = 0;
        if (!ready) Initialize();

        Bucket bucket;
        if (string.IsNullOrWhiteSpace(id) ||
            !buckets.TryGetValue(id.Trim(), out bucket))
            return false;

        active = bucket.InUse.Count;
        available = bucket.Available.Count;
        total = bucket.Created;
        return true;
    }

    private GameObject CreateInactive(Bucket bucket)
    {
        if (bucket.Definition.prefab == null)
            return null;

        // Inactive staging prevents premature OnEnable calls while prewarming.
        GameObject instance = Instantiate(bucket.Definition.prefab, staging);
        instance.name = bucket.Definition.prefab.name + " [Pooled]";
        instance.SetActive(false);
        instance.transform.SetParent(bucket.Storage, false);

        bucket.Created++;
        owners.Add(instance, bucket);
        bucket.Available.Enqueue(instance);

        // Cache optional lifecycle hooks once rather than allocating every spawn.
        MonoBehaviour[] behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
        List<IPoolCallbacks> hooks = new List<IPoolCallbacks>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            IPoolCallbacks callback = behaviour as IPoolCallbacks;
            if (callback != null)
                hooks.Add(callback);
        }
        callbacks.Add(instance, hooks.ToArray());
        return instance;
    }

    private void InvokeCallbacks(GameObject instance, bool spawned)
    {
        IPoolCallbacks[] hooks;
        if (!callbacks.TryGetValue(instance, out hooks))
            return;

        foreach (IPoolCallbacks hook in hooks)
        {
            // Destroyed MonoBehaviours implement Unity's special null semantics.
            UnityEngine.Object unityHook = hook as UnityEngine.Object;
            if (unityHook == null)
                continue;

            try
            {
                if (spawned) hook.OnPoolSpawned();
                else hook.OnPoolReturned();
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
            }
        }
    }

    private void Update()
    {
        if (expiry.Count == 0)
            return;

        expired.Clear();
        float now = Time.time;
        foreach (KeyValuePair<GameObject, float> pair in expiry)
        {
            if (pair.Key == null || now >= pair.Value)
                expired.Add(pair.Key);
        }

        foreach (GameObject instance in expired)
        {
            if (instance == null)
                expiry.Remove(instance);
            else
                Return(instance);
        }
        expired.Clear();
    }

    private void OnValidate()
    {
        if (poolDefinitions == null)
            return;

        foreach (PoolDefinition definition in poolDefinitions)
        {
            if (definition == null)
                continue;

            definition.maximumInstances = Mathf.Max(1, definition.maximumInstances);
            definition.prewarm = Mathf.Clamp(
                definition.prewarm, 0, definition.maximumInstances);
        }
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || !showStatsOverlay)
            return;

        GUILayout.BeginArea(new Rect(12, 155, 340,
            Mathf.Min(Screen.height - 160, 42 + buckets.Count * 24)), GUI.skin.box);
        GUILayout.Label("RUNTIME OBJECT POOLS");
        foreach (KeyValuePair<string, Bucket> entry in buckets)
        {
            Bucket bucket = entry.Value;
            GUILayout.Label(entry.Key + ": " + bucket.InUse.Count +
                " active / " + bucket.Available.Count +
                " available / " + bucket.Created + " total");
        }
        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        // Spawned instances might have been reparented outside this manager.
        // The manager owns their lifetimes, so destroy them with the manager.
        foreach (KeyValuePair<GameObject, Bucket> entry in owners)
        {
            if (entry.Key != null)
                Destroy(entry.Key);
        }
        owners.Clear();
        callbacks.Clear();
        expiry.Clear();
        buckets.Clear();
    }
}
