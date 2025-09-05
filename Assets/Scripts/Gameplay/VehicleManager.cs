using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleManager : MonoBehaviour
{
    [Header("Spawner Settings")]
    public Transform vehicleSpawner;

    [Tooltip("How many buses are visible at once (default 2).")]
    public int visibleCount = 2;

    [Header("Pool Settings")]
    public int poolSize;

    [Header("Vehicle Settings")]
    public GameObject vehiclePrefab;
    public int busCapacity = 3;
    public Color[] possibleColors;

    [Tooltip("Spacing between buses along spawner.forward direction.")]
    public float busSpacing = 17f;

    //private List<Vehicle> activeVehicles = new List<Vehicle>();

    private List<Vehicle> _pool = new List<Vehicle>();
    private List<Vehicle> _visibleQueue = new List<Vehicle>();
    private Transform _vehiclesParent;

    private void Awake()
    {
        if (vehicleSpawner == null)
        {
            vehicleSpawner = GameObject.Find("VehicleSpawner").transform ?? this.transform; 
            //vehicleSpawner = this.transform;
        }

        // create parent
        _vehiclesParent = transform.Find("Vehicles");
        if (_vehiclesParent == null)
        {
            _vehiclesParent = new GameObject("Vehicles").transform;
            _vehiclesParent.SetParent(this.transform, false);
        }

        //InitializePool();
        //ActivateInitialVisible();
    }

    void Start()
    {
        //SpawnBus();
        poolSize = GridSpawner.passengerCount / busCapacity;
        InitializePool();
        ActivateInitialVisible();
        //Debug.Log(poolSize);
    }

    void InitializePool()
    {
        if (vehiclePrefab == null)
        {
            Debug.LogError("VehicleManager: vehiclePrefab is not assigned.");
            return;
        }

        // instantiate pool and keep them inactive initially
        for (int i = 0; i < poolSize; i++)
        {
            GameObject vehicleObj = Instantiate(vehiclePrefab, _vehiclesParent);
            vehicleObj.name = $"Vehicle_{i}";
            Vehicle v = vehicleObj.GetComponent<Vehicle>();
            //if (v == null)
            //{
            //    Debug.LogError("Vehicle prefab has no Vehicle component!");
            //    Destroy(vehicleObj);
            //    continue;
            //}

            // pick a color (random from possibleColors if provided) --- WILL BE OVERRIDDEN LATER FROM EDITOR LEVEL DESIGNER
            Color c = possibleColors != null && possibleColors.Length > 0
                ? possibleColors[Random.Range(0, possibleColors.Length)]
                : Color.white;

            v.Initialize(c, busCapacity);
            v.ResetForReuse();

            // ensure inactive to start (hidden until shown)
            v.SetVisible(false);

            // subscribe event (optional) — you can have manager listen when bus becomes full
            v.onVehicleFull += OnVehicleFull;

            _pool.Add(v);
        }
    }

    void ActivateInitialVisible()
    {
        _visibleQueue.Clear();

        int toShow = Mathf.Min(visibleCount, _pool.Count);
        for (int i = 0; i < toShow; i++)
        {
            Vehicle v = _pool[i];
            v.SetVisible(true);
            _visibleQueue.Add(v);
        }

        // Position visible vehicles (front at spawner, others behind)
        RepositionVisibleVehicles();
    }

    void RepositionVisibleVehicles()
    {
        for (int i = 0; i < _visibleQueue.Count; i++)
        {
            // front (i==0) is fully visible at spawner
            //Vector3 pos = vehicleSpawner.position + vehicleSpawner.forward * (-i * busSpacing);
            Vector3 pos = vehicleSpawner.position + vehicleSpawner.forward * (i * busSpacing);
            Quaternion rot = vehicleSpawner.rotation;
            _visibleQueue[i].SetTransform(pos, rot);
        }
    }

    /// <summary>
    /// Returns the first visible bus that has free capacity, or null.
    /// Searches visible buses first (game logic usually fills visible ones).
    /// </summary>
    public Vehicle GetAvailableVisibleBus()
    {
        foreach (var v in _visibleQueue)
        {
            if (!v.isFull) return v;
        }
        return null;
    }

    /// <summary>
    /// Call this when a bus departed (or should be recycled). The manager will:
    /// - remove the departed bus from visible queue and hide it/reset it,
    /// - bring the next hidden bus (if any) into visibility as the last slot,
    /// - reposition visible buses so the line is continuous.
    /// </summary>
    public void NotifyBusDeparted(Vehicle departed)
    {
        if (departed == null) return;

        // find in visible queue
        int idx = _visibleQueue.IndexOf(departed);
        if (idx == -1)
        {
            // if it wasn't visible, just make sure we reset it
            departed.ResetForReuse();
            departed.SetVisible(false);
            return;
        }

        // remove it from visible queue and hide/reset
        _visibleQueue.RemoveAt(idx);
        departed.ResetForReuse();
        departed.SetVisible(false);

        // find the first hidden bus in pool (inactive) to become the new last visible
        Vehicle nextHidden = _pool.Find(x => !x.gameObject.activeSelf);
        if (nextHidden != null)
        {
            nextHidden.SetVisible(true);
            _visibleQueue.Add(nextHidden);
        }
        else
        {
            // if none hidden, we can optionally reuse the departed bus (circular pool).
            // For now we won't re-add it immediately.
        }

        // reposition the visible line (front at spawner)
        RepositionVisibleVehicles();
    }

    // example hook for when a vehicle becomes full
    private void OnVehicleFull(Vehicle v)
    {
        Debug.Log($"VehicleManager noticed {v.name} is full.");
        // Option: automatically trigger departure (recycle) or move vehicle.
        // For example:
        // NotifyBusDeparted(v);
    }

    // Optional: helper to force-spawn next bus immediately (useful for testing)
    public void ForceShowNextHiddenBus()
    {
        Vehicle nextHidden = _pool.Find(x => !x.gameObject.activeSelf);
        if (nextHidden != null)
        {
            _visibleQueue.Add(nextHidden);
            nextHidden.SetVisible(true);
            RepositionVisibleVehicles();
        }
    }

    //public Vehicle SpawnBus()
    //{
    //    if (vehiclePrefab == null || vehicleSpawner == null)
    //    {
    //        Debug.LogError("VehicleManager: Missing prefab or spawner reference.");
    //        return null;
    //    }

    //    // Instantiate the bus
    //    GameObject busObj = Instantiate(vehiclePrefab, vehicleSpawner.position, vehicleSpawner.rotation);

    //    // Get Vehicle component
    //    Vehicle vehicle = busObj.GetComponent<Vehicle>();
    //    if (vehicle == null)
    //    {
    //        Debug.LogError("Vehicle prefab has no Vehicle script attached!");
    //        return null;
    //    }

    //    // Pick a random color if not assigned
    //    Color chosenColor = possibleColors.Length > 0 ? possibleColors[Random.Range(0, possibleColors.Length)] : Color.white;

    //    // Initialize bus
    //    vehicle.Initialize(chosenColor, busCapacity);

    //    // Track it
    //    activeVehicles.Add(vehicle);

    //    return vehicle;
    //}

    ///// <summary>
    ///// Returns the first bus that still has space.
    ///// </summary>
    //public Vehicle GetAvailableBus()
    //{
    //    return activeVehicles.Find(v => !v.isFull);
    //}
}
