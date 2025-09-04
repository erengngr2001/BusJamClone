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
    public static List<Vector2Int> FindPathAStar(GridCell[,] _grid, int startX, int startY, int targetY)
    {
        // A* pathfinding implementation would go here
        // This is a placeholder for the actual pathfinding logic
        return new List<Vector2Int>();
    }
}
