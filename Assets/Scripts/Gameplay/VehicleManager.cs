using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleManager : MonoBehaviour
{
    [Header("Spawner Settings")]
    public Transform vehicleSpawner;
    public int visibleCount = 2;

    [Header("Vehicle Settings")]
    public int busCapacity = 3;
    public float busSpacing = 17f;

    [Tooltip("How long the bus shifting animation takes.")]
    public float shiftAnimationDuration = 0.5f;

    private List<Vehicle> _pool = new List<Vehicle>();
    private List<Vehicle> _visibleQueue = new List<Vehicle>();
    private Transform _vehiclesParent;

    public static VehicleManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    /// <summary>
    /// This method is now called by GridSpawner after it has been initialized with a level.
    /// It clears old vehicles and builds the new queue based on the new LevelData.
    /// </summary>
    public void InitializeWithLevel(LevelData level)
    {
        if (level == null)
        {
            Debug.LogError("[VehicleManager] InitializeWithLevel called with a null level!");
            return;
        }

        // Clean up any vehicles from a previous run
        if (_vehiclesParent != null)
        {
            Destroy(_vehiclesParent.gameObject);
        }
        _vehiclesParent = new GameObject("Vehicles").transform;
        _vehiclesParent.SetParent(this.transform, false);

        _pool.Clear();
        _visibleQueue.Clear();

        // --- All logic from Start() is now here ---
        int poolSize = level.vehicleCount;
        Debug.Log($"[VehicleManager] Initializing pool with {poolSize} vehicles for level '{level.name}'.");
        InitializePool(level);
        ActivateInitialVisible();
    }

    void InitializePool(LevelData level)
    {
        for (int i = 0; i < level.vehicleCount; i++)
        {
            if (i >= level.vehicles.Count || level.vehicles[i] == null)
            {
                Debug.LogWarning($"Vehicle prefab at index {i} is missing in LevelData '{level.name}'. Skipping.");
                continue;
            }

            GameObject vehicleObj = Instantiate(level.vehicles[i], _vehiclesParent);
            vehicleObj.name = $"Vehicle_{i + 1}";
            Vehicle v = vehicleObj.GetComponent<Vehicle>();

            SetColorFromMaterial(vehicleObj, v);
            v.ResetForReuse();
            v.SetVisible(false);
            v.onVehicleFull += OnVehicleFull;
            _pool.Add(v);
        }
    }

    // This method is now private as it's only called during initialization
    private void ActivateInitialVisible()
    {
        _visibleQueue.Clear();
        int toShow = Mathf.Min(visibleCount, _pool.Count);
        for (int i = 0; i < toShow; i++)
        {
            Vehicle v = _pool[i];
            v.SetVisible(true);
            _visibleQueue.Add(v);
        }

        for (int i = 0; i < _visibleQueue.Count; i++)
        {
            if (vehicleSpawner == null) vehicleSpawner = this.transform;
            Vector3 pos = vehicleSpawner.position + vehicleSpawner.forward * (-i * busSpacing);
            _visibleQueue[i].SetTransform(pos, vehicleSpawner.rotation);
        }

        // A final check to ensure GridSpawner is ready before processing boarding
        if (GridSpawner.Instance != null)
        {
            GridSpawner.Instance.ProcessBoarding();
        }
    }

    void SetColorFromMaterial(GameObject vehicleObj, Vehicle v)
    {
        if (v == null || vehicleObj == null) return;

        Renderer rend = vehicleObj.GetComponentInChildren<Renderer>();
        if (rend == null || rend.materials.Length < 2)
        {
            v.Initialize(Color.white, busCapacity);
            Debug.LogWarning($"Could not find renderer or material on {vehicleObj.name} to set color.");
            return;
        }

        Material mat = rend.materials[1];
        Color matColor = ExtractColorFromMaterial(mat);
        v.Initialize(matColor, busCapacity);
    }

    Color ExtractColorFromMaterial(Material mat)
    {
        if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
        if (mat.HasProperty("_Color")) return mat.GetColor("_Color");
        return Color.white;
    }

    public Vehicle GetFrontBus()
    {
        return _visibleQueue.Count > 0 ? _visibleQueue[0] : null;
    }

    public void NotifyBusDeparted(Vehicle departed)
    {
        if (departed == null) return;

        _visibleQueue.Remove(departed);
        _pool.Remove(departed);

        Vehicle nextInLine = null;
        foreach (var bus in _pool)
        {
            if (!_visibleQueue.Contains(bus))
            {
                nextInLine = bus;
                break;
            }
        }

        if (nextInLine != null)
        {
            Vector3 newBusPos = vehicleSpawner.position + vehicleSpawner.forward * (-visibleCount * busSpacing);
            nextInLine.SetTransform(newBusPos, vehicleSpawner.rotation);
            nextInLine.SetVisible(true);
            _visibleQueue.Add(nextInLine);
        }

        StartCoroutine(AnimateBusQueueShift());
    }

    private void OnVehicleFull(Vehicle v)
    {
        v.Depart();
    }

    private IEnumerator AnimateBusQueueShift()
    {
        float elapsed = 0f;
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> endPositions = new List<Vector3>();

        for (int i = 0; i < _visibleQueue.Count; i++)
        {
            startPositions.Add(_visibleQueue[i].transform.position);
            endPositions.Add(vehicleSpawner.position + vehicleSpawner.forward * (-i * busSpacing));
        }

        while (elapsed < shiftAnimationDuration)
        {
            for (int i = 0; i < _visibleQueue.Count; i++)
            {
                if (_visibleQueue[i] != null)
                {
                    _visibleQueue[i].transform.position = Vector3.Lerp(startPositions[i], endPositions[i], elapsed / shiftAnimationDuration);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < _visibleQueue.Count; i++)
        {
            if (_visibleQueue[i] != null)
            {
                _visibleQueue[i].transform.position = endPositions[i];
            }
        }

        if (GridSpawner.Instance != null)
        {
            GridSpawner.Instance.ProcessBoarding();
        }
    }
}

