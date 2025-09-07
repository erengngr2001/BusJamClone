using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Waiting-slot small container
[Serializable]
public class WaitingSlot
{
    public GridCell slotCell;
    public GameObject placeholder; // visible "empty slot" object
    public GameObject occupant;    // the actual passenger moved here (null if empty)
}

public class GridSpawner : MonoBehaviour
{
    const int WAITING_SIZE = 5;

    [Header("Use This to Shift Grid on Ground")]
    public Vector3 shift = new Vector3(0f, 0f, -2f); // to center of cell

    [Header("Current Level")]
    public LevelData level;            // assign the LevelData asset

    [HideInInspector] public List<Vector2Int> passengerSpawnCoords;
    public static int passengerCount = 0;

    [Header("Prefabs")]
    public GameObject passengerPrefab; // assign passenger prefab
    public GameObject obstaclePrefab;  // assign obstacle prefab
    public GameObject pipePrefab;      // assign pipe prefab

    // Grid data
    private GridCell[,] _grid;

    // Waiting line data
    private WaitingSlot[] _waitingSlots;
    private int _waitingLineCount = 0;
    private Transform _waitingParent;

    // SINGLETON
    public static GridSpawner Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            if (passengerSpawnCoords == null)
                passengerSpawnCoords = new List<Vector2Int>();
            GenerateGridData();
            GenerateWaitingLine();
        }

        // ADDED TO AWAKE FROM START IN ORDER TO EASIER ACCESS TO PASSENGERCOUNT
        if (passengerSpawnCoords == null || passengerSpawnCoords.Count == 0)
        {
            PreparePassengerList();
        }
        SpawnObstacles();
        SpawnPipes();
        SpawnPassengers();
        //Debug.Log($"[GridSpawner] Spawned {passengerCount} passenger groups.");
        ComputePathsForAllPassengers();
    }

    //private void Start()
    //{
    //    if (passengerSpawnCoords == null || passengerSpawnCoords.Count == 0)
    //    {
    //        PreparePassengerList();
    //    }
    //    SpawnPassengers();
    //    //Debug.Log($"[GridSpawner] Spawned {passengerCount} passenger groups.");
    //    ComputePathsForAllPassengers();
    //}

    private void GenerateGridData()
    {
        if (level == null)
        {
            Debug.LogError("LevelData is not assigned in GridSpawner!");
            return;
        }

        _grid = new GridCell[level.width, level.height];

        for (int y = 0; y < level.height; y++)
        {
            for (int x = 0; x < level.width; x++)
            {
                Vector3 worldPos = GetWorldPosition(x, y);
                _grid[x, y] = new GridCell(x, y, worldPos);
            }
        }
        Debug.Log($"Grid data generated for a {level.width}x{level.height} grid.");

    }

    private void GenerateWaitingLine()
    {
        _waitingSlots = new WaitingSlot[WAITING_SIZE];

        // Parent to keep hierarchy tidy
        _waitingParent = transform.Find("WaitingLine");
        if (_waitingParent == null)
        {
            _waitingParent = new GameObject("WaitingLine").transform;
            _waitingParent.SetParent(transform);
        }

        for (int i = 0; i < WAITING_SIZE; i++)
        {
            Vector3 pos = transform.position
                + transform.forward * (level.cellSize * (level.height / 2 + 1))
                + transform.right * (level.cellSize * (i - WAITING_SIZE / 2))
                + new Vector3(0f, 0f, level.cellSize)
                + shift;

            var slot = new WaitingSlot();
            slot.slotCell = new GridCell(-1, -1, pos);

            GameObject placeholder = new GameObject($"WaitingSlot_{i}");
            placeholder.transform.SetParent(_waitingParent, true);
            placeholder.transform.position = pos + new Vector3(0f, .55f, 0f);
            slot.placeholder = placeholder;
            slot.slotCell.SetOccupyingObject(placeholder);

            slot.occupant = null; // initially empty
            _waitingSlots[i] = slot;

        }
        _waitingLineCount = 0;
        Debug.Log("Waiting line initialized with WAITING_SIZE slots.");
    }

    public void PreparePassengerList()
    {
        if (level == null)
        {
            Debug.LogWarning("LevelData not assigned. Cannot prepare passenger list.");
            return;
        }
        passengerSpawnCoords.Clear();
        passengerCount = 0;

        for (int x = 0; x < level.width; x++)
        {
            for (int y = 0; y < level.height; y++)
            {
                if (level.GetCell(x, y) == CellType.ColorPassenger)
                {
                    passengerSpawnCoords.Add(new Vector2Int(x, y));
                    passengerCount++;
                }
            }
        }
    }

    void SpawnObstacles()
    {
        if (passengerPrefab == null)
        {
            Debug.LogError("Obstacle Prefab not assigned!");
            return;
        }

        Transform existing = transform.Find("Obstacles");
        Transform obstacleParent;
        if (existing != null)
        {
            obstacleParent = existing;
        }
        else
        {
            obstacleParent = new GameObject("Obstacles").transform;
            obstacleParent.SetParent(this.transform);
        }

        for (int x = 0; x < level.width; x++)
        {
            for (int y = 0; y < level.height; y++)
            {
                if (level.GetCell(x, y) == CellType.Obstacle)
                {
                    GridCell cell = GetGridCell(x, y);
                    if (cell != null && !cell.IsOccupied())
                    {
                        Vector3 spawnPos = cell.worldPos + new Vector3(0f, .55f, 0f);
                        GameObject obstacleInstance = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity, obstacleParent);
                        obstacleInstance.name = $"Obstacle_({x},{y})";
                        cell.SetOccupyingObject(obstacleInstance); // Mark the cell as occupied
                    }
                }
            }
        }

    }

    void SpawnPipes()
    {
        if (pipePrefab == null)
        {
            Debug.LogError("Pipe Prefab not assigned!");
            return;
        }
        Transform existing = transform.Find("Pipes");
        Transform pipeParent;
        if (existing != null)
        {
            pipeParent = existing;
        }
        else
        {
            pipeParent = new GameObject("Pipes").transform;
            pipeParent.SetParent(this.transform);
        }
        for (int x = 0; x < level.width; x++)
        {
            for (int y = 0; y < level.height; y++)
            {
                if (level.GetCell(x, y) == CellType.Pipe)
                { 
                    GridCell cell = GetGridCell(x, y);
                    if (cell != null && !cell.IsOccupied())
                    {
                        Vector3 spawnPos = cell.worldPos + new Vector3(0f, .55f, 0f);
                        GameObject pipeInstance = Instantiate(pipePrefab, spawnPos, pipePrefab.transform.rotation, pipeParent);
                        pipeInstance.name = $"Pipe_({x},{y})";
                        pipeInstance.GetComponent<Pipe>()?.Initialize(x, y);
                        cell.SetOccupyingObject(pipeInstance); // Mark the cell as occupied
                    }
                }
            }
        }
    }

    public void SpawnPassengers()
    {
        if (passengerPrefab == null)
        {
            Debug.LogError("Passenger Prefab not assigned!");
            return;
        }

        // Create a parent object for clean hierarchy (destroy existing one to avoid duplicates)
        Transform existing = transform.Find("Passengers");
        Transform passengerParent;
        if (existing != null)
        {
            passengerParent = existing;
        }
        else
        {
            passengerParent = new GameObject("Passengers").transform;
            passengerParent.SetParent(this.transform);
        }
         
        foreach (Vector2Int coord in passengerSpawnCoords)
        {
            GridCell cell = GetGridCell(coord.x, coord.y);
            if (cell != null && !cell.IsOccupied())
            {
                // We use the pre-calculated world position from the GridCell
                Vector3 spawnPos = cell.worldPos + new Vector3(0f, .55f, 0f);
                GameObject passengerInstance = Instantiate(passengerPrefab, spawnPos, Quaternion.identity, passengerParent);
                passengerInstance.name = $"Passenger_({coord.x},{coord.y})";

                Passenger psg = passengerInstance.GetComponent<Passenger>();
                psg?.InitializeGridCoord(coord.x, coord.y);
                if (psg != null)
                    psg.onClickedByPlayer = OnPassengerClicked;

                Material mat = level.GetCellMaterial(coord.x, coord.y);
                if (mat != null)
                {
                    Renderer bodyRenderer = null;
                    Transform body = passengerInstance.transform.Find("Body");

                    if (body != null)
                        bodyRenderer = body.GetComponent<Renderer>();

                    if (bodyRenderer != null)
                    {
                        var r = passengerInstance.GetComponentInChildren<Renderer>();
                        Debug.Log($"Renderer found: {r.gameObject.name}");
                        r.material = mat;
                    }

                    if (bodyRenderer != null)
                    {
                        bodyRenderer.material = mat;
                    }
                    else
                    {
                        Debug.LogWarning($"Body renderer not found in passenger prefab instance at ({coord.x}, {coord.y}).");
                    }

                }

                var passColorManager = passengerInstance.GetComponent<PassengerColorManager>();
                if (passColorManager != null && level.cellColors != null)
                {
                    passColorManager.RefreshOriginalColor();
                    passColorManager.ApplyReachability(psg != null ? psg.isReachable : true);
                }

                cell.SetOccupyingObject(passengerInstance); // Mark the cell as occupied
            }
            else
            {
                if (cell == null)
                    Debug.LogWarning($"Spawn coordinate ({coord.x}, {coord.y}) is outside the grid bounds!");
                else
                    Debug.LogWarning($"Attempted to spawn passenger at ({coord.x}, {coord.y}), but cell is already occupied.");
            }
        }
    }

    // Calculates the world position of the center of a cell
    public Vector3 GetWorldPosition(int x, int y)
    {
        Vector3 right = transform.right;
        Vector3 forward = transform.forward;
        Vector3 center = transform.position;
        float cs = level.cellSize;
        int w = level.width;
        int h = level.height;

        Vector3 origin = center - right * ((w - 1) * cs * 0.5f) - forward * ((h - 1) * cs * 0.5f);

        return origin + right * (x * cs) + forward * (y * cs) + shift;
    }

    public GridCell GetGridCell(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < level.width && y < level.height)
        {
            return _grid[x, y];
        }
        return null;
    }

    // Event triggered by Passenger when clicked
    public void OnPassengerClicked(Passenger clicked)
    {
        if (clicked.isReachable)
        {
            // Further actions for reachable passenger can be added here
            Debug.Log($"[GridSpawner] Clicked reachable passenger at ({clicked.gridCoord.x},{clicked.gridCoord.y}).");
            if (_waitingLineCount >= _waitingSlots.Length || level.GetRemaniningTime() <= 0)
            {
                Debug.Log("GAME OVER - LOOOOSEEEERRRRR");
                return;
            }
            else
            {
                Debug.Log("Passenger added to waiting line.");

                // find first empty slot
                int slotIndex = -1;

                for (int i = 0; i < _waitingSlots.Length; i++)
                {
                    if (_waitingSlots[i].occupant == null)
                    {
                        slotIndex = i;
                        break;
                    }
                }
                if (slotIndex == -1)
                {
                    Debug.LogWarning("[GridSpawner] No empty waiting slot found despite count check.");
                    return;
                }

                WaitingSlot slot = _waitingSlots[slotIndex];

                // Clear original grid cell occupancy
                GridCell originalCell = GetGridCell(clicked.gridCoord.x, clicked.gridCoord.y);
                if (originalCell != null)
                {
                    originalCell.ClearOccupyingObject();
                }

                // Move passenger GameObject to waiting slot (no instantiate/destroy)
                clicked.transform.SetParent(_waitingParent, true);
                clicked.transform.position = slot.slotCell.worldPos + new Vector3(0f, .55f, 0f);

                // mark passenger as non-interactive and mark its gridCoord as "off-grid"
                clicked.SetInteractable(false);
                clicked.SetReachableImmediate(true);
                clicked.InitializeGridCoord(-1, -1);

                // update slot bookkeeping
                slot.occupant = clicked.gameObject;
                if (slot.placeholder != null) slot.placeholder.SetActive(false);
                slot.slotCell.SetOccupyingObject(clicked.gameObject);

                _waitingSlots[slotIndex] = slot;
                _waitingLineCount++;

                Debug.Log($"Passenger moved to waiting slot {slotIndex}. _waitingLineCount={_waitingLineCount}");

                // Trigger the boarding check now that a new passenger is waiting
                ProcessBoarding();

                // Recompute paths for remaining passengers (passengers parent only)
                ComputePathsForAllPassengers();

            }
        }
        else
        {
            Debug.Log($"[GridSpawner] Clicked passenger at ({clicked.gridCoord.x},{clicked.gridCoord.y}) but NOT reachable.");
        }
    }

    void ComputePathsForAllPassengers()
    {
        int w = level.width;
        int h = level.height;
        int frontY = h - 1;

        Transform parent = transform.Find("Passengers");
        if (parent == null) return;

        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject passengerObj = parent.GetChild(i).gameObject;
            Passenger passenger = passengerObj.GetComponent<Passenger>();
            if (passenger == null) continue;

            List<Vector2Int> shortestPath = Pathfinding.FindPathAStar(level, passenger.gridCoord.x, passenger.gridCoord.y, frontY);
            bool isReachable = shortestPath != null && shortestPath.Count > 0;

            passenger.SetReachable(isReachable);
            passenger.SetPath(shortestPath);
            //Debug.Log($"[GridSpawner] Passenger ({passenger.gridCoord.x},{passenger.gridCoord.y}) reachable={passenger.isReachable} pathLen={(shortestPath == null ? 0 : shortestPath.Count)}");
        }
    }

    public int GetPassengerCount()
    {
        return passengerCount;
    }

    // Heuristic static walkability check: treat cells that contain obstacle/pipe/wall in their enum name as blocked
    public bool IsCellStaticallyWalkable(int x, int y)
    {
        var ct = level.GetCell(x, y);
        if (ct == CellType.Obstacle || ct == CellType.Pipe)
            return false;
        return true;
    }


    /// <summary>
    /// Checks waiting passengers and boards them onto the front bus if colors match.
    /// </summary>
    public void ProcessBoarding()
    {
        Vehicle frontBus = VehicleManager.Instance.GetFrontBus();
        if (frontBus == null || frontBus.isFull)
        {
            return; // No bus or bus is already full
        }

        bool passengerBoarded = false;

        // Iterate through waiting slots to find matching passengers
        for (int i = 0; i < _waitingSlots.Length; i++)
        {
            if (frontBus.isFull) break; // Stop if bus becomes full mid-check

            WaitingSlot slot = _waitingSlots[i];
            if (slot.occupant != null)
            {
                Passenger passenger = slot.occupant.GetComponent<Passenger>();
                PassengerColorManager colorManager = slot.occupant.GetComponent<PassengerColorManager>();

                // Compare passenger color with bus color
                if (passenger != null && colorManager != null && ColorUtility.ToHtmlStringRGB(colorManager.GetOriginalColor()) == ColorUtility.ToHtmlStringRGB(frontBus.color))
                {
                    if (frontBus.TryAddPassenger(passenger.gameObject))
                    {
                        // Boarding successful
                        Destroy(passenger.gameObject); // Destroy the passenger

                        // Clear the waiting slot
                        slot.occupant = null;
                        if (slot.placeholder != null) slot.placeholder.SetActive(true);
                        slot.slotCell.ClearOccupyingObject();
                        _waitingLineCount--;

                        passengerBoarded = true;
                    }
                }
            }
        }

        // If at least one passenger boarded, compact the line to remove gaps
        if (passengerBoarded)
        {
            CompactWaitingLine();
        }
    }

    /// <summary>
    /// Reorganizes the waiting line to fill empty slots from the front.
    /// </summary>
    private void CompactWaitingLine()
    {
        // Get all passengers that are still waiting
        List<GameObject> remainingPassengers = _waitingSlots
            .Where(s => s.occupant != null)
            .Select(s => s.occupant)
            .ToList();

        // Clear all slots in the data structure
        for (int i = 0; i < _waitingSlots.Length; i++)
        {
            _waitingSlots[i].occupant = null;
            if (_waitingSlots[i].placeholder != null) _waitingSlots[i].placeholder.SetActive(true);
            _waitingSlots[i].slotCell.ClearOccupyingObject();
        }

        // Re-add the passengers to the front of the line
        for (int i = 0; i < remainingPassengers.Count; i++)
        {
            GameObject passengerObj = remainingPassengers[i];
            WaitingSlot slot = _waitingSlots[i];

            slot.occupant = passengerObj;
            if (slot.placeholder != null) slot.placeholder.SetActive(false);
            slot.slotCell.SetOccupyingObject(passengerObj);

            // Snap the passenger to the new slot's position
            passengerObj.transform.position = slot.slotCell.worldPos + new Vector3(0f, .55f, 0f);
        }
    }

    public void SetGridCell(GridCell cell)
    {
        if (cell.x >= 0 && cell.y >= 0 && cell.x < level.width && cell.y < level.height)
        {
            _grid[cell.x, cell.y] = cell;
        }
    }

    private void OnDrawGizmos()
    {
        if (level == null || !Application.isPlaying) return;

        for (int y = 0; y < level.height; y++)
        {
            for (int x = 0; x < level.width; x++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(GetWorldPosition(x, y), new Vector3(level.cellSize, 0.1f, level.cellSize));
            }
        }

        for (int i = 0; i < WAITING_SIZE; i++)
        {
            Vector3 pos = transform.position
                + transform.forward * (level.cellSize * (level.height / 2 + 1))
                + transform.right * (level.cellSize * (i - WAITING_SIZE / 2))
                + new Vector3(0f, 0f, level.cellSize);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(pos + shift, new Vector3(level.cellSize, 0.1f, level.cellSize));
        }

    }


}