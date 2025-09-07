using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pipe : MonoBehaviour
{
    const string SPAWN_POINT_NAME = "PassengerSpawnPoint";
    private Transform _passengerSpawnPoint;

    [Header("# of Passengers in Pipe")]
    public int pipePoolSize = 3;

    // pool
    private List<GameObject> _passengerPool = new List<GameObject>();
    private int _frontIndex = 0;

    [HideInInspector] public int x;
    [HideInInspector] public int y;
    [HideInInspector] public float rotationY = 0f;

    // simple guard to avoid concurrently starting multiple release coroutines for same pipe
    private bool _releaseInProgress = false;

    //private void Awake()
    //{
    //    TryCreateSpawnPoint();
    //}

    public void Initialize(int x, int y)
    {
        this.x = x;
        this.y = y;

        if (GridSpawner.Instance != null && GridSpawner.Instance.level != null)
        {
            PipeData pd = GridSpawner.Instance.level.GetPipeData(x, y);
            if (pd != null)
            {
                // apply Y rotation in degrees
                transform.localRotation = Quaternion.Euler(0f, pd.rotationY, 0f);
            }
        }

        TryCreateSpawnPoint();
    }

    private void TryCreateSpawnPoint()
    {
        // Don't create duplicates
        var existing = transform.Find(SPAWN_POINT_NAME);
        if (existing != null)
        {
            _passengerSpawnPoint = existing;
            return;
        }

        // Determine cell size (fallback to 2f if GridSpawner/LevelData not ready)
        float cellSize = 2f;
        if (GridSpawner.Instance != null && GridSpawner.Instance.level != null)
            cellSize = GridSpawner.Instance.level.cellSize;

        // Create the empty child and place it forward by cellSize in local space
        GameObject go = new GameObject(SPAWN_POINT_NAME);

        go.transform.SetParent(this.transform, false);      // keep as local child
        go.transform.localPosition = Vector3.forward * cellSize;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        _passengerSpawnPoint = go.transform;
        GridCell passengerSpawnerCell = GetGridCellForSpawner();
        passengerSpawnerCell?.SetOccupyingObject(go);
        GridSpawner.Instance?.SetGridCell(passengerSpawnerCell);
    }

    /// <summary>
    /// Optional helper: returns the spawn point transform (may be null if creation failed).
    /// </summary>
    public Transform GetPassengerSpawnPoint()
    {
        return _passengerSpawnPoint;
    }

    public GridCell GetGridCellForSpawner()
    {
        GridCell pipeCell = GridSpawner.Instance.GetGridCell(x, y);

        if (pipeCell == null)
        {
            Debug.LogWarning($"Pipe at ({x},{y}) could not find its GridCell.");
            return null;
        }

        // Convert spawn point into local pipe space so rotation is respected.
        Vector3 local = transform.InverseTransformPoint(_passengerSpawnPoint.position);

        // Decide cardinal direction by whichever axis has larger absolute value (x or z).
        // We assume spawn point is placed roughly along one axis (forward/back/right/left).
        float absX = Mathf.Abs(local.x);
        float absZ = Mathf.Abs(local.z);

        if (absZ >= absX)
        {
            // forward or backward in local Z
            if (local.z > 0f) // forward
            {
                return new GridCell(pipeCell.x, pipeCell.y + 1, _passengerSpawnPoint.position);
            }
            else // backward
            {
                return new GridCell(pipeCell.x, pipeCell.y - 1, _passengerSpawnPoint.position);
            }
        }
        else
        {
            // left or right in local X
            if (local.x > 0f) // right
            {
                return new GridCell(pipeCell.x + 1, pipeCell.y, _passengerSpawnPoint.position);
            }
            else // left
            {
                return new GridCell(pipeCell.x - 1, pipeCell.y, _passengerSpawnPoint.position);
            }
        }

    }

    /// Create a local pool of passengers as children of the spawn point.
    /// Only the front passenger (index 0) will be active & interactable at first.
    public void CreatePassengerPool()
    {
        if (_passengerSpawnPoint == null)
        {
            Debug.LogWarning($"Pipe.CreatePassengerPool: spawn point missing on {name}.");
            return;
        }
        if (GridSpawner.Instance == null)
        {
            Debug.LogWarning("Pipe.CreatePassengerPool: GridSpawner.Instance missing.");
            return;
        }
        GameObject passengerPrefab = GridSpawner.Instance.passengerPrefab;
        if (passengerPrefab == null)
        {
            Debug.LogWarning("Pipe.CreatePassengerPool: passengerPrefab not set on GridSpawner.");
            return;
        }

        // get pipe config from level data
        PipeData pd = GridSpawner.Instance.level.GetPipeData(x, y);
        int configuredSize = pd != null ? pd.poolSize : pipePoolSize;
        GridSpawner.passengerCount += configuredSize;
        List<Material> configuredMats = (pd != null && pd.materials != null) ? pd.materials : null;

        // cleanup if previously created
        foreach (var go in _passengerPool)
            if (go != null) Destroy(go);
        _passengerPool.Clear();
        _frontIndex = 0;

        // clamp non-negative
        configuredSize = Mathf.Max(0, configuredSize);
        pipePoolSize = configuredSize; // sync instance value

        for (int i = 0; i < pipePoolSize; i++)
        {
            Vector3 spawnPos = _passengerSpawnPoint.position + new Vector3(0f, 0.55f, 0f);
            GameObject p = Instantiate(passengerPrefab, spawnPos, Quaternion.identity, _passengerSpawnPoint);
            p.name = $"{name}_Passenger_{i}";
            var passengerComp = p.GetComponent<Passenger>();

            // ensure off-grid
            passengerComp.InitializeGridCoord(-1, -1);
            // route clicks to this pipe (so pipe knows which pool to advance)
            passengerComp.onClickedByPlayer = HandlePipePassengerClicked;

            Renderer rend = p.GetComponentInChildren<Renderer>();

            if (rend == null)
            {
                rend = p.GetComponentInChildren<Renderer>();
            }

            if (rend != null)
            {
                rend.material = configuredMats[i];
                // refresh color manager to pick up the new material color
                var pcm = p.GetComponent<PassengerColorManager>();
                if (pcm != null)
                {
                    var refresh = pcm.GetType().GetMethod("RefreshOriginalColor");
                    if (refresh != null) refresh.Invoke(pcm, null);

                    pcm.ApplyReachability(passengerComp != null ? passengerComp.isReachable : true);
                }
            }

            // The front one is visible & interactable, others are hidden (both collider and visibility inactive)
            if (i == 0)
            {
                p.SetActive(true);
                passengerComp?.SetInteractable(true);
            }
            else
            {
                p.SetActive(false);
                passengerComp?.SetInteractable(false);
            }

            _passengerPool.Add(p);
        }
    }


    /// Called via each passenger's onClickedByPlayer action.
    /// The pipe forwards the clicked passenger to GridSpawner to add into waiting, then advances the pool (enables the next passenger).
    public void HandlePipePassengerClicked(Passenger clicked)
    {
        if (clicked == null) return;

        // Ask GridSpawner to add this passenger to the waiting line.
        GridSpawner.Instance?.HandleClick(clicked);

        // Remove or mark the front passenger as used and enable the next one
        if (_passengerPool.Count == 0) return;

        if (_frontIndex < _passengerPool.Count)
        {
            _passengerPool[_frontIndex] = null; // mark consumed
            _frontIndex++;
        }

        // skip any nulls to find next available index
        while (_frontIndex < _passengerPool.Count && (_passengerPool[_frontIndex] == null))
            _frontIndex++;

        // Start delayed release of the next passenger (if any)
        if (_frontIndex < _passengerPool.Count && _passengerPool[_frontIndex] != null)
        {
            // prevent overlapping coroutines for this pipe
            if (!_releaseInProgress)
                StartCoroutine(ReleaseNextAfterDelay());
        }
    }

    // For a small delay for revealing next passenger in the pipe
    private IEnumerator ReleaseNextAfterDelay()
    {
        _releaseInProgress = true;

        // small visual delay
        yield return new WaitForSeconds(0.5f);

        // find next non-null passenger and enable it
        while (_frontIndex < _passengerPool.Count && (_passengerPool[_frontIndex] == null))
            _frontIndex++;

        if (_frontIndex < _passengerPool.Count)
        {
            var next = _passengerPool[_frontIndex];
            if (next != null)
            {
                next.SetActive(true);
                var nextComp = next.GetComponent<Passenger>();
                nextComp?.SetInteractable(true);
            }
        }

        _releaseInProgress = false;
    }

}
