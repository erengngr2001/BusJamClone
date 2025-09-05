using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleManager : MonoBehaviour
{
    [Header("Spawner Settings")]
    public Transform vehicleSpawner;
    public int visibleCount = 2;

    [Header("Pool Settings")]
    public int poolSize;

    [Header("Vehicle Settings")]
    public GameObject vehiclePrefab;
    public int busCapacity = 3;
    public Color[] possibleColors;
    public float busSpacing = 17f;

    [Tooltip("How long the bus shifting animation takes.")]
    public float shiftAnimationDuration = 0.5f; // Animation speed control

    private List<Vehicle> _pool = new List<Vehicle>();
    private List<Vehicle> _visibleQueue = new List<Vehicle>();
    private Transform _vehiclesParent;

    public static VehicleManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (vehicleSpawner == null)
            vehicleSpawner = GameObject.Find("VehicleSpawner").transform ?? this.transform;

        _vehiclesParent = transform.Find("Vehicles");
        if (_vehiclesParent == null)
        {
            _vehiclesParent = new GameObject("Vehicles").transform;
            _vehiclesParent.SetParent(this.transform, false);
        }
    }

    void Start()
    {
        poolSize = GridSpawner.passengerCount / busCapacity;
        InitializePool();
        ActivateInitialVisible();
    }

    void InitializePool()
    {
        if (vehiclePrefab == null)
        {
            Debug.LogError("VehicleManager: vehiclePrefab is not assigned.");
            return;
        }
        for (int i = 0; i < poolSize; i++)
        {
            GameObject vehicleObj = Instantiate(vehiclePrefab, _vehiclesParent);
            vehicleObj.name = $"Vehicle_{i + 1}"; // Name them 1-based for clarity
            Vehicle v = vehicleObj.GetComponent<Vehicle>();
            Color c = possibleColors != null && possibleColors.Length > 0
                ? possibleColors[Random.Range(0, possibleColors.Length)]
                : Color.white;
            v.Initialize(c, busCapacity);
            v.ResetForReuse();
            v.SetVisible(false);
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

        // Snap to initial positions
        for (int i = 0; i < _visibleQueue.Count; i++)
        {
            Vector3 pos = vehicleSpawner.position + vehicleSpawner.forward * (-i * busSpacing);
            _visibleQueue[i].SetTransform(pos, vehicleSpawner.rotation);
        }

        GridSpawner.Instance.ProcessBoarding();
    }

    public Vehicle GetFrontBus()
    {
        return _visibleQueue.Count > 0 ? _visibleQueue[0] : null;
    }

    public void NotifyBusDeparted(Vehicle departed)
    {
        // The departed bus is already destroyed, so we just update our lists.
        _visibleQueue.Remove(departed);
        _pool.Remove(departed);

        // Find the next bus in the main pool that isn't already visible.
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
            // Position the new bus at the back before the animation starts.
            Vector3 newBusPos = vehicleSpawner.position + vehicleSpawner.forward * (-visibleCount * busSpacing);
            nextInLine.SetTransform(newBusPos, vehicleSpawner.rotation);
            nextInLine.SetVisible(true);
            _visibleQueue.Add(nextInLine);
        }

        // Animate all visible buses moving forward.
        StartCoroutine(AnimateBusQueueShift());
    }

    private void OnVehicleFull(Vehicle v)
    {
        Debug.Log($"VehicleManager noticed {v.name} is full.");
        v.Depart();
    }

    private IEnumerator AnimateBusQueueShift()
    {
        float elapsed = 0f;

        // Store start and end positions for a smooth lerp
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
                // Safety check in case a bus was destroyed during the animation
                if (_visibleQueue[i] != null)
                {
                    _visibleQueue[i].transform.position = Vector3.Lerp(startPositions[i], endPositions[i], elapsed / shiftAnimationDuration);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap to final positions to ensure accuracy
        for (int i = 0; i < _visibleQueue.Count; i++)
        {
            if (_visibleQueue[i] != null)
            {
                _visibleQueue[i].transform.position = endPositions[i];
            }
        }

        // After the animation is complete, check if the new front bus can board anyone.
        GridSpawner.Instance.ProcessBoarding();
    }
}

