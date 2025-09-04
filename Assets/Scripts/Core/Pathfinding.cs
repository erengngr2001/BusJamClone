using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Pathfinding
{
    // Manhattan-style heuristic: distance to nearest front row cell (admissible)
    public static int ManhattanHeuristic(int y, int frontY)
    {
        return Mathf.Abs(y - frontY);
    }

    // WILL BE IMPLEMENTED TOMORROW - placeholder for A* pathfinding
    public static List<Vector2Int> FindPathAStar(LevelData level, int startX, int startY, int targetY)
    {
        int w = level.width;
        int h = level.height;

        //GridCell startCell = GridSpawner.Instance.GetGridCell(startX, startY);
        Vector2Int startCell = new Vector2Int(startX, startY);

        Vector2Int[] neighbors = new Vector2Int[] {
            new Vector2Int(1,0),
            new Vector2Int(-1,0),
            new Vector2Int(0,1),
            new Vector2Int(0,-1)
        };

        if (startY == targetY)
        {
            // Already at the target row
            return new List<Vector2Int>() { startCell };
        }

        var openSet = new PriorityQueue<Vector2Int>(GridSpawner.Instance.GetPassengerCount()); // Nodes to be evaluated
        var closedSet = new HashSet<Vector2Int>(); // Set of nodes already evaluated
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>(); // For path reconstruction
        var gScore = new Dictionary<Vector2Int, int>(); // Score for cost from start to current node

        gScore[startCell] = 0;
        int startF = ManhattanHeuristic(startY, targetY);
        openSet.Enqueue(startCell, startF);

        while (openSet.Count() > 0)
        {
            Vector2Int current = openSet.Dequeue();

            if (closedSet.Contains(current)) continue;
            closedSet.Add(current);

            if (current.y == targetY)
            {
                var cell = GridSpawner.Instance.GetGridCell(current.x, current.y);

                // Ensure the target cell is walkable and unoccupied, or it's the starting cell
                //if (GridSpawner.Instance.IsCellStaticallyWalkable(current.x, current.y) && cell != null && !cell.IsOccupied() && current.x == startX && current.y == startY)
                if (GridSpawner.Instance.IsCellStaticallyWalkable(current.x, current.y) && cell != null && (!cell.IsOccupied() || (current.x == startX && current.y == startY)))
                {
                    return ReconstructPath(cameFrom, current, startCell);
                }
            }

            int currentG = gScore.ContainsKey(current) ? gScore[current] : int.MaxValue;

            foreach (var d in neighbors)
            {
                int nx = current.x + d.x;
                int ny = current.y + d.y;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;

                Vector2Int neighbor = new Vector2Int(nx, ny);

                // static walkability
                if (!GridSpawner.Instance.IsCellStaticallyWalkable(nx, ny)) continue;

                // dynamic occupancy: block if occupied and not the starting cell
                var nCell = GridSpawner.Instance.GetGridCell(nx, ny);
                if (nCell != null && nCell.IsOccupied() && !(nx == startX && ny == startY)) continue;

                if (closedSet.Contains(neighbor)) continue;

                int tentativeG = currentG + 1;

                bool better = false;
                if (!gScore.ContainsKey(neighbor))
                {
                    better = true;
                }
                else if (tentativeG < gScore[neighbor])
                {
                    better = true;
                }

                if (better)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    int f = tentativeG + ManhattanHeuristic(ny, targetY);
                    // Allow multiple entries for the same node; when dequeued we skip processed ones using closed set.
                    openSet.Enqueue(neighbor, f);
                }
            }

        }

        return null;

    }

    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current, Vector2Int startCell)
    {
        var path = new List<Vector2Int>();
        
        Vector2Int node = current;
        path.Add(current);
        while (!(node.x == startCell.x && node.y == startCell.y))
        {
            if (cameFrom.ContainsKey(node))
            {
                node = cameFrom[node];
                path.Add(node);
            }
            else
            {
                // No path found
                break;
            }
        }

        path.Reverse();
        return path;
    }
}
