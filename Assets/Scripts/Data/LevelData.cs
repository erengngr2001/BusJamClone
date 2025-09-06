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
    [Header("Grid Settings")]
    public int width = 5;      // columns
    public int height = 3;     // rows
    public float cellSize = 2f;// size of one cell in world units

    [Header("Countdown for Losing")]
    [SerializeField] float countdown = 90f; // seconds for the level

    // flattened row-major list: index = y * width + x
    public List<CellType> cells = new List<CellType>();

    // parallel per-cell colors (used only when the cell is a passenger type)
    public List<Color> cellColors = new List<Color>();

    // If assigned, spawner/other systems can use these
    // to set the passenger's material at runtime (use GetCellMaterial in spawner).
    public List<Material> cellMaterials = new List<Material>();

    // Unity callback in the editor when values change — keeps the list in sync
    private void OnValidate()
    {
        ResizeCells();
    }

    // Ensure the cells list size matches width*height
    public void ResizeCells()
    {
        int target = Mathf.Max(0, width * height);
        if (cells == null) cells = new List<CellType>();
        if (cellColors == null) cellColors = new List<Color>();
        if (cellMaterials == null) cellMaterials = new List<Material>();

        if (cells.Count > target)
        {
            cells.RemoveRange(target, cells.Count - target);
        }
        else
        {
            while (cells.Count < target)
                cells.Add(CellType.Empty);
        }

        // Keep colors in sync with cells list length
        if (cellColors.Count > target)
        {
            cellColors.RemoveRange(target, cellColors.Count - target);
        }
        else
        {
            while (cellColors.Count < target)
                cellColors.Add(Color.white); // default color
        }

        // Keep materials in sync with cells list length
        if (cellMaterials.Count > target)
        {
            cellMaterials.RemoveRange(target, cellMaterials.Count - target);
        }
        else
        {
            while (cellMaterials.Count < target)
                cellMaterials.Add(null); // default no material
        }
    }

    // Safe accessor
    public CellType GetCell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) 
            return CellType.Empty;
        int idx = y * width + x;
        if (idx < 0 || idx >= cells.Count) 
            return CellType.Empty;
        return cells[idx];
    }

    // Safe setter
    public void SetCell(int x, int y, CellType t)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) 
            return;
        int idx = y * width + x;
        if (idx < 0 || idx >= cells.Count) 
            return;
        cells[idx] = t;
    }

    // Safe color accessor (returns white if out-of-range)
    public Color GetCellColor(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return Color.white;
        int idx = y * width + x;
        if (idx < 0 || idx >= cellColors.Count) return Color.white;
        return cellColors[idx];
    }

    // Safe color setter
    public void SetCellColor(int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        int idx = y * width + x;
        if (idx < 0 || idx >= cellColors.Count) return;
        cellColors[idx] = c;
    }

    // Safe material accessor (returns null if out-of-range)
    public Material GetCellMaterial(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return null;
        int idx = y * width + x;
        if (idx < 0 || idx >= cellMaterials.Count) return null;
        return cellMaterials[idx];
    }

    // Safe material setter
    public void SetCellMaterial(int x, int y, Material mat)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        int idx = y * width + x;
        if (idx < 0 || idx >= cellMaterials.Count) return;
        cellMaterials[idx] = mat;
    }

    public float GetRemaniningTime()
    {
        return countdown;
    }
}
