using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum CellType
{
    Empty,
    ColorPassenger,
    HiddenColorPassenger,
    Obstacle,
    Pipe
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Level/Simple Level Data")]
public class LevelData : ScriptableObject
{
    public int width = 5;      // columns
    public int height = 3;     // rows
    public float cellSize = 1f;// size of one cell in world units
    [SerializeField] float countdown = 90f;

    // flattened row-major list: index = y * width + x
    public List<CellType> cells = new List<CellType>();

    // Ensure the cells list size matches width*height
    public void ResizeCells()
    {
        int target = Mathf.Max(0, width * height);
        if (cells == null) cells = new List<CellType>();

        if (cells.Count > target)
        {
            cells.RemoveRange(target, cells.Count - target);
        }
        else
        {
            while (cells.Count < target)
                cells.Add(CellType.Empty);
        }
    }

    // Safe accessor
    public CellType GetCell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return CellType.Empty;
        int idx = y * width + x;
        if (idx < 0 || idx >= cells.Count) return CellType.Empty;
        return cells[idx];
    }

    // Safe setter
    public void SetCell(int x, int y, CellType t)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        int idx = y * width + x;
        if (idx < 0 || idx >= cells.Count) return;
        cells[idx] = t;
    }

    // Unity callback in the editor when values change — keeps the list in sync
    private void OnValidate()
    {
        ResizeCells();
    }
}
