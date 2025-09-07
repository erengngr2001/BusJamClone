using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pipe : MonoBehaviour
{
    const string SPAWN_POINT_NAME = "PassengerSpawnPoint";
    private Transform _passengerSpawnPoint;

    [Header("# of Passengers in Pipe")]
    public int pipePoolSize = 3;

    [HideInInspector] public int x;
    [HideInInspector] public int y;

    //private void Awake()
    //{
    //    TryCreateSpawnPoint();
    //}

    public void Initialize(int x, int y)
    {
        this.x = x;
        this.y = y;
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
        //// parent without preserving world position so localPosition is applied predictably
        //go.transform.SetParent(this.transform, false);
        ////go.transform.localPosition = Vector3.forward * cellSize;
        //go.transform.localRotation = Quaternion.identity;
        //go.transform.position = this.transform.position + Vector3.forward * cellSize;

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




        //float pipeX = this.transform.position.x;
        //float pipeZ = this.transform.position.z; // z is y in grid terms
        //float psgX = _passengerSpawnPoint.position.x;
        //float psgZ = _passengerSpawnPoint.position.z;

        ////int cellX = -1;
        ////int cellY = -1;
        ////GridCell psgSpawnerCell = null;

        //if (pipeX == psgX && pipeZ < psgZ)
        //{
        //    return new GridCell(pipeCell.x, pipeCell.y + 1, _passengerSpawnPoint.position);
        //}
        //else if (pipeX == psgX && pipeZ > psgZ)
        //{
        //    return new GridCell(pipeCell.x, pipeCell.y - 1, _passengerSpawnPoint.position);
        //}
        //else if (pipeX < psgX && pipeZ == psgZ)
        //{
        //    return new GridCell(pipeCell.x + 1, pipeCell.y, _passengerSpawnPoint.position);
        //}
        //else if (pipeX > psgX && pipeZ == psgZ)
        //{
        //    return new GridCell(pipeCell.x - 1, pipeCell.y, _passengerSpawnPoint.position);
        //}
        //else
        //{
        //    Debug.LogWarning($"Pipe at ({x},{y}) has a misaligned PassengerSpawnPoint.");
        //    return null;
        //}

    }

}
