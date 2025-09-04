using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        //var pq = new PriorityQueue<(int x, int y), int>();

        int w = level.width;
        int h = level.height;

        GridCell startCell = GridSpawner.Instance.GetGridCell(startX, startY);

        Vector2Int[] neighbors = new Vector2Int[] {
            new Vector2Int(1,0),
            new Vector2Int(-1,0),
            new Vector2Int(0,1),
            new Vector2Int(0,-1)
        };

        if (startY == targetY)
        {
            // Already at the target row
            return new List<Vector2Int>() { new Vector2Int(startX, startY) };
        }

        // A* pathfinding implementation would go here
        // This is a placeholder for the actual pathfinding logic
        return new List<Vector2Int>();
    }
}
