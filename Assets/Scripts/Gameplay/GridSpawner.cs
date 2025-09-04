using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    public Vector3 shift = new Vector3(0f, 0f, -2f); // to center of cell

    public LevelData level;            // assign the LevelData asset
    public GameObject passengerPrefab; // assign passenger prefab
    public List<Vector2Int> passengerSpawnCoords;

    // Grid data
    private GridCell[,] _grid;

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
        }
    }

    private void Start()
    {
        if (passengerSpawnCoords == null || passengerSpawnCoords.Count == 0)
        {
            PreparePassengerList();
        }
        SpawnPassengers();
    }

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

    public void PreparePassengerList()
    {
        if (level == null)
        {
            Debug.LogWarning("LevelData not assigned. Cannot prepare passenger list.");
            return;
        }
        passengerSpawnCoords.Clear();

        for (int x = 0; x < level.width; x++)
        {
            for (int y = 0; y < level.height; y++)
            {
                if (level.GetCell(x, y) == CellType.ColorPassenger)
                    passengerSpawnCoords.Add(new Vector2Int(x, y));
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
                //passengerInstance.GetComponent<Passenger>()?.InitializeGridCoord(coord.x, coord.y);
                Passenger psg = passengerInstance.GetComponent<Passenger>();
                psg?.InitializeGridCoord(coord.x, coord.y);
                if (psg != null)
                    psg.onClickedByPlayer = OnPassengerClicked;
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
        Debug.Log($"[GridSpawner] Passenger clicked at ({clicked.gridCoord.x},{clicked.gridCoord.y}). Computing paths for all passengers...");
        ComputePathsForAllPassengers();
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
            passenger.isReachable = isReachable;
            passenger.SetPath(shortestPath);
            Debug.Log($"[GridSpawner] Passenger ({passenger.gridCoord.x},{passenger.gridCoord.y}) reachable={passenger.isReachable} pathLen={(shortestPath == null ? 0 : shortestPath.Count)}");
        }
    }


    private void OnDrawGizmos()
    {
        if (level == null || !Application.isPlaying) return;

        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        for (int y = 0; y < level.height; y++)
        {
            for (int x = 0; x < level.width; x++)
            {
                Gizmos.DrawWireCube(GetWorldPosition(x, y), new Vector3(level.cellSize, 0.1f, level.cellSize));
                Gizmos.color = Color.red;
            }
        }
    }

}
